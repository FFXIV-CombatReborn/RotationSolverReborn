using static RotationSolver.Engine.Helpers.BeirutaParty;

namespace RotationSolver.Rotations.Healer.Astrologian;

[Rotation("BeirutaAST", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Astrologian. Damage and healing are automatic. Planned cooldowns (mitigation, HoTs, Lightspeed and the like) follow the built-in CD plan when there is one (see the CD Plan section); otherwise use your Boss Mod / BMR CD planner or press them yourself, with Intercept set to GCD only and ability intercept unticked. Questions? Ask @Beiruta on the RSR Discord.")]
[SourceCode(Path = "main/Rotations/Healer/Astrologian/BeirutaAST.cs")]
[ExtraRotation]

public sealed class BeirutaAST : AstrologianRotation, Updaters.ICdPlanRotation, IBeirutaHealContext, IBeirutaHealer
{
	#region Config Options

	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Use built-in CD plan: planned cooldowns (mitigation, HoTs, Lightspeed and the like) follow the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press those cooldowns.")]
	public BeirutaPlanSource CdPlanFrom { get; set; } = BeirutaPlanSource.BuiltIn;

	public bool UseTimelinePlan => CdPlanFrom == BeirutaPlanSource.BuiltIn;

	[RotationConfig(CombatType.PvE, Name = "    > Show CD Plan timeline", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"A small window like the action timeline, showing the plan's actions coming up in the next 10s. It shows in a duty that has a CD plan; hover the window for lock / close.")]
	public bool ShowPlanTimeline { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use recommended settings for the plan", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"When a built-in plan loads, the settings it recommends are applied, like a preset. Changing a setting afterwards (e.g. your Ward Mode macro) works as usual.\n" +
		"Your saved settings are not changed by the plan: they show as you set them and come back once the plan unloads or this is off.")]
	public bool UsePlanSettings { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Helios Conjunction", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan's Helios Conjunctions are cast in their window, and no automatic Helios GCD 15s around them.\n" +
		"Off: they are skipped; Helios Conjunction goes by party HP and after Neutral Sect / Horoscope.")]
	public bool UsePlannedHeliosConjunction { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Earthly Star", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: Earthly Star follows the plan, including the countdown one, and goes where RSR places ground heals (Beneficial positions list); [Damage] Earthly Star and [Prepull] Earthly Star at are ignored.\n" +
		"Off: it is placed whenever it is ready.\n" +
		"Stellar Detonation always goes by party HP under Giant Dominance.")]
	public bool UsePlannedEarthlyStar { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions =>
	[
		((uint)ActionID.HeliosConjunctionPvE, nameof(UsePlannedHeliosConjunction)),
		((uint)ActionID.EarthlyStarPvE, nameof(UsePlannedEarthlyStar)),
	];

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		(UsePlannedHeliosConjunction || actionId != (uint)ActionID.HeliosConjunctionPvE) &&
		(UsePlannedEarthlyStar || actionId != (uint)ActionID.EarthlyStarPvE);

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) =>
		actionId != (uint)ActionID.HeliosConjunctionPvE ? null
		: PlannedPartnerPending(NeutralSectPvE, HasNeutralSect) ? "waiting for the planned Neutral Sect"
		: PlannedPartnerPending(HoroscopePvE, HasHoroscope || HasHoroscopeHelios) ? "waiting for the planned Horoscope"
		: null;

	private static bool PlannedPartnerPending(IBaseAction partner, bool active) =>
		!active &&
		partner.EnoughLevel &&
		!partner.Cooldown.IsCoolingDown &&
		Updaters.TimelineUpdater.PlannedWithin(PairedHeliosWaitSeconds, partner.ID);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		Updaters.TimelineUpdater.HoldQueuedPlannedAction(action.ID);

	private float? CountdownEarthlyStarAt
	{
		get
		{
			if (UsePlannedEarthlyStar &&
				Updaters.TimelineUpdater.PlanOption("CountdownEarthlyStar") is { } v &&
				float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
			{
				return seconds > 0 ? seconds : null;
			}

			return EarthlyStarMode == EarthlyStarUsage.OnCooldown ? UseEarthlyStarTime : null;
		}
	}

	private bool EarthlyStarOnCooldown =>
		EarthlyStarMode == EarthlyStarUsage.OnCooldown ||
		(!UsePlannedEarthlyStar && Updaters.TimelineUpdater.HasPlan);

	#endregion

	#region Manual / BMR / VBM Triggers

	[RotationConfig(CombatType.PvE, Name = "    > [Macrocosmos] Custom triggers (off = CD planner / manual)", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.ManualPlan, Tooltip =
		"A trigger is something you do that makes AST cast Macrocosmos.\n" +
		"In DMU, triggers are blocked while 2+ players have Accretion, or while Earth Resistance Down II is up with one left.\n" +
		"Only used with BMR / VBM / Manual; the built-in CD plan presses Macrocosmos itself.")]
	public bool UseMacrocosmosSignals { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Surecast", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSurecast { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Sprint", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSprint { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Synastry (Synastry is then never automatic)", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSynastry { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Celestial Intersection on yourself (keeps a charge)", Parent = nameof(UseMacrocosmosSignals), Tooltip =
		"While Macrocosmos is ready within 30s, one Intersection charge is kept for the trigger.")]
	public bool MacrocosmosSignalIntersection { get; set; } = false;

	private bool MacrocosmosSignalsAllowed => !UseTimelinePlan;
	private bool MacrocosmosSignalsOn => UseMacrocosmosSignals && MacrocosmosSignalsAllowed;
	private bool SignalSurecast => MacrocosmosSignalsOn && MacrocosmosSignalSurecast;
	private bool SignalSprint => MacrocosmosSignalsOn && MacrocosmosSignalSprint;
	private bool SignalSynastry => MacrocosmosSignalsOn && MacrocosmosSignalSynastry;
	private bool SignalIntersection => MacrocosmosSignalsOn && MacrocosmosSignalIntersection;

	#endregion

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps every non-tank full.\n" +
		"- Helios Conjunction / Helios as soon as one non-tank is below the heal's full-use line (about 85-90%). Plain Helios is only used in Ward Mode.\n" +
		"- No waiting for AoE oGCDs, Giant Dominance or Macrocosmos; thresholds use real HP and ignore the co-healer and HoTs.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[More settings] Show", Tooltip =
		"Pick a page to show its detailed settings. This only changes what is shown.")]
	public BeirutaSettingsPage MoreSettings { get; set; } = BeirutaSettingsPage.Hide;

	private bool WardActive => WardMode || Updaters.TimelineUpdater.PlannedWardMode;

	bool IBeirutaHealContext.WardMode => WardActive;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Aspected Benefic + Celestial Intersection on the stance tank at about 5s, or Neutral Sect + Helios Conjunction for the party.")]
	public CountdownShieldStrategy CountdownShield { get; set; } = CountdownShieldStrategy.None;

	[Range(4, 20, ConfigUnitType.Seconds)]
	[RotationConfig(CombatType.PvE, Name = "[Prepull] Earthly Star at", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Seconds left on the countdown when Earthly Star is placed. Needs Earthly Star = On cooldown. A built-in plan with planned Earthly Star sets its own time.")]
	public float UseEarthlyStarTime { get; set; } = 4;

	public enum CountdownShieldStrategy : byte
	{
		[Description("None")]
		None,

		[Description("Aspected Benefic + Celestial Intersection on stance tank (5s)")]
		TankAspectedBeneficIntersection,

		[Description("Neutral Sect + Helios Conjunction")]
		NeutralSectHeliosConjunction,
	}

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Divination", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"When the first Divination goes out (needs AutoBurst).\n" +
		"0s: sent just before the countdown ends, then Combust, then Lightspeed in its two weave windows.")]
	public DivinationOpenerTiming DivinationOpener { get; set; } = DivinationOpenerTiming.ThreeGcd;

	public enum DivinationOpenerTiming : byte
	{
		[Description("0s")]
		ZeroSeconds,

		[Description("2.2s (1 GCD)")]
		OneGcd,

		[Description("5s (2 GCD)")]
		TwoGcd,

		[Description("7s (3 GCD)")]
		ThreeGcd,
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

	[RotationConfig(CombatType.PvE, Name = "[Damage] Delay Oracle 5s after Divination", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"Burst oGCD order: Divination > Lightspeed > potion > Balance / Spear > Lord of Crowns > Draw > Oracle.")]
	public bool DelayOracleForAlignment { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Earthly Star", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"On cooldown: placed in the countdown and again whenever it is ready. Healing changes while Earthly / Giant Dominance is up.\n" +
		"A built-in plan with planned Earthly Star places it instead.")]
	public EarthlyStarUsage EarthlyStarMode { get; set; } = EarthlyStarUsage.OnCooldown;

	public enum EarthlyStarUsage : byte
	{
		[Description("On cooldown (and countdown)")]
		OnCooldown,

		[Description("Manual / CD planner")]
		Manual,
	}

	#endregion

	#region Lightspeed

	[RotationConfig(CombatType.PvE, Name = "[Lightspeed] Spare charge", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"One charge is always kept for burst and used about 5s before Divination. This sets what the other charge does.\n" +
		"A built-in plan that has Lightspeed uses the other charge itself.")]
	public LightspeedOtherChargeUsage LightspeedOtherCharge { get; set; } = LightspeedOtherChargeUsage.Manual;

	public enum LightspeedOtherChargeUsage : byte
	{
		[Description("Manual")]
		Manual,

		[Description("Movement")]
		Movement,

		[Description("Movement, and before capping")]
		MovementAndCap,
	}

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Combust refresh", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"While moving, refreshes Combust early as an instant GCD.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"Instant options while moving: Lightspeed > early Combust > Aspected Benefic (tanks first), then Swiftcast.\n" +
		"Nothing extra is used while Lightspeed or Swiftcast is up.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > When to use it", Parent = nameof(UseSwiftcastForMovement), Tooltip =
		"Last resort: only when no instant option is available. First: before the instant options.\n" +
		"Raise and AoE heal GCDs only: kept for raise and for moving Helios GCDs.")]
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
		"Off: each area heal waits until 3+ players are low enough that it won't be wasted, worked out from its potency, Neutral Sect, the co-healer's smallest GCD heal and incoming HoT ticks.\n" +
		"Off also means: Helios Conjunction after Horoscope, Essential Dignity on tanks below 80% / 45% / 30% (3 / 2 / 1 charges), Synastry second target below 50%.\n" +
		"On: each area heal fires when 3+ players in range are below the value set here.\n" +
		"Ward Mode ignores these values and uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Helios Conjunction after Horoscope", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"After you press Horoscope, the next Helios Conjunction upgrades it to Horoscope Helios.")]
	public bool AutoUpgradeHoroscope { get; set; } = true;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Lady of Crowns: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float LadyOfHeals { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Stellar Detonation: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float StellarDetonationHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Horoscope: area heal when 3+ Horoscope holders below", Parent = nameof(UseCustomHPThreshold))]
	public float HoroscopeHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Celestial Opposition: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float CelestialOppositionHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Helios Conjunction: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float HeliosConjunctionHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Helios: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float HeliosHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 3 charges: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignityThird { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 2 charges: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignitySecond { get; set; } = 0.45f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 1 charge: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignityLast { get; set; } = 0.3f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Synastry second target: below", Parent = nameof(UseCustomHPThreshold))]
	public float SynastryHeal { get; set; } = 0.5f;

	private bool AutoUpgradeHoroscopeValue => !UseCustomHPThreshold || AutoUpgradeHoroscope;
	private float EssentialDignityThirdValue => UseCustomHPThreshold ? EssentialDignityThird : 0.8f;
	private float EssentialDignitySecondValue => UseCustomHPThreshold ? EssentialDignitySecond : 0.45f;
	private float EssentialDignityLastValue => UseCustomHPThreshold ? EssentialDignityLast : 0.3f;
	private float SynastryHealValue => UseCustomHPThreshold ? SynastryHeal : 0.5f;

	#endregion

	#endregion

	#region Constants / Fields

	private const long DivinationFirst5sMs = 5_000;
	private const float BallparkPercent = 0.08f;
	private const float DivinationCombustRefreshSeconds = 11f;
	private const float CombustWorthRefreshSeconds = 15f;
	private const float LightspeedLocksSwiftcastSeconds = 18f;
	private const float SwiftcastLocksLightspeedSeconds = 3f;
	private const float LightspeedBurstLeadSeconds = 5f;

	private long _divinationUsedAtMs;
	private float _lastLightspeedUseCombatTime = float.MinValue;
	private float _lastSwiftcastUseCombatTime = float.MinValue;
	private DateTime _neutralSectStartedAt = DateTime.MinValue;
	private bool _neutralSectWasUp;
	private bool _lastWardMode;
	private readonly BeirutaRaidwide _raidwide = new();

	#endregion

	#region Burst / Opener

	private float DivinationOpenerSeconds => DivinationOpener switch
	{
		DivinationOpenerTiming.ZeroSeconds => 0f,
		DivinationOpenerTiming.OneGcd => 2.2f,
		DivinationOpenerTiming.TwoGcd => 5.1f,
		_ => 7.2f,
	};

	private bool IsOpen => InCombat && CombatTime < DivinationOpenerSeconds;

	private bool DivinationOpenerPassed => InCombat && !CombatElapsedLess(DivinationOpenerSeconds);

	private bool InFirst5sAfterDivination =>
		_divinationUsedAtMs != 0 &&
		Environment.TickCount64 - _divinationUsedAtMs < DivinationFirst5sMs;

	private bool OracleGatedByDivination => DelayOracleForAlignment && InFirst5sAfterDivination;

	private float DivIn =>
		DivinationPvE.Cooldown.CurrentCharges >= 1
			? 0f
			: DivinationPvE.Cooldown.RecastTimeRemainOneCharge;

	private float DivinationDueIn
	{
		get
		{
			if (!DivinationPvE.EnoughLevel)
			{
				return float.MaxValue;
			}

			var openerWait = InCombat ? MathF.Max(0f, DivinationOpenerSeconds - CombatTime) : DivinationOpenerSeconds;
			return MathF.Max(DivIn, openerWait);
		}
	}

	private bool BurstPrep => DivinationPvE.EnoughLevel && DivinationDueIn <= LightspeedBurstLeadSeconds;

	private bool BurstCardsAllowed => !DivinationPvE.EnoughLevel || HasDivination;

	private bool ShouldUsePotionInCombat =>
		!OpenerWeaveHold &&
		BeirutaPotion.Ready(UsePotion, PotionTiming, IsBurst, BurstPrep || !DivinationPvE.EnoughLevel);

	private bool ShouldUseLightspeedForBurst =>
		IsBurst &&
		InCombat &&
		!HasLightspeed &&
		LightspeedPvE.EnoughLevel &&
		DivinationPvE.EnoughLevel &&
		(BurstPrep || InFirst5sAfterDivination);

	private bool DrawReadySoon(uint gcds) =>
		(AstralDrawPvE.EnoughLevel && AstralDrawPvE.Cooldown.WillHaveOneChargeGCD(gcds)) ||
		(UmbralDrawPvE.EnoughLevel && UmbralDrawPvE.Cooldown.WillHaveOneChargeGCD(gcds));

	private bool ShouldUseLord =>
		HasLord &&
		(HasDivination || !DivinationPvE.EnoughLevel);

	private static bool HoldingDamageCard => HasBalance || HasSpear || HasLord;

	private bool TryDraw(out IAction? act)
	{
		act = null;
		return !HoldingDamageCard &&
			(AstralDrawPvE.CanUse(out act) || UmbralDrawPvE.CanUse(out act));
	}

	#endregion

	#region Lightspeed / Movement

	private bool MovingPastLead => BeirutaMove.PastLead;

	private bool IsLightspeedLockingSwiftcastActive =>
		InCombat &&
		_lastLightspeedUseCombatTime > float.MinValue / 2 &&
		CombatTime - _lastLightspeedUseCombatTime <= LightspeedLocksSwiftcastSeconds;

	private bool IsSwiftcastLockingLightspeedActive =>
		InCombat &&
		_lastSwiftcastUseCombatTime > float.MinValue / 2 &&
		CombatTime - _lastSwiftcastUseCombatTime <= SwiftcastLocksLightspeedSeconds;

	private float LightspeedNextChargeIn =>
		LightspeedPvE.Cooldown.CurrentCharges >= LightspeedPvE.Cooldown.MaxCharges
			? 0f
			: LightspeedPvE.Cooldown.RecastTimeRemainOneCharge;

	private bool HoldLastLightspeedForDivination
	{
		get
		{
			if (!DivinationPvE.EnoughLevel ||
				DivinationDueIn > 60f ||
				BurstPrep ||
				LightspeedPvE.Cooldown.CurrentCharges != 1 ||
				HasLightspeed)
			{
				return false;
			}

			var lsMustBeBackBy = MathF.Max(0f, DivinationDueIn - LightspeedBurstLeadSeconds);
			return LightspeedNextChargeIn > lsMustBeBackBy;
		}
	}

	private void UpdateMovementCooldownTracking()
	{
		if (!InCombat)
		{
			_lastLightspeedUseCombatTime = float.MinValue;
			_lastSwiftcastUseCombatTime = float.MinValue;
			return;
		}

		if (IsLastAction(ActionID.LightspeedPvE))
		{
			_lastLightspeedUseCombatTime = CombatTime;
		}

		if (IsLastAction(ActionID.SwiftcastPvE))
		{
			_lastSwiftcastUseCombatTime = CombatTime;
		}
	}

	private bool PlanOwnsLightspeed => Updaters.TimelineUpdater.PlanContains(LightspeedPvE.ID);

	private bool LightspeedReadyForMovement =>
		!PlanOwnsLightspeed &&
		LightspeedOtherCharge != LightspeedOtherChargeUsage.Manual &&
		MovingPastLead &&
		!HasLightspeed &&
		!HasSwift &&
		LightspeedPvE.EnoughLevel &&
		!HoldLastLightspeedForDivination &&
		!IsSwiftcastLockingLightspeedActive &&
		LightspeedPvE.CanUse(out _, usedUp: LightspeedPvE.Cooldown.CurrentCharges > 1);

	private bool LightspeedCapping =>
		LightspeedOtherCharge == LightspeedOtherChargeUsage.MovementAndCap &&
		InCombat &&
		!HasLightspeed &&
		LightspeedPvE.EnoughLevel &&
		LightspeedPvE.Cooldown.MaxCharges >= 2 &&
		LightspeedPvE.Cooldown.WillHaveXCharges(LightspeedPvE.Cooldown.MaxCharges, 5f);

	private bool SwiftcastReadyForMovement =>
		UseSwiftcastForMovement &&
		MovingPastLead &&
		!HasSwift &&
		!HasLightspeed &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!BeirutaMove.RaiseSwiftHeld &&
		!IsLightspeedLockingSwiftcastActive;

	private bool MovementCombustDue(float expectedHPToLive12Seconds) =>
		EarlyDotRefreshWhenMoving &&
		BeirutaMove.Sufficient &&
		CurrentTarget != null &&
		CurrentTargetCombustMissingOrEnding(CombustWorthRefreshSeconds) &&
		CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds);

	private static IBattleChara? MovementAspectedBeneficTarget() =>
		AccretionUp ? null : FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && HealHp(m) < MovementBeneficHp && !HasSingleTargetHot(m));

	private const float MovementBeneficHp = 0.8f;

	private bool HasCheaperMovementOption =>
		LightspeedReadyForMovement ||
		MovementCombustDue(GetExpectedHpToLive12Seconds()) ||
		(AspectedBeneficPvE.EnoughLevel && AspectedBeneficPvE.IsEnabled && MovementAspectedBeneficTarget() != null);

	private bool IsMovementPreferredNextGCD(IAction nextGCD)
	{
		if (nextGCD == null)
		{
			return false;
		}

		if (nextGCD.IsTheSameTo(false, AspectedBeneficPvE, MacrocosmosPvE))
		{
			return true;
		}

		return CanUseCurrentCombust(out var combustAct) && nextGCD == combustAct;
	}

	private bool ShouldUseSwiftcastForMovement(IAction nextGCD)
	{
		if (!SwiftcastReadyForMovement || IsLastAction(ActionID.SwiftcastPvE))
		{
			return false;
		}

		if (SwiftcastMovement == SwiftcastMovementPriority.HealOnly)
		{
			return !MergedStatus.HasFlag(AutoStatus.Raise) && HeliosGcdWanted;
		}

		if (IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return SwiftcastMovement == SwiftcastMovementPriority.First || !HasCheaperMovementOption;
	}

	private bool ShouldUseLightspeedForMovement(IAction nextGCD)
	{
		if (!LightspeedReadyForMovement ||
			IsLastAction(ActionID.LightspeedPvE) ||
			IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return !(SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement);
	}

	private bool TryMovementGCD(float expectedHPToLive12Seconds, out IAction? act)
	{
		act = null;

		if (!BeirutaMove.PastGrace || HasSwift || HasLightspeed || BeirutaMove.RaiseSwiftHeld)
		{
			return false;
		}

		if ((SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement) ||
			LightspeedReadyForMovement)
		{
			return false;
		}

		if (MovementCombustDue(expectedHPToLive12Seconds) &&
			CanUseCurrentCombust(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (MovementAspectedBeneficTarget() is { } target &&
			BeirutaTarget.TryUseOn(AspectedBeneficPvE, target, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Damage / DoT Helpers

	private const float CombustInitialPotency = 0f;
	private const float CombustTickPotency = 70f;
	private const float MaleficFillerPotency = 270f;

	private static bool CombustWorthBeforeDowntime(IBaseAction combust) =>
		BeirutaDotValue.WorthBeforeDowntime(combust.Target.Target, CombustInitialPotency, CombustTickPotency, MaleficFillerPotency);

	private bool CanUseCurrentCombust(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (CombustIiiPvE.EnoughLevel && CombustIiiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			CombustWorthBeforeDowntime(CombustIiiPvE))
		{
			return true;
		}

		if (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel && CombustIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			CombustWorthBeforeDowntime(CombustIiPvE))
		{
			return true;
		}

		if (!CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel && CombustPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			CombustWorthBeforeDowntime(CombustPvE))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CanUseCurrentGravity(out IAction? act)
	{
		if (GravityIiPvE.EnoughLevel && GravityIiPvE.CanUse(out act))
		{
			return true;
		}

		if (!GravityIiPvE.EnoughLevel && GravityPvE.EnoughLevel && GravityPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CanUseCurrentMalefic(out IAction? act)
	{
		if (FallMaleficPvE.EnoughLevel && FallMaleficPvE.CanUse(out act))
		{
			return true;
		}

		if (!FallMaleficPvE.EnoughLevel && MaleficIvPvE.EnoughLevel && MaleficIvPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIvPvE.EnoughLevel && MaleficIiiPvE.EnoughLevel && MaleficIiiPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIiiPvE.EnoughLevel && MaleficIiPvE.EnoughLevel && MaleficIiPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIiPvE.Info.EnoughLevelAndQuest() && MaleficPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CurrentTargetCombustMissingOrEnding(float remainingSeconds)
	{
		if (CurrentTarget == null)
		{
			return false;
		}

		return
			(CombustIiiPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.CombustIii) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.CombustIii)))
			||
			(!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.CombustIi) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.CombustIi)))
			||
			(!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.Combust) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Combust)));
	}

	private float GetExpectedHpToLive12Seconds()
	{
		if (Player == null)
		{
			return 0f;
		}

		var alive = 0;
		foreach (var member in PartyMembers)
		{
			if (!member.IsDead)
			{
				alive++;
			}
		}

		return BallparkPercent * Player.MaxHp * Math.Max(1, alive) * 12f;
	}

	private bool CurrentTargetHasEnoughHpForCombust(float expectedHpToLive12Seconds) =>
		CurrentTarget != null &&
		CurrentTarget.CurrentHp >= expectedHpToLive12Seconds;

	#endregion

	#region Healing Engine

	private bool CanRunHealEngine => BeirutaHeal.CanRun();

	private static bool AccretionGroupHealBlocked => BeirutaHeal.GroupHealBlocked;

	private float EffectiveHpRatio(IBattleChara member) => BeirutaHeal.EffectiveHpRatio(this, member);

	private bool CountsAsInjured(IBattleChara member, float threshold) => BeirutaHeal.Injured(this, member, threshold);

	private int CountInjured(float range, float threshold) => BeirutaHeal.CountInjured(this, range, threshold);

	private bool AoeNeeded(float range, float threshold) => BeirutaHeal.AoeNeeded(this, range, threshold);

	private bool SpotHealOk(IBattleChara member) => SpotHealAllowed(member, AoeNeeded(PartyHealRange, LadyHp), PartyHealRange);

	private bool AoeGcdNeeded(float range, float threshold) => BeirutaHeal.AoeGcdNeeded(this, range, threshold);

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) => BeirutaHeal.Threshold(this, potency, customWardOff, wardOffCap);

	float IBeirutaHealContext.HealBoost => HealBoost;

	float IBeirutaHealContext.LandingHotPotency(IBattleChara member) => LandingHotPotency(member);
	private static bool HoldAoe => BeirutaHeal.HoldAoe(PartyHealRange);

	private const float SingleHealRange = 30f;
	private const float PartyHealRange = 20f;
	private const float HeliosRange = 15f;
	private const int AoeInjuredCount = 3;
	private const float CapTankHp = 0.9f;
	private const float NeutralSectTankHp = 0.9f;
	private const float LadyPotency = 400f;
	private const float HoroscopeHeliosPotency = 400f;
	private const float StellarDetonationPotency = 720f;
	private const float OppositionDirectPotency = 400f;
	private const float OppositionHotCountedPotency = 100f * BeirutaHealModel.HotCreditTicks;
	private const float OppositionCountedPotency = OppositionDirectPotency + OppositionHotCountedPotency;
	private const float HeliosPotency = 400f;
	private const float HeliosWardOffCap = 0.6f;
	private const float ConjunctionDirectPotency = 250f;
	private const float ConjunctionHotCountedPotency = 175f * BeirutaHealModel.HotCreditTicks;
	private const float ConjunctionCountedPotency = ConjunctionDirectPotency + ConjunctionHotCountedPotency;
	private const float NeutralSectBoost = 1.2f;

	private static readonly StatusID[] HeliosHotStatus =
	[
		StatusID.HeliosConjunction,
		StatusID.AspectedHelios,
	];

	private static readonly StatusID[] MacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];

	private static bool IsSelf(IBattleChara member) => member.GameObjectId == Player?.GameObjectId;

	private float LandingHotPotency(IBattleChara member)
	{
		var potency = 0f;

		if (IsHealableInRange(member, PartyHealRange) &&
			!member.HasStatus(false, StatusID.Opposition) &&
			BeirutaHealWindow.OgcdJustUsed(CelestialOppositionPvE))
		{
			potency += OppositionHotCountedPotency;
		}

		if (IsHealableInRange(member, HeliosRange) &&
			!member.HasStatus(false, HeliosHotStatus) &&
			BeirutaHealWindow.GcdJustUsed(ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE))
		{
			potency += ConjunctionHotCountedPotency;
		}

		return potency;
	}

	private int CountInjuredWithHoroscope(float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (member != null &&
				!member.IsDead &&
				!IsHealBlocked(member) &&
				member.HasStatus(true, StatusID.HoroscopeHelios) &&
				CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	private static IBattleChara? MainTank => EnemyAggroTank ?? FindLowest(SingleHealRange, static m => IsTankMember(m));

	private static float HealBoost => HasNeutralSect ? NeutralSectBoost : 1f;

	private float DirectPartThreshold(float directPotency, float countedPotency, float customWardOff) =>
		WardActive
			? BeirutaHealModel.RoundToFive(BeirutaHealModel.FullUseHp(directPotency, HealBoost))
			: AoeThreshold(countedPotency, customWardOff);

	private float LadyHp => AoeThreshold(LadyPotency, LadyOfHeals);

	private float StellarHp => AoeThreshold(StellarDetonationPotency, StellarDetonationHeal);

	private float HoroscopeHp => AoeThreshold(HoroscopeHeliosPotency, HoroscopeHeal);

	private float OppositionHp => DirectPartThreshold(OppositionDirectPotency, OppositionCountedPotency, CelestialOppositionHeal);

	private float HeliosHp => AoeThreshold(HeliosPotency, HeliosHeal, HeliosWardOffCap);

	private float ConjunctionHp => DirectPartThreshold(ConjunctionDirectPotency, ConjunctionCountedPotency, HeliosConjunctionHeal);

	private static bool AoeHealGcdPending =>
		BeirutaHealWindow.GcdJustUsed(ActionID.HeliosPvE, ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE);

	private const float HeliosHotRefreshSeconds = 5f;

	private static bool HeliosWithHot => HasNeutralSect || !PartyHasHeliosHot;

	private static bool PartyHasHeliosHot
	{
		get
		{
			if (BeirutaHealWindow.GcdJustUsed(ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE))
			{
				return true;
			}

			var total = 0;
			var withHot = 0;
			foreach (var member in PartyMembers)
			{
				if (!IsHealableInRange(member, HeliosRange))
				{
					continue;
				}

				total++;
				if (member.StatusTime(true, HeliosHotStatus) > HeliosHotRefreshSeconds)
				{
					withHot++;
				}
			}
			return total > 0 && withHot * 2 >= total;
		}
	}

	private static int CountWithoutNeutralSectShield(float range)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !member.HasStatus(true, StatusID.NeutralSect_1921))
			{
				count++;
			}
		}
		return count;
	}

	private bool EssentialDignityReady =>
		EssentialDignityPvE.EnoughLevel &&
		EssentialDignityPvE.IsEnabled &&
		EssentialDignityPvE.Cooldown.HasOneCharge;

	private float EssentialDignityThreshold => EssentialDignityPvE.Cooldown.CurrentCharges switch
	{
		>= 3 => EssentialDignityThirdValue,
		2 => EssentialDignitySecondValue,
		_ => EssentialDignityLastValue,
	};

	private bool AoeOgcdHealReady =>
		(HasLady && LadyOfCrownsPvE.IsEnabled) ||
		HasGiantDominance ||
		HasHoroscopeHelios ||
		(CelestialOppositionPvE.EnoughLevel && CelestialOppositionPvE.IsEnabled && !CelestialOppositionPvE.Cooldown.IsCoolingDown);

	private static bool HasHoroscopeHelios => StatusHelper.PlayerHasStatus(true, StatusID.HoroscopeHelios);

	private static bool HasHoroscope => StatusHelper.PlayerHasStatus(true, StatusID.Horoscope);

	#endregion

	#region Macrocosmos Signal

	private const float SignalFreshSeconds = 5f;
	private const float SynastryDuration = 20f;
	private const float IntersectionDuration = 30f;
	private const float MacrocosmosReserveSeconds = 30f;

	private static bool PlayerStatusFresh(StatusID status, float duration) =>
		StatusHelper.PlayerHasStatus(true, status) &&
		StatusHelper.PlayerStatusTime(true, status) > duration - SignalFreshSeconds;

	private bool MacrocosmosSignalActive =>
		(SignalSurecast && StatusHelper.PlayerHasStatus(true, StatusID.Surecast)) ||
		(SignalSprint && StatusHelper.PlayerHasStatus(true, StatusID.Sprint)) ||
		(SignalSynastry && PlayerStatusFresh(StatusID.Synastry, SynastryDuration)) ||
		(SignalIntersection && PlayerStatusFresh(StatusID.Intersection, IntersectionDuration));

	private bool MacrocosmosReadySoon =>
		MacrocosmosPvE.EnoughLevel &&
		MacrocosmosPvE.Cooldown.WillHaveOneCharge(MacrocosmosReserveSeconds);

	private bool TryMacrocosmosSignal(out IAction? act)
	{
		act = null;
		return MacrocosmosSignalActive &&
			!HasMacrocosmos &&
			BeirutaAccretion.MicrocosmosSafe(PartyMembers) &&
			MacrocosmosPvE.CanUse(out act, skipAoeCheck: true);
	}

	#endregion

	#region Single-Target Helpers

	private static bool HasCelestialIntersection(IBattleChara target) => target.HasStatus(true, StatusID.Intersection);

	private static bool HasAspectedBeneficFromSelf(IBattleChara target) => target.HasStatus(true, StatusID.AspectedBenefic);

	private bool IntersectionAllowedOn(IBattleChara target) =>
		!IsHealBlocked(target) &&
		!HasCelestialIntersection(target) &&
		!(SignalIntersection && IsSelf(target)) &&
		!(SignalIntersection && MacrocosmosReadySoon && CelestialIntersectionPvE.Cooldown.CurrentCharges < 2);

	private bool TryIntersectionOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return IntersectionAllowedOn(target) &&
			BeirutaTarget.TryUseOn(CelestialIntersectionPvE, target, out act, usedUp: true);
	}

	private bool TryExaltationOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !IsHealBlocked(target) &&
			!target.HasStatus(true, StatusID.Exaltation) &&
			BeirutaTarget.TryUseOn(ExaltationPvE, target, out act);
	}

	private bool TryTankMitigation(IBattleChara? tank, out IAction? act)
	{
		act = null;

		if (tank == null || !IsTankMember(tank) || IsHealBlocked(tank) || !IsHealableInRange(tank, SingleHealRange))
		{
			return false;
		}

		return TryIntersectionOn(tank, out act) || TryExaltationOn(tank, out act);
	}

	private bool TryRaidwideIntersection(out IAction? act)
	{
		act = null;
		var target = FindLowest(SingleHealRange, m => !IsTankMember(m) && IntersectionAllowedOn(m));
		return target != null && TryIntersectionOn(target, out act);
	}

	private bool SaveDignityForAccretion =>
		BeirutaAccretion.GuardActive(PartyMembers) ||
		EssentialDignityPvE.Cooldown.CurrentCharges <= Updaters.TimelineUpdater.KeepCharges(EssentialDignityPvE.ID);

	private bool TryEssentialDignityOn(IBattleChara target, out IAction? act) =>
		BeirutaTarget.TryUseOn(EssentialDignityPvE, target, out act, usedUp: true);

	private bool DignitySpareForSpot =>
		EssentialDignityPvE.Cooldown.CurrentCharges > BeirutaAccretion.ToFillCount(PartyMembers);

	#endregion

	#region Countdown Logic

	private readonly BeirutaOpenerHold _openerHold = new();

	private bool OpenerWeaveHold => _openerHold.Active(CombustIiiPvE, CombustIiPvE, CombustPvE);

	private readonly BeirutaCountdownDelay _countdownDelay = new();

	private bool TryOpenerDivination(float remainTime, out IAction? act)
	{
		act = null;

		if (DivinationOpener != DivinationOpenerTiming.ZeroSeconds ||
			!IsBurst ||
			remainTime >= BeirutaOpenerHold.SendAtCountdownSeconds ||
			ECommons.GameHelpers.Player.Object is not { IsCasting: false } ||
			!DivinationPvE.CanUse(out act, skipTTKCheck: true))
		{
			return false;
		}

		_openerHold.Start();
		_divinationUsedAtMs = Environment.TickCount64;
		return true;
	}

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

		if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead && MaleficPvE.CanUse(out var act))
		{
			return act;
		}

		if (TryOpenerDivination(remainTime, out act))
		{
			return act;
		}

		if (remainTime < 3f &&
			UsePotion &&
			PotionTiming == BeirutaPotTiming.Opener &&
			UseBurstMedicine(out act))
		{
			return act;
		}

		if (CountdownEarthlyStarAt is { } starAt &&
			remainTime < starAt &&
			EarthlyStarPvE.CanUse(out act, skipTTKCheck: true))
		{
			return act;
		}

		if (TryCountdownShield(remainTime, out act))
		{
			return act;
		}

		return remainTime < 30 && !HoldingDamageCard && AstralDrawPvE.CanUse(out act)
			? act
			: base.CountDownAction(remainTime);
	}

	private bool TryCountdownShield(float remainTime, out IAction? act)
	{
		act = null;

		switch (CountdownShield)
		{
			case CountdownShieldStrategy.TankAspectedBeneficIntersection:
				if (BeirutaWard.StanceTank(PartyMembers, SingleHealRange) is not { } tank)
				{
					return false;
				}

				if (remainTime is < 5.5f and > 4.5f &&
					!HasAspectedBeneficFromSelf(tank) &&
					BeirutaTarget.TryUseOn(AspectedBeneficPvE, tank, out act))
				{
					return true;
				}

				return remainTime is < 5f and > 2f && TryIntersectionOn(tank, out act);

			case CountdownShieldStrategy.NeutralSectHeliosConjunction:
				if (remainTime is < 9f and > 7f && !HasNeutralSect && NeutralSectPvE.CanUse(out act))
				{
					return true;
				}

				return remainTime is < 7f and > 4.5f && HasNeutralSect && !NeutralSectHeliosDone && TryHeliosGCD(true, out act);

			default:
				return false;
		}
	}

	#endregion

	#region Healing Abilities

	private bool TrySynastryPairing(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (SignalSynastry ||
			HasSynastry ||
			nextGCD == null ||
			!nextGCD.IsTheSameTo(false, BeneficIiPvE))
		{
			return false;
		}

		var healTargetId = BeneficIiPvE.Target.Target?.GameObjectId;
		var threshold = SynastryHealValue;
		var bond =
			FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && m.GameObjectId != healTargetId && HealHp(m) < threshold);

		return bond != null && BeirutaTarget.TryUseOn(SynastryPvE, bond, out act);
	}

	#endregion

	#region Utility Cards

	private const float UtilityCardTankHp = 0.7f;
	private const uint UtilityCardDumpGcds = 2;

	private bool UtilityCardsHeld => IsOpen || HasDivination || OpenerWeaveHold;

	private static bool HasCardStatus(IBattleChara member) =>
		member.HasStatus(false, StatusID.TheBole_3890, StatusID.TheSpire_3892, StatusID.TheEwer_3891, StatusID.TheArrow_3888);

	private bool TryArrowBeforeHelios(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (!HasArrow ||
			IsOpen ||
			OpenerWeaveHold ||
			nextGCD == null ||
			!nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, AspectedHeliosPvE, HeliosPvE) ||
			Player is not { } self ||
			self.HasStatus(true, StatusID.TheArrow_3888))
		{
			return false;
		}

		return BeirutaTarget.TryUseOn(TheArrowPvE, self, out act);
	}

	private bool TryUtilityCards(out IAction? act)
	{
		act = null;

		if (!InCombat || UtilityCardsHeld)
		{
			return false;
		}

		if (HasBole || HasSpire)
		{
			var card = HasBole ? TheBolePvE : TheSpirePvE;

			var target =
				FindLowest(SingleHealRange, static m => IsTankMember(m) && !HasCardStatus(m) && (HasFreshBigMitigation(m) || m.GetHealthRatio() < UtilityCardTankHp)) ??
				(_raidwide.Coming ? FindLowest(SingleHealRange, static m => !IsTankMember(m) && !HasCardStatus(m)) : null);

			if (target == null && DrawReadySoon(UtilityCardDumpGcds) && MainTank is { } tank && !HasCardStatus(tank))
			{
				target = tank;
			}

			if (target != null && BeirutaTarget.TryUseOn(card, target, out act))
			{
				return true;
			}
		}

		if (HasEwer)
		{
			var target =
				FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < 1f && !HasCardStatus(m)) ??
				FindLowest(SingleHealRange, static m => m.GetHealthRatio() < 1f && !HasCardStatus(m));

			if (target != null && BeirutaTarget.TryUseOn(TheEwerPvE, target, out act))
			{
				return true;
			}
		}

		act = null;
		return false;
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
		(DoomTarget(SingleHealRange) is { } doomed && TryEssentialDignityOn(doomed, out act));

	public bool BurstAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!InCombat)
		{
			return false;
		}

		if (IsBurst && DivinationOpenerPassed && DivinationPvE.CanUse(out act))
		{
			_divinationUsedAtMs = Environment.TickCount64;
			return true;
		}

		if (OpenerWeaveHold)
		{
			return false;
		}

		if (ShouldUseLightspeedForBurst && LightspeedPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		if (BurstCardsAllowed && (TheBalancePvE.CanUse(out act) || TheSpearPvE.CanUse(out act)))
		{
			return true;
		}

		return ShouldUseLord && LordOfCrownsPvE.CanUse(out act);
	}

	public bool ResourceAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!InCombat || OpenerWeaveHold)
		{
			return false;
		}

		if (HasLady &&
			!HasDivination &&
			!AccretionGroupHealBlocked &&
			DrawReadySoon(1) &&
			LadyOfCrownsPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (TryDraw(out act))
		{
			return true;
		}

		if (!OracleGatedByDivination && OraclePvE.CanUse(out act))
		{
			return true;
		}

		return StatusHelper.PlayerHasStatus(true, StatusID.Suntouched) &&
			StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Suntouched) &&
			SunSignPvE.CanUse(out act, skipAoeCheck: true, skipTTKCheck: true);
	}

	public bool AreaHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (!AccretionGroupHealBlocked)
		{
			if (AoeNeeded(PartyHealRange, LadyHp) && LadyOfCrownsPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (HasGiantDominance &&
				AoeNeeded(PartyHealRange, StellarHp) &&
				StellarDetonationPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (!HoldAoe)
		{
			if (HasHoroscopeHelios &&
				CountInjuredWithHoroscope(HoroscopeHp) >= AoeInjuredCount &&
				HoroscopePvE_16558.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (AoeNeeded(PartyHealRange, OppositionHp) &&
				!Updaters.TimelineUpdater.PlanContains(CelestialOppositionPvE.ID) &&
				CelestialOppositionPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		return TryArrowBeforeHelios(nextGCD, out act);
	}

	public bool SpotHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TopUpAllTarget(SingleHealRange) is { } topUp &&
			TryEssentialDignityOn(topUp, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m)) is { } undeadTank &&
			TryEssentialDignityOn(undeadTank, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && HealHp(m) < CriticalHp) is { } critical &&
			DignitySpareForSpot &&
			TryEssentialDignityOn(critical, out act))
		{
			return true;
		}

		if (CelestialIntersectionPvE.EnoughLevel &&
			CelestialIntersectionPvE.Cooldown.MaxCharges >= 2 &&
			CelestialIntersectionPvE.Cooldown.CurrentCharges >= CelestialIntersectionPvE.Cooldown.MaxCharges &&
			MainTank is { } capTank &&
			HealHp(capTank) < CapTankHp &&
			SpotHealOk(capTank) &&
			TryIntersectionOn(capTank, out act))
		{
			return true;
		}

		var edThreshold = EssentialDignityThreshold;
		if (!SaveDignityForAccretion &&
			FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && EffectiveHpRatio(m) < edThreshold) is { } edTarget &&
			TryEssentialDignityOn(edTarget, out act))
		{
			return true;
		}

		return TrySynastryPairing(nextGCD, out act);
	}

	public bool TankHelpAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (BeirutaIncoming.TankbusterWithin(TankbusterLeadSeconds) &&
			TryTankMitigation(FindLowest(SingleHealRange, IsFreshMitigatingTank) ?? MainTank, out act))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsFreshMitigatingTank(member) && (TryTankMitigation(member, out act) || TrySupportCardOn(member, out act)))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	private bool TrySupportCardOn(IBattleChara tank, out IAction? act)
	{
		act = null;
		if (UtilityCardsHeld || IsHealBlocked(tank) || HasCardStatus(tank) || !IsHealableInRange(tank, SingleHealRange))
		{
			return false;
		}

		return ((HasBole || HasSpire) && BeirutaTarget.TryUseOn(HasBole ? TheBolePvE : TheSpirePvE, tank, out act)) ||
			(HasEwer && BeirutaTarget.TryUseOn(TheEwerPvE, tank, out act));
	}

	public bool LowPriorityAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return TryUtilityCards(out act);
		}

		if (_raidwide.Coming &&
			CelestialIntersectionPvE.Cooldown.CurrentCharges >= 2 &&
			TryRaidwideIntersection(out act))
		{
			return true;
		}

		return TryUtilityCards(out act);
	}

	public bool PlanGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public bool SpecialGCD(out IAction? act)
	{
		act = null;
		if (!EssentialDignityReady &&
			DoomTarget(SingleHealRange) is { } doomed &&
			BeirutaTarget.TryUseOn(BeneficIiPvE.EnoughLevel ? BeneficIiPvE : BeneficPvE, doomed, out act))
		{
			return true;
		}

		return BeirutaAccretion.FindTarget(PartyMembers) is { } target &&
			!MicrocosmosFillsAccretion(target) &&
			!HasSwift &&
			!EssentialDignityReady &&
			BeirutaTarget.TryUseOn(BeneficIiPvE, target, out act);
	}

	public bool WardGCD(out IAction? act)
	{
		act = null;
		return WardActive && HeliosGcdWanted && TryHeliosGCD(HeliosWithHot, out act);
	}

	public bool AreaHealGCD(out IAction? act)
	{
		act = null;
		return !WardActive && HeliosGcdWanted && TryHeliosGCD(HeliosWithHot, out act);
	}

	public bool SpotHealGCD(out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if ((!EssentialDignityReady || !DignitySpareForSpot) &&
			FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && BeirutaCritical.ForGcd(m, CriticalHp)) is { } critical &&
			BeirutaTarget.TryUseOn(BeneficIiPvE.EnoughLevel ? BeneficIiPvE : BeneficPvE, critical, out act))
		{
			return true;
		}

		return HasNeutralSect &&
			MainTank is { } tank &&
			!IsHealBlocked(tank) &&
			HealHp(tank) < NeutralSectTankHp &&
			SpotHealOk(tank) &&
			!tank.HasStatus(true, StatusID.NeutralSect_1921) &&
			BeirutaTarget.TryUseOn(AspectedBeneficPvE, tank, out act);
	}

	#endregion

	#region Hooks

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		UpdateMovementCooldownTracking();
		return BeirutaHealerFlow.Ability(this, nextGCD, out act) || base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act) =>
		LowPriorityAbility(nextGCD, out act) || base.GeneralAbility(nextGCD, out act);

	[RotationDesc(
		ActionID.DivinationPvE,
		ActionID.LightspeedPvE,
		ActionID.TheBalancePvE,
		ActionID.TheSpearPvE,
		ActionID.LordOfCrownsPvE,
		ActionID.AstralDrawPvE,
		ActionID.UmbralDrawPvE,
		ActionID.OraclePvE,
		ActionID.EarthlyStarPvE,
		ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		UpdateMovementCooldownTracking();

		if (BurstAbility(nextGCD, out act) || ResourceAbility(nextGCD, out act))
		{
			return true;
		}

		if (InCombat)
		{
			if (ShouldUseSwiftcastForMovement(nextGCD) && SwiftcastPvE.CanUse(out act))
			{
				return true;
			}

			if (ShouldUseLightspeedForMovement(nextGCD) &&
				LightspeedPvE.CanUse(out act, usedUp: LightspeedPvE.Cooldown.CurrentCharges > 1))
			{
				return true;
			}

			if (EarthlyStarOnCooldown &&
				IsBurst &&
				!OpenerWeaveHold &&
				!HasGiantDominance &&
				!HasEarthlyDominance &&
				!Updaters.TimelineUpdater.PlanContains(EarthlyStarPvE.ID) &&
				EarthlyStarPvE.CanUse(out act))
			{
				return true;
			}

			if (LightspeedCapping && LightspeedPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act) =>
		BeirutaHealerFlow.UrgentGcd(this, out act) || base.EmergencyGCD(nextGCD, out act);

	#endregion

	#region Accretion Top Up

	private static bool MicrocosmosFillsAccretion(IBattleChara target) =>
		!target.IsJobCategory(JobRole.Healer) &&
		HasMacrocosmos &&
		target.HasStatus(false, MacrocosmosStatus) &&
		BeirutaAccretion.MicrocosmosSafe(PartyMembers);

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		if (BeirutaAccretion.FindTarget(PartyMembers, requireSafeParty: false) is { } last &&
			MicrocosmosFillsAccretion(last))
		{
			return MicrocosmosPvE.CanUse(out act, skipAoeCheck: true);
		}

		if (!SignalSynastry &&
			!HasSynastry &&
			BeirutaAccretion.NextToFill(PartyMembers) is { } accretionHealer &&
			accretionHealer.IsJobCategory(JobRole.Healer) &&
			accretionHealer.GameObjectId != Player?.GameObjectId &&
			BeirutaTarget.TryUseOn(SynastryPvE, accretionHealer, out act))
		{
			return true;
		}

		var target = BeirutaAccretion.FindTarget(PartyMembers);
		return target != null && BeirutaTarget.TryUseOn(EssentialDignityPvE, target, out act, usedUp: true);
	}

	#endregion

	#region Neutral Sect / Horoscope

	private void TrackNeutralSect()
	{
		var up = HasNeutralSect;
		if (up && !_neutralSectWasUp)
		{
			_neutralSectStartedAt = DateTime.Now;
		}

		_neutralSectWasUp = up;
	}

	private bool NeutralSectHeliosDone =>
		BeirutaHealWindow.UsedSince(_neutralSectStartedAt, ActionID.HeliosConjunctionPvE, ActionID.AspectedHeliosPvE);

	private const float PlannedConjunctionGapSeconds = 15f;

	private bool PlannedConjunctionNear =>
		Updaters.TimelineUpdater.PlannedAround(PlannedConjunctionGapSeconds, HeliosConjunctionPvE.ID);

	private bool TryHeliosGCD(bool withHot, out IAction? act)
	{
		if (PlannedConjunctionNear)
		{
			act = null;
			return false;
		}

		if (withHot)
		{
			if (HeliosConjunctionPvE.EnoughLevel &&
				HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
			{
				return true;
			}

			if (!HeliosConjunctionPvE.EnoughLevel &&
				AspectedHeliosPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (HeliosPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryHoroscopeUpgradeGCD(out IAction? act)
	{
		act = null;
		return AutoUpgradeHoroscopeValue &&
			HasHoroscope &&
			!HasHoroscopeHelios &&
			!AoeHealGcdPending &&
			!WaitForPlannedNeutralSect &&
			TryHeliosGCD(true, out act);
	}

	private const float PairedHeliosWaitSeconds = 6f;
	private const float HoroscopeKeepSeconds = 3f;

	private bool WaitForPlannedNeutralSect =>
		!HasNeutralSect &&
		NeutralSectPvE.EnoughLevel &&
		!NeutralSectPvE.Cooldown.IsCoolingDown &&
		StatusHelper.PlayerStatusTime(true, StatusID.Horoscope) > HoroscopeKeepSeconds &&
		Updaters.TimelineUpdater.PlannedWithin(PairedHeliosWaitSeconds, NeutralSectPvE.ID);

	private bool WaitForPlannedHoroscope =>
		!HasHoroscope &&
		!HasHoroscopeHelios &&
		HoroscopePvE.EnoughLevel &&
		!HoroscopePvE.Cooldown.IsCoolingDown &&
		Updaters.TimelineUpdater.PlannedWithin(PairedHeliosWaitSeconds, HoroscopePvE.ID);

	private bool TryNeutralSectHeliosGCD(out IAction? act)
	{
		act = null;
		return HasNeutralSect &&
			!NeutralSectHeliosDone &&
			!AccretionGroupHealBlocked &&
			!WaitForPlannedHoroscope &&
			CountWithoutNeutralSectShield(HeliosRange) >= BeirutaWard.UnshieldedPartyCount &&
			TryHeliosGCD(true, out act);
	}

	#endregion

	#region GCD Logic

	private bool HeliosGcdWanted
	{
		get
		{
			if (!CanRunHealEngine ||
				(WardActive ? AccretionGroupHealBlocked : HoldAoe) ||
				AoeHealGcdPending ||
				(EssentialDignityReady && BeirutaAccretion.ToFillCount(PartyMembers) > 1 && BeirutaAccretion.FindTarget(PartyMembers) != null))
			{
				return false;
			}

			var aoeNeeded = AoeGcdNeeded(HeliosRange, HeliosWithHot ? ConjunctionHp : HeliosHp);
			var aoeEmergency =
				AoeNeeded(HeliosRange, 0f) ||
				(AnyCritical(HeliosRange) && aoeNeeded);

			return aoeEmergency || (aoeNeeded && (WardActive || !AoeOgcdHealReady));
		}
	}

	[RotationDesc(ActionID.AscendPvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		if (AscendPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		UpdateMovementCooldownTracking();

		if (BeirutaMove.RaiseSwiftHeld)
		{
			return base.GeneralGCD(out act);
		}

		if (TryMacrocosmosSignal(out act) ||
			TryNeutralSectHeliosGCD(out act) ||
			TryHoroscopeUpgradeGCD(out act) ||

			BeirutaHealerFlow.HealGcd(this, out act) ||
			CanUseCurrentGravity(out act))
		{
			return true;
		}

		var expectedHPToLive12Seconds = GetExpectedHpToLive12Seconds();

		if (HasDivination &&
			InCombat &&
			StatusHelper.PlayerStatusTime(true, StatusID.Divination) <= 5f &&
			CurrentTargetCombustMissingOrEnding(DivinationCombustRefreshSeconds) &&
			CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds) &&
			CanUseCurrentCombust(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds) && CanUseCurrentCombust(out act))
		{
			return true;
		}

		if (TryMovementGCD(expectedHPToLive12Seconds, out act) || CanUseCurrentMalefic(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Overrides

	protected override void UpdateInfo()
	{
		BeirutaWard.Track(this, WardMode, ref _lastWardMode);
		_raidwide.Track(InCombat, PartyMembers);
		TrackNeutralSect();
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

	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Divination due in: {DivinationDueIn:F1}s");
		ImGui.Text($"Hold last Lightspeed: {HoldLastLightspeedForDivination}");
		ImGui.Text($"Macrocosmos signal: {MacrocosmosSignalActive}");
		ImGui.Text($"Hold AoE: {HoldAoe}");
		ImGui.Text($"Heal boost: {HealBoost:P0}");
	}

	#endregion
}
