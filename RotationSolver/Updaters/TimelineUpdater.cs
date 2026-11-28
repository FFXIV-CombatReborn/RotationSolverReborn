// Runs a duty's timeline.csv and the job's plan file: keeps the fight clock in sync, answers "what is coming" for the rotations, and queues planned actions.
using ECommons.DalamudServices;
using ECommons.Logging;

namespace RotationSolver.Updaters;

// Settings a rotation exposes to the timeline runner.
internal interface ICdPlanRotation
{
	bool UseTimelinePlan { get; }

	bool AllowPlannedAction(uint actionId) => true;

	bool ShowTimelineMessages => true;

	string? PlannedActionWait(uint actionId) => null;

	bool UsePlanSettings => false;

	bool RotationRunsPlannedAction(uint actionId) => false;

	// Planned actions the player can opt in / out of, with the bool setting that controls each (shown in the CD Plan section).
	IEnumerable<(uint ActionId, string Config)> OptionalPlannedActions => [];

	// Show the CD Plan timeline window (the plan's next few seconds).
	bool ShowPlanTimeline => false;

	// DPS: the action that comes from a shared mitigation profile (Feint for melee, Addle for casters), and which
	// profile to use (Feint: M1 / M2, Addle: R2 / Fake melee) or the job plan.
	string MitProfileAction => string.Empty;

	MitProfileChoice MitProfile => MitProfileChoice.JobPlan;

	// Tank: follow the plan's "Tank Stance" On / Off rows.
	bool UsePlannedTankStance => false;
}

// One plan row for the CD Plan timeline window.
internal enum CdPlanPreviewState : byte
{
	Upcoming,
	Queued,
	Waiting,
	Done,
	Skipped,
	Off,
	RotationRuns,
}

internal sealed record CdPlanPreview(IAction Action, bool Gcd, string Phase, float Start, float End, string Target,
	CdPlanPreviewState State, string? Note);

// How a timeline Damage row hits: a light or heavy raidwide, or a tankbuster.
internal enum TimelineDamageKind : byte
{
	Light,
	Heavy,
	Tankbuster,
}

internal static class TimelineUpdater
{
	#region State

	// Loaded rows and the live phase clock.
	// Plans only run for rotations that opt in (ICdPlanRotation).
	private static bool PlanAllowed => DataCenter.CurrentRotation is ICdPlanRotation { UseTimelinePlan: true };

	private sealed record SyncRow(string Phase, string Name, string Watch, uint Oid, HashSet<uint> ActionIds, float PhaseTime);
	private sealed record PlanRow(string Phase, float Start, float End, string Action, string Target);

	private static string? _loadedFor;
	private static List<SyncRow> _sync = [];
	private sealed record DowntimeRow(string Phase, uint Oid, HashSet<string> Jobs, float PhaseTime);
	private static List<DowntimeRow> _downtime = [];
	private sealed record DamageRow(string Phase, string Name, TimelineDamageKind Kind, float PhaseTime);
	private static List<DamageRow> _damage = [];
	private static List<PlanRow> _plan = [];

	// Every loaded row (job plan + the shared mitigation profiles, e.g. M1 / M2 Feint) and where it came from. _plan holds
	// the same rows in the same order, with the ones not in use (e.g. the other profile) replaced by OffRow, so queued row
	// indices stay valid.
	private const string JobGroup = "Job";
	private const string JobMitGroup = "JobMit";
	private static readonly PlanRow OffRow = new(string.Empty, 0f, 0f, string.Empty, string.Empty);
	private static List<PlanRow> _allPlan = [];
	private static List<string> _planGroup = [];
	private static readonly HashSet<string> _profileSlots = [];
	private static MitProfileKind? _profileKind;
	private static string? _activeSlot;
	private static bool _autoSecond;
	private static string _autoDuty = string.Empty;

	private static int _lastSync = -1;
	private static string? _phase;
	private static DateTime _phaseStart;
	private static readonly HashSet<uint> _seenOids = [];
	private static readonly HashSet<int> _queued = [];

	public static string Status => _phase == null ? "idle" : $"{_phase} {PhaseTime:F1}s";

	private static float PhaseTime => (float)(DateTime.Now - _phaseStart).TotalSeconds;

	private static readonly Dictionary<int, string> _waiting = [];
	private static readonly Dictionary<int, DateTime> _queuedAt = [];
	private const float PendingAbilityReadySeconds = 1f;

	private const int SyncLookahead = 4;

	private static (ulong Id, uint Action) _castLock;

	private static DateTime _nextFileCheck;
	private static int _candidateCount;

	private static readonly Dictionary<string, string> _planOptions = new(StringComparer.OrdinalIgnoreCase);

	#endregion

	#region Update and Phase Sync

	// Each frame: start the clock on pull, re-sync it on sync rows, reset when combat ends.
	private const string LockedOutTag = "LockedOut";

	private static readonly (StatusID Hero, StatusID Villain)[] HeroVillainPairs =
	[
		(StatusID.EpicHero, StatusID.EpicVillain),
		(StatusID.VauntedHero, StatusID.VauntedVillain),
		(StatusID.FatedHero, StatusID.FatedVillain),
	];

	private static uint _lockedBoss;

	private static void TrackLockedBoss()
	{
		if (_phase == null || ECommons.GameHelpers.Player.Object is not { } player)
		{
			return;
		}

		foreach (var (hero, villain) in HeroVillainPairs)
		{
			if (!player.HasStatus(false, hero))
			{
				continue;
			}

			foreach (var enemy in DataCenter.AllHostileTargets)
			{
				if (enemy != null && enemy.HasStatus(false, villain))
				{
					_lockedBoss = enemy.BaseId;
					return;
				}
			}
		}
	}

	public static void Update(bool rsrOn)
	{
		try
		{
			EnsureLoaded();
			ApplyProfile();
			ApplyPlanSettings();
			if (_sync.Count == 0)
			{
				return;
			}

			if (!FightActive())
			{
				if (_phase != null)
				{
					Info("nobody in the party is in combat any more: timeline reset");
				}

				Reset();
				return;
			}

			TrackOids();
			TrackLockedBoss();

			if (_phase == null)
			{
				var startRow = 0;
				for (var i = 0; i < _sync.Count; i++)
				{
					if (_sync[i].Watch == "CombatStart" && (_sync[i].Oid == 0 || AnyEnemy(_sync[i].Oid)))
					{
						startRow = i;
					}
				}

				_lastSync = startRow;
				_phase = _sync[startRow].Phase;
				var selfInCombat = Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
				_phaseStart = DateTime.Now.AddSeconds(selfInCombat ? -DataCenter.CombatTimeRaw : 0);
				Info($"start {_phase} at {PhaseTime:F1}s");
			}

			ReleaseCastLock();
			for (var i = _lastSync + 1; i < _sync.Count; i++)
			{
				var row = _sync[i];
				if (i > _lastSync + SyncLookahead && !(row.Phase != _phase && IsPhaseFirstRow(i)))
				{
					continue;
				}

				if (!Detect(row, out var castNpc))
				{
					continue;
				}

				if (castNpc != null)
				{
					_castLock = (castNpc.GameObjectId, castNpc.CastActionId);
				}

				var drift = row.Phase == _phase ? PhaseTime - row.PhaseTime : 0f;
				_lastSync = i;
				_phase = row.Phase;
				_phaseStart = DateTime.Now.AddSeconds(-row.PhaseTime);
				Info($"sync '{row.Name}' -> {_phase} {row.PhaseTime:F1}s (drift {drift:+0.0;-0.0}s)");
				break;
			}

			var dead = ECommons.GameHelpers.Player.Object is not { } self || self.IsDead || self.CurrentHp == 0;
			RunPlan(rsrOn && !dead, dead ? "you were dead" : "RSR was off");
		}
		catch (Exception ex)
		{
			PluginLog.Error($"[Timeline] {ex}");
		}
	}

