using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.4",
    Description =
        "Candles lit, runes drawn upon the floor, sacrifice prepared. Everything is ready for the summoning. I begin the incantation: \"Shakira, Shakira!\"")]
[SourceCode(Path = "main/ExtraRotations/Ranged/ChurinDNC.cs")]
[ExtraRotation]
public sealed class ChurinDNC : DancerRotation
{
    #region Enums

    private enum HoldStrategy
    {
        [Description("Hold Step only if no targets in range")]
        HoldStepOnly,

        [Description("Hold Finish only if no targets in range")]
        HoldFinishOnly,

        [Description("Hold Step and Finish if no targets in range")]
        HoldStepAndFinish,

        [Description("Don't hold Step and Finish if no targets in range")]
        DontHoldStepAndFinish
    }

    private enum DancerOpener
    {
        [Description("Standard Opener")]
        Standard,
        [Description("Tech Opener")]
        Tech,
    }

#endregion

    #region Properties

    #region Constants

    private const int SaberDanceEspritCost = 50;
    private const int HighEspritThreshold = 90;
    private const int MidEspritThreshold = 70;
    private const int DanceTargetRange = 15;

    #endregion

    #region Tracking

    public override void DisplayRotationStatus()
    {
        ImGui.Text($"Weapon Total: {WeaponTotal}");
        ImGui.Text($"Tech Hold Strategy: {TechHoldStrategy}");
        ImGui.Text($"Can Use Step Hold Check for Technical Step: {CanUseStepHoldCheck(TechHoldStrategy)}");
        ImGui.Text($"Standard Hold Strategy: {StandardHoldStrategy}");
        ImGui.Text($"Can Use Step Hold Check for Standard Step: {CanUseStepHoldCheck(StandardHoldStrategy)}");
        ImGui.Text($"Potion Usage Enabled: {PotionUsageEnabled}");
        ImGui.Text($"Potion Usage Presets: {PotionUsagePresets}");
        ImGui.Text($"Can Use Technical Step: {CanUseTechnicalStep} - Tech Step Ready?: {_techStepReady}");
        ImGui.Text($"Can Use Standard Step: {CanUseStandardStep} - Standard Step Ready?: {_standardReady}");
        ImGui.Text($"Saber Dance Primed?: {_saberDancePrimed}");
    }

    #endregion

    #region Status Booleans

