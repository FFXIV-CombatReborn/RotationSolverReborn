namespace RotationSolver.Rotations.Healer.WhiteMage;

[Rotation("BeirutaWHM", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate White Mage. Damage and healing are automatic; mitigation comes from your CD planner or your own presses. AutoBurst off is enough to hold burst, including Presence of Mind and Assize. Set Intercept to GCD only and leave ability intercept unticked, so your planner's oGCDs go straight through and any extra heal or shield GCD you press is queued. Questions? Ask @Beiruta on the RSR Discord. Hover each setting for details.")]
[SourceCode(Path = "main/Rotations/Healer/WhiteMage/BeirutaWHM.cs")]
[ExtraRotation]

public sealed class BeirutaWHM : WhiteMageRotation
{
	#region Config Options

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps everyone topped up.\n" +
		"- Afflatus Rapture (also spends the kept lily) > Medica III > Cure III as soon as 3+ are below the heal's full-use line (about 85-90%).\n" +
		"- Thresholds use real HP; no waiting for Presence of Mind or the co-healer.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Presence of Mind / Afflatus Misery", Tooltip =
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

	#region Damage

	[RotationConfig(CombatType.PvE, Name = "[Damage] Use potion")]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > First potion", Parent = nameof(UsePotion), Tooltip =
		"Opener: during the countdown. 2 minutes: the first potion waits for the 2-minute burst.")]
	public PotTiming PotionTiming { get; set; } = PotTiming.Opener;

	public enum PotTiming : byte
	{
		[Description("Opener (countdown)")]
		Opener,

		[Description("2 minutes")]
		TwoMinutes,
	}

	[RotationConfig(CombatType.PvE, Name = "[Damage] Hold Afflatus Misery for raid buffs (if a raid-buff job is present)", Tooltip =
		"Glare IV is also held until the shortest raid buff is about to end.\n" +
		"Misery is still dumped early when there is enough time and lilies to rebuild 3 Blood Lilies before the next burst, so it doesn't block lilies.")]
	public bool HoldMiseryForBurst { get; set; } = true;

	#endregion

	#region Thin Air

	[Range(0, 10000, ConfigUnitType.None, 100)]
	[RotationConfig(CombatType.PvE, Name = "[Thin Air] Minimum spell MP", Tooltip =
		"Thin Air goes on spells costing at least this much MP, on Raise, and before it would cap.")]
	public float ThinAirNeed { get; set; } = 1000;

	[RotationConfig(CombatType.PvE, Name = "[Thin Air] Last charge")]
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

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Tooltip =
		"Off: AoE thresholds come from each heal's potency, Temperance, the co-healer's smallest GCD heal and incoming HoT ticks, so heals are not wasted.\n" +
		"Off also means: one lily kept, Benediction off, Tetragrammaton below 70% (2 charges) / 60% (1 charge), Afflatus Solace on tanks below 50%.\n" +
		"On: the values below replace the Ward-off thresholds. Heal area = 3+ players below that HP, like RSR's own heal-area settings. Ward Mode always uses its own.")]
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
	public float TetragrammatonSecond { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Tetragrammaton 1 charge: below", Parent = nameof(UseCustomHPThreshold))]
	public float TetragrammatonLast { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Afflatus Solace (no Tetragrammaton): tank below", Parent = nameof(UseCustomHPThreshold))]
	public float SolaceTankHeal { get; set; } = 0.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Afflatus Rapture: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float RaptureHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Medica III: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float MedicaHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Cure III: heal area around the target (Ward off)", Parent = nameof(UseCustomHPThreshold))]
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
	private float TetragrammatonSecondValue => UseCustomHPThreshold ? TetragrammatonSecond : 0.7f;
	private float TetragrammatonLastValue => UseCustomHPThreshold ? TetragrammatonLast : 0.6f;
	private float SolaceTankHealValue => UseCustomHPThreshold ? SolaceTankHeal : 0.5f;

	#endregion

	#region DMU

	[RotationConfig(CombatType.PvE, Name = "[DMU] Experimental Accretion top-up", Tooltip =
		"Fills Accretion players in order: the healer first, then the DPS once Earth Resistance Down II is gone.\n" +
		"Waits until 3+ non-tanks other than the target are above 30%, and 3s after Accretion changes.\n" +
		"WHM: Benediction > Tetragrammaton, then Afflatus Solace > Cure II once Benediction is used.\n" +
		"Always active in DMU, even with this off: group heals stop once an Accretion player is above 50%, and no heal fills an Accretion player out of order.")]
	public bool AccretionTopUp { get; set; } = false;

	#endregion

	#endregion

	#region Helpers

	private const float BurstDiaRefreshSeconds = 11f;
	private const float MovingDiaRefreshSeconds = 11f;
	private const float MovementGraceSeconds = 0.3f;
	private const float MovementTimeThreshold = 1.0f;
	private const float MovementLeadSeconds = 0.5f;
	private const float MovementHealHp = 0.9f;
	private const float BurstLilyPrepSeconds = 15f;

	private bool MovingPastGrace =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementGraceSeconds;

	private bool HasSufficientMovement =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementTimeThreshold;

	private bool HasLiturgyOfTheBell => StatusHelper.PlayerHasStatus(true, StatusID.LiturgyOfTheBell);

	private bool HasSacredSight => SacredSightStacks > 0;

	private bool HasHealingLockout => HasLiturgyOfTheBell;

	private bool ShouldHoldRaiseSwift =>
		(HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
		MergedStatus.HasFlag(AutoStatus.Raise);

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

	private bool CanUseCurrentDia(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (DiaPvE.EnoughLevel &&
			DiaPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!DiaPvE.EnoughLevel && AeroIiPvE.EnoughLevel &&
			AeroIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!DiaPvE.EnoughLevel && !AeroIiPvE.EnoughLevel && AeroPvE.EnoughLevel &&
			AeroPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
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

	private static readonly StatusID[] TargetRaidDebuffStatus =
	[
		StatusID.ChainStratagem,
		StatusID.ChainStratagem_1406,
		StatusID.Dokumori,
		StatusID.Dokumori_4303,
	];

	private static bool IsPartyBurst
	{
		get
		{
			if (CurrentTarget != null && CurrentTarget.HasStatus(false, TargetRaidDebuffStatus))
			{
				return true;
			}

			if (PartyMembers == null)
			{
				return false;
			}

			foreach (var member in PartyMembers)
			{
				if (member?.StatusList == null)
				{
					continue;
				}

				foreach (var status in member.StatusList)
				{
					if (PartyRaidBuffStatusIds.Contains(status.StatusId))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

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
		UsePotion &&
		InCombat &&
		IsBurst &&
		!(PotionTiming == PotTiming.TwoMinutes && CombatElapsedLess(30f)) &&
		(HasPresenceOfMind ||
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

		if (TargetDying)
		{
			return true;
		}

		return MiseryHoldActive
			? IsPartyBurst || MiseryDumpSafe
			: MovingPastGrace || InDamageBurst;
	}

	private const uint SacredSightSpareGcds = 2;
	private const uint SacredSightWardSpareGcds = 2;

	private bool SacredSightExpiring =>
		HasSacredSight &&
		StatusHelper.PlayerWillStatusEndGCD(
			SacredSightStacks + SacredSightSpareGcds + (WardMode ? SacredSightWardSpareGcds : 0u),
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
			SacredSightStacks + SacredSightSpareGcds + (WardMode ? SacredSightWardSpareGcds : 0u),
			DataCenter.DefaultGCDRemain);

	private bool ShouldUseGlareIv => BurstEndingForGlareIv || SacredSightExpiring || MovingPastGrace || TargetDying;

	#endregion

	#region Lilies

	private bool LilyAvailable(bool emergency) =>
		Lily > (KeepEmergencyLilyValue && !emergency ? 1 : 0);

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
	private const float CriticalHp = 0.3f;
	private const float BenisonCapTankHp = 0.9f;
	private const float UndeadReleaseSeconds = 1f;
	private const float EarthlyDominanceGateSeconds = 3f;
	private const float FreshMitigationSeconds = 6f;
	private const float RampartDuration = 20f;
	private const float BigMitigationDuration = 15f;

	private readonly BeirutaRaidwide _raidwide = new();

	private static readonly StatusID[] FullInvulnStatus =
	[
		StatusID.HallowedGround,
		StatusID.HallowedGround_1302,
		StatusID.Superbolide,
	];

	private static readonly StatusID[] HolmgangStatus =
	[
		StatusID.Holmgang,
		StatusID.Holmgang_409,
	];

	private static readonly StatusID[] TankBigMitigationStatus =
	[
		StatusID.Rampart,
		StatusID.Rampart_1191,
		StatusID.Sentinel,
		StatusID.Guardian,
		StatusID.Vengeance,
		StatusID.Damnation,
		StatusID.ShadowWall,
		StatusID.ShadowedVigil,
		StatusID.Nebula,
		StatusID.GreatNebula,
	];

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

	private static readonly StatusID[] CoHealerMacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];

	private bool CanRunHealEngine =>
		Service.Config.AutoHeal &&
		InCombat &&
		!HasHealingLockout;

	private static bool IsTankMember(IBattleChara member) => member.IsJobCategory(JobRole.Tank);

	private static bool IsHealBlocked(IBattleChara member)
	{
		if (member.HasStatus(false, FullInvulnStatus) || member.HasStatus(false, StatusID.LivingDead))
		{
			return true;
		}

		return member.HasStatus(false, HolmgangStatus) &&
			member.StatusTime(false, HolmgangStatus) > UndeadReleaseSeconds;
	}

	private static bool IsJustOffHolmgang(IBattleChara member) =>
		member.HasStatus(false, HolmgangStatus) &&
		member.StatusTime(false, HolmgangStatus) <= UndeadReleaseSeconds;

	private static bool HasFreshBigMitigation(IBattleChara member)
	{
		foreach (var status in TankBigMitigationStatus)
		{
			if (!member.HasStatus(false, status))
			{
				continue;
			}

			var duration = status is StatusID.Rampart or StatusID.Rampart_1191 ? RampartDuration : BigMitigationDuration;
			if (duration - member.StatusTime(false, status) <= FreshMitigationSeconds)
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsFreshMitigatingTank(IBattleChara member) =>
		IsTankMember(member) &&
		!IsHealBlocked(member) &&
		HasFreshBigMitigation(member);

	private float LandingHotPotency(IBattleChara member) =>
		MedicaHotPending &&
		IsHealableInRange(member, PartyHealRange) &&
		!member.HasStatus(false, MedicaHotStatus)
			? MedicaHotCountedPotency
			: 0f;

	private float EffectiveHpRatio(IBattleChara member) =>
		BeirutaHealModel.EffectiveHpRatio(member, WardMode, LandingHotPotency(member));

	private bool CountsAsInjured(IBattleChara member, float threshold) =>
		member.GetHealthRatio() < BeirutaHealModel.AlwaysInjuredHp ||
		EffectiveHpRatio(member) < threshold;

	private static bool IsHealableInRange(IBattleChara? member, float range) =>
		member != null &&
		!member.IsDead &&
		member.DistanceToPlayer() <= range;

	private int CountInjured(float range, float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member) && CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	private static bool AnyCritical(float range)
	{
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member) && member.GetHealthRatio() < CriticalHp)
			{
				return true;
			}
		}
		return false;
	}

	private bool AoeNeeded(float range, float threshold) => CountInjured(range, threshold) >= AoeInjuredCount;

	private static bool CoHealerBigHealSoon
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member == null || member.IsDead)
				{
					continue;
				}

				if (member.HasStatus(false, StatusID.GiantDominance) ||
					member.HasStatus(false, CoHealerMacrocosmosStatus) ||
					(member.HasStatus(false, StatusID.EarthlyDominance) &&
					 member.StatusTime(false, StatusID.EarthlyDominance) < EarthlyDominanceGateSeconds))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool HoldAoeForCoHealer => CoHealerBigHealSoon && !AnyCritical(PartyHealRange);

	private static IBattleChara? FindLowest(float range, Func<IBattleChara, bool> filter)
	{
		IBattleChara? best = null;
		foreach (var member in PartyMembers)
		{
			if (!IsHealableInRange(member, range) || IsHealBlocked(member) || !filter(member))
			{
				continue;
			}

			if (best == null || member.GetHealthRatio() < best.GetHealthRatio())
			{
				best = member;
			}
		}
		return best;
	}

	private static bool MedicaHotPending => BeirutaHealWindow.GcdJustUsed(ActionID.MedicaIiiPvE, ActionID.MedicaIiPvE);

	private static float HealBoost => StatusHelper.PlayerHasStatus(true, StatusID.Temperance) ? TemperanceBoost : 1f;

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) =>
		BeirutaHealModel.Threshold(potency, HealBoost, WardMode, UseCustomHPThreshold, customWardOff, PartyMembers, wardOffCap);

	private float RaptureHp => AoeThreshold(RapturePotency, RaptureHeal, RaptureWardOffCap);

	private float CureIiiHp => AoeThreshold(CureIiiPotency, CureIIIHeal, CureIiiWardOffCap);

	private float MedicaHp =>
		WardMode
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

	private IBattleChara? EnemyAggroTank
	{
		get
		{
			var aggroId = CurrentTarget?.TargetObject?.GameObjectId;
			if (aggroId == null)
			{
				return null;
			}

			foreach (var member in PartyMembers)
			{
				if (member.GameObjectId == aggroId && IsTankMember(member))
				{
					return member;
				}
			}
			return null;
		}
	}

	private IBattleChara? FindCureIiiCenter(float threshold)
	{
		IBattleChara? best = null;
		var bestCount = AoeInjuredCount - 1;
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

	private static bool AccretionGroupHealBlocked => BeirutaAccretion.GroupHealBlocked(PartyMembers);

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
		FindLowest(SingleHealRange, static m => m.GetHealthRatio() < MovementHealHp && AccretionTargetSafe(m, SolaceHealHp));

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
		var tank = FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < 1f && !m.HasStatus(true, RegenStatus));
		return tank ?? FindLowest(SingleHealRange, static m =>
			m.GetHealthRatio() < MovementHealHp &&
			!m.HasStatus(true, RegenStatus) &&
			AccretionTargetSafe(m, RegenHealHp));
	}

	private bool MovementRegenUseful =>
		CanUseMovementHeal &&
		RegenPvE.EnoughLevel &&
		RegenPvE.IsEnabled &&
		MovementRegenTarget() != null;

	private bool MovementMiseryUseful => (!MiseryHoldActive || MiseryDumpSafe) && OpenerTimingPassed && BloodLily >= 3 && AfflatusMiseryPvE.CanUse(out _, skipAoeCheck: true);

	private bool MovementDiaDue =>
		HasSufficientMovement &&
		CurrentTargetDiaMissingOrEnding(MovingDiaRefreshSeconds);

	private bool SwiftcastAvailableForMovement =>
		UseSwiftcastForMovement &&
		!HasSwift &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!ShouldHoldRaiseSwift;

	private bool SwiftcastReadyForMovement =>
		SwiftcastAvailableForMovement &&
		HasSufficientMovement &&
		MovingTime > MovementTimeThreshold + MovementLeadSeconds;

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

		if (!MovingPastGrace || HasSwift || ShouldHoldRaiseSwift)
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
			BeirutaAccretion.TryUseOn(AfflatusSolacePvE, solaceTarget, out act))
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

		if (HasSufficientMovement &&
			!SwiftcastAvailableForMovement &&
			MovementRegenUseful && MovementRegenTarget() is { } regenTarget &&
			BeirutaAccretion.TryUseOn(RegenPvE, regenTarget, out act))
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

	private const long CountdownResponseDelayMs = 1_000;
	private long _countdownStartedAtMs;
	private long _countdownLastSeenMs;
	private float _countdownLastRemain;

	private bool CountdownDelayPassed(float remainTime)
	{
		var now = Environment.TickCount64;

		if (_countdownStartedAtMs == 0 ||
			now - _countdownLastSeenMs > 500 ||
			remainTime > _countdownLastRemain + 0.5f)
		{
			_countdownStartedAtMs = now;
		}

		_countdownLastSeenMs = now;
		_countdownLastRemain = remainTime;

		return now - _countdownStartedAtMs >= CountdownResponseDelayMs;
	}

	protected override IAction? CountDownAction(float remainTime)
	{
		if (WardMode)
		{
			BeirutaWard.SetWardMode(this, false);
		}

		if (!CountdownDelayPassed(remainTime))
		{
			return null;
		}

		IAction? act;

		if (remainTime <= 2.1f &&
			UsePotion &&
			PotionTiming == PotTiming.Opener &&
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
				BeirutaAccretion.TryUseOn(RegenPvE, tank, out act))
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
			BeirutaAccretion.TryUseOn(DivineBenisonPvE, target, out act, usedUp: true);
	}

	private bool TryAquaveilOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.Aquaveil, StatusID.Aquaveil_3086) &&
			BeirutaAccretion.TryUseOn(AquaveilPvE, target, out act);
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
			BeirutaAccretion.TryUseOn(TetragrammatonPvE, target, out act, usedUp: true);
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
			if (walkingDead != null && BeirutaAccretion.TryUseOn(BenedictionPvE, walkingDead, out act))
			{
				return true;
			}
		}

		var tankOnly = BenedictionTankOnlyValue;
		var threshold = BenedictionHealValue;
		var critical = FindLowest(SingleHealRange, m =>
			(!tankOnly || IsTankMember(m)) &&
			m.GetHealthRatio() < threshold &&
			AccretionTargetSafe(m, BenedictionHealHp) &&
			!HasFreshBigMitigation(m) &&
			!m.HasStatus(false, StatusID.WalkingDead));

		return critical != null && BeirutaAccretion.TryUseOn(BenedictionPvE, critical, out act);
	}

	private bool TryHealAbility(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TryBenediction(out act))
		{
			return true;
		}

		var holmgangTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffHolmgang(m));
		if (holmgangTank != null && TryTetragrammatonOn(holmgangTank, out act))
		{
			return true;
		}

		if (_raidwide.Coming && TryRaidwideBenison(out act))
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

		if (DivineBenisonPvE.EnoughLevel &&
			DivineBenisonPvE.Cooldown.CurrentCharges >= DivineBenisonPvE.Cooldown.MaxCharges &&
			DivineBenisonPvE.Cooldown.MaxCharges >= 2)
		{
			var capTank = EnemyAggroTank ?? FindLowest(SingleHealRange, static m => IsTankMember(m));
			if (capTank != null &&
				!IsHealBlocked(capTank) &&
				capTank.GetHealthRatio() < BenisonCapTankHp &&
				TryBenisonOn(capTank, out act))
			{
				return true;
			}
		}

		var threshold = CurrentTetragrammatonThreshold;
		var tetraTank = FindLowest(SingleHealRange, m => IsTankMember(m) && EffectiveHpRatio(m) < threshold);
		if (tetraTank != null && TryTetragrammatonOn(tetraTank, out act))
		{
			return true;
		}

		if (!AoeNeeded(PartyHealRange, RaptureHp))
		{
			var topUp = FindLowest(SingleHealRange, m => EffectiveHpRatio(m) < threshold && AccretionTargetSafe(m, TetragrammatonHealHp));
			if (topUp != null && TryTetragrammatonOn(topUp, out act))
			{
				return true;
			}
		}

		if (NonTankTopUp is { } nonTank && TryTetragrammatonOn(nonTank, out act))
		{
			return true;
		}

		act = null;
		return false;
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

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (TryAccretionAbility(out act))
		{
			return true;
		}

		if (TryThinAir(nextGCD, out act))
		{
			return true;
		}

		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		if (PresenceOfMindAllowed && PresenceOfMindPvE.CanUse(out act, skipTTKCheck: IsInHighEndDuty))
		{
			return true;
		}

		if (TryHealAbility(out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.DivineBenisonPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (TryRaidwideBenison(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.DivineBenisonPvE, ActionID.AquaveilPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		var tank =
			FindLowest(SingleHealRange, static m => IsFreshMitigatingTank(m)) ??
			EnemyAggroTank ??
			FindLowest(SingleHealRange, static m => IsTankMember(m));

		if (TryTankMitigation(tank, out act))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.PresenceOfMindPvE, ActionID.AssizePvE, ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat)
		{
			if (PresenceOfMindAllowed && PresenceOfMindPvE.CanUse(out act, skipTTKCheck: IsInHighEndDuty))
			{
				return true;
			}

			if (IsBurst && CombatTime > 6f && !AccretionGroupHealBlocked && AssizePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (ShouldSwiftcastForMovement(nextGCD) &&
				SwiftcastPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}

	#endregion

	#region Accretion Top Up

	private bool BenedictionAvailable =>
		BenedictionPvE.EnoughLevel &&
		BenedictionPvE.IsEnabled &&
		!BenedictionPvE.Cooldown.IsCoolingDown;

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target == null)
		{
			return false;
		}

		if (BeirutaAccretion.TryUseOn(BenedictionPvE, target, out act))
		{
			return true;
		}

		if (!BenedictionAvailable &&
			BeirutaAccretion.TryUseOn(TetragrammatonPvE, target, out act, usedUp: true))
		{
			return true;
		}

		act = null;
		return false;
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target != null && !HasSwift && !BenedictionAvailable)
		{
			if (BeirutaAccretion.TryUseOn(AfflatusSolacePvE, target, out act) ||
				BeirutaAccretion.TryUseOn(CureIiPvE, target, out act))
			{
				return true;
			}
		}

		return base.EmergencyGCD(nextGCD, out act);
	}

	#endregion

	#region GCD Logic

	private bool CanCastHealSpell => !MovingPastGrace || HasSwift;

	private bool TryAoeHealGCD(bool emergency, out IAction? act)
	{
		act = null;

		var lilyOk = LilyAvailable(emergency || WardMode || MovingPastGrace) && RaptureSafe;
		var raptureNeeded = lilyOk && AoeNeeded(PartyHealRange, RaptureHp);
		var preferLily = emergency || WardMode || MovingPastGrace || BurstLilyPrepWanted || LilyCapping || LilyRich;

		if (preferLily && raptureNeeded && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		if (CanCastHealSpell)
		{
			if (!PartyHasMedicaHot && MedicaSafe && AoeNeeded(PartyHealRange, MedicaHp))
			{
				if (MedicaIiiPvE.EnoughLevel ? MedicaIiiPvE.CanUse(out act) : MedicaIiPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (CureIiiPvE.EnoughLevel &&
				FindCureIiiCenter(CureIiiHp) is { } center &&
				BeirutaAccretion.TryUseOn(CureIiiPvE, center, out act))
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
			(!WardMode && HoldAoeForCoHealer) ||
			AoeHealGcdPending)
		{
			return false;
		}

		emergency =
			AoeNeeded(PartyHealRange, 0f) ||
			(AnyCritical(PartyHealRange) && AoeNeeded(PartyHealRange, RaptureHp));
		var highestHp = MathF.Max(RaptureHp, MathF.Max(CureIiiHp, PartyHasMedicaHot ? 0f : MedicaHp));

		return emergency || (AoeNeeded(PartyHealRange, highestHp) && (WardMode || !HasPresenceOfMind));
	}

	private bool CastAoeHealWanted =>
		AoeHealGcdWanted(out _) &&
		!(LilyAvailable(true) && RaptureSafe && AoeNeeded(PartyHealRange, RaptureHp)) &&
		((!PartyHasMedicaHot && MedicaSafe && AoeNeeded(PartyHealRange, MedicaHp)) ||
		 (CureIiiPvE.EnoughLevel && FindCureIiiCenter(CureIiiHp) != null));

	private IBattleChara? NonTankTopUp =>
		WardMode && BeirutaWard.NonTanksShielded(PartyMembers, SingleHealRange)
			? BeirutaHealModel.NonTankTopUpTarget(PartyMembers, SingleHealRange, EffectiveHpRatio, static m => IsHealBlocked(m) || HasSingleHealLockoutStatus(m), AoeInjuredCount)
			: null;
	private bool TryHealGCD(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		var rescue =
			FindLowest(SingleHealRange, static m => BeirutaCritical.ForGcd(m, CriticalHp) && AccretionTargetSafe(m, SolaceHealHp)) ??
			(TetragrammatonReady
				? null
				: FindLowest(SingleHealRange, m => IsTankMember(m) && m.GetHealthRatio() < SolaceTankHealValue));

		if (rescue != null &&
			!HasSingleHealLockoutStatus(rescue) &&
			LilyAvailable(true) &&
			BeirutaAccretion.TryUseOn(AfflatusSolacePvE, rescue, out act))
		{
			return true;
		}

		if (AoeHealGcdWanted(out var aoeEmergency) && TryAoeHealGCD(aoeEmergency, out act))
		{
			return true;
		}

		if (NonTankTopUp is { } topUp &&
			!TetragrammatonReady &&
			CanCastHealSpell &&
			BeirutaAccretion.TryUseOn(CureIiPvE, topUp, out act))
		{
			return true;
		}

		if (BurstLilyPrepWanted && RaptureSafe && AfflatusRapturePvE.CanUse(out act))
		{
			return true;
		}

		if (LilyCapping &&
			MovementSolaceTarget() is { } capTarget &&
			!HasSingleHealLockoutStatus(capTarget) &&
			BeirutaAccretion.TryUseOn(AfflatusSolacePvE, capTarget, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

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

		if (ShouldHoldRaiseSwift)
		{
			return base.GeneralGCD(out act);
		}

		if (TryHealGCD(out act))
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
