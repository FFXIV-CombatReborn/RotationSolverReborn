// Shared by the four Beiruta healers: the step order every job runs, heal maths, party checks and small timing helpers.
namespace RotationSolver.Engine.Helpers;

#region Pipeline

// The steps each healer fills in, and the fixed order they run in.
internal interface IBeirutaHealer
{
	bool PlanAbility(IAction nextGCD, out IAction? act);
	bool SpecialAbility(IAction nextGCD, out IAction? act);
	bool BurstAbility(IAction nextGCD, out IAction? act);
	bool ResourceAbility(IAction nextGCD, out IAction? act);
	bool AreaHealAbility(IAction nextGCD, out IAction? act);
	bool SpotHealAbility(IAction nextGCD, out IAction? act);
	bool TankHelpAbility(IAction nextGCD, out IAction? act);
	bool LowPriorityAbility(IAction nextGCD, out IAction? act);

	bool PlanGCD(out IAction? act);
	bool SpecialGCD(out IAction? act);
	bool WardGCD(out IAction? act);
	bool AreaHealGCD(out IAction? act);
	bool SpotHealGCD(out IAction? act);
}

internal static class BeirutaHealerFlow
{
	public static bool Ability(IBeirutaHealer job, IAction nextGCD, out IAction? act) =>
		Updaters.TimelineUpdater.TryPlannedAction(false, out act) ||
		job.PlanAbility(nextGCD, out act) ||
		job.SpecialAbility(nextGCD, out act) ||
		job.BurstAbility(nextGCD, out act) ||
		job.ResourceAbility(nextGCD, out act) ||
		job.AreaHealAbility(nextGCD, out act) ||
		(!BeirutaParty.AccretionUp &&
		 ((BeirutaIncoming.TankbusterWithin(BeirutaIncoming.TankbusterLeadSeconds) && job.TankHelpAbility(nextGCD, out act)) ||
		  job.SpotHealAbility(nextGCD, out act) ||
		  job.TankHelpAbility(nextGCD, out act)));

	public static bool WeaveRoomWanted(int pendingAbilities) =>
		DataCenter.InCombat && Updaters.TimelineUpdater.PendingPlannedAbilities() >= pendingAbilities;

	public static bool UrgentGcd(IBeirutaHealer job, out IAction? act) =>
		Updaters.TimelineUpdater.TryPlannedAction(true, out act) ||
		job.PlanGCD(out act) ||
		job.SpecialGCD(out act);

	public static bool HealGcd(IBeirutaHealer job, out IAction? act) =>
		job.WardGCD(out act) ||
		job.AreaHealGCD(out act) ||
		(!BeirutaParty.AccretionUp && job.SpotHealGCD(out act));
}

#endregion

#region Heal Maths

// Injured counts and area-heal thresholds, using the job's Ward Mode and incoming heal-over-time ticks.
internal interface IBeirutaHealContext
{
	bool WardMode { get; }
	bool UseCustomHPThreshold { get; }
	float HealBoost { get; }
	float LandingHotPotency(IBattleChara member);
}

internal static class BeirutaHeal
{
	public const int AoeCount = 3;

	public static bool CanRun(bool lockout = false) => Service.Config.AutoHeal && DataCenter.InCombat && !lockout;

	public static bool GroupHealBlocked => BeirutaAccretion.GroupHealBlocked(DataCenter.PartyMembers);

	public static bool HoldAoe(float range) =>
		GroupHealBlocked || (BeirutaParty.CoHealerBigHealSoon && !BeirutaParty.AnyCritical(range));

	public static float EffectiveHpRatio(IBeirutaHealContext job, IBattleChara member) =>
		BeirutaHealModel.EffectiveHpRatio(member, job.WardMode, job.LandingHotPotency(member));

	public static bool Injured(IBeirutaHealContext job, IBattleChara member, float threshold) =>
		BeirutaParty.HealHp(member) < BeirutaHealModel.AlwaysInjuredHp ||
		EffectiveHpRatio(job, member) < threshold;

	public static int CountInjured(IBeirutaHealContext job, float range, float threshold, bool nonTanksOnly = false)
	{
		var count = 0;
		foreach (var member in DataCenter.PartyMembers)
		{
			if (BeirutaParty.IsHealableInRange(member, range) &&
				!BeirutaParty.IsHealBlocked(member) &&
				!(nonTanksOnly && BeirutaParty.IsTankMember(member)) &&
				Injured(job, member, threshold))
			{
				count++;
			}
		}

		return count;
	}

