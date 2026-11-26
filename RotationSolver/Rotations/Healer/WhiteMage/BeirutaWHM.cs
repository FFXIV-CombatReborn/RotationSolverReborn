using static RotationSolver.Engine.Helpers.BeirutaParty;

namespace RotationSolver.Rotations.Healer.WhiteMage;

[Rotation("BeirutaWHM", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate White Mage. Damage and healing are automatic. Planned cooldowns (mitigation, HoTs, Lightspeed and the like) follow the built-in CD plan when there is one (see the CD Plan section); otherwise use your Boss Mod / BMR CD planner or press them yourself, with Intercept set to GCD only and ability intercept unticked. Questions? Ask @Beiruta on the RSR Discord.")]
[SourceCode(Path = "main/Rotations/Healer/WhiteMage/BeirutaWHM.cs")]
[ExtraRotation]

public sealed class BeirutaWHM : WhiteMageRotation, Updaters.IBeirutaTimelineOption, IBeirutaHealContext, IBeirutaHealer
{
	#region Config Options

	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Use built-in CD plan: mitigation follows the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press those cooldowns.")]
	public BeirutaPlanSource CdPlanFrom { get; set; } = BeirutaPlanSource.BuiltIn;

	public bool UseTimelinePlan => CdPlanFrom == BeirutaPlanSource.BuiltIn;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use recommended settings for the plan", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"When a built-in plan loads, the settings it recommends are applied, like a preset. Changing a setting afterwards (e.g. your Ward Mode macro) works as usual.\n" +
		"Your saved settings are not changed by the plan: they show as you set them and come back once the plan unloads or this is off.")]
	public bool UsePlanSettings { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Cure III", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"On: the plan's Cure III is cast in its window once 2+ party members are below 95%, and no automatic AoE heal GCD within 5s of it.\n" +
		"Off: planned Cure III is skipped; AoE heal GCDs go by party HP.")]
	public bool UsePlannedCureIii { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Medica III", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"On: the plan's Medica III is cast in its window once 2+ party members are below 95%, and no automatic AoE heal GCD within 5s of it.\n" +
		"Off: planned Medica III is skipped; AoE heal GCDs go by party HP.")]
	public bool UsePlannedMedicaIii { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Afflatus Rapture", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"On: the plan's Afflatus Rapture is cast in its window once 2+ party members are below 95%, and no automatic AoE heal GCD within 5s of it.\n" +
		"Lilies are kept for it, and Afflatus Misery is used before a planned Rapture would overcap Blood Lilies.\n" +
		"Off: planned Afflatus Rapture is skipped; lilies go by party HP.")]
	public bool UsePlannedAfflatusRapture { get; set; } = false;

	IEnumerable<(uint ActionId, string Config)> Updaters.IBeirutaTimelineOption.OptionalPlannedActions =>
	[
		((uint)ActionID.CureIiiPvE, nameof(UsePlannedCureIii)),
		((uint)ActionID.MedicaIiiPvE, nameof(UsePlannedMedicaIii)),
		((uint)ActionID.AfflatusRapturePvE, nameof(UsePlannedAfflatusRapture)),
	];

	bool Updaters.IBeirutaTimelineOption.AllowPlannedAction(uint actionId) => actionId switch
	{
		(uint)ActionID.CureIiiPvE => UsePlannedCureIii,
		(uint)ActionID.MedicaIiiPvE => UsePlannedMedicaIii,
		(uint)ActionID.AfflatusRapturePvE => UsePlannedAfflatusRapture,
		_ => true,
	};

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		Updaters.BeirutaTimelineUpdater.HoldQueuedPlannedAction(action.ID);

	string? Updaters.IBeirutaTimelineOption.PlannedActionWait(uint actionId) => actionId switch
	{
		(uint)ActionID.CureIiiPvE or (uint)ActionID.MedicaIiiPvE or (uint)ActionID.MedicaIiPvE or (uint)ActionID.MedicaPvE or (uint)ActionID.AfflatusRapturePvE
			=> CountInjured(PartyHealRange, PlannedHealHp) >= PlannedAoeHealCount ? null : "nobody needed healing",
		(uint)ActionID.AfflatusSolacePvE or (uint)ActionID.CurePvE or (uint)ActionID.CureIiPvE or (uint)ActionID.TetragrammatonPvE or (uint)ActionID.BenedictionPvE
			=> FindLowest(SingleHealRange, static m => HealHp(m) < PlannedHealHp) != null ? null : "nobody needed healing",
		_ => null,
	};

	#endregion

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps every non-tank full.\n" +
		"- Afflatus Rapture (also spends the kept lily) > Medica III > Cure III as soon as one non-tank is below the heal's full-use line (about 85-90%).\n" +
		"- Thresholds use real HP; no waiting for Presence of Mind or the co-healer.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[More settings] Show", Tooltip =
		"Pick a page to show its detailed settings. This only changes what is shown.")]
	public BeirutaSettingsPage MoreSettings { get; set; } = BeirutaSettingsPage.Hide;

	private bool WardActive => WardMode || Updaters.BeirutaTimelineUpdater.PlannedWardMode;

	bool IBeirutaHealContext.WardMode => WardActive;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Regen on the stance tank at about 5s, then Divine Benison before the pull.")]
	public CountdownShieldStrategy CountdownShield { get; set; } = CountdownShieldStrategy.None;

	public enum CountdownShieldStrategy : byte
	{
		[Description("None")]
		None,

		[Description("Regen + Divine Benison on stance tank (5s)")]
		TankRegenBenison,
	}

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Presence of Mind / Afflatus Misery", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Holds Presence of Mind and Afflatus Misery until this long after the pull. Presence of Mind also needs AutoBurst.")]
	public PresenceOfMindOpenerTiming PresenceOfMindOpener { get; set; } = PresenceOfMindOpenerTiming.SixSeconds;

	public enum PresenceOfMindOpenerTiming : byte
	{
		[Description("0s")]
		ZeroSeconds,

		[Description("3s")]
		ThreeSeconds,

		[Description("6s")]
		SixSeconds,
	}

	#endregion

	#region Potion

	[RotationConfig(CombatType.PvE, Name = "[Potion] Use potion", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage)]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > First potion", Parent = nameof(UsePotion), Tooltip =
		"Opener: during the countdown. 2 minutes: the first potion waits for the 2-minute burst.\n" +
		"With others: only when a party member already has the potion buff (Medicated), so you pot together with them.")]
	public BeirutaPotTiming PotionTiming { get; set; } = BeirutaPotTiming.Opener;

	#endregion

	#region Damage

	[RotationConfig(CombatType.PvE, Name = "[Damage] Hold Afflatus Misery for raid buffs (if a raid-buff job is present)", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"Glare IV is also held until the shortest raid buff is about to end.\n" +
		"Misery is still dumped early when there is enough time and lilies to rebuild 3 Blood Lilies before the next burst, so it doesn't block lilies.")]
	public bool HoldMiseryForBurst { get; set; } = true;

	#endregion

	#region Thin Air

	[Range(0, 10000, ConfigUnitType.None, 100)]
	[RotationConfig(CombatType.PvE, Name = "[Thin Air] Minimum spell MP", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"Thin Air goes on spells costing at least this much MP, on Raise, and before it would cap.")]
	public float ThinAirNeed { get; set; } = 1000;

	[RotationConfig(CombatType.PvE, Name = "[Thin Air] Last charge", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage)]
	public ThinAirUsageStrategy ThinAirLastChargeUsage { get; set; } = ThinAirUsageStrategy.ReserveLastChargeForRaise;

	public enum ThinAirUsageStrategy : byte
	{
		[Description("Use all charges")]
		UseAllCharges,

		[Description("Keep for raise")]
		ReserveLastChargeForRaise,

		[Description("Keep for manual use")]
		ReserveLastCharge,
	}

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"Instant options while moving: lily heals > Afflatus Misery > Glare IV > early Dia, then Swiftcast, then Regen.\n" +
		"No heal GCDs are used just for movement in burst.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > When to use it", Parent = nameof(UseSwiftcastForMovement), Tooltip =
		"Last resort: only when no instant option is available. First: before the instant options.\n" +
		"Raise and AoE heal GCDs only: kept for raise and for moving Medica III / Cure III when Afflatus Rapture can't cover.")]
	public SwiftcastMovementPriority SwiftcastMovement { get; set; } = SwiftcastMovementPriority.LastResort;

	public enum SwiftcastMovementPriority : byte
	{
		[Description("Last resort (keep for raise)")]
		LastResort = 0,

		[Description("First (e.g. death = enrage)")]
		First = 2,

		[Description("Raise and AoE heal GCDs only")]
		HealOnly = 3,
	}

	#endregion

	#region Healing

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.HealingPage, Tooltip =
		"Off: each area heal waits until 3+ players are low enough that it won't be wasted, worked out from its potency, Temperance, the co-healer's smallest GCD heal and incoming HoT ticks.\n" +
		"Off also means: one lily kept, Benediction off, Tetragrammaton on tanks below 60% (2 charges) / 50% (1 charge), Afflatus Solace on tanks below 40%.\n" +
		"On: each area heal fires when 3+ players in range are below the value set here.\n" +
		"Ward Mode ignores these values and uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Keep one lily for emergencies", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"The last lily is only spent on rescues, Ward Mode and movement.")]
	public bool KeepEmergencyLily { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Benediction usage", Parent = nameof(UseCustomHPThreshold))]
	public BenedictionUsage BenedictionMode { get; set; } = BenedictionUsage.Off;

	[RotationConfig(CombatType.PvE, Name = "    > Benediction: tanks only", Parent = nameof(UseCustomHPThreshold))]
	public bool BenedictionTankOnly { get; set; } = true;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Benediction: below", Parent = nameof(UseCustomHPThreshold))]
	public float BenedictionHeal { get; set; } = 0.15f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Tetragrammaton 2 charges: below", Parent = nameof(UseCustomHPThreshold))]
	public float TetragrammatonSecond { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Tetragrammaton 1 charge: below", Parent = nameof(UseCustomHPThreshold))]
	public float TetragrammatonLast { get; set; } = 0.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Afflatus Solace (no Tetragrammaton): tank below", Parent = nameof(UseCustomHPThreshold))]
	public float SolaceTankHeal { get; set; } = 0.4f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Afflatus Rapture: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float RaptureHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Medica III: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float MedicaHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Cure III: area heal when 3+ around the target below", Parent = nameof(UseCustomHPThreshold))]
	public float CureIIIHeal { get; set; } = 0.7f;

	public enum BenedictionUsage : byte
	{
		[Description("Off")]
		Off,

		[Description("Critical only")]
		CriticalOnly,

		[Description("Critical and Walking Dead")]
		CriticalAndWalkingDead,
	}

	private BenedictionUsage BenedictionModeValue => UseCustomHPThreshold ? BenedictionMode : BenedictionUsage.Off;
	private bool BenedictionTankOnlyValue => !UseCustomHPThreshold || BenedictionTankOnly;
	private float BenedictionHealValue => UseCustomHPThreshold ? BenedictionHeal : 0.15f;
	private bool KeepEmergencyLilyValue => !UseCustomHPThreshold || KeepEmergencyLily;
	private float TetragrammatonSecondValue => UseCustomHPThreshold ? TetragrammatonSecond : 0.6f;
	private float TetragrammatonLastValue => UseCustomHPThreshold ? TetragrammatonLast : 0.5f;
	private float SolaceTankHealValue => UseCustomHPThreshold ? SolaceTankHeal : 0.4f;

	#endregion

	#endregion

	#region Helpers

	private const float BurstDiaRefreshSeconds = 11f;
	private const float MovingDiaRefreshSeconds = 11f;
	private const float MovementHealHp = 0.8f;
	private const float MovementRegenTankHp = 0.8f;
	private const float MovementSolaceHp = 0.9f;
	private const float BurstLilyPrepSeconds = 15f;

	private bool HasLiturgyOfTheBell => StatusHelper.PlayerHasStatus(true, StatusID.LiturgyOfTheBell);

	private bool HasSacredSight => SacredSightStacks > 0;

	private bool HasHealingLockout => HasLiturgyOfTheBell;

	private bool CurrentTargetDiaMissingOrEnding(float remainingSeconds)
	{
		if (CurrentTarget == null)
		{
			return false;
		}

		return
			(DiaPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.Dia) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Dia))) ||
			(!DiaPvE.EnoughLevel && AeroIiPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.AeroIi) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.AeroIi))) ||
			(!DiaPvE.EnoughLevel && !AeroIiPvE.EnoughLevel && AeroPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.Aero) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Aero)));
	}

	private const float DiaInitialPotency = 85f;
	private const float DiaTickPotency = 85f;
	private const float GlareFillerPotency = 330f;

	private static bool DiaWorthBeforeDowntime(IBaseAction dia) =>
		BeirutaDotValue.WorthBeforeDowntime(dia.Target.Target, DiaInitialPotency, DiaTickPotency, GlareFillerPotency);

	private bool CanUseCurrentDia(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (DiaPvE.EnoughLevel &&
			DiaPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			DiaWorthBeforeDowntime(DiaPvE))
		{
			return true;
		}

		if (!DiaPvE.EnoughLevel && AeroIiPvE.EnoughLevel &&
			AeroIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			DiaWorthBeforeDowntime(AeroIiPvE))
		{
			return true;
		}

		if (!DiaPvE.EnoughLevel && !AeroIiPvE.EnoughLevel && AeroPvE.EnoughLevel &&
			AeroPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			DiaWorthBeforeDowntime(AeroPvE))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool IsMovementPreferredNextGCD(IAction nextGCD)
	{
		if (nextGCD == null)
		{
			return false;
		}

		return nextGCD.IsTheSameTo(false,
			AfflatusMiseryPvE,
			GlareIvPvE,
			AfflatusRapturePvE,
			AfflatusSolacePvE,
			RegenPvE,
			AeroPvE,
			AeroIiPvE,
			DiaPvE);
	}

	#endregion

	#region Burst

	private static readonly HashSet<uint> PartyRaidBuffStatusIds =
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

	private static bool RaidBuffJobInParty
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member != null &&
					member.IsJobs(
						ECommons.ExcelServices.Job.SCH,
						ECommons.ExcelServices.Job.BRD,
						ECommons.ExcelServices.Job.DNC,
						ECommons.ExcelServices.Job.RPR,
						ECommons.ExcelServices.Job.NIN,
						ECommons.ExcelServices.Job.DRG,
						ECommons.ExcelServices.Job.RDM,
						ECommons.ExcelServices.Job.SMN,
						ECommons.ExcelServices.Job.PCT,
						ECommons.ExcelServices.Job.MNK))
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool InDamageBurst => HasPresenceOfMind || IsPartyBurst;

	private float PresenceOfMindOpenerSeconds => PresenceOfMindOpener switch
	{
		PresenceOfMindOpenerTiming.ZeroSeconds => 0f,
		PresenceOfMindOpenerTiming.ThreeSeconds => 3f,
		_ => 6f,
	};

	private bool OpenerTimingPassed => InCombat && !CombatElapsedLess(PresenceOfMindOpenerSeconds);

	private bool PresenceOfMindAllowed => IsBurst && OpenerTimingPassed;

	private bool ShouldUsePotionInCombat =>
		BeirutaPotion.Ready(UsePotion, PotionTiming, IsBurst, HasPresenceOfMind ||
		 (PresenceOfMindPvE.EnoughLevel &&
		  !CombatElapsedLess(Math.Max(0f, PresenceOfMindOpenerSeconds - 5f)) &&
		  PresenceOfMindPvE.Cooldown.WillHaveOneCharge(5)));

	private bool MiseryHoldActive => HoldMiseryForBurst && RaidBuffJobInParty;

	private const uint MiseryRebuildGcds = 3;
	private const float LilyIntervalSeconds = 20f;

	private float SecondsToNextBurst =>
		PresenceOfMindPvE.EnoughLevel && PresenceOfMindPvE.Cooldown.IsCoolingDown
			? PresenceOfMindPvE.Cooldown.RecastTimeRemainOneCharge
			: 0f;

	private bool MiseryDumpSafe
	{
		get
		{
			if (!MiseryHoldActive || BloodLily < 3 || !IsBurst)
			{
				return false;
			}

			var spendWindow = SecondsToNextBurst - GCDTime(MiseryRebuildGcds);
			if (spendWindow <= 0f)
			{
				return false;
			}

			var lilyIn = MathF.Max(0f, LilyTime);
			var gained = lilyIn <= spendWindow ? 1 + (int)((spendWindow - lilyIn) / LilyIntervalSeconds) : 0;
			var needed = KeepEmergencyLilyValue ? 4 : 3;
			return Lily + gained >= needed;
		}
	}

	private bool TargetDying => CurrentTarget != null && CurrentTarget.IsDying();

	private bool ShouldUseMisery()
	{
		if (BloodLily < 3 || !OpenerTimingPassed)
		{
			return false;
		}

		if (TargetDying || PlannedLilySoon)
		{
			return true;
		}

		return MiseryHoldActive
			? IsPartyBurst || MiseryDumpSafe
			: BeirutaMove.PastGrace || InDamageBurst;
	}

	private const uint SacredSightSpareGcds = 2;
	private const uint SacredSightWardSpareGcds = 2;

	private bool SacredSightExpiring =>
		HasSacredSight &&
		StatusHelper.PlayerWillStatusEndGCD(
			SacredSightStacks + SacredSightSpareGcds + (WardActive ? SacredSightWardSpareGcds : 0u),
			1f, true, StatusID.SacredSight);

	private static readonly HashSet<uint> TargetRaidDebuffIds =
	[
		(uint)StatusID.ChainStratagem,
		(uint)StatusID.ChainStratagem_1406,
		(uint)StatusID.Dokumori,
		(uint)StatusID.Dokumori_4303,
	];

	private static float ShortestBurstRemaining
	{
		get
		{
			var shortest = float.MaxValue;

			if (CurrentTarget?.StatusList != null)
			{
				foreach (var status in CurrentTarget.StatusList)
				{
					if (TargetRaidDebuffIds.Contains(status.StatusId) && status.RemainingTime > 0f)
					{
						shortest = MathF.Min(shortest, status.RemainingTime);
					}
				}
			}

			if (PartyMembers != null)
			{
				foreach (var member in PartyMembers)
				{
					if (member?.StatusList == null)
					{
						continue;
					}

					foreach (var status in member.StatusList)
					{
						if (PartyRaidBuffStatusIds.Contains(status.StatusId) && status.RemainingTime > 0f)
						{
							shortest = MathF.Min(shortest, status.RemainingTime);
						}
					}
				}
			}

			return shortest;
		}
	}

	private bool BurstEndingForGlareIv =>
		IsPartyBurst &&
		ShortestBurstRemaining <= GCDTime(
			SacredSightStacks + SacredSightSpareGcds + (WardActive ? SacredSightWardSpareGcds : 0u),
			DataCenter.DefaultGCDRemain);

	private bool ShouldUseGlareIv => BurstEndingForGlareIv || SacredSightExpiring || BeirutaMove.PastGrace || TargetDying;

	#endregion

	#region Lilies

	private bool LilyAvailable(bool emergency) =>
		Lily > (KeepEmergencyLilyValue && !emergency ? 1 : 0) &&
		(emergency || PlanLilyAllowsSpend);

	private const float PlannedHealHp = 0.95f;
	private const int PlannedAoeHealCount = 2;
	private const float PlannedLilyLookahead = 60f;
	private const float PlannedMiseryLeadSeconds = 6f;
	private const float PlannedAoeHealGapSeconds = 5f;

	private static readonly List<float> _plannedLilyStarts = [];

	private bool PlanLilyAllowsSpend
	{
		get
		{
			Updaters.BeirutaTimelineUpdater.UpcomingStarts(PlannedLilyLookahead, _plannedLilyStarts,
				AfflatusRapturePvE.ID, AfflatusSolacePvE.ID);
			return _plannedLilyStarts.Count == 0 || PlannedLiliesServed(Lily - 1) >= PlannedLiliesServed(Lily);
		}
	}

	private static int PlannedLiliesServed(int lilies)
	{
		var next = MathF.Max(0f, LilyTime);
		var served = 0;
		foreach (var at in _plannedLilyStarts)
		{
			while (next <= at)
			{
				lilies = Math.Min(3, lilies + 1);
				next += LilyIntervalSeconds;
			}

			if (lilies > 0)
			{
				lilies--;
				served++;
			}
		}

		return served;
	}

	private bool PlannedLilySoon =>
		Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedMiseryLeadSeconds, AfflatusRapturePvE.ID, AfflatusSolacePvE.ID);

	private bool PlannedAoeHealNear =>
		Updaters.BeirutaTimelineUpdater.PlannedAround(PlannedAoeHealGapSeconds, CureIiiPvE.ID, MedicaIiiPvE.ID, AfflatusRapturePvE.ID);

	private const float LilyCapSoonSeconds = 5f;

	private const int LilyRichCount = 2;

	private static bool LilyRich => Lily >= LilyRichCount;

	private static bool LilyCapping =>
		BloodLily < 3 &&
		(Lily >= 3 || (Lily == 2 && LilyAfter(LilyCapSoonSeconds)));

	private bool BurstLilyPrepWanted
	{
		get
		{
			if (!MiseryHoldActive || BloodLily >= 3 || Lily == 0)
			{
				return false;
			}

			if (KeepEmergencyLilyValue && Lily < 2 && BloodLily < 2)
			{
				return false;
			}

			var burstSoon =
				IsBurst &&
				!HasPresenceOfMind &&
				PresenceOfMindPvE.EnoughLevel &&
				PresenceOfMindPvE.Cooldown.WillHaveOneCharge(BurstLilyPrepSeconds);

			return burstSoon || (IsPartyBurst && BloodLily == 2);
		}
	}

	#endregion

	#region Healing Engine

	private bool CanRunHealEngine => BeirutaHeal.CanRun(HasHealingLockout);

	private static bool AccretionGroupHealBlocked => BeirutaHeal.GroupHealBlocked;

	private float EffectiveHpRatio(IBattleChara member) => BeirutaHeal.EffectiveHpRatio(this, member);

	private bool CountsAsInjured(IBattleChara member, float threshold) => BeirutaHeal.Injured(this, member, threshold);

	private int CountInjured(float range, float threshold) => BeirutaHeal.CountInjured(this, range, threshold);

	private bool AoeNeeded(float range, float threshold) => BeirutaHeal.AoeNeeded(this, range, threshold);

	private bool SpotHealOk(IBattleChara member) => SpotHealAllowed(member, AoeNeeded(PartyHealRange, RaptureHp), PartyHealRange);

	private bool AoeGcdNeeded(float range, float threshold) => BeirutaHeal.AoeGcdNeeded(this, range, threshold);

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) => BeirutaHeal.Threshold(this, potency, customWardOff, wardOffCap);

	float IBeirutaHealContext.HealBoost => HealBoost;

	float IBeirutaHealContext.LandingHotPotency(IBattleChara member) => LandingHotPotency(member);
	private static bool HoldAoeForCoHealer => BeirutaHeal.HoldAoe(PartyHealRange);

	private const float SingleHealRange = 30f;
	private const float PartyHealRange = 20f;
	private const float CureIiiRadius = 10f;
	private const int AoeInjuredCount = 3;
	private const float RapturePotency = 400f;
	private const float CureIiiPotency = 600f;
	private const float MedicaDirectPotency = 250f;
	private const float MedicaTickPotency = 175f;
	private const float MedicaHotCountedPotency = MedicaTickPotency * BeirutaHealModel.HotCreditTicks;
	private const float MedicaCountedPotency = MedicaDirectPotency + MedicaHotCountedPotency;
	private const float RaptureWardOffCap = 0.8f;
	private const float CureIiiWardOffCap = 0.7f;
	private const float TemperanceBoost = 1.2f;
	private const float BenisonCapTankHp = 0.9f;
	private readonly BeirutaRaidwide _raidwide = new();

	private static readonly StatusID[] RegenStatus =
	[
		StatusID.Regen,
		StatusID.Regen_897,
		StatusID.Regen_1330,
	];

	private static readonly StatusID[] MedicaHotStatus =
	[
		StatusID.MedicaIii,
		StatusID.MedicaIii_3986,
		StatusID.MedicaIi,
	];

	private float LandingHotPotency(IBattleChara member) =>
		MedicaHotPending &&
		IsHealableInRange(member, PartyHealRange) &&
		!member.HasStatus(false, MedicaHotStatus)
			? MedicaHotCountedPotency
			: 0f;

	private static bool MedicaHotPending => BeirutaHealWindow.GcdJustUsed(ActionID.MedicaIiiPvE, ActionID.MedicaIiPvE);

	private static float HealBoost => StatusHelper.PlayerHasStatus(true, StatusID.Temperance) ? TemperanceBoost : 1f;

	private float RaptureHp => AoeThreshold(RapturePotency, RaptureHeal, RaptureWardOffCap);

	private float CureIiiHp => AoeThreshold(CureIiiPotency, CureIIIHeal, CureIiiWardOffCap);

	private float MedicaHp =>
		WardActive
			? BeirutaHealModel.RoundToFive(BeirutaHealModel.FullUseHp(MedicaDirectPotency, HealBoost))
			: AoeThreshold(MedicaCountedPotency, MedicaHeal);

	private static bool AoeHealGcdPending =>
		BeirutaHealWindow.GcdJustUsed(ActionID.MedicaIiiPvE, ActionID.MedicaIiPvE, ActionID.MedicaPvE, ActionID.CureIiiPvE, ActionID.AfflatusRapturePvE);

	private static bool PartyHasMedicaHot
	{
		get
		{
			if (MedicaHotPending)
			{
				return true;
			}

			var total = 0;
			var withHot = 0;
			foreach (var member in PartyMembers)
			{
				if (!IsHealableInRange(member, PartyHealRange))
				{
					continue;
				}

				total++;
				if (member.HasStatus(true, MedicaHotStatus))
				{
					withHot++;
				}
			}
			return total > 0 && withHot * 2 >= total;
		}
	}

	private IBattleChara? FindCureIiiCenter(float threshold)
	{
		IBattleChara? best = null;
		var bestCount = WardActive ? 0 : AoeInjuredCount - 1;
		foreach (var center in PartyMembers)
		{
			if (!IsHealableInRange(center, SingleHealRange) ||
				!AccretionAreaSafe(center.Position, CureIiiRadius, CureIiiHealHp))
			{
				continue;
			}

			var count = 0;
			foreach (var member in PartyMembers)
			{
				if (member != null &&
					!member.IsDead &&
					!IsHealBlocked(member) &&
					System.Numerics.Vector3.Distance(center.Position, member.Position) <= CureIiiRadius &&
					(!WardActive || !IsTankMember(member)) &&
					CountsAsInjured(member, threshold))
				{
					count++;
				}
			}

			if (count > bestCount)
			{
				bestCount = count;
				best = center;
			}
		}
		return best;
	}

	private const float RaptureHealHp = RapturePotency * BeirutaHealModel.HpPerPotency;
	private const float MedicaHealHp = MedicaCountedPotency * BeirutaHealModel.HpPerPotency;
	private const float CureIiiHealHp = CureIiiPotency * BeirutaHealModel.HpPerPotency;
	private const float SolaceHealHp = 800f * BeirutaHealModel.HpPerPotency;
	private const float TetragrammatonHealHp = 700f * BeirutaHealModel.HpPerPotency;
	private const float RegenHealHp = 1500f * BeirutaHealModel.HpPerPotency;
	private const float BenedictionHealHp = float.MaxValue;

	private static bool AccretionTargetSafe(IBattleChara target, float healHp) =>
		!BeirutaAccretion.WouldFillOutOfOrder(PartyMembers, target, healHp);

	private static bool AccretionAreaSafe(System.Numerics.Vector3 center, float radius, float healHp) =>
		!AccretionGroupHealBlocked &&
		BeirutaAccretion.AreaSafe(PartyMembers, center, radius, healHp);

	private static bool RaptureSafe =>
		Player != null && AccretionAreaSafe(Player.Position, PartyHealRange, RaptureHealHp);

	private static bool MedicaSafe =>
		Player != null && AccretionAreaSafe(Player.Position, PartyHealRange, MedicaHealHp);

	private float CurrentTetragrammatonThreshold => TetragrammatonPvE.Cooldown.CurrentCharges >= 2
		? TetragrammatonSecondValue
		: TetragrammatonLastValue;

	private bool TetragrammatonReady =>
		TetragrammatonPvE.EnoughLevel &&
		TetragrammatonPvE.IsEnabled &&
		TetragrammatonPvE.Cooldown.HasOneCharge;

	#endregion

	#region Movement

	private bool CanUseMovementHeal =>
		!InDamageBurst &&
		!HasHealingLockout;

	private IBattleChara? MovementSolaceTarget() =>
		AccretionUp ? null : FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && HealHp(m) < MovementSolaceHp && AccretionTargetSafe(m, SolaceHealHp));

	private bool MovementRaptureUseful =>
		CanUseMovementHeal &&
		LilyAvailable(false) &&
		RaptureSafe &&
		CountInjured(PartyHealRange, MovementHealHp) >= AoeInjuredCount &&
		AfflatusRapturePvE.CanUse(out _);

	private bool MovementSolaceUseful =>
		CanUseMovementHeal &&
		LilyAvailable(false) &&
		AfflatusSolacePvE.EnoughLevel &&
		AfflatusSolacePvE.IsEnabled &&
		BloodLily < 3 &&
		MovementSolaceTarget() != null;

	private IBattleChara? MovementRegenTarget()
	{
		return AccretionUp ? null : FindLowest(SingleHealRange, static m =>
			SpotHealWorthy(m) &&
			HealHp(m) < MovementRegenTankHp &&
			!HasSingleTargetHot(m) &&
			AccretionTargetSafe(m, RegenHealHp));
	}

	private bool MovementRegenUseful =>
		CanUseMovementHeal &&
		RegenPvE.EnoughLevel &&
		RegenPvE.IsEnabled &&
		MovementRegenTarget() != null;

	private bool MovementMiseryUseful => (!MiseryHoldActive || MiseryDumpSafe) && OpenerTimingPassed && BloodLily >= 3 && AfflatusMiseryPvE.CanUse(out _, skipAoeCheck: true);

	private bool MovementDiaDue =>
		BeirutaMove.Sufficient &&
		CurrentTargetDiaMissingOrEnding(MovingDiaRefreshSeconds);

	private bool SwiftcastAvailableForMovement =>
		UseSwiftcastForMovement &&
		!HasSwift &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!BeirutaMove.RaiseSwiftHeld &&
		!Updaters.BeirutaTimelineUpdater.PlannedWithin(40f, SwiftcastPvE.ID);

	private bool SwiftcastReadyForMovement =>
		SwiftcastAvailableForMovement &&
		BeirutaMove.Sufficient &&
		BeirutaMove.PastLead;

	private bool HasCheaperMovementOption =>
		HasSacredSight ||
		MovementRaptureUseful ||
		MovementSolaceUseful ||
		MovementMiseryUseful ||
		MovementDiaDue;

	private bool ShouldSwiftcastForMovement(IAction nextGCD)
	{
		if (!SwiftcastReadyForMovement || IsLastAction(ActionID.SwiftcastPvE))
		{
			return false;
		}

		if (SwiftcastMovement == SwiftcastMovementPriority.HealOnly)
		{
			return !MergedStatus.HasFlag(AutoStatus.Raise) && CastAoeHealWanted;
		}

		if (IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return SwiftcastMovement == SwiftcastMovementPriority.First || !HasCheaperMovementOption;
	}

	private bool TryMovementGCD(out IAction? act)
	{
		act = null;

		if (!BeirutaMove.PastGrace || HasSwift || BeirutaMove.RaiseSwiftHeld)
		{
			return false;
		}

		if (SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement)
		{
			return false;
		}

		if (MovementRaptureUseful && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		if (MovementSolaceUseful && MovementSolaceTarget() is { } solaceTarget &&
			BeirutaTarget.TryUseOn(AfflatusSolacePvE, solaceTarget, out act))
		{
			return true;
		}

		if (MovementMiseryUseful && AfflatusMiseryPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (GlareIvPvE.CanUse(out act))
		{
			return true;
		}

		if (MovementDiaDue && CanUseCurrentDia(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (BeirutaMove.Sufficient &&
			!SwiftcastAvailableForMovement &&
			MovementRegenUseful && MovementRegenTarget() is { } regenTarget &&
			BeirutaTarget.TryUseOn(RegenPvE, regenTarget, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Ward Mode

	private bool _lastWardMode;

	protected override void UpdateInfo()
	{
		BeirutaWard.Track(this, WardMode, ref _lastWardMode);
		_raidwide.Track(InCombat, PartyMembers);
		base.UpdateInfo();
	}

	public override void OnTerritoryChanged()
	{
		if (WardMode)
		{
			BeirutaWard.SetWardMode(this, false);
		}

		base.OnTerritoryChanged();
	}

	#endregion

	#region Countdown Logic

	private readonly BeirutaCountdownDelay _countdownDelay = new();

	protected override IAction? CountDownAction(float remainTime)
	{
		if (WardMode)
		{
			BeirutaWard.SetWardMode(this, false);
		}

		if (!_countdownDelay.Passed(remainTime))
		{
			return null;
		}

		IAction? act;

		if (remainTime <= 2.1f &&
			UsePotion &&
			PotionTiming == BeirutaPotTiming.Opener &&
			UseBurstMedicine(out act))
		{
			return act;
		}

		if (remainTime < StonePvE.Info.CastTime + CountDownAhead &&
			StonePvE.CanUse(out act))
		{
			return act;
		}

		if (CountdownShield == CountdownShieldStrategy.TankRegenBenison &&
			BeirutaWard.StanceTank(PartyMembers, SingleHealRange) is { } tank)
		{
			if (remainTime is < 5.5f and > 4.5f &&
				!tank.HasStatus(true, RegenStatus) &&
				BeirutaTarget.TryUseOn(RegenPvE, tank, out act))
			{
				return act;
			}

			if (remainTime is < 5f and > 2f && TryBenisonOn(tank, out act))
			{
				return act;
			}
		}

		return base.CountDownAction(remainTime);
	}

	#endregion

	#region oGCD Logic

	private bool TryBenisonOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.DivineBenison, StatusID.DivineBenison_1404) &&
			BeirutaTarget.TryUseOn(DivineBenisonPvE, target, out act, usedUp: true);
	}

	private bool TryAquaveilOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.Aquaveil, StatusID.Aquaveil_3086) &&
			BeirutaTarget.TryUseOn(AquaveilPvE, target, out act);
	}

	private bool TryTankMitigation(IBattleChara? tank, out IAction? act)
	{
		act = null;

		if (tank == null || !IsTankMember(tank) || IsHealBlocked(tank) || !IsHealableInRange(tank, SingleHealRange))
		{
			return false;
		}

		return TryBenisonOn(tank, out act) || TryAquaveilOn(tank, out act);
	}

	private bool TryRaidwideBenison(out IAction? act)
	{
		act = null;
		var target = FindLowest(SingleHealRange, static m =>
			!IsTankMember(m) &&
			!m.HasStatus(false, StatusID.DivineBenison, StatusID.DivineBenison_1404));
		return target != null && TryBenisonOn(target, out act);
	}

	private bool TryTetragrammatonOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !HasSingleHealLockoutStatus(target) &&
			AccretionTargetSafe(target, TetragrammatonHealHp) &&
			BeirutaTarget.TryUseOn(TetragrammatonPvE, target, out act, usedUp: true);
	}

	private static bool HasSingleHealLockoutStatus(IBattleChara target) =>
		target.HasStatus(false, StatusID.LivingDead);

	private bool TryBenediction(out IAction? act)
	{
		act = null;

		if (BenedictionModeValue == BenedictionUsage.Off)
		{
			return false;
		}

		if (BenedictionModeValue == BenedictionUsage.CriticalAndWalkingDead)
		{
			var walkingDead = FindLowest(SingleHealRange, static m => m.HasStatus(false, StatusID.WalkingDead));
			if (walkingDead != null && BeirutaTarget.TryUseOn(BenedictionPvE, walkingDead, out act))
			{
				return true;
			}
		}

		var tankOnly = BenedictionTankOnlyValue;
		var threshold = BenedictionHealValue;
		var critical = FindLowest(SingleHealRange, m =>
			SpotHealWorthy(m) &&
			(!tankOnly || IsTankMember(m)) &&
			HealHp(m) < threshold &&
			AccretionTargetSafe(m, BenedictionHealHp) &&
			!HasFreshBigMitigation(m) &&
			!m.HasStatus(false, StatusID.WalkingDead));

		return critical != null && BeirutaTarget.TryUseOn(BenedictionPvE, critical, out act);
	}

	private bool TryThinAir(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (HasThinAir || IsLastAction(ActionID.ThinAirPvE) || IsLastAction() != IsLastGCD())
		{
			return false;
		}

		var raising = MergedStatus.HasFlag(AutoStatus.Raise) || nextGCD == RaisePvE;
		var useLastCharge =
			ThinAirLastChargeUsage == ThinAirUsageStrategy.UseAllCharges ||
			(ThinAirLastChargeUsage == ThinAirUsageStrategy.ReserveLastChargeForRaise && raising);

		var nextAction = nextGCD as IBaseAction;
		var expensive =
			raising ||
			(nextAction != null && nextAction.Info.MPNeed >= ThinAirNeed) ||
			nextGCD == MedicaIiiPvE ||
			nextGCD == CureIiiPvE;

		if (expensive && ThinAirPvE.CanUse(out act, usedUp: useLastCharge))
		{
			return true;
		}

		var capping =
			ThinAirPvE.Cooldown.MaxCharges >= 2 &&
			ThinAirPvE.Cooldown.WillHaveXCharges(ThinAirPvE.Cooldown.MaxCharges, 5f);

		return capping &&
			nextAction != null &&
			nextAction.Info.MPNeed > 0 &&
			ThinAirPvE.CanUse(out act);
	}

	#endregion

	#region Steps

	private const float TankbusterLeadSeconds = BeirutaIncoming.TankbusterLeadSeconds;

	public bool PlanAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public bool SpecialAbility(IAction nextGCD, out IAction? act) =>
		TryAccretionAbility(out act) ||
		(DoomTarget(SingleHealRange) is { } doomed && TryTetragrammatonOn(doomed, out act));

	public bool BurstAbility(IAction nextGCD, out IAction? act)
	{
		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		if (PresenceOfMindAllowed && PresenceOfMindPvE.CanUse(out act, skipTTKCheck: IsInHighEndDuty))
		{
			return true;
		}

		act = null;
		return InCombat && IsBurst && CombatTime > 6f && !AccretionGroupHealBlocked && AssizePvE.CanUse(out act, skipAoeCheck: true);
	}

	public bool ResourceAbility(IAction nextGCD, out IAction? act) => TryThinAir(nextGCD, out act);

	public bool AreaHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public bool SpotHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TopUpAllTarget(SingleHealRange) is { } topUp &&
			TryTetragrammatonOn(topUp, out act))
		{
			return true;
		}

		if (TryBenediction(out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m)) is { } holmgangTank &&
			TryTetragrammatonOn(holmgangTank, out act))
		{
			return true;
		}

		if (DivineBenisonPvE.EnoughLevel &&
			DivineBenisonPvE.Cooldown.CurrentCharges >= DivineBenisonPvE.Cooldown.MaxCharges &&
			DivineBenisonPvE.Cooldown.MaxCharges >= 2 &&
			(EnemyAggroTank ?? FindLowest(SingleHealRange, static m => IsTankMember(m))) is { } capTank &&
			!IsHealBlocked(capTank) &&
			HealHp(capTank) < BenisonCapTankHp &&
			SpotHealOk(capTank) &&
			TryBenisonOn(capTank, out act))
		{
			return true;
		}

		var threshold = CurrentTetragrammatonThreshold;
		return FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && EffectiveHpRatio(m) < threshold) is { } tetraTarget &&
			TryTetragrammatonOn(tetraTarget, out act);
	}

	public bool TankHelpAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (BeirutaIncoming.TankbusterWithin(TankbusterLeadSeconds) &&
			TryTankMitigation(
				FindLowest(SingleHealRange, static m => IsFreshMitigatingTank(m)) ??
				EnemyAggroTank ??
				FindLowest(SingleHealRange, static m => IsTankMember(m)), out act))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsFreshMitigatingTank(member) && TryTankMitigation(member, out act))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	public bool LowPriorityAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine || !_raidwide.Coming)
		{
			return false;
		}

		if (DivineBenisonPvE.Cooldown.CurrentCharges >= 2 && TryRaidwideBenison(out act))
		{
			return true;
		}

		return FindLowest(SingleHealRange, static m => !IsTankMember(m)) is { } veilTarget &&
			TryAquaveilOn(veilTarget, out act);
	}

	public bool PlanGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public bool SpecialGCD(out IAction? act)
	{
		act = null;
		if (!TetragrammatonReady &&
			DoomTarget(SingleHealRange) is { } doomed &&
			((LilyAvailable(true) && BeirutaTarget.TryUseOn(AfflatusSolacePvE, doomed, out act)) ||
			 BeirutaTarget.TryUseOn(CureIiPvE, doomed, out act)))
		{
			return true;
		}

		return BeirutaAccretion.FindTarget(PartyMembers) is { } target &&
			!HasSwift &&
			!BenedictionAvailable &&
			(BeirutaTarget.TryUseOn(AfflatusSolacePvE, target, out act) ||
			 BeirutaTarget.TryUseOn(CureIiPvE, target, out act));
	}

	public bool WardGCD(out IAction? act)
	{
		act = null;
		return WardActive && AoeHealGcdWanted(out var emergency) && TryAoeHealGCD(emergency, out act);
	}

	public bool AreaHealGCD(out IAction? act)
	{
		act = null;
		return !WardActive && AoeHealGcdWanted(out var emergency) && TryAoeHealGCD(emergency, out act);
	}

	public bool SpotHealGCD(out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		var rescue =
			FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && BeirutaCritical.ForGcd(m, CriticalHp) && AccretionTargetSafe(m, SolaceHealHp)) ??
			(TetragrammatonReady
				? null
				: FindLowest(SingleHealRange, m => IsTankMember(m) && SpotHealOk(m) && HealHp(m) < SolaceTankHealValue));

		if (rescue != null &&
			!HasSingleHealLockoutStatus(rescue) &&
			LilyAvailable(true) &&
			BeirutaTarget.TryUseOn(AfflatusSolacePvE, rescue, out act))
		{
			return true;
		}

		if (BurstLilyPrepWanted && PlanLilyAllowsSpend && RaptureSafe && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		return LilyCapping &&
			PlanLilyAllowsSpend &&
			(FindLowest(SingleHealRange, IsTankMember) ?? FindLowest(SingleHealRange, static m => AccretionTargetSafe(m, SolaceHealHp))) is { } capTarget &&
			!HasSingleHealLockoutStatus(capTarget) &&
			BeirutaTarget.TryUseOn(AfflatusSolacePvE, capTarget, out act);
	}

	#endregion

	#region Hooks

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act) =>
		BeirutaHealerFlow.Ability(this, nextGCD, out act) || base.EmergencyAbility(nextGCD, out act);

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act) =>
		LowPriorityAbility(nextGCD, out act) || base.GeneralAbility(nextGCD, out act);

	[RotationDesc(ActionID.PresenceOfMindPvE, ActionID.AssizePvE, ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat && BurstAbility(nextGCD, out act))
		{
			return true;
		}

		if (InCombat && ShouldSwiftcastForMovement(nextGCD) && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act) =>
		BeirutaHealerFlow.UrgentGcd(this, out act) || base.EmergencyGCD(nextGCD, out act);

	#endregion

	#region Accretion Top Up

	private bool BenedictionAvailable =>
		BenedictionPvE.EnoughLevel &&
		BenedictionPvE.IsEnabled &&
		!BenedictionPvE.Cooldown.IsCoolingDown;

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		var target = BeirutaAccretion.FindTarget(PartyMembers);
		if (target == null)
		{
			return false;
		}

		if (BeirutaTarget.TryUseOn(BenedictionPvE, target, out act))
		{
			return true;
		}

		if (!BenedictionAvailable &&
			BeirutaTarget.TryUseOn(TetragrammatonPvE, target, out act, usedUp: true))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region GCD Logic

	private bool CanCastHealSpell => !BeirutaMove.PastGrace || HasSwift;

	private bool TryAoeHealGCD(bool emergency, out IAction? act)
	{
		act = null;

		var lilyOk = LilyAvailable(emergency || WardActive || BeirutaMove.PastGrace) && (emergency || PlanLilyAllowsSpend) && RaptureSafe;
		var raptureNeeded = lilyOk && AoeGcdNeeded(PartyHealRange, RaptureHp);
		var preferLily = emergency || WardActive || BeirutaMove.PastGrace || BurstLilyPrepWanted || LilyCapping || LilyRich;

		if (preferLily && raptureNeeded && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		if (CanCastHealSpell)
		{
			if (!PartyHasMedicaHot && MedicaSafe && AoeGcdNeeded(PartyHealRange, MedicaHp))
			{
				if (MedicaIiiPvE.EnoughLevel ? MedicaIiiPvE.CanUse(out act) : MedicaIiPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (CureIiiPvE.EnoughLevel &&
				FindCureIiiCenter(CureIiiHp) is { } center &&
				BeirutaTarget.TryUseOn(CureIiiPvE, center, out act))
			{
				return true;
			}
		}

		if (raptureNeeded && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool AoeHealGcdWanted(out bool emergency)
	{
		emergency = false;

		if (!CanRunHealEngine ||
			(!WardActive && HoldAoeForCoHealer) ||
			AoeHealGcdPending)
		{
			return false;
		}

		emergency =
			AoeNeeded(PartyHealRange, 0f) ||
			(AnyCritical(PartyHealRange) && AoeNeeded(PartyHealRange, RaptureHp));
		if (!emergency && PlannedAoeHealNear)
		{
			return false;
		}

		var highestHp = MathF.Max(RaptureHp, MathF.Max(CureIiiHp, PartyHasMedicaHot ? 0f : MedicaHp));

		return emergency || (AoeGcdNeeded(PartyHealRange, highestHp) && (WardActive || !HasPresenceOfMind));
	}

	private bool CastAoeHealWanted =>
		AoeHealGcdWanted(out _) &&
		!(LilyAvailable(true) && PlanLilyAllowsSpend && RaptureSafe && AoeGcdNeeded(PartyHealRange, RaptureHp)) &&
		((!PartyHasMedicaHot && MedicaSafe && AoeGcdNeeded(PartyHealRange, MedicaHp)) ||
		 (CureIiiPvE.EnoughLevel && FindCureIiiCenter(CureIiiHp) != null));

	[RotationDesc(ActionID.RaisePvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		if (RaisePvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		act = null;

		if (HasThinAir && MergedStatus.HasFlag(AutoStatus.Raise))
		{
			return RaiseGCD(out act);
		}

		if (BeirutaMove.RaiseSwiftHeld)
		{
			return base.GeneralGCD(out act);
		}

		if (BeirutaHealerFlow.HealGcd(this, out act))
		{
			return true;
		}

		if (ShouldUseMisery() && AfflatusMiseryPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (ShouldUseGlareIv && GlareIvPvE.CanUse(out act))
		{
			return true;
		}

		if (HolyPvE.EnoughLevel)
		{
			if (HolyIiiPvE.EnoughLevel && HolyIiiPvE.CanUse(out act))
			{
				return true;
			}

			if (!HolyIiiPvE.EnoughLevel && HolyPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (InCombat &&
			InDamageBurst &&
			CurrentTargetDiaMissingOrEnding(BurstDiaRefreshSeconds) &&
			CanUseCurrentDia(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (CanUseCurrentDia(out act))
		{
			return true;
		}

		if (TryMovementGCD(out act))
		{
			return true;
		}

		if (GlareIiiPvE.EnoughLevel && GlareIiiPvE.CanUse(out act))
		{
			return true;
		}

		if (GlarePvE.EnoughLevel && !GlareIiiPvE.EnoughLevel && GlarePvE.CanUse(out act))
		{
			return true;
		}

		if (StoneIvPvE.EnoughLevel && !GlarePvE.EnoughLevel && StoneIvPvE.CanUse(out act))
		{
			return true;
		}

		if (StoneIiiPvE.EnoughLevel && !StoneIvPvE.EnoughLevel && StoneIiiPvE.CanUse(out act))
		{
			return true;
		}

		if (StoneIiPvE.EnoughLevel && !StoneIiiPvE.Info.EnoughLevelAndQuest() && StoneIiPvE.CanUse(out act))
		{
			return true;
		}

		if (!StoneIiPvE.EnoughLevel && StonePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion
}
