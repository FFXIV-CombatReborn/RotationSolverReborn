using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin BRD", CombatType.PvE, GameVersion = "7.5",
	Description = "I sing the body electric. I gasp the body organic. I miss the body remembered.")]
[SourceCode(Path = "main/ExtraRotations/Ranged/ChurinBRD.cs")]
[ExtraRotation]

public sealed class ChurinBRD : BardRotation
{
	#region Properties

	#region Enums

	private enum SongTiming
	{
		[Description("Standard 3-3-12 Cycle")] Standard,

		[Description("Adjusted Standard Cycle - 2.48 GCD ideal")]
		AdjustedStandard,

		[Description("3-6-9 Cycle - 2.49 or 2.5 GCD ideal")]
		Cycle369,
		[Description("Custom")] Custom
	}

	private enum WandererWeave
	{
		[Description("Early")] Early,
		[Description("Late")] Late
	}

	#endregion

	#region Constants
	private const float SongMaxDuration             = 45f;
	private const float DoTEndBuffer                = 0.5f;
	private const float ArmyHeartbreakHoldThreshold = 30f;
	private const float SidewinderBuffLookahead     = 10f;
	#endregion

	#region Song Timings

	private static bool Is369 => SongTimings == SongTiming.Cycle369;
	private static bool IsCustom => SongTimings == SongTiming.Custom;
	private static bool IsStandardTiming =>
		SongTimings is SongTiming.Standard
			or SongTiming.AdjustedStandard;
	private static float GetSongUptime(float standard, float cycle369, float custom) =>
		SongTimings switch
		{
			SongTiming.Standard or SongTiming.AdjustedStandard => standard,
			SongTiming.Cycle369                                 => cycle369,
			SongTiming.Custom                                   => custom,
			_                                                   => 0f
		};
	private float WandTime => GetSongUptime(42f, 42f, CustomWandTime);
	private float MageTime => GetSongUptime(42f, 39f, CustomMageTime);
	private float ArmyTime => GetSongUptime(33f, 36f, CustomArmyTime);
	private float WandRemainTime => SongMaxDuration - WandTime;
	private float MageRemainTime => SongMaxDuration - MageTime;
	private float ArmyRemainTime => SongMaxDuration - ArmyTime;

	#endregion

	#region Player Status

	private StatusID[] BurstStatus
	{
		get
		{
			if (field != null) return field;
			if (BurstActions.Length == 0) return field = [];
			var statuses = new List<StatusID>();
			foreach (var action in BurstActions)
			{
				if (action == RagingStrikesPvE) statuses.Add(StatusID.RagingStrikes);
				if (action == BattleVoicePvE) statuses.Add(StatusID.BattleVoice);
				if (action == RadiantFinalePvE) statuses.Add(StatusID.RadiantFinale);
			}
			return field = [.. statuses];
		}
	}

	private IBaseAction Stormbite => field ??= StormbitePvE.EnoughLevel ? StormbitePvE : WindbitePvE;

	private IBaseAction CausticBite => field ??= CausticBitePvE.EnoughLevel ? CausticBitePvE : VenomousBitePvE;
	private IBaseAction[] DoTActions => field ??= [Stormbite, CausticBite];

	private IBaseAction[] BurstActions
	{
		get
		{
			if (field != null) return field;
			var actions = new List<IBaseAction>();
			if (RagingStrikesPvE.EnoughLevel) actions.Add(RagingStrikesPvE);
			if (BattleVoicePvE.EnoughLevel) actions.Add(BattleVoicePvE);
			if (RadiantFinalePvE.EnoughLevel) actions.Add(RadiantFinalePvE);
			return field = [.. actions];
		}
	}

	private IBaseAction[] SongList
	{
		get
		{
			if (field != null) return field;
			var songs = new List<IBaseAction>();
			if (TheWanderersMinuetPvE.EnoughLevel) songs.Add(TheWanderersMinuetPvE);
			if (MagesBalladPvE.EnoughLevel) songs.Add(MagesBalladPvE);
			if (ArmysPaeonPvE.EnoughLevel) songs.Add(ArmysPaeonPvE);
			return field = [.. songs];
		}
	}