	public static bool AoeNeeded(IBeirutaHealContext job, float range, float threshold) =>
		CountInjured(job, range, threshold) >= AoeCount;

	public static bool AoeGcdNeeded(IBeirutaHealContext job, float range, float threshold) =>
		AoeNeeded(job, range, threshold) || (job.WardMode && CountInjured(job, range, threshold, true) > 0);

	public static float Threshold(IBeirutaHealContext job, float potency, float customWardOff, float wardOffCap = 1f) =>
		BeirutaHealModel.Threshold(potency, job.HealBoost, job.WardMode, job.UseCustomHPThreshold, customWardOff, DataCenter.PartyMembers, wardOffCap);
}

#endregion

#region Movement / Potion / Opener

// When the player counts as moving, when to drink a potion, and the short hold after the opener cast.
internal static class BeirutaMove
{
	public const float GraceSeconds = 0.3f;
	public const float ThresholdSeconds = 1.0f;
	public const float LeadSeconds = 0.5f;

	private static float MovingTime => DataCenter.IsMoving ? DataCenter.MovingRaw + DataCenter.DefaultGCDRemain : 0;

	public static bool PastGrace => DataCenter.InCombat && DataCenter.IsMoving && MovingTime > GraceSeconds;

	public static bool Sufficient => DataCenter.InCombat && DataCenter.IsMoving && MovingTime > ThresholdSeconds;

	public static bool PastLead => Sufficient && MovingTime > ThresholdSeconds + LeadSeconds;

	public static bool RaiseSwiftHeld =>
		(StatusHelper.PlayerHasStatus(true, StatusHelper.SwiftcastStatus) || IActionHelper.IsLastAction(ActionID.SwiftcastPvE)) &&
		DataCenter.MergedStatus.HasFlag(AutoStatus.Raise);
}

public static class BeirutaSettingNames
{
	public const string HidePage = "Hide";
	public const string OpenerPage = "Opener / Potion";
	public const string DamagePage = "Damage";
	public const string MovementPage = "Movement";
	public const string HealingPage = "Healing";
}

public enum BeirutaPlanSource : byte
{
	[Description(CdPlanNames.BuiltInPlan)]
	BuiltIn,

	[Description(CdPlanNames.ManualPlan)]
	Manual,
}

public enum BeirutaSettingsPage : byte
{
	[Description(BeirutaSettingNames.HidePage)]
	Hide,

	[Description(BeirutaSettingNames.OpenerPage)]
	Opener,

	[Description(BeirutaSettingNames.DamagePage)]
	Damage,

	[Description(BeirutaSettingNames.MovementPage)]
	Movement,

	[Description(BeirutaSettingNames.HealingPage)]
	Healing,
}

public enum BeirutaPotTiming : byte
{
	[Description("Opener (countdown)")]
	Opener,

	[Description("2 minutes")]
	TwoMinutes,

	[Description("With others (someone has the potion buff)")]
	WithOthers,
}

internal static class BeirutaPotion
{
	public static bool Ready(bool use, BeirutaPotTiming timing, bool isBurst, bool burstSoon) =>
		use &&
		DataCenter.InCombat &&
		isBurst &&
		!(timing == BeirutaPotTiming.TwoMinutes && DataCenter.CombatTimeRaw < 30f) &&
		(timing == BeirutaPotTiming.WithOthers ? BeirutaParty.IsPartyMedicated : burstSoon);
}

internal sealed class BeirutaOpenerHold
{
	public const float SendAtCountdownSeconds = 0.35f;
	private const long HoldMs = 3_500;

	private long _sentMs;

	public void Start() => _sentMs = Environment.TickCount64;

	public bool Active(params ReadOnlySpan<IBaseAction> dots) =>
		_sentMs != 0 &&
		Environment.TickCount64 - _sentMs < HoldMs &&
		!IActionHelper.IsLastGCD(true, [.. dots]);
}

#endregion

#region Incoming Damage

