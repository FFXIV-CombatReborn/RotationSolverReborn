// Shared building blocks behind the healer rules: the HP and heal-over-time model, recent-cast checks, Ward Mode, raidwide prep, DMU Accretion, single-target casting and pending heals.
namespace RotationSolver.Engine.Helpers;

#region Heal Model

// HP after pending heals and heal-over-time ticks, and the HP threshold where a heal of a given potency is fully used.
internal static class BeirutaHealModel
{
	public const float HpPerPotency = 84f;
	public const float AlwaysInjuredHp = 0.5f;
	public const float HotCreditTicks = 3f;
	public const float WhisperingDawnTickPotency = 72f;
	public const float PhysisIiBoostedTickPotency = 143f;
	public const float KeracholeTickPotency = 100f;

	private const float ReferenceMaxHp = 226813f;
	private const float HotTickSeconds = 3f;
	private const float EudaimoniaPotency = 180f;

	private static readonly (StatusID[] Status, float TickPotency)[] PartyHots =
	[
		([StatusID.PhysisIi], PhysisIiBoostedTickPotency),
		([StatusID.Physis], 100f),
		([StatusID.Kerakeia], KeracholeTickPotency),
		([StatusID.WhisperingDawn, StatusID.AngelsWhisper], WhisperingDawnTickPotency),
		([StatusID.SacredSoil_1944], 100f),
		([StatusID.MedicaIii, StatusID.MedicaIii_3986], 175f),
		([StatusID.MedicaIi], 150f),
		([StatusID.Asylum, StatusID.Asylum_1911], 100f),
		([StatusID.Regen, StatusID.Regen_897, StatusID.Regen_1330], 250f),
		([StatusID.AspectedBenefic], 250f),
		([StatusID.HeliosConjunction], 175f),
		([StatusID.AspectedHelios], 150f),
		([StatusID.Opposition], 100f),
		([StatusID.WheelOfFortune], 100f),
	];

	public static float EffectiveHpRatio(IBattleChara member, bool wardMode, float landingPotency = 0f)
	{
		if (member.MaxHp == 0)
		{
			return 1f;
		}

		var ratio = ((float)member.CurrentHp + PendingHeals.PendingHp(member)) / member.MaxHp;
		if (!wardMode)
		{
			ratio += PotencyToRatio(HotCreditPotency(member) + landingPotency);
		}

		return MathF.Min(1f, ratio);
	}

	public static float FullUseHp(float potency, float healBoost = 1f) =>
		1f - PotencyToRatio(potency * healBoost);

	public static float RoundToFive(float ratio) => MathF.Round(ratio * 20f) / 20f;

	public static float Threshold(
		float potency,
		float healBoost,
		bool wardMode,
		bool useCustom,
		float customWardOff,
		IEnumerable<IBattleChara>? party,
		float wardOffCap = 1f)
	{
		var fullUse = FullUseHp(potency, healBoost);
		if (wardMode)
		{
			return RoundToFive(fullUse);
		}

		return useCustom
			? customWardOff
			: MathF.Min(wardOffCap, RoundToFive(fullUse - PotencyToRatio(PartnerGcdPotency(party))));
	}

	private static float HotCreditPotency(IBattleChara member)
	{
		var potency = 0f;

		foreach (var (status, tickPotency) in PartyHots)
		{
			var remain = member.StatusTime(false, status);
			if (remain > 0f)
			{
				potency += MathF.Min(HotCreditTicks, MathF.Floor(remain / HotTickSeconds)) * tickPotency;
			}
		}

		if (member.HasStatus(false, StatusID.Eudaimonia))
		{
			potency += EudaimoniaPotency * HotCreditTicks;
		}

		return potency;
	}

	private static float PotencyToRatio(float potency) => potency * HpPerPotency / ReferenceMaxHp;

