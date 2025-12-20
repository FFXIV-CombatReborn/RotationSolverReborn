using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin BRD", CombatType.PvE, GameVersion = "7.3",
    Description = "I sing the body electric. I gasp the body organic. I miss the body remembered.")]
[SourceCode(Path = "main/ExtraRotations/Ranged/ChurinBRD.cs")]
[ExtraRotation]
public sealed class ChurinBRD : BardRotation
{
    #region Properties

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

    private float WandTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 42f,
        SongTiming.Cycle369 => 42f,
        SongTiming.Custom => CustomWandTime,
        _ => 0f
    };

    private float MageTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 42f,
        SongTiming.Cycle369 => 39f,
        SongTiming.Custom => CustomMageTime,
        _ => 0f
    };

    private float ArmyTime => SongTimings switch
    {
        SongTiming.Standard or SongTiming.AdjustedStandard => 33f,
        SongTiming.Cycle369 => 36f,
        SongTiming.Custom => CustomArmyTime,
        _ => 0f
    };
    private float WandRemainTime => 45f - WandTime;
    private float MageRemainTime => 45f - MageTime;
    private float ArmyRemainTime => 45f - ArmyTime;

    private static bool CanLateWeave => (WeaponRemain > 0) && (WeaponRemain <= LateWeaveWindow) && EnoughWeaveTime;
    private static bool CanEarlyWeave => (WeaponRemain > 0) && (WeaponRemain > LateWeaveWindow);
    private static float LateWeaveWindow => WeaponTotal * 0.5f;

    private static bool TargetHasDoTs =>
        CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true &&
        CurrentTarget.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite);

    private static bool DoTsEnding => CurrentTarget?.WillStatusEndGCD(1, 0.5f, true, StatusID.Windbite,
        StatusID.Stormbite,
        StatusID.VenomousBite, StatusID.CausticBite) ?? false;

    private static bool InWanderers => Song == Song.Wanderer;
    private static bool InMages => Song == Song.Mage;
    private static bool InArmys => Song == Song.Army;
    private static bool NoSong => Song == Song.None;
    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !StatusHelper.PlayerWillStatusEnd(0f,true, StatusID.Medicated);
    private static bool HasResonantArrow => StatusHelper.PlayerHasStatus(true, StatusID.ResonantArrowReady);
    private static bool InOddMinuteWindow => InMages && SongTime > 15f;
    private static bool EnoughWeaveTime => (WeaponRemain > AnimLock) && WeaponRemain > 0;
    private static float AnimLock => Math.Max(AnimationLock, WeaponTotal * 0.25f);

    private bool InBurst => (!BattleVoicePvE.EnoughLevel && !RadiantFinalePvE.EnoughLevel && HasRagingStrikes) ||
                            (!RadiantFinalePvE.EnoughLevel && HasRagingStrikes && HasBattleVoice) ||
                            (HasRagingStrikes && HasBattleVoice && HasRadiantFinale);

    private static bool IsFirstCycle { get; set; }

    #endregion

    #region  Tracking Properties

    public override void DisplayRotationStatus()
    {
        ImGui.Text("===GCD Status===");
        ImGui.Text($"Weapon Remain: {WeaponRemain}");
        ImGui.Text($"Animation Lock: {AnimLock}");
        ImGui.Text($"Enough Weave Time: {EnoughWeaveTime}");
        ImGui.Text($"Can Late Weave: {CanLateWeave}");
        ImGui.Text($"Can Early Weave: {CanEarlyWeave}");
    }

    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Only use DOTs on targets with Boss Icon")]
    private bool DoTsBoss { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Choose Bard Song Timing Preset")]
    private static SongTiming SongTimings { get; set; }

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Minuet Uptime", Parent = nameof(SongTimings), ParentValue = SongTiming.Custom)]
    private float CustomWandTime { get; set; } = 45f;

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Mage's Ballad Uptime", Parent = nameof(SongTimings), ParentValue = SongTiming.Custom)]
    private float CustomMageTime { get; set; } = 45f;

    [Range(1, 45, ConfigUnitType.Seconds, 1)]
    [RotationConfig(CombatType.PvE, Name = "Custom Army's Paeon Uptime", Parent = nameof(SongTimings), ParentValue = SongTiming.Custom)]
    private float CustomArmyTime { get; set; } = 45f;

    [RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Weave Slot Timing", Parent = nameof(SongTimings), ParentValue = SongTiming.Custom)]
    private WandererWeave WanderersWeave { get; set; } = WandererWeave.Early;

    [RotationConfig(CombatType.PvE, Name = "Enable PrepullHeartbreak Shot? - Use with BMR Auto Attack Manager")]
    private bool EnablePrepullHeartbreakShot { get; set; } = true;

    private static readonly ChurinBRDPotions ChurinPotions = new();
    private float _firstPotionTiming;
    private float _secondPotionTiming;
    private float _thirdPotionTiming;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    { get => ChurinPotions.Enabled; set => ChurinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    { get => ChurinPotions.Strategy; set => ChurinPotions.Strategy = value; }

    [Range(0,20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus time in seconds - only use if potting early in the opener", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => ChurinPotions.OpenerPotionTime; set => ChurinPotions.OpenerPotionTime = value; }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 1st Potion at (value in seconds - leave at 0 if using in opener)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float FirstPotionTiming
    {
        get => _firstPotionTiming;
        set
        {
            _firstPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float SecondPotionTiming
    {
        get => _secondPotionTiming;
        set
        {
            _secondPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 3rd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float ThirdPotionTiming
    {
        get => _thirdPotionTiming;
        set
        {
            _thirdPotionTiming = value;
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

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        IsFirstCycle = true;
        if (ChurinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }
        return SongTimings switch
        {
            SongTiming.AdjustedStandard when remainTime <= 0f && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Cycle369 when EnablePrepullHeartbreakShot && remainTime < 1.65f && HeartbreakShotPvE.CanUse(out var act) => act,
            SongTiming.Cycle369 when remainTime < 1.29f && StormbitePvE.CanUse(out var act) => act,
            _ => base.CountDownAction(remainTime)
        };    
    }

#endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (ChurinPotions.ShouldUsePotion(this, out act)) return true;

        if (IsFirstCycle && InArmys && !RadiantFinalePvE.Cooldown.IsCoolingDown)
        {
            IsFirstCycle = false;
        }

        if (!EnoughWeaveTime) return false;

        return TryUseEmpyrealArrow(out act)
               || TryUseHeartBreakShot(out act)
               || TryUsePitchPerfect(out act)
               || base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!EnoughWeaveTime) return false;

        return TryUseWanderers(out act)
               || TryUseMages(out act)
               || TryUseArmys(out act)
               || base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGcd, out IAction? act)
    {
        act = null;
        if (!EnoughWeaveTime) return false;

            return TryUseRadiantFinale(out act)
                   || TryUseBattleVoice(out act)
                   || TryUseRagingStrikes(out act)
                   || TryUseBarrage(out act)
                   || TryUseSideWinder(out act)
                   || base.AttackAbility(nextGcd, out act);
        }

    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (TryUseIronJaws(out act)) return true;
        if (TryUseDoTs(out act)) return true;

        return InBurst &&RadiantEncorePvE.CanUse(out act, skipComboCheck: true) ||
               TryUseApexArrow(out act) ||
               TryUseBlastArrow(out act) ||
               HasResonantArrow && ResonantArrowPvE.CanUse(out act) ||
               TryUseAoE(out act) ||
               TryUseFiller(out act) ||
               base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods

    #region GCD Skills

    private bool TryUseIronJaws(out IAction? act)
    {
        if (IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true) && (IronJawsPvE.Target.Target?.WillStatusEnd(30f, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) ?? false))
        {
            if (InBurst && StatusHelper.PlayerWillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes) && !BlastArrowPvE.CanUse(out _))
            {
                return true;
            }
        }
        return IronJawsPvE.CanUse(out act);
    }

    private bool TryUseDoTs(out IAction? act)
    {
        if (IronJawsPvE.CanUse(out act) || TargetHasDoTs) return false;

        if (StormbitePvE.EnoughLevel)
        {
            if (StormbitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
                (!DoTsBoss || StormbitePvE.Target.Target.IsBossFromIcon()) &&
                !StormbitePvE.Target.Target.HasStatus(true, StatusID.Stormbite))
            {
                return true;
            }
        }

        if (CausticBitePvE.EnoughLevel)
        {
            if (CausticBitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
                (!DoTsBoss || CausticBitePvE.Target.Target.IsBossFromIcon()) &&
                !CausticBitePvE.Target.Target.HasStatus(true, StatusID.VenomousBite))
            {
                return true;
            }
        }

        if (!StormbitePvE.EnoughLevel && WindbitePvE.CanUse(out act, skipStatusProvideCheck:true) &&
            (!DoTsBoss || WindbitePvE.Target.Target.IsBossFromIcon()))
        {
            if (!IronJawsPvE.EnoughLevel ||
                (IronJawsPvE.EnoughLevel && !WindbitePvE.Target.Target.HasStatus(true, StatusID.Windbite)))
            {
                return true;
            }
        }

        if (!CausticBitePvE.EnoughLevel && VenomousBitePvE.CanUse(out act, skipStatusProvideCheck: true) &&
            (!DoTsBoss || VenomousBitePvE.Target.Target.IsBossFromIcon()))
        {
            if (!IronJawsPvE.EnoughLevel ||
                (IronJawsPvE.EnoughLevel && !VenomousBitePvE.Target.Target.HasStatus(true, StatusID.CausticBite)))
            {
                return true;
            }
        }

        act = null;
        return false;
    }

    private bool TryUseApexArrow(out IAction? act)
        {
            if (ShouldEnterSandbagMode() || InBurst && HasBarrage || SoulVoice < 80) return SetActToNull(out act);

            var hasFullSoul = SoulVoice == 100;
            var hasRagingStrikes = StatusHelper.PlayerHasStatus(true, StatusID.RagingStrikes);
            var hasBattleVoice = StatusHelper.PlayerHasStatus(true, StatusID.BattleVoice);

            return ApexArrowPvE.CanUse(out act) switch
            {
                true when (QuickNockPvE.CanUse(out _) || LadonsbitePvE.CanUse(out _)) && hasFullSoul => true,
                true when CurrentTarget?.WillStatusEndGCD(1, 1, true, StatusID.Windbite, StatusID.Stormbite,
                    StatusID.VenomousBite, StatusID.CausticBite) ?? false => false,
                true when hasFullSoul && BattleVoicePvE.Cooldown.WillHaveOneCharge(25) => false,
                true when InWanderers && SoulVoice >= 80 && !hasRagingStrikes => false,
                true when hasRagingStrikes && StatusHelper.PlayerWillStatusEnd(10, true, StatusID.RagingStrikes) &&
                          (hasFullSoul || SoulVoice >= 80) => true,
                true when hasFullSoul && hasRagingStrikes && hasBattleVoice => true,
                true when InMages && SoulVoice >= 80 && SongEndAfter(22) && SongEndAfter(18) => true,
                true when hasFullSoul && !hasRagingStrikes => true,
                _ => SetActToNull(out act)
            };
        }

    private bool TryUseBlastArrow(out IAction? act)
    {
        return (HasRagingStrikes, DoTsEnding) switch
        {
            (true, false) when BarragePvE.Cooldown.IsCoolingDown || IsLastGCD(ActionID.ApexArrowPvE) => BlastArrowPvE.CanUse(out act),
            (false, false) when InMages => BlastArrowPvE.CanUse(out act),
            _ => SetActToNull(out act)
        };
    }

    private bool TryUseAoE(out IAction? act)
    {
        if (ShouldEnterSandbagMode()) return SetActToNull(out act);

        return ShadowbitePvE.CanUse(out act) ||
               WideVolleyPvE.CanUse(out act) ||
               QuickNockPvE.CanUse(out act);
    }

    private bool TryUseFiller(out IAction? act)
    {
        if (ShouldEnterSandbagMode()) return SetActToNull(out act);

        return (RefulgentArrowPvE.CanUse(out act, skipComboCheck: true) && TargetHasDoTs) ||
               StraightShotPvE.CanUse(out act) ||
               (BurstShotPvE.CanUse(out act) && !HasHawksEye && !HasResonantArrow && !HasBarrage);
    }

    #endregion

    #region oGCD Abilities

        #region Emergency Abilities

        private bool TryUseBarrage(out IAction? act)
        {
            act = null;
            var empyrealArrowReady = EmpyrealArrowPvE.EnoughLevel && Repertoire == 3;

            if (!HasRagingStrikes || empyrealArrowReady || HasHawksEye || ShouldEnterSandbagMode())
                return false;

            return BarragePvE.CanUse(out act) && EnoughWeaveTime ;
        }

        private bool TryUseEmpyrealArrow(out IAction? act)
        {
            act = null;
            if (ShouldEnterSandbagMode()
                || !EmpyrealArrowPvE.Cooldown.HasOneCharge) return false;

            if (EmpyrealArrowPvE.CanUse(out act))
            {
                return (SongTimings, Song) switch
                {
                    (SongTiming.Standard or SongTiming.Custom, _) => IsFirstCycle
                        ? (InWanderers || !NoSong) && CanLateWeave
                        : EnoughWeaveTime,
                    (SongTiming.AdjustedStandard, _) => InWanderers && CanLateWeave ||
                                                        (InMages || InArmys) && EnoughWeaveTime,
                    (SongTiming.Cycle369, Song.Wanderer) =>  HasRagingStrikes && EnoughWeaveTime ||
                                                             RagingStrikesPvE.Cooldown.IsCoolingDown &&
                                                             !RagingStrikesPvE.Cooldown.WillHaveOneCharge(1f) &&
                                                             EnoughWeaveTime,
                    (SongTiming.Cycle369, Song.Mage) => IsFirstCycle ? EnoughWeaveTime : !SongEndAfter(MageRemainTime),
                    (SongTiming.Cycle369, Song.Army) => EnoughWeaveTime,
                    _ => false,

                };
            }
            return false;
        }

        #endregion

        #region Songs

        private bool TryUseWanderers(out IAction? act)
        {
            act = null;
            if (!TheWanderersMinuetPvE.EnoughLevel
                || !EnableSandbagMode && (IsLastAbility(ActionID.ArmysPaeonPvE)
                                          || IsLastAbility(ActionID.MagesBalladPvE)))
            {
                return false;
            }

            if (NoSong && IsFirstCycle && TheWanderersMinuetPvE.CanUse(out act))
                return SongTimings switch
                {
                    SongTiming.Standard or SongTiming.AdjustedStandard => true,
                    SongTiming.Cycle369 => CanLateWeave,
                    SongTiming.Custom => WanderersWeave == WandererWeave.Early && CanEarlyWeave ||
                                         WanderersWeave == WandererWeave.Late && CanLateWeave,
                                         _ => false
                };

            if (!IsFirstCycle &&
                (InArmys && SongEndAfter(ArmyRemainTime)
                 || (NoSong && (ArmysPaeonPvE.Cooldown.IsCoolingDown
                                || MagesBalladPvE.Cooldown.IsCoolingDown))) && CanLateWeave)
            {
                return TheWanderersMinuetPvE.CanUse(out act);
            }
            return false;
        }

        private bool TryUseMages(out IAction? act)
        {
            act = null;
            if (!MagesBalladPvE.EnoughLevel
                || !EnableSandbagMode && (IsLastAbility(ActionID.ArmysPaeonPvE)
                                          || IsLastAbility(ActionID.TheWanderersMinuetPvE)))
            {
                return false;
            }

            if (MagesBalladPvE.CanUse(out act))
            {
                if (InWanderers && SongEndAfter(WandRemainTime - 0.625f)
                                && (Repertoire == 0 || !HasHostilesInMaxRange || IsLastAbility(ActionID.PitchPerfectPvE))
                    || InArmys && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown
                    || NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown)
                    || EnableSandbagMode && SongEndAfter(WandRemainTime))
                {
                    return CanLateWeave;
                }

            }

            return false;
        }

        private bool TryUseArmys(out IAction? act)
        {
            act = null;
            if (!ArmysPaeonPvE.EnoughLevel
                || !EnableSandbagMode && (IsLastAbility(ActionID.TheWanderersMinuetPvE)
                                          || IsLastAbility(ActionID.MagesBalladPvE)))
            {
                return false;
            }

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if (((InMages && SongEndAfter(MageRemainTime))
                         || (InWanderers && SongEndAfter(2f) && MagesBalladPvE.Cooldown.IsCoolingDown)
                         || (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2f))
                         || (NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown ||
                                        MagesBalladPvE.Cooldown.IsCoolingDown))) && CanLateWeave
                        || EnableSandbagMode && InMages && SongEndAfter(MageRemainTime))
                    {
                        return ArmysPaeonPvE.CanUse(out act);
                    }
                    break;

                case SongTiming.Cycle369:
                    if (!EnableSandbagMode && (InMages && SongEndAfter(MageRemainTime)
                                               || (InWanderers && SongEndAfter(2f) && MagesBalladPvE.Cooldown.IsCoolingDown)
                                               || (!TheWanderersMinuetPvE.EnoughLevel && SongEndAfter(2f))))
                        switch (IsFirstCycle)
                        {
                            case true:
                                if (CanLateWeave && ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                            case false:
                                if (ArmysPaeonPvE.CanUse(out act)) return true;
                                break;
                        }

                    if (EnableSandbagMode && InMages && SongEndAfter(MageRemainTime))
                    {
                        return ArmysPaeonPvE.CanUse(out act);
                    }

                    break;
            }

            return SetActToNull(out act);
        }

        #endregion

        #region Buffs

        private bool TryUseRadiantFinale(out IAction? act)
        {
            act = null;
            if (!RadiantFinalePvE.EnoughLevel || !RadiantFinalePvE.IsEnabled) return false;

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if ((IsFirstCycle && HasBattleVoice && InWanderers) ||
                        (!IsFirstCycle && InWanderers && BattleVoicePvE.Cooldown.WillHaveOneCharge(0.5f)))
                        return RadiantFinalePvE.CanUse(out act);

                    break;
                case SongTiming.Cycle369:
                {
                    if (IsFirstCycle && InWanderers && TargetHasDoTs)
                        return RadiantFinalePvE.CanUse(out act) && CanLateWeave;
                }
                    if (!IsFirstCycle && InWanderers && BattleVoicePvE.Cooldown.WillHaveOneCharge(0.5f))
                        return RadiantFinalePvE.CanUse(out act) && CanEarlyWeave;
                    break;
            }

            return false;
        }

        private bool TryUseBattleVoice(out IAction? act)
        {
            act = null;
            if (!BattleVoicePvE.EnoughLevel || !BattleVoicePvE.IsEnabled) return false;

            switch (SongTimings)
            {
                case SongTiming.Standard or SongTiming.AdjustedStandard or SongTiming.Custom:
                    if (InWanderers)
                        if (IsFirstCycle && CanLateWeave && !HasRadiantFinale ||
                            !IsFirstCycle && HasRadiantFinale ||
                            IsLastAbility(ActionID.RadiantFinalePvE))
                            return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                    break;
                case SongTiming.Cycle369:
                    if (InWanderers)
                    {
                        if (IsFirstCycle && TargetHasDoTs && HasRadiantFinale)
                        {
                            return BattleVoicePvE.CanUse(out act) && CanEarlyWeave;
                        }

                        if (!IsFirstCycle && HasRadiantFinale)
                        {
                            return BattleVoicePvE.CanUse(out act) && CanLateWeave;
                        }
                    }

                    break;
            }

            return SetActToNull(out act);
        }

        private bool TryUseRagingStrikes(out IAction? act)
        {
            act = null;
            if (!RagingStrikesPvE.Cooldown.WillHaveOneCharge(WeaponRemain)) return false;

            if ((HasBattleVoice && HasRadiantFinale
                 || !RadiantFinalePvE.EnoughLevel
                 || !BattleVoicePvE.EnoughLevel) && CanLateWeave)
            {
                return RagingStrikesPvE.CanUse(out act);
            }

            return false;
        }

        #endregion

        #region Attack Abilities

        private bool TryUseHeartBreakShot(out IAction? act)
        {
            act = null;
            if (ShouldEnterSandbagMode() || !HeartbreakShotPvE.Cooldown.HasOneCharge) return false;

            var willHaveMaxCharges = HeartbreakShotPvE.Cooldown.WillHaveXCharges(BloodletterMax, WeaponRemain - AnimLock) && WeaponRemain > AnimLock;
            var willHaveOneCharge = HeartbreakShotPvE.Cooldown.WillHaveOneCharge(WeaponRemain - AnimLock) && WeaponRemain > AnimLock;

            if ((InWanderers && (((!InBurst || !HasRagingStrikes) && BloodletterPvE.Cooldown.CurrentCharges < 3 && !willHaveMaxCharges)
                || InBurst && (!willHaveOneCharge || IsLastAbility(ActionID.BloodletterPvE, ActionID.RainOfDeathPvE, ActionID.HeartbreakShotPvE))))
                || (InArmys && SongTime <= 30f && BloodletterPvE.Cooldown.CurrentCharges < 3 && !willHaveMaxCharges)
                || (InMages && SongEndAfter(MageRemainTime + WeaponTotal * 0.9f))
                || (!NoSong && (EmpyrealArrowPvE.CanUse(out _) || EmpyrealArrowPvE.Cooldown.WillHaveOneCharge(WeaponTotal))))
            {
                return false;
            }

            if (SongTimings == SongTiming.Cycle369 && NoSong && HeartbreakShotPvE.CanUse(out act, usedUp: false)) 
            {
                return true;
            }

            if ((InBurst || IsMedicated || willHaveOneCharge && (InMages || InArmys && SongTime > 30f)) && EnoughWeaveTime)
            {
                return RainOfDeathPvE.CanUse(out act, usedUp: true) ||
                    HeartbreakShotPvE.CanUse(out act, usedUp: true) ||
                    BloodletterPvE.CanUse(out act, usedUp: true);
            }
            
            if ((BloodletterPvE.Cooldown.CurrentCharges == BloodletterMax 
            || willHaveMaxCharges) && EnoughWeaveTime)
            {
                return RainOfDeathPvE.CanUse(out act, usedUp: false) 
                || HeartbreakShotPvE.CanUse(out act, usedUp: false) 
                || BloodletterPvE.CanUse(out act, usedUp: false);
            }
            return SetActToNull(out act);
        }

        private bool TryUseSideWinder(out IAction? act)
        {
            act = null;
            if (ShouldEnterSandbagMode() || !SidewinderPvE.Cooldown.WillHaveOneCharge(WeaponRemain)) return false;

            var rFWillHaveCharge = RadiantFinalePvE.Cooldown.IsCoolingDown &&RadiantFinalePvE.Cooldown.WillHaveOneCharge(10f);
            var bVWillHaveCharge = BattleVoicePvE.Cooldown.IsCoolingDown && BattleVoicePvE.Cooldown.WillHaveOneCharge(10f);

            if  (InBurst || !RadiantFinalePvE.EnoughLevel ||
                (!rFWillHaveCharge && !bVWillHaveCharge && RagingStrikesPvE.Cooldown.IsCoolingDown) ||
                (RagingStrikesPvE.Cooldown.IsCoolingDown && !HasRagingStrikes))
                return SidewinderPvE.CanUse(out act) && EnoughWeaveTime;

            return SetActToNull(out act);
        }

        private bool TryUsePitchPerfect(out IAction? act)
        {
            act = null;
            if (!InBurst && !RagingStrikesPvE.Cooldown.IsCoolingDown || ShouldEnterSandbagMode() || Song != Song.Wanderer) return false;

            return Repertoire switch
            {
                3 when PitchPerfectPvE.CanUse(out act) => true,
                2 when EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1) && PitchPerfectPvE.CanUse(out act) => true,
                > 0 when SongEndAfter(WandRemainTime) && SongTime < (WandRemainTime - AnimationLock) && PitchPerfectPvE.CanUse(out act) => true,
                _ => false
            };
        }

        #endregion

        #endregion

    #region Miscellaneous

        private static bool SetActToNull(out IAction? act)
        {
            act = null;
            return false;
        }

        private bool ShouldEnterSandbagMode()
        {
            return EnableSandbagMode && (!InBurst || Song != Song.Wanderer) &&
                   (IsFirstCycle && !RadiantFinalePvE.Cooldown.HasOneCharge && !BattleVoicePvE.Cooldown.HasOneCharge && !RagingStrikesPvE.Cooldown.HasOneCharge &&
                    RadiantFinalePvE.Cooldown.IsCoolingDown && BattleVoicePvE.Cooldown.IsCoolingDown && RagingStrikesPvE.Cooldown.IsCoolingDown ||
                    !IsFirstCycle && !BattleVoicePvE.Cooldown.HasOneCharge && !RagingStrikesPvE.Cooldown.HasOneCharge);
        }

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
                    case 0f when InWanderers && TargetHasDoTs:
                        return true;
                }
            }
            else
            {
                if (InWanderers && HasBattleVoice && HasRadiantFinale)
                {
                    return true;
                }

                if (InOddMinuteWindow)
                {
                    return true;
                }
            }
            return false;
        }

        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && (DataCenter.CombatTimeRaw >= timing) && ((DataCenter.CombatTimeRaw - timing) <= TimingWindowSeconds))
            {
                return true;
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;

            if (IsOpenerPotion(timing))
            {
                if (ChurinBRD.OpenerPotionTime == 0f)
                {
                    return IsFirstCycle && InWanderers;
                }
                else
                {
                    return countDown > 0f && countDown <= ChurinBRD.OpenerPotionTime; 
                }
            }
            return false;
        }
    }

    #endregion

    #endregion
}