// Whether a raidwide or tankbuster is coming: timeline Damage rows first, the boss cast otherwise.
internal static class BeirutaIncoming
{
	public static bool RaidwideWithin(float seconds) => RaidwideWithin(seconds, out _);

	public static bool RaidwideWithin(float seconds, out bool heavy)
	{
		heavy = false;
		if (Updaters.TimelineUpdater.HasDamageRows)
		{
			if (!Updaters.TimelineUpdater.NextDamage(seconds, false, out _, out var kind))
			{
				return false;
			}

			heavy = kind == Updaters.TimelineDamageKind.Heavy;
			return true;
		}

		return DataCenter.IsHostileCastingAOE;
	}

	public const float TankbusterLeadSeconds = 5f;

	public static bool TankbusterWithin(float seconds) =>
		Updaters.TimelineUpdater.HasTankbusterRows
			? Updaters.TimelineUpdater.NextDamage(seconds, true, out _, out _)
			: DataCenter.IsHostileCastingToTank;
}

#endregion

#region Party Checks

// Who to heal: tanks, invulnerable or undead players, fresh tank mitigation, spot-heal rules, lowest-HP lookups.
internal static class BeirutaParty
{
	public const float SingleHealRange = 30f;
	public const float CriticalHp = 0.3f;
	public const float ExtremeLowTankHp = 0.3f;

	private const float UndeadReleaseSeconds = 1f;
	private const float EarthlyDominanceGateSeconds = 3f;
	private const float FreshMitigationSeconds = 6f;
	private const float BallparkDamagePercent = 0.08f;

	private static readonly StatusID[] FullInvulnStatus =
	[
		StatusID.HallowedGround,
		StatusID.HallowedGround_1302,
		StatusID.Superbolide,
	];

	private static readonly StatusID[] UndeadStatus =
	[
		StatusID.Holmgang_409,
		StatusID.WalkingDead,
	];

	private static readonly (StatusID Status, float Duration)[] TankBigMitigationStatus =
	[
		(StatusID.Rampart, 20f),
		(StatusID.Rampart_1191, 20f),
		(StatusID.Sentinel, 15f),
		(StatusID.Guardian, 15f),
		(StatusID.Vengeance, 15f),
		(StatusID.Damnation, 15f),
		(StatusID.ShadowWall, 15f),
		(StatusID.ShadowedVigil, 15f),
		(StatusID.Nebula, 15f),
		(StatusID.GreatNebula, 15f),
	];

	private static readonly (StatusID Status, float Duration)[] TankSmallMitigationStatus =
	[
		(StatusID.Sheltron, 4f),
		(StatusID.HolySheltron, 8f),
		(StatusID.Bulwark, 10f),
		(StatusID.RawIntuition, 6f),
		(StatusID.Bloodwhetting, 8f),
		(StatusID.ThrillOfBattle, 10f),
		(StatusID.BlackestNight, 7f),
		(StatusID.Oblation, 10f),
		(StatusID.DarkMind, 10f),
		(StatusID.HeartOfStone, 7f),
		(StatusID.HeartOfCorundum, 8f),
		(StatusID.Camouflage, 20f),
	];