	private static float PartnerGcdPotency(IEnumerable<IBattleChara>? party)
	{
		if (party == null)
		{
			return 0f;
		}

		var selfId = ECommons.GameHelpers.Player.Object?.GameObjectId;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.GameObjectId == selfId ||
				!member.IsJobCategory(JobRole.Healer))
			{
				continue;
			}

			if (member.IsJobs(ECommons.ExcelServices.Job.WHM, ECommons.ExcelServices.Job.AST))
			{
				return 250f;
			}

			return member.IsJobs(ECommons.ExcelServices.Job.SCH) ? 200f : 100f;
		}

		return 0f;
	}
}

#endregion

#region Heal Window

// Whether a heal was just used or is being cast, so two heals don't go out back to back.
internal static class BeirutaHealWindow
{
	public const float OgcdSeconds = 5f;
	public const double GcdSeconds = 3d;

	public static bool OgcdJustUsed(params ReadOnlySpan<IBaseAction> actions)
	{
		foreach (var action in actions)
		{
			if (!action.EnoughLevel || !action.Cooldown.IsCoolingDown)
			{
				continue;
			}

			var oneCharge = action.Cooldown.RecastTimeOneChargeRaw;
			var elapsed = action.Cooldown.RecastTimeElapsedRaw;
			if (oneCharge > 0f)
			{
				elapsed %= oneCharge;
			}

			if (elapsed < OgcdSeconds)
			{
				return true;
			}
		}

		return false;
	}

	public static bool GcdJustUsed(params ReadOnlySpan<ActionID> ids) =>
		IsCasting(ids) || UsedWithin(GcdSeconds, ids);

	public static bool UsedSince(DateTime since, params ReadOnlySpan<ActionID> ids) =>
		IsCasting(ids) || DataCenter.AnyRecordedActionSince(since, ids);

	private static bool IsCasting(ReadOnlySpan<ActionID> ids) =>
		ECommons.GameHelpers.Player.Object is { IsCasting: true } player && Contains(ids, player.CastActionId);

	private static bool UsedWithin(double seconds, ReadOnlySpan<ActionID> ids) =>
		DataCenter.AnyRecordedActionWithin(seconds, ids);

	private static bool Contains(ReadOnlySpan<ActionID> ids, uint id)
	{
		foreach (var item in ids)
		{
			if ((uint)item == id)
			{
				return true;
			}
		}

		return false;
	}
}

#endregion

#region Ward Mode

// The Ward Mode toggle, its on/off toast, and counts of players missing a shield.
internal static class BeirutaWard
{
	public const int UnshieldedPartyCount = 4;

	private const string WardModeConfigName = "WardMode";

	public static void Track(ICustomRotation rotation, bool wardMode, ref bool lastWardMode)
	{
		if (wardMode && ECommons.GameHelpers.Player.Object is { IsDead: true })
		{
			SetWardMode(rotation, false);
			wardMode = false;
		}

		if (wardMode == lastWardMode)
		{
			return;
		}

		lastWardMode = wardMode;
		ECommons.DalamudServices.Svc.Toasts.ShowQuest($"Ward Mode {(wardMode ? "ON" : "OFF")}");
	}

	public static void SetWardMode(ICustomRotation rotation, bool value)
	{
		foreach (var config in rotation.Configs.Configs)
		{
			if (config.Name == WardModeConfigName)
			{
				config.Value = value.ToString();
				return;
			}
		}
	}

	public static int CountMissing(IEnumerable<IBattleChara>? party, float range, Func<IBattleChara, bool> hasEffect, bool nonTanksOnly = false)
	{
		if (party == null)
		{
			return 0;
		}

		var count = 0;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				(nonTanksOnly && member.IsJobCategory(JobRole.Tank)) ||
				member.DistanceToPlayer() > range ||
				hasEffect(member))
			{
				continue;
			}