    private static bool HasTillana => StatusHelper.PlayerHasStatus(true, StatusID.FlourishingFinish) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingFinish);
    private static bool IsBurstPhase => HasDevilment && HasTechnicalFinish;
    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.Medicated);
    private static bool HasAnyProc => StatusHelper.PlayerHasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry, StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => StatusHelper.PlayerHasStatus(true, StatusID.FinishingMoveReady) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasFlourishingStarfall && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingStarfall);

    private static bool AreDanceTargetsInRange
    {
        get
        {
            return AllHostileTargets.Any(target => target.DistanceToPlayer() <= DanceTargetRange);
        }
    }

    private static bool ShouldSwapDancePartner => CurrentDancePartner != null && (CurrentDancePartner.HasStatus(false, StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911) || CurrentDancePartner.IsDead);

    #endregion

    #region Conditionals

    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled && TechnicalStepPvE.EnoughLevel  && MergedStatus.HasFlag(AutoStatus.Burst);
    private bool ShouldUseStandardStep => StandardStepPvE.IsEnabled && StandardStepPvE.EnoughLevel &&!HasLastDance;
    private bool ShouldUseFinishingMove => FinishingMovePvE.IsEnabled && FinishingMovePvE.EnoughLevel && !HasLastDance;
    private static bool CanWeave => WeaponRemain >= DataCenter.CalculatedActionAhead && DataCenter.DefaultGCDElapsed > 0 && DataCenter.DefaultGCDElapsed >= DataCenter.CalculatedActionAhead;

    private bool StandardSoon => (StandardStepPvE.Cooldown.WillHaveOneCharge(5)
                                  || HasFinishingMove && FinishingMovePvE.Cooldown.WillHaveOneCharge(5)
                                  || StandardStepPvE.Cooldown.HasOneCharge
                                  || HasFinishingMove && FinishingMovePvE.Cooldown.HasOneCharge) && CanUseStandardStep;

    private bool CanUseStandardBasedOnEsprit
    {
        get
        {
            if (!HasTechnicalFinish)
            {
                return Esprit <= HighEspritThreshold || !_saberDancePrimed;
            }

            if (DisableStandardInBurstCheck)
            {
                return Esprit < HighEspritThreshold || !_saberDancePrimed;
            }
            return false;
        }
    }

    private bool DisableStandardInBurstCheck
    {
        get
        {
            if (!HasTechnicalFinish || !DisableStandardInBurst)
            {
                return true;
            }

            return HasFinishingMove || !FinishingMovePvE.EnoughLevel;
        }
    }

    private bool CanUseStepHoldCheck(HoldStrategy strategy)
    {
        var isTech = strategy == TechHoldStrategy;
        var isStandard = strategy == StandardHoldStrategy;

        if (!isTech && !isStandard) return false;

        var shouldHoldStep = isTech
            ? strategy is HoldStrategy.HoldStepOnly && !HasTillana && !HasTechnicalStep
            : strategy is HoldStrategy.HoldStepOnly && !HasStandardStep && !HasFinishingMove;

        var shouldHoldFinish = isTech
            ? strategy is HoldStrategy.HoldFinishOnly && (HasTillana || HasTechnicalStep)
            : strategy is HoldStrategy.HoldFinishOnly && (HasFinishingMove || HasStandardStep);

        return strategy switch
        {
            HoldStrategy.DontHoldStepAndFinish => true,
            HoldStrategy.HoldStepAndFinish => AreDanceTargetsInRange,
            _ when shouldHoldStep || shouldHoldFinish => AreDanceTargetsInRange,
            _ => true,
        };
    }

    private bool _techStepReady;
    private bool _standardReady;

    private bool CanUseTechnicalStep
    {
        get
        {
            var technicalRemain = TechnicalStepPvE.Cooldown.RecastTimeRemain;
            var devilmentRemain = DevilmentPvE.Cooldown.RecastTimeRemain;
            var noFinishBuff = StandardStepPvE.CanUse(out _) && !HasStandardFinish;

            if (!ShouldUseTechStep
                || IsDancing && HasTechnicalStep
                || HasTillana
                || noFinishBuff
                || devilmentRemain - WeaponTotal >= 7f)
            {
                _techStepReady = false;
                return false;
            }

            if (TechnicalStepPvE.Cooldown.IsCoolingDown)
            {
                if (technicalRemain <= WeaponTotal && WeaponElapsed <= 1f)
                {
                    _techStepReady = true;
                }
            }

            if (TechnicalStepPvE.CanUse(out _) && !HasTillana)
            {
                _techStepReady = true;
            }

            return _techStepReady && CanUseStepHoldCheck(TechHoldStrategy);
        }
    }

    private bool CanUseStandardStep
    {
        get
        {
            var standardRemain = StandardStepPvE.Cooldown.RecastTimeRemain;
            var finishingRemain = FinishingMovePvE.Cooldown.RecastTimeRemain;
            var standardDisabled = !ShouldUseStandardStep && !HasFinishingMove;
            var finishingDisabled = !ShouldUseFinishingMove && HasFinishingMove;
            var noFinish = InCombat && HasStandardFinish && ShouldUseTechStep &&
                           TechnicalStepPvE.Cooldown.WillHaveOneCharge(5) && !HasTillana;

            if (IsDancing
                || standardDisabled
                || finishingDisabled
                || noFinish
                || !CanUseStandardBasedOnEsprit)
            {
                _standardReady = false;
                return false;
            }

            if (!HasFinishingMove && StandardStepPvE.Cooldown.IsCoolingDown
                || HasFinishingMove && FinishingMovePvE.Cooldown.IsCoolingDown)
            {
                if ((standardRemain <= WeaponTotal || finishingRemain <= WeaponTotal)  && (WeaponElapsed <= 0.5f || WeaponRemain >= 2f))
                {
                    _standardReady = true;
                }
            }

            if (!HasFinishingMove && StandardStepPvE.CanUse(out _)
                || HasFinishingMove && FinishingMovePvE.CanUse(out _))
            {
                _standardReady = true;
            }

            return _standardReady && CanUseStepHoldCheck(StandardHoldStrategy);
        }
    }

    private bool _saberDancePrimed;

    private void IsSaberDancePrimed()
    {
        var willHaveOneCharge = StandardStepPvE.Cooldown.WillHaveOneCharge(5);

        if ((IsLastGCD(ActionID.SaberDancePvE, ActionID.DanceOfTheDawnPvE)
        && Esprit < SaberDanceEspritCost)
        || Esprit < SaberDanceEspritCost)
        {
            _saberDancePrimed = false;
            return;
        }

        if (WeaponRemain < DataCenter.CalculatedActionAhead) return;

        if (IsBurstPhase)
        {
            if (willHaveOneCharge)
            {
                if (HasLastDance)
                {
                    _saberDancePrimed = Esprit >= HighEspritThreshold;
                    return;
                }

                if (StandardStepPvE.Cooldown.RecastTimeRemain < WeaponTotal)
                {
                    _saberDancePrimed = Esprit >= HighEspritThreshold && !HasLastDance;
                    return;
                }

                _saberDancePrimed = Esprit >= SaberDanceEspritCost
                                    && !StatusHelper.PlayerWillStatusEnd(7f, true, StatusID.FlourishingStarfall);
                return;
            }

            if (Esprit >= SaberDanceEspritCost)
            {
                _saberDancePrimed = true;
                return;
            }

            _saberDancePrimed = false;
            return;
        }

        if (Esprit >= MidEspritThreshold || IsMedicated && Esprit >= SaberDanceEspritCost)
        {
            _saberDancePrimed = true;
            return;
        }

        _saberDancePrimed = false;
    }

    #endregion

    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Technical Step, Technical Finish & Tillana Hold Strategy")]
    private HoldStrategy TechHoldStrategy { get; set; } = HoldStrategy.HoldStepAndFinish;

    [RotationConfig(CombatType.PvE, Name = "Standard Step, Standard Finish & Finishing Move Hold Strategy")]
    private HoldStrategy StandardHoldStrategy { get; set; } = HoldStrategy.HoldStepAndFinish;

    [RotationConfig(CombatType.PvE, Name = "Select an opener")]
    private DancerOpener ChosenOpener { get; set; } = DancerOpener.Standard;

    [Range(0,16, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Step?",
        Parent = nameof(ChosenOpener), ParentValue = "Standard Opener")]
    private float OpenerStandardStepTime { get; set; } = 15.5f;

    [Range(0, 1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Finish?",
        Parent = nameof(ChosenOpener), ParentValue = "Standard Opener")]
    private float OpenerFinishTime { get; set; } = 0.5f;

    [Range(0, 16, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Technical Step?",
        Parent = nameof(ChosenOpener), ParentValue = "Tech Opener", Tooltip = "If countdown is set above 13 seconds, it will start with Standard Step before initiating Tech Step, please go out of range of any enemies before the cd reaches your configured time")]
    private float OpenerTechTime { get; set; } = 7f;

    [Range(0, 1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Technical Finish?",
        Parent = nameof(ChosenOpener), ParentValue = "Tech Opener")]
    private float OpenerTechFinishTime { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Disable Standard Step in Burst")]
    private bool DisableStandardInBurst { get; set; } = true;

    private static readonly ChurinDNCPotions ChurinPotions = new();

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    { get => ChurinPotions.Enabled; set => ChurinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    { get => ChurinPotions.Strategy; set => ChurinPotions.Strategy = value; }

    [Range(0,20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => ChurinPotions.OpenerPotionTime; set => ChurinPotions.OpenerPotionTime = value; }

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

    #endregion

    #region Main Combat Logic

    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        if (ChurinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }
        if (remainTime > OpenerStandardStepTime)
        {
            return base.CountDownAction(remainTime);
        }
        if (ChosenOpener == DancerOpener.Standard)
        {
            if (TryUseClosedPosition(out var act)
             || remainTime <= OpenerStandardStepTime && StandardStepPvE.CanUse(out act)
             || ExecuteStepGCD(out act)
             || remainTime <= OpenerFinishTime && DoubleStandardFinishPvE.CanUse(out act))
            {
                return act;
            }
        }

        if (ChosenOpener == DancerOpener.Tech)
        {
            if (TryUseClosedPosition(out var act)
                || remainTime > OpenerTechTime && remainTime > 13 && StandardStepPvE.CanUse(out act)
                || remainTime <= OpenerTechTime && TechnicalStepPvE.CanUse(out act)
                || ExecuteStepGCD(out act)
                || remainTime > OpenerTechTime && IsDancing && HasStandardStep && !AreDanceTargetsInRange &&
                DoubleStandardFinishPvE.CanUse(out act)
                || remainTime <= OpenerTechFinishTime && TryFinishTheDance(out act))
            {
                return act;
            }
        }

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        IsSaberDancePrimed();
        if (TryUseDevilment(out act)) return true;
        if (ChurinPotions.ShouldUsePotion(this, out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;

        if (CanUseStandardStep || CanUseTechnicalStep || IsDancing)
        {
            act = null;
            return false;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (IsDancing || !CanWeave) return false;
        if (TryUseFlourish(out act)) return true;
        return TryUseFeathers(out act)
               || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (IsDancing)
        {
            return TryFinishTheDance(out act);
        }

        if (TryUseStep(out act))
        {
            return true;
        }

        // During burst phase, prioritize burst GCDs
        if (IsBurstPhase && TryUseBurstGCD(out act))
        {
            return true;
        }

        return TryUseFillerGCD(out act) || base.GeneralGCD(out act);
    }

    #endregion

    #endregion

    #region Extra Methods

    #region Dance Partner Logic

    private bool TryUseClosedPosition(out IAction? act)
    {
        act = null;

        // Already have a dance partner or no party members
        if (StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition)
            || !PartyMembers.Any()
            || !ClosedPositionPvE.IsEnabled)
        {
            return false;
        }

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        act = null;
        if (!StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition)
        || !ShouldSwapDancePartner
        || !ClosedPositionPvE.IsEnabled)
        {
            return false;
        }

        if ((StandardStepPvE.Cooldown.WillHaveOneCharge(3f)
        || FinishingMovePvE.Cooldown.WillHaveOneCharge(3f)
        || TechnicalStepPvE.Cooldown.WillHaveOneCharge(3f))
        && ShouldSwapDancePartner)
        {
            return EndingPvE.CanUse(out act);
        }
        return false;
    }

    #endregion

    #region Dance Logic

    private bool TryUseStep(out IAction? act)
    {
        act = null;
        if (IsDancing) return false;

        if (CanUseTechnicalStep)
        {
            act = TechnicalStepPvE;
            return true;
        }


        switch (CanUseStandardStep)
        {
            case true when !HasFinishingMove:
                act = StandardStepPvE;
                return true;

            case true when HasFinishingMove:
                act = FinishingMovePvE;
                return true;
        }

        return false;
    }

    private bool TryFinishStandard(out IAction? act)
    {
        act = null;
        if (!HasStandardStep || HasFinishingMove || !IsDancing) return false;

        if (CompletedSteps < 2) return ExecuteStepGCD(out act);

        var shouldFinish = HasStandardStep && CompletedSteps == 2 && CanUseStepHoldCheck(StandardHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.StandardStep);

        return (shouldFinish || aboutToTimeOut) && DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true);
    }

    private bool TryFinishTech(out IAction? act)
    {
        act = null;
        if (!HasTechnicalStep || HasTillana || !IsDancing) return false;

        if (CompletedSteps < 4) return ExecuteStepGCD(out act);

        var shouldFinish = HasTechnicalStep && CompletedSteps == 4 && CanUseStepHoldCheck(TechHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.TechnicalStep);

        return (shouldFinish || aboutToTimeOut) && QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true);
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        act = null;
        if (!IsDancing || HasFinishingMove || HasTillana) return false;

        return TryFinishStandard(out act) || TryFinishTech(out act);
    }

    #endregion

    #region Burst Logic

    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;
        if (TryUseStep(out act)) return true;

        if (TryUseTillana(out act)) return true;

        if (TryUseDanceOfTheDawn(out act)) return true;

        if (TryUseLastDance(out act)) return true;

        if (TryUseStarfallDance(out act)) return true;

        return TryUseSaberDance(out act) || TryUseFillerGCD(out act);
    }

    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        act = null;
        if (Esprit < SaberDanceEspritCost
            || !StatusHelper.PlayerHasStatus(true, StatusID.DanceOfTheDawnReady)
            || StandardStepPvE.Cooldown.WillHaveOneCharge(7.5f) && HasLastDance && Esprit < HighEspritThreshold) return false;

        return DanceOfTheDawnPvE.CanUse(out act);
    }

    private bool TryUseTillana(out IAction? act)
    {
        act = null;

        if (!HasTillana
            || Esprit >= SaberDanceEspritCost)
        {
            return false;
        }

        var gcdsUntilStandard = 0;
        for (uint i = 1; i <= 5; i++)
        {
            if (StandardStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
            {
                gcdsUntilStandard = (int)i;
                break;
            }
        }

        if (TillanaPvE.CanUse(out act))
        {
            switch (gcdsUntilStandard)
            {
                case 5:
                case 4:
                case 3:
                    if (Esprit < 20) return true;
                    if (!HasLastDance) return Esprit < SaberDanceEspritCost;
                    break;
                case 2:
                case 1:
                    return Esprit < 10 && !HasLastDance;
            }

        }

        return Esprit < SaberDanceEspritCost && TillanaPvE.CanUse(out act);
    }

    private bool ShouldUseLastDance
    {
        get
        {
            var lastDanceEndingSoon = StatusHelper.PlayerWillStatusEnd(5, true, StatusID.LastDanceReady);
            var standardSoonish = StandardStepPvE.Cooldown.WillHaveOneCharge(10);

            if (lastDanceEndingSoon)
            {
                return true;
            }

            if (IsBurstPhase)
            {
                if (standardSoonish)
                {
                    if (HasTillana && Esprit >= 20 || !TryUseDanceOfTheDawn(out _))
                    {
                        return Esprit < HighEspritThreshold || !_saberDancePrimed;
                    }
                }
                else
                {
                    if (!HasStarfall && (Esprit < SaberDanceEspritCost || !_saberDancePrimed))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Esprit < MidEspritThreshold
                    && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15f))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance) return false;

        return LastDancePvE.CanUse(out act) && ShouldUseLastDance;
    }

    private bool ShouldUseStarfallDance
    {
        get
        {
            var willHaveOneCharge = StandardStepPvE.Cooldown.WillHaveOneCharge(5);

            if (StatusHelper.PlayerWillStatusEnd(7f, true, StatusID.FlourishingStarfall))
            {
                return true;
            }

            if (HasLastDance && willHaveOneCharge
                || Esprit >= HighEspritThreshold || _saberDancePrimed)
            {
                return false;
            }

            return Esprit < SaberDanceEspritCost || !_saberDancePrimed;
        }
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall || CanUseStandardStep) return false;

        return ShouldUseStarfallDance && StarfallDancePvE.CanUse(out act);
    }

    #endregion

    #region GCD Skills

    private bool TryUseFillerGCD(out IAction? act)
    {
        act = null;
        if (TryUseStep(out act)) return true;
        if (TryUseSaberDance(out act)) return true;
        if (TryUseTillana(out act)) return true;
        if (TryUseProcs(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        return TryUseLastDance(out act) || TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        act = null;
        if (TryUseStep(out act)) return true;
        if (IsBurstPhase && !HasLastDance && Esprit >= SaberDanceEspritCost
            || IsMedicated && Esprit >= SaberDanceEspritCost) return SaberDancePvE.CanUse(out act);

        if (Esprit > HighEspritThreshold) return false;
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        return WindmillPvE.CanUse(out act) || CascadePvE.CanUse(out act);
    }

    private bool TryUseFeatherGCD(out IAction? act)
    {
        act = null;
        if (Feathers < 4 || CanUseStandardStep || CanUseTechnicalStep || IsDancing ) return false;

        var hasSilkenProcs = HasSilkenFlow || HasSilkenSymmetry;
        var hasFlourishingProcs = HasFlourishingFlow || HasFlourishingSymmetry;

        if (Feathers > 3 && !hasSilkenProcs && hasFlourishingProcs && Esprit < SaberDanceEspritCost && !IsBurstPhase)
        {
            if (FountainPvE.CanUse(out act)) return true;
            if (CascadePvE.CanUse(out act)) return true;
        }

        if (Feathers > 3 && (hasSilkenProcs || hasFlourishingProcs) && Esprit > SaberDanceEspritCost)
        {
            return SaberDancePvE.CanUse(out act);
        }

        return false;
    }


    private bool TryUseSaberDance(out IAction? act)
    {
        act = null;
        var willHaveOneCharge = StandardStepPvE.Cooldown.WillHaveOneCharge(5);

        // Need at least 50 Esprit to use Saber Dance
        if (Esprit < SaberDanceEspritCost) return false;

        // Don't use if Technical Step is ready (prioritize starting Tech)
        if (CanUseTechnicalStep || IsDancing) return false;

        if (!SaberDancePvE.CanUse(out act) || !_saberDancePrimed)
        {
            return false;
        }

        if (!IsBurstPhase)
        {
            if (IsMedicated)
            {
                return Esprit >= SaberDanceEspritCost;
            }
            return Esprit >= MidEspritThreshold;
        }

        if (!willHaveOneCharge)
        {
            return Esprit >= SaberDanceEspritCost;
        }

        if (HasLastDance)
        {
            return Esprit >= HighEspritThreshold;
        }

        return false;

    }

    private bool TryUseProcs(out IAction? act)
    {
        act = null;
        if (IsBurstPhase || !ShouldUseTechStep || CanUseStandardStep || CanUseTechnicalStep || IsDancing) return false;

        var gcdsUntilTech = 0;
        for (uint i = 1; i <= 5; i++)
        {
            if (TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f))
            {
                gcdsUntilTech = (int)i;
                break;
            }
        }

        if (gcdsUntilTech is 0 or > 5 ) return false;

        switch (gcdsUntilTech)
        {
            case 5:
            case 4:
                if (!HasAnyProc || Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (Esprit >= HighEspritThreshold) return SaberDancePvE.CanUse(out act);
                break;
            case 3:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                return FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act);
            case 2:
                if (Esprit >= SaberDanceEspritCost && !HasAnyProc) return SaberDancePvE.CanUse(out act);
                if (Esprit < SaberDanceEspritCost) return TryUseBasicGCD(out act);
                break;
            case 1:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);
                if (!HasAnyProc && Esprit < SaberDanceEspritCost && FountainPvE.CanUse(out act)) return true;
                if (!HasAnyProc && Esprit >= SaberDanceEspritCost) return SaberDancePvE.CanUse(out act);
                if (!HasAnyProc && Esprit < SaberDanceEspritCost) return LastDancePvE.CanUse(out act);
                break;
        }
        return false;
    }

    #endregion

    #region OGCD Abilities

    private bool TryUseDevilment(out IAction? act)
    {
        act = null;
        if (IsDancing || !DevilmentPvE.EnoughLevel || DevilmentPvE.Cooldown.IsCoolingDown) return false;

        if (HasTechnicalFinish
            || IsLastGCD(true, QuadrupleTechnicalFinishPvE)
            || HasTillana
            || !TechnicalStepPvE.EnoughLevel && IsLastGCD(true, DoubleStandardFinishPvE))
        {
            act = DevilmentPvE;
            return true;
        }
        return false;
    }

    private bool TryUseFlourish(out IAction? act)
    {
        act = null;
        if (!InCombat || HasThreefoldFanDance || !FlourishPvE.IsEnabled || !FlourishPvE.EnoughLevel) return false;

        if (FlourishPvE.CanUse(out act))
        {
            if (IsBurstPhase)
            {
                return true;
            }

            switch (ShouldUseTechStep)
            {
                case true when TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15):
                case false:
                    return true;
            }
        }
        return false;
    }

    private bool TryUseFeathers(out IAction? act)
    {
        act = null;
        var hasEnoughFeathers = Feathers > 3;

        if (hasEnoughFeathers && (HasAnyProc || FlourishPvE.Cooldown.WillHaveOneCharge(3)))
        {
            if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        if (HasFourfoldFanDance && FanDanceIvPvE.CanUse(out act)) return true;
        if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;

        if (IsBurstPhase || (hasEnoughFeathers && HasAnyProc && !CanUseTechnicalStep) || IsMedicated && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(10))
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }
        return false;
    }

    #endregion

    #endregion

    /// <summary>
    /// DNC-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinDNCPotions : Potions
    {
        public override bool IsConditionMet()
        {
            var churinDNC = new ChurinDNC();

            if (CompletedSteps <= 0) return false;

            // Check for Technical Step completion (4+ steps) or Standard Step completion (2+ steps)
            return (HasTechnicalStep && CompletedSteps > 3)
                   || (((InCombat && !churinDNC.TechnicalStepPvE.Cooldown.WillHaveOneCharge(30))
                        || !InCombat) && HasStandardStep && CompletedSteps > 1);
        }

        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && DataCenter.CombatTimeRaw >= timing &&
                DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds) return true;

            // Check opener timing: if it's an opener potion and countdown is within configured time
            var countDown = Service.CountDownTime;
            return IsOpenerPotion(timing) && countDown > 0 && countDown <= ChurinDNC.OpenerPotionTime;
        }
    }

}