	private static readonly StatusID[] CoHealerMacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];

	private static readonly HashSet<uint> RaidBuffStatusIds =
	[
		(uint)StatusID.Divination,
		(uint)StatusID.Brotherhood,
		(uint)StatusID.BattleLitany,
		(uint)StatusID.StarryMuse,
		(uint)StatusID.Embolden,
		(uint)StatusID.SearingLight,
		(uint)StatusID.BattleVoice,
		(uint)StatusID.TechnicalFinish,
		(uint)StatusID.ArcaneCircle,
	];

	private static IEnumerable<IBattleChara> Party => DataCenter.PartyMembers;

	public static float HealHp(IBattleChara member) =>
		member.MaxHp == 0
			? member.GetHealthRatio()
			: MathF.Min(1f, member.GetHealthRatio() + ((float)PendingHeals.PendingHp(member) / member.MaxHp));

	public static bool AccretionUp => BeirutaAccretion.AnyHolder(DataCenter.PartyMembers);

	public static float HpWithShield(IBattleChara member) =>
		member.MaxHp == 0 ? 1f : HealHp(member) + ((float)member.GetObjectShield() / member.MaxHp);

	public static bool IsTankMember(IBattleChara member) => member.IsJobCategory(JobRole.Tank);

	public static bool IsHealableInRange(IBattleChara? member, float range) =>
		member != null && !member.IsDead && member.DistanceToPlayer() <= range;

	public static bool AnyHas(Func<uint, bool> match)
	{
		foreach (var member in Party)
		{
			if (member?.StatusList == null)
			{
				continue;
			}

			foreach (var status in member.StatusList)
			{
				if (match(status.StatusId))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static readonly StatusID[] TargetRaidDebuffStatus =
	[
		StatusID.ChainStratagem,
		StatusID.ChainStratagem_1406,
		StatusID.Dokumori,
		StatusID.Dokumori_4303,
	];

	public static bool IsPartyBurst =>
		(ECommons.DalamudServices.Svc.Targets.Target is IBattleChara target && target.HasStatus(false, TargetRaidDebuffStatus)) ||
		AnyHas(static id => RaidBuffStatusIds.Contains(id));

	public static bool IsPartyMedicated => AnyHas(static id => id == (uint)StatusID.Medicated);

	private static IBattleChara? FirstMember(Func<IBattleChara, bool> match)
	{
		foreach (var member in Party)
		{
			if (member != null && match(member))
			{
				return member;
			}
		}

		return null;
	}

	public static bool AnyPartyMemberDead => FirstMember(static m => m.IsDead) != null;

	public static bool HasPartyMemberWithOwnStatus(StatusID status) => FirstMember(m => m.HasStatus(true, status)) != null;

	public static bool WhmInParty => FirstMember(static m =>
		!m.IsDead &&
		m.IsJobs(ECommons.ExcelServices.Job.WHM) &&
		m.GameObjectId != ECommons.GameHelpers.Player.Object?.GameObjectId) != null;

	public static bool IsHealBlocked(IBattleChara member)
	{
		if (member.HasStatus(false, FullInvulnStatus) || member.HasStatus(false, StatusID.LivingDead))
		{
			return true;
		}

		if (member.HasStatus(false, UndeadStatus))
		{
			return WhmInParty || member.StatusTime(false, UndeadStatus) > UndeadReleaseSeconds;
		}

		return false;
	}

	public static bool IsJustOffUndead(IBattleChara member) =>
		!WhmInParty &&
		member.HasStatus(false, UndeadStatus) &&
		member.StatusTime(false, UndeadStatus) <= UndeadReleaseSeconds;

	public static bool SkipSingleHeal(IBattleChara member) =>
		IsHealBlocked(member) ||
		(WhmInParty && IsTankMember(member) && HealHp(member) < ExtremeLowTankHp);

	public static bool IsFreshMitigatingTank(IBattleChara member) =>
		IsTankMember(member) && !IsHealBlocked(member) && (HasFreshBigMitigation(member) || HasFreshSmallMitigation(member));

	public static bool IsFreshBigMitigatingTank(IBattleChara member) =>
		IsTankMember(member) && !IsHealBlocked(member) && HasFreshBigMitigation(member);

	public static bool HasTankMitigation(IBattleChara member) =>
		HasMitigation(member, TankBigMitigationStatus, false) || HasMitigation(member, TankSmallMitigationStatus, false);

	public static bool HasFreshSmallMitigation(IBattleChara member) => HasMitigation(member, TankSmallMitigationStatus, true);

	public static bool HasFreshBigMitigation(IBattleChara member) => HasMitigation(member, TankBigMitigationStatus, true);

	private static bool HasMitigation(IBattleChara member, (StatusID Status, float Duration)[] statuses, bool freshOnly)
	{
		foreach (var (status, duration) in statuses)
		{
			if (member.HasStatus(false, status) &&
				(!freshOnly || duration - member.StatusTime(false, status) <= FreshMitigationSeconds))
			{
				return true;
			}
		}

		return false;
	}

	public static IBattleChara? FindLowest(float range, Func<IBattleChara, bool> filter)
	{
		IBattleChara? best = null;
		foreach (var member in Party)
		{
			if (!IsHealableInRange(member, range) || SkipSingleHeal(member) || !filter(member))
			{
				continue;
			}

			if (best == null || HealHp(member) < HealHp(best))
			{
				best = member;
			}
		}

		return best;
	}

	public static int Count(float range, Func<IBattleChara, bool> filter)
	{
		var count = 0;
		foreach (var member in Party)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member) && filter(member))
			{
				count++;
			}
		}

		return count;
	}

	public static bool SpotHealWorthy(IBattleChara member) => IsTankMember(member) || IsJustRaised(member) || TopUpAll;

	public static bool TopUpAll => Updaters.TimelineUpdater.PlannedTopUpAll;

	public static IBattleChara? TopUpAllTarget(float range) =>
		TopUpAll ? FindLowest(range, static m => HealHp(m) < 1f) : null;

	private const float WeaknessDuration = 100f;
	private const float JustRaisedSeconds = 6f;

	public static bool IsJustRaised(IBattleChara member) =>
		member.HasStatus(false, StatusID.Weakness, StatusID.BrinkOfDeath) &&
		member.StatusTime(false, StatusID.Weakness, StatusID.BrinkOfDeath) >= WeaknessDuration - JustRaisedSeconds;

	private const float BehindPartyMargin = 0.2f;

	public static float AverageHp(float range)
	{
		var total = 0f;
		var count = 0;
		foreach (var member in Party)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member))
			{
				total += HealHp(member);
				count++;
			}
		}

		return count == 0 ? 1f : total / count;
	}

	public static bool SpotHealAllowed(IBattleChara member, bool aoeNeeded, float range) =>
		!aoeNeeded ||
		TopUpAll ||
		IsJustRaised(member) ||
		HealHp(member) < CriticalHp ||
		HealHp(member) < AverageHp(range) - BehindPartyMargin;

	public static bool HasSingleTargetHot(IBattleChara member) =>
		member.HasStatus(false, StatusID.AspectedBenefic, StatusID.Regen, StatusID.Regen_897, StatusID.Regen_1330);

	public static IBattleChara? DoomTarget(float range) => FindLowest(range, static m => m.DoomNeedHealing());

	public static bool AnyCritical(float range) => Count(range, static m => HealHp(m) < CriticalHp) > 0;

	public static IBattleChara? AggroTank(IBattleChara? enemy) =>
		enemy?.TargetObject?.GameObjectId is { } aggroId
			? FirstMember(m => m.GameObjectId == aggroId && IsTankMember(m))
			: null;

	public static IBattleChara? EnemyAggroTank => AggroTank(ECommons.DalamudServices.Svc.Targets.Target as IBattleChara);

	public static IBattleChara? TankToHelp(IBattleChara? enemy) =>
		FindLowest(SingleHealRange, IsFreshMitigatingTank) ??
		AggroTank(enemy) ??
		FindLowest(SingleHealRange, IsTankMember);

	public static bool CoHealerBigHealSoon => FirstMember(static m =>
		!m.IsDead &&
		(m.HasStatus(false, StatusID.GiantDominance) ||
		 m.HasStatus(false, CoHealerMacrocosmosStatus) ||
		 (m.HasStatus(false, StatusID.EarthlyDominance) && m.StatusTime(false, StatusID.EarthlyDominance) < EarthlyDominanceGateSeconds))) != null;

	public static int AttackableHostilesInRangeOf(float range)
	{
		var count = 0;
		foreach (var enemy in DataCenter.AllHostileTargets)
		{
			if (enemy != null && enemy.IsAttackable() && enemy.DistanceToPlayer() < range)
			{
				count++;
			}
		}

		return count;
	}

	public static float ExpectedHpToLive12Seconds(IBattleChara? player) =>
		player == null ? 1f : BallparkDamagePercent * player.MaxHp * DataCenter.PartyMembers.Count * 12;
}

#endregion

#region Countdown

// Waits a moment after a countdown starts before the opener reacts to it.
internal sealed class BeirutaCountdownDelay
{
	private const long ResponseDelayMs = 1_000;

	private long _startedAtMs;
	private long _lastSeenMs;
	private float _lastRemain;

	public bool Passed(float remainTime)
	{
		var now = Environment.TickCount64;
		if (_startedAtMs == 0 || now - _lastSeenMs > 500 || remainTime > _lastRemain + 0.5f)
		{
			_startedAtMs = now;
		}

		_lastSeenMs = now;
		_lastRemain = remainTime;
		return now - _startedAtMs >= ResponseDelayMs;
	}
}

#endregion