			count++;
		}

		return count;
	}

	public static int CountNonTanksMissing(IEnumerable<IBattleChara>? party, float range, Func<IBattleChara, bool> hasEffect) =>
		CountMissing(party, range, hasEffect, true);

	public static IBattleChara? StanceTank(IEnumerable<IBattleChara>? party, float range)
	{
		if (party == null)
		{
			return null;
		}

		IBattleChara? fallback = null;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				!member.IsJobCategory(JobRole.Tank) ||
				member.DistanceToPlayer() > range)
			{
				continue;
			}

			if (member.HasStatus(false, StatusHelper.TankStanceStatus))
			{
				return member;
			}

			fallback ??= member;
		}

		return fallback;
	}
}

#endregion

#region Raidwide Tracking

// Treats a raidwide as coming when the timeline says so, or for 10s after most of the party gets prep shields or mitigation.
internal sealed class BeirutaRaidwide
{
	private const float Range = 30f;
	private const long WindowMs = 10_000;

	private static readonly StatusID[] PrepStatus =
	[
		StatusID.EukrasianPrognosis,
		StatusID.Kerachole,
		StatusID.Holos,
		StatusID.Holosakos,
		StatusID.Panhaima,
		StatusID.Panhaimatinon,
		StatusID.Galvanize,
		StatusID.SeraphicVeil,
		StatusID.SacredSoil,
		StatusID.SacredSoil_299,
		StatusID.FeyIllumination,
		StatusID.CollectiveUnconscious_849,
		StatusID.NeutralSect_1921,
		StatusID.SunSign,
	];

	private long _sinceMs;
	private bool _wasActive;

	private const float TimelineLeadSeconds = 10f;

	public bool Coming =>
		BeirutaIncoming.RaidwideWithin(TimelineLeadSeconds) ||
		(_sinceMs != 0 && Environment.TickCount64 - _sinceMs <= WindowMs);

	public void Track(bool inCombat, IEnumerable<IBattleChara>? party)
	{
		var active = inCombat && PrepStatusesUp(party);
		if (active && !_wasActive)
		{
			_sinceMs = Environment.TickCount64;
		}
		else if (!active)
		{
			_sinceMs = 0;
		}

		_wasActive = active;
	}

	private static bool PrepStatusesUp(IEnumerable<IBattleChara>? party)
	{
		if (party == null)
		{
			return false;
		}

		var nonTanks = 0;
		var prepared = 0;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.IsJobCategory(JobRole.Tank) ||
				member.DistanceToPlayer() > Range)
			{
				continue;
			}

			nonTanks++;
			if (member.HasStatus(false, PrepStatus))
			{
				prepared++;
			}
		}

		return nonTanks > 0 && prepared >= Math.Max(2, (nonTanks + 1) / 2);
	}
}

#endregion

#region DMU Accretion

// DMU P3 Accretion: who to fill next, and keeping group heals from filling players out of order.
internal static class BeirutaAccretion
{
	private const float HealRange = 30f;
	private const float TopUpSafeHp = 0.3f;
	private const int TopUpSafeCount = 3;

	private static bool PartySafeForTopUp(IEnumerable<IBattleChara> party, IBattleChara target)
	{
		var count = 0;
		foreach (var member in party)
		{
			if (member != null &&
				member.GameObjectId != target.GameObjectId &&
				!member.IsDead &&
				!member.IsJobCategory(JobRole.Tank) &&
				BeirutaParty.HpWithShield(member) > TopUpSafeHp)
			{
				count++;
			}
		}

		return count >= TopUpSafeCount;
	}

	public static IBattleChara? FindTarget(IEnumerable<IBattleChara>? party, bool requireSafeParty = true)
	{
		if (party == null || !DataCenter.IsInDMU || ReadState(party).Settling)
		{
			return null;
		}

		var target = FindLowest(party, static m => m.IsJobCategory(JobRole.Healer));
		if (target == null)
		{
			if (AnyMember(party, static m => m.IsJobCategory(JobRole.Healer) && m.HasStatus(false, StatusID.Accretion)) ||
				AnyMember(party, static m => m.HasStatus(false, StatusID.EarthResistanceDownIi_3372)))
			{
				return null;
			}

			target = FindLowest(party, static m => !m.IsJobCategory(JobRole.Healer) && !m.IsJobCategory(JobRole.Tank));
		}

		return target != null && (!requireSafeParty || PartySafeForTopUp(party, target)) ? target : null;
	}

