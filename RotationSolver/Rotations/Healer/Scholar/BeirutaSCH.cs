namespace RotationSolver.Rotations.Healer.Scholar;

[Rotation("BeirutaSCH", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Scholar. Damage and healing are automatic; mitigation comes from your CD planner or your own presses. AutoBurst off is enough to hold burst. Set Intercept to GCD only and leave ability intercept unticked, so your planner's oGCDs go straight through and any extra heal or shield GCD you press is queued. Questions? Ask @Beiruta on the RSR Discord. Hover each setting for details.")]
[SourceCode(Path = "main/Rotations/Healer/Scholar/BeirutaSCH.cs")]
[ExtraRotation]

public sealed class BeirutaSCH : ScholarRotation
{
	#region Config Options

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps the party shielded and heals sooner.\n" +
		"- Succor / Concitation (Accession under Seraphism) whenever 4+ are unshielded.\n" +
		"- AoE thresholds use real HP and ignore the co-healer and HoTs.\n" +
		"- Fairy order: Fey Blessing > Whispering Dawn > Indomitability.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Tooltip =
		"What SCH puts up during the countdown. Deployment options spread the Adloquium shield to the party; Recitation makes it critical.")]
	public CountdownOpenerStrategy CountdownOpener { get; set; } = CountdownOpenerStrategy.RecitationAdloquiumDeploymentTactics;

	[RotationConfig(CombatType.PvE, Name = "    > Adloquium tank pick", Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Chain Stratagem", Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Damage] Energy Drain in burst", Tooltip =
		"How many Aetherflow stacks are spent on Energy Drain during burst. Leftovers are always dumped before Aetherflow / Dissipation comes back.")]
	public EnergyDrainStrategy EnergyDrainUsage { get; set; } = EnergyDrainStrategy.AllInBurst;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Delay Baneful Impaction 5s after Chain Stratagem")]
	public bool DelayBanefulImpaction { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Aetherflow before Dissipation", Tooltip =
		"When both are available, Aetherflow goes first and Dissipation is kept for later.")]
	public bool PrioritizeAetherflowOverDissipation { get; set; } = false;

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

	#region Deployment Tactics

	[RotationConfig(CombatType.PvE, Name = "[Deployment Tactics] Restrict Recitation", Tooltip =
		"Deployment Tactics only: Recitation is kept for preparing Deployment Tactics.\n" +
		"Deployment Tactics, or heals: when Deployment Tactics is not being prepared, Recitation also goes on Indomitability, Excogitation or Succor.")]
	public RecitationUsageStrategy RecitationUsage { get; set; } = RecitationUsageStrategy.Flexible;

	[RotationConfig(CombatType.PvE, Name = "[Deployment Tactics] Recitation before Adloquium", Tooltip =
		"Preparing Deployment Tactics becomes Recitation - Adloquium - Deployment Tactics, spreading a critical shield.\n" +
		"With Restrict Recitation = Deployment Tactics only, preparation waits for Recitation.")]
	public bool OnlyUseDeploymentTacticsOnCriticalShields { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Deployment Tactics] Custom triggers (off = your Protraction on yourself)", Tooltip =
		"A trigger is something you do that tells the rotation to prepare Deployment Tactics: Adloquium on the right target, then Deployment Tactics, within 15s.\n" +
		"A toast shows Preparing Deployment Tactics, or the cooldown left if it is not ready.")]
	public bool UseCustomDeploymentSignals { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Protraction on yourself", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalProtraction { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > RSR raidwide warning (DefenseArea)", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"When RSR's DefenseArea state is on (its raidwide warning or your DefenseArea macro), Deployment Tactics is prepared instead of a Succor shield.")]
	public bool DeploymentSignalDefenseArea { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Recitation", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"Pressing Recitation yourself prepares Deployment Tactics; the rotation then never spends it on heals.")]
	public bool DeploymentSignalRecitation { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Fey Illumination", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalFeyIllumination { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Surecast", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalSurecast { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Sprint", Parent = nameof(UseCustomDeploymentSignals))]
	public bool DeploymentSignalSprint { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Swiftcast (not while raising)", Parent = nameof(UseCustomDeploymentSignals), Tooltip =
		"Your own Swiftcast prepares Deployment Tactics. The rotation then stops using Swiftcast for movement or Succor.")]
	public bool DeploymentSignalSwiftcast { get; set; } = false;

	public enum RecitationUsageStrategy : byte
	{
		[Description("Deployment Tactics only")]
		DeploymentOnly,

		[Description("Deployment Tactics, or heals")]
		Flexible,
	}

	private bool SignalProtraction => (!UseCustomDeploymentSignals || DeploymentSignalProtraction) && ProtractionUsageValue != ProtractionUsageStrategy.HelpOnly;
	private bool SignalDefenseArea => UseCustomDeploymentSignals && DeploymentSignalDefenseArea;
	private bool SignalRecitation => UseCustomDeploymentSignals && DeploymentSignalRecitation;
	private bool SignalFeyIllumination => UseCustomDeploymentSignals && DeploymentSignalFeyIllumination;
	private bool SignalSurecast => UseCustomDeploymentSignals && DeploymentSignalSurecast;
	private bool SignalSprint => UseCustomDeploymentSignals && DeploymentSignalSprint;
	private bool SignalSwiftcast => UseCustomDeploymentSignals && DeploymentSignalSwiftcast;

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Biolysis refresh", Tooltip =
		"While moving, refreshes Biolysis early instead of using Ruin II.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Custom Swiftcast usage (off = default)", Tooltip =
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

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Tooltip =
		"Off: AoE thresholds come from each heal's potency, the co-healer's smallest GCD heal and incoming HoT ticks, so heals are not wasted; Protraction is used to help.\n" +
		"On: the values below replace the Ward-off thresholds. Heal area = 3+ players below that HP, like RSR's own heal-area settings. Ward Mode always uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Automatic Protraction", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"Automatic: Protraction on mitigating tanks, critical players and the lowest non-tank before raidwides.\n" +
		"Your own Protraction on yourself can also be a Deployment trigger (Deployment Tactics settings).")]
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
	[RotationConfig(CombatType.PvE, Name = "    > Whispering Dawn: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float WhisperingDawnHeal { get; set; } = 0.85f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Fey Blessing: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float FeyBlessingHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Indomitability: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float IndomitabilityHeal { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Consolation: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float ConsolationHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Accession: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float AccessionHeal { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Excogitation: tank below", Parent = nameof(UseCustomHPThreshold))]
	public float ExcogitationTankHeal { get; set; } = 0.5f;

	private bool FirstConsolationAsapValue => !UseCustomHPThreshold || UseFirstConsolationAsapWhenSeraphIsOut;
	private ProtractionUsageStrategy ProtractionUsageValue => UseCustomHPThreshold ? ProtractionUsage : ProtractionUsageStrategy.Both;
	private float ExcogitationTankHealValue => UseCustomHPThreshold ? ExcogitationTankHeal : 0.5f;

	#endregion

	#region DMU

	[RotationConfig(CombatType.PvE, Name = "[DMU] Experimental Accretion top-up", Tooltip =
		"Fills Accretion players in order: the healer first, then the DPS once Earth Resistance Down II is gone.\n" +
		"Waits until 3+ non-tanks other than the target are above 30%, and 3s after Accretion changes.\n" +
		"SCH: Excogitation (target below 50%) > Lustrate > Emergency Tactics, then Adloquium under Emergency Tactics.\n" +
		"Always active in DMU, even with this off: group heals stop once an Accretion player is above 50%, the party is kept shielded as in Ward Mode, and no Aetherpact goes on Accretion players.")]
	public bool AccretionTopUp { get; set; } = false;

	#endregion

	#endregion

	#region Constants / Fields

	private const long ChainStratagemBanefulBlockMs = 5_000;
	private const long ChainStratagemDotRefreshMs = 20_000;
	private const long DeploymentTacticsAfterAdloquiumWindowMs = 5_000;

	private const float ChainStratagemDotRefreshSeconds = 11f;
	private const float AetherpactRemoveHpThreshold = 0.9f;
	private const float AetherpactStartHpThreshold = 0.8f;
	private const float BallparkDamagePercent = 0.08f;

	private const int DotOffsetMobs = 1;

	private const bool EnableBallparkTtkEstimator = true;

	private long _chainStratagemUsedAtMs;
	private long _adloquiumUsedAtMs;

	private const long DeploymentChainWindowMs = 15_000;
	private long _deploymentChainStartedAtMs;
	private DateTime _deploymentChainStartedAt;
	private IBattleChara? _deploymentTarget;
	private const double DeploymentAdloLandSeconds = 3d;

	private readonly BeirutaRaidwide _raidwide = new();

	#endregion

	#region Simple Status Helpers

	private bool HasGalvanize => StatusHelper.PlayerHasStatus(true, StatusID.Galvanize);
	private bool HasProtraction => StatusHelper.PlayerHasStatus(true, StatusID.Protraction);
	private bool HasSeraphism => StatusHelper.PlayerHasStatus(true, StatusID.Seraphism);

	private bool HasWhisperingDawn => HasPartyMemberWithOwnStatus(StatusID.WhisperingDawn);
	private bool HasAngelsWhisper => HasPartyMemberWithOwnStatus(StatusID.AngelsWhisper);

	private const float MovementGraceSeconds = 0.3f;
	private const float MovementTimeThreshold = 1.0f;
	private const float MovementShieldHealHp = 0.95f;
	private const float BiolysisWorthRefreshSeconds = 17f;

	private bool MovingPastGrace =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementGraceSeconds;

	private bool HasSufficientMovement =>
		InCombat &&
		IsMoving &&
		MovingTime > MovementTimeThreshold;

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

	private static bool IsPartyBurst => PartyHasStatus(static id => PartyRaidBuffStatusIds.Contains(id));

	private bool InDamageBurst => InChainStratagemBurstWindow || IsPartyBurst;

	private static int AttackableHostilesInRangeOf(float range)
	{
		if (AllHostileTargets == null)
		{
			return 0;
		}

		var count = 0;
		foreach (var enemy in AllHostileTargets)
		{
			if (enemy != null && enemy.IsAttackable() && enemy.DistanceToPlayer() < range)
			{
				count++;
			}
		}
		return count;
	}

	private bool InFirst5sAfterChainStratagem =>
		_chainStratagemUsedAtMs != 0 &&
		Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemBanefulBlockMs;

	private bool InFirst20sAfterChainStratagem =>
		_chainStratagemUsedAtMs != 0 &&
		Environment.TickCount64 - _chainStratagemUsedAtMs < ChainStratagemDotRefreshMs;

	private bool InFirst5sAfterAdloquium =>
		_adloquiumUsedAtMs != 0 &&
		Environment.TickCount64 - _adloquiumUsedAtMs < DeploymentTacticsAfterAdloquiumWindowMs;

	private static bool AnyPartyMemberDead
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member != null && member.IsDead)
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool SwiftcastAvailable =>
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!HasSwift &&
		!IsLastAction(ActionID.SwiftcastPvE) &&
		!ShouldDeferToRaise() &&
		!(SwiftKeepForRaiseValue && AnyPartyMemberDead);

	private bool ShouldSwiftcastForAdloquium() =>
		SwiftForDeploymentValue &&
		!HasSeraphism &&
		HasSufficientMovement &&
		SwiftcastAvailable &&
		ShouldUseDeploymentAdloquium();

	private bool SuccorShieldWanted =>
		!HasSeraphism &&
		!HasEmergencyTactics &&
		!DeploymentChainActive &&
		!ProtectRecitationForDeployment &&
		CountUnshielded(SuccorRange) >= BeirutaWard.UnshieldedPartyCount &&
		(WardMode ||
		 BeirutaAccretion.GuardActive(PartyMembers) ||
		 (!SignalDefenseArea && MergedStatus.HasFlag(AutoStatus.DefenseArea)));

	private bool ShouldSwiftcastForSuccor() =>
		SwiftForSuccorValue &&
		!SignalSwiftcast &&
		HasSufficientMovement &&
		SwiftcastAvailable &&
		SuccorShieldWanted;

	private static bool IsPartyMedicated => PartyHasStatus(static id => id == (uint)StatusID.Medicated);

	private static bool AccretionGroupHealBlocked => BeirutaAccretion.GroupHealBlocked(PartyMembers);

	private bool ShouldUsePotionInCombat =>
		UsePotion &&
		IsBurst &&
		!OpenerWeaveHold &&
		!(PotionTiming == PotTiming.TwoMinutes && CombatElapsedLess(30f)) &&
		(IsPartyMedicated || ChainStratagemPvE.Cooldown.WillHaveOneCharge(5));

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
			!HasSufficientMovement ||
			IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return !FreeMovementInstantAvailable(AttackableHostilesInRangeOf(5), CalculateExpectedHpToLive12Seconds());
	}

	private bool MovementAccessionUseful =>
		HasSeraphism &&
		!HasGalvanize &&
		CountInjured(SuccorRange, MovementShieldHealHp) >= AoeInjuredCount;

	private bool MovementBiolysisDue(float expectedHPToLive12Seconds, int nearbyHostiles) =>
		EarlyDotRefreshWhenMoving &&
		HasSufficientMovement &&
		CurrentTarget != null &&
		CurrentTarget.CurrentHp >= expectedHPToLive12Seconds &&
		nearbyHostiles < GetAoWBreakevenTargets() &&
		CurrentTargetBioMissingOrEnding(BiolysisWorthRefreshSeconds);

	private bool TryMovementGCD(int nearbyHostiles, float expectedHPToLive12Seconds, out IAction? act)
	{
		act = null;

		if (!MovingPastGrace || HasSwift || ShouldDeferToRaise())
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

	private const float OpenerBuffCountdownSeconds = 0.35f;
	private const long OpenerWeaveHoldMs = 3_500;
	private const float ChainStratagemRange = 25f;

	private long _openerBuffSentMs;

	private bool OpenerWeaveHold =>
		_openerBuffSentMs != 0 &&
		Environment.TickCount64 - _openerBuffSentMs < OpenerWeaveHoldMs &&
		!IsLastGCD(true, BiolysisPvE, BioIiPvE, BioPvE);

	private bool TryOpenerChainStratagem(float remainTime, out IAction? act)
	{
		act = null;

		if (ChainStratagemOpener != ChainStratagemOpenerTiming.ZeroSeconds ||
			!IsBurst ||
			remainTime >= OpenerBuffCountdownSeconds ||
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

		ChainStratagemPvE.Target = new TargetResult(target, [target], target.Position);
		act = ChainStratagemPvE;
		_openerBuffSentMs = Environment.TickCount64;
		StampChainStratagemUse();
		return true;
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
			PotionTiming == PotTiming.Opener &&
			UseBurstMedicine(out act))
		{
			return act;
		}

		switch (CountdownOpener)
		{
			case CountdownOpenerStrategy.RecitationAdloquiumDeploymentTactics:
				if (remainTime is < 4f and > 3f && DeploymentTacticsPvE.CanUse(out act))
				{
					return act;
				}

				if (remainTime is < 7f and > 6f &&
					AdloquiumPvE.CanUse(out act, targetOverride: TargetType.Tank))
				{
					StampAdloquiumUse();
					return act;
				}

				if (remainTime is < 14f and > 9f &&
					RecitationPvE.CanUse(out act))
				{
					return act;
				}
				break;

			case CountdownOpenerStrategy.AdloquiumDeploymentTactics:
				if (remainTime is < 4f and > 3f && DeploymentTacticsPvE.CanUse(out act))
				{
					return act;
				}

				if (remainTime is < 7f and > 6f &&
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
	private const float CriticalHp = 0.3f;
	private const float ExtremeLowTankHp = 0.3f;
	private const float UndeadReleaseSeconds = 1f;
	private const float EarthlyDominanceGateSeconds = 3f;
	private const float FreshMitigationSeconds = 6f;
	private const float RampartDuration = 20f;
	private const float BigMitigationDuration = 15f;
	private const float RecitationSalvageSeconds = 4f;

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

	private static readonly StatusID[] CoHealerMacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];

	private bool CanRunHealEngine =>
		Service.Config.AutoHeal &&
		InCombat;

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
		IsHealableInRange(member, FairyHealRange) &&
		!member.HasStatus(false, StatusID.WhisperingDawn, StatusID.AngelsWhisper) &&
		BeirutaHealWindow.OgcdJustUsed(WhisperingDawnPvE)
			? WhisperingDawnCountedPotency
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

	private static bool HoldAoe => AccretionGroupHealBlocked || (CoHealerBigHealSoon && !AnyCritical(SingleHealRange));

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

	#endregion

	#region Aetherflow

	private const float AetherpactDissipationLeadSeconds = 10f;

	private bool DissipationUsable =>
		IsBurst &&
		DissipationPvE.EnoughLevel &&
		DissipationPvE.IsEnabled;

	private static uint AetherflowSpendGcds => (uint)SCHAetherFlowStacks + 1;

	private bool FreshAetherflowSoon =>
		AetherflowPvE.Cooldown.WillHaveOneChargeGCD(AetherflowSpendGcds) ||
		(DissipationUsable && DissipationPvE.Cooldown.WillHaveOneChargeGCD(AetherflowSpendGcds));

	private bool DissipationSoonForAetherpact =>
		DissipationUsable &&
		DissipationPvE.Cooldown.WillHaveOneCharge(AetherpactDissipationLeadSeconds);

	private bool SacredSoilReserve =>
		SacredSoilPvE.EnoughLevel &&
		!FreshAetherflowSoon &&
		SacredSoilPvE.Cooldown.WillHaveOneCharge(Math.Max(5f, AetherflowPvE.Cooldown.RecastTimeRemainOneCharge));

	private bool CanSpendAetherflow =>
		SCHAetherFlowStacks >= 2 ||
		(SCHAetherFlowStacks == 1 && !SacredSoilReserve);

	#endregion

	#region Recitation / Protraction

	private bool RecitationForHealingAllowed =>
		RecitationUsage == RecitationUsageStrategy.Flexible &&
		!SignalRecitation &&
		!DeploymentChainActive;

	private bool CanPressRecitation =>
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
			target.GameObjectId != Player?.GameObjectId &&
			!target.HasStatus(false, StatusID.Protraction) &&
			BeirutaAccretion.TryUseOn(ProtractionPvE, target, out act);
	}

	private bool TryExcogitationOn(IBattleChara target, out IAction? act)
	{
		act = null;

		if (target.HasStatus(false, StatusID.Excogitation, StatusID.Excogitation_2182) ||
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

		return BeirutaAccretion.TryUseOn(ExcogitationPvE, target, out act);
	}

	private bool TryIndomitability(out IAction? act)
	{
		act = null;

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

	private IBattleChara? NonTankTopUp =>
		BeirutaHealModel.NonTankTopUpTarget(PartyMembers, SingleHealRange, EffectiveHpRatio, static m => IsHealBlocked(m), AoeInjuredCount);
	private bool TryHealAbility(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		var undeadTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m));
		if (undeadTank != null && TryExcogitationOn(undeadTank, out act))
		{
			return true;
		}

		var critical = FindLowest(SingleHealRange, static m => m.GetHealthRatio() < CriticalHp);
		if (critical != null &&
			(TryExcogitationOn(critical, out act) || TryProtractionOn(critical, out act)))
		{
			return true;
		}

		var lowTank = FindLowest(SingleHealRange, m => IsTankMember(m) && m.GetHealthRatio() < ExcogitationTankHealValue);
		if (lowTank != null && TryExcogitationOn(lowTank, out act))
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

		if (_raidwide.Coming &&
			FindLowest(SingleHealRange, static m => !IsTankMember(m) && !m.HasStatus(false, StatusID.Protraction)) is { } raidwideTarget &&
			TryProtractionOn(raidwideTarget, out act))
		{
			return true;
		}

		if (!HoldAoe && TryAoeHealAbility(out act))
		{
			return true;
		}

		if (CanPressRecitation &&
			nextGCD != null &&
			nextGCD.IsTheSameTo(false, SuccorPvE, ConcitationPvE) &&
			RecitationPvE.CanUse(out act))
		{
			return true;
		}

		if (RecitationAvailableForHealing &&
			StatusHelper.PlayerStatusTime(true, StatusID.Recitation) <= RecitationSalvageSeconds)
		{
			var tank = EnemyAggroTank ?? FindLowest(SingleHealRange, static m => IsTankMember(m));
			if (tank != null && TryExcogitationOn(tank, out act))
			{
				return true;
			}
		}

		var hasLink = HasPartyMemberWithOwnStatus(StatusID.FeyUnion_1223);
		var aoeCalm = !AoeNeeded(SingleHealRange, 0.8f) && !DissipationSoonForAetherpact;

		if (!hasLink && aoeCalm && !InDamageBurst && FairyGauge > AetherpactMinimumGauge)
		{
			var linkTarget =
				FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < AetherpactRemoveHpThreshold && !AccretionLinkBlocked(m)) ??
				FindLowest(SingleHealRange, static m => m.GetHealthRatio() <= AetherpactStartHpThreshold && !AccretionLinkBlocked(m));

			if (linkTarget != null && BeirutaAccretion.TryUseOn(AetherpactPvE, linkTarget, out act))
			{
				return true;
			}
		}

		if (HasAetherflow &&
			!DeploymentChainActive &&
			NonTankTopUp is { } topUp &&
			BeirutaAccretion.TryUseOn(LustratePvE, topUp, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

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

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) =>
		BeirutaHealModel.Threshold(potency, HealBoost, WardMode, UseCustomHPThreshold, customWardOff, PartyMembers, wardOffCap);

	private float IndomitabilityHp =>
		!WardMode && RecitationAvailableForHealing
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

		if (WardMode)
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

		var lowest = FindLowest(SingleHealRange, m => EffectiveHpRatio(m) < singleHp);
		if (lowest != null && BeirutaAccretion.TryUseOn(HasSeraphism ? ManifestationPvE : AdloquiumPvE, lowest, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region oGCD Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		UpdateActionTracking();

		if (TryPriorityWeaves(out act))
		{
			return true;
		}

		if (TryAccretionAbility(out act))
		{
			return true;
		}

		if (ShouldUseRecitationForDeploymentTactics() && RecitationPvE.CanUse(out act))
		{
			return true;
		}

		if ((ShouldSwiftcastForAdloquium() || ShouldSwiftcastForSuccor()) &&
			SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldUseDeploymentTactics() &&
			DeploymentGalvanizeTarget is { } deployTarget &&
			BeirutaAccretion.TryUseOn(DeploymentTacticsPvE, deployTarget, out act))
		{
			return true;
		}

		if (!AccretionGroupHealBlocked &&
			FirstConsolationAsapValue &&
			ConsolationPvE.Cooldown.CurrentCharges == 2 &&
			ConsolationPvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldRemoveAetherpact())
		{
			act = AetherpactPvE;
			return true;
		}

		if (TryHealAbility(nextGCD, out act))
		{
			return true;
		}

		if (!AccretionGroupHealBlocked && SeraphTime > 0 && SeraphTime < 3 && ConsolationPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}


	[RotationDesc(ActionID.ExcogitationPvE, ActionID.ProtractionPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		UpdateActionTracking();

		var tank =
			FindLowest(SingleHealRange, static m => IsFreshMitigatingTank(m)) ??
			EnemyAggroTank ??
			FindLowest(SingleHealRange, static m => IsTankMember(m));

		if (tank != null &&
			(TryExcogitationOn(tank, out act) || TryProtractionOn(tank, out act)))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(
		ActionID.ChainStratagemPvE,
		ActionID.EnergyDrainPvE,
		ActionID.BanefulImpactionPvE,
		ActionID.AetherflowPvE,
		ActionID.DissipationPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		UpdateActionTracking();

		if (ShouldSwiftcastForMovement(nextGCD) &&
			SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (TryPriorityWeaves(out act))
		{
			return true;
		}

		if (InChainStratagemBurstWindow &&
			SCHAetherFlowStacks > EnergyDrainStacksToKeep &&
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

	#endregion

	#region Accretion Top Up

	private const float AccretionExcogHpThreshold = 0.5f;

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target == null)
		{
			return false;
		}

		if (target.GetHealthRatio() < AccretionExcogHpThreshold &&
			!ProtectRecitationForDeployment &&
			BeirutaAccretion.TryUseOn(ExcogitationPvE, target, out act))
		{
			return true;
		}

		if (BeirutaAccretion.TryUseOn(LustratePvE, target, out act))
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

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		act = null;

		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target != null &&
			HasEmergencyTactics &&
			!HasSwift &&
			!ProtectRecitationForDeployment &&
			BeirutaAccretion.TryUseOn(AdloquiumPvE, target, out act))
		{
			return true;
		}

		return base.EmergencyGCD(nextGCD, out act);
	}

	#endregion

	#region GCD Logic

	private bool TryHealGCD(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine || DeploymentChainActive)
		{
			return false;
		}

		var critical = FindLowest(SingleHealRange, static m => m.GetHealthRatio() < CriticalHp);

		if (HasSeraphism)
		{
			if (critical != null && BeirutaAccretion.TryUseOn(ManifestationPvE, critical, out act))
			{
				return true;
			}

			if (SeraphismAoeHealNeeded && AccessionPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
			{
				return true;
			}

			return TryTopUpGCD(out act);
		}

		var criticalTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < CriticalHp);
		if (criticalTank != null &&
			(!ExcogitationPvE.EnoughLevel || ExcogitationPvE.Cooldown.IsCoolingDown) &&
			!criticalTank.HasStatus(false, StatusID.Galvanize) &&
			BeirutaAccretion.TryUseOn(AdloquiumPvE, criticalTank, out act))
		{
			return true;
		}

		return TryTopUpGCD(out act);
	}

	private bool TryTopUpGCD(out IAction? act)
	{
		act = null;
		return !HasAetherflow &&
			!ProtectRecitationForDeployment &&
			NonTankTopUp is { } topUp &&
			BeirutaAccretion.TryUseOn(HasSeraphism ? ManifestationPvE : AdloquiumPvE, topUp, out act);
	}

	[RotationDesc(ActionID.ConcitationPvE)]
	protected override bool DefenseAreaGCD(out IAction? act)
	{
		UpdateActionTracking();

		if (ShouldDeferToRaise())
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

		if (!(WardMode || BeirutaAccretion.GuardActive(PartyMembers)) ||
			HasEmergencyTactics ||
			ProtectRecitationForDeployment ||
			DeploymentChainActive ||
			CountUnshielded(SuccorRange) < BeirutaWard.UnshieldedPartyCount)
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

		if (ShouldDeferToRaise())
		{
			return base.GeneralGCD(out act);
		}

		if (TryUseDeploymentAdloquium(out act))
		{
			return true;
		}


		if (TryEmergencyTacticsGCD(out act))
		{
			return true;
		}

		if (TryWardModeGCD(out act))
		{
			return true;
		}

		if (TryHealGCD(out act))
		{
			return true;
		}

		if (ShouldUseSummonEos() &&
			SummonEosPvE.CanUse(out act))
		{
			return true;
		}

		var nearbyHostiles = AttackableHostilesInRangeOf(5);
		var expectedHPToLive12Seconds = CalculateExpectedHpToLive12Seconds();

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

		if (!BioIiPvE.EnoughLevel)
		{
			if (BioPvE.CanUse(out act) && BioPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (RuinPvE.CanUse(out act))
			{
				return true;
			}

			if (BioPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}
		else if (!ArtOfWarPvE.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) && BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (RuinPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!BroilMasteryTrait.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) &&
				nearbyHostiles < GetAoWBreakevenTargets() &&
				BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
			{
				return true;
			}

			if (RuinPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!BroilMasteryIiTrait.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) &&
				nearbyHostiles < GetAoWBreakevenTargets() &&
				BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilPvE.CanUse(out act))
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 0 && InCombat)
			{
				return true;
			}
		}
		else if (!BroilIiiPvE.EnoughLevel)
		{
			if (BioIiPvE.CanUse(out act) &&
				nearbyHostiles < GetAoWBreakevenTargets() &&
				BioIiPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIiPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!BroilIvPvE.EnoughLevel)
		{
			if (BiolysisPvE.CanUse(out act) &&
				nearbyHostiles < GetAoWBreakevenTargets() &&
				BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIiiPvE.CanUse(out act))
			{
				return true;
			}
		}
		else
		{
			if (BiolysisPvE.CanUse(out act) &&
				nearbyHostiles < GetAoWBreakevenTargets() &&
				BiolysisPvE.Target.Target.CurrentHp >= expectedHPToLive12Seconds)
			{
				return true;
			}

			if (ArtOfWarPvE.CanUse(out act, skipAoeCheck: true) && nearbyHostiles > 1 && InCombat)
			{
				return true;
			}

			if (BroilIvPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (TryMovementGCD(nearbyHostiles, expectedHPToLive12Seconds, out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Decision Helpers

	#region Deployment Tactics Chain

	private bool DeploymentChainActive =>
		_deploymentChainStartedAtMs != 0 &&
		Environment.TickCount64 - _deploymentChainStartedAtMs < DeploymentChainWindowMs;

	private static bool HasDefenseAreaSignal => MergedStatus.HasFlag(AutoStatus.DefenseArea);

	private bool DeploymentSignalActive =>
		(SignalProtraction && HasProtraction) ||
		(SignalDefenseArea && HasDefenseAreaSignal) ||
		(SignalRecitation && HasRecitation) ||
		(SignalFeyIllumination && StatusHelper.PlayerHasStatus(false, StatusID.FeyIllumination)) ||
		(SignalSurecast && StatusHelper.PlayerHasStatus(true, StatusID.Surecast)) ||
		(SignalSprint && StatusHelper.PlayerHasStatus(true, StatusID.Sprint)) ||
		(SignalSwiftcast && HasSwift && !MergedStatus.HasFlag(AutoStatus.Raise));

	private bool _deploymentChainUsesRecitation;

	private bool DeploymentChainWantsRecitation =>
		OnlyUseDeploymentTacticsOnCriticalShields && RecitationPvE.EnoughLevel;

	private bool RecitationReadyForChain =>
		HasRecitation || RecitationPvE.Cooldown.WillHaveOneChargeGCD(1);

	private bool DeploymentChainNeedsRecitation => _deploymentChainUsesRecitation;

	private bool DeploymentTacticsReadySoon =>
		DeploymentTacticsPvE.EnoughLevel &&
		DeploymentTacticsPvE.Cooldown.WillHaveOneChargeGCD(2);

	private bool CanStartDeploymentChain()
	{
		if (!DeploymentTacticsReadySoon || DeploymentAdloTarget() == null)
		{
			return false;
		}

		if (DeploymentChainWantsRecitation &&
			!RecitationReadyForChain &&
			RecitationUsage == RecitationUsageStrategy.DeploymentOnly)
		{
			return false;
		}

		return true;
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

		if (signal && CanStartDeploymentChain())
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
			m.HasStatus(true, StatusID.Protraction) &&
			!m.HasStatus(false, StatusID.Galvanize));
		if (protraction != null)
		{
			return protraction;
		}

		var injured = FindLowest(SingleHealRange, static m =>
			!IsTankMember(m) &&
			m.GetHealthRatio() < 1f &&
			!m.HasStatus(false, StatusID.Galvanize));
		if (injured != null)
		{
			return injured;
		}

		var player = ECommons.GameHelpers.Player.Object;
		return player != null && !player.HasStatus(false, StatusID.Galvanize) ? player : null;
	}

	private IBattleChara? DeploymentGalvanizeTarget =>
		_deploymentTarget != null &&
		!_deploymentTarget.IsDead &&
		_deploymentTarget.HasStatus(true, StatusID.Galvanize)
			? _deploymentTarget
			: null;

	private bool ShouldUseRecitationForDeploymentTactics() =>
		DeploymentChainActive &&
		DeploymentChainNeedsRecitation &&
		!HasRecitation &&
		DeploymentGalvanizeTarget == null;

	private bool DeploymentAdloPending
	{
		get
		{
			if (ECommons.GameHelpers.Player.Object is { IsCasting: true } player &&
				(player.CastActionId == (uint)ActionID.AdloquiumPvE || player.CastActionId == (uint)ActionID.ManifestationPvE))
			{
				return true;
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
					return (DateTime.Now - rec.UsedTime).TotalSeconds < DeploymentAdloLandSeconds;
				}
			}

			return false;
		}
	}

	private bool ShouldUseDeploymentAdloquium() =>
		DeploymentChainActive &&
		DeploymentGalvanizeTarget == null &&
		!DeploymentAdloPending &&
		(HasRecitation || !DeploymentChainNeedsRecitation);

	private bool TryUseDeploymentAdloquium(out IAction? act)
	{
		act = null;

		if (!ShouldUseDeploymentAdloquium() || DeploymentAdloTarget() is not { } target)
		{
			return false;
		}

		var adlo = HasSeraphism ? ManifestationPvE : AdloquiumPvE;
		if (BeirutaAccretion.TryUseOn(adlo, target, out act))
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

	private bool ShouldDeferToRaise() =>
		(HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
		MergedStatus.HasFlag(AutoStatus.Raise);

	private bool TryPriorityWeaves(out IAction? act)
	{
		act = null;

		if (IsBurst && !CombatElapsedLess(ChainStratagemOpenerSeconds) && ChainStratagemPvE.CanUse(out act))
		{
			StampChainStratagemUse();
			return true;
		}

		if (OpenerWeaveHold)
		{
			return false;
		}

		if (CombatTime > 5 &&
			HasAetherflow &&
			FreshAetherflowSoon &&
			EnergyDrainPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!HasAetherflow)
		{
			if (PrioritizeAetherflowOverDissipation)
			{
				if (AetherflowPvE.CanUse(out act))
				{
					return true;
				}

				if (IsBurst && DissipationPvE.CanUse(out act))
				{
					return true;
				}
			}
			else
			{
				if (IsBurst && DissipationPvE.CanUse(out act))
				{
					return true;
				}

				if (AetherflowPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (ShouldUseBanefulImpaction(NumberOfHostilesInRangeOf(5), out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool ShouldUseBanefulImpaction(int closeTargetCount, out IAction? act)
	{
		act = null;

		if (DelayBanefulImpaction && InFirst5sAfterChainStratagem)
		{
			return false;
		}

		return BanefulImpactionPvE.CanUse(out act) &&
			   (closeTargetCount > 3 ||
				Target.IsBossFromTTK() ||
				Player != null && StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.ImpactImminent));
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
				(member.IsDead || IsHealBlocked(member) || member.GetHealthRatio() >= AetherpactRemoveHpThreshold))
			{
				return true;
			}
		}

		return false;
	}

	private bool HasPartyMemberWithOwnStatus(StatusID statusId)
	{
		foreach (var member in PartyMembers)
		{
			if (member.HasStatus(true, statusId))
			{
				return true;
			}
		}

		return false;
	}


	#endregion

	#region Damage / DoT Helpers

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

	private float CalculateExpectedHpToLive12Seconds()
	{
		if (Player is null || !EnableBallparkTtkEstimator)
		{
			return 1f;
		}

		var partyMemberCount = 0;
		foreach (var _ in PartyMembers)
		{
			partyMemberCount++;
		}

		return BallparkDamagePercent * Player.MaxHp * partyMemberCount * 12;
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

	private bool CanUseCurrentBio(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (BiolysisPvE.EnoughLevel &&
			BiolysisPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!BiolysisPvE.EnoughLevel && BioIiPvE.EnoughLevel &&
			BioIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!BiolysisPvE.EnoughLevel && !BioIiPvE.EnoughLevel && BioPvE.EnoughLevel &&
			BioPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
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
			!BeirutaAccretion.TryUseOn(AdloquiumPvE, tank, out act))
		{
			act = null;
			return false;
		}

		StampAdloquiumUse();
		return true;
	}

	#endregion

}
