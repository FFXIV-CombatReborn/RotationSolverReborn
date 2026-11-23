using static RotationSolver.Engine.Helpers.BeirutaParty;

namespace RotationSolver.Rotations.Healer.Scholar;

[Rotation("BeirutaSCH", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Scholar. Damage and healing are automatic. Mitigation follows the built-in timeline plan when there is one (see [CD Plan]); otherwise use your Boss Mod / BMR CD planner or press it yourself, with Intercept set to GCD only and ability intercept unticked. Questions? Ask @Beiruta on the RSR Discord.")]
[SourceCode(Path = "main/Rotations/Healer/Scholar/BeirutaSCH.cs")]
[ExtraRotation]

public sealed class BeirutaSCH : ScholarRotation, Updaters.IBeirutaTimelineOption, IBeirutaHealer, IBeirutaHealContext
{
	#region Config Options

	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] CD plan from", Tooltip =
		"Built-in plan: mitigation follows the built-in timeline plan. Built-in SCH plans:\n" +
		"- Dancing Mad (Ultimate): Default\n" +
		"- Futures Rewritten (Ultimate): Default\n" +
		"- AAC Heavyweight (Savage): M9S, M10S, M11S, M12S part 1, M12S part 2: Default\n" +
		"Manual / BMR / VBM: you or your Boss Mod / BMR CD planner press the mitigation; the trigger settings below tell the rotation when to prepare Deployment Tactics.")]
	public BeirutaPlanSource CdPlanFrom { get; set; } = BeirutaPlanSource.BuiltIn;

	public bool UseTimelinePlan => CdPlanFrom == BeirutaPlanSource.BuiltIn;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Use recommended settings for the plan", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"When a built-in plan loads, the settings it recommends are applied, like a preset. Changing a setting afterwards (e.g. your Ward Mode macro) works as usual.\n" +
		"Your saved settings are not changed by the plan: they show as you set them and come back once the plan unloads or this is off.")]
	public bool UsePlanSettings { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Succor / Concitation", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.BuiltInPlan, Tooltip =
		"On: the plan's party shields are cast in their window, unless the party is already shielded.\n" +
		"Off: they are skipped; the rotation shields as usual.\n" +
		"Under Seraphism the rotation keeps non-tanks shielded with Accession by itself (as in Ward Mode).")]
	public bool UsePlannedShields { get; set; } = true;

	bool Updaters.IBeirutaTimelineOption.AllowPlannedAction(uint actionId) =>
		UsePlannedShields ||
		actionId is not ((uint)ActionID.SuccorPvE or (uint)ActionID.ConcitationPvE or (uint)ActionID.AccessionPvE);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		Updaters.BeirutaTimelineUpdater.HoldQueuedPlannedAction(action.ID);

	bool Updaters.IBeirutaTimelineOption.RotationRunsPlannedAction(uint actionId) =>
		actionId is (uint)ActionID.AdloquiumPvE or (uint)ActionID.ManifestationPvE or (uint)ActionID.DeploymentTacticsPvE;

	#endregion

	#region Manual / BMR / VBM Triggers

	[RotationConfig(CombatType.PvE, Name = "    > [Deployment Tactics] Restrict Recitation", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.ManualPlan, Tooltip =
		"On: the rotation only uses Recitation for Deployment Tactics, and pressing Recitation yourself is a trigger.\n" +
		"Off: Recitation also goes on Indomitability, Excogitation or Succor whenever possible.\n" +
		"Either way, preparing Deployment Tactics uses Recitation if it is ready, otherwise goes straight to Adloquium - Deployment Tactics.")]
	public bool RestrictRecitation { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > [Deployment Tactics] Custom triggers (off = your Protraction on yourself)", Parent = nameof(CdPlanFrom), ParentValue = BeirutaSettingNames.ManualPlan, Tooltip =
		"A trigger is something you do that tells the rotation to prepare Deployment Tactics: Adloquium on the right target, then Deployment Tactics, within 15s.\n" +
		"A toast shows Preparing Deployment Tactics, or the cooldown left if it is not ready.\n" +
		"Only used with CD plan from Manual / BMR / VBM; the built-in plan runs the Deployment chain itself.")]
	public bool UseCustomDeploymentSignals { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Protraction on yourself", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalProtraction { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "        > RSR raidwide warning (DefenseArea)", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"When RSR's DefenseArea state is on (its raidwide warning or your DefenseArea macro), Deployment Tactics is prepared instead of a Succor shield.")]
	public bool DeploymentSignalDefenseArea { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Recitation", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"Pressing Recitation yourself prepares Deployment Tactics; the rotation then never spends it on heals. Always on while Restrict Recitation is on.")]
	public bool DeploymentSignalRecitation { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Fey Illumination", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalFeyIllumination { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Surecast", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalSurecast { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Sprint", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalSprint { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "        > Your Swiftcast (not while raising)", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"Your own Swiftcast prepares Deployment Tactics. The rotation then stops using Swiftcast for movement or Succor.")]
	public bool DeploymentSignalSwiftcast { get; set; } = false;

	private bool DeploymentSignalsAllowed => !UseTimelinePlan;
	private bool RestrictRecitationValue => DeploymentSignalsAllowed && RestrictRecitation;
	private bool CustomDeploymentSignalsOn => UseCustomDeploymentSignals && DeploymentSignalsAllowed;

	private bool SignalProtraction => DeploymentSignalsAllowed && (!UseCustomDeploymentSignals || DeploymentSignalProtraction) && ProtractionUsageValue != ProtractionUsageStrategy.HelpOnly;
	private bool SignalDefenseArea => CustomDeploymentSignalsOn && DeploymentSignalDefenseArea;
	private bool SignalRecitation => DeploymentSignalsAllowed && (RestrictRecitation || (UseCustomDeploymentSignals && DeploymentSignalRecitation));
	private bool SignalFeyIllumination => CustomDeploymentSignalsOn && DeploymentSignalFeyIllumination;
	private bool SignalSurecast => CustomDeploymentSignalsOn && DeploymentSignalSurecast;
	private bool SignalSprint => CustomDeploymentSignalsOn && DeploymentSignalSprint;
	private bool SignalSwiftcast => CustomDeploymentSignalsOn && DeploymentSignalSwiftcast;

	#endregion

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps every non-tank shielded.\n" +
		"- Succor / Concitation (Accession under Seraphism) whenever a non-tank in range is unshielded.\n" +
		"- AoE thresholds use real HP and ignore the co-healer and HoTs.\n" +
		"- Fairy order: Fey Blessing > Whispering Dawn > Indomitability.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[More settings] Show", Tooltip =
		"Pick a page to show its detailed settings. This only changes what is shown.")]
	public BeirutaSettingsPage MoreSettings { get; set; } = BeirutaSettingsPage.Hide;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"What SCH puts up during the countdown. Deployment options spread the Adloquium shield to the party; Recitation makes it critical.")]
	public CountdownOpenerStrategy CountdownOpener { get; set; } = CountdownOpenerStrategy.RecitationAdloquiumDeploymentTactics;

	[RotationConfig(CombatType.PvE, Name = "    > Adloquium tank pick", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"Which tank gets the countdown Adloquium. The priority list is Adloquium Tank Priority, below these settings.")]
	public CountdownTankPickStrategy CountdownTankPick { get; set; } = CountdownTankPickStrategy.Stance;

	public enum CountdownOpenerStrategy : byte
	{
		[Description("None")]
		None = 0,

		[Description("Adloquium on main tank")]
		AdloquiumMainTank = 1,

		[Description("Adloquium on both tanks")]
		AdloquiumBothTanks = 2,

		[Description("Adloquium - Deployment Tactics")]
		AdloquiumDeploymentTactics = 3,

		[Description("Recitation - Adloquium - Deployment Tactics")]
		RecitationAdloquiumDeploymentTactics = 4,

		[Description("Concitation/Succor")]
		ConcitationOrSuccor = 5,

		[Description("Recitation - Concitation/Succor")]
		RecitationConcitationOrSuccor = 6,
	}

	public enum CountdownTankPickStrategy : byte
	{
		[Description("Stance first, then priority list")]
		Stance,

		[Description("Priority list only")]
		PriorityList,
	}

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Chain Stratagem", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.OpenerPage, Tooltip =
		"When the first Chain Stratagem goes out (needs AutoBurst).\n" +
		"0s: sent just before the countdown ends, then Biolysis, then Dissipation / Aetherflow in its two weave windows.")]
	public ChainStratagemOpenerTiming ChainStratagemOpener { get; set; } = ChainStratagemOpenerTiming.FiveSeconds;

	public enum ChainStratagemOpenerTiming : byte
	{
		[Description("0s")]
		ZeroSeconds,

		[Description("3s")]
		ThreeSeconds,

		[Description("5s")]
		FiveSeconds,
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

	[RotationConfig(CombatType.PvE, Name = "[Damage] Energy Drain in burst", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"How many Aetherflow stacks are spent on Energy Drain during burst. Leftovers are always dumped before Aetherflow / Dissipation comes back.")]
	public EnergyDrainStrategy EnergyDrainUsage { get; set; } = EnergyDrainStrategy.AllInBurst;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Delay Baneful Impaction 5s after Chain Stratagem", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage)]
	public bool DelayBanefulImpaction { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Aetherflow before Dissipation", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.DamagePage, Tooltip =
		"When both are available, Aetherflow goes first and Dissipation is kept for later.\n" +
		"A built-in timeline plan sets this for its fight.")]
	public bool PrioritizeAetherflowOverDissipation { get; set; } = false;

	private bool AetherflowFirstValue =>
		Updaters.BeirutaTimelineUpdater.PlanOption("AetherflowFirst") is { } v
			? v.Equals("on", StringComparison.OrdinalIgnoreCase)
			: PrioritizeAetherflowOverDissipation;

	public enum EnergyDrainStrategy : byte
	{
		[Description("Use all")]
		AllInBurst,

		[Description("Leave 1")]
		LeaveOne,

		[Description("Leave 2")]
		LeaveTwo,

		[Description("Only before capping")]
		CappingOnly,
	}

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Biolysis refresh", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"While moving, refreshes Biolysis early instead of using Ruin II.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Custom Swiftcast usage (off = default)", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.MovementPage, Tooltip =
		"Default: kept for raise while someone is dead, and used while moving for Deployment Adloquium and Succor shields (Ward, Accretion, raidwide).\n" +
		"It is only used to keep casting damage when Damage casts while moving is on.")]
	public bool UseCustomSwiftcast { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Keep for raise while someone is dead", Parent = nameof(UseCustomSwiftcast))]
	public bool SwiftcastKeepForRaise { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Deployment Adloquium while moving", Parent = nameof(UseCustomSwiftcast))]
	public bool SwiftcastForDeployment { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Succor while moving (Ward, Accretion, raidwide shield)", Parent = nameof(UseCustomSwiftcast))]
	public bool SwiftcastForSuccor { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > Damage casts while moving", Parent = nameof(UseCustomSwiftcast), Tooltip =
		"Also uses Swiftcast for damage while moving when nothing free is available (Art of War, early Biolysis).")]
	public bool SwiftcastForMovement { get; set; } = false;

	private bool SwiftKeepForRaiseValue => !UseCustomSwiftcast || SwiftcastKeepForRaise;
	private bool SwiftForDeploymentValue => !UseCustomSwiftcast || SwiftcastForDeployment;
	private bool SwiftForSuccorValue => !UseCustomSwiftcast || SwiftcastForSuccor;
	private bool SwiftForMovementValue => UseCustomSwiftcast && SwiftcastForMovement;

	#endregion

	#region Healing

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Parent = nameof(MoreSettings), ParentValue = BeirutaSettingNames.HealingPage, Tooltip =
		"Off: each area heal waits until 3+ players are low enough that it won't be wasted, worked out from its potency, the co-healer's smallest GCD heal and incoming heal-over-time ticks.\n" +
		"On: each area heal fires when 3+ players in range are below the value set here.\n" +
		"Ward Mode ignores these values and uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Automatic Protraction", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"On: Protraction goes on a tank that just pressed a mitigation or before a tankbuster, and on the lowest non-tank before a raidwide.\n" +
		"With CD plan from Manual / BMR / VBM, your own Protraction on yourself can also prepare Deployment Tactics.")]
	public ProtractionUsageStrategy ProtractionUsage { get; set; } = ProtractionUsageStrategy.Both;

	public enum ProtractionUsageStrategy : byte
	{
		[Description("Off (yours is only a trigger)")]
		SignalOnly,

		[Description("On, yours is not a trigger")]
		HelpOnly,

		[Description("On, yours is also a trigger")]
		Both,
	}

	[RotationConfig(CombatType.PvE, Name = "    > First Consolation as soon as Seraph is out", Parent = nameof(UseCustomHPThreshold))]
	public bool UseFirstConsolationAsapWhenSeraphIsOut { get; set; } = true;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Whispering Dawn: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float WhisperingDawnHeal { get; set; } = 0.85f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Fey Blessing: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float FeyBlessingHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Indomitability: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float IndomitabilityHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Consolation: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float ConsolationHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Accession: area heal when 3+ below", Parent = nameof(UseCustomHPThreshold))]
	public float AccessionHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Excogitation: tank below", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Excogitation also goes on a tank at any HP right after they press a mitigation or before a tankbuster.")]
	public float ExcogitationTankHeal { get; set; } = 0.5f;

	private bool FirstConsolationAsapValue => !UseCustomHPThreshold || UseFirstConsolationAsapWhenSeraphIsOut;
	private ProtractionUsageStrategy ProtractionUsageValue => UseCustomHPThreshold ? ProtractionUsage : ProtractionUsageStrategy.Both;
	private float ExcogitationTankHealValue => UseCustomHPThreshold ? ExcogitationTankHeal : 0.5f;

	#endregion

	#endregion

	#region Constants / Fields

	private const long ChainStratagemBanefulBlockMs = 5_000;
	private const long ChainStratagemDotRefreshMs = 20_000;
	private const long DeploymentTacticsAfterAdloquiumWindowMs = 5_000;

	private const float ChainStratagemDotRefreshSeconds = 11f;
	private const float AetherpactRemoveHpThreshold = 0.9f;
	private const float AetherpactStartHpThreshold = 0.8f;

	private const int DotOffsetMobs = 1;

	private long _chainStratagemUsedAtMs;
	private long _adloquiumUsedAtMs;

	private const long DeploymentChainWindowMs = 15_000;
	private const long PlannedDeploymentChainWindowMs = 12_000;
	private const float PlannedDeploymentChainLeadSeconds = 6f;
	private bool _deploymentChainFromPlan;
	private long _deploymentChainStartedAtMs;
	private DateTime _deploymentChainStartedAt;
	private IBattleChara? _deploymentTarget;

	private readonly BeirutaRaidwide _raidwide = new();

	#endregion

	#region Simple Status Helpers

	private bool HasGalvanize => StatusHelper.PlayerHasStatus(true, StatusID.Galvanize);
	private bool HasProtraction => StatusHelper.PlayerHasStatus(true, StatusID.Protraction);
	private bool HasSeraphism => StatusHelper.PlayerHasStatus(true, StatusID.Seraphism);

	private bool WardActive => WardMode || HasSeraphism || Updaters.BeirutaTimelineUpdater.PlannedWardMode;

	bool IBeirutaHealContext.WardMode => WardActive;

	private bool HasWhisperingDawn => HasPartyMemberWithOwnStatus(StatusID.WhisperingDawn);
	private bool HasAngelsWhisper => HasPartyMemberWithOwnStatus(StatusID.AngelsWhisper);

	private const float MovementShieldHealHp = 0.95f;
	private const float BiolysisWorthRefreshSeconds = 17f;

	private bool InDamageBurst => InChainStratagemBurstWindow || IsPartyBurst;

	private bool InFirst5sAfterChainStratagem =>
		_chainStratagemUsedAtMs != 0 &&
		Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemBanefulBlockMs;

	private bool InFirst20sAfterChainStratagem =>
		_chainStratagemUsedAtMs != 0 &&
		Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemDotRefreshMs;

	private bool InFirst5sAfterAdloquium =>
		_adloquiumUsedAtMs != 0 &&
		Environment.TickCount64 - _adloquiumUsedAtMs < DeploymentTacticsAfterAdloquiumWindowMs;

	private bool SwiftcastAvailable =>
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!HasSwift &&
		!IsLastAction(ActionID.SwiftcastPvE) &&
		!BeirutaMove.RaiseSwiftHeld &&
		!(SwiftKeepForRaiseValue && AnyPartyMemberDead) &&
		!Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedSwiftcastReserveSeconds, SwiftcastPvE.ID);

	private const float PlannedSwiftcastReserveSeconds = 40f;

	private bool ShouldSwiftcastForAdloquium() =>
		SwiftForDeploymentValue &&
		!HasSeraphism &&
		BeirutaMove.Sufficient &&
		SwiftcastAvailable &&
		ShouldUseDeploymentAdloquium();

	private bool SuccorShieldWanted =>
		!HasSeraphism &&
		!HasEmergencyTactics &&
		!DeploymentChainActive &&
		!ProtectRecitationForDeployment &&
		((WardActive && WardNonTanksUnshielded) ||
		 (CountUnshielded(SuccorRange) >= BeirutaWard.UnshieldedPartyCount &&
		  (BeirutaAccretion.GuardActive(PartyMembers) ||
		   (!SignalDefenseArea && MergedStatus.HasFlag(AutoStatus.DefenseArea)))));

	private static bool WardNonTanksUnshielded =>
		BeirutaWard.CountNonTanksMissing(PartyMembers, SuccorRange, static m => HasPartyShield(m)) > 0;

	private bool ShouldSwiftcastForSuccor() =>
		SwiftForSuccorValue &&
		!SignalSwiftcast &&
		BeirutaMove.Sufficient &&
		SwiftcastAvailable &&
		SuccorShieldWanted;

	private bool ShouldUsePotionInCombat =>
		!OpenerWeaveHold &&
		BeirutaPotion.Ready(UsePotion, PotionTiming, IsBurst, ChainStratagemPvE.Cooldown.WillHaveOneCharge(5) || !ChainStratagemPvE.EnoughLevel);

	private bool CanUseCountdownShieldGCD(out IAction? act)
	{
		if (ConcitationPvE.CanUse(out act))
		{
			return true;
		}

		if (SuccorPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Helper Methods

	private float ChainStratagemOpenerSeconds => ChainStratagemOpener switch
	{
		ChainStratagemOpenerTiming.ZeroSeconds => 0f,
		ChainStratagemOpenerTiming.ThreeSeconds => 3f,
		_ => 5f,
	};

	private bool InChainStratagemBurstWindow =>
		ChainStratagemPvE.EnoughLevel &&
		ChainStratagemPvE.Cooldown.IsCoolingDown &&
		!ChainStratagemPvE.Cooldown.WillHaveOneCharge(100f);

	private int EnergyDrainStacksToKeep => EnergyDrainUsage switch
	{
		EnergyDrainStrategy.AllInBurst => 0,
		EnergyDrainStrategy.LeaveOne => 1,
		EnergyDrainStrategy.LeaveTwo => 2,
		_ => 3,
	};

	private bool CurrentTargetInLast5sOfChainStratagem
	{
		get
		{
			if (CurrentTarget == null)
			{
				return false;
			}

			return CurrentTarget.HasStatus(true, StatusID.ChainStratagem) &&
				   CurrentTarget.WillStatusEnd(5f, true, StatusID.ChainStratagem);
		}
	}

	private void UpdateActionTracking()
	{
		UpdateDeploymentChain();
	}

	private bool IsMovementPreferredNextGCD(IAction nextGCD)
	{
		if (nextGCD == null)
		{
			return false;
		}

		return nextGCD.IsTheSameTo(false,
			ArtOfWarPvE,
			ArtOfWarIiPvE,
			ManifestationPvE,
			AccessionPvE,
			BioPvE,
			BioIiPvE,
			BiolysisPvE);
	}

	private bool SwiftcastPlannedForMovement =>
		SwiftForMovementValue &&
		!SignalSwiftcast &&
		SwiftcastAvailable;

	private bool FreeMovementInstantAvailable(int nearbyHostiles, float expectedHPToLive12Seconds) =>
		(nearbyHostiles > 1 && ArtOfWarPvE.CanUse(out _, skipAoeCheck: true)) ||
		(!InDamageBurst && !AccretionGroupHealBlocked && MovementAccessionUseful) ||
		MovementBiolysisDue(expectedHPToLive12Seconds, nearbyHostiles);

	private bool ShouldSwiftcastForMovement(IAction nextGCD)
	{
		if (!SwiftcastPlannedForMovement ||
			!BeirutaMove.Sufficient ||
			IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return !FreeMovementInstantAvailable(AttackableHostilesInRangeOf(5), ExpectedHpToLive12Seconds(Player));
	}

	private bool MovementAccessionUseful =>
		HasSeraphism &&
		!HasGalvanize &&
		CountInjured(SuccorRange, MovementShieldHealHp) >= AoeInjuredCount;

	private bool MovementBiolysisDue(float expectedHPToLive12Seconds, int nearbyHostiles) =>
		EarlyDotRefreshWhenMoving &&
		BeirutaMove.Sufficient &&
		CurrentTarget != null &&
		CurrentTarget.CurrentHp >= expectedHPToLive12Seconds &&
		nearbyHostiles < GetAoWBreakevenTargets() &&
		CurrentTargetBioMissingOrEnding(BiolysisWorthRefreshSeconds);

	private bool TryMovementGCD(int nearbyHostiles, float expectedHPToLive12Seconds, out IAction? act)
	{
		act = null;

		if (!BeirutaMove.PastGrace || HasSwift || BeirutaMove.RaiseSwiftHeld)
		{
			return false;
		}

		if (nearbyHostiles > 1 && ArtOfWarPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (!InDamageBurst && !AccretionGroupHealBlocked && MovementAccessionUseful && AccessionPvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (MovementBiolysisDue(expectedHPToLive12Seconds, nearbyHostiles) && CanUseCurrentBio(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (SwiftcastPlannedForMovement)
		{
			act = null;
			return false;
		}

		if (RuinIiPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Fairy Tracking

	private const float DissipationDuration = 30f;
	private const float SeraphDuration = 22f;
	private const float FairyReturnGraceSeconds = 5f;

	private long _lastDeathMs;

	private void TrackDeath()
	{
		if (ECommons.GameHelpers.Player.Object is { IsDead: true })
		{
			_lastDeathMs = Environment.TickCount64;
		}
	}

	private bool FairyAwayWindow(IBaseAction action, float duration)
	{
		if (!action.EnoughLevel || !action.Cooldown.IsCoolingDown)
		{
			return false;
		}

		var elapsed = action.Cooldown.RecastTimeElapsed;
		if (elapsed > duration + FairyReturnGraceSeconds)
		{
			return false;
		}

		var usedAtMs = Environment.TickCount64 - (long)(elapsed * 1000f);
		return _lastDeathMs < usedAtMs;
	}

	private bool FairyReturning =>
		FairyAwayWindow(DissipationPvE, DissipationDuration) ||
		FairyAwayWindow(SummonSeraphPvE, SeraphDuration);

	private bool ShouldUseSummonEos() => !FairyReturning;

	#endregion

	#region Countdown Logic

	private readonly BeirutaOpenerHold _openerHold = new();

	private bool OpenerWeaveHold => _openerHold.Active(BiolysisPvE, BioIiPvE, BioPvE);

	private readonly BeirutaCountdownDelay _countdownDelay = new();

	private const float ChainStratagemRange = 25f;

	private bool TryOpenerChainStratagem(float remainTime, out IAction? act)
	{
		act = null;

		if (ChainStratagemOpener != ChainStratagemOpenerTiming.ZeroSeconds ||
			!IsBurst ||
			remainTime >= BeirutaOpenerHold.SendAtCountdownSeconds ||
			ECommons.GameHelpers.Player.Object is not { IsCasting: false } ||
			!ChainStratagemPvE.EnoughLevel ||
			!ChainStratagemPvE.IsEnabled ||
			ChainStratagemPvE.Cooldown.IsCoolingDown ||
			CurrentTarget is not { } target ||
			!target.IsAttackable() ||
			target.DistanceToPlayer() > ChainStratagemRange)
		{
			return false;
		}

		if (!BeirutaTarget.TryRsrTarget(ChainStratagemPvE, target, out act, skipCastingCheck: true, skipTTKCheck: true))
		{
			return false;
		}

		_openerHold.Start();
		StampChainStratagemUse();
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

		if ((remainTime is < 14f and > 7f || remainTime is < 4f and > 3f) && SummonEosPvE.CanUse(out var act))
		{
			return act;
		}

		if (remainTime < RuinPvE.Info.CastTime + CountDownAhead && RuinPvE.CanUse(out act))
		{
			return act;
		}

		if (TryOpenerChainStratagem(remainTime, out act))
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

		var deployAt = CountdownDeploymentAt;
		switch (CountdownOpener)
		{
			case CountdownOpenerStrategy.RecitationAdloquiumDeploymentTactics:
				if (remainTime > deployAt - 0.5f && remainTime < deployAt + 0.5f && DeploymentTacticsPvE.CanUse(out act))
				{
					return act;
				}

				if (remainTime > deployAt + 2.5f && remainTime < deployAt + 3.5f &&
					AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Tank))
				{
					StampAdloquiumUse();
					return act;
				}

				if (remainTime > deployAt + 3.5f && remainTime < deployAt + 10.5f &&
					RecitationPvE.CanUse(out act))
				{
					return act;
				}
				break;

			case CountdownOpenerStrategy.AdloquiumDeploymentTactics:
				if (remainTime > deployAt - 0.5f && remainTime < deployAt + 0.5f && DeploymentTacticsPvE.CanUse(out act))
				{
					return act;
				}

				if (remainTime > deployAt + 2.5f && remainTime < deployAt + 3.5f &&
					AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Tank))
				{
					StampAdloquiumUse();
					return act;
				}
				break;

			case CountdownOpenerStrategy.AdloquiumMainTank:
				if (remainTime is < 7f and > 5f && TryCountdownTankAdloquium(0, out act))
				{
					return act;
				}
				break;

			case CountdownOpenerStrategy.AdloquiumBothTanks:
				if (remainTime is < 10f and > 7.5f && TryCountdownTankAdloquium(0, out act))
				{
					return act;
				}

				if (remainTime is < 7f and > 5f && TryCountdownTankAdloquium(1, out act))
				{
					return act;
				}
				break;

			case CountdownOpenerStrategy.ConcitationOrSuccor:
				if (remainTime is < 7f and > 6f && CanUseCountdownShieldGCD(out act))
				{
					return act;
				}

				break;

			case CountdownOpenerStrategy.RecitationConcitationOrSuccor:
				if (remainTime is < 7f and > 6f && CanUseCountdownShieldGCD(out act))
				{
					return act;
				}

				if (remainTime is < 14f and > 9f &&
					RecitationPvE.CanUse(out act))
				{
					return act;
				}
				break;

			case CountdownOpenerStrategy.None:
			default:
				break;
		}

		return base.CountDownAction(remainTime);
	}

	#endregion

	#region Healing Engine

	private bool CanRunHealEngine => BeirutaHeal.CanRun();

	private static bool AccretionGroupHealBlocked => BeirutaHeal.GroupHealBlocked;

	private float EffectiveHpRatio(IBattleChara member) => BeirutaHeal.EffectiveHpRatio(this, member);

	private bool CountsAsInjured(IBattleChara member, float threshold) => BeirutaHeal.Injured(this, member, threshold);

	private int CountInjured(float range, float threshold) => BeirutaHeal.CountInjured(this, range, threshold);

	private bool AoeNeeded(float range, float threshold) => BeirutaHeal.AoeNeeded(this, range, threshold);

	private bool SpotHealOk(IBattleChara member) => SpotHealAllowed(member, AoeNeeded(IndomitabilityRange, IndomitabilityHp), IndomitabilityRange);

	private bool AoeGcdNeeded(float range, float threshold) => BeirutaHeal.AoeGcdNeeded(this, range, threshold);

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) => BeirutaHeal.Threshold(this, potency, customWardOff, wardOffCap);

	float IBeirutaHealContext.HealBoost => HealBoost;

	float IBeirutaHealContext.LandingHotPotency(IBattleChara member) => LandingHotPotency(member);
	private static bool HoldAoe => BeirutaHeal.HoldAoe(SingleHealRange);

	private const float SingleHealRange = 30f;
	private const float FairyHealRange = 20f;
	private const float IndomitabilityRange = 15f;
	private const float SuccorRange = 15f;
	private const int AoeInjuredCount = 3;
	private const float WhisperingDawnCountedPotency = BeirutaHealModel.WhisperingDawnTickPotency * BeirutaHealModel.HotCreditTicks;
	private const float FeyBlessingPotency = 288f;
	private const float ConsolationPotency = 225f;
	private const float IndomitabilityPotency = 400f;
	private const float AccessionPotency = 240f;
	private const float AccessionWardOffCap = 0.7f;
	private const float DissipationBoost = 1.2f;
	private const float RecitationSalvageSeconds = 4f;

	private float LandingHotPotency(IBattleChara member) =>
		IsHealableInRange(member, FairyHealRange) &&
		!member.HasStatus(false, StatusID.WhisperingDawn, StatusID.AngelsWhisper) &&
		BeirutaHealWindow.OgcdJustUsed(WhisperingDawnPvE)
			? WhisperingDawnCountedPotency
			: 0f;

	private int CountInjuredAroundPet(float radius, float threshold)
	{
		var pet = DataCenter.GetPet();
		if (pet == null)
		{
			return 0;
		}

		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (member != null &&
				!member.IsDead &&
				!IsHealBlocked(member) &&
				System.Numerics.Vector3.Distance(pet.Position, member.Position) <= radius &&
				CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	#endregion

	#region Aetherflow

	private const float AetherpactDissipationLeadSeconds = 10f;

	private const float DissipationFairyAwaySeconds = 33f;

	private static readonly uint[] PlannedFairyActions =
	[
		(uint)ActionID.WhisperingDawnPvE, (uint)ActionID.FeyBlessingPvE, (uint)ActionID.FeyIlluminationPvE,
		(uint)ActionID.SeraphismPvE, (uint)ActionID.ConsolationPvE, (uint)ActionID.AetherpactPvE,
	];

	private bool DissipationBlockedByPlan =>
		Updaters.BeirutaTimelineUpdater.PlanContains(DissipationPvE.ID) ||
		Updaters.BeirutaTimelineUpdater.PlannedWindowClosesWithin(DissipationFairyAwaySeconds, PlannedFairyActions);

	private static float AetherflowSpendSeconds => SCHAetherFlowStacks * DataCenter.DefaultGCDTotal;

	private bool DissipationCountsAsRefill =>
		DissipationUsable &&
		!(Updaters.BeirutaTimelineUpdater.HasPlan &&
		  Updaters.BeirutaTimelineUpdater.PlannedWindowClosesWithin(DissipationFairyAwaySeconds + AetherflowSpendSeconds, PlannedFairyActions));

	private bool DissipationUsable =>
		IsBurst &&
		DissipationPvE.EnoughLevel &&
		DissipationPvE.IsEnabled &&
		!DissipationBlockedByPlan;

	private static uint AetherflowSpendGcds => (uint)SCHAetherFlowStacks + 1;

	private bool FreshAetherflowSoon =>
		AetherflowPvE.Cooldown.WillHaveOneChargeGCD(AetherflowSpendGcds) ||
		(DissipationCountsAsRefill && DissipationPvE.Cooldown.WillHaveOneChargeGCD(AetherflowSpendGcds));

	private const float PlannedRecitationCoverSeconds = 15f;

	private static readonly List<float> _plannedAetherflowSpends = [];
	private static readonly List<float> _plannedRecitationFree = [];
	private static readonly List<float> _plannedRecitations = [];

	private float SecondsToFreshAetherflow
	{
		get
		{
			var refill = AetherflowPvE.Cooldown.IsCoolingDown ? AetherflowPvE.Cooldown.RecastTimeRemainOneCharge : 0f;
			if (DissipationCountsAsRefill)
			{
				var dissipation = DissipationPvE.Cooldown.IsCoolingDown ? DissipationPvE.Cooldown.RecastTimeRemainOneCharge : 0f;
				refill = MathF.Min(refill, dissipation);
			}

			return refill;
		}
	}

	private int PlannedAetherflowNeed
	{
		get
		{
			var window = SecondsToFreshAetherflow;
			if (window <= 0f)
			{
				return 0;
			}

			Updaters.BeirutaTimelineUpdater.UpcomingStarts(window, _plannedAetherflowSpends, SacredSoilPvE.ID, LustratePvE.ID);
			Updaters.BeirutaTimelineUpdater.UpcomingStarts(window, _plannedRecitationFree, IndomitabilityPvE.ID, ExcogitationPvE.ID);
			Updaters.BeirutaTimelineUpdater.UpcomingStarts(window, _plannedRecitations, RecitationPvE.ID);

			var need = _plannedAetherflowSpends.Count;
			var recitationUp = HasRecitation;
			foreach (var at in _plannedRecitationFree)
			{
				if (recitationUp && at <= PlannedRecitationCoverSeconds)
				{
					recitationUp = false;
					continue;
				}

				var covered = false;
				for (var i = 0; i < _plannedRecitations.Count; i++)
				{
					var r = _plannedRecitations[i];
					if (r <= at && at - r <= PlannedRecitationCoverSeconds)
					{
						_plannedRecitations.RemoveAt(i);
						covered = true;
						break;
					}
				}

				if (!covered)
				{
					need++;
				}
			}

			return Math.Min(3, need);
		}
	}

	private int EnergyDrainKeep => Math.Max(EnergyDrainStacksToKeep, PlannedAetherflowNeed);

	private bool DissipationSoonForAetherpact =>
		DissipationUsable &&
		DissipationPvE.Cooldown.WillHaveOneCharge(AetherpactDissipationLeadSeconds);

	private bool SacredSoilReserve =>
		SacredSoilPvE.EnoughLevel &&
		!FreshAetherflowSoon &&
		SacredSoilPvE.Cooldown.WillHaveOneCharge(Math.Max(5f, AetherflowPvE.Cooldown.RecastTimeRemainOneCharge));

	private bool CanSpendAetherflow =>
		SCHAetherFlowStacks > PlannedAetherflowNeed &&
		(SCHAetherFlowStacks >= 2 ||
		 (SCHAetherFlowStacks == 1 && !SacredSoilReserve));

	#endregion

	#region Recitation / Protraction

	private bool RecitationForHealingAllowed =>
		!RestrictRecitationValue &&
		!SignalRecitation &&
		!DeploymentChainActive;

	private bool PlanOwnsRecitation => Updaters.BeirutaTimelineUpdater.PlanContains(RecitationPvE.ID);

	private bool RecitationReservedForPlan => HasRecitation && PlanOwnsRecitation;

	private bool CanPressRecitation =>
		!PlanOwnsRecitation &&
		RecitationForHealingAllowed &&
		!HasRecitation &&
		RecitationPvE.EnoughLevel &&
		RecitationPvE.IsEnabled &&
		!RecitationPvE.Cooldown.IsCoolingDown;

	private bool RecitationAvailableForHealing => HasRecitation && RecitationForHealingAllowed;

	private bool ProtractionHelpAllowed => ProtractionUsageValue != ProtractionUsageStrategy.SignalOnly;

	private bool TryProtractionOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return ProtractionHelpAllowed &&
			!IsHealBlocked(target) &&
			target.GameObjectId != Player?.GameObjectId &&
			!target.HasStatus(false, StatusID.Protraction) &&
			BeirutaTarget.TryUseOn(ProtractionPvE, target, out act);
	}

	private bool TryExcogitationOn(IBattleChara target, out IAction? act)
	{
		act = null;

		if (RecitationReservedForPlan)
		{
			return false;
		}

		if (IsHealBlocked(target) ||
			target.HasStatus(false, StatusID.Excogitation, StatusID.Excogitation_2182) ||
			!ExcogitationPvE.EnoughLevel ||
			!ExcogitationPvE.IsEnabled ||
			ExcogitationPvE.Cooldown.IsCoolingDown)
		{
			return false;
		}

		if (!RecitationAvailableForHealing && !CanSpendAetherflow)
		{
			if (CanPressRecitation && RecitationPvE.CanUse(out act))
			{
				return true;
			}

			return false;
		}

		return BeirutaTarget.TryUseOn(ExcogitationPvE, target, out act);
	}

	private bool TryIndomitability(out IAction? act)
	{
		act = null;

		if (RecitationReservedForPlan)
		{
			return false;
		}

		if (!IndomitabilityPvE.EnoughLevel || !IndomitabilityPvE.IsEnabled || IndomitabilityPvE.Cooldown.IsCoolingDown)
		{
			return false;
		}

		if (!RecitationAvailableForHealing && CanPressRecitation && RecitationPvE.CanUse(out act))
		{
			return true;
		}

		if (!RecitationAvailableForHealing && !CanSpendAetherflow)
		{
			return false;
		}

		return IndomitabilityPvE.CanUse(out act);
	}

	#endregion

	#region Healing Abilities

	private const int AetherpactMinimumGauge = 20;

	private static bool AccretionLinkBlocked(IBattleChara member) =>
		DataCenter.IsInDMU && member.HasStatus(false, StatusID.Accretion);

	private static bool MostPartyVeiled()
	{
		var total = 0;
		var veiled = 0;
		foreach (var member in PartyMembers)
		{
			if (!IsHealableInRange(member, FairyHealRange))
			{
				continue;
			}

			total++;
			if (member.HasStatus(false, StatusID.SeraphicVeil))
			{
				veiled++;
			}
		}
		return total > 0 && veiled * 2 >= total;
	}

	private static float HealBoost => HasDissipation ? DissipationBoost : 1f;

	private float IndomitabilityHp =>
		!WardActive && RecitationAvailableForHealing
			? BeirutaHealModel.RoundToFive(BeirutaHealModel.FullUseHp(IndomitabilityPotency, HealBoost))
			: AoeThreshold(IndomitabilityPotency, IndomitabilityHeal);

	private bool AoeEmergency =>
		AoeNeeded(IndomitabilityRange, 0f) ||
		(AnyCritical(IndomitabilityRange) && AoeNeeded(IndomitabilityRange, IndomitabilityHp));

	private bool TryConsolationHeal(out IAction? act)
	{
		act = null;
		return SeraphTime > 0 &&
			!MostPartyVeiled() &&
			CountInjuredAroundPet(FairyHealRange, AoeThreshold(ConsolationPotency, ConsolationHeal)) >= AoeInjuredCount &&
			ConsolationPvE.CanUse(out act, usedUp: true);
	}

	private bool TryWhisperingDawnHeal(out IAction? act)
	{
		act = null;
		return !HasWhisperingDawn &&
			!HasAngelsWhisper &&
			CountInjuredAroundPet(FairyHealRange, AoeThreshold(WhisperingDawnCountedPotency, WhisperingDawnHeal)) >= AoeInjuredCount &&
			WhisperingDawnPvE.CanUse(out act);
	}

	private bool TryFeyBlessingHeal(out IAction? act)
	{
		act = null;
		return CountInjuredAroundPet(FairyHealRange, AoeThreshold(FeyBlessingPotency, FeyBlessingHeal)) >= AoeInjuredCount &&
			FeyBlessingPvE.CanUse(out act);
	}

	private bool TryIndomitabilityHeal(out IAction? act)
	{
		act = null;
		return AoeNeeded(IndomitabilityRange, IndomitabilityHp) && TryIndomitability(out act);
	}

	private bool TryAoeHealAbility(out IAction? act)
	{
		if (TryConsolationHeal(out act))
		{
			return true;
		}

		if (AoeEmergency)
		{
			return TryFeyBlessingHeal(out act) || TryIndomitabilityHeal(out act) || TryWhisperingDawnHeal(out act);
		}

		if (WardActive)
		{
			return TryFeyBlessingHeal(out act) || TryWhisperingDawnHeal(out act) || TryIndomitabilityHeal(out act);
		}

		if (RecitationAvailableForHealing && TryIndomitabilityHeal(out act))
		{
			return true;
		}

		return TryWhisperingDawnHeal(out act) || TryFeyBlessingHeal(out act) || TryIndomitabilityHeal(out act);
	}

	private bool SeraphismAoeHealNeeded =>
		HasSeraphism &&
		!HoldAoe &&
		CountInjured(SuccorRange, AoeThreshold(AccessionPotency, AccessionHeal, AccessionWardOffCap)) >= AoeInjuredCount;

	private static bool HasPartyShield(IBattleChara member) =>
		member.HasStatus(false, StatusID.Galvanize, StatusID.EukrasianPrognosis);

	private static int CountUnshielded(float range)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !HasPartyShield(member))
			{
				count++;
			}
		}
		return count;
	}

	#endregion

	#region Emergency Tactics (pressed outside)

	private const float EmergencyTacticsAoeHp = 0.9f;
	private const float EmergencyTacticsSingleHp = 0.9f;
	private const float EmergencyTacticsLastCallSeconds = 3f;

	private bool TryEmergencyTacticsGCD(out IAction? act)
	{
		act = null;

		if (RecitationReservedForPlan)
		{
			return false;
		}

		if (!HasEmergencyTactics || DeploymentChainActive)
		{
			return false;
		}

		var lastCall = StatusHelper.PlayerWillStatusEnd(EmergencyTacticsLastCallSeconds, true, StatusID.EmergencyTactics);
		var aoeHp = lastCall ? 1f : EmergencyTacticsAoeHp;
		var singleHp = lastCall ? 1f : EmergencyTacticsSingleHp;

		if (!AccretionGroupHealBlocked && CountInjured(SuccorRange, aoeHp) >= AoeInjuredCount)
		{
			if (HasSeraphism && AccessionPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true, skipAoeCheck: true))
			{
				return true;
			}

			if (!HasSeraphism &&
				(ConcitationPvE.EnoughLevel
					? ConcitationPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)
					: SuccorPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)))
			{
				return true;
			}
		}

		var lowest =
			FindLowest(SingleHealRange, m => SpotHealWorthy(m) && EffectiveHpRatio(m) < singleHp) ??
			(lastCall ? FindLowest(SingleHealRange, m => EffectiveHpRatio(m) < singleHp) : null);
		if (lowest != null && BeirutaTarget.TryUseOn(HasSeraphism ? ManifestationPvE : AdloquiumPvE, lowest, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Steps

	private const float TankbusterLeadSeconds = BeirutaIncoming.TankbusterLeadSeconds;

	public bool PlanAbility(IAction nextGCD, out IAction? act)
	{
		if (TryDeploymentChainAbility(out act))
		{
			return true;
		}

		return (ShouldSwiftcastForAdloquium() || ShouldSwiftcastForSuccor()) && SwiftcastPvE.CanUse(out act);
	}

	public bool SpecialAbility(IAction nextGCD, out IAction? act) => TryAccretionAbility(out act);

	public bool BurstAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (IsBurst && !CombatElapsedLess(ChainStratagemOpenerSeconds) && ChainStratagemPvE.CanUse(out act))
		{
			StampChainStratagemUse();
			return true;
		}

		return false;
	}

	public bool ResourceAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (OpenerWeaveHold)
		{
			return false;
		}

		if (CombatTime > 5 &&
			HasAetherflow &&
			FreshAetherflowSoon &&
			SCHAetherFlowStacks > PlannedAetherflowNeed &&
			EnergyDrainPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!HasAetherflow && TryAetherflowRefill(out act))
		{
			return true;
		}

		if (!BurstEnergyDrainPending && ShouldUseBanefulImpaction(out act))
		{
			return true;
		}

		if (!AccretionGroupHealBlocked &&
			(FirstConsolationAsapValue && ConsolationPvE.Cooldown.CurrentCharges == 2 || SeraphTime > 0 && SeraphTime < 3) &&
			ConsolationPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (ShouldRemoveAetherpact())
		{
			act = AetherpactPvE;
			return true;
		}

		if (CanRunHealEngine &&
			RecitationAvailableForHealing &&
			StatusHelper.PlayerStatusTime(true, StatusID.Recitation) <= RecitationSalvageSeconds &&
			(EnemyAggroTank ?? FindLowest(SingleHealRange, IsTankMember)) is { } salvageTank &&
			TryExcogitationOn(salvageTank, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryAetherflowRefill(out IAction? act)
	{
		var dissipationOk = IsBurst && !DissipationBlockedByPlan;
		if (AetherflowFirstValue)
		{
			return AetherflowPvE.CanUse(out act) || (dissipationOk && DissipationPvE.CanUse(out act));
		}

		return (dissipationOk && DissipationPvE.CanUse(out act)) || AetherflowPvE.CanUse(out act);
	}

	public bool AreaHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (!HoldAoe && TryAoeHealAbility(out act))
		{
			return true;
		}

		return CanPressRecitation &&
			nextGCD != null &&
			nextGCD.IsTheSameTo(false, SuccorPvE, ConcitationPvE) &&
			RecitationPvE.CanUse(out act);
	}

	public bool SpotHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (TopUpAllTarget(SingleHealRange) is { } topUp &&
			CanSpendAetherflow &&
			BeirutaTarget.TryUseOn(LustratePvE, topUp, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m)) is { } undeadTank &&
			TryExcogitationOn(undeadTank, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && HealHp(m) < CriticalHp) is { } critical &&
			TryExcogitationOn(critical, out act))
		{
			return true;
		}

		if (FindLowest(SingleHealRange, m => SpotHealWorthy(m) && SpotHealOk(m) && HealHp(m) < ExcogitationTankHealValue) is { } lowTank &&
			TryExcogitationOn(lowTank, out act))
		{
			return true;
		}

		return TryAetherpactLink(out act);
	}

	private bool TryAetherpactLink(out IAction? act)
	{
		act = null;
		if (HasPartyMemberWithOwnStatus(StatusID.FeyUnion_1223) ||
			AoeNeeded(SingleHealRange, 0.8f) ||
			DissipationSoonForAetherpact ||
			InDamageBurst ||
			FairyGauge <= AetherpactMinimumGauge)
		{
			return false;
		}

		var linkTarget =
			FindLowest(SingleHealRange, static m => IsTankMember(m) && HealHp(m) < AetherpactRemoveHpThreshold && !AccretionLinkBlocked(m)) ??
			FindLowest(SingleHealRange, static m => SpotHealWorthy(m) && HealHp(m) <= AetherpactStartHpThreshold && !AccretionLinkBlocked(m));

		return linkTarget != null && BeirutaTarget.TryUseOn(AetherpactPvE, linkTarget, out act);
	}

	public bool TankHelpAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (!CanRunHealEngine)
		{
			return false;
		}

		if (BeirutaIncoming.TankbusterWithin(TankbusterLeadSeconds) &&
			(FindLowest(SingleHealRange, IsFreshMitigatingTank) ?? TankToHelp(CurrentTarget)) is { } busterTank &&
			(TryExcogitationOn(busterTank, out act) || TryProtractionOn(busterTank, out act)))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsFreshMitigatingTank(member) &&
				IsHealableInRange(member, SingleHealRange) &&
				(TryExcogitationOn(member, out act) || TryProtractionOn(member, out act)))
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
		if (!CanRunHealEngine)
		{
			return false;
		}

		return _raidwide.Coming &&
			FindLowest(SingleHealRange, static m => !IsTankMember(m) && !m.HasStatus(false, StatusID.Protraction)) is { } raidwideTarget &&
			TryProtractionOn(raidwideTarget, out act);
	}

	public bool PlanGCD(out IAction? act)
	{
		act = null;
		return _deploymentChainFromPlan && TryUseDeploymentAdloquium(out act);
	}

	public bool SpecialGCD(out IAction? act)
	{
		act = null;
		if (DoomTarget(SingleHealRange) is { } doomed &&
			!DeploymentChainActive &&
			BeirutaTarget.TryUseOn(HasSeraphism ? ManifestationPvE : AdloquiumPvE, doomed, out act))
		{
			return true;
		}

		return BeirutaAccretion.FindTarget(PartyMembers) is { } target &&
			HasEmergencyTactics &&
			!HasSwift &&
			!ProtectRecitationForDeployment &&
			BeirutaTarget.TryUseOn(AdloquiumPvE, target, out act);
	}

	public bool WardGCD(out IAction? act) => TryWardModeGCD(out act);

	public bool AreaHealGCD(out IAction? act)
	{
		if (TryEmergencyTacticsGCD(out act))
		{
			return true;
		}

		act = null;
		return CanRunHealEngine &&
			!RecitationReservedForPlan &&
			!DeploymentChainActive &&
			SeraphismAoeHealNeeded &&
			AccessionPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true);
	}

	public bool SpotHealGCD(out IAction? act)
	{
		act = null;
		return CanRunHealEngine &&
			HasSeraphism &&
			!RecitationReservedForPlan &&
			!DeploymentChainActive &&
			FindLowest(SingleHealRange, static m => IsTankMember(m) && BeirutaCritical.ForGcd(m, CriticalHp)) is { } criticalTank &&
			BeirutaTarget.TryUseOn(ManifestationPvE, criticalTank, out act);
	}

	#endregion

	#region Hooks

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		UpdateActionTracking();
		return BeirutaHealerFlow.Ability(this, nextGCD, out act) || base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act) =>
		base.GeneralAbility(nextGCD, out act) || LowPriorityAbility(nextGCD, out act);

	[RotationDesc(
		ActionID.ChainStratagemPvE,
		ActionID.EnergyDrainPvE,
		ActionID.BanefulImpactionPvE,
		ActionID.AetherflowPvE,
		ActionID.DissipationPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		UpdateActionTracking();

		if (ShouldSwiftcastForMovement(nextGCD) && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (BurstAbility(nextGCD, out act) || ResourceAbility(nextGCD, out act))
		{
			return true;
		}

		if (!AccretionUp && InChainStratagemBurstWindow &&
			SCHAetherFlowStacks > EnergyDrainKeep &&
			EnergyDrainPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act) =>
		BeirutaHealerFlow.UrgentGcd(this, out act) || base.EmergencyGCD(nextGCD, out act);

	#endregion

	#region Accretion Top Up

	private const float AccretionExcogHpThreshold = 0.5f;

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		var target = BeirutaAccretion.FindTarget(PartyMembers);
		if (target == null)
		{
			return false;
		}

		if (HealHp(target) < AccretionExcogHpThreshold &&
			!ProtectRecitationForDeployment &&
			BeirutaTarget.TryUseOn(ExcogitationPvE, target, out act))
		{
			return true;
		}

		if (BeirutaTarget.TryUseOn(LustratePvE, target, out act))
		{
			return true;
		}

		if (!HasEmergencyTactics && !DeploymentChainActive && EmergencyTacticsPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryDeploymentChainAbility(out IAction? act)
	{
		act = null;
		if (ShouldUseRecitationForDeploymentTactics() && RecitationPvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldUseDeploymentTactics() &&
			DeploymentGalvanizeTarget is { } deployTarget &&
			BeirutaTarget.TryUseOn(DeploymentTacticsPvE, deployTarget, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region GCD Logic

	[RotationDesc(ActionID.ConcitationPvE)]
	protected override bool DefenseAreaGCD(out IAction? act)
	{
		UpdateActionTracking();

		if (RecitationReservedForPlan)
		{
			return base.DefenseAreaGCD(out act);
		}

		if (BeirutaMove.RaiseSwiftHeld)
		{
			return base.DefenseAreaGCD(out act);
		}

		if (!SignalDefenseArea &&
			!DeploymentChainActive &&
			CountUnshielded(SuccorRange) >= BeirutaWard.UnshieldedPartyCount &&
			(ConcitationPvE.EnoughLevel
				? ConcitationPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)
				: SuccorPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)))
		{
			return true;
		}

		return base.DefenseAreaGCD(out act);
	}

	[RotationDesc(ActionID.ResurrectionPvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		UpdateActionTracking();

		if (ResurrectionPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	private bool _lastWardMode;

	protected override void UpdateInfo()
	{
		BeirutaWard.Track(this, WardMode, ref _lastWardMode);
		_raidwide.Track(InCombat, PartyMembers);
		TrackDeath();
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

		if (RecitationReservedForPlan && !WardActive)
		{
			return false;
		}

		var shieldWanted =
			(WardActive && WardNonTanksUnshielded) ||
			(BeirutaAccretion.GuardActive(PartyMembers) && CountUnshielded(SuccorRange) >= BeirutaWard.UnshieldedPartyCount);

		if (!shieldWanted ||
			HasEmergencyTactics ||
			ProtectRecitationForDeployment ||
			DeploymentChainActive)
		{
			return false;
		}

		if (HasSeraphism && AccessionPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true, skipAoeCheck: true))
		{
			return true;
		}

		if (ConcitationPvE.EnoughLevel && ConcitationPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
		{
			return true;
		}

		if (!ConcitationPvE.EnoughLevel && SuccorPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
		{
			return true;
		}

		act = null;
		return false;
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		UpdateActionTracking();

		if (BeirutaMove.RaiseSwiftHeld)
		{
			return base.GeneralGCD(out act);
		}

		if (TryUseDeploymentAdloquium(out act))
		{
			return true;
		}

		if (BeirutaHealerFlow.HealGcd(this, out act))
		{
			return true;
		}

		if (ShouldUseSummonEos() &&
			SummonEosPvE.CanUse(out act))
		{
			return true;
		}

		var nearbyHostiles = AttackableHostilesInRangeOf(5);
		var expectedHPToLive12Seconds = ExpectedHpToLive12Seconds(Player);

		if (InCombat &&
			InFirst20sAfterChainStratagem &&
			CurrentTargetInLast5sOfChainStratagem &&
			nearbyHostiles < GetAoWBreakevenTargets() &&
			CurrentTargetBioMissingOrEnding(ChainStratagemDotRefreshSeconds) &&
			CurrentTarget != null &&
			CurrentTarget.CurrentHp >= expectedHPToLive12Seconds &&
			CanUseCurrentBio(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (TryDamageGCD(nearbyHostiles, expectedHPToLive12Seconds, out act))
		{
			return true;
		}

		if (TryMovementGCD(nearbyHostiles, expectedHPToLive12Seconds, out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Decision Helpers

	private const int RuinIiWeaveAbilities = 2;

	private static bool WeaveRoomWanted => BeirutaHealerFlow.WeaveRoomWanted(RuinIiWeaveAbilities);

	#region Deployment Tactics Chain

	private bool DeploymentChainActive =>
		_deploymentChainStartedAtMs != 0 &&
		Environment.TickCount64 - _deploymentChainStartedAtMs < (_deploymentChainFromPlan ? PlannedDeploymentChainWindowMs : DeploymentChainWindowMs);

	private bool PlannedDeploymentDue =>
		Updaters.BeirutaTimelineUpdater.HasPlan &&
		Updaters.BeirutaTimelineUpdater.PlannedWithin(0f, AdloquiumPvE.ID, ManifestationPvE.ID) &&
		Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedDeploymentChainLeadSeconds, DeploymentTacticsPvE.ID);

	private static bool HasDefenseAreaSignal => MergedStatus.HasFlag(AutoStatus.DefenseArea);

	private bool DeploymentSignalActive =>
		!Updaters.BeirutaTimelineUpdater.HasPlan &&
		DeploymentSignalSeen;

	private bool DeploymentSignalSeen =>
		(SignalProtraction && HasProtraction) ||
		(SignalDefenseArea && HasDefenseAreaSignal) ||
		(SignalRecitation && HasRecitation) ||
		(SignalFeyIllumination && StatusHelper.PlayerHasStatus(false, StatusID.FeyIllumination)) ||
		(SignalSurecast && StatusHelper.PlayerHasStatus(true, StatusID.Surecast)) ||
		(SignalSprint && StatusHelper.PlayerHasStatus(true, StatusID.Sprint)) ||
		(SignalSwiftcast && HasSwift && !MergedStatus.HasFlag(AutoStatus.Raise));

	private bool _deploymentChainUsesRecitation;

	private bool DeploymentChainWantsRecitation => RecitationPvE.EnoughLevel;

	private bool RecitationReadyForChain =>
		HasRecitation || RecitationPvE.Cooldown.WillHaveOneChargeGCD(1);

	private bool DeploymentChainNeedsRecitation => _deploymentChainUsesRecitation;

	private bool DeploymentTacticsReadySoon =>
		DeploymentTacticsPvE.EnoughLevel &&
		DeploymentTacticsPvE.Cooldown.WillHaveOneChargeGCD(2);

	private bool CanStartDeploymentChain()
	{
		return DeploymentTacticsReadySoon && DeploymentAdloTarget() != null;
	}

	private bool _deploymentSignalSeen;

	private void UpdateDeploymentChain()
	{
		var signal = DeploymentSignalActive;
		var newSignal = signal && !_deploymentSignalSeen;
		_deploymentSignalSeen = signal;

		if (DeploymentChainActive)
		{
			if (IsLastAbility(true, DeploymentTacticsPvE) || !DeploymentTacticsReadySoon)
			{
				_deploymentChainStartedAtMs = 0;
				_deploymentTarget = null;
			}
			return;
		}

		_deploymentChainStartedAtMs = 0;
		_deploymentChainFromPlan = false;

		if (PlannedDeploymentDue && CanStartDeploymentChain())
		{
			_deploymentChainStartedAtMs = Environment.TickCount64;
			_deploymentChainStartedAt = DateTime.Now;
			_deploymentTarget = null;
			_deploymentChainFromPlan = true;
			_deploymentChainUsesRecitation = DeploymentChainWantsRecitation &&
				(HasRecitation || Updaters.BeirutaTimelineUpdater.PlannedWithin(PlannedDeploymentChainLeadSeconds, RecitationPvE.ID));
		}
		else if (signal && CanStartDeploymentChain())
		{
			_deploymentChainStartedAtMs = Environment.TickCount64;
			_deploymentChainStartedAt = DateTime.Now;
			_deploymentTarget = null;
			_deploymentChainUsesRecitation = DeploymentChainWantsRecitation && RecitationReadyForChain;
			ECommons.DalamudServices.Svc.Toasts.ShowQuest("Preparing Deployment Tactics");
		}
		else if (newSignal && DeploymentTacticsPvE.EnoughLevel && !DeploymentTacticsReadySoon)
		{
			var remain = MathF.Ceiling(DeploymentTacticsPvE.Cooldown.RecastTimeRemainOneCharge);
			ECommons.DalamudServices.Svc.Toasts.ShowQuest($"Deployment Tactics on cooldown ({remain:0}s)");
		}

	}

	private IBattleChara? DeploymentAdloTarget()
	{
		var protraction = FindLowest(SingleHealRange, static m =>
			!IsTankMember(m) &&
			m.HasStatus(true, StatusID.Protraction));
		if (protraction != null)
		{
			return protraction;
		}

		var injured = FindLowest(SingleHealRange, static m =>
			!IsTankMember(m) &&
			HealHp(m) < 1f);
		if (injured != null)
		{
			return injured;
		}

		return ECommons.GameHelpers.Player.Object;
	}

	private const float GalvanizeDuration = 30f;
	private const float GalvanizeFreshSlackSeconds = 1.5f;

	private static bool IsCastingDeploymentAdlo =>
		ECommons.GameHelpers.Player.Object is { IsCasting: true } player &&
		(player.CastActionId == (uint)ActionID.AdloquiumPvE || player.CastActionId == (uint)ActionID.ManifestationPvE);

	private DateTime? DeploymentAdloUsedAt
	{
		get
		{
			if (_deploymentChainStartedAt == default)
			{
				return null;
			}

			foreach (var rec in RecordActions)
			{
				if (rec.UsedTime < _deploymentChainStartedAt)
				{
					break;
				}

				var id = rec.Action.RowId;
				if (id == (uint)ActionID.AdloquiumPvE || id == (uint)ActionID.ManifestationPvE)
				{
					return rec.UsedTime;
				}
			}

			return null;
		}
	}

	private bool DeploymentAdloDone => IsCastingDeploymentAdlo || DeploymentAdloUsedAt != null;

	private IBattleChara? DeploymentGalvanizeTarget
	{
		get
		{
			if (DeploymentAdloUsedAt is not { } usedAt)
			{
				return null;
			}

			var minRemain = GalvanizeDuration - (float)(DateTime.Now - usedAt).TotalSeconds - GalvanizeFreshSlackSeconds;

			bool Fresh(IBattleChara? m) =>
				m != null &&
				!m.IsDead &&
				m.HasStatus(true, StatusID.Galvanize) &&
				m.StatusTime(true, StatusID.Galvanize) >= minRemain;

			if (Fresh(_deploymentTarget))
			{
				return _deploymentTarget;
			}

			foreach (var member in PartyMembers)
			{
				if (Fresh(member))
				{
					return member;
				}
			}

			return null;
		}
	}

	private bool ShouldUseRecitationForDeploymentTactics() =>
		DeploymentChainActive &&
		DeploymentChainNeedsRecitation &&
		!HasRecitation &&
		!DeploymentAdloDone;

	private bool ShouldUseDeploymentAdloquium() =>
		DeploymentChainActive &&
		!DeploymentAdloDone &&
		(HasRecitation || !DeploymentChainNeedsRecitation || !RecitationReadyForChain);

	private bool TryUseDeploymentAdloquium(out IAction? act)
	{
		act = null;

		if (RecitationReservedForPlan && !DeploymentChainActive)
		{
			return false;
		}

		if (!ShouldUseDeploymentAdloquium() || DeploymentAdloTarget() is not { } target)
		{
			return false;
		}

		var adlo = HasSeraphism ? ManifestationPvE : AdloquiumPvE;
		if (BeirutaTarget.TryUseOn(adlo, target, out act))
		{
			_deploymentTarget = target;
			StampAdloquiumUse();
			return true;
		}

		act = null;
		return false;
	}

	private bool ShouldUseDeploymentTactics() =>
		DeploymentGalvanizeTarget != null &&
		(DeploymentChainActive || InFirst5sAfterAdloquium);
	private bool ProtectRecitationForDeployment => HasRecitation && DeploymentChainActive;

	#endregion

	private bool BurstEnergyDrainPending =>
		!AccretionUp && InChainStratagemBurstWindow &&
		SCHAetherFlowStacks > EnergyDrainKeep &&
		EnergyDrainPvE.CanUse(out _, usedUp: true);

	private bool ShouldUseBanefulImpaction(out IAction? act)
	{
		act = null;

		if (DelayBanefulImpaction && InFirst5sAfterChainStratagem)
		{
			return false;
		}

		return BanefulImpactionPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true, skipTTKCheck: true);
	}

	#endregion

	#region Party / Target Helpers

	private bool ShouldRemoveAetherpact()
	{
		if (InDamageBurst)
		{
			return false;
		}

		foreach (var member in PartyMembers)
		{
			if (member.HasStatus(true, StatusID.FeyUnion_1223) &&
				(member.IsDead || IsHealBlocked(member) || HealHp(member) >= AetherpactRemoveHpThreshold))
			{
				return true;
			}
		}

		return false;
	}

	#endregion

	#region Damage / DoT Helpers

	private IBaseAction CurrentBio => BiolysisPvE.EnoughLevel ? BiolysisPvE : BioIiPvE.EnoughLevel ? BioIiPvE : BioPvE;

	private IBaseAction CurrentFiller =>
		BroilIvPvE.EnoughLevel ? BroilIvPvE :
		BroilIiiPvE.EnoughLevel ? BroilIiiPvE :
		BroilIiPvE.EnoughLevel ? BroilIiPvE :
		BroilPvE.EnoughLevel ? BroilPvE : RuinPvE;

	private bool TryDamageGCD(int nearbyHostiles, float hpToLive, out IAction? act)
	{
		var bio = CurrentBio;
		if (bio.CanUse(out act) &&
			nearbyHostiles < GetAoWBreakevenTargets() &&
			BioWorthBeforeDowntime(bio) &&
			bio.Target.Target.CurrentHp >= hpToLive)
		{
			return true;
		}

		var artOfWarMin = BroilMasteryTrait.EnoughLevel ? 1 : 0;
		if (InCombat && nearbyHostiles > artOfWarMin && ArtOfWarPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (WeaveRoomWanted && RuinIiPvE.CanUse(out act))
		{
			return true;
		}

		if (CurrentFiller.CanUse(out act))
		{
			return true;
		}

		if (InCombat && !BroilMasteryIiTrait.EnoughLevel && nearbyHostiles > 0 && ArtOfWarPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return !BioIiPvE.EnoughLevel && BioPvE.CanUse(out act, skipTTKCheck: true);
	}
	private int GetAoWBreakevenTargets()
	{
		if (!ArtOfWarPvE.EnoughLevel)
		{
			return 100;
		}

		if (!BroilMasteryTrait.EnoughLevel)
		{
			return 3 - DotOffsetMobs;
		}

		if (!ArtOfWarMasteryTrait.EnoughLevel)
		{
			return 4 - DotOffsetMobs;
		}

		return 5 - DotOffsetMobs;
	}

	private bool CurrentTargetBioMissingOrEnding(float remainingSeconds)
	{
		if (CurrentTarget == null)
		{
			return false;
		}

		return
			(BiolysisPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.Biolysis) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Biolysis))) ||
			(!BiolysisPvE.EnoughLevel && BioIiPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.BioIi) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.BioIi))) ||
			(!BiolysisPvE.EnoughLevel && !BioIiPvE.EnoughLevel && BioPvE.EnoughLevel &&
			 (!CurrentTarget.HasStatus(true, StatusID.Bio) ||
			  CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Bio)));
	}

	private const float BioInitialPotency = 0f;
	private const float BioTickPotency = 80f;
	private const float BroilFillerPotency = 310f;

	private static bool BioWorthBeforeDowntime(IBaseAction bio) =>
		BeirutaDotValue.WorthBeforeDowntime(bio.Target.Target, BioInitialPotency, BioTickPotency, BroilFillerPotency);

	private bool CanUseCurrentBio(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (BiolysisPvE.EnoughLevel &&
			BiolysisPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			BioWorthBeforeDowntime(BiolysisPvE))
		{
			return true;
		}

		if (!BiolysisPvE.EnoughLevel && BioIiPvE.EnoughLevel &&
			BioIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			BioWorthBeforeDowntime(BioIiPvE))
		{
			return true;
		}

		if (!BiolysisPvE.EnoughLevel && !BioIiPvE.EnoughLevel && BioPvE.EnoughLevel &&
			BioPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck) &&
			BioWorthBeforeDowntime(BioPvE))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Timestamp Helpers

	private void StampChainStratagemUse() => _chainStratagemUsedAtMs = Environment.TickCount64;

	private void StampAdloquiumUse() => _adloquiumUsedAtMs = Environment.TickCount64;

	private List<IBattleChara> CountdownTanksInOrder()
	{
		List<IBattleChara> ordered = [];
		var priority = OtherConfiguration.AdloquiumTankPriority;

		void AddByPriority(Func<IBattleChara, bool> match)
		{
			foreach (var job in priority)
			{
				foreach (var member in PartyMembers)
				{
					if (member != null &&
						!member.IsDead &&
						member.IsJobCategory(JobRole.Tank) &&
						member.IsJobs(job) &&
						match(member) &&
						!ordered.Contains(member))
					{
						ordered.Add(member);
					}
				}
			}

			foreach (var member in PartyMembers)
			{
				if (member != null &&
					!member.IsDead &&
					member.IsJobCategory(JobRole.Tank) &&
					match(member) &&
					!ordered.Contains(member))
				{
					ordered.Add(member);
				}
			}
		}

		if (CountdownTankPick == CountdownTankPickStrategy.Stance)
		{
			AddByPriority(static m => m.HasStatus(false, StatusHelper.TankStanceStatus));
		}

		AddByPriority(static _ => true);
		return ordered;
	}

	private const float DefaultCountdownDeploymentAt = 3.5f;

	private static float CountdownDeploymentAt =>
		Updaters.BeirutaTimelineUpdater.PlanOption("CountdownDeploymentAt") is { } v &&
		float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds) &&
		seconds > 0
			? seconds
			: DefaultCountdownDeploymentAt;
	private bool TryCountdownTankAdloquium(int tankIndex, out IAction? act)
	{
		act = null;

		var tanks = CountdownTanksInOrder();
		if (tankIndex >= tanks.Count)
		{
			return false;
		}

		var tank = tanks[tankIndex];
		if (tank.HasStatus(false, StatusID.Galvanize) ||
			tank.DistanceToPlayer() > 30f ||
			!BeirutaTarget.TryUseOn(AdloquiumPvE, tank, out act))
		{
			act = null;
			return false;
		}

		StampAdloquiumUse();
		return true;
	}

	#endregion

}