	private static IBattleChara? FindLowest(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> role) =>
		LowestMatching(party, m =>
			!m.IsDead &&
			role(m) &&
			m.HasStatus(false, StatusID.Accretion) &&
			!m.HasStatus(false, StatusID.EarthResistanceDownIi_3372) &&
			BeirutaParty.HealHp(m) < 1f &&
			m.DistanceToPlayer() <= HealRange);

	private static bool HoldsAccretion(IBattleChara m) =>
		!m.IsDead && !m.IsJobCategory(JobRole.Tank) && m.HasStatus(false, StatusID.Accretion);

	private static bool HasAccretionToFill(IBattleChara m) =>
		!m.IsDead &&
		!m.IsJobCategory(JobRole.Tank) &&
		m.HasStatus(false, StatusID.Accretion) &&
		!m.HasStatus(false, StatusID.EarthResistanceDownIi_3372);

	public static bool AnyHolder(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			return false;
		}

		foreach (var member in party)
		{
			if (member != null && !member.IsDead && member.HasStatus(false, StatusID.Accretion))
			{
				return true;
			}
		}

		return false;
	}

	public static int ToFillCount(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			return 0;
		}

		var count = 0;
		foreach (var member in party)
		{
			if (member != null && HasAccretionToFill(member))
			{
				count++;
			}
		}

		return count;
	}

	public static bool GuardActive(IEnumerable<IBattleChara>? party) =>
		party != null &&
		DataCenter.IsInDMU &&
		AnyMember(party, HasAccretionToFill);

	private const long AccretionSettleMs = 3_000;
	private static int _lastHolders;
	private static long _holdersChangedAtMs;

	private static (int Holders, bool EarthDown, bool Settling) ReadState(IEnumerable<IBattleChara> party)
	{
		var holders = 0;
		var earthDown = false;
		foreach (var member in party)
		{
			if (member == null || member.IsDead)
			{
				continue;
			}

			if (member.HasStatus(false, StatusID.Accretion))
			{
				holders++;
			}

			if (member.HasStatus(false, StatusID.EarthResistanceDownIi_3372))
			{
				earthDown = true;
			}
		}

		var now = Environment.TickCount64;
		if (holders != _lastHolders)
		{
			_holdersChangedAtMs = now;
		}

		_lastHolders = holders;

		if (earthDown)
		{
			_holdersChangedAtMs = 0;
		}

		return (holders, earthDown, _holdersChangedAtMs != 0 && now - _holdersChangedAtMs < AccretionSettleMs);
	}

	public static bool MicrocosmosSafe(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			_lastHolders = 0;
			_holdersChangedAtMs = 0;
			return true;
		}

		var state = ReadState(party);
		return !state.Settling && (state.Holders == 0 || (state.Holders == 1 && !state.EarthDown));
	}

	public const float GroupHealSafeHp = 0.3f;

	public static bool GroupHealBlocked(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			return false;
		}

		var holders = 0;
		IBattleChara? blocking = null;
		foreach (var member in party)
		{
			if (member == null || !HoldsAccretion(member))
			{
				continue;
			}

			holders++;
			if (BeirutaParty.HealHp(member) > GroupHealSafeHp)
			{
				blocking = member;
			}
		}

		if (blocking == null)
		{
			return false;
		}

		return holders > 1 || NextToFill(party)?.GameObjectId != blocking.GameObjectId;
	}

	public static IBattleChara? NextToFill(IEnumerable<IBattleChara> party)
	{
		var healer = LowestMatching(party, static m => HasAccretionToFill(m) && m.IsJobCategory(JobRole.Healer));
		if (healer != null)
		{
			return healer;
		}

		if (AnyMember(party, static m => m.HasStatus(false, StatusID.EarthResistanceDownIi_3372)))
		{
			return null;
		}

		return LowestMatching(party, static m => HasAccretionToFill(m) && !m.IsJobCategory(JobRole.Healer));
	}

	public static bool WouldFillOutOfOrder(IEnumerable<IBattleChara>? party, IBattleChara? member, float healHp)
	{
		if (party == null || member == null || !AnyHolder(party) || !HoldsAccretion(member))
		{
			return false;
		}

		var next = NextToFill(party);
		if (next != null && next.GameObjectId == member.GameObjectId)
		{
			return false;
		}

		return (float)member.MaxHp - member.CurrentHp - PendingHeals.PendingHp(member) <= healHp;
	}

	public static bool AreaSafe(IEnumerable<IBattleChara>? party, System.Numerics.Vector3 center, float radius, float healHp)
	{
		if (party == null || !AnyHolder(party))
		{
			return true;
		}

		foreach (var member in party)
		{
			if (member != null &&
				System.Numerics.Vector3.Distance(center, member.Position) <= radius &&
				WouldFillOutOfOrder(party, member, healHp))
			{
				return false;
			}
		}

		return true;
	}

	private static IBattleChara? LowestMatching(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> match)
	{
		IBattleChara? best = null;
		foreach (var member in party)
		{
			if (member == null || !match(member))
			{
				continue;
			}

			if (best == null || BeirutaParty.HealHp(member) < BeirutaParty.HealHp(best))
			{
				best = member;
			}
		}

		return best;
	}

	private static bool AnyMember(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> match)
	{
		foreach (var member in party)
		{
			if (member != null && !member.IsDead && match(member))
			{
				return true;
			}
		}

		return false;
	}

}

