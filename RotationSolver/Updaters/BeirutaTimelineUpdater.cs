// Runs a duty's timeline.csv and the job's plan file: keeps the fight clock in sync, answers "what is coming" for the rotations, and queues planned actions.
using ECommons.DalamudServices;
using ECommons.Logging;

namespace RotationSolver.Updaters;

// Settings a rotation exposes to the timeline runner.
internal interface IBeirutaTimelineOption
{
	bool UseTimelinePlan { get; }

	bool AllowPlannedAction(uint actionId) => true;

	bool ShowTimelineMessages => true;

	string? PlannedActionWait(uint actionId) => null;

	bool UsePlanSettings => false;

	bool RotationRunsPlannedAction(uint actionId) => false;
}

// How a timeline Damage row hits: a light or heavy raidwide, or a tankbuster.
internal enum BeirutaDamageKind : byte
{
	Light,
	Heavy,
	Tankbuster,
}

internal static class BeirutaTimelineUpdater
{
	#region State

	// Loaded rows and the live phase clock.
	// Plans only run for rotations that opt in (the Beiruta healers for now).
	private static bool PlanAllowed => DataCenter.CurrentRotation is IBeirutaTimelineOption { UseTimelinePlan: true };

	private sealed record SyncRow(string Phase, string Name, string Watch, uint Oid, HashSet<uint> ActionIds, float PhaseTime);
	private sealed record PlanRow(string Phase, float Start, float End, string Action, string Target);

	private static string? _loadedFor;
	private static List<SyncRow> _sync = [];
	private sealed record DowntimeRow(string Phase, uint Oid, HashSet<string> Jobs, float PhaseTime);
	private static List<DowntimeRow> _downtime = [];
	private sealed record DamageRow(string Phase, string Name, BeirutaDamageKind Kind, float PhaseTime);
	private static List<DamageRow> _damage = [];
	private static List<PlanRow> _plan = [];

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
			PluginLog.Error($"[BeirutaTimeline] {ex}");
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
				if (row.Kind == BeirutaDamageKind.Tankbuster)
				{
					return true;
				}
			}

			return false;
		}
	}

	public static bool NextDamage(float within, bool tankbuster, out float inSeconds, out BeirutaDamageKind kind)
	{
		inSeconds = float.MaxValue;
		kind = BeirutaDamageKind.Light;
		if (_phase == null || _damage.Count == 0)
		{
			return false;
		}

		var t = PhaseTime;
		foreach (var row in _damage)
		{
			if (row.Phase != _phase || row.PhaseTime < t || row.PhaseTime > t + within ||
				(row.Kind == BeirutaDamageKind.Tankbuster) != tankbuster)
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

	private static bool IsFlagRow(PlanRow row) =>
		row.Action.Equals(WardModeRow, StringComparison.OrdinalIgnoreCase) ||
		row.Action.Equals(TopUpAllRow, StringComparison.OrdinalIgnoreCase);

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

		return DataCenter.CurrentRotation is IBeirutaTimelineOption o ? o.PlannedActionWait(id) : null;
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
		DataCenter.CurrentRotation is not IBeirutaTimelineOption o || o.AllowPlannedAction(actionId);

	private static bool RotationRuns(uint actionId) =>
		DataCenter.CurrentRotation is IBeirutaTimelineOption o && o.RotationRunsPlannedAction(actionId);

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

	private static IAction? FindAction(string name)
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

		var dutyDir = ChooseTimelineFolder(duty);
		var key = baseKey + dutyDir;
		var syncPath = string.IsNullOrEmpty(dutyDir) ? string.Empty : Path.Combine(dutyDir, "timeline.csv");
		var planPath = FindPlanFile(dutyDir, job);
		if (key == _loadedFor && planPath == _planPath && FileStamp(syncPath, planPath) == _fileStamp)
		{
			return;
		}

		_planPath = planPath;

		_loadedFor = key;
		_fileStamp = FileStamp(syncPath, planPath);
		_sync = [];
		_downtime = [];
		_damage = [];
		_plan = [];
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
				if (Enum.TryParse<BeirutaDamageKind>(cols[5], true, out var kind))
				{
					_damage.Add(new DamageRow(cols[0], cols[1], kind, ParseFloat(cols[7])));
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

		if (File.Exists(planPath))
		{
			foreach (var cols in ReadCsv(planPath))
			{
				if (cols.Length >= 4)
				{
					_plan.Add(new PlanRow(cols[0], ParseFloat(cols[1]), ParseFloat(cols[2]), cols[3], cols.Length >= 5 ? cols[4] : string.Empty));
				}
			}
		}

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
		Print(planName != null ? $"{planName} loaded ({_plan.Count} rows)" : $"{Path.GetFileName(dutyDir)} timeline loaded, no plan for this job");
	}

	private static string ChooseTimelineFolder(string duty)
	{
		_candidateCount = 0;
		var root = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Timelines");
		if (string.IsNullOrEmpty(duty) || !Directory.Exists(root))
		{
			return string.Empty;
		}

		var candidates = new List<string>();
		foreach (var dir in Directory.GetDirectories(root))
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

	private static string TimelineHeader(string path, string key)
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

		var files = Directory.GetFiles(dutyDir, $"*.{job}.plan.csv");
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		return files.Length > 0 ? files[0] : string.Empty;
	}
	private static long _fileStamp;

	private static long FileStamp(string syncPath, string planPath) =>
		(File.Exists(syncPath) ? File.GetLastWriteTimeUtc(syncPath).Ticks : 0) ^
		(File.Exists(planPath) ? File.GetLastWriteTimeUtc(planPath).Ticks * 31 : 0);

	private static string PlanName(string path)
	{
		foreach (var line in File.ReadLines(path))
		{
			return line.StartsWith('#') ? line.TrimStart('#').Trim() : Path.GetFileNameWithoutExtension(path);
		}

		return Path.GetFileNameWithoutExtension(path);
	}

	private static IEnumerable<string[]> ReadCsv(string path)
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

	private static string Normalize(string? name)
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
		var wanted = rotation is IBeirutaTimelineOption o && o.UseTimelinePlan && o.UsePlanSettings && _plan.Count > 0;
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
		(name.Equals(nameof(IBeirutaTimelineOption.UseTimelinePlan), StringComparison.OrdinalIgnoreCase) ||
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
		if (DataCenter.CurrentRotation is IBeirutaTimelineOption { ShowTimelineMessages: true })
		{
			Print(msg);
		}
		else
		{
			PluginLog.Debug($"[BeirutaTimeline] {msg}");
		}
	}

	// Print: load notices and plan problems, shown when the rotation uses the plan or wants timeline messages.
	private static void Print(string msg)
	{
		PluginLog.Debug($"[BeirutaTimeline] {msg}");
		if (DataCenter.CurrentRotation is IBeirutaTimelineOption o && (o.UseTimelinePlan || o.ShowTimelineMessages))
		{
			Svc.Chat.Print($"[Timeline] {msg}");
		}
	

	#endregion
}
}