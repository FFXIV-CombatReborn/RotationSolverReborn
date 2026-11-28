using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.ComponentModel;

namespace RotationSolver.Rotations.Magical.Pictomancer;

[Rotation("BeirutaPCT", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Automatic Pictomancer, tuned for fights that start with a countdown Rainbow Drip. Recommended GCD is 2.48-2.50 by ping (2.48 fits Rainbow Drip inside Starry Muse more often). AutoBurst off is enough to hold burst. Hover each setting for details.")]
[SourceCode(Path = "main/Rotations/Magical/Pictomancer/BeirutaPCT.cs")]
[ExtraRotation]

public sealed class BeirutaPCT : PictomancerRotation, Updaters.ICdPlanRotation
{
	#region CD Plan

	[RotationConfig(CombatType.PvE, Name = "[CD Plan] Planned cooldowns", Tooltip =
		"Auto: Addle, Tempera Coat and Tempera Grassa respond to RSR's defense signals (raidwide detection, Boss Mod or the commands).\n" +
		"Use built-in CD plan: they follow the plan picked below for the duty you're in.\n" +
		"BMR / VBM / Manual: you or your Boss Mod / BMR CD planner press them.\n" +
		"With the built-in plan or BMR / VBM / Manual, the defense signals do not use them.")]
	public CdPlanSource CdPlanFrom { get; set; } = CdPlanSource.Auto;

	public bool UseTimelinePlan => CdPlanFrom == CdPlanSource.BuiltIn;

	private bool AutoDefense => CdPlanFrom == CdPlanSource.Auto;

	[RotationConfig(CombatType.PvE, Name = "    > Show CD Plan timeline", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"A small window like the action timeline, showing the plan's actions coming up in the next 10s. It shows in a duty that has a CD plan; hover the window for lock / close.")]
	public bool ShowPlanTimeline { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Show timeline messages in chat", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"Syncs and queued / skipped planned actions. Loading messages are always shown.")]
	public bool ShowTimelineMessages { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Addle from", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"Where planned Addle comes from. R2 / Fake melee profile: the duty's shared Addle plan for that slot (Fake melee = the second caster of a double caster comp, using Addle like a melee's Feint); the job plan's own Addle rows are then not used (everything else still comes from the job plan).\n" +
		"Auto: R2, and once an R2 Addle turns out to be another caster's, Fake melee for the rest of the duty.\n" +
		"Job plan: Addle from the picked job plan. Also used when the duty has no profile.")]
	public AddleProfile AddleFrom { get; set; } = AddleProfile.Auto;

	string Updaters.ICdPlanRotation.MitProfileAction => "Addle";

	MitProfileChoice Updaters.ICdPlanRotation.MitProfile => (MitProfileChoice)AddleFrom;

	[RotationConfig(CombatType.PvE, Name = "    > Use planned Tempera Grassa", Parent = nameof(CdPlanFrom), ParentValue = CdPlanNames.BuiltInPlan, Tooltip =
		"On: the plan's Tempera Grassa are used in their window.\nOff: they are skipped.")]
	public bool UsePlannedTemperaGrassa { get; set; } = true;

	IEnumerable<(uint ActionId, string Config)> Updaters.ICdPlanRotation.OptionalPlannedActions =>
	[
		((uint)ActionID.TemperaGrassaPvE, nameof(UsePlannedTemperaGrassa)),
	];

	bool Updaters.ICdPlanRotation.AllowPlannedAction(uint actionId) =>
		UsePlannedTemperaGrassa || actionId != (uint)ActionID.TemperaGrassaPvE;

	// A planned Tempera Grassa needs Tempera Coat up first.
	private bool PlannedGrassaNeedsCoat =>
		UseTimelinePlan && UsePlannedTemperaGrassa && !StatusHelper.PlayerHasStatus(true, StatusID.TemperaCoat) &&
		Updaters.TimelineUpdater.PlannedOpenByName(TemperaGrassaPvE.Name);

	string? Updaters.ICdPlanRotation.PlannedActionWait(uint actionId) => CdPlanHelper.Wait(actionId);

	#endregion

	#region Config Options

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Ignore short countdowns from others", Tooltip =
		"Countdowns of 5s or less started by someone else get no pre-pull actions, so a bait countdown can't pull you early.")]
	public bool IgnoreShortForeignCountdown { get; set; } = true;

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Starry Muse", Tooltip =
		"Balance: about 5s into the pull (2s below level 92).\n" +
		"0s: sent at the end of the countdown; other oGCDs wait for the first GCD.")]
	public OpenWindowMode OpenWindow { get; set; } = OpenWindowMode.Balance;

	public enum OpenWindowMode : byte
	{
		[Description("Balance")]
		Balance,

		[Description("0s (end of countdown)")]
		ZeroGcd,
	}

	#endregion

	#region Burst

	[Range(0, 9, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "[Burst] Delay Madeen / Mog / Living Muses after Starry Muse", Tooltip =
		"Seconds to wait after Starry Muse, to line up with party buffs. Madeen goes before the Living Muses so it isn't overwritten.")]
	public float MuseDelayAfterStarry { get; set; } = 0f;

	[RotationConfig(CombatType.PvE, Name = "[Burst] Use potion")]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > First potion", Parent = nameof(UsePotion))]
	public FirstPotionTiming FirstPotion { get; set; } = FirstPotionTiming.Opener;

	public enum FirstPotionTiming : byte
	{
		[Description("Opener (first Starry Muse)")]
		Opener,

		[Description("Second Starry Muse (2 min)")]
		SecondBurst,
	}

	#endregion

	#region Defence

	[RotationConfig(CombatType.PvE, Name = "[Defence] No defensive abilities in or near Starry Muse", Tooltip =
		"Blocks them during Starry Muse and within 10s of it. Also try not to press defensive abilities yourself in the first 5s of the fight or of burst.")]
	public bool BurstDefense { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Defence] Tempera with healer mitigation (outside burst)", Tooltip =
		"Tempera Coat, then Tempera Grassa, whenever Kerachole or Sacred Soil is on you.")]
	public bool TemperaWithHealerMit { get; set; } = false;

	#endregion

	#region Movement

	[Range(0, 3, ConfigUnitType.Seconds, 0.1f)]
	[RotationConfig(CombatType.PvE, Name = "[Movement] Stand still before hard-casting a motif", Tooltip =
		"Seconds you must stand still first. Swiftcast and motifs you press yourself are exempt.")]
	public float MotifStandStillSeconds { get; set; } = 1f;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Swiftcast a needed motif while moving", Tooltip =
		"Movement order: hammers > Rainbow Drip (Rainbow Bright) > Holy / Comet > Swiftcast motif.\n" +
		"While potted outside burst, Swiftcast goes on the creature motif when a Living Muse is available.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > When to use it", Parent = nameof(UseSwiftcastForMovement))]
	public SwiftcastMovementPriority SwiftcastMovement { get; set; } = SwiftcastMovementPriority.LastResort;

	public enum SwiftcastMovementPriority : byte
	{
		[Description("Last resort (spend hammers / Rainbow Drip / Star Prism / Holy / Comet first)")]
		LastResort = 0,

		[Description("Highest priority (Swiftcast as soon as movement is sustained)")]
		First = 2,
	}

	#endregion

	#region Rainbow Drip

	[RotationConfig(CombatType.PvE, Name = "[Rainbow Drip] Always Swiftcast a Rainbow Drip you press", Tooltip =
		"Never hard-cast in combat and ignores the weave rules. Dropped if Swiftcast is on cooldown.")]
	public bool RainbowDripSwift { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Rainbow Drip] Finish the boss with Swiftcast Rainbow Drip", Tooltip =
		"When the boss's time to kill is under 2s.")]
	public bool RainbowDripFinisher { get; set; } = true;

	#endregion

	#region 112

	[RotationConfig(CombatType.PvE, Name = "[Advanced] Single 112 once per 120s (experimental)", Tooltip =
		"Skips White 1 and 2 for a faster Subtractive Palette.")]
	public bool UseSingle112 { get; set; } = false;

	[Range(0, 3, ConfigUnitType.None, 1)]
	[RotationConfig(CombatType.PvE, Name = "    > Most White Paint (Holy in White) spent as filler", Parent = nameof(UseSingle112))]
	public int Max112WhitePaint { get; set; } = 1;

	#endregion

	#endregion

	#region Constants / Fields

	private const float OpenerSeconds = 5f;
	private const float OpenerBuffCountdownSeconds = 0.35f;
	private const long OpenerWeaveHoldMs = 3_500;
	private const float ShortCountdownSeconds = 5.5f;

	private const float MovementGraceSeconds = 0.3f;
	private const float MovementTimeThreshold = 1f;
	private const float MovementLeadSeconds = 0.5f;

	private const float StandardGCDTotal = 2.5f;
	private const float LongRecastThreshold = 2.6f;
	private const float WeaveLockAllowance = 0.7f;
	private const float StarryLateWeaveRemain = StandardGCDTotal / 2f;
	private const float StarryIdleLimit = 1f;
	private const float StarryPaletteLockSeconds = 1.5f;
	private const float NearBurstDefenseLead = 10f;
	private const float BurstPrepWindow = 30f;
	private const float PaintReserveWindow = 20f;
	private const int PaintReserveAmount = 2;
	private const int SubtractiveInkCount = 3;
	private const int PaletteCost = 50;
	private const int PaletteGaugeHigh = 75;

	private const float HammerGcd = 2.5f;
	private const float HammerTimeDuration = 30f;
	private const int HammerStackCount = 3;

	private const float MotifPaintTime = 4f;
	private const float StrikingOvercapLead = 10f;
	private const float MotifCapLead = StrikingOvercapLead + MotifPaintTime;
	private const float LandscapeFirstLead = MotifPaintTime * 2f;
	private const int CreatureFirstCharges = 2;

	private const float StrikingPreserveLead = 60f;
	private const float LivingPreserveLead = 40f;
	private const long MogHoldAfterFangedMs = 160_000;

	private const float RainbowDripFinisherTtk = 2f;

	private const float Combo112BuffSeconds = 31f;
	private const float Min112StarryDistance = 45f;
	private const float Comet112Seconds = 3.3f;
	private const long Single112IntervalMs = 120_000;

	private enum Phase112 : byte
	{
		None,
		Armed,
		Filler,
		Second,
	}

	private IBaseAction[]? _subtractiveCombo;
	private IBaseAction[]? _whiteCombo;
	private IBaseAction[]? _fillerCombo;
	private IBaseAction[]? _creatureMotifs;

	private IAction? _heldGCD;
	private bool _motifCheckForSwift;

	private long _starryUsedAtMs;
	private long _starryIdleSinceMs;
	private long _fangedUsedInStarryAtMs;
	private long _openerBuffSentMs;
	private int _starryBurstCount;
	private bool _hadStarryMuse;

	private float _countdownStartLength;
	private bool _countdownSeen;
	private bool _isShortCountdownFromOthers;

	private Phase112 _phase112 = Phase112.None;
	private long _last112AtMs;
	private int _112PaintAtStart;
	private bool _112BlackAtStart;

	#endregion

	#region Action Lists

	private IBaseAction[] SubtractiveCombo => _subtractiveCombo ??=
	[
		ThunderIiInMagentaPvE, StoneIiInYellowPvE, BlizzardIiInCyanPvE,
		ThunderInMagentaPvE, StoneInYellowPvE, BlizzardInCyanPvE,
	];

	private IBaseAction[] WhiteCombo => _whiteCombo ??=
	[
		WaterIiInBluePvE, AeroIiInGreenPvE, FireIiInRedPvE,
		WaterInBluePvE, AeroInGreenPvE, FireInRedPvE,
	];

	private IBaseAction[] FillerCombo => _fillerCombo ??=
	[
		ThunderIiInMagentaPvE, StoneIiInYellowPvE, BlizzardIiInCyanPvE,
		WaterIiInBluePvE, AeroIiInGreenPvE, FireIiInRedPvE,
		ThunderInMagentaPvE, StoneInYellowPvE, BlizzardInCyanPvE,
		WaterInBluePvE, AeroInGreenPvE, FireInRedPvE,
	];

	private IBaseAction[] CreatureMotifs => _creatureMotifs ??=
	[
		PomMotifPvE, WingMotifPvE, ClawMotifPvE, MawMotifPvE,
	];

	private IAction[] SubtractiveInks => SubtractiveCombo;

	private static bool TryFirstUsable(IBaseAction[] actions, bool skipCastingCheck, out IAction? act)
	{
		foreach (var action in actions)
		{
			if (action.CanUse(out act, skipCastingCheck: skipCastingCheck))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	#endregion

	#region Simple Status Helpers

	private static bool IsPotted => StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

	private static bool HasInspiration => StatusHelper.PlayerHasStatus(true, StatusID.Inspiration);

	private static bool HasHealerGroundMit =>
		StatusHelper.PlayerHasStatus(false, StatusID.Kerachole) ||
		StatusHelper.PlayerHasStatus(false, StatusID.SacredSoil_299) ||
		StatusHelper.PlayerHasStatus(false, StatusID.SacredSoil_1944);

	private static bool HasAetherhuesI => StatusHelper.PlayerHasStatus(true, StatusID.Aetherhues);

	private static bool HasAetherhuesII => StatusHelper.PlayerHasStatus(true, StatusID.AetherhuesIi);

	private static bool HasComboActive => HasAetherhuesI || HasAetherhuesII;

	private static int SubtractiveStacksHeld =>
		HasSubtractivePalette ? StatusHelper.PlayerStatusStack(true, StatusID.SubtractivePalette) : 0;

	private static bool IsQueuedByUser(IBaseAction action) =>
		DataCenter.CommandNextAction is IBaseAction queued && queued.ID == action.ID;

	private int OpenerAdjustTime =>
		OpenWindow == OpenWindowMode.ZeroGcd ? 0 : (DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5);

	private float StarryIn =>
		HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;

	private bool StarryComing =>
		IsBurst && !HasStarryMuse && StarryMusePvE.EnoughLevel;

	private bool StarryUsedWithin(float seconds) =>
		_starryUsedAtMs != 0 && Environment.TickCount64 - _starryUsedAtMs < seconds * 1000f;

	private bool MuseDelayActive =>
		MuseDelayAfterStarry > 0f && StarryUsedWithin(MuseDelayAfterStarry);

	private bool InOrNearBurst =>
		HasStarryMuse || (IsBurst && StarryMusePvE.EnoughLevel && StarryIn <= NearBurstDefenseLead);

	private bool AutoDefenseBlockedForBurst => BurstDefense && InOrNearBurst;

	private bool HolyCometAllowedByPaintReserve =>
		!(IsBurst && !HasStarryMuse && StarryIn <= PaintReserveWindow && Paint <= PaintReserveAmount);

	#endregion

	#region Action Checks

	private bool RainbowDripCastAllowed =>
		!RainbowDripSwift || !InCombat || HasRainbowBright || HasSwift;

	private bool CometCastAllowed =>
		!HasStarryMuse || HasHyperphantasia || IsQueuedByUser(CometInBlackPvE);

	private bool MotifCastAllowed(IBaseAction motif) =>
		HasSwift ||
		_motifCheckForSwift ||
		StopMovingTime >= MotifStandStillSeconds ||
		IsQueuedByUser(motif);

	protected override bool ShouldDelayCommandAction(IBaseAction action) =>
		(action.ID == RainbowDripPvE.ID && !RainbowDripCastAllowed) ||
		CdPlanHelper.HoldCommand(action, UseTimelinePlan);

	private bool TryCometInBlack(out IAction? act, bool skipCastingCheck = false)
	{
		act = null;
		return CometCastAllowed && CometInBlackPvE.CanUse(out act, skipCastingCheck: skipCastingCheck);
	}

	private bool TryMotif(IBaseAction motif, bool skipCastingCheck, out IAction? act)
	{
		act = null;
		return MotifCastAllowed(motif) && motif.CanUse(out act, skipCastingCheck: skipCastingCheck);
	}

	protected override void UpdateInfo()
	{
		TrackStarryBursts();
		TrackCountdown();

		base.UpdateInfo();
	}

	private void TrackStarryBursts()
	{
		if (!InCombat)
		{
			_starryBurstCount = 0;
			_hadStarryMuse = false;
			return;
		}

		if (HasStarryMuse && !_hadStarryMuse)
		{
			_starryBurstCount++;
		}

		_hadStarryMuse = HasStarryMuse;
	}

	#endregion

	#region Countdown Logic

	private unsafe void TrackCountdown()
	{
		var agent = AgentCountDownSettingDialog.Instance();
		var player = Player;
		if (agent == null || !agent->Active || player == null)
		{
			_countdownSeen = false;
			_isShortCountdownFromOthers = false;
			return;
		}

		if (!_countdownSeen)
		{
			_countdownSeen = true;
			_countdownStartLength = agent->TimeRemaining;
		}

		_isShortCountdownFromOthers =
			_countdownStartLength <= ShortCountdownSeconds && agent->InitiatorId != player.EntityId;
	}

	private bool OpenerWeaveHold =>
		_openerBuffSentMs != 0 &&
		Environment.TickCount64 - _openerBuffSentMs < OpenerWeaveHoldMs &&
		IsLastGCD(false, RainbowDripPvE, FireInRedPvE);

	private bool TryOpenerStarry(float remainTime, out IAction? act)
	{
		act = null;

		if (OpenWindow != OpenWindowMode.ZeroGcd ||
			!IsBurst ||
			remainTime >= OpenerBuffCountdownSeconds ||
			ECommons.GameHelpers.Player.Object is not { IsCasting: false } player ||
			!StarryMusePvE.EnoughLevel ||
			!StarryMusePvE.IsEnabled ||
			!StarryMusePvEReady ||
			StarryMusePvE.Cooldown.IsCoolingDown)
		{
			return false;
		}

		StarryMusePvE.Target = new TargetResult(player, [player], player.Position);
		act = StarryMusePvE;
		_openerBuffSentMs = Environment.TickCount64;
		_starryUsedAtMs = _openerBuffSentMs;
		return true;
	}

	protected override IAction? CountDownAction(float remainTime)
	{
		if (IgnoreShortForeignCountdown && _isShortCountdownFromOthers)
		{
			return null;
		}

		if (remainTime < RainbowDripPvE.Info.CastTime + 0.4f + CountDownAhead && RainbowDripCastAllowed && RainbowDripPvE.CanUse(out var act))
		{
			return act;
		}

		if (remainTime < FireInRedPvE.Info.CastTime + CountDownAhead &&
			DataCenter.PlayerSyncedLevel() < 92 &&
			FireInRedPvE.CanUse(out act))
		{
			return act;
		}

		if (TryOpenerStarry(remainTime, out var starry))
		{
			return starry;
		}

		return base.CountDownAction(remainTime);
	}

	#endregion

	#region Movement

	private bool MovingPastGrace =>
		InCombat && IsMoving && MovingTime > MovementGraceSeconds;

	private bool HasSufficientMovement =>
		InCombat && IsMoving && MovingTime > MovementTimeThreshold;

	private bool IsMovementPreferredNextGCD(IAction nextGCD) =>
		nextGCD != null && nextGCD.IsTheSameTo(false,
			HolyInWhitePvE,
			CometInBlackPvE,
			HammerStampPvE,
			HammerBrushPvE,
			PolishingHammerPvE,
			StarPrismPvE,
			RainbowDripPvE);

	private bool SwiftcastReadyForMovement =>
		UseSwiftcastForMovement &&
		HasSufficientMovement &&
		MovingTime > MovementTimeThreshold + MovementLeadSeconds &&
		!HasSwift &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown;

	private bool HasCheaperMovementOption =>
		(HasHammerTime && !HammersReservedForBurst) ||
		HasRainbowBright ||
		HasStarstruck ||
		(HolyCometAllowedByPaintReserve &&
			(TryCometInBlack(out _) || HolyInWhitePvE.CanUse(out _)));

	private bool ShouldSwiftcastForMovement(IAction nextGCD)
	{
		if (!SwiftcastReadyForMovement ||
			HasStarryMuse ||
			IsLastAction(ActionID.SwiftcastPvE) ||
			IsMovementPreferredNextGCD(nextGCD) ||
			!MotifReadyForSwift(() => TryNeededMotif(skipCastingCheck: true, out _)))
		{
			return false;
		}

		return SwiftcastMovement == SwiftcastMovementPriority.First || !HasCheaperMovementOption;
	}

	private bool PottedCreatureMotifWanted =>
		InCombat &&
		IsPotted &&
		!HasStarryMuse &&
		!HasHyperphantasia &&
		CombatTime >= OpenerSeconds &&
		!CreatureMotifDrawn &&
		LivingMusePvE.Cooldown.WillHaveOneCharge(MotifPaintTime);

	private bool ShouldSwiftcastPottedMotif =>
		PottedCreatureMotifWanted &&
		!HasSwift &&
		!IsLastAction(ActionID.SwiftcastPvE) &&
		MotifReadyForSwift(() => TryCreatureMotif(skipCastingCheck: true, out _));

	private bool MotifReadyForSwift(Func<bool> motifCheck)
	{
		_motifCheckForSwift = true;
		var ready = motifCheck();
		_motifCheckForSwift = false;
		return ready;
	}

	#endregion

	#region Motifs

	private bool WeaponPaintAllowed => !HasHammerTime && !IsPotted;

	private bool WeaponMotifWanted =>
		SteelMusePvE.Cooldown.HasOneCharge ||
		SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= WeaponMotifPvE.Info.CastTime ||
		ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= BurstPrepWindow ||
		(IsBurst && StarryIn <= BurstPrepWindow);

	private bool CreatureMotifWanted =>
		LivingMusePvE.Cooldown.HasOneCharge ||
		LivingMusePvE.Cooldown.RecastTimeRemainOneCharge <= CreatureMotifPvE.Info.CastTime * 1.7f;

	private bool LandscapeNeededFirst =>
		IsBurst &&
		!LandscapeMotifDrawn &&
		StarryMusePvE.EnoughLevel &&
		StarryIn <= LandscapeFirstLead;

	private int MotifsToPaintBeforeStarry =>
		(LandscapeMotifDrawn ? 0 : 1) +
		(WeaponMotifDrawn || HasHammerTime ? 0 : 1) +
		(CreatureMotifDrawn || !LivingMusePvE.Cooldown.WillHaveOneCharge(StarryIn) ? 0 : 1);

	private static float TimeToChargeCap(IBaseAction muse, int maxCharges)
	{
		var cooldown = muse.Cooldown;
		var missing = maxCharges - cooldown.CurrentCharges;
		return missing <= 0
			? 0f
			: ((missing - 1) * cooldown.RecastTimeOneChargeRaw) + MathF.Max(0f, cooldown.RecastTimeRemainOneCharge);
	}

	private float StrikingCapIn => TimeToChargeCap(SteelMusePvE, MaxStrikingMuse);

	private float LivingCapIn => TimeToChargeCap(LivingMusePvE, LivingMusePvE.Cooldown.MaxCharges);

	private bool TryCreatureMotif(bool skipCastingCheck, out IAction? act)
	{
		foreach (var motif in CreatureMotifs)
		{
			if (TryMotif(motif, skipCastingCheck, out act))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	private bool TryOrderedMotif(bool allowWeapon, bool allowCreature, bool skipCastingCheck, out IAction? act)
	{
		var creatureFirst = LivingMusePvE.Cooldown.CurrentCharges >= CreatureFirstCharges;

		if (creatureFirst && allowCreature && TryCreatureMotif(skipCastingCheck, out act))
		{
			return true;
		}

		if (allowWeapon && TryMotif(HammerMotifPvE, skipCastingCheck, out act))
		{
			return true;
		}

		if (!creatureFirst && allowCreature && TryCreatureMotif(skipCastingCheck, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryCappingMotif(bool skipCastingCheck, out IAction? act) =>
		TryOrderedMotif(
			!WeaponMotifDrawn && !HasHammerTime && StrikingCapIn <= MotifCapLead,
			!CreatureMotifDrawn && LivingCapIn <= MotifCapLead,
			skipCastingCheck,
			out act);

	private bool TryNeededMotif(bool skipCastingCheck, out IAction? act)
	{
		if (TryCappingMotif(skipCastingCheck, out act) ||
			TryOrderedMotif(WeaponPaintAllowed, true, skipCastingCheck, out act))
		{
			return true;
		}

		if (!HasStarryMuse && !HasHyperphantasia && TryMotif(StarrySkyMotifPvE, skipCastingCheck, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Hammer Management

	private float HammerTimeRemain =>
		HasHammerTime ? StatusHelper.PlayerStatusTime(true, StatusID.HammerTime) : 0f;

	private float HammerSpendTime => HammerStacks * HammerGcd;

	private bool HammersReachBurst =>
		IsBurst && StarryIn + HammerSpendTime + 1f <= HammerTimeRemain;

	private bool HammersReservedForBurst =>
		HasHammerTime && StarryComing && HammersReachBurst;

	private bool StrikingHammersReachBurst =>
		StarryComing && StarryIn + (HammerStackCount * HammerGcd) + 1f <= HammerTimeDuration;

	private float BurstPrepLeadTime
	{
		get
		{
			var paintings = 1 + (LandscapeMotifDrawn ? 0 : 1) + (CreatureMotifDrawn ? 0 : 1);
			return HammerSpendTime + (paintings * MotifPaintTime) + (HammerGcd * 2f);
		}
	}

	private bool ShouldDumpHammers =>
		HasHammerTime &&
		!HasStarryMuse &&
		((IsPotted && !HammersReservedForBurst) ||
			HammerTimeRemain <= HammerSpendTime + HammerGcd ||
			StrikingCapIn <= HammerSpendTime + MotifPaintTime + HammerGcd ||
			(IsBurst && StarryIn <= BurstPrepWindow && !HammersReachBurst && StarryIn <= BurstPrepLeadTime));

	private bool TryHammerChain(out IAction? act)
	{
		if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true) ||
			HammerBrushPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		return HammerStampPvE.CanUse(out act, skipComboCheck: true);
	}

	#endregion

	#region Starry Muse Entry

	private bool HoldPaletteForStarry =>
		StarryComing &&
		StarryIn <= (SubtractiveInkCount * BlizzardInCyanPvE.Cooldown.RecastTimeOneChargeRaw) +
			(MotifsToPaintBeforeStarry * MotifPaintTime) +
			StarryLateWeaveRemain;

	private bool StarryEntryPending =>
		InCombat &&
		StarryComing &&
		LandscapeMotifDrawn &&
		CombatTime > OpenerAdjustTime;

	private bool StarryWaitingToWeave =>
		StarryEntryPending && !StarryMusePvE.Cooldown.IsCoolingDown;

	private bool StarryIdleWeaveOpen =>
		StarryEntryPending && DataCenter.DefaultGCDRemain <= 0f;

	private float StarryReadyAfterNextGcd =>
		StarryMusePvE.Cooldown.IsCoolingDown
			? MathF.Max(0f, StarryMusePvE.Cooldown.RecastTimeRemainOneCharge)
			: 0f;

	private bool ShouldHoldGcdForStarry()
	{
		var remain = DataCenter.DefaultGCDRemain;
		if (remain > 0f)
		{
			_starryIdleSinceMs = 0;
		}

		if (!StarryEntryPending ||
			StarryIn > StarryIdleLimit ||
			remain > DataCenter.CalculatedActionAhead ||
			(!StarryMusePvE.Cooldown.IsCoolingDown && !StarryMusePvE.CanUse(out _, skipCastingCheck: true)))
		{
			return false;
		}

		if (remain > 0f)
		{
			return true;
		}

		var now = Environment.TickCount64;
		if (_starryIdleSinceMs == 0)
		{
			_starryIdleSinceMs = now;
		}

		return now - _starryIdleSinceMs < StarryIdleLimit * 1000f;
	}

	private static bool LeavesWeaveSlotFor(IBaseAction gcd, float starryReady)
	{
		var latestWeave = gcd.Cooldown.RecastTimeOneChargeRaw - WeaveLockAllowance;
		var cast = HasSwift ? 0f : gcd.Info.CastTime;
		return MathF.Max(cast, starryReady) <= latestWeave;
	}

	private IAction AdjustForStarryEntry(IAction act)
	{
		if (!StarryEntryPending || act is not IBaseAction current)
		{
			return act;
		}

		var starryReady = StarryReadyAfterNextGcd;
		var recast = current.Cooldown.RecastTimeOneChargeRaw;

		if (current.IsTheSameTo(false, SubtractiveInks))
		{
			return starryReady <= recast + StarryLateWeaveRemain && HolyInWhitePvE.CanUse(out var inkHoly)
				? inkHoly
				: act;
		}

		if (starryReady > recast - WeaveLockAllowance || LeavesWeaveSlotFor(current, starryReady))
		{
			return act;
		}

		foreach (var white in WhiteCombo)
		{
			if (white.CanUse(out var whiteAct) && LeavesWeaveSlotFor(white, starryReady))
			{
				return whiteAct;
			}
		}

		return HolyInWhitePvE.CanUse(out var holy) ? holy : act;
	}

	#endregion

	#region Rainbow Drip

	private bool RainbowDripFinisherDue
	{
		get
		{
			if (!RainbowDripFinisher || !InCombat || !RainbowDripPvE.EnoughLevel)
			{
				return false;
			}

			var target = HostileTarget;
			if (target == null || !(target.IsBossFromTTK() || target.IsBossFromIcon()))
			{
				return false;
			}

			var ttk = target.GetTTK();
			return !float.IsNaN(ttk) && ttk < RainbowDripFinisherTtk;
		}
	}

	private bool RainbowDripSwiftPending =>
		InCombat &&
		!HasRainbowBright &&
		!HasSwift &&
		!IsLastAction(ActionID.SwiftcastPvE) &&
		((RainbowDripSwift && IsQueuedByUser(RainbowDripPvE)) || RainbowDripFinisherDue);

	private bool TryRainbowBrightDrip(out IAction? act)
	{
		act = null;
		return HasRainbowBright && RainbowDripPvE.CanUse(out act);
	}

	#endregion

	#region oGCD Logic

	private bool WeaveBlocked =>
		OpenerWeaveHold ||
		IsLastGCD(false, SubtractiveInks) ||
		(HasInspiration && IActionHelper.IsLastActionAbility() && IsLastGCD(false, StarPrismPvE));

	private bool PreserveStrikingForStarry =>
		IsBurst &&
		!HasStarryMuse &&
		StarryIn <= StrikingPreserveLead &&
		StrikingMusePvE.Cooldown.CurrentCharges == 1 &&
		StrikingMusePvE.Cooldown.RecastTimeRemainOneCharge > MathF.Max(0f, StarryIn - OpenerSeconds);

	private bool PreserveLivingForBurst =>
		CombatTime > OpenerSeconds &&
		IsBurst &&
		!HasStarryMuse &&
		StarryIn <= LivingPreserveLead &&
		LivingMusePvE.Cooldown.CurrentCharges <= 1;

	private bool MogHeldAfterFanged =>
		!HasStarryMuse &&
		_fangedUsedInStarryAtMs != 0 &&
		Environment.TickCount64 - _fangedUsedInStarryAtMs < MogHoldAfterFangedMs;

	private bool NeedsStrikingMovementRescue(IAction nextGCD) =>
		InCombat &&
		IsBurst &&
		!nextGCD.IsTheSameTo(false, HolyInWhitePvE, CometInBlackPvE) &&
		!HasSwift &&
		!HasHammerTime &&
		!StrikingHammersReachBurst &&
		!PreserveStrikingForStarry &&
		StrikingMusePvE.Cooldown.CurrentCharges > 0 &&
		HasSufficientMovement;

	private bool ShouldUseStrikingOutsideBurst(IAction nextGCD) =>
		(!HasStarryMuse && !HasHammerTime && StrikingCapIn <= StrikingOvercapLead && CombatTime > OpenerAdjustTime) ||
		(_phase112 == Phase112.Filler && !HasHammerTime && !PreserveStrikingForStarry) ||
		NeedsStrikingMovementRescue(nextGCD);

	private bool TryLivingMuse(out IAction? act)
	{
		act = null;

		if (!IsBurst || PreserveLivingForBurst || MuseDelayActive || StarryWaitingToWeave)
		{
			return false;
		}

		if (!RetributionOfTheMadeenPvE.CanUse(out _) &&
			!(InCombat && CombatTime < 2f && !HasHammerTime) &&
			PomMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (WingedMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!MogOfTheAgesPvE.CanUse(out _) && ClawedMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (FangedMusePvE.CanUse(out act, usedUp: true))
		{
			if (HasStarryMuse)
			{
				_fangedUsedInStarryAtMs = Environment.TickCount64;
			}

			return true;
		}

		return false;
	}

	private bool PotionAllowedFor(int burstNumber) =>
		UsePotion && burstNumber >= (FirstPotion == FirstPotionTiming.SecondBurst ? 2 : 1);

	private bool TryPotion(out IAction? act)
	{
		act = null;

		if (!InCombat)
		{
			return false;
		}

		var openerPotion = CombatTime <= OpenerSeconds && HasHammerTime && PotionAllowedFor(1);
		var prePotion = !IsPotted && IsBurst && !HasStarryMuse && StarryIn <= OpenerSeconds && PotionAllowedFor(_starryBurstCount + 1);
		var burstPotion = HasStarryMuse && PotionAllowedFor(_starryBurstCount);

		return (openerPotion || prePotion || burstPotion) && UseBurstMedicine(out act);
	}

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		nextGCD = _heldGCD ?? nextGCD;
		act = null;

		if (PlannedGrassaNeedsCoat && TemperaCoatPvE.CanUse(out act))
		{
			return true;
		}

		if (CdPlanHelper.TryPlanned(out act))
		{
			return true;
		}

		if (RainbowDripSwiftPending && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (WeaveBlocked && !StarryIdleWeaveOpen)
		{
			return false;
		}

		if (CombatTime < OpenerAdjustTime && StrikingMusePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (IsBurst &&
			CombatTime > OpenerAdjustTime &&
			DataCenter.DefaultGCDRemain <= StarryLateWeaveRemain &&
			StarryMusePvE.CanUse(out act, skipCastingCheck: true))
		{
			_starryUsedAtMs = Environment.TickCount64;
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.SmudgePvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (WeaveBlocked)
		{
			act = null;
			return false;
		}

		return SmudgePvE.CanUse(out act) || base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TemperaCoatPvE, ActionID.TemperaGrassaPvE, ActionID.AddlePvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (WeaveBlocked || AutoDefenseBlockedForBurst || !AutoDefense)
		{
			act = null;
			return false;
		}

		return TemperaCoatPvE.CanUse(out act) ||
			TemperaGrassaPvE.CanUse(out act) ||
			AddlePvE.CanUse(out act) ||
			base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TemperaCoatPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (WeaveBlocked || AutoDefenseBlockedForBurst || !AutoDefense)
		{
			act = null;
			return false;
		}

		return TemperaCoatPvE.CanUse(out act) || base.DefenseSingleAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		nextGCD = _heldGCD ?? nextGCD;

		if (WeaveBlocked)
		{
			act = null;
			return false;
		}

		if (HasStarryMuse &&
			!HasHammerTime &&
			InCombat &&
			StrikingMusePvE.Cooldown.CurrentCharges > 0 &&
			StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (!StarryUsedWithin(StarryPaletteLockSeconds) &&
			!HoldPaletteForStarry &&
			SubtractivePalettePvE.CanUse(out act))
		{
			return true;
		}

		if (ShouldUseStrikingOutsideBurst(nextGCD) && StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (HasStarryMuse && !MuseDelayActive && IsBurst && RetributionOfTheMadeenPvE.CanUse(out act))
		{
			return true;
		}

		if (!MuseDelayActive &&
			!StarryWaitingToWeave &&
			!MogHeldAfterFanged &&
			IsBurst &&
			!HasHyperphantasia &&
			MogOfTheAgesPvE.CanUse(out act))
		{
			return true;
		}

		return TryLivingMuse(out act) || base.AttackAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		nextGCD = _heldGCD ?? nextGCD;

		if (WeaveBlocked)
		{
			act = null;
			return false;
		}

		if ((ShouldSwiftcastPottedMotif || ShouldSwiftcastForMovement(nextGCD)) && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		if (AutoDefense && TemperaWithHealerMit && InCombat && !InOrNearBurst && HasHealerGroundMit)
		{
			if (TemperaCoatPvE.CanUse(out act))
			{
				return true;
			}

			if (StatusHelper.PlayerHasStatus(true, StatusID.TemperaCoat) && TemperaGrassaPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!AutoDefenseBlockedForBurst &&
			((AutoDefense && MergedStatus.HasFlag(AutoStatus.DefenseArea)) ||
				StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.TemperaCoat)) &&
			TemperaGrassaPvE.CanUse(out act))
		{
			return true;
		}

		return TryPotion(out act) || base.GeneralAbility(nextGCD, out act);
	}

	#endregion

	#region GCD Logic

	private static bool InLongRecastWeaveWindow
	{
		get
		{
			var total = DataCenter.DefaultGCDTotal;
			if (!InCombat || total <= LongRecastThreshold)
			{
				return false;
			}

			var standardAhead = MathF.Min(total, StandardGCDTotal) * Service.Config.Action6Head;
			return DataCenter.DefaultGCDRemain > standardAhead;
		}
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		_heldGCD = null;

		if (RainbowDripSwiftPending && SwiftcastPvE.CanUse(out _))
		{
			_heldGCD = RainbowDripPvE;
			act = null;
			return false;
		}

		if (RainbowDripFinisherDue && (HasSwift || HasRainbowBright) && RainbowDripPvE.CanUse(out act))
		{
			return true;
		}

		if (!ChooseGeneralGCD(out act))
		{
			return false;
		}

		if (act == null)
		{
			return true;
		}

		act = AdjustForStarryEntry(act);

		if (ShouldHoldGcdForStarry() || InLongRecastWeaveWindow)
		{
			_heldGCD = act;
			act = null;
			return false;
		}

		return true;
	}

	private bool TryStarPrism(out IAction? act) =>
		StarPrismPvE.CanUse(out act) && HasStarstruck;

	private bool TryPlannedMotif(out IAction? act)
	{
		act = null;

		if (HasStarryMuse || HasHyperphantasia)
		{
			return false;
		}

		if (InCombat && !LandscapeNeededFirst && TryCappingMotif(skipCastingCheck: false, out act))
		{
			return true;
		}

		if (ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= BurstPrepWindow && TryMotif(StarrySkyMotifPvE, false, out act))
		{
			return true;
		}

		return TryOrderedMotif(WeaponPaintAllowed && WeaponMotifWanted, CreatureMotifWanted, skipCastingCheck: false, out act);
	}

	private bool TryPrePullPaint(out IAction? act)
	{
		if (TryOrderedMotif(WeaponPaintAllowed, true, skipCastingCheck: false, out act))
		{
			return true;
		}

		if (!HasHyperphantasia && !IsPotted && TryMotif(StarrySkyMotifPvE, false, out act))
		{
			return true;
		}

		return TryRainbowBrightDrip(out act);
	}

	private bool ShouldSpendBlackForPalette =>
		!HasStarryMuse &&
		HasMonochromeTones &&
		!HasSubtractivePalette &&
		PaletteGauge >= PaletteCost &&
		!HoldPaletteForStarry &&
		(!(IsBurst && StarryIn <= BurstPrepWindow) || PaletteGauge > PaletteGaugeHigh);

	private bool ChooseGeneralGCD(out IAction? act)
	{
		if (Try112GCD(out act))
		{
			return true;
		}

		if (HasSwift && PottedCreatureMotifWanted && TryCreatureMotif(skipCastingCheck: false, out act))
		{
			return true;
		}

		if (HasSwift && !HasStarryMuse && TryNeededMotif(skipCastingCheck: false, out act))
		{
			return true;
		}

		if (CombatTime < OpenerSeconds && !HasStarryMuse &&
			(HolyInWhitePvE.CanUse(out act) || TryCreatureMotif(skipCastingCheck: false, out act)))
		{
			return true;
		}

		var reserveHyperForPrism = HasStarstruck && StatusHelper.PlayerStatusStack(true, StatusID.Hyperphantasia) == 1;

		if (HasStarryMuse && InCombat && TryBurstGCD(reserveHyperForPrism, out act))
		{
			return true;
		}

		if (TryStarPrism(out act))
		{
			return true;
		}

		var movingCommit = MovingPastGrace && NextAbilityToNextGCD < 0.6f && !HasSwift;

		if (!HasStarryMuse && InCombat && !HasSwift &&
			((movingCommit && !HammersReservedForBurst) || ShouldDumpHammers) &&
			TryHammerChain(out act))
		{
			return true;
		}

		if (TryRainbowBrightDrip(out act))
		{
			return true;
		}

		if (!InCombat && TryPrePullPaint(out act))
		{
			return true;
		}

		if (TryPlannedMotif(out act))
		{
			return true;
		}

		if (movingCommit &&
			(!HasHammerTime || HammersReservedForBurst) &&
			HolyCometAllowedByPaintReserve &&
			(TryCometInBlack(out act) || HolyInWhitePvE.CanUse(out act)))
		{
			return true;
		}

		if (ShouldSpendBlackForPalette && TryCometInBlack(out act))
		{
			return true;
		}

		if (TryFirstUsable(FillerCombo, skipCastingCheck: false, out act))
		{
			return true;
		}

		if (TryOrderedMotif(WeaponPaintAllowed, true, skipCastingCheck: false, out act) ||
			TryMotif(StarrySkyMotifPvE, false, out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Burst GCD

	private bool TryBurstGCD(bool reserveHyperForPrism, out IAction? act)
	{
		var inspirationCasts = HasInspiration && !reserveHyperForPrism;
		var palettePending = !HasSubtractivePalette && (HasSubtractiveSpectrum || PaletteGauge >= PaletteCost);
		var needStrikingSlot = !HasHammerTime && StrikingMusePvE.Cooldown.CurrentCharges > 0;

		if (inspirationCasts && palettePending && HasMonochromeTones &&
			TryCometInBlack(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (needStrikingSlot || palettePending || MovingPastGrace)
		{
			if (inspirationCasts && TryCometInBlack(out act, skipCastingCheck: true))
			{
				return true;
			}

			if (TryStarPrism(out act) || TryHammerChain(out act))
			{
				return true;
			}

			if (MovingPastGrace && TryRainbowBrightDrip(out act))
			{
				return true;
			}
		}

		if (inspirationCasts &&
			(TryFirstUsable(SubtractiveCombo, skipCastingCheck: false, out act) ||
				TryCometInBlack(out act, skipCastingCheck: true) ||
				TryFirstUsable(WhiteCombo, skipCastingCheck: false, out act)))
		{
			return true;
		}

		return TryStarPrism(out act) || TryHammerChain(out act) || TryRainbowBrightDrip(out act);
	}

	#endregion

	#region 112

	private int WhitePaintUsedIn112 =>
		Math.Max(0, _112PaintAtStart - Paint - (_112BlackAtStart && !HasMonochromeTones ? 1 : 0));

	private float Estimate112FillerSeconds()
	{
		var striking = StrikingMusePvE.Cooldown.CurrentCharges;
		var seconds = striking * HammerStackCount * HammerGcd;

		if (HasHammerTime)
		{
			seconds += HammerSpendTime;
		}

		var living = LivingMusePvE.Cooldown.CurrentCharges +
			(LivingMusePvE.Cooldown.RecastTimeRemainOneCharge <= 24f ? 1 : 0);
		var paintings = (CreatureMotifDrawn ? 0 : 1) + living + (WeaponMotifDrawn ? 0 : 1) + striking;
		seconds += paintings * MotifPaintTime;

		var paint = (int)Paint;
		if (paint >= 1)
		{
			seconds += Comet112Seconds;
			paint--;
		}

		return seconds + (Math.Min(Max112WhitePaint, paint) * HammerGcd);
	}

	private bool Can112Start =>
		UseSingle112 &&
		InCombat &&
		IsBurst &&
		!HasStarryMuse &&
		SubtractiveStacksHeld == SubtractiveInkCount &&
		!HasComboActive &&
		BlizzardInCyanPvE.CanUse(out _) &&
		!BlizzardIiInCyanPvE.CanUse(out _) &&
		StarryIn > Min112StarryDistance &&
		(_last112AtMs == 0 || Environment.TickCount64 - _last112AtMs >= Single112IntervalMs) &&
		!(HostileTarget?.IsDying() ?? false) &&
		Estimate112FillerSeconds() >= Combo112BuffSeconds;

	private bool Try112FillerGCD(out IAction? act)
	{
		if (TryHammerChain(out act) ||
			TryOrderedMotif(!HasHammerTime, true, skipCastingCheck: false, out act) ||
			TryCometInBlack(out act))
		{
			return true;
		}

		if (WhitePaintUsedIn112 < Max112WhitePaint && HolyInWhitePvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool Try112GCD(out IAction? act)
	{
		act = null;

		if (!UseSingle112 || !InCombat || HasStarryMuse)
		{
			_phase112 = Phase112.None;
			return false;
		}

		switch (_phase112)
		{
			case Phase112.None:
				if (!Can112Start)
				{
					return false;
				}

				_phase112 = Phase112.Armed;
				goto case Phase112.Armed;

			case Phase112.Armed:
				if (SubtractiveStacksHeld == 2 && HasAetherhuesI)
				{
					_phase112 = Phase112.Filler;
					_112PaintAtStart = Paint;
					_112BlackAtStart = HasMonochromeTones;
					goto case Phase112.Filler;
				}

				if (SubtractiveStacksHeld != SubtractiveInkCount || HasComboActive)
				{
					_phase112 = Phase112.None;
					return false;
				}

				return BlizzardInCyanPvE.CanUse(out act);

			case Phase112.Filler:
				if (SubtractiveStacksHeld != 2)
				{
					_phase112 = Phase112.None;
					return false;
				}

				if (!HasComboActive)
				{
					_phase112 = Phase112.Second;
					goto case Phase112.Second;
				}

				if (Try112FillerGCD(out act))
				{
					return true;
				}

				if (StatusHelper.PlayerStatusTime(true, StatusID.Aetherhues) <= 0.5f)
				{
					act = null;
					return true;
				}

				_phase112 = Phase112.None;
				return false;

			case Phase112.Second:
				if (SubtractiveStacksHeld <= 1 || HasComboActive)
				{
					_phase112 = Phase112.None;
					_last112AtMs = Environment.TickCount64;
					return false;
				}

				return BlizzardInCyanPvE.CanUse(out act);
		}

		return false;
	}

	#endregion
}