	private bool BurstEndGCD(uint gcdCount) => StatusHelper.PlayerHasStatus(true, BurstStatus)
	                                           && StatusHelper.PlayerWillStatusEndGCD(gcdCount, DataCenter.CalculatedActionAhead, true, BurstStatus);
	private static bool CanUseEnhancedFiller => HasBarrage || HasHawksEye;
	private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) &&
	                                   !StatusHelper.PlayerWillStatusEnd(0f, true, StatusID.Medicated);
	private static bool InOddMinuteWindow => InMages && SongTime > 15f;
	private static float WeaponAhead => WeaponRemain + DataCenter.CalculatedActionAhead;

	private bool InBurst
	{
		get
		{
			if (BurstStatus.Length == 0) return false;
			foreach (var status in BurstStatus)
			{
				if (!StatusHelper.PlayerHasStatus(true, status)) return false;
			}
			return !StatusHelper.PlayerWillStatusEnd(0f, true, BurstStatus);
		}
	}

	private bool CanBurst
	{
		get
		{
			if (!MergedStatus.HasFlag(AutoStatus.Burst)) return false;

			if (BurstActions.Length == 0) return false;
			foreach (var burstAction in BurstActions)
			{
				if (!burstAction.IsEnabled) return false;
			}
			return true;
		}
	}

	public bool CanBurstChanged
	{
		get
		{
			var currentCanBurst = CanBurst;
			if (currentCanBurst == field) return false;
			field = currentCanBurst;
			return true;
		}
	}

	private static bool IsFirstCycle { get; set; }

	#endregion

	#region Target Status

	private static bool TargetHasDoT(IBaseAction action)
	{
		return CurrentTarget != null
		       && action.Setting.TargetStatusProvide != null
		       && CurrentTarget.HasStatus(true, action.Setting.TargetStatusProvide);
	}

	private static bool TargetIsBoss
	{
		get
		{
			if (CurrentTarget == null) return false;


			return CurrentTarget.IsBossFromIcon()
			       || CurrentTarget.IsBossFromTTK();
		}
	}


	#endregion

	#endregion

	#region Config Options

	[RotationConfig(CombatType.PvE, Name = "Only use DOTs on targets with Boss Icon")]
	private bool DoTsBoss { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Choose Bard Song Timing Preset")]
	private static SongTiming SongTimings { get; set; }

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Minuet Uptime", Parent = nameof(SongTimings),
		ParentValue = SongTiming.Custom)]
	private float CustomWandTime { get; set; } = 45f;

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Mage's Ballad Uptime", Parent = nameof(SongTimings),
		ParentValue = SongTiming.Custom)]
	private float CustomMageTime { get; set; } = 45f;

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Army's Paeon Uptime", Parent = nameof(SongTimings),
		ParentValue = SongTiming.Custom)]
	private float CustomArmyTime { get; set; } = 45f;

	[RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Weave Slot Timing", Parent = nameof(SongTimings),
		ParentValue = SongTiming.Custom)]
	private WandererWeave WanderersWeave { get; set; } = WandererWeave.Early;

	[RotationConfig(CombatType.PvE, Name = "Enable PrepullHeartbreak Shot? - Use with BMR Auto Attack Manager")]
	private bool EnablePrepullHeartbreakShot { get; set; } = true;

	private static readonly ChurinBRDPotions ChurinPotions = new();

	[RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
	private static bool PotionUsageEnabled
	{
		get => ChurinPotions.Enabled;
		set => ChurinPotions.Enabled = value;
	}

	[RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
	private static PotionStrategy PotionUsagePresets
	{
		get => ChurinPotions.Strategy;
		set => ChurinPotions.Strategy = value;
	}

	[Range(0, 20, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE,
		Name = "Use Opener Potion at minus time in seconds - only use if potting early in the opener",
		Parent = nameof(PotionUsageEnabled))]
	private static float OpenerPotionTime
	{
		get => ChurinPotions.OpenerPotionTime;
		set => ChurinPotions.OpenerPotionTime = value;
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 1st Potion at (value in seconds - leave at 0 if using in opener)",
		Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
	private float FirstPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets),
		ParentValue = "Use custom potion timings")]
	private float SecondPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 3rd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets),
		ParentValue = "Use custom potion timings")]
	private float ThirdPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	private void UpdateCustomTimings()
	{
		ChurinPotions.CustomTimings = new Potions.CustomTimingsData
		{
			Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
		};
	}

	[RotationConfig(CombatType.PvE, Name = "Enable Sandbag Mode?")]
	private static bool EnableSandbagMode { get; set; } = false;

	#endregion

	#region Main Combat Logic

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		IsFirstCycle = true;
		if (ChurinPotions.ShouldUsePotion(this, out var potionAct)) return potionAct;

		IAction? act;
		if (SongTimings == SongTiming.AdjustedStandard
		    && remainTime <= 0f)
		{
			 if (HeartbreakShotPvE.CanUse(out act)) return act;
		}

		if (Is369 && EnablePrepullHeartbreakShot && remainTime < 1.65f && HeartbreakShotPvE.CanUse(out act))
		{
			return act;
		}

		return  remainTime <= 0.1f && TryUseDoTs(out act) ? act : base.CountDownAction(remainTime);
	}

	#endregion

	#region oGCD Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		if (ChurinPotions.ShouldUsePotion(this, out act)) return true;

		if (IsFirstCycle && InArmys && !RadiantFinalePvE.Cooldown.IsCoolingDown) IsFirstCycle = false;

		if (!CanWeave) return false;
		return TryUseEmpyrealArrow(out act)
		       || TryUseBarrage(out act)
			   || TryUsePitchPerfect(out act)
			   || base.EmergencyAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return TryUseSong(out act)
			   || base.GeneralAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGcd, out IAction? act)
	{
		act = null;
		if (!CanWeave) return false;
		return TryUseRadiantFinale(out act)
			   || TryUseBattleVoice(out act)
			   || TryUseRagingStrikes(out act)
			   || TryUseHeartBreakShot(out act)
			   || TryUseSideWinder(out act)
			   || base.AttackAbility(nextGcd, out act);
	}

	#endregion

	#region GCD Logic

	protected override bool GeneralGCD(out IAction? act)
	{
		if (TryUseIronJaws(out act)) return true;
		if (TryUseDoTs(out act)) return true;
		if (TryUseBurst(out act)) return true;
		if (TryUseApexArrow(out act)
		    ||TryUseBlastArrow(out act)) return true;
		return  TryUseFiller(out act)
		        || base.GeneralGCD(out act);
	}

	#endregion

	#endregion

	#region Extra Methods

	#region GCD Skills

	#region DoTs

	private bool WouldUseIronJaws
	{
		get
		{
			if (!IronJawsPvE.EnoughLevel || !CanDoTMobs) return false;
			foreach (var doTAction in DoTActions)
			{
				if (CurrentTarget != null && !TargetHasDoT(doTAction)) return false;
				if (!DoTsEnding(doTAction) || InBurst) continue;
				return true;
			}
			return InBurst && BurstEndGCD(1) && !IsLastGCD(ActionID.IronJawsPvE);
		}
	}

	private bool WouldUseDoTs =>
		CurrentTarget != null
		&& CanDoTMobs
		&& (!TargetHasDoT(Stormbite)
			|| !TargetHasDoT(CausticBite)
			|| (!IronJawsPvE.EnoughLevel && (DoTsEnding(Stormbite) || DoTsEnding(CausticBite))));

	private static bool DoTsEnding(IBaseAction action)
	{
		return CurrentTarget != null
		       && TargetHasDoT(action)
		       && action.Setting.TargetStatusProvide != null
		       && CurrentTarget.WillStatusEndGCD(1, DoTEndBuffer, true, action.Setting.TargetStatusProvide);
	}

	private bool CanDoTMobs => !DoTsBoss || TargetIsBoss;

	private bool TryUseIronJaws(out IAction? act)
	{
		act = null;
		if (!IronJawsPvE.EnoughLevel || !CanDoTMobs) return false;

		foreach (var doTAction in DoTActions)
		{
			if (CurrentTarget != null && !TargetHasDoT(doTAction)) return false;

			if (!DoTsEnding(doTAction) || InBurst) continue;

			return IronJawsPvE.CanUse(out act, true);

		}

		if (InBurst && BurstEndGCD(1) && !IsLastGCD(ActionID.IronJawsPvE))
		{
			return IronJawsPvE.CanUse(out act, true);
		}

		return false;
	}

	private bool TryUseDoTs(out IAction? act)
	{
		act = null;
		if (CurrentTarget == null || !CanDoTMobs) return false;

		if (!TargetHasDoT(Stormbite) && Stormbite.CanUse(out act, true)) return true;

		if (!TargetHasDoT(CausticBite) && CausticBite.CanUse(out act, true)) return true;

		var stormEnding   = DoTsEnding(Stormbite);
		var causticEnding = DoTsEnding(CausticBite);

		if (!stormEnding && !causticEnding) return false;

		if (!IronJawsPvE.EnoughLevel)
		{
			return stormEnding  && Stormbite.CanUse(out act, true)
			       || causticEnding && CausticBite.CanUse(out act, true);
		}

		return false;
	}

	#endregion

	#region Burst GCDs

	private bool TryUseBurst(out IAction? act)
	{
		act = null;
		if (!InBurst) return false;
		if (TryUseRadiantEncore(out act)) return true;
		if (TryUseApexArrow(out act) || TryUseBlastArrow(out act)) return true;
		if (TryUseResonantArrow(out act)) return true;
		if (TryUseIronJaws(out act)) return true;
		return TryUseFiller(out act)
			|| base.GeneralGCD(out act);
	}

	private bool CanSpendSoulVoice
	{
		get
		{
			if (NoSong) return false;

			if (SoulVoice < 80) return false;

			if (InWanderers)
			{
				return InBurst && (SoulVoice == 100 || BurstEndGCD(3));
			}

			if (!InMages) return false;

			if (SoulVoice == 100 && !WouldUseIronJaws) return true;

			return SongEndAfter(18);
		}
	}

	private bool TryUseApexArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanSpendSoulVoice) return false;

		return ApexArrowPvE.CanUse(out act);

	}

	private bool TryUseBlastArrow(out IAction? act)
	{
		act = null;
		if (!BlastArrowPvEReady) return false;

		if (WouldUseIronJaws || WouldUseDoTs) return false;

		return BlastArrowPvE.CanUse(out act, skipComboCheck: true);
	}

	private bool TryUseRadiantEncore(out IAction? act)
	{
		act = null;
		if (!HasRadiantFinale || !InBurst) return false;
		return RadiantEncorePvE.CanUse(out act, skipComboCheck: true);
	}

	private bool TryUseResonantArrow(out IAction? act)
	{
		act = null;
		if (!HasResonantArrow && !InBurst) return false;

		return ResonantArrowPvE.CanUse(out act, skipComboCheck: true);
	}

	#endregion

	#region Filler GCDs

	private bool TryUseAoE(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;

		var procAoE = ShadowbitePvE.EnoughLevel ? ShadowbitePvE : WideVolleyPvE;

		if (CanUseEnhancedFiller && !WouldUseDoTs)
		{
			return procAoE.CanUse(out act, skipComboCheck: true);
		}
		return LadonsbitePvE.EnoughLevel
			? LadonsbitePvE.CanUse(out act)
			: QuickNockPvE.CanUse(out act);
	}

	private bool TryUseFiller(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;

		if (TryUseAoE(out act)) return true;

		var procArrow = RefulgentArrowPvE.EnoughLevel ? RefulgentArrowPvE : StraightShotPvE;

		if (CanUseEnhancedFiller && !WouldUseDoTs)
		{
			return procArrow.CanUse(out act, skipComboCheck: true);
		}

		return BurstShotPvE.CanUse(out act, skipComboCheck: true) && !CanUseEnhancedFiller && !HasResonantArrow;
	}

	#endregion

	#endregion

	#region oGCD Abilities

	#region Emergency Abilities

	private bool TryUseBarrage(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !InBurst) return false;

		if (HasHawksEye && !BurstEndGCD(3)) return false;

		return BarragePvE.CanUse(out act) && CanWeave;
	}

	private bool CanUseEmpyrealArrow
	{
		get
		{
			if (WeaponRemain <= DataCenter.CalculatedActionAhead + Math.Max(AnimationLock, 0.6f)) return false;

			if (!EmpyrealArrowPvE.Cooldown.IsCoolingDown
			    || EmpyrealArrowPvE.Cooldown.HasOneCharge)
			{
				return CanWeave;
			}

			var recast = EmpyrealArrowPvE.Cooldown.RecastTimeRemain;

			if (recast >= WeaponTotal) return false;

			if (recast >= WeaponRemain) return false;

			return CanWeave && EmpyrealArrowPvE.Cooldown.HasOneCharge;
		}
	}

	private bool EmpyrealArrowTimingCheck
	{
		get
		{
            if (IsStandardTiming)
            {
	            return true;
            }

            if (!Is369)
            {
	            return false;
            }

            if (InWanderers)
            {
	            return InBurst || RagingStrikesPvE.Cooldown.IsCoolingDown;
            }
            if (InMages)
            {
	            return IsFirstCycle ? EnoughWeaveTime : !SongEndAfter(MageRemainTime);
            }

            return InArmys && EnoughWeaveTime;
		}
	}

	private bool TryUseEmpyrealArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;

		if (NoSong) return false;

		if (EmpyrealArrowTimingCheck && CanUseEmpyrealArrow)
		{
			return EmpyrealArrowPvE.CanUse(out act);
		}

		return false;
	}

	#endregion

	#region Songs

	private bool ShouldSwapSong
	{
		get
		{
			if (NoSong) return true;
			if (InWanderers) return SongEndAfter(WandRemainTime - Math.Max(DataCenter.CalculatedActionAhead, AnimationLock));
			if (InMages) return SongEndAfter(MageRemainTime);
			return InArmys && SongEndAfter(ArmyRemainTime) && CanLateWeave;
		}
	}
	private static bool ShouldBlockSongSwap(ActionID prev1, ActionID prev2) =>
		!EnableSandbagMode && (IsLastAbility(prev1) || IsLastAbility(prev2));
	private bool TryUseSong(out IAction? act)
	{
		act = null;

		if (SongList.Length == 0)
			return false;

		if (!NoSong && !ShouldSwapSong)
			return false;

		foreach (var song in SongList)
		{
			if (song == TheWanderersMinuetPvE && TryUseWanderersMinuet(out act)) return true;
			if (song == MagesBalladPvE && TryUseMagesBallad(out act)) return true;
			if (song == ArmysPaeonPvE && TryUseArmys(out act)) return true;
		}

		return false;
	}

	private bool CanUseWanderersMinuet
	{
		get
		{
			if (NoSong && IsFirstCycle)
			{
				if (IsStandardTiming) return true;
				if (IsCustom) return (WanderersWeave == WandererWeave.Early && CanEarlyWeave)
					|| (WanderersWeave == WandererWeave.Late && CanLateWeave);
				if (Is369) return CanLateWeave;
			}

			if (InArmys) return ShouldSwapSong;

			return NoSong && ArmysPaeonPvE.Cooldown.IsCoolingDown && MagesBalladPvE.Cooldown.IsCoolingDown;
		}
	}

	private bool TryUseWanderersMinuet(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.ArmysPaeonPvE, ActionID.MagesBalladPvE)) return false;

		return CanUseWanderersMinuet && TheWanderersMinuetPvE.CanUse(out act);
	}

	private bool CanUseMagesBallad
	{
		get
		{
			if (InWanderers && ShouldSwapSong)
			{
				return (Repertoire == 0
				       || IsLastAbility(ActionID.PitchPerfectPvE)
				       || !HasHostilesInMaxRange
				       || EnableSandbagMode) && CanLateWeave;
			}

			if (InArmys && ShouldSwapSong) return TheWanderersMinuetPvE.Cooldown.IsCoolingDown;

			return NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown);
		}
	}
	private bool TryUseMagesBallad(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.ArmysPaeonPvE, ActionID.TheWanderersMinuetPvE)) return false;

		return CanUseMagesBallad && MagesBalladPvE.CanUse(out act);
	}

	private bool CanUseArmysPaeon
	{
		get
		{
			if (EnableSandbagMode) return InMages && ShouldSwapSong;

			if (IsStandardTiming)
			{
				if (InMages) return ShouldSwapSong;

				if (InWanderers) return ShouldSwapSong && MagesBalladPvE.Cooldown.IsCoolingDown;

				if (NoSong) return TheWanderersMinuetPvE.Cooldown.IsCoolingDown || MagesBalladPvE.Cooldown.IsCoolingDown;
			}

			if (!Is369 || !ShouldSwapSong) return false;

			if (IsFirstCycle)
			{
				return CanLateWeave
				       || IsLastAbility(ActionID.EmpyrealArrowPvE);
			}
			return EnoughWeaveTime;
		}
	}

	private bool TryUseArmys(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.TheWanderersMinuetPvE, ActionID.MagesBalladPvE)) return false;

		return CanUseArmysPaeon && ArmysPaeonPvE.CanUse(out act);
	}

	#endregion

	#region Buffs

	private static bool RecastIsLessThanGCD(IBaseAction action)
	{
		if (!action.Cooldown.IsCoolingDown) return true;

		return action.Cooldown.RecastTimeRemain < WeaponTotal;
	}

	private static bool ElapsedIsMoreThanGCD(IBaseAction action)
	{
		if (!action.Cooldown.IsCoolingDown) return false;

		return action.Cooldown.RecastTimeElapsedRaw > WeaponTotal;
	}

	private bool CanUseRadiantFinale
	{
		get
		{
			if (!InWanderers && !CanBurstChanged) return false;

			if (IsStandardTiming)
			{
				return  IsFirstCycle
					? HasBattleVoice
					: ElapsedIsMoreThanGCD(TheWanderersMinuetPvE) && RecastIsLessThanGCD(BattleVoicePvE);
			}

			return Is369
			       && (IsFirstCycle
				       ? !WouldUseDoTs && CanLateWeave
				       : ElapsedIsMoreThanGCD(TheWanderersMinuetPvE) && RecastIsLessThanGCD(BattleVoicePvE) && CanEarlyWeave
				       );
		}
	}

	private bool TryUseRadiantFinale(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;

		return CanUseRadiantFinale && RadiantFinalePvE.CanUse(out act);
	}

	private bool CanUseBattleVoice
	{
		get
		{
			if (!InWanderers && !CanBurstChanged) return false;
			if (IsStandardTiming)
			{
				return  CanLateWeave
					&& (IsFirstCycle
						? !HasRadiantFinale
						: HasRadiantFinale || IsLastAbility(ActionID.RadiantFinalePvE));
			}
			return Is369
			       && (IsFirstCycle
			       ? !WouldUseDoTs && HasRadiantFinale && CanEarlyWeave
			       : HasRadiantFinale && CanLateWeave);
		}
	}

	private bool TryUseBattleVoice(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;

		return CanUseBattleVoice && BattleVoicePvE.CanUse(out act);
	}

	private bool TryUseRagingStrikes(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;

		var hasOtherBurst = false;
		var allOtherPresent = true;
		foreach (var s in BurstStatus)
		{
			if (s == StatusID.RagingStrikes) continue;
			hasOtherBurst = true;
			if (StatusHelper.PlayerHasStatus(true, s)) continue;
			allOtherPresent = false;
			break;
		}

		if (!hasOtherBurst || allOtherPresent)
			return RagingStrikesPvE.CanUse(out act) && CanLateWeave;

		return false;
	}

	#endregion

	#region Attack Abilities

	private bool TryUseBloodletterVariant(out IAction? act, bool usedUp) =>
		RainOfDeathPvE.CanUse(out act, usedUp: usedUp)
		|| HeartbreakShotPvE.CanUse(out act, usedUp: usedUp)
		|| BloodletterPvE.CanUse(out act, usedUp: usedUp);

	private bool TryUseHeartBreakShot(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanWeave || !EnoughWeaveTime) return false;

		var willHaveMaxCharges = HeartbreakShotPvE.Cooldown.WillHaveXCharges(BloodletterMax, 5);
		var willHaveOneCharge  = HeartbreakShotPvE.Cooldown.WillHaveOneCharge(5);
		var wontHaveCharge     = HeartbreakShotPvE.Cooldown.IsCoolingDown
		                         && !HeartbreakShotPvE.Cooldown.WillHaveOneCharge(WeaponAhead)
		                         && WeaponElapsed <= 1f;

		var holdForRagingOrCap = (!InBurst || !HasRagingStrikes)
			&& BloodletterPvE.Cooldown.CurrentCharges < 3
			&& !willHaveMaxCharges;
		var holdForBurstTiming = InBurst
			&& (!willHaveOneCharge || !CanWeave);
		var isInWanderersHold  = InWanderers && (holdForRagingOrCap || holdForBurstTiming);

		var isInArmysHold = InArmys
			&& SongTime <= ArmyHeartbreakHoldThreshold
			&& BloodletterPvE.Cooldown.CurrentCharges < 3
			&& !willHaveMaxCharges;

		var isInMagesHold = InMages && SongEndAfter(MageRemainTime + WeaponTotal * 0.9f);

		var isEmpyrealBlocking = !NoSong && !InBurst
			&& (EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(WeaponTotal) && CanUseEmpyrealArrow
			    || wontHaveCharge);

		if (isInWanderersHold || isInArmysHold || isInMagesHold || isEmpyrealBlocking) return false;

		if (SongTimings == SongTiming.Cycle369 && NoSong && HeartbreakShotPvE.CanUse(out act, usedUp: false)) return true;

		if (!CanWeave) return false;

		var shouldUseUsedUp = InBurst || IsMedicated
			|| (willHaveOneCharge && (InMages || (InArmys && SongTime > 30f)));
		if (shouldUseUsedUp) return TryUseBloodletterVariant(out act, usedUp: true);

		var atChargeCap = BloodletterPvE.Cooldown.CurrentCharges == BloodletterMax || willHaveMaxCharges;
		return atChargeCap && TryUseBloodletterVariant(out act, usedUp: false);
	}

	private bool TryUseSideWinder(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;
		if (!SidewinderPvE.Cooldown.WillHaveOneCharge(WeaponAhead)) return false;

		var rFWillHaveCharge = RadiantFinalePvE.Cooldown.IsCoolingDown
			&& RadiantFinalePvE.Cooldown.WillHaveOneCharge(SidewinderBuffLookahead);
		var bVWillHaveCharge = BattleVoicePvE.Cooldown.IsCoolingDown
			&& BattleVoicePvE.Cooldown.WillHaveOneCharge(SidewinderBuffLookahead);

		if (!EnoughWeaveTime || !SidewinderPvE.CanUse(out act)) return false;

		var noBurstIncoming = !rFWillHaveCharge && !bVWillHaveCharge && RagingStrikesPvE.Cooldown.IsCoolingDown;
		var rsExpiring      = RagingStrikesPvE.Cooldown.IsCoolingDown && !HasRagingStrikes;
		return InBurst || !RadiantFinalePvE.EnoughLevel || noBurstIncoming || rsExpiring;
	}

	private bool TryUsePitchPerfect(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || Song != Song.WanderersMinuet) return false;

		if (!PitchPerfectPvE.CanUse(out act)) return false;

		if (Repertoire == 3) return true;
		if (!InBurst && !RagingStrikesPvE.Cooldown.IsCoolingDown) return false;
		if (Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1)) return true;

		return SongEndAfter(WandRemainTime - DataCenter.CalculatedActionAhead + AnimationLock) && WeaponRemain > LateWeaveWindow;
	}

	#endregion

	#endregion

	#region Miscellaneous

	private bool IsInSandbagMode =>
		EnableSandbagMode && (!InBurst || Song != Song.WanderersMinuet) &&
		((IsFirstCycle
		  && !RadiantFinalePvE.Cooldown.HasOneCharge
		  && !BattleVoicePvE.Cooldown.HasOneCharge
		  && !RagingStrikesPvE.Cooldown.HasOneCharge
		  && RadiantFinalePvE.Cooldown.IsCoolingDown
		  && BattleVoicePvE.Cooldown.IsCoolingDown
		  && RagingStrikesPvE.Cooldown.IsCoolingDown)
		 || (!IsFirstCycle
		     && !BattleVoicePvE.Cooldown.HasOneCharge
		     && !RagingStrikesPvE.Cooldown.HasOneCharge));

	/// <summary>
	/// BRD-specific potion manager that extends base potion logic with job-specific conditions.
	/// </summary>
	private class ChurinBRDPotions : Potions
	{
		public override bool IsConditionMet()
		{
			if (IsFirstCycle)
			{
				switch (ChurinBRD.OpenerPotionTime)
				{
					case > 0f:
					case 0f when InWanderers:
						return true;
				}
			}
			else
			{
				if (InWanderers && HasBattleVoice && HasRadiantFinale) return true;

				if (InOddMinuteWindow) return true;
			}

			return false;
		}

		protected override bool IsTimingValid(float timing)
		{
			if (timing > 0 && DataCenter.CombatTimeRaw >= timing &&
				DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds) return true;

			// Check opener timing: if it's an opener potion and countdown is within configured time
			var countDown = Service.CountDownTime;

			if (IsOpenerPotion(timing))
			{
				if (ChurinBRD.OpenerPotionTime == 0f) return IsFirstCycle && InWanderers;

				return countDown > 0f && countDown <= ChurinBRD.OpenerPotionTime;
			}

			return false;
		}
	}

	#endregion

	#endregion

	#region Tracking Properties

	public override void DisplayRotationStatus()
              	{
              		ImGui.Text("===GCD Status===");
              		ImGui.Text($"Weapon Remain: {WeaponRemain}");
	                ImGui.Text($"Weapon Elapsed {WeaponElapsed}");
	                ImGui.Text($"Calculated Action Ahead {DataCenter.CalculatedActionAhead}");
	                ImGui.Text($"Can Weave {CanWeave}");
              		ImGui.Text($"Enough Weave Time: {EnoughWeaveTime}");
	                ImGui.Text($"Late Weave Window: {LateWeaveWindow}");
              		ImGui.Text($"Can Late Weave: {CanLateWeave}");
              		ImGui.Text($"Can Early Weave: {CanEarlyWeave}");
					ImGui.Text($"Empyreal Arrow Recast Remain: {EmpyrealArrowPvE.Cooldown.RecastTimeRemain} - {WeaponRemain} = {Math.Abs(EmpyrealArrowPvE.Cooldown.RecastTimeRemain - WeaponRemain)}");
	                ImGui.Text($"Target Has Stormbite: {TargetHasDoT(Stormbite)}");
	                ImGui.Text($"Target Has Caustic Bite: {TargetHasDoT(CausticBite)}");
			  		ImGui.Text($"In Burst: {InBurst}");
              	}

	#endregion
}