#endregion

#region Single-Target Casting

// Casts an action on one specific party member, trying the RSR target types that can reach them.
internal static class BeirutaTarget
{
	public static bool TryUseOn(IBaseAction action, IBattleChara target, out IAction? act, bool usedUp = false)
	{
		act = null;

		if (!action.EnoughLevel || !action.IsEnabled || target == null || !target.IsValid() || target.IsDead)
		{
			return false;
		}

		if (target.DistanceToPlayer() > action.TargetInfo.Range || !target.CanSee())
		{
			return false;
		}

		return TryRsrTarget(action, target, out act, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, usedUp: usedUp, skipAoeCheck: true);
	}

	public static bool TryRsrTarget(IBaseAction action, IBattleChara target, out IAction? act,
		bool skipStatusProvideCheck = false, bool skipTargetStatusNeedCheck = false, bool usedUp = false,
		bool skipAoeCheck = false, bool skipCastingCheck = false, bool skipTTKCheck = false)
	{
		act = null;
		if (target == null)
		{
			return false;
		}

		var self = ECommons.GameHelpers.Player.Object;
		var isSelf = self != null && target.GameObjectId == self.GameObjectId;
		ReadOnlySpan<TargetType> tries = isSelf
			? [TargetType.Self]
			: !target.IsJobCategory(JobRole.Tank) || target.IsEnemy()
				? [default, TargetType.LowHPPercent, TargetType.LowHP, TargetType.Heal]
				: [default, TargetType.BeAttacked, TargetType.Tank, TargetType.LowHPPercent, TargetType.LowHP];

		var id = target.GameObjectId;
		bool OnlyTarget(IBattleChara member) => member.GameObjectId == id;

		foreach (var type in tries)
		{
			if (action.CanUse(out var used, skipStatusProvideCheck: skipStatusProvideCheck, skipTargetStatusNeedCheck: skipTargetStatusNeedCheck,
					usedUp: usedUp, skipAoeCheck: skipAoeCheck, skipCastingCheck: skipCastingCheck, skipTTKCheck: skipTTKCheck, targetOverride: type,
					targetFilter: OnlyTarget) &&
				action.Target.Target?.GameObjectId == id)
			{
				act = used;
				return true;
			}
		}

		return false;
	}
}

