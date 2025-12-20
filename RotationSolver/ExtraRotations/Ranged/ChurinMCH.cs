using System.ComponentModel;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin MCH", CombatType.PvE, GameVersion = "7.3", Description = "Kill it with kindness. And if that fails, kill it with sharp sticks or knives...or guns!")]
[SourceCode(Path = "ArgentiRotations/Ranged/Machinist/ChurinMCH.cs")]
[ExtraRotation]
public sealed class ChurinMCH: MachinistRotation
{
    #region Properties
    #region Boolean Properties
    #region Potion Booleans
    private bool InTwoMinuteWindow => WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.WillHaveOneChargeGCD(5) &&
                                       AirAnchorPvE.Cooldown.IsCoolingDown && AirAnchorPvE.Cooldown.WillHaveOneChargeGCD(1);
    private bool InOddMinuteWindow => !InTwoMinuteWindow && AirAnchorPvE.Cooldown.IsCoolingDown && AirAnchorPvE.Cooldown.WillHaveOneChargeGCD(1);

    #endregion
    #region Status Booleans

    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true,StatusID.Medicated) ||
                                       !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.Medicated);

    #endregion
    #region  Logic Booleans

    private static bool CanLateWeave => WeaponRemain <= LateWeaveWindow && EnoughWeaveTime;
    private static bool EnoughWeaveTime => WeaponRemain >= DefaultAnimationLock;
    private bool BurstSoon => WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.WillHaveOneCharge(40);
    private bool InFiller => !AreToolsComingOffCooldown() && !BurstSoon && WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.ElapsedAfter(30);
    //private bool InRebuildPhase => (CombatTime % 120 >= 70 && CombatTime % 120 <= 90) && !InBurstPrepPhase;

    private static bool CanDoubleHypercharge => HasHypercharged && Heat >= 50;

    #endregion
    #endregion
    #region Other Properties
    private const float TimeToNextTool = 8;
    private static double RecastTime => ActionManager.GetAdjustedRecastTime(ActionType.Action, (uint)ActionID.HeatedCleanShotPvE, false) / 1000.00;

    private const float DefaultAnimationLock = 0.6f;
    private static float LateWeaveWindow => (float)(RecastTime * 0.5f);
    #endregion

    #region Potion Properties
    private readonly ChurinMCHPotions _churinPotions = new();

    private float _firstPotionTiming = 0;
    private float _secondPotionTiming = 0;
    private float _thirdPotionTiming = 0;

    private void UpdateCustomTimings()
    {
        _churinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }

    #endregion



    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Use Bioblaster while moving")]
    private bool BioMove { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Only use Wildfire on Boss targets")]
    private bool WildfireBoss { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private bool PotionUsageEnabled
    { get => _churinPotions.Enabled; set => _churinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private PotionStrategy PotionUsagePresets
    { get => _churinPotions.Strategy; set => _churinPotions.Strategy = value; }

    [Range(0,20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)", Parent = nameof(PotionUsageEnabled))]
    private float OpenerPotionTime { get => _churinPotions.OpenerPotionTime; set => _churinPotions.OpenerPotionTime = value; }

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

    #endregion

    #region Main Combat Logic

    #region Countdown logic
    protected override IAction? CountDownAction(float remainTime)
    {
        ResetQueenTracking();
        if (_churinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }
        if (remainTime <= 5 && ReassemblePvE.CanUse(out var act, usedUp: false) ||
            remainTime <= 0.1f && AirAnchorPvE.CanUse(out act))
        {
            return act;
        }        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (_churinPotions.ShouldUsePotion(this, out act)) return true;
        if (InCombat)
        {
            UpdateQueenStep();
            UpdateQueenStepPair();
        }

        return TryUseQueen(out act) ||
                TryUseWildfire(out act) ||
                TryUseHypercharge(out act) ||
                base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (TryUseReassemble(nextGCD, out act)) return true;
        if (TryUsePingPong(out act)) return true;
        return TryUseBarrelStabilizer(out act) || base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (TryUseTools(out act)) return true;
        if (TryUseFullMetalField(out act)) return true;
        if (OverheatedStacks > 0 && TryUseOverheatedGCD(out act)) return true;
        if (TryUseCombo(out act)) return true;
        if (TryUseAOEGCDs(out act)) return true;
        return TryUseFillerGCD(out act) || base.GeneralGCD(out act);
    }
    #endregion

    #endregion

    #region Extra Methods
    #region GCD Skills

    #region MultiTool
    private bool TryUseTools(out IAction? act)
    {
        act = null;
        if (HasOverheated || OverheatedStacks >= 1) return false;

        if (TryUseAirAnchor(out act)) return true;
        if (TryUseDrill(out act)) return true;
        return TryUseChainSaw(out act) ||
               TryUseExcavator(out act);
    }

    private bool TryUseDrill(out IAction? act)
    {
        var drillHasMaxCharges = DrillPvE.Cooldown.CurrentCharges == 2;
        var drillAfterExcavator = !HasExcavatorReady && !IsLastGCD(ActionID.DrillPvE);
        var justUseDrill = WildfirePvE.Cooldown.IsCoolingDown && !WildfirePvE.Cooldown.WillHaveOneChargeGCD(2) && !CanDoubleHypercharge;
        switch (EnhancedMultiweaponTrait.EnoughLevel)
        {
            case true when !CombatElapsedLess(340) && DrillPvE.CanUse(out act, usedUp: false) && WildfirePvE.Cooldown.IsCoolingDown &&
                AirAnchorPvE.Cooldown.WillHaveOneCharge(25):
            case true when !CombatElapsedLess(360) && WildfirePvE.Cooldown.IsCoolingDown && OverheatedStacks < 1 &&
                           DrillPvE.CanUse(out act, usedUp:true):
            case true when drillHasMaxCharges && DrillPvE.CanUse(out act, usedUp: false):
            case true when drillAfterExcavator && DrillPvE.CanUse(out act, usedUp: true):
            case true when justUseDrill && DrillPvE.CanUse(out act, usedUp: true):
            case false when DrillPvE.CanUse(out act) && !HasHypercharged:
                return true;
            default:
                act = null;
                return false;
        }
    }

    private bool TryUseAirAnchor(out IAction? act)
    {
        act = null;
        if (HotShotMasteryTrait.EnoughLevel && AirAnchorPvE.CanUse(out act)) return true;
        return !AirAnchorPvE.EnoughLevel && HotShotPvE.CanUse(out act);
    }

    private bool TryUseChainSaw(out IAction? act)
    {
        act = null;
        return ChainSawPvE.CanUse(out act);
    }

    private bool TryUseExcavator(out IAction? act)
    {
        act = null;

        if ((WildfirePvE.Cooldown.HasOneCharge ||
             WildfirePvE.Cooldown.WillHaveOneChargeGCD(1)) && ExcavatorPvE.CanUse(out act)) return true;

        byte nextTargetBattery = 0;
        if (_queenStep < _queenStepPairs.Length)
        {
            nextTargetBattery = _queenStepPairs[_queenStep].to;
        }

        return nextTargetBattery switch
        {
            90 when HasExcavatorReady && Battery <= 70 && ExcavatorPvE.CanUse(out act) =>true,
            90 when HasExcavatorReady && Battery >= 80 && ExcavatorPvE.CanUse(out act) => false,
            80 when HasExcavatorReady && Battery >= 70 && ExcavatorPvE.CanUse(out act)=> false,
            80 when HasExcavatorReady && Battery <= 60 && ExcavatorPvE.CanUse(out act) => true,
            60 when HasExcavatorReady && Battery >= 50 && ExcavatorPvE.CanUse(out act)=> false,
            60 when HasExcavatorReady && Battery <= 40  && ExcavatorPvE.CanUse(out act)=> true,
            _ => HasExcavatorReady && ExcavatorPvE.CanUse (out act)
        };
    }

    #endregion

    #region Other GCDs
    private bool TryUseFullMetalField(out IAction? act)
    {
        if (AirAnchorPvE.Cooldown.IsCoolingDown &&
            DrillPvE.Cooldown.CurrentCharges < 2 &&
            ChainSawPvE.Cooldown.IsCoolingDown
            && !HasExcavatorReady)
        {
            if (!WildfirePvE.Cooldown.IsCoolingDown || IsLastAbility(true, WildfirePvE) ||
                WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.WillHaveOneChargeGCD(1))
            {
                if (FullMetalFieldPvE.CanUse(out act)) return true;
            }
        }

        return SetActToNull(out act);
    }
    private bool TryUseCombo(out IAction? act)
    {
        if (IsLastComboAction(true, SlugShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            if (CleanShotPvE.CanUse(out act)) return true;
        }

        if (IsLastComboAction(true, SplitShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
        {
            if (SlugShotPvE.CanUse(out act)) return true;
        }

        return SetActToNull(out act);
    }
    private bool TryUseOverheatedGCD(out IAction? act)
    {
        if (!IsOverheated || OverheatedStacks < 1 ) return SetActToNull(out act);

        return AutoCrossbowPvE.CanUse(out act) ||
               HeatBlastPvE.CanUse(out act);
    }
    private bool TryUseAOEGCDs(out IAction? act)
    {
        if (IsOverheated || OverheatedStacks > 0) return SetActToNull(out act);
        if ((BioMove || (!IsMoving && !BioMove)) && BioblasterPvE.CanUse(out act, usedUp: true)) return true;
        return SpreadShotPvE.CanUse(out act);
    }
    private bool TryUseFillerGCD(out IAction? act)
    {
        if (IsOverheated || OverheatedStacks > 0) return SetActToNull(out act);
        return HeatedCleanShotPvE.CanUse(out act) ||
               HeatedSlugShotPvE.CanUse(out act) ||
               HeatedSplitShotPvE.CanUse(out act) ||
               base.GeneralGCD(out act);
    }

    #endregion

    #endregion
    #region oGCD Abilities
    #region Queen Logic
    // Step-based Queen battery transitions: (from, to, step)
    private readonly (byte from, byte to, int step)[] _queenStepPairs =
    [
        (0, 60, 0),    // Opener: 60 after Excavator
        (60, 90, 1),   // Next: 90
        (90, 100, 2),  // 100
        (100, 50, 3),  // 50
        (50, 60, 4),   // 60
        (60, 100, 5),  // 100
        (100, 50, 6),  // 50
        (50, 70, 7),   // 70
        (80, 100, 8),  // 100
        (100, 50, 9),  // 50
        (50, 80, 10),  // 80
        (70, 100, 11), // 100
        (100, 50, 12), // 50
        (50, 60, 13)   // 60
    ];
    private int _queenStep;
    private byte _lastTrackedQueenBattery;
    private byte _lastOddQueenBattery;
    private byte _nextOddQueenBattery;
    private bool _foundQueenStepPair;
    private void UpdateQueenStepPair()
    {
        if (_queenStep < _queenStepPairs.Length)
        {
            var (from, to, _) = _queenStepPairs[_queenStep];

            _foundQueenStepPair = LastSummonBatteryPower == from && Battery == to;

            if (LastSummonBatteryPower == 50)
            {
                switch (to)
                {
                 case 60:
                     _lastOddQueenBattery = 0;
                     _nextOddQueenBattery = 70;
                     break;
                 case 70:
                     _lastOddQueenBattery = 60;
                     _nextOddQueenBattery = 80;
                     break;
                 case 80:
                     _lastOddQueenBattery = 70;
                     _nextOddQueenBattery = 60;
                     break;

                }
            }
        }
        else
        {
            _foundQueenStepPair = false;
        }
    }
    private void UpdateQueenStep()
    {
        if (_lastTrackedQueenBattery != LastSummonBatteryPower)
        {
            _lastTrackedQueenBattery = LastSummonBatteryPower;
            _queenStep++;
        }
    }
    private void ResetQueenTracking()
    {
        _queenStep = 0;
        _lastTrackedQueenBattery = 0;
        _lastOddQueenBattery = 0;
        _nextOddQueenBattery = 0;
        _foundQueenStepPair = false;
    }
    private bool TryUseQueen(out IAction? act)
    {
        act = null;
        if (!InCombat || IsRobotActive)
        {
            return SetActToNull(out act);
        }

        if (Battery == 60 && IsLastGCD(ActionID.ExcavatorPvE) &&
            CombatTime < 15 && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
        {
            return true;
        }

        switch (_foundQueenStepPair)
        {
            case true when InCombat && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true):
                return true;
            // Fallback in case the step tracking fails
            case false when InCombat:
            {
                if (LastSummonBatteryPower == 50 && Battery > _nextOddQueenBattery && Battery > _lastOddQueenBattery ||
                    Battery == 100 && LastSummonBatteryPower is 60 or 70 or 80 or 90 ||
                    LastSummonBatteryPower == 100 && Battery == 50)
                {
                    if (RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
                    {
                        return true;
                    }
                }

                break;
            }
        }

        return SetActToNull(out act);

    }
    #endregion
    #region Hypercharge Logic

    // Core hypercharge decision logic
    private bool TryUseHypercharge(out IAction? act)
    {
        act = null;
        // Determine whether to use Hypercharge based on priority conditions
        return ShouldUseHypercharge() && HyperchargePvE.CanUse(out act);
    }

    private bool ShouldUseHypercharge()
    {
        if (AreToolsComingOffCooldown()) return false;

        return IsBurstWindow() ||
               IsOptimalHeatThreshold() ||
               IsHeatOvercapRisk();
    }

    private bool IsBurstWindow()
    {
        // Full Metal Field + Wildfire burst window
        if (CombatTime < 20 && IsLastGCD(ActionID.FullMetalFieldPvE) && CanLateWeave)
        {
            return true;
        }

        if (Heat >= 85 && HasHypercharged && (IsLastGCD(ActionID.ExcavatorPvE) ||
                                              !ChainSawPvE.Cooldown.HasOneCharge || ExcavatorPvE.Cooldown.JustUsedAfter(WeaponTotal/ 2) && !HasExcavatorReady) && CanLateWeave)
        {
            return true;
        }

        if (IsLastGCD(ActionID.FullMetalFieldPvE))
        {
            return true;
        }

        if (IsLastGCD(ActionID.BlazingShotPvE) && Heat >= 50 && WildfirePvE.Cooldown.IsCoolingDown && !WildfirePvE.Cooldown.ElapsedAfter(20) )
        {
            return true;
        }

        // 2-minute burst window detection
        return HasWildfire || IsLastAbility(ActionID.WildfirePvE);
    }

    private static bool IsHeatOvercapRisk()
    {
        return Heat >= 95;
    }

    private bool IsOptimalHeatThreshold()
    {
        if (CombatElapsedLess(60) && Heat >= 50) return true;
        if (BurstSoon && Heat >= 50) return false;
        return InFiller && Heat >= 50;         // Filler phase
        //return InRebuildPhase && Heat >= 50; // Rebuild phase
    }

    private bool AreToolsComingOffCooldown()
    {
        // Don't Hypercharge if major tools are about to come off cooldown
        return DrillPvE.Cooldown.CurrentCharges == 1 && DrillPvE.Cooldown.WillHaveXCharges(2, TimeToNextTool) ||
               AirAnchorPvE.Cooldown.WillHaveOneCharge(TimeToNextTool) || AirAnchorPvE.CanUse(out _) ||
               ChainSawPvE.Cooldown.WillHaveOneCharge(TimeToNextTool) || ChainSawPvE.CanUse(out _) ||
               (HasExcavatorReady && ExcavatorPvE.CanUse(out _));
    }



    #endregion
    #region Other oGCDs
    private bool TryUseWildfire(out IAction? act)
    {
        act = null;

        // Check if we want to limit Wildfire to boss targets only
        if (WildfireBoss)
        {
            var isTargetBoss = WildfirePvE.Target.Target?.IsBossFromIcon() ?? false;
            if (!isTargetBoss)
            {
                return false;
            }
        }

        // Level 100+ rotation: Use after tools are spent
        if (FullMetalFieldPvE.EnoughLevel)
        {
            if (IsLastGCD(ActionID.FullMetalFieldPvE) || !HasFullMetalMachinist && BarrelStabilizerPvE.Cooldown.IsCoolingDown ||
                IsLastGCD(ActionID.DrillPvE) && DrillPvE.Cooldown.CurrentCharges == 0 && HasFullMetalMachinist)
            {
                return WildfirePvE.CanUse(out act) && CanLateWeave;
            }
        }
        // Pre-100 rotation
        else
        {
            // Use during Hypercharge
            if (IsOverheated && WildfirePvE.CanUse(out act))
                return true;

            // Use when about to Hypercharge
            if (Heat >= 45 && HyperchargePvE.CanUse(out _) && CanLateWeave && WildfirePvE.CanUse(out act))
                return true;
        }

        return false;
    }
    private bool TryUseReassemble(IAction nextGCD, out IAction? act)
    {
        if (HasReassembled || WildfirePvE.Cooldown.IsCoolingDown && WildfirePvE.Cooldown.WillHaveOneCharge(30) &&
            !WildfirePvE.Cooldown.WillHaveOneCharge(10))
        {
            return SetActToNull(out act);
        }

        var isReassembleTarget =
                ReassemblePvE.Cooldown.CurrentCharges > 0 && !HasReassembled &&
                nextGCD.IsTheSameTo(true, ExcavatorPvE) && HasExcavatorReady ||
                IsLastGCD(true,ChainSawPvE) && HasExcavatorReady ||
                ChainSawPvE.Cooldown.IsCoolingDown && ReassemblePvE.Cooldown.CurrentCharges == 1 && ReassemblePvE.Cooldown.WillHaveXCharges(2, ChainSawPvE.Cooldown.RecastTimeElapsed) &&
                !ChainSawPvE.Cooldown.HasOneCharge && nextGCD.IsTheSameTo(true, DrillPvE) && !CombatElapsedLessGCD(5)||
                IsMedicated && IsLastGCD(ActionID.HeatBlastPvE) && OverheatedStacks < 1 && nextGCD.IsTheSameTo(true, DrillPvE) ||
                !ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(false, AirAnchorPvE);

        // If target is eligible, use Reassemble
        if (isReassembleTarget && ReassemblePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return SetActToNull(out act);
    }
    private bool TryUseBarrelStabilizer(out IAction? act)
    {
        return BarrelStabilizerPvE.CanUse(out act) || SetActToNull(out act);
    }
    private bool TryUseCheckmate(out IAction? act)
    {
        act = null;
        return !CheckmatePvE.Cooldown.IsCoolingDown && CheckmatePvE.CanUse(out act);
    }
    private bool TryUseDoubleCheck(out IAction? act)
    {
        act = null;
        return !DoubleCheckPvE.Cooldown.IsCoolingDown && DoubleCheckPvE.CanUse(out act);
    }
    private bool TryUsePingPong(out IAction? act)
    {
        if (TryUseCheckmate(out act)) return true;
        if (TryUseDoubleCheck(out act)) return true;

        var whichToUse = RicochetPvE.EnoughLevel switch
        {
            true when RicochetPvE.Cooldown.RecastTimeElapsed > GaussRoundPvE.Cooldown.RecastTimeElapsed => "Ricochet",
            true when GaussRoundPvE.Cooldown.RecastTimeElapsed > RicochetPvE.Cooldown.RecastTimeElapsed => "GaussRound",
            true => "Ricochet", // Default to Ricochet if equal
            _ => "GaussRound"
        };

        switch (whichToUse)
        {
            case "Ricochet":
                if (BarrelStabilizerPvE.Cooldown.IsCoolingDown &&
                    (FullMetalFieldPvE.CanUse(out _) || FullMetalFieldPvE.Cooldown.IsCoolingDown) && RicochetPvE.CanUse(out act, usedUp: true))
                    return true;
                break;
            case "GaussRound":
                if (BarrelStabilizerPvE.Cooldown.IsCoolingDown &&
                    (FullMetalFieldPvE.CanUse(out _) || FullMetalFieldPvE.Cooldown.IsCoolingDown) && GaussRoundPvE.CanUse(out act, usedUp: true))
                    return true;
                break;
        }

        return SetActToNull(out act);
    }
    #endregion
    #endregion

    #region Miscellaneous

    private static bool SetActToNull(out IAction? act)
    {
        act = null;
        return false;
    }

    /// <summary>
    /// MCH-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinMCHPotions : Potions
    {
        public override bool IsConditionMet()
        {
            var churinMCH = new ChurinMCH();

            if (churinMCH.InTwoMinuteWindow)
            {
                return true;
            }

            if (churinMCH.InOddMinuteWindow)
            {
                return true;
            }
        
            return false;
        }

        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && DataCenter.CombatTimeRaw >= timing && DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds)
            {
                return true;
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;
            if (IsOpenerPotion(timing) && countDown > 0 && countDown <= OpenerPotionTime)
            {
                return true;
            }

            return false;
        }
    }

    #endregion

    #endregion
}
