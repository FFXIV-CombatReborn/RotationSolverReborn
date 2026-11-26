using static RotationSolver.Engine.Helpers.BeirutaParty;

namespace RotationSolver.Rotations.Healer.Sage;

[Rotation("BeirutaSGE", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Sage. Damage and healing are automatic. Planned cooldowns (mitigation, HoTs, Lightspeed and the like) follow the built-in CD plan when there is one (see the CD Plan section); otherwise use your Boss Mod / BMR CD planner or press them yourself, with Intercept set to GCD only and ability intercept unticked. Questions? Ask @Beiruta on the RSR Discord.")]
[SourceCode(Path = "main/Rotations/Healer/Sage/BeirutaSGE.cs")]
[ExtraRotation]

public sealed class BeirutaSGE : SageRotation, Updaters.IBeirutaTimelineOption, IBeirutaHealContext, IBeirutaHealer
{
	#region Config Options

	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Use built-in CD plan: planned cooldowns (mitigation, HoTs, Lightspeed and the like) follow the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press those cooldowns.")]
	public BeirutaPlanSource CdPlanFrom { get; set; } = BeirutaPlanSource.BuiltIn;

	public bool UseTimelinePlan => CdPlanFrom == BeirutaPlanSource.BuiltIn;

	[RotationConfig(CombatType.PvE, Name = "    > Show CD Plan timeline", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"A small window like the action timeline, showing the plan's actions coming up in the next 10s. It shows in a duty that has a CD plan; hover the window for lock / close.")]
	public bool ShowPlanTimeline { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use recommended settings for the plan", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"When a built-in plan loads, the settings it recommends are applied, like a preset. Changing a setting afterwards (e.g. your Ward Mode macro) works as usual.\n" +
		"Your saved settings are not changed by the plan: they show as you set them and come back once the plan unloads or this is off.")]
	public bool UsePlanSettings { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Eukrasian Prognosis", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"On: the plan's Eukrasian Prognosis are cast in their window, unless the party is already shielded.\n" +
		"Off: they are skipped; the rotation shields as usual (still after a planned Zoe).")]
	public bool UsePlannedShields { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.IBeirutaTimelineOption.OptionalPlannedActions =>
	[
		((uint)ActionID.EukrasianPrognosisIiPvE, nameof(UsePlannedShields)),
	];

	bool Updaters.IBeirutaTimelineOption.AllowPlannedAction(uint actionId) =>
		UsePlannedShields ||
		actionId is not ((uint)ActionID.EukrasianPrognosisPvE or (uint)ActionID.EukrasianPrognosisIiPvE);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		Updaters.BeirutaTimelineUpdater.HoldQueuedPlannedAction(action.ID);

	bool Updaters.IBeirutaTimelineOption.RotationRunsPlannedAction(uint actionId) =>
		actionId is (uint)ActionID.EukrasianPrognosisPvE or (uint)ActionID.EukrasianPrognosisIiPvE;

	#endregion

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps every non-tank shielded.\n" +
		"- Eukrasian Prognosis whenever a non-tank in range is unshielded.\n" +
		"- AoE thresholds use real HP and ignore the co-healer, HoTs and Eudaimonia.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[More settings] Show", Tooltip =
		"Pick a page to show its detailed settings. This only changes what is shown.")]
	public BeirutaSettingsPage MoreSettings { get; set; } = BeirutaSettingsPage.Hide;

	private bool WardActive => WardMode || Updaters.BeirutaTimelineUpdater.PlannedWardMode;

	bool IBeirutaHealContext.WardMode => WardActive;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Pre-pull GCD", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Pneuma lands as the countdown ends. Toxikon is sent about 1s earlier, since Eukrasia can't be precast and Toxikon lands late.")]
	public OpenerStrategy OpenerSelection { get; set; } = OpenerStrategy.PneumaOpener;

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Phlegma / Psyche", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
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

	#region Potion

	[RotationConfig(CombatType.PvE, Name = "[Potion] Use potion", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage)]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > First potion", Parent = nameof(UsePotion), Tooltip =
		"Opener: during the countdown. 2 minutes: the first potion waits for the 2-minute burst.\n" +
		"In combat it follows raid buffs, with a Phlegma charge and Psyche ready within 30s.\n" +
		"With others: only when a party member already has the potion buff (Medicated), so you pot together with them.")]
	public BeirutaPotTiming PotionTiming { get; set; } = BeirutaPotTiming.Opener;

	#endregion

	#region Damage

	[RotationConfig(CombatType.PvE, Name = "[Damage] Phlegma / Toxikon on 2+ enemies", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage)]
	public bool MultiTargetPhlegmaDump { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Toxikon at 3 Addersting while your shields are up", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"Breaking Eukrasian shields gives Addersting, so Toxikon is spent at 3 to avoid overcapping.")]
	public bool KeepTwoAddersting { get; set; } = true;

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Eukrasian Dosis refresh", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"While moving, refreshes Eukrasian Dosis early as an instant GCD.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.HealingPage, Tooltip =
		"Off: each area heal waits until 3+ players are low enough that it won't be wasted, worked out from its potency, the co-healer's smallest GCD heal, incoming HoT ticks and Eudaimonia.\n" +
		"Off also means: Taurochole on tanks, Zoe before a healing Pneuma, no heals during Macrocosmos, Eukrasian Prognosis while moving, no Kardia swap.\n" +
		"On: each area heal fires when 3+ players in range are below the value set here.\n" +
		"Ward Mode ignores these values and uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Taurochole targets", Parent = nameof(UseCustomHPThreshold))]
	public TaurocholeUsage TaurocholeMode { get; set; } = TaurocholeUsage.TanksOnly;

	[RotationConfig(CombatType.PvE, Name = "    > Zoe usage", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Auto: Zoe right before a healing Pneuma.\n" +
		"Only after you press Zoe: the rotation follows with Pneuma or Eukrasian Prognosis by party HP.\n" +
		"A built-in plan that has Zoe presses it itself; the rotation only follows up.")]
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
	[RotationConfig(CombatType.PvE, Name = "    > Physis II: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float PhysisHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Ixochole: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float IxocholeHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Pneuma: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float PneumaHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Pepsis: area heal when 3+ shielded players below", Parent = nameof(UseCustomHPThreshold))]
	public float PepsisHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Soteria: Kardion target below", Parent = nameof(UseCustomHPThreshold))]
	public float SoteriaHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Taurochole: below", Parent = nameof(UseCustomHPThreshold))]
	public float HealSingleTaurocholeHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Druochole: tank below", Parent = nameof(UseCustomHPThreshold))]
	public float HealSingleDruocholeHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > After your Zoe: Pneuma if party average below (else Eukrasian Prognosis)", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"With Zoe usage = Only after you press Zoe: party average HP below this uses Pneuma, otherwise Eukrasian Prognosis.")]
	public float ZoePneumaHeal { get; set; } = 0.40f;

	private TaurocholeUsage TaurocholeModeValue => UseCustomHPThreshold ? TaurocholeMode : TaurocholeUsage.TanksOnly;
	private ZoeUsage ZoeModeValue => PlannedZoe ? ZoeUsage.SignalOnly : UseCustomHPThreshold ? ZoeMode : ZoeUsage.AutoPneuma;
	private bool ZoePepsisReshieldValue => UseCustomHPThreshold && ZoePepsisReshield;
	private bool MacrocosmosLockValue => !UseCustomHPThreshold || LockHealingActionsDuringMacrocosmos;
	private bool MovementShieldHealingValue => !UseCustomHPThreshold || MovementShieldHealing;
	private KardiaSwapMode KardiaSwapValue => UseCustomHPThreshold ? KardiaSwap : KardiaSwapMode.None;
	private float PepsisHealValue => UseCustomHPThreshold ? PepsisHeal : 0.6f;
	private float SoteriaHealValue => UseCustomHPThreshold ? SoteriaHeal : 0.8f;
	private float HealSingleTaurocholeHealValue => UseCustomHPThreshold ? HealSingleTaurocholeHeal : 0.7f;
	private float HealSingleDruocholeHealValue => UseCustomHPThreshold ? HealSingleDruocholeHeal : 0.65f;
	private float ZoePneumaHealValue => UseCustomHPThreshold ? ZoePneumaHeal : 0.40f;

	#endregion

	#endregion

	#region Constants / Fields

	private const float EarlyDotRefreshSeconds = 12f;
	private const float PhlegmaDumpTTKSeconds = 10f;
	private const float DotRefreshTTKSeconds = 15f;
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

	private bool PlannedZoe => Updaters.BeirutaTimelineUpdater.PlanContains(ZoePvE.ID);

	private bool PlanOwnsPhysis => Updaters.BeirutaTimelineUpdater.PlanContains(PhysisIiPvE.ID);

	private const float PlannedZoeHoldSeconds = 8f;

	private bool ShieldHeldForPlannedZoe =>
		!HasZoe &&
		ZoePvE.EnoughLevel &&
		!ZoePvE.Cooldown.IsCoolingDown &&
		Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedZoeHoldSeconds, ZoePvE.ID);

	private bool ZoeReservedForPlan => HasZoe && PlannedZoe;

	private const float PlannedZoeLastCallSeconds = 5f;
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

		if (!BeirutaMove.PastGrace || HasEukrasia || HasSwift || BeirutaMove.RaiseSwiftHeld)
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

		if (BeirutaMove.Sufficient && MovementDotRefreshDue && SetEukrasianDosisAim() && DoAimedEukrasianDosis(out act))
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
			!BeirutaMove.PastLead ||
			HasSwift ||
			IsLastAction(ActionID.SwiftcastPvE) ||
			BeirutaMove.RaiseSwiftHeld)
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
		BeirutaMove.Sufficient &&
		BeirutaMove.PastLead &&
		!BeirutaMove.RaiseSwiftHeld;

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

		return BeirutaTarget.TryRsrTarget(EukrasianDiagnosisPvE, target, out act, skipStatusProvideCheck: true);
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

	private bool ShouldUsePotionInCombat =>
		BeirutaPotion.Ready(UsePotion, PotionTiming, IsBurst,
			IsPartyBurst &&
			!(PhlegmaPvE.EnoughLevel && PhlegmaPvE.Cooldown.CurrentCharges < 1) &&
			!(PsychePvE.EnoughLevel && !PsychePvE.Cooldown.WillHaveOneCharge(30f)));

	private bool ShouldDumpPhlegmaForTTK => CurrentTargetDiesWithin(PhlegmaDumpTTKSeconds);

	private const int ToxikonWeaveAbilities = 2;

	private static bool WeaveRoomWanted => BeirutaHealerFlow.WeaveRoomWanted(ToxikonWeaveAbilities);

	private bool ShouldSkipDotForTTK => CurrentTargetDiesWithin(DotRefreshTTKSeconds) || !DotWorthBeforeDowntime;

	private const float EukrasianDosisInitialPotency = 0f;
	private const float EukrasianDosisTickPotency = 85f;
	private const float DosisFillerPotency = 380f;
	private const float EukrasianDosisGcdCost = 1.4f;

	private bool DotWorthBeforeDowntime =>
		BeirutaDotValue.WorthBeforeDowntime(CurrentTarget, EukrasianDosisInitialPotency, EukrasianDosisTickPotency, DosisFillerPotency, EukrasianDosisGcdCost);

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

		return BeirutaTarget.TryRsrTarget(KardiaPvE, target!, out act, skipStatusProvideCheck: true);
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
		ImGui.Text($"Moving: {IsMoving} ({MovingTime:F1}s)  Grace passed: {BeirutaMove.PastGrace}  Sustained: {BeirutaMove.Sufficient}");
	}

	#endregion

	#region Countdown Logic

	private readonly BeirutaCountdownDelay _countdownDelay = new();

	private const float ToxikonCountdownLeadSeconds = 1f;

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

		if (OpenerSelection == OpenerStrategy.PneumaOpener
			&& remainTime < PneumaPvE.Info.CastTime + CountDownAhead
			&& PneumaPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime <= 2.1f
			&& UsePotion
			&& PotionTiming == BeirutaPotTiming.Opener
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

	private bool CanRunHealEngine => BeirutaHeal.CanRun(HasHealingLockout);

	private static bool AccretionGroupHealBlocked => BeirutaHeal.GroupHealBlocked;

	private float EffectiveHpRatio(IBattleChara member) => BeirutaHeal.EffectiveHpRatio(this, member);

	private bool CountsAsInjured(IBattleChara member, float threshold) => BeirutaHeal.Injured(this, member, threshold);

	private int CountInjured(float range, float threshold) => BeirutaHeal.CountInjured(this, range, threshold);

	private bool AoeNeeded(float range, float threshold) => BeirutaHeal.AoeNeeded(this, range, threshold);

	private bool SpotHealOk(IBattleChara member) => SpotHealAllowed(member, AoeNeeded(IxocholeRange, IxocholeHp), IxocholeRange);

	private bool AoeGcdNeeded(float range, float threshold) => BeirutaHeal.AoeGcdNeeded(this, range, threshold);

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) => BeirutaHeal.Threshold(this, potency, customWardOff, wardOffCap);

	float IBeirutaHealContext.HealBoost => HealBoost;

	float IBeirutaHealContext.LandingHotPotency(IBattleChara member) => LandingHotPotency(member);
	private static bool HoldAoeForCoHealer(float range) => BeirutaHeal.HoldAoe(range);

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
	private const long DiagnosisThrottleMs = 30_000;

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

	private static readonly StatusID[] CoHealerMacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];


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

	private static float HealBoost => HasPhilosophia ? PhilosophiaBoost : 1f;

	private float PhysisHp => AoeThreshold(PhysisIiCountedPotency, PhysisHeal);

	private float IxocholeHp => AoeThreshold(IxocholePotency, IxocholeHeal);

	private float PneumaHp => AoeThreshold(PneumaPotency, PneumaHeal, PneumaWardOffCap);

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

		if (MergedStatus.HasFlag(AutoStatus.DefenseArea) || TankbusterSoon)
		{
			return false;
		}

		return !AccretionGroupHealBlocked && CountPepsisTargets(PepsisHealValue) >= AoeInjuredCount && PepsisPvE.CanUse(out act);
	}

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

	private const float PlannedKeracholeReserveSeconds = 25f;

	private bool KeracholeNeedsReserve =>
		KeracholePvE.EnoughLevel &&
		((!(RhizomataPvE.EnoughLevel && !RhizomataPvE.Cooldown.IsCoolingDown) &&
		  KeracholePvE.Cooldown.WillHaveOneCharge(AddersgallTime)) ||
		 Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedKeracholeReserveSeconds, KeracholePvE.ID));

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
		!WardActive &&
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
		return !target.HasStatus(false, StatusID.Krasis) && BeirutaTarget.TryUseOn(KrasisPvE, target, out act);
	}

	private bool TryDruocholeOn(IBattleChara target, bool ignoreReserve, out IAction? act)
	{
		act = null;
		return CanSpendAddersgall(ignoreReserve) && BeirutaTarget.TryUseOn(DruocholePvE, target, out act);
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
			BeirutaTarget.TryUseOn(TaurocholePvE, target, out act);
	}

	private bool TryHaimaOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(false, StatusID.Haima) && BeirutaTarget.TryUseOn(HaimaPvE, target, out act);
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

		if (TryKrasisOn(tank, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool PrognosisQueued =>
		HasEukrasia &&
		(_EukrasiaActionAim == EukrasianPrognosisPvE || _EukrasiaActionAim == EukrasianPrognosisIiPvE);

	#endregion

	#region Steps

	private const float TankbusterLeadSeconds = BeirutaIncoming.TankbusterLeadSeconds;

	private static bool TankbusterSoon => BeirutaIncoming.TankbusterWithin(TankbusterLeadSeconds);

	public bool PlanAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public bool SpecialAbility(IAction nextGCD, out IAction? act)
	{
		if (TryAccretionAbility(out act) || TryZoePepsisReshield(out act))
		{
			return true;
		}

		if (DoomTarget(SingleHealRange) is { } doomed && TryDruocholeOn(doomed, true, out act))
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

		return ZoeModeValue == ZoeUsage.AutoPneuma &&
			ZoePvE.EnoughLevel &&
			!ZoePvE.Cooldown.IsCoolingDown &&
			nextGCD.IsTheSameTo(false, PneumaPvE) &&
			ZoePvE.CanUse(out act);
	}

	public bool BurstAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (IsBurst && PhlegmaOpenerAllowed && PsychePvE.CanUse(out act))
		{
			return true;
		}

		return ShouldUsePotionInCombat && UseBurstMedicine(out act);
	}

	public bool ResourceAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return Addersgall <= 1 && RhizomataPvE.CanUse(out act);
	}

	public bool AreaHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (!HoldAoeForCoHealer(PhysisRange))
		{
			if (!IxocholeFirst &&
				AoeNeeded(PhysisRange, PhysisHp) &&
				!PlanOwnsPhysis &&
				(PhysisIiPvE.CanUse(out act) || (!PhysisIiPvE.EnoughLevel && PhysisPvE.CanUse(out act))))
			{
				return true;
			}

			if (IxocholeCanCover && IxocholePvE.CanUse(out act))
			{
				return true;
			}
		}

		return TryPepsis(out act);
	}

	public bool SpotHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TopUpAllTarget(SingleHealRange) is { } topUp &&
			(TryDruocholeOn(topUp, false, out act) || TryTaurocholeOn(topUp, false, out act)))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m)) is { } undeadTank &&
			(TryKrasisOn(undeadTank, out act) ||
			 TryTaurocholeOn(undeadTank, true, out act) ||
			 TryDruocholeOn(undeadTank, true, out act) ||
			 TrySoteriaFor(undeadTank, out act)))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && HealHp(m) < KrasisTankHp) is { } lowTank &&
			TryKrasisOn(lowTank, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, m => m.HasStatus(true, StatusID.Kardion) && SpotHealWorthy(m) && SpotHealOk(m) && HealHp(m) < SoteriaHealValue) is { } soteriaHolder &&
			TrySoteriaFor(soteriaHolder, out act))
		{
			return true;
		}

		if (CoHealerInParty && CountNonTankInjured(SingleHealRange, TopUpHp) > CoHealerSpreadNonTankCount)
		{
			return false;
		}

		if (FindLowest(SingleHealRange, m => TaurocholeAllowedOn(m) && SpotHealWorthy(m) && SpotHealOk(m) && HealHp(m) < HealSingleTaurocholeHealValue) is { } taurocholeTarget &&
			TryTaurocholeOn(taurocholeTarget, false, out act))
		{
			return true;
		}

		return FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && HealHp(m) < HealSingleDruocholeHealValue) is { } druocholeTarget &&
			TryDruocholeOn(druocholeTarget, false, out act);
	}

	public bool TankHelpAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TankbusterSoon &&
			TryMitigationSupport(FindLowest(SingleHealRange, IsFreshMitigatingTank) ?? EnemyAggroTarget ?? FindLowest(SingleHealRange, static m => IsTankMember(m)), out act))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsFreshMitigatingTank(member) && TryMitigationSupport(member, out act))
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
		if ((InCombat || !HasKardia) && KardiaPvE.CanUse(out act))
		{
			return true;
		}

		if (TrySwapKardiaBack(out act))
		{
			return true;
		}

		if (CanRunHealEngine && !AccretionUp)
		{
			if (PrognosisQueued &&
				FindLowest(SingleHealRange, static m => HealHp(m) < 1f) is { } lowest &&
				TryKrasisOn(lowest, out act))
			{
				return true;
			}

			if (AddersgallOvercapSoon &&
				(FindLowest(SingleHealRange, IsTankMember) ?? FindLowest(SingleHealRange, static _ => true)) is { } dumpTarget &&
				TryDruocholeOn(dumpTarget, false, out act))
			{
				return true;
			}
		}

		return !HasHealingLockout && !AccretionUp && TrySwapKardiaToInjured(out act);
	}

	// A planned Eukrasian Prognosis is in its window and wanted: the party lacks shields, or the row is Force.
	private bool PlannedShieldDue =>
		UsePlannedShields &&
		!ShieldHeldForPlannedZoe &&
		Updaters.BeirutaTimelineUpdater.PlannedWithin(0f, (uint)ActionID.EukrasianPrognosisIiPvE, (uint)ActionID.EukrasianPrognosisPvE) &&
		(MostPartyLacksEukrasianShield() ||
		 Updaters.BeirutaTimelineUpdater.PlannedForcedNow((uint)ActionID.EukrasianPrognosisIiPvE, (uint)ActionID.EukrasianPrognosisPvE));

	private IBaseAction? PlannedPrognosis =>
		EukrasianPrognosisIiPvE.EnoughLevel && EukrasianPrognosisIiPvE.IsEnabled ? EukrasianPrognosisIiPvE :
		EukrasianPrognosisPvE.EnoughLevel && EukrasianPrognosisPvE.IsEnabled ? EukrasianPrognosisPvE :
		null;

	// Points the Eukrasia chain at the planned Prognosis, replacing any other aim (the Eukrasia buff is shared).
	private bool AimPlannedPrognosis(out IBaseAction? prognosis)
	{
		prognosis = PlannedShieldDue ? PlannedPrognosis : null;
		if (prognosis == null)
		{
			return false;
		}

		if (_EukrasiaActionAim != prognosis)
		{
			_EukrasiaActionAim = null;
			_diagnosisTarget = null;
			SetEukrasia(prognosis);
		}

		return true;
	}

	// GCD side of the chain: if the GCD comes up before an ability slot, aim and run the planned shield from here.
	public bool PlanGCD(out IAction? act)
	{
		act = null;
		if (!AimPlannedPrognosis(out var prognosis))
		{
			return false;
		}

		return prognosis == EukrasianPrognosisIiPvE ? DoEukrasianPrognosisIi(out act) : DoEukrasianPrognosis(out act);
	}

	public bool SpecialGCD(out IAction? act)
	{
		act = null;
		if (DoomTarget(SingleHealRange) is { } doomed &&
			!HasEukrasia &&
			BeirutaTarget.TryUseOn(DiagnosisPvE, doomed, out act))
		{
			return true;
		}

		return BeirutaAccretion.FindTarget(PartyMembers) is { } target &&
			!HasEukrasia &&
			!HasSwift &&
			BeirutaTarget.TryUseOn(DiagnosisPvE, target, out act);
	}

	public bool WardGCD(out IAction? act) => TryWardModeGCD(out act);

	public bool AreaHealGCD(out IAction? act)
	{
		act = null;
		if (HasZoe)
		{
			if (ZoeModeValue == ZoeUsage.AutoPneuma || PartyMembersAverHP < ZoePneumaHealValue)
			{
				if (PneumaPvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (PlannedZoe
				? MostPartyLacksEukrasianShield() || StatusHelper.PlayerStatusTime(true, StatusID.Zoe) < PlannedZoeLastCallSeconds
				: !HasEukrasianPrognosis)
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

		if (!CanRunHealEngine || BeirutaMove.RaiseSwiftHeld)
		{
			return false;
		}

		return !HoldAoeForCoHealer(PneumaHealRange) &&
			(!(IxocholeCanCover || PhysisCanCover) || AnyCritical(PneumaHealRange)) &&
			AoeNeeded(PneumaHealRange, PneumaHp) &&
			PneumaPvE.CanUse(out act);
	}

	public bool SpotHealGCD(out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine ||
			BeirutaMove.RaiseSwiftHeld ||
			ZoeReservedForPlan ||
			IsPartyBurst ||
			HasMedicated ||
			HasPhilosophia ||
			!EukrasianDiagnosisPvE.EnoughLevel ||
			!EukrasianDiagnosisPvE.IsEnabled ||
			(DiagnosisThrottled && !(HasEukrasia && _EukrasiaActionAim == EukrasianDiagnosisPvE)))
		{
			return false;
		}

		if (DiagnosisTankTarget() is not { } tank)
		{
			return false;
		}

		QueueDiagnosis(tank);
		return DoEukrasianDiagnosis(out act);
	}

	private IBattleChara? DiagnosisTankTarget()
	{
		if (Addersting >= 2 || WardActive || AccretionUp || !BeirutaMove.Sufficient)
		{
			return null;
		}

		return FindLowest(SingleHealRange, static m => IsFreshBigMitigatingTank(m) && CanDiagnose(m)) ??
			(TankbusterSoon ? FindLowest(SingleHealRange, static m => IsTankMember(m) && HasTankMitigation(m) && CanDiagnose(m)) : null);
	}

	#endregion

	#region Hooks

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act) =>
		BeirutaHealerFlow.Ability(this, nextGCD, out act) || base.EmergencyAbility(nextGCD, out act);

	[RotationDesc(ActionID.PsychePvE, ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (ShouldSwiftcastForMovement(nextGCD) && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		return BurstAbility(nextGCD, out act) || base.AttackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.KardiaPvE, ActionID.RhizomataPvE)]
	protected override bool GeneralAbility(IAction nextGCD, out IAction? act) =>
		LowPriorityAbility(nextGCD, out act) || ResourceAbility(nextGCD, out act) || base.GeneralAbility(nextGCD, out act);

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act) =>
		BeirutaHealerFlow.UrgentGcd(this, out act) || base.EmergencyGCD(nextGCD, out act);

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
		TankbusterSoon;

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

		// A planned shield comes first and overrides any other aim.
		if (AimPlannedPrognosis(out var planned))
		{
			return !HasEukrasia && planned!.CanUse(out _) && EukrasiaPvE.CanUse(out act);
		}

		if (!HasEukrasia && DefenseSignalActive && ShieldEukrasiaOnRetryCooldown)
		{
			return false;
		}

		if (HasEukrasia &&
			_EukrasiaActionAim != null &&
			!MergedStatus.HasFlag(AutoStatus.DefenseArea) &&
			!TankbusterSoon)
		{
			return false;
		}

		if (EukrasianPrognosisIiPvE.EnoughLevel
			&& EukrasianPrognosisIiPvE.IsEnabled
			&& MergedStatus.HasFlag(AutoStatus.DefenseArea)
			&& MostPartyLacksEukrasianShield()
			&& !ShieldHeldForPlannedZoe
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
			&& !DiagnosisThrottled
			&& TankbusterSoon
			&& DiagnosisTankTarget() is { } defenseTank)
		{
			SetEukrasia(EukrasianDiagnosisPvE);
			_diagnosisTarget = defenseTank;
			selected = true;
		}
		else if (!ShouldSkipDotForTTK
			&& EukrasianDyskrasiaPvE.EnoughLevel
			&& EukrasianDyskrasiaPvE.IsEnabled
			&& !TankbusterSoon
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
			&& !TankbusterSoon
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
			&& !TankbusterSoon
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
			&& !TankbusterSoon
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

		if (ShieldHeldForPlannedZoe)
		{
			return false;
		}

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

		if (ShieldHeldForPlannedZoe)
		{
			return false;
		}

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

		if (ZoeReservedForPlan)
		{
			return false;
		}

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

		return BeirutaTarget.TryUseOn(EukrasianDiagnosisPvE, _diagnosisTarget!, out act);
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

		var target = BeirutaAccretion.FindTarget(PartyMembers);
		if (target == null)
		{
			return false;
		}

		return BeirutaTarget.TryUseOn(KrasisPvE, target, out act)
			|| BeirutaTarget.TryUseOn(DruocholePvE, target, out act)
			|| BeirutaTarget.TryUseOn(TaurocholePvE, target, out act);
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

		if (!(WardActive || BeirutaAccretion.GuardActive(PartyMembers)) || HasHealingLockout || ShieldHeldForPlannedZoe)
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

		var shieldWanted =
			(WardActive && BeirutaWard.CountNonTanksMissing(PartyMembers, PrognosisRange, static m => HasEukrasianShield(m)) > 0) ||
			(BeirutaAccretion.GuardActive(PartyMembers) &&
			 BeirutaWard.CountMissing(PartyMembers, PrognosisRange, static m => HasEukrasianShield(m)) >= BeirutaWard.UnshieldedPartyCount);

		if (prognosis == null || !shieldWanted)
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

		if (BeirutaMove.RaiseSwiftHeld)
		{
			return base.GeneralGCD(out act);
		}

		if (BeirutaHealerFlow.HealGcd(this, out act))
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

		if (WeaveRoomWanted && Addersting > 0 && ToxikonPvE.CanUse(out act))
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