#endregion

#region Critical Delay

// A non-tank must stay critical for a moment before a GCD rescue heal goes on them.
internal static class BeirutaCritical
{
	public const float NonTankDelaySeconds = 1.5f;

	private const long StaleMs = 500;
	private static readonly Dictionary<ulong, (long Since, long Seen)> _low = [];

	public static bool ForGcd(IBattleChara member, float criticalHp) =>
		BeirutaParty.HealHp(member) < criticalHp &&
		(member.IsJobCategory(JobRole.Tank) || HeldBelow(member, criticalHp, NonTankDelaySeconds));

	private static bool HeldBelow(IBattleChara member, float hpRatio, float seconds)
	{
		var id = member.GameObjectId;
		if (BeirutaParty.HealHp(member) >= hpRatio)
		{
			_low.Remove(id);
			return false;
		}

		var now = Environment.TickCount64;
		if (!_low.TryGetValue(id, out var entry) || now - entry.Seen > StaleMs)
		{
			entry = (now, now);
		}

		_low[id] = (entry.Since, now);
		return now - entry.Since >= seconds * 1000f;
	}
}

#endregion

#region DoT Before Downtime

// Whether a DoT still beats a filler when the target leaves soon, using timeline downtime.
internal static class BeirutaDotValue
{
	private const float TickSeconds = 3f;
	private const float DotDuration = 30f;

	public static bool WorthBeforeDowntime(IBattleChara? target, float initial, float tick, float filler, float gcdCost = 1f)
	{
		if (target == null ||
			Updaters.BeirutaTimelineUpdater.SecondsUntilDowntime(target.BaseId) is not { } left)
		{
			return true;
		}

		var ticks = Math.Min(left, DotDuration) / TickSeconds;
		return initial + (tick * ticks) >= filler * gcdCost;
	}
}

#endregion

#region Pending Heals

// Heals that have landed but not yet shown on the HP bar, so the same player isn't healed twice.
internal static class PendingHeals
{
	private const long TimeoutMs = 3000;

	private readonly record struct Entry(ulong TargetId, uint Amount, uint HpAtSend, long SentAt);

	private static readonly List<Entry> _entries = [];

	public static void OnActionEffect(ECommons.Hooks.ActionEffectTypes.ActionEffectSet set)
	{
		try
		{
			var party = DataCenter.PartyMembers;
			if (party == null || party.Count == 0)
			{
				return;
			}

			var now = Environment.TickCount64;
			foreach (var effect in set.TargetEffects)
			{
				var targetId = effect.TargetID;
				IBattleChara? target = null;
				foreach (var member in party)
				{
					if (member != null && member.GameObjectId == targetId)
					{
						target = member;
						break;
					}
				}

				if (target == null || target.IsDead)
				{
					continue;
				}

				uint amount = 0;
				effect.ForEach(entry =>
				{
					if (entry.type == ECommons.Hooks.ActionEffectTypes.ActionEffectType.Heal)
					{
						amount += entry.value;
					}
				});

				if (amount > 0)
				{
					_entries.Add(new Entry(targetId, amount, target.CurrentHp, now));
				}
			}
		}
		catch (Exception ex)
		{
			ECommons.Logging.PluginLog.Error($"Error in PendingHeals: {ex.Message}");
		}
	}

	public static uint PendingHp(IBattleChara member)
	{
		if (_entries.Count == 0)
		{
			return 0;
		}

		var now = Environment.TickCount64;
		_ = _entries.RemoveAll(e => now - e.SentAt > TimeoutMs);

		uint total = 0;
		var id = member.GameObjectId;
		var hp = member.CurrentHp;
		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			var e = _entries[i];
			if (e.TargetId != id)
			{
				continue;
			}

			if (hp != e.HpAtSend || member.IsDead)
			{
				_entries.RemoveAt(i);
				continue;
			}

			total += e.Amount;
		}

		return total;
	}
}

#endregion