	private static bool FightActive()
	{
		if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
		{
			return true;
		}

		foreach (var p in Svc.Party)
		{
			if (p.GameObject is ICharacter c && !c.IsDead && c.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat))
			{
				return true;
			}
		}

		return false;
	}

	private static void ReleaseCastLock()
	{
		if (_castLock.Id == 0)
		{
			return;
		}

		foreach (var obj in Svc.Objects)
		{
			if (obj is IBattleNpc npc && npc.GameObjectId == _castLock.Id && npc.IsCasting && npc.CastActionId == _castLock.Action)
			{
				return;
			}
		}

		_castLock = default;
	}

	private static bool IsPhaseFirstRow(int i) => i > 0 && _sync[i].Phase != _sync[i - 1].Phase;

	private static bool AnyEnemy(uint oid)
	{
		foreach (var npc in Enemies(oid))
		{
			if (!npc.IsDead)
			{
				return true;
			}
		}

		return false;
	}

	private static bool Detect(SyncRow row, out IBattleNpc? castNpc)
	{
		castNpc = null;
		switch (row.Watch)
		{
			case "CombatStart":
				if (row.Oid == 0)
				{
					return false;
				}

				foreach (var npc in Enemies(row.Oid))
				{
					if (npc.IsTargetable && !npc.IsDead)
					{
						return true;
					}
				}

				return false;
			case "Gone":
				if (!_seenOids.Contains(row.Oid))
				{
					return false;
				}

				foreach (var npc in Enemies(row.Oid))
				{
					if (!npc.IsDead && (npc.IsTargetable || npc.CurrentHp >= npc.MaxHp))
					{
						return false;
					}
				}

				return true;
		}

		foreach (var npc in Enemies(row.Oid))
		{
			switch (row.Watch)
			{
				case "CastStart" when npc.IsCasting && row.ActionIds.Contains(npc.CastActionId) &&
					!(npc.GameObjectId == _castLock.Id && npc.CastActionId == _castLock.Action):
					castNpc = npc;
					return true;
				case "Targetable" when npc.IsTargetable && !npc.IsDead:
				case "Untargetable" when !npc.IsTargetable && !npc.IsDead:
					return true;
			}
		}

		return false;
	}

	private static IEnumerable<IBattleNpc> Enemies(uint oid)
	{
		foreach (var obj in Svc.Objects)
		{
			if (obj is IBattleNpc npc && npc.BaseId == oid)
			{
				yield return npc;
			}
		}
	}

	private static void TrackOids()
	{
		foreach (var row in _sync)
		{
			if (row.Oid == 0 || _seenOids.Contains(row.Oid))
			{
				continue;
			}

			foreach (var npc in Enemies(row.Oid))
			{
				_ = _seenOids.Add(row.Oid);
				break;
			}
		}
	}

	private static void Reset()
	{
		_phase = null;
		_lastSync = -1;
		_seenOids.Clear();
		_queued.Clear();
		_waiting.Clear();
		_queuedAt.Clear();
		_castLock = default;
		_lockedBoss = 0;
	}

	#endregion

	#region Damage and Downtime Queries

	// What the timeline says is coming: raidwides, tankbusters and when a target leaves.
	public static bool HasDamageRows => _phase != null && _damage.Count > 0;

	public static bool HasTankbusterRows
	{
		get
		{
			if (_phase == null)
			{
				return false;
			}

			foreach (var row in _damage)
			{
				if (row.Kind == TimelineDamageKind.Tankbuster)
				{
					return true;
				}
			}

			return false;
		}
	}

	public static bool NextDamage(float within, bool tankbuster, out float inSeconds, out TimelineDamageKind kind)
	{
		inSeconds = float.MaxValue;
		kind = TimelineDamageKind.Light;
		if (_phase == null || _damage.Count == 0)
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var row in _damage)
		{
			if (row.Phase != _phase || row.PhaseTime < t || row.PhaseTime > t + within ||
				(row.Kind == TimelineDamageKind.Tankbuster) != tankbuster)
			{
				continue;
			}

			var at = row.PhaseTime - t;
			if (at < inSeconds)
			{
				inSeconds = at;
				kind = row.Kind;
			}
		}

		return inSeconds != float.MaxValue;
	}

	public static float? SecondsUntilDowntime(uint oid)
	{
		if (_phase == null || oid == 0)
		{
			return null;
		}

		var t = PhaseTime;
		var job = ECommons.GameHelpers.Player.Available ? ECommons.GameHelpers.Player.Job.ToString() : string.Empty;
		float? best = null;
		for (var i = _lastSync + 1; i < _sync.Count; i++)
		{
			var row = _sync[i];
			if (row.Phase != _phase)
			{
				break;
			}

			if ((row.Watch == "Untargetable" || row.Watch == "Gone") && row.Oid == oid && row.PhaseTime > t)
			{
				best = row.PhaseTime - t;
				break;
			}
		}

		foreach (var row in _downtime)
		{
			if (row.Phase == _phase && (row.Oid == 0 || row.Oid == oid) && row.PhaseTime > t &&
				(row.Jobs.Count == 0 || row.Jobs.Contains(job) || (row.Jobs.Contains(LockedOutTag) && _lockedBoss != 0 && _lockedBoss != row.Oid)) &&
				(best == null || row.PhaseTime - t < best))
			{
				best = row.PhaseTime - t;
			}
		}

		return best;
	}

	#endregion

	#region Plan Queries

	// What the plan wants around now, for rotations to hold or reserve resources.
	public static bool PlanContains(uint actionId)
	{
		if (!PlanAllowed)
		{
			return false;
		}

		foreach (var row in _plan)
		{
			if (RowActionId(row) == actionId)
			{
				return true;
			}
		}

		return false;
	}

	// A row for this action name is open now and nothing by that name was used since it opened (for rows the rotation runs itself).
	public static bool PlannedOpenByName(string actionName)
	{
		if (_phase == null || !PlanAllowed)
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var row in _plan)
		{
			if (row.Phase == _phase && t >= row.Start && t < row.End &&
				row.Action.Equals(actionName, StringComparison.OrdinalIgnoreCase) &&
				!UsedSinceByName(actionName, _phaseStart.AddSeconds(row.Start)))
			{
				return true;
			}
		}

		return false;
	}

	private static bool UsedSinceByName(string actionName, DateTime since)
	{
		foreach (var rec in DataCenter.RecordActions)
		{
			if (rec.UsedTime < since)
			{
				break;
			}

			if (rec.Action.Name.ExtractText().Equals(actionName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	public static bool PlannedWithin(float seconds, params ReadOnlySpan<uint> actionIds) =>
		AnyPlanRow(actionIds, seconds, static (r, t, s) => r.End > t && r.Start <= t + s);

	public static bool PlannedForcedNow(params ReadOnlySpan<uint> actionIds) =>
		AnyPlanRow(actionIds, 0f, static (r, t, _) => t >= r.Start && t < r.End && r.Target.Equals(ForceTarget, StringComparison.OrdinalIgnoreCase));

	public static void UpcomingStarts(float seconds, List<float> into, params ReadOnlySpan<uint> actionIds)
	{
		into.Clear();
		if (_phase == null || !PlanAllowed)
		{
			return;
		}

		var t = PhaseTime;
		for (var i = 0; i < _plan.Count; i++)
		{
			var row = _plan[i];
			if (row.Phase != _phase || row.End <= t || row.Start > t + seconds)
			{
				continue;
			}

			var id = RowActionId(row);
			if (_queued.Contains(i) && (!_queuedAt.TryGetValue(i, out var queuedAt) || UsedSince(id, queuedAt)))
			{
				continue;
			}

			if (IdIn(id, actionIds))
			{
				into.Add(MathF.Max(0f, row.Start - t));
			}
		}

		into.Sort();
	}

	public static int PendingPlannedAbilities()
	{
		if (_phase == null || !PlanAllowed || _queuedAt.Count == 0)
		{
			return 0;
		}

		var t = PhaseTime;
		var count = 0;
		foreach (var (i, queuedAt) in _queuedAt)
		{
			var row = _plan[i];
			if (row.Phase != _phase || t >= row.End ||
				FindAction(row.Action) is not IBaseAction action ||
				action.Info.IsRealGCD ||
				!ActionAllowed(action.ID) ||
				!action.Cooldown.WillHaveOneCharge(PendingAbilityReadySeconds) ||
				UsedSince(action.ID, queuedAt))
			{
				continue;
			}

			count++;
		}

		return count;
	}

	public static bool PlannedWindowClosesWithin(float seconds, params ReadOnlySpan<uint> actionIds) =>
		AnyPlanRow(actionIds, seconds, static (r, t, s) => r.End > t && r.End <= t + s);

	public static bool PlannedAround(float seconds, params ReadOnlySpan<uint> actionIds) =>
		AnyPlanRow(actionIds, seconds, static (r, t, s) => r.Start <= t + s && r.End >= t - s);

	private static bool AnyPlanRow(ReadOnlySpan<uint> actionIds, float arg, Func<PlanRow, float, float, bool> when)
	{
		if (_phase == null || !PlanAllowed)
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var row in _plan)
		{
			if (row.Phase == _phase && when(row, t, arg) && IdIn(RowActionId(row), actionIds))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IdIn(uint id, ReadOnlySpan<uint> actionIds)
	{
		foreach (var wanted in actionIds)
		{
			if (id == wanted)
			{
				return true;
			}
		}

		return false;
	}

	private const string KeepPrefix = "Keep:";

	private const string WardModeRow = "Ward Mode";
	private const string TopUpAllRow = "Top Up All";

	public static bool PlannedWardMode => PlannedFlagActive(WardModeRow);

	public static bool PlannedTopUpAll => PlannedFlagActive(TopUpAllRow);

	// Tank: "Tank Stance" rows with Target On / Off say whether stance should be on during the row, not a press.
	private const string TankStanceRow = "Tank Stance";

	public static bool? PlannedTankStance
	{
		get
		{
			if (_phase == null || !PlanAllowed || DataCenter.CurrentRotation is not ICdPlanRotation { UsePlannedTankStance: true })
			{
				return null;
			}

			var t = PhaseTime;
			foreach (var row in _plan)
			{
				if (row.Phase == _phase && t >= row.Start && t < row.End &&
					row.Action.Equals(TankStanceRow, StringComparison.OrdinalIgnoreCase))
				{
					return !row.Target.Equals("Off", StringComparison.OrdinalIgnoreCase);
				}
			}

			return null;
		}
	}

	private static bool IsFlagRow(PlanRow row) =>
		row.Action.Equals(WardModeRow, StringComparison.OrdinalIgnoreCase) ||
		row.Action.Equals(TopUpAllRow, StringComparison.OrdinalIgnoreCase) ||
		row.Action.Equals(TankStanceRow, StringComparison.OrdinalIgnoreCase);

	private static bool PlannedFlagActive(string flag)
	{
		if (_phase == null || !PlanAllowed)
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var row in _plan)
		{
			if (row.Phase == _phase && t >= row.Start && t < row.End &&
				row.Action.Equals(flag, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	public static int KeepCharges(uint actionId)
	{
		if (_phase == null || !PlanAllowed)
		{
			return 0;
		}

		var t = PhaseTime;
		var keep = 0;
		foreach (var row in _plan)
		{
			if (row.Phase != _phase || t < row.Start || t >= row.End ||
				!row.Action.StartsWith(KeepPrefix, StringComparison.OrdinalIgnoreCase) ||
				FindAction(row.Action[KeepPrefix.Length..].Trim())?.ID != actionId)
			{
				continue;
			}

			if (int.TryParse(row.Target, out var n) && n > keep)
			{
				keep = n;
			}
		}

		return keep;
	}

	public static bool HoldQueuedPlannedAction(uint actionId)
	{
		if (_phase == null || _queuedAt.Count == 0 || (PlanAllowed && ActionAllowed(actionId)))
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var (i, _) in _queuedAt)
		{
			var row = _plan[i];
			if (row.Phase == _phase && t < row.End && FindAction(row.Action)?.ID == actionId)
			{
				return true;
			}
		}

		return false;
	}

	public static bool HasPlan => PlanAllowed && _plan.Count > 0;

	// Fills the CD Plan timeline window: plan rows whose window is open now or opens within `seconds`.
	// Before the pull the first phase is previewed from the countdown (or held at 0s).
	public static bool PreviewPlan(float seconds, List<CdPlanPreview> into, out float now, out bool pulled)
	{
		into.Clear();
		pulled = _phase != null;
		now = 0f;
		if (!HasPlan)
		{
			return false;
		}

		string phase;
		if (pulled)
		{
			phase = _phase!;
			now = PhaseTime;
		}
		else
		{
			phase = _sync.Count > 0 ? _sync[0].Phase : _plan[0].Phase;
			now = Service.CountDownTime > 0f ? -Service.CountDownTime : 0f;
		}

		for (var i = 0; i < _plan.Count; i++)
		{
			var row = _plan[i];
			if (row.Phase != phase || row.End <= now || row.Start > now + seconds ||
				row.Action.StartsWith(KeepPrefix, StringComparison.OrdinalIgnoreCase) || IsFlagRow(row) ||
				FindAction(row.Action) is not { } action)
			{
				continue;
			}

			var state = CdPlanPreviewState.Upcoming;
			string? note = null;
			if (!ActionAllowed(action.ID))
			{
				state = CdPlanPreviewState.Off;
			}
			else if (RotationRuns(action.ID))
			{
				state = CdPlanPreviewState.RotationRuns;
			}
			else if (pulled && _queuedAt.TryGetValue(i, out var queuedAt))
			{
				state = UsedSince(action.ID, queuedAt) ? CdPlanPreviewState.Done : CdPlanPreviewState.Queued;
			}
			else if (pulled && _waiting.TryGetValue(i, out var wait))
			{
				state = CdPlanPreviewState.Waiting;
				note = wait;
			}
			else if (pulled && _queued.Contains(i))
			{
				state = CdPlanPreviewState.Skipped;
			}

			var gcd = action is IBaseAction b && b.Info.IsRealGCD;
			into.Add(new CdPlanPreview(action, gcd, row.Phase, row.Start, row.End, row.Target, state, note));
		}

		return true;
	}

	public static string? PlanOption(string key) =>
		PlanAllowed && _plan.Count > 0 && _planOptions.TryGetValue(key, out var v) ? v : null;

	#endregion

	#region Running the Plan

	// Queues each plan row when its window opens, or skips it with a reason.
	private static bool UsedSince(uint actionId, DateTime since)
	{
		foreach (var rec in DataCenter.RecordActions)
		{
			if (rec.UsedTime < since)
			{
				break;
			}

			if (rec.Action.RowId == actionId)
			{
				return true;
			}
		}

		return false;
	}
	private const float PartyShieldRange = 15f;

	private static bool IsPartyShield(uint id) =>
		id is (uint)ActionID.SuccorPvE or (uint)ActionID.ConcitationPvE or (uint)ActionID.AccessionPvE
			or (uint)ActionID.EukrasianPrognosisPvE or (uint)ActionID.EukrasianPrognosisIiPvE;

	private static bool IsGroupHeal(uint id) =>
		id is (uint)ActionID.MedicaPvE or (uint)ActionID.MedicaIiPvE or (uint)ActionID.MedicaIiiPvE or (uint)ActionID.CureIiiPvE
			or (uint)ActionID.AfflatusRapturePvE
			or (uint)ActionID.HeliosPvE or (uint)ActionID.AspectedHeliosPvE or (uint)ActionID.HeliosConjunctionPvE
			or (uint)ActionID.LadyOfCrownsPvE or (uint)ActionID.CelestialOppositionPvE or (uint)ActionID.StellarDetonationPvE
			or (uint)ActionID.IndomitabilityPvE or (uint)ActionID.FeyBlessingPvE or (uint)ActionID.ConsolationPvE
			or (uint)ActionID.PrognosisPvE or (uint)ActionID.IxocholePvE or (uint)ActionID.HolosPvE or (uint)ActionID.PneumaPvE;

	private const string IfNotReadyPrefix = "IfNotReady:";

	private static string? ConditionActionName(PlanRow row) =>
		row.Target.StartsWith(IfNotReadyPrefix, StringComparison.OrdinalIgnoreCase) ? row.Target[IfNotReadyPrefix.Length..].Trim() : null;

	private const string PrecastTarget = "Precast";
	private const string ForceTarget = "Force";
	private const float PrecastLeadSeconds = 1f;

	private static string? WaitReason(uint id)
	{
		if (IsPartyShield(id) && !PartyNeedsShield())
		{
			return "party stayed shielded";
		}

		if (IsGroupHeal(id) && BeirutaAccretion.GroupHealBlocked(DataCenter.PartyMembers))
		{
			return "held for Accretion";
		}

		return DataCenter.CurrentRotation is ICdPlanRotation o ? o.PlannedActionWait(id) : null;
	}

	private static bool PartyNeedsShield() =>
		BeirutaWard.CountMissing(DataCenter.PartyMembers, PartyShieldRange,
			static m => m.HasStatus(false, StatusID.Galvanize, StatusID.EukrasianPrognosis)) >= BeirutaWard.UnshieldedPartyCount;

	private static uint RowActionId(PlanRow row)
	{
		var id = FindAction(row.Action)?.ID ?? 0;
		return id != 0 && ActionAllowed(id) ? id : 0;
	}

	private static bool ActionAllowed(uint actionId) =>
		DataCenter.CurrentRotation is not ICdPlanRotation o || o.AllowPlannedAction(actionId);

	private static bool RotationRuns(uint actionId) =>
		DataCenter.CurrentRotation is ICdPlanRotation o && o.RotationRunsPlannedAction(actionId);

	public static bool TryPlannedAction(bool gcd, out IAction? act)
	{
		act = null;
		if (_phase == null || !PlanAllowed || _queuedAt.Count == 0)
		{
			return false;
		}

		var t = PhaseTime;
		var bestEnd = float.MaxValue;
		IBaseAction? best = null;
		foreach (var (i, queuedAt) in _queuedAt)
		{
			var row = _plan[i];
			if (row.Phase != _phase || t >= row.End || row.End >= bestEnd ||
				FindAction(row.Action) is not IBaseAction action ||
				action.Info.IsRealGCD != gcd ||
				!ActionAllowed(action.ID) ||
				UsedSince(action.ID, queuedAt))
			{
				continue;
			}

			using (new ForceEnableScope())
			{
				if (!action.CanUse(out _, usedUp: true, skipAoeCheck: true, skipStatusProvideCheck: true))
				{
					continue;
				}
			}

			best = action;
			bestEnd = row.End;
		}

		act = best;
		return best != null;
	}

	private static void RunPlan(bool canAct, string reason)
	{
		if (!PlanAllowed)
		{
			return;
		}

		var t = PhaseTime;
		for (var i = 0; i < _plan.Count; i++)
		{
			var row = _plan[i];
			if (row.Phase != _phase || _queued.Contains(i) || t < row.Start)
			{
				continue;
			}

			if (row.Action.StartsWith(KeepPrefix, StringComparison.OrdinalIgnoreCase) ||
				IsFlagRow(row))
			{
				_ = _queued.Add(i);
				continue;
			}

			if (FindAction(row.Action) is { } planned && (!ActionAllowed(planned.ID) || RotationRuns(planned.ID)))
			{
				_ = _queued.Add(i);
				continue;
			}

			if (t >= row.End)
			{
				_ = _queued.Add(i);
				var waited = _waiting.Remove(i, out var waitWhy);
				var why = waited ? waitWhy! : canAct ? $"clock was already at {t:F1}s" : reason;
				Info($"{(waited ? "skipped" : "missed")} {row.Action} ({row.Phase} {row.Start:F0}-{row.End:F0}s): {why}");
				continue;
			}

			if (!canAct)
			{
				continue;
			}

			if (TrySwitchToSecond(row))
			{
				continue;
			}

			if (ConditionActionName(row) is { } conditionName && FindAction(conditionName) is IBaseAction condition && condition.Cooldown.HasOneCharge)
			{
				_ = _queued.Add(i);
				Info($"skipped {row.Action}: {condition.Name} is ready ({row.Phase} {row.Start:F0}s)");
				continue;
			}

			var precasting = row.Target.Equals(PrecastTarget, StringComparison.OrdinalIgnoreCase) && t < row.Start + PrecastLeadSeconds;
			if (!precasting && !row.Target.Equals(ForceTarget, StringComparison.OrdinalIgnoreCase) && FindAction(row.Action) is { } gated &&
				WaitReason(gated.ID) is { } wait)
			{
				_waiting[i] = wait;
				continue;
			}

			_ = _waiting.Remove(i);

			_ = _queued.Add(i);

			var action = FindAction(row.Action);
			if (action == null)
			{
				Print($"plan: no action named '{row.Action}' for this job");
				continue;
			}

			CommandTarget? target = null;
			var targetName = string.Empty;
			var targetKey = row.Target.Equals(ForceTarget, StringComparison.OrdinalIgnoreCase) || row.Target.Equals(PrecastTarget, StringComparison.OrdinalIgnoreCase) || ConditionActionName(row) != null
				? string.Empty
				: row.Target;
			if (!string.IsNullOrEmpty(targetKey))
			{
				if (!TryResolveTarget(targetKey, out target, out targetName))
				{
					Info($"skipped {action.Name}: no '{row.Target}' found ({row.Phase} {row.Start:F0}s)");
					continue;
				}

				targetName = $" on {targetName}";
			}

			DataCenter.AddCommandAction(action, row.End - t, target);
			_queuedAt[i] = DateTime.Now;
			Info($"queued {action.Name}{targetName} ({row.Phase} {row.Start:F0}-{row.End:F0}s, now {t:F1}s)");
		}
	}

	private static bool TryResolveTarget(string key, out CommandTarget? target, out string name)
	{
		target = null;
		name = key;

		if (key.Equals("LowHP", StringComparison.OrdinalIgnoreCase))
		{
			target = CommandTarget.FromTargetingType(TargetingType.LowHPPercent);
			name = "lowest HP";
			return true;
		}

		IBattleChara? found = null;
		if (key.Equals("Self", StringComparison.OrdinalIgnoreCase))
		{
			found = ECommons.GameHelpers.Player.Object;
		}
		else if (key.Equals("TankWithAggro", StringComparison.OrdinalIgnoreCase))
		{
			found = AggroTank();
		}
		else if (key.Equals("OtherTank", StringComparison.OrdinalIgnoreCase))
		{
			var aggro = AggroTank();
			foreach (var p in Svc.Party)
			{
				if (p.GameObject is IBattleChara b && !b.IsDead && b.IsJobCategory(JobRole.Tank) && b.GameObjectId != aggro?.GameObjectId)
				{
					found = b;
					break;
				}
			}
		}

		if (found == null)
		{
			return false;
		}

		target = CommandTarget.FromObject(found.GameObjectId);
		name = found.Name.TextValue;
		return true;
	}

	private static IBattleChara? AggroTank()
	{
		IBattleChara? fallback = null;
		foreach (var obj in Svc.Objects)
		{
			if (obj is not IBattleNpc npc || npc.IsDead || npc.TargetObject is not IBattleChara t || t.IsDead || !t.IsJobCategory(JobRole.Tank) || !IsPartyMember(t))
			{
				continue;
			}

			if (_seenOids.Contains(npc.BaseId))
			{
				return t;
			}

			fallback ??= t;
		}

		return fallback;
	}

	private static bool IsPartyMember(IBattleChara chara)
	{
		foreach (var p in Svc.Party)
		{
			if (p.EntityId == chara.EntityId)
			{
				return true;
			}
		}

		return false;
	}

	internal static IAction? FindAction(string name)
	{
		foreach (var list in new[] { RotationUpdater.CurrentRotationActions, RotationUpdater.CurrentDutyRotationActions })
		{
			foreach (var action in list)
			{
				if (action is IBaseAction baseAction && baseAction.Info.IsPvP != DataCenter.IsPvP)
				{
					continue;
				}

				if (name.Equals(action.Name, StringComparison.OrdinalIgnoreCase) ||
					(uint.TryParse(name, out var id) && action.ID == id))
				{
					return action;
				}
			}
		}

		return null;
	}

	#endregion

	#region Loading Files

	// Finds the duty's timeline.csv and the job's plan file, and reads them.
	private static void EnsureLoaded()
	{
		var duty = Normalize(DataCenter.Territory?.ContentFinderName);
		var job = ECommons.GameHelpers.Player.Available ? ECommons.GameHelpers.Player.Job.ToString() : string.Empty;
		var baseKey = $"{duty}|{job}|";
		var pulling = FightActive() && _phase == null && _candidateCount > 1;
		if ((_loadedFor ?? string.Empty).StartsWith(baseKey, StringComparison.Ordinal) && !pulling)
		{
			if (FightActive() || DateTime.Now < _nextFileCheck)
			{
				return;
			}

			_nextFileCheck = DateTime.Now.AddSeconds(2);
		}

		if (!duty.Equals(_autoDuty, StringComparison.OrdinalIgnoreCase))
		{
			_autoDuty = duty;
			_autoSecond = false;
		}

		var kind = CurrentProfileKind();
		var dutyDir = ChooseTimelineFolder(duty);
		var key = baseKey + dutyDir + "|" + kind?.Action;
		var syncPath = string.IsNullOrEmpty(dutyDir) ? string.Empty : Path.Combine(dutyDir, "timeline.csv");
		var planPath = FindPlanFile(dutyDir, job);
		var firstPath = kind == null ? string.Empty : FindProfile(dutyDir, kind.First, kind.Action);
		var secondPath = kind == null ? string.Empty : FindProfile(dutyDir, kind.Second, kind.Action);
		var stamp = FileStamp(syncPath, planPath) ^ (FileStamp(firstPath, secondPath) * 7);
		if (key == _loadedFor && planPath == _planPath && stamp == _fileStamp)
		{
			return;
		}

		_planPath = planPath;

		_loadedFor = key;
		LoadedFolder = dutyDir;
		_fileStamp = stamp;
		_sync = [];
		_downtime = [];
		_damage = [];
		_plan = [];
		_allPlan = [];
		_planGroup = [];
		_profileSlots.Clear();
		_profileKind = kind;
		_activeSlot = null;
		Reset();
		if (string.IsNullOrEmpty(duty))
		{
			return;
		}

		if (!File.Exists(syncPath))
		{
			return;
		}

		foreach (var cols in ReadCsv(syncPath))
		{
			if (cols.Length < 8)
			{
				continue;
			}

			var ids = new HashSet<uint>();
			foreach (var id in cols[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (uint.TryParse(id, out var v))
				{
					_ = ids.Add(v);
				}
			}

			if (cols[2].Equals("Damage", StringComparison.OrdinalIgnoreCase))
			{
				if (Enum.TryParse<TimelineDamageKind>(cols[5], true, out var damageKind))
				{
					_damage.Add(new DamageRow(cols[0], cols[1], damageKind, ParseFloat(cols[7])));
				}

				continue;
			}

			if (cols[2].Equals("Downtime", StringComparison.OrdinalIgnoreCase))
			{
				var jobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var j in cols[5].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					_ = jobs.Add(j);
				}

				_downtime.Add(new DowntimeRow(cols[0], ParseUInt(cols[4]), jobs, ParseFloat(cols[7])));
				continue;
			}


			_sync.Add(new SyncRow(cols[0], cols[1], cols[2], ParseUInt(cols[4]), ids, ParseFloat(cols[7])));
		}

		AddPlanRows(planPath, null);
		if (kind != null)
		{
			AddPlanRows(firstPath, kind.First);
			AddPlanRows(secondPath, kind.Second);
		}

		ApplyProfile(force: true);

		_planOptions.Clear();
		if (File.Exists(planPath))
		{
			var lineNo = 0;
			foreach (var line in File.ReadLines(planPath))
			{
				if (!line.StartsWith('#'))
				{
					break;
				}

				var colon = line.IndexOf(':');
				if (lineNo++ > 0 && colon > 1)
				{
					_planOptions[line[1..colon].Trim()] = line[(colon + 1)..].Trim();
				}
			}
		}

		var planName = File.Exists(planPath) ? PlanName(planPath) : null;
		var profiles = string.Empty;
		if (kind != null && _profileSlots.Count > 0)
		{
			var names = (_profileSlots.Contains(kind.First), _profileSlots.Contains(kind.Second)) switch
			{
				(true, true) => $"{kind.FirstName} / {kind.SecondName}",
				(true, false) => kind.FirstName,
				_ => kind.SecondName,
			};
			profiles = $" + {names} {kind.Action} profile";
		}
		Print(planName != null ? $"{planName} loaded ({_plan.Count} rows){profiles}" : $"{Path.GetFileName(dutyDir)} timeline loaded, no plan for this job{profiles}");
	}

	// Job plan rows (slot == null; its profile action rows, e.g. Feint, are kept apart) or a profile's rows of that action.
	private static void AddPlanRows(string path, string? slot)
	{
		if (!File.Exists(path))
		{
			return;
		}

		foreach (var cols in ReadCsv(path))
		{
			if (cols.Length < 4)
			{
				continue;
			}

			var mit = _profileKind != null && cols[3].Equals(_profileKind.Action, StringComparison.OrdinalIgnoreCase);
			if (slot != null && !mit)
			{
				continue;
			}

			_allPlan.Add(new PlanRow(cols[0], ParseFloat(cols[1]), ParseFloat(cols[2]), cols[3], cols.Length >= 5 ? cols[4] : string.Empty));
			_planGroup.Add(slot ?? (mit ? JobMitGroup : JobGroup));
			if (slot != null)
			{
				_ = _profileSlots.Add(slot);
			}
		}
	}

	#endregion

	#region Mitigation Profiles

	// Shared DPS mitigation profiles: <slot>.<action>.plan.csv next to the timeline, e.g. M1.Feint.plan.csv. When one is in
	// use, the job plan's own rows of that action are not. Auto starts on the first slot and moves to the second for the
	// duty once a first-slot window turns out to be covered by someone else's (e.g. the other melee's Feint).
	private sealed record MitProfileKind(string Action, string First, string Second, string FirstName, string SecondName,
		StatusID Status, string Who);

	private static readonly MitProfileKind[] ProfileKinds =
	[
		new("Feint", "M1", "M2", "M1", "M2", StatusID.Feint, "melee"),
		new("Addle", "R2", "FakeMelee", "R2", "Fake melee", StatusID.Addle, "caster"),
	];

	private static MitProfileKind? CurrentProfileKind()
	{
		if (DataCenter.CurrentRotation is not ICdPlanRotation o || string.IsNullOrEmpty(o.MitProfileAction))
		{
			return null;
		}

		foreach (var kind in ProfileKinds)
		{
			if (kind.Action.Equals(o.MitProfileAction, StringComparison.OrdinalIgnoreCase))
			{
				return kind;
			}
		}

		return null;
	}

	private static string? WantedSlot
	{
		get
		{
			if (_profileKind is not { } kind || DataCenter.CurrentRotation is not ICdPlanRotation o)
			{
				return null;
			}

			var slot = o.MitProfile switch
			{
				MitProfileChoice.First => kind.First,
				MitProfileChoice.Second => kind.Second,
				MitProfileChoice.Auto => _autoSecond ? kind.Second : kind.First,
				_ => null,
			};
			return slot != null && _profileSlots.Contains(slot) ? slot : null;
		}
	}

	// The shared profiles a timeline folder has for this action, by display name (e.g. "M1", "M2" for Feint).
	internal static List<string> ProfileNames(string dutyDir, string action)
	{
		List<string> names = [];
		foreach (var kind in ProfileKinds)
		{
			if (!kind.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (PlanFiles(dutyDir, $"{kind.First}.{kind.Action}.plan.csv").Count > 0)
			{
				names.Add(kind.FirstName);
			}

			if (PlanFiles(dutyDir, $"{kind.Second}.{kind.Action}.plan.csv").Count > 0)
			{
				names.Add(kind.SecondName);
			}
		}

		return names;
	}

	// The profile in use for the loaded duty (e.g. "M2"), or null when the job plan's own rows are used.
	internal static string? ActiveProfileName =>
		_profileKind is { } kind && _activeSlot is { } slot ? (slot == kind.First ? kind.FirstName : kind.SecondName) : null;

	private static void ApplyProfile(bool force = false)
	{
		var slot = WantedSlot;
		if (!force && slot == _activeSlot && _plan.Count == _allPlan.Count)
		{
			return;
		}

		_activeSlot = slot;
		_plan = new List<PlanRow>(_allPlan.Count);
		for (var i = 0; i < _allPlan.Count; i++)
		{
			var group = _planGroup[i];
			var on = group == JobGroup || (group == JobMitGroup ? slot == null : group == slot);
			_plan.Add(on ? _allPlan[i] : OffRow);
		}
	}

	// A first-slot window opened and the target already has someone else's (e.g. Feint): Auto moves to the second slot.
	private static bool TrySwitchToSecond(PlanRow row)
	{
		if (_profileKind is not { } kind || _activeSlot != kind.First ||
			!row.Action.Equals(kind.Action, StringComparison.OrdinalIgnoreCase) ||
			DataCenter.CurrentRotation is not ICdPlanRotation { MitProfile: MitProfileChoice.Auto } ||
			!_profileSlots.Contains(kind.Second) ||
			DataCenter.HostileTarget is not { } target ||
			!target.HasStatus(false, kind.Status) || target.HasStatus(true, kind.Status) ||
			target.WillStatusEndGCD(1, 0, false, kind.Status))
		{
			return false;
		}

		_autoSecond = true;
		ApplyProfile();
		Print($"the {kind.FirstName} {kind.Action} at {row.Phase} {row.Start:F0}s was already covered by another {kind.Who}: " +
			$"using the {kind.SecondName} {kind.Action} profile for this duty");
		return true;
	}

	#endregion

	#region Timeline Folders

	private static string ChooseTimelineFolder(string duty)
	{
		_candidateCount = 0;
		var root = TimelineRoot;
		if (string.IsNullOrEmpty(duty) || !Directory.Exists(root))
		{
			return string.Empty;
		}

		var candidates = new List<string>();
		foreach (var dir in TimelineFolders())
		{
			var timeline = Path.Combine(dir, "timeline.csv");
			if (File.Exists(timeline) &&
				(Path.GetFileName(dir).Equals(duty, StringComparison.OrdinalIgnoreCase) ||
				 TimelineHeader(timeline, "Duty").Equals(duty, StringComparison.OrdinalIgnoreCase)))
			{
				candidates.Add(dir);
			}
		}

		candidates.Sort(StringComparer.OrdinalIgnoreCase);
		_candidateCount = candidates.Count;
		if (candidates.Count <= 1)
		{
			return candidates.Count == 1 ? candidates[0] : string.Empty;
		}

		foreach (var dir in candidates)
		{
			var boss = TimelineBoss(Path.Combine(dir, "timeline.csv"));
			if (boss != 0 && AnyEnemy(boss))
			{
				return dir;
			}
		}

		return candidates[0];
	}

	internal static string TimelineHeader(string path, string key)
	{
		foreach (var line in File.ReadLines(path))
		{
			if (!line.StartsWith('#'))
			{
				break;
			}

			var colon = line.IndexOf(':');
			if (colon > 1 && line[1..colon].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
			{
				return line[(colon + 1)..].Trim();
			}
		}

		return string.Empty;
	}

	private static uint TimelineBoss(string path)
	{
		foreach (var cols in ReadCsv(path))
		{
			if (cols.Length >= 5 && cols[2] == "CombatStart")
			{
				return ParseUInt(cols[4]);
			}
		}

		return 0;
	}
	private static string _planPath = string.Empty;

	private static string FindPlanFile(string dutyDir, string job)
	{
		if (string.IsNullOrEmpty(job) || !Directory.Exists(dutyDir))
		{
			return string.Empty;
		}

		var files = PlanFiles(dutyDir, $"*.{job}.plan.csv");
		if (Service.Config.CdPlanChoice.TryGetValue(PlanChoiceKey(dutyDir, job), out var chosen))
		{
			foreach (var file in files)
			{
				if (Path.GetFileName(file).Equals(chosen, StringComparison.OrdinalIgnoreCase))
				{
					return file;
				}
			}
		}

		return files.Count > 0 ? files[0] : string.Empty;
	}

	// A shared mitigation profile of a timeline folder (e.g. M1.Feint.plan.csv), or empty.
	private static string FindProfile(string dutyDir, string slot, string action)
	{
		var files = PlanFiles(dutyDir, $"{slot}.{action}.plan.csv");
		return files.Count > 0 ? files[0] : string.Empty;
	}

	// The plan file the player picked for a timeline folder and job (CD Plan section). Falls back to the first file by name.
	internal static string PlanChoiceKey(string dutyDir, string job) => $"{Path.GetFileName(dutyDir)}|{job}";

	internal static string ChosenPlanFile(string dutyDir, string job) => Path.GetFileName(FindPlanFile(dutyDir, job));

	internal static void ChoosePlan(string dutyDir, string job, string fileName)
	{
		Service.Config.CdPlanChoice[PlanChoiceKey(dutyDir, job)] = fileName;
		Service.Config.Save();
		_nextFileCheck = DateTime.MinValue;
	}

	internal static string TimelineRoot => Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Timelines");

	// The built-in timelines and plans that ship with the plugin (RotationSolver.Timeline in the repo).
	internal static string BundledRoot =>
		Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty, "RotationSolver.Timeline");

	// Every timeline folder, at any depth (e.g. Dawntrail\Savage\M10S The Xtremes): the player's own Timelines folder, then
	// the built-in ones for duties the player has no timeline of their own for.
	internal static List<string> TimelineFolders()
	{
		var own = FoldersWithTimeline(TimelineRoot);
		var ownDuties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var dir in own)
		{
			_ = ownDuties.Add(TimelineDutyKey(dir));
		}

		List<string> result = [.. own];
		foreach (var dir in FoldersWithTimeline(BundledRoot))
		{
			if (!ownDuties.Contains(TimelineDutyKey(dir)))
			{
				result.Add(dir);
			}
		}

		return result;
	}

	private static List<string> FoldersWithTimeline(string root)
	{
		List<string> result = [];
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			return result;
		}

		try
		{
			foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
			{
				if (File.Exists(Path.Combine(dir, "timeline.csv")))
				{
					result.Add(dir);
				}
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}

		return result;
	}

	private static string TimelineDutyKey(string dir)
	{
		try
		{
			var duty = TimelineHeader(Path.Combine(dir, "timeline.csv"), "Duty");
			return string.IsNullOrEmpty(duty) ? Normalize(Path.GetFileName(dir)) : duty;
		}
		catch (IOException)
		{
			return Normalize(Path.GetFileName(dir));
		}
	}

	// Where a timeline folder's plans are: for a built-in timeline, the same path under the player's Timelines folder
	// (their own plans, e.g. from Log2Timeline, without a timeline.csv) and then the built-in folder.
	internal static List<string> PlanDirs(string dutyDir)
	{
		List<string> dirs = [];
		if (string.IsNullOrEmpty(dutyDir))
		{
			return dirs;
		}

		var bundled = BundledRoot;
		if (!string.IsNullOrEmpty(bundled) && dutyDir.StartsWith(bundled + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
		{
			var own = Path.Combine(TimelineRoot, Path.GetRelativePath(bundled, dutyDir));
			if (Directory.Exists(own))
			{
				dirs.Add(own);
			}
		}

		if (Directory.Exists(dutyDir))
		{
			dirs.Add(dutyDir);
		}

		return dirs;
	}

	// The plan files of one pattern (e.g. "*.SGE.plan.csv") across a timeline's plan folders, sorted by file name;
	// the player's own file wins over a built-in one with the same name.
	internal static List<string> PlanFiles(string dutyDir, string pattern)
	{
		var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var dir in PlanDirs(dutyDir))
		{
			foreach (var file in Directory.GetFiles(dir, pattern))
			{
				_ = byName.TryAdd(Path.GetFileName(file), file);
			}
		}

		List<string> files = [.. byName.Values];
		files.Sort(static (a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
		return files;
	}

	// The timeline folder and plan file loaded for the current duty, or empty.
	internal static string LoadedFolder { get; private set; } = string.Empty;

	internal static string LoadedPlanPath => _planPath;

	// The fight is running, so the plan can't be swapped.
	internal static bool PlanLocked => FightActive() && _phase != null;

	// Plan rows that aren't actions: Keep:<action> and flag rows like Ward Mode.
	internal static bool IsPlanMetaAction(string action) =>
		action.StartsWith(KeepPrefix, StringComparison.OrdinalIgnoreCase) ||
		action.Equals(WardModeRow, StringComparison.OrdinalIgnoreCase) ||
		action.Equals(TopUpAllRow, StringComparison.OrdinalIgnoreCase) ||
		action.Equals(TankStanceRow, StringComparison.OrdinalIgnoreCase);
	private static long _fileStamp;

	private static long FileStamp(string syncPath, string planPath) =>
		(File.Exists(syncPath) ? File.GetLastWriteTimeUtc(syncPath).Ticks : 0) ^
		(File.Exists(planPath) ? File.GetLastWriteTimeUtc(planPath).Ticks * 31 : 0);

	internal static string PlanName(string path)
	{
		foreach (var line in File.ReadLines(path))
		{
			return line.StartsWith('#') ? line.TrimStart('#').Trim() : Path.GetFileNameWithoutExtension(path);
		}

		return Path.GetFileNameWithoutExtension(path);
	}

	internal static IEnumerable<string[]> ReadCsv(string path)
	{
		var first = true;
		foreach (var line in File.ReadAllLines(path))
		{
			if (line.StartsWith('#'))
			{
				continue;
			}

			if (first)
			{
				first = false;
				continue;
			}

			if (!string.IsNullOrWhiteSpace(line))
			{
				yield return line.Split(',', StringSplitOptions.TrimEntries);
			}
		}
	}

	internal static string Normalize(string? name)
	{
		if (name == null)
		{
			return string.Empty;
		}

		var sb = new System.Text.StringBuilder(name.Length);
		foreach (var ch in name)
		{
			if (char.IsLetterOrDigit(ch))
			{
				_ = sb.Append(ch);
			}
		}

		return sb.ToString();
	}

	private static uint ParseUInt(string s) =>
		s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			? uint.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out var h) ? h : 0
			: uint.TryParse(s, out var d) ? d : 0;

	private static float ParseFloat(string s) =>
		float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;

	#endregion

	#region Recommended Settings

	// Applies a plan's '# Set.' lines on top of the user's settings while the plan is loaded.
	private const string SettingPrefix = "Set.";
	private static ICustomRotation? _settingsRotation;
	private static string? _settingsFor;
	private static readonly List<(System.Reflection.PropertyInfo Property, object Value)> _settings = [];

	private static void ApplyPlanSettings()
	{
		var rotation = DataCenter.CurrentRotation;
		var wanted = rotation is ICdPlanRotation o && o.UseTimelinePlan && o.UsePlanSettings && _plan.Count > 0;
		var key = wanted ? $"{_loadedFor}|{_planPath}|{_fileStamp}" : null;

		if (_settingsRotation != null && (!ReferenceEquals(_settingsRotation, rotation) || key != _settingsFor))
		{
			RestoreSettings();
		}

		if (!wanted || rotation == null)
		{
			return;
		}

		if (_settingsRotation != null)
		{
			return;
		}

		_settingsRotation = rotation;
		_settingsFor = key;
		ResolveSettings(rotation);
		foreach (var (property, value) in _settings)
		{
			property.SetValue(rotation, value);
		}
	}

	private static void ResolveSettings(ICustomRotation rotation)
	{
		_settings.Clear();
		foreach (var (name, raw) in _planOptions)
		{
			if (!name.StartsWith(SettingPrefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var settingName = name[SettingPrefix.Length..].Trim();
			var config = FindConfig(rotation, settingName);
			if (IsPlanControl(config?.Parent) || IsPlanControl(settingName) ||
				settingName.Equals("MoreSettings", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var property = config != null ? rotation.GetType().GetProperty(settingName) : null;
			if (property == null || !property.CanWrite || !TryConvert(raw, property.PropertyType, out var value))
			{
				Print($"plan setting '{settingName}: {raw}' isn't a {rotation.Name} setting value, ignored");
				continue;
			}

			_settings.Add((property, value));
		}

		if (_settings.Count > 0)
		{
			var list = new List<string>(_settings.Count);
			foreach (var (property, value) in _settings)
			{
				list.Add($"{property.Name} = {value}");
			}

			Print($"using {_settings.Count} recommended settings for this plan (the settings page still shows your own): {string.Join(", ", list)}");
		}
	}

	private static bool IsPlanControl(string? name) =>
		name != null &&
		(name.Equals(nameof(ICdPlanRotation.UseTimelinePlan), StringComparison.OrdinalIgnoreCase) ||
		 name.Equals("CdPlanFrom", StringComparison.OrdinalIgnoreCase));

	private static void RestoreSettings()
	{
		var rotation = _settingsRotation;
		if (rotation != null)
		{
			foreach (var (property, _) in _settings)
			{
				if (FindConfig(rotation, property.Name) is { } config && TryConvert(config.Value, property.PropertyType, out var mine))
				{
					property.SetValue(rotation, mine);
				}
			}
		}

		_settings.Clear();
		_settingsRotation = null;
		_settingsFor = null;
	}

	private static Configuration.RotationConfig.IRotationConfig? FindConfig(ICustomRotation rotation, string name)
	{
		foreach (var config in rotation.Configs)
		{
			if (config.Name.Equals(name, StringComparison.Ordinal))
			{
				return config;
			}
		}

		return null;
	}

	private static bool TryConvert(string raw, Type type, out object value)
	{
		value = null!;
		raw = raw.Trim();
		if (type.IsEnum)
		{
			foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
			{
				var description = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
				if (field.Name.Equals(raw, StringComparison.OrdinalIgnoreCase) ||
					(description != null && description.Description.Equals(raw, StringComparison.OrdinalIgnoreCase)))
				{
					value = field.GetValue(null)!;
					return true;
				}
			}

			return false;
		}

		if (type == typeof(bool))
		{
			if (raw.Equals("on", StringComparison.OrdinalIgnoreCase) || raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
			{
				raw = "true";
			}
			else if (raw.Equals("off", StringComparison.OrdinalIgnoreCase) || raw.Equals("no", StringComparison.OrdinalIgnoreCase))
			{
				raw = "false";
			}

			if (bool.TryParse(raw, out var b))
			{
				value = b;
				return true;
			}

			return false;
		}

		var percent = raw.EndsWith('%');
		var number = percent ? raw[..^1] : raw;
		if (!double.TryParse(number, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
		{
			return false;
		}

		if (percent)
		{
			d /= 100d;
		}

		try
		{
			value = Convert.ChangeType(d, type, System.Globalization.CultureInfo.InvariantCulture);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	#endregion

	#region Chat Messages

	// Timeline messages in chat and the log. Only rotations that opt into the timeline get chat output; everything else is log only.
	// Info: sync/queue chatter, shown only with ShowTimelineMessages on.
	private static void Info(string msg)
	{
		if (DataCenter.CurrentRotation is ICdPlanRotation { ShowTimelineMessages: true })
		{
			Print(msg);
		}
		else
		{
			PluginLog.Debug($"[Timeline] {msg}");
		}
	}

	// Print: load notices and plan problems, shown when the rotation uses the plan or wants timeline messages.
	private static void Print(string msg)
	{
		PluginLog.Debug($"[Timeline] {msg}");
		if (DataCenter.CurrentRotation is ICdPlanRotation o && (o.UseTimelinePlan || o.ShowTimelineMessages))
		{
			Svc.Chat.Print($"[Timeline] {msg}");
		}
	

	#endregion
}
}
