namespace RotationSolver.Rotations.Healer.Sage;

[Rotation("BeirutaSGE", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Sage. Damage and healing are automatic; mitigation comes from your CD planner or your own presses. AutoBurst off is enough to hold burst. Set Intercept to GCD only and leave ability intercept unticked, so your planner's oGCDs go straight through and any extra heal or shield GCD you press is queued. Questions? Ask @Beiruta on the RSR Discord. Hover each setting for details.")]
[SourceCode(Path = "main/Rotations/Healer/Sage/BeirutaSGE.cs")]
[ExtraRotation]

public sealed class BeirutaSGE : SageRotation
{
	#region Config Options

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps the party shielded and heals sooner.\n" +
		"- Eukrasian Prognosis whenever 4+ are unshielded.\n" +
		"- AoE thresholds use real HP and ignore the co-healer, HoTs and Eudaimonia.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Pre-pull GCD", Tooltip =
		"Pneuma lands as the countdown ends. Toxikon is sent about 1s earlier, since Eukrasia can't be precast and Toxikon lands late.")]
	public OpenerStrategy OpenerSelection { get; set; } = OpenerStrategy.PneumaOpener;

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Tooltip =
		"What SGE puts up during the countdown. Kardia is placed between 15s and 9s.")]
	public CountdownShieldStrategy CountdownShield { get; set; } = CountdownShieldStrategy.ZoeEukrasianPrognosis;

	public enum OpenerStrategy : byte
	{
		[Description("Toxikon")]
		ToxikonOpener,

		[Description("Pneuma")]
		PneumaOpener,
	}

	public enum CountdownShieldStrategy : byte
	{
		[Description("None")]
		None,

		[Description("Eukrasian Prognosis")]
		EukrasianPrognosis,

		[Description("Zoe + Eukrasian Prognosis")]
		ZoeEukrasianPrognosis,

		[Description("Eukrasian Diagnosis on Kardion tank")]
		EukrasianDiagnosisKardionTank,

		[Description("Eukrasian Diagnosis on both tanks")]
		EukrasianDiagnosisBothTanks,
	}

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Phlegma / Psyche", Tooltip =
		"Holds Phlegma and Psyche until this long after the pull. Psyche also needs AutoBurst.")]
	public PhlegmaOpenerTiming PhlegmaOpener { get; set; } = PhlegmaOpenerTiming.ThreeSeconds;

	public enum PhlegmaOpenerTiming : byte
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
		"Opener: during the countdown. 2 minutes: the first potion waits for the 2-minute burst.\n" +
		"In combat it follows raid buffs or party potions, with a Phlegma charge and Psyche ready within 30s.")]
	public PotTiming PotionTiming { get; set; } = PotTiming.Opener;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Phlegma / Toxikon on 2+ enemies")]
	public bool MultiTargetPhlegmaDump { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Toxikon at 3 Addersting while your shields are up", Tooltip =
		"Breaking Eukrasian shields gives Addersting, so Toxikon is spent at 3 to avoid overcapping.")]
	public bool KeepTwoAddersting { get; set; } = true;

	public enum PotTiming : byte
	{
		[Description("Opener (countdown)")]
		Opener,

		[Description("2 minutes")]
		TwoMinutes,
	}

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Eukrasian Dosis refresh", Tooltip =
		"While moving, refreshes Eukrasian Dosis early as an instant GCD.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Tooltip =
		"Swiftcast while moving. Raise always takes priority.\n" +
		"Instant options: Toxikon > spare Phlegma > Eukrasian Prognosis (not in raid buffs) > early Eukrasian Dosis.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > When to use it", Parent = nameof(UseSwiftcastForMovement), Tooltip =
		"Last resort: only when no instant option is available. First: before the instant options.")]
	public SwiftcastMovementPriority SwiftcastMovement { get; set; } = SwiftcastMovementPriority.LastResort;

	public enum SwiftcastMovementPriority : byte
	{
		[Description("Last resort (keep for raise)")]
		LastResort = 0,

		[Description("First (e.g. death = enrage)")]
		First = 2,
	}

	#endregion

	#region Healing

	public enum TaurocholeUsage : byte
	{
		[Description("Off")]
		Off,

		[Description("Tanks only")]
		TanksOnly,

		[Description("Anyone")]
		Anyone,
	}

	public enum KardiaSwapMode : byte
	{
		[Description("None")]
		None,

		[Description("Tanks only")]
		TanksOnly,

		[Description("Everyone")]
		Everyone,
	}

	public enum ZoeUsage : byte
	{
		[Description("Auto before healing Pneuma")]
		AutoPneuma,

		[Description("Only after you press Zoe")]
		SignalOnly,
	}

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Tooltip =
		"Off: AoE thresholds come from each heal's potency, the co-healer's smallest GCD heal, incoming HoT ticks and Eudaimonia, so heals are not wasted.\n" +
		"Off also means: Taurochole on tanks, Zoe before a healing Pneuma, no heals during Macrocosmos, Eukrasian Prognosis while moving, no Kardia swap.\n" +
		"On: the values below replace the Ward-off thresholds. Heal area = 3+ players below that HP, like RSR's own heal-area settings. Ward Mode always uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Taurochole targets", Parent = nameof(UseCustomHPThreshold))]
	public TaurocholeUsage TaurocholeMode { get; set; } = TaurocholeUsage.TanksOnly;

	[RotationConfig(CombatType.PvE, Name = "    > Zoe usage", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Auto: Zoe right before a healing Pneuma.\n" +
		"Only after you press Zoe: the rotation follows with Pneuma or Eukrasian Prognosis by party HP.")]
	public ZoeUsage ZoeMode { get; set; } = ZoeUsage.AutoPneuma;

	[RotationConfig(CombatType.PvE, Name = "    > After your Zoe: Pepsis old shields, then reapply", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Needs Zoe usage = Only after you press Zoe. If 3+ already have your Eukrasian Prognosis shield, Pepsis turns them into healing first, then the boosted shield goes back up.")]
	public bool ZoePepsisReshield { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Lock healing during Macrocosmos", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"No heals while you have an Astrologian's Macrocosmos, since it heals the damage taken when it ends.")]
	public bool LockHealingActionsDuringMacrocosmos { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Eukrasian Prognosis as a movement GCD", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"While moving, Eukrasian Prognosis counts as an instant GCD, except in raid buffs.")]
	public bool MovementShieldHealing { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Kardia swap", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Moves Kardia to an injured player and back to the tank afterwards.")]
	public KardiaSwapMode KardiaSwap { get; set; } = KardiaSwapMode.None;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Physis II: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float PhysisHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Ixochole: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float IxocholeHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Pneuma: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float PneumaHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Pepsis: heal area, shielded players only", Parent = nameof(UseCustomHPThreshold))]
	public float PepsisHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Soteria: Kardion target below", Parent = nameof(UseCustomHPThreshold))]
	public float SoteriaHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Taurochole: below", Parent = nameof(UseCustomHPThreshold))]
	public float HealSingleTaurocholeHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Druochole: tank below", Parent = nameof(UseCustomHPThreshold))]
	public float HealSingleDruocholeHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > After your Zoe: Pneuma if party average below (else Eukrasian Prognosis)", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"With Zoe usage = Only after you press Zoe: party average HP below this uses Pneuma, otherwise Eukrasian Prognosis.")]
	public float ZoePneumaHeal { get; set; } = 0.40f;

	private TaurocholeUsage TaurocholeModeValue => UseCustomHPThreshold ? TaurocholeMode : TaurocholeUsage.TanksOnly;
	private ZoeUsage ZoeModeValue => UseCustomHPThreshold ? ZoeMode : ZoeUsage.AutoPneuma;
	private bool ZoePepsisReshieldValue => UseCustomHPThreshold && ZoePepsisReshield;
	private bool MacrocosmosLockValue => !UseCustomHPThreshold || LockHealingActionsDuringMacrocosmos;
	private bool MovementShieldHealingValue => !UseCustomHPThreshold || MovementShieldHealing;
	private KardiaSwapMode KardiaSwapValue => UseCustomHPThreshold ? KardiaSwap : KardiaSwapMode.None;
	private float PepsisHealValue => UseCustomHPThreshold ? PepsisHeal : 0.6f;
	private float SoteriaHealValue => UseCustomHPThreshold ? SoteriaHeal : 0.8f;
	private float HealSingleTaurocholeHealValue => UseCustomHPThreshold ? HealSingleTaurocholeHeal : 0.7f;
	private float HealSingleDruocholeHealValue => UseCustomHPThreshold ? HealSingleDruocholeHeal : 0.8f;
	private float ZoePneumaHealValue => UseCustomHPThreshold ? ZoePneumaHeal : 0.40f;

	#endregion

	#region DMU

	[RotationConfig(CombatType.PvE, Name = "[DMU] Experimental Accretion top-up", Tooltip =
		"Fills Accretion players in order: the healer first, then the DPS once Earth Resistance Down II is gone.\n" +
		"Waits until 3+ non-tanks other than the target are above 30%, and 3s after Accretion changes.\n" +
		"SGE: Krasis > Druochole > Taurochole, then Diagnosis.\n" +
		"Always active in DMU, even with this off: group heals stop once an Accretion player is above 50%, and the party is kept shielded as in Ward Mode.")]
	public bool AccretionTopUp { get; set; } = false;

	#endregion

	#endregion

	#region Constants / Fields

	private const float EarlyDotRefreshSeconds = 12f;
	private const float PhlegmaDumpTTKSeconds = 10f;
	private const float DotRefreshTTKSeconds = 15f;
	private const float MovementLeadSeconds = 0.5f;
	private const float MovementGraceSeconds = 0.3f;
	private const float MovementTimeThreshold = 1.0f;
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
		(uint)StatusID.ArcaneCircle
	];

	private static bool PartyHasStatus(Func<uint, bool> match)
	{
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
				if (match(status.StatusId))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool PartyHasOwnEukrasianShield
	{
		get
		{
			if (PartyMembers == null)
			{
				return false;
			}

			foreach (var member in PartyMembers)
			{
				if (member != null &&
					!member.IsDead &&
					(member.HasStatus(true, StatusID.EukrasianDiagnosis) || member.HasStatus(true, StatusID.EukrasianPrognosis)))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool IsPartyMedicated => PartyHasStatus(static id => id == (uint)StatusID.Medicated);

	private static bool IsPartyBurst => PartyHasStatus(static id => PartyRaidBuffStatusIds.Contains(id));

	private float PhlegmaOpenerSeconds => PhlegmaOpener switch
	{
		PhlegmaOpenerTiming.ZeroSeconds => 0f,
		PhlegmaOpenerTiming.SixSeconds => 6f,
		_ => 3f,
	};

	private bool PhlegmaOpenerAllowed => InCombat && !CombatElapsedLess(PhlegmaOpenerSeconds);

	#endregion

	#region Tracking Properties

	private IBaseAction? _lastEukrasiaActionAim;
	private IBaseAction? _EukrasiaActionAim;

	private bool HasKerachole => StatusHelper.PlayerHasStatus(true, StatusID.Kerachole);
	private bool HasZoe => StatusHelper.PlayerHasStatus(true, StatusID.Zoe);
	private static bool HasMacrocosmos => StatusHelper.PlayerHasStatus(false, CoHealerMacrocosmosStatus);
	private bool HasEukrasianPrognosis => StatusHelper.PlayerHasStatus(true, StatusID.EukrasianPrognosis);
	private bool HasMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

	private static bool HasPhilosophia => StatusHelper.PlayerHasStatus(true, StatusID.Philosophia);

	private bool IsPhlegmaAoeAtLeast(int threshold)
	{
		if (!MultiTargetPhlegmaDump || CurrentTarget == null || AllHostileTargets == null)
		{
			return false;
		}

		var count = 0;
		foreach (var enemy in AllHostileTargets)
		{
			if (enemy != null &&
				enemy.IsAttackable() &&
				Vector3.Distance(CurrentTarget.Position, enemy.Position) < (5f + enemy.HitboxRadius))
			{
				count++;
			}
		}
		return count >= threshold;
	}

	private bool MovingPastGrace =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementGraceSeconds;

	private bool HasSufficientMovement =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementTimeThreshold;

	private const float MovementShieldHealHp = 0.95f;

	private static bool HasEukrasianShield(IBattleChara? member) =>
		member != null &&
		(member.HasStatus(false, StatusID.EukrasianPrognosis) ||
		 member.HasStatus(false, StatusID.Galvanize));

	private static bool HasSingleShield(IBattleChara? member) =>
		HasEukrasianShield(member) ||
		(member != null && member.HasStatus(false, StatusID.EukrasianDiagnosis));

	private const float PrognosisRange = 20f;

	private static bool MostPartyLacksEukrasianShield()
	{
		var inRange = BeirutaWard.CountMissing(PartyMembers, PrognosisRange, static _ => false);
		var unshielded = BeirutaWard.CountMissing(PartyMembers, PrognosisRange, static m => HasEukrasianShield(m));
		return inRange > 0 && unshielded * 2 >= inRange;
	}


	private bool MovementPrognosisUseful(out IBaseAction? prognosis)
	{
		prognosis = null;

		if (!MovementShieldHealingValue ||
			IsPartyBurst ||
			HasPhilosophia ||
			HasHealingLockout ||
			PartyMembersAverHP >= MovementShieldHealHp ||
			BeirutaWard.CountMissing(PartyMembers, PrognosisRange, static m => !HasEukrasianShield(m)) > 0)
		{
			return false;
		}

		if (EukrasianPrognosisIiPvE.EnoughLevel && EukrasianPrognosisIiPvE.IsEnabled)
		{
			prognosis = EukrasianPrognosisIiPvE;
		}
		else if (EukrasianPrognosisPvE.EnoughLevel && EukrasianPrognosisPvE.IsEnabled)
		{
			prognosis = EukrasianPrognosisPvE;
		}

		return prognosis != null && prognosis.CanUse(out _);
	}

	private bool PhlegmaSpareChargeAvailable =>
		IsBurst &&
		PhlegmaOpenerAllowed &&
		PhlegmaPvE.EnoughLevel &&
		PhlegmaPvE.Cooldown.CurrentCharges > 0;

	private bool MovementDotRefreshDue =>
		EarlyDotRefreshWhenMoving &&
		!ShouldSkipDotForTTK &&
		CurrentTarget != null &&
		CurrentTargetEukrasianDosisMissingOrEnding(EarlyDotRefreshSeconds);

	private bool SetEukrasianDosisAim()
	{
		_EukrasiaActionAim = null;

		if (EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiiPvE);
			return true;
		}

		if (EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisIiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiPvE);
			return true;
		}

		if (EukrasianDosisPvE.EnoughLevel && EukrasianDosisPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisPvE);
			return true;
		}

		return false;
	}

	private bool DoAimedEukrasianDosis(out IAction? act)
	{
		if (DoEukrasianDosisIii(out act) || DoEukrasianDosisIi(out act) || DoEukrasianDosis(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryMovementGCD(out IAction? act)
	{
		act = null;

		if (!MovingPastGrace || HasEukrasia || HasSwift || ShouldDeferToRaise())
		{
			return false;
		}

		if (SwiftcastOutranksMovementGCDs)
		{
			return false;
		}

		if (ToxikonPvE.CanUse(out act))
		{
			return true;
		}

		if (PhlegmaSpareChargeAvailable && PhlegmaPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (MovementPrognosisUseful(out var prognosis) && prognosis != null)
		{
			_EukrasiaActionAim = null;
			SetEukrasia(prognosis);
			if (prognosis == EukrasianPrognosisIiPvE ? DoEukrasianPrognosisIi(out act) : DoEukrasianPrognosis(out act))
			{
				return true;
			}
		}

		if (HasSufficientMovement && MovementDotRefreshDue && SetEukrasianDosisAim() && DoAimedEukrasianDosis(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool HasCheaperMovementOption =>
		HasEukrasia ||
		Addersting > 0 ||
		PhlegmaSpareChargeAvailable ||
		MovementPrognosisUseful(out _) ||
		MovementDotRefreshDue;

	private bool HasHealingLockout =>
		MacrocosmosLockValue &&
		HasMacrocosmos;

	private bool ShouldDeferToRaise() =>
		(HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
		MergedStatus.HasFlag(AutoStatus.Raise);

	private static bool HasSingleHealLockoutStatus(IBattleChara? target)
	{
		if (target == null)
		{
			return true;
		}

		try
		{
			return target.HasStatus(false, StatusID.LivingDead) ||
				   target.HasStatus(false, StatusID.Holmgang) ||
				   target.HasStatus(false, StatusID.WalkingDead);
		}
		catch
		{
			return true;
		}
	}

	private bool IsMovementPreferredNextGCD(IAction nextGCD)
	{
		if (nextGCD == null)
		{
			return false;
		}

		return nextGCD.IsTheSameTo(false,
			EukrasiaPvE,
			PhlegmaPvE,
			ToxikonPvE,
			ToxikonIiPvE,
			DyskrasiaPvE,
			DyskrasiaIiPvE,
			EsunaPvE,
			EukrasianDiagnosisPvE,
			EukrasianPrognosisPvE,
			EukrasianPrognosisIiPvE,
			EukrasianDyskrasiaPvE,
			EukrasianDosisPvE,
			EukrasianDosisIiPvE,
			EukrasianDosisIiiPvE);
	}

	private bool ShouldSwiftcastForMovement(IAction nextGCD)
	{
		if (!UseSwiftcastForMovement ||
			!HasSufficientMovement ||
			MovingTime <= MovementTimeThreshold + MovementLeadSeconds ||
			HasSwift ||
			IsLastAction(ActionID.SwiftcastPvE) ||
			ShouldDeferToRaise())
		{
			return false;
		}

		if (IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return SwiftcastMovement == SwiftcastMovementPriority.First
			? !HasEukrasia
			: !HasCheaperMovementOption;
	}

	private bool SwiftcastReadyForMovement =>
		UseSwiftcastForMovement &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		HasSufficientMovement &&
		MovingTime > MovementTimeThreshold + MovementLeadSeconds &&
		!ShouldDeferToRaise();

	private bool SwiftcastOutranksMovementGCDs =>
		SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement;

	private bool CanUseCountdownEukrasianPrognosis(out IAction? act)
	{
		act = null;

		if (!MostPartyLacksEukrasianShield())
		{
			return false;
		}

		if (EukrasianPrognosisIiPvE.EnoughLevel &&
			EukrasianPrognosisIiPvE.IsEnabled &&
			EukrasianPrognosisIiPvE.CanUse(out act))
		{
			return true;
		}

		if (EukrasianPrognosisPvE.EnoughLevel &&
			EukrasianPrognosisPvE.IsEnabled &&
			EukrasianPrognosisPvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	private static bool CanReceiveEukrasianDiagnosis(IBattleChara? member)
		=> member != null
			&& !member.IsDead
			&& !HasSingleShield(member);

	private IBattleChara? GetCountdownDiagnosisTarget()
	{
		if (PartyMembers == null)
		{
			return null;
		}

		if (CountdownShield == CountdownShieldStrategy.EukrasianDiagnosisKardionTank)
		{
			foreach (var member in PartyMembers)
			{
				if (member.HasStatus(true, StatusID.Kardion))
				{
					return CanReceiveEukrasianDiagnosis(member) ? member : null;
				}
			}
			return null;
		}

		if (CountdownShield == CountdownShieldStrategy.EukrasianDiagnosisBothTanks)
		{
			IBattleChara? fallback = null;
			foreach (var tank in PartyMembers.GetJobCategory(JobRole.Tank))
			{
				if (!CanReceiveEukrasianDiagnosis(tank))
				{
					continue;
				}

				if (tank.HasStatus(true, StatusID.Kardion))
				{
					return tank;
				}

				fallback ??= tank;
			}
			return fallback;
		}

		return null;
	}

	private bool CanUseCountdownEukrasianDiagnosis(out IAction? act)
	{
		act = null;

		var target = GetCountdownDiagnosisTarget();
		if (target == null || !EukrasianDiagnosisPvE.EnoughLevel || !EukrasianDiagnosisPvE.IsEnabled)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		if (EukrasianDiagnosisPvE.CanUse(out act, skipStatusProvideCheck: true, targetOverride: TargetType.Tank))
		{
			EukrasianDiagnosisPvE.Target = new TargetResult(target, [target], target.Position);
			return true;
		}

		return false;
	}

	private float CurrentTargetTTK => CurrentTarget?.GetTTK() ?? float.NaN;

	private bool CurrentTargetDiesWithin(float seconds)
	{
		if (seconds <= 0f)
		{
			return false;
		}

		var ttk = CurrentTargetTTK;
		return !float.IsNaN(ttk) && ttk < seconds;
	}

	private bool ShouldUsePotionInCombat
	{
		get
		{
			if (!UsePotion || !InCombat || !IsBurst)
			{
				return false;
			}

			if (PotionTiming == PotTiming.TwoMinutes && CombatElapsedLess(30f))
			{
				return false;
			}

			if (!IsPartyMedicated && !IsPartyBurst)
			{
				return false;
			}

			if (PhlegmaPvE.EnoughLevel && PhlegmaPvE.Cooldown.CurrentCharges < 1)
			{
				return false;
			}

			if (PsychePvE.EnoughLevel && !PsychePvE.Cooldown.WillHaveOneCharge(30f))
			{
				return false;
			}

			return true;
		}
	}

	private bool ShouldDumpPhlegmaForTTK => CurrentTargetDiesWithin(PhlegmaDumpTTKSeconds);

	private bool ShouldSkipDotForTTK => CurrentTargetDiesWithin(DotRefreshTTKSeconds);

	#region Kardia Swap

	private const float KardiaRange = 30f;
	private const float KardiaSwapToHp = 0.8f;
	private const float KardiaFullHp = 0.99f;

	private IBattleChara? CurrentKardionHolder
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member.HasStatus(true, StatusID.Kardion))
				{
					return member;
				}
			}
			return null;
		}
	}

	private IBattleChara? EnemyAggroTarget
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
				if (member.GameObjectId == aggroId)
				{
					return member;
				}
			}
			return null;
		}
	}

	private bool CanKardiaTarget(IBattleChara? target) =>
		target != null &&
		!target.IsDead &&
		!target.HasStatus(true, StatusID.Kardion) &&
		target.DistanceToPlayer() <= KardiaRange;

	private bool TryKardiaOn(IBattleChara? target, out IAction? act)
	{
		act = null;

		if (!KardiaPvE.EnoughLevel ||
			!KardiaPvE.IsEnabled ||
			KardiaPvE.Cooldown.IsCoolingDown ||
			!CanKardiaTarget(target))
		{
			return false;
		}

		KardiaPvE.Target = new TargetResult(target!, [target!], target!.Position);
		act = KardiaPvE;
		return true;
	}

	private const float KardiaNotFullHp = 0.95f;
	private const float KardiaSwapHpGap = 0.2f;

	private bool KardiaAllowedOn(IBattleChara member) => KardiaSwapValue switch
	{
		KardiaSwapMode.TanksOnly => IsTankMember(member),
		KardiaSwapMode.Everyone => true,
		_ => false,
	};

	private IBattleChara? LowestKardiaCandidate()
	{
		IBattleChara? lowest = null;
		foreach (var member in PartyMembers)
		{
			if (!CanKardiaTarget(member) ||
				IsHealBlocked(member) ||
				HasSingleHealLockoutStatus(member) ||
				!KardiaAllowedOn(member))
			{
				continue;
			}

			if (lowest == null || member.GetHealthRatio() < lowest.GetHealthRatio())
			{
				lowest = member;
			}
		}
		return lowest;
	}

	private bool TrySwapKardiaToInjured(out IAction? act)
	{
		act = null;

		if (KardiaSwapValue == KardiaSwapMode.None || !InCombat)
		{
			return false;
		}

		var candidate = LowestKardiaCandidate();
		if (candidate == null)
		{
			return false;
		}

		var holder = CurrentKardionHolder;
		var holderHp = holder == null || IsHealBlocked(holder) ? 1f : holder.GetHealthRatio();
		var candidateHp = candidate.GetHealthRatio();

		var holderDoneWithIt = holder == null || IsHealBlocked(holder) || holderHp >= KardiaFullHp;
		var shouldSwap =
			(holderDoneWithIt && candidateHp < KardiaNotFullHp) ||
			(candidateHp < KardiaSwapToHp && candidateHp < holderHp - KardiaSwapHpGap);

		return shouldSwap && TryKardiaOn(candidate, out act);
	}

	private bool TrySwapKardiaBack(out IAction? act)
	{
		act = null;

		if (KardiaSwapValue == KardiaSwapMode.None || !InCombat)
		{
			return false;
		}

		var holder = CurrentKardionHolder;
		var aggro = EnemyAggroTarget;

		if (holder == null ||
			aggro == null ||
			!IsTankMember(aggro) ||
			holder.GameObjectId == aggro.GameObjectId ||
			holder.GetHealthRatio() < KardiaFullHp)
		{
			return false;
		}

		var candidate = LowestKardiaCandidate();
		if (candidate != null && candidate.GetHealthRatio() < KardiaNotFullHp)
		{
			return false;
		}

		return TryKardiaOn(aggro, out act);
	}
	#endregion


	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Last E.Action Aim Cleared From Queue: {_lastEukrasiaActionAim}");
		ImGui.Text($"Current E.Action Aim: {_EukrasiaActionAim}");
		ImGui.Text($"Moving: {IsMoving} ({MovingTime:F1}s)  Grace passed: {MovingPastGrace}  Sustained: {HasSufficientMovement}");
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

	private const float ToxikonCountdownLeadSeconds = 1f;

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

		if (OpenerSelection == OpenerStrategy.PneumaOpener
			&& remainTime < PneumaPvE.Info.CastTime + CountDownAhead
			&& PneumaPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime <= 2.1f
			&& UsePotion
			&& PotionTiming == PotTiming.Opener
			&& UseBurstMedicine(out act))
		{
			return act;
		}

		if (OpenerSelection == OpenerStrategy.ToxikonOpener
			&& remainTime < ToxikonCountdownLeadSeconds + CountDownAhead
			&& ToxikonIiPvE.CanUse(out act))
		{
			return act;
		}

		var usesPrognosis = CountdownShield is
			CountdownShieldStrategy.EukrasianPrognosis or
			CountdownShieldStrategy.ZoeEukrasianPrognosis;

		var usesDiagnosis = CountdownShield is
			CountdownShieldStrategy.EukrasianDiagnosisKardionTank or
			CountdownShieldStrategy.EukrasianDiagnosisBothTanks;

		if (remainTime is < 15f and > 9f && KardiaPvE.CanUse(out act))
		{
			return act;
		}

		if (usesPrognosis)
		{
			if (remainTime is < 7f and > 6f
				&& HasEukrasia
				&& CanUseCountdownEukrasianPrognosis(out act))
			{
				return act;
			}

			if (remainTime is < 8f and > 7f
				&& !HasEukrasia
				&& EukrasiaPvE.CanUse(out act))
			{
				return act;
			}
		}

		if (CountdownShield == CountdownShieldStrategy.ZoeEukrasianPrognosis
			&& remainTime is < 14f and > 9f
			&& ZoePvE.CanUse(out act))
		{
			return act;
		}

		if (usesDiagnosis
			&& remainTime is < 13f and > 4.5f
			&& CanUseCountdownEukrasianDiagnosis(out act))
		{
			return act;
		}

		if (remainTime < 4f && EukrasiaPvE.CanUse(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}

	#endregion

	#region Healing Engine

	private const float IxocholeRange = 15f;
	private const float PhysisRange = 30f;
	private const float PneumaHealRange = 20f;
	private const float PepsisRange = 15f;
	private const float SingleHealRange = 30f;
	private const int AoeInjuredCount = 3;
	private const float PhysisIiCountedPotency = BeirutaHealModel.PhysisIiBoostedTickPotency * BeirutaHealModel.HotCreditTicks;
	private const float KeracholeCountedPotency = BeirutaHealModel.KeracholeTickPotency * BeirutaHealModel.HotCreditTicks;
	private const float IxocholePotency = 400f;
	private const float PneumaPotency = 600f;
	private const float PneumaWardOffCap = 0.75f;
	private const float PhilosophiaBoost = 1.2f;
	private const float IxocholeLightDamageHp = 0.7f;
	private const float AddersgallRichSeconds = 10f;

	private const float TopUpHp = 0.85f;
	private const float KrasisTankHp = 0.6f;
	private const float MitigatingKrasisHp = 0.8f;
	private const float CriticalHp = 0.3f;
	private const float ExtremeLowTankHp = 0.3f;
	private const float UndeadReleaseSeconds = 1f;
	private const float EarthlyDominanceGateSeconds = 3f;

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

	private const long DiagnosisThrottleMs = 20_000;
	private long _lastDiagnosisCastMs;
	private bool _diagnosisWasLastGcd;

	private void TrackDiagnosisCast()
	{
		var isLast = IsLastGCD(true, EukrasianDiagnosisPvE);
		if (isLast && !_diagnosisWasLastGcd)
		{
			_lastDiagnosisCastMs = Environment.TickCount64;
		}

		_diagnosisWasLastGcd = isLast;
	}

	private bool DiagnosisThrottled =>
		_lastDiagnosisCastMs != 0 &&
		Environment.TickCount64 - _lastDiagnosisCastMs < DiagnosisThrottleMs;

	private const float FreshMitigationSeconds = 6f;
	private const float RampartDuration = 20f;
	private const float BigMitigationDuration = 15f;

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

	private static bool IsBigMitigatingTank(IBattleChara member) =>
		IsTankMember(member) &&
		!IsHealBlocked(member) &&
		HasFreshBigMitigation(member);

	private static readonly StatusID[] TankMitigationStatus =
	[
		StatusID.BlackestNight,
		StatusID.HeartOfStone,
		StatusID.HeartOfCorundum,
		StatusID.RawIntuition,
		StatusID.Bloodwhetting,
		StatusID.Sheltron,
		StatusID.HolySheltron,
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

	private static bool WhmInParty
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member != null && !member.IsDead && member.IsJobs(ECommons.ExcelServices.Job.WHM))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool IsHealBlocked(IBattleChara member)
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

	private static bool IsJustOffUndead(IBattleChara member) =>
		!WhmInParty &&
		member.HasStatus(false, UndeadStatus) &&
		member.StatusTime(false, UndeadStatus) <= UndeadReleaseSeconds;

	private static bool SkipSingleHeal(IBattleChara member) =>
		IsHealBlocked(member) ||
		(WhmInParty && IsTankMember(member) && member.GetHealthRatio() < ExtremeLowTankHp);

	private static bool IsMitigatingTank(IBattleChara member) =>
		IsTankMember(member) &&
		!IsHealBlocked(member) &&
		(HasFreshBigMitigation(member) || member.HasStatus(false, TankMitigationStatus));

	private float LandingHotPotency(IBattleChara member)
	{
		if (!IsHealableInRange(member, PhysisRange))
		{
			return 0f;
		}

		var potency = 0f;
		if (!member.HasStatus(false, StatusID.PhysisIi) && BeirutaHealWindow.OgcdJustUsed(PhysisIiPvE))
		{
			potency += PhysisIiCountedPotency;
		}

		if (!member.HasStatus(false, StatusID.Kerakeia) && BeirutaHealWindow.OgcdJustUsed(KeracholePvE))
		{
			potency += KeracholeCountedPotency;
		}

		return potency;
	}

	private float EffectiveHpRatio(IBattleChara member) =>
		BeirutaHealModel.EffectiveHpRatio(member, WardMode, LandingHotPotency(member));

	private bool CountsAsInjured(IBattleChara member, float threshold) =>
		member.GetHealthRatio() < BeirutaHealModel.AlwaysInjuredHp ||
		EffectiveHpRatio(member) < threshold;

	private static float HealBoost => HasPhilosophia ? PhilosophiaBoost : 1f;

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) =>
		BeirutaHealModel.Threshold(potency, HealBoost, WardMode, UseCustomHPThreshold, customWardOff, PartyMembers, wardOffCap);

	private float PhysisHp => AoeThreshold(PhysisIiCountedPotency, PhysisHeal);

	private float IxocholeHp => AoeThreshold(IxocholePotency, IxocholeHeal);

	private float PneumaHp => AoeThreshold(PneumaPotency, PneumaHeal, PneumaWardOffCap);

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

	private const int CoHealerSpreadNonTankCount = 3;

	private static bool CoHealerInParty
	{
		get
		{
			var selfId = Player?.GameObjectId;
			foreach (var member in PartyMembers)
			{
				if (member != null &&
					!member.IsDead &&
					member.GameObjectId != selfId &&
					member.IsJobCategory(JobRole.Healer))
				{
					return true;
				}
			}
			return false;
		}
	}

	private int CountNonTankInjured(float range, float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) &&
				!IsTankMember(member) &&
				!IsHealBlocked(member) &&
				EffectiveHpRatio(member) < threshold)
			{
				count++;
			}
		}
		return count;
	}

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

	private static bool AccretionGroupHealBlocked => BeirutaAccretion.GroupHealBlocked(PartyMembers);

	private static bool HoldAoeForCoHealer(float range) => AccretionGroupHealBlocked || (CoHealerBigHealSoon && !AnyCritical(range));

	private int CountPepsisTargets(float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, PepsisRange) &&
				!IsHealBlocked(member) &&
				(member.HasStatus(true, StatusID.EukrasianPrognosis) || member.HasStatus(true, StatusID.EukrasianDiagnosis)) &&
				CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	private static int CountOwnPrognosisShields()
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, PepsisRange) && member.HasStatus(true, StatusID.EukrasianPrognosis))
			{
				count++;
			}
		}
		return count;
	}

	private bool ZoeWantsPrognosis =>
		HasZoe &&
		ZoeModeValue == ZoeUsage.SignalOnly &&
		PartyMembersAverHP >= ZoePneumaHealValue;

	private bool TryZoePepsisReshield(out IAction? act)
	{
		act = null;

		return ZoePepsisReshieldValue &&
			InCombat &&
			ZoeWantsPrognosis &&
			!HasHealingLockout &&
			!AccretionGroupHealBlocked &&
			CountOwnPrognosisShields() >= AoeInjuredCount &&
			PepsisPvE.CanUse(out act);
	}

	private bool TryPepsis(out IAction? act)
	{
		act = null;

		if (MergedStatus.HasFlag(AutoStatus.DefenseArea) || MergedStatus.HasFlag(AutoStatus.DefenseSingle))
		{
			return false;
		}

		return !AccretionGroupHealBlocked && CountPepsisTargets(PepsisHealValue) >= AoeInjuredCount && PepsisPvE.CanUse(out act);
	}

	private static IBattleChara? FindLowest(float range, Func<IBattleChara, bool> filter)
	{
		IBattleChara? best = null;
		foreach (var member in PartyMembers)
		{
			if (!IsHealableInRange(member, range) || SkipSingleHeal(member) || !filter(member))
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

	private IBattleChara? NonTankTopUp =>
		WardMode && BeirutaWard.NonTanksShielded(PartyMembers, SingleHealRange)
			? BeirutaHealModel.NonTankTopUpTarget(PartyMembers, SingleHealRange, EffectiveHpRatio, static m => IsHealBlocked(m) || HasSingleHealLockoutStatus(m), AoeInjuredCount)
			: null;


	private bool CanSpendAddersgall(bool ignoreReserve = false)
	{
		if (Addersgall >= 2)
		{
			return true;
		}

		if (Addersgall < 1)
		{
			return false;
		}

		return ignoreReserve || !KeracholeNeedsReserve;
	}

	private bool KeracholeNeedsReserve =>
		KeracholePvE.EnoughLevel &&
		!(RhizomataPvE.EnoughLevel && !RhizomataPvE.Cooldown.IsCoolingDown) &&
		KeracholePvE.Cooldown.WillHaveOneCharge(AddersgallTime);

	private const uint AddersgallDumpMaxMp = 9000;

	private static bool AddersgallMpUseful => CurrentMp <= AddersgallDumpMaxMp;

	private bool AddersgallOvercapSoon =>
		AddersgallMpUseful &&
		(Addersgall >= 3 ||
		(Addersgall >= 2 && AddersgallTime <= 3f) ||
		(Addersgall >= 2 && RhizomataPvE.EnoughLevel && !RhizomataPvE.Cooldown.IsCoolingDown));

	private bool RhizomataReady => RhizomataPvE.EnoughLevel && !RhizomataPvE.Cooldown.IsCoolingDown;

	private bool AddersgallRich =>
		Addersgall >= 3 ||
		(Addersgall >= 2 && (AddersgallTime <= AddersgallRichSeconds || RhizomataReady));

	private bool IxocholeReady =>
		IxocholePvE.EnoughLevel &&
		CanSpendAddersgall(AnyCritical(IxocholeRange)) &&
		IxocholePvE.CanUse(out _);

	private bool IxocholeFirst =>
		!WardMode &&
		AddersgallRich &&
		IxocholeReady &&
		AoeNeeded(IxocholeRange, BeirutaHealModel.RoundToFive(BeirutaHealModel.FullUseHp(IxocholePotency, HealBoost))) &&
		!AoeNeeded(IxocholeRange, IxocholeLightDamageHp);

	private bool IxocholeCanCover =>
		IxocholeFirst ||
		(IxocholeReady && AoeNeeded(IxocholeRange, IxocholeHp));

	private bool PhysisReady =>
		PhysisIiPvE.EnoughLevel ? PhysisIiPvE.CanUse(out _) : PhysisPvE.CanUse(out _);

	private bool PhysisCanCover => PhysisReady && AoeNeeded(PhysisRange, PhysisHp);

	private bool TryKrasisOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.Krasis) && BeirutaAccretion.TryUseOn(KrasisPvE, target, out act);
	}

	private bool TryDruocholeOn(IBattleChara target, bool ignoreReserve, out IAction? act)
	{
		act = null;
		return CanSpendAddersgall(ignoreReserve) && BeirutaAccretion.TryUseOn(DruocholePvE, target, out act);
	}

	private bool TaurocholeAllowedOn(IBattleChara target) =>
		TaurocholeModeValue switch
		{
			TaurocholeUsage.TanksOnly => IsTankMember(target),
			TaurocholeUsage.Anyone => true,
			_ => false,
		};

	private bool TryTaurocholeOn(IBattleChara target, bool ignoreReserve, out IAction? act)
	{
		act = null;
		return TaurocholeAllowedOn(target) &&
			!HasKerachole &&
			!target.HasStatus(false, StatusID.Kerachole) &&
			!target.HasStatus(false, StatusID.Taurochole) &&
			CanSpendAddersgall(ignoreReserve) &&
			BeirutaAccretion.TryUseOn(TaurocholePvE, target, out act);
	}

	private bool TryHaimaOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.Haima) && BeirutaAccretion.TryUseOn(HaimaPvE, target, out act);
	}

	private bool TrySoteriaFor(IBattleChara target, out IAction? act)
	{
		act = null;
		return target.HasStatus(true, StatusID.Kardion) && SoteriaPvE.CanUse(out act);
	}

	private bool TryMitigationSupport(IBattleChara? tank, out IAction? act)
	{
		act = null;

		if (tank == null || !IsTankMember(tank) || IsHealBlocked(tank) || !IsHealableInRange(tank, SingleHealRange))
		{
			return false;
		}

		if (TryHaimaOn(tank, out act))
		{
			return true;
		}

		if (TryTaurocholeOn(tank, false, out act))
		{
			return true;
		}

		if (tank.GetHealthRatio() < MitigatingKrasisHp && TryKrasisOn(tank, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool PrognosisQueued =>
		HasEukrasia &&
		(_EukrasiaActionAim == EukrasianPrognosisPvE || _EukrasiaActionAim == EukrasianPrognosisIiPvE);

	private bool TryHealAbility(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		var undeadTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m));
		if (undeadTank != null &&
			(TryKrasisOn(undeadTank, out act) ||
			 TryTaurocholeOn(undeadTank, true, out act) ||
			 TryDruocholeOn(undeadTank, true, out act) ||
			 TrySoteriaFor(undeadTank, out act)))
		{
			return true;
		}

		if (PrognosisQueued)
		{
			var lowest = FindLowest(SingleHealRange, static m => m.GetHealthRatio() < 1f);
			if (lowest != null && TryKrasisOn(lowest, out act))
			{
				return true;
			}
		}

		if (!HoldAoeForCoHealer(PhysisRange))
		{
			if (!IxocholeFirst &&
				AoeNeeded(PhysisRange, PhysisHp) &&
				(PhysisIiPvE.CanUse(out act) || (!PhysisIiPvE.EnoughLevel && PhysisPvE.CanUse(out act))))
			{
				return true;
			}

			if (IxocholeCanCover && IxocholePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (TryPepsis(out act))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsMitigatingTank(member) && TryMitigationSupport(member, out act))
			{
				return true;
			}
		}

		var aoeSituation =
			AoeNeeded(IxocholeRange, IxocholeHp) ||
			(CoHealerInParty && CountNonTankInjured(SingleHealRange, TopUpHp) > CoHealerSpreadNonTankCount);

		if (!aoeSituation)
		{
			var lowTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < KrasisTankHp);
			if (lowTank != null && TryKrasisOn(lowTank, out act))
			{
				return true;
			}

			var soteriaHolder = FindLowest(SingleHealRange, m => m.HasStatus(true, StatusID.Kardion) && m.GetHealthRatio() < SoteriaHealValue);
			if (soteriaHolder != null && TrySoteriaFor(soteriaHolder, out act))
			{
				return true;
			}

			var taurocholeTarget = FindLowest(SingleHealRange, m => TaurocholeAllowedOn(m) && m.GetHealthRatio() < HealSingleTaurocholeHealValue);
			if (taurocholeTarget != null && TryTaurocholeOn(taurocholeTarget, false, out act))
			{
				return true;
			}

			var druocholeTank = FindLowest(SingleHealRange, m => IsTankMember(m) && m.GetHealthRatio() < HealSingleDruocholeHealValue);
			if (druocholeTank != null && TryDruocholeOn(druocholeTank, false, out act))
			{
				return true;
			}

			if (NonTankTopUp is { } topUp && TryDruocholeOn(topUp, false, out act))
			{
				return true;
			}
		}

		if (AddersgallOvercapSoon)
		{
			var dumpTarget = FindLowest(SingleHealRange, static _ => true);
			if (dumpTarget != null && TryDruocholeOn(dumpTarget, false, out act))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	private bool TryHealGCD(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine || ShouldDeferToRaise())
		{
			return false;
		}

		if (!HoldAoeForCoHealer(PneumaHealRange) &&
			(!(IxocholeCanCover || PhysisCanCover) || AnyCritical(PneumaHealRange)))
		{
			if (AoeNeeded(PneumaHealRange, PneumaHp) && PneumaPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (NonTankTopUp is { } topUp &&
			!CanSpendAddersgall() &&
			!HasEukrasia &&
			BeirutaAccretion.TryUseOn(DiagnosisPvE, topUp, out act))
		{
			return true;
		}

		if (IsPartyBurst || HasMedicated || HasPhilosophia || !EukrasianDiagnosisPvE.EnoughLevel || !EukrasianDiagnosisPvE.IsEnabled)
		{
			return false;
		}

		if (DiagnosisThrottled && !(HasEukrasia && _EukrasiaActionAim == EukrasianDiagnosisPvE))
		{
			return false;
		}

		var tank = FindLowest(SingleHealRange, static m => IsBigMitigatingTank(m) && CanDiagnose(m));
		if (tank == null)
		{
			return false;
		}

		if (!HasEukrasia && Addersting >= 3)
		{
			return ToxikonPvE.CanUse(out act);
		}

		QueueDiagnosis(tank);
		return DoEukrasianDiagnosis(out act);
	}

	#endregion

	#region oGCD Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (TryAccretionAbility(out act) || TryZoePepsisReshield(out act))
		{
			return true;
		}

		if (IsLastGCD(false,
				EukrasianPrognosisIiPvE,
				EukrasianPrognosisPvE,
				EukrasianDiagnosisPvE,
				EukrasianDyskrasiaPvE,
				EukrasianDosisIiiPvE,
				EukrasianDosisIiPvE,
				EukrasianDosisPvE)
			|| !InCombat)
		{
			ClearEukrasia(nextGCD);
		}

		if (ChoiceEukrasia(out act))
		{
			return true;
		}

		if (ZoeModeValue == ZoeUsage.AutoPneuma &&
			ZoePvE.EnoughLevel &&
			!ZoePvE.Cooldown.IsCoolingDown &&
			nextGCD.IsTheSameTo(false, PneumaPvE) &&
			ZoePvE.CanUse(out act))
		{
			return true;
		}

		if (TryHealAbility(out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.PsychePvE, ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (ShouldSwiftcastForMovement(nextGCD) &&
			SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (IsBurst && PhlegmaOpenerAllowed && PsychePvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.HaimaPvE, ActionID.TaurocholePvE, ActionID.KrasisPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		var tank = FindLowest(SingleHealRange, static m => IsMitigatingTank(m)) ?? EnemyAggroTarget ?? FindLowest(SingleHealRange, static m => IsTankMember(m));
		if (TryMitigationSupport(tank, out act))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}


	[RotationDesc(ActionID.KardiaPvE, ActionID.RhizomataPvE)]
	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (InCombat || !HasKardia)
		{
			if (KardiaPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (TrySwapKardiaBack(out act))
		{
			return true;
		}

		if (!HasHealingLockout && TrySwapKardiaToInjured(out act))
		{
			return true;
		}

		if (Addersgall <= 1 && RhizomataPvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	#endregion

	#region Eukrasia Logic

	private void SetEukrasia(IBaseAction act)
	{
		if (act == null || (_EukrasiaActionAim != null && IsLastGCD(true, _EukrasiaActionAim)))
		{
			return;
		}

		_EukrasiaActionAim = act;
	}

	private void ClearEukrasia(IAction nextGCD)
	{
		if (_EukrasiaActionAim != null)
		{
			_lastEukrasiaActionAim = _EukrasiaActionAim;
			_EukrasiaActionAim = null;

			if (HasEukrasia
				&& ((InCombat && HasHostilesInMaxRange && nextGCD == null && SecondsSinceLastNextGCDChange >= 2f)
					|| (!InCombat && !HasHostilesInMaxRange)))
			{
				StatusHelper.StatusOff(StatusID.Eukrasia);
			}
		}
	}

	private const long ShieldEukrasiaRetryMs = 3_000;
	private long _shieldEukrasiaFailedMs;

	private bool DefenseSignalActive =>
		MergedStatus.HasFlag(AutoStatus.DefenseArea) ||
		MergedStatus.HasFlag(AutoStatus.DefenseSingle);

	private bool ShieldEukrasiaOnRetryCooldown =>
		_shieldEukrasiaFailedMs != 0 &&
		Environment.TickCount64 - _shieldEukrasiaFailedMs < ShieldEukrasiaRetryMs;

	private bool EukrasiaAimIsDamage =>
		_EukrasiaActionAim == EukrasianDosisIiiPvE ||
		_EukrasiaActionAim == EukrasianDosisIiPvE ||
		_EukrasiaActionAim == EukrasianDosisPvE ||
		_EukrasiaActionAim == EukrasianDyskrasiaPvE;

	private bool ChoiceEukrasia(out IAction? act)
	{
		act = null;
		var selected = false;

		if (!HasEukrasia && DefenseSignalActive && ShieldEukrasiaOnRetryCooldown)
		{
			return false;
		}

		if (HasEukrasia &&
			_EukrasiaActionAim != null &&
			!MergedStatus.HasFlag(AutoStatus.DefenseArea) &&
			!MergedStatus.HasFlag(AutoStatus.DefenseSingle))
		{
			return false;
		}

		if (EukrasianPrognosisIiPvE.EnoughLevel
			&& EukrasianPrognosisIiPvE.IsEnabled
			&& MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& MostPartyLacksEukrasianShield()
			&& EukrasianPrognosisIiPvE.CanUse(out _))
		{
			SetEukrasia(EukrasianPrognosisIiPvE);
			selected = true;
		}
		else if (!EukrasianPrognosisIiPvE.EnoughLevel
			&& EukrasianPrognosisPvE.EnoughLevel
			&& EukrasianPrognosisPvE.IsEnabled
			&& MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& MostPartyLacksEukrasianShield()
			&& EukrasianPrognosisPvE.CanUse(out _))
		{
			SetEukrasia(EukrasianPrognosisPvE);
			selected = true;
		}
		else if (EukrasianDiagnosisPvE.EnoughLevel
			&& EukrasianDiagnosisPvE.IsEnabled
			&& Addersting < 3
			&& !DiagnosisThrottled
			&& MergedStatus.HasFlag(AutoStatus.DefenseSingle)
			&& FindLowest(SingleHealRange, static m => IsMitigatingTank(m) && CanDiagnose(m)) is { } defenseTank)
		{
			SetEukrasia(EukrasianDiagnosisPvE);
			_diagnosisTarget = defenseTank;
			selected = true;
		}
		else if (!ShouldSkipDotForTTK
			&& EukrasianDyskrasiaPvE.EnoughLevel
			&& EukrasianDyskrasiaPvE.IsEnabled
			&& !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
			&& !MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& EukrasianDyskrasiaPvE.CanUse(out _))
		{
			SetEukrasia(EukrasianDyskrasiaPvE);
			selected = true;
		}
		else if (!ShouldSkipDotForTTK
			&& (!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
			&& EukrasianDosisIiiPvE.CanUse(out _)
			&& EukrasianDosisIiiPvE.EnoughLevel
			&& !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
			&& !MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& EukrasianDosisIiiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiiPvE);
			selected = true;
		}
		else if (!ShouldSkipDotForTTK
			&& (!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
			&& EukrasianDosisIiPvE.CanUse(out _)
			&& !EukrasianDosisIiiPvE.EnoughLevel
			&& EukrasianDosisIiPvE.EnoughLevel
			&& !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
			&& !MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& EukrasianDosisIiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiPvE);
			selected = true;
		}
		else if (!ShouldSkipDotForTTK
			&& (!EukrasianDyskrasiaPvE.CanUse(out _) || !DyskrasiaPvE.CanUse(out _))
			&& EukrasianDosisPvE.CanUse(out _)
			&& !EukrasianDosisIiPvE.EnoughLevel
			&& EukrasianDosisPvE.EnoughLevel
			&& !MergedStatus.HasFlag(AutoStatus.DefenseSingle)
			&& !MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& EukrasianDosisPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisPvE);
			selected = true;
		}

		if (!selected)
		{
			return false;
		}

		if (!HasEukrasia && EukrasiaPvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	#endregion

	#region Eukrasia Execution

	private bool DoEukrasianPrognosisIi(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianPrognosisIiPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianPrognosisIiPvE.CanUse(out act);
	}

	private bool DoEukrasianPrognosis(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianPrognosisPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianPrognosisPvE.CanUse(out act);
	}

	private bool DoEukrasianDiagnosis(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianDiagnosisPvE)
		{
			return false;
		}

		if (!CanDiagnose(_diagnosisTarget))
		{
			_EukrasiaActionAim = null;
			_diagnosisTarget = null;
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return BeirutaAccretion.TryUseOn(EukrasianDiagnosisPvE, _diagnosisTarget!, out act);
	}

	private IBattleChara? _diagnosisTarget;

	private static bool CanDiagnose(IBattleChara? target) =>
		target != null &&
		IsHealableInRange(target, SingleHealRange) &&
		!IsHealBlocked(target) &&
		!HasSingleShield(target) &&
		!HasSingleHealLockoutStatus(target);

	private void QueueDiagnosis(IBattleChara target)
	{
		_EukrasiaActionAim = null;
		SetEukrasia(EukrasianDiagnosisPvE);
		_diagnosisTarget = target;
	}

	private bool DoEukrasianDyskrasia(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianDyskrasiaPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianDyskrasiaPvE.CanUse(out act);
	}

	private bool DoEukrasianDosisIii(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianDosisIiiPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianDosisIiiPvE.CanUse(out act);
	}

	private bool DoEukrasianDosisIi(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianDosisIiPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianDosisIiPvE.CanUse(out act);
	}

	private bool DoEukrasianDosis(out IAction? act)
	{
		act = null;

		if (_EukrasiaActionAim != EukrasianDosisPvE)
		{
			return false;
		}

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return EukrasianDosisPvE.CanUse(out act);
	}

	#endregion

	#region Damage / DoT Helpers

	private bool CurrentTargetEukrasianDosisMissingOrEnding(float remainingSeconds)
	{
		if (CurrentTarget == null)
		{
			return false;
		}

		return
			(EukrasianDosisIiiPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosisIii) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosisIii))) ||
			(!EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosisIi) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosisIi))) ||
			(!EukrasianDosisIiiPvE.EnoughLevel && !EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.EukrasianDosis) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.EukrasianDosis)));
	}

	private bool ShouldEarlyRefreshEukrasianDosis()
	{
		if (!InCombat || CurrentTarget == null)
		{
			return false;
		}

		if (ShouldSkipDotForTTK || !CurrentTargetEukrasianDosisMissingOrEnding(EarlyDotRefreshSeconds))
		{
			return false;
		}

		return HasMedicated;
	}

	private bool PrepareEarlyEukrasianDosisRefresh(out IAction? act)
	{
		act = null;

		if (!ShouldEarlyRefreshEukrasianDosis())
		{
			return false;
		}

		_EukrasiaActionAim = null;

		if (EukrasianDosisIiiPvE.EnoughLevel && EukrasianDosisIiiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiiPvE);
			return DoEukrasianDosisIii(out act);
		}

		if (EukrasianDosisIiPvE.EnoughLevel && EukrasianDosisIiPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisIiPvE);
			return DoEukrasianDosisIi(out act);
		}

		if (EukrasianDosisPvE.EnoughLevel && EukrasianDosisPvE.IsEnabled)
		{
			SetEukrasia(EukrasianDosisPvE);
			return DoEukrasianDosis(out act);
		}

		return false;
	}

	#endregion

	#region Accretion Top Up

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target == null)
		{
			return false;
		}

		return BeirutaAccretion.TryUseOn(KrasisPvE, target, out act)
			|| BeirutaAccretion.TryUseOn(DruocholePvE, target, out act)
			|| BeirutaAccretion.TryUseOn(TaurocholePvE, target, out act);
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target != null &&
			!HasEukrasia &&
			!HasSwift &&
			BeirutaAccretion.TryUseOn(DiagnosisPvE, target, out act))
		{
			return true;
		}

		return base.EmergencyGCD(nextGCD, out act);
	}

	#endregion

	#region GCD Logic


	[RotationDesc(ActionID.EgeiroPvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		act = null;

		if (EgeiroPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	private bool _lastWardMode;

	protected override void UpdateInfo()
	{
		BeirutaWard.Track(this, WardMode, ref _lastWardMode);
		TrackDiagnosisCast();
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

	private bool TryWardModeGCD(out IAction? act)
	{
		act = null;

		if (!(WardMode || BeirutaAccretion.GuardActive(PartyMembers)) || HasHealingLockout)
		{
			return false;
		}

		IBaseAction? prognosis = null;
		if (EukrasianPrognosisIiPvE.EnoughLevel && EukrasianPrognosisIiPvE.IsEnabled)
		{
			prognosis = EukrasianPrognosisIiPvE;
		}
		else if (EukrasianPrognosisPvE.EnoughLevel && EukrasianPrognosisPvE.IsEnabled)
		{
			prognosis = EukrasianPrognosisPvE;
		}

		if (prognosis == null ||
			BeirutaWard.CountMissing(PartyMembers, PrognosisRange, static m => HasEukrasianShield(m)) < BeirutaWard.UnshieldedPartyCount)
		{
			return false;
		}

		_EukrasiaActionAim = null;
		SetEukrasia(prognosis);

		if (!HasEukrasia)
		{
			return EukrasiaPvE.CanUse(out act);
		}

		return prognosis.CanUse(out act, skipStatusProvideCheck: true);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		act = null;

		if (ShouldDeferToRaise())
		{
			return base.GeneralGCD(out act);
		}

		if (TryWardModeGCD(out act))
		{
			return true;
		}

		if (HasZoe)
		{
			if (ZoeModeValue == ZoeUsage.AutoPneuma || PartyMembersAverHP < ZoePneumaHealValue)
			{
				if (PneumaPvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (!HasEukrasianPrognosis)
			{
				_EukrasiaActionAim = null;

				if (EukrasianPrognosisIiPvE.EnoughLevel && EukrasianPrognosisIiPvE.IsEnabled)
				{
					SetEukrasia(EukrasianPrognosisIiPvE);

					if (DoEukrasianPrognosisIi(out act))
					{
						return true;
					}
				}
				else if (EukrasianPrognosisPvE.EnoughLevel && EukrasianPrognosisPvE.IsEnabled)
				{
					SetEukrasia(EukrasianPrognosisPvE);

					if (DoEukrasianPrognosis(out act))
					{
						return true;
					}
				}
			}
		}

		if (TryHealGCD(out act))
		{
			return true;
		}

		if (DoEukrasianPrognosisIi(out act))
		{
			return true;
		}

		if (DoEukrasianPrognosis(out act))
		{
			return true;
		}

		if (DoEukrasianDiagnosis(out act))
		{
			return true;
		}

		if (PrepareEarlyEukrasianDosisRefresh(out act))
		{
			return true;
		}

		if (IsBurst && PhlegmaOpenerAllowed)
		{
			if (PhlegmaPvE.CanUse(out act, usedUp:
				IsPartyBurst
				|| HasMedicated
				|| ShouldDumpPhlegmaForTTK
				|| PhlegmaPvE.Cooldown.WillHaveXChargesGCD(2, 2)
				|| IsPhlegmaAoeAtLeast(2)))
			{
				return true;
			}
		}

		if (TryMovementGCD(out act))
		{
			return true;
		}

		if ((IsPhlegmaAoeAtLeast(2) || (KeepTwoAddersting && Addersting > 2 && PartyHasOwnEukrasianShield)) &&
	ToxikonPvE.CanUse(out act))
		{
			return true;
		}

		if (HasEukrasia && DefenseSignalActive && !EukrasiaAimIsDamage)
		{
			_EukrasiaActionAim = null;
			_diagnosisTarget = null;
			_shieldEukrasiaFailedMs = Environment.TickCount64;
			StatusHelper.StatusOff(StatusID.Eukrasia);
			act = null;
			return false;
		}

		if (DoEukrasianDyskrasia(out act))
		{
			return true;
		}

		if (DyskrasiaPvE.CanUse(out act))
		{
			return true;
		}

		if (DoEukrasianDosisIii(out act))
		{
			return true;
		}

		if (DoEukrasianDosisIi(out act))
		{
			return true;
		}

		if (DoEukrasianDosis(out act))
		{
			return true;
		}

		if (DosisPvE.CanUse(out act))
		{
			return true;
		}

		if (!InCombat && !HasEukrasia && EukrasiaPvE.CanUse(out act))
		{
			return true;
		}

		if (InCombat && !HasHostilesInRange && EukrasiaPvE.CanUse(out act))
		{
			return true;
		}

		if (InCombat && HasHostilesInRange && HasEukrasia)
		{
			if (MostPartyLacksEukrasianShield() && EukrasianPrognosisPvE.CanUse(out act))
			{
				return true;
			}

			StatusHelper.StatusOff(StatusID.Eukrasia);
		}

		return base.GeneralGCD(out act);
	}

	#endregion
}