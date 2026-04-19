using System.ComponentModel;
using ECommons.GameFunctions;
using CombatRole = ECommons.GameFunctions.CombatRole;

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

    public enum DancerOpener
    {
        [Description("Standard Opener")]
        Standard,
        [Description("Tech Opener")]
        Tech
    }

#endregion

    #region Properties

    #region Constants

    private const int SaberDanceEspritCost = 50;
    private const int RiskyEspritThreshold = 40;
    private const int HighEspritThreshold = 80;
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
        ImGui.Text($"Completed Steps: {CompletedSteps}");
        ImGui.Text($"Potion Condition Met: {ChurinPotions.IsConditionMet()} | Can Use At Time: {ChurinPotions.CanUseAtTime()}");
        ImGui.Text($"Is Burst Phase: {IsBurstPhase}");
        ImGui.Text($"Feathers: {Feathers}");
        ImGui.Text($"Has Any Procs: {HasAnyProc}");
        ImGui.Text($"Has Enough Feathers: {HasEnoughFeathers}");
        ImGui.Text($"CanWeave: {CanWeave}");
        ImGui.Text($"Is Dancing: {IsDancing}");
    }

    #endregion

    #region Status Booleans

    private static bool HasTillana => StatusHelper.PlayerHasStatus(true, StatusID.FlourishingFinish) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingFinish);
    private bool IsBurstPhase => (DevilmentPvE.EnoughLevel && TechnicalStepPvE.EnoughLevel && HasTechnicalFinish && HasDevilment)
                                 || (!TechnicalStepPvE.EnoughLevel && DevilmentPvE.EnoughLevel && HasDevilment && HasStandardFinish);
    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.Medicated);
    private static bool HasAnyProc => StatusHelper.PlayerHasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry, StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);
    private static bool HasFinishingMove => StatusHelper.PlayerHasStatus(true, StatusID.FinishingMoveReady) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasFlourishingStarfall && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingStarfall);
    private static bool HasDanceOfTheDawn => StatusHelper.PlayerHasStatus(true, StatusID.DanceOfTheDawnReady) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.DanceOfTheDawnReady);
    private static bool HasEnoughFeathers => Feathers > 3;
    private static bool IsAttackableBoss => IsInHighEndDuty && CurrentTarget != null && CurrentTarget.IsAttackable() && (CurrentTarget.IsBossFromIcon() || CurrentTarget.IsBossFromTTK());

    private static bool AreDanceTargetsInRange
    {
        get
        {
            return AllHostileTargets.Any(target => target.DistanceToPlayer() <= DanceTargetRange);
        }
    }

    private static readonly StatusID[] HasWeaknessOrDamageDown = [StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911];

    private static bool ShouldSwapDancePartner => CurrentDancePartner != null
                                                  && (CurrentDancePartner.HasStatus(false, HasWeaknessOrDamageDown)
                                                      || CurrentDancePartner.IsDead);

    #endregion

    #region Conditionals

    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled && TechnicalStepPvE.EnoughLevel  && MergedStatus.HasFlag(AutoStatus.Burst);
    private bool ShouldUseStandardStep => StandardStepPvE.IsEnabled && StandardStepPvE.EnoughLevel &&!HasLastDance;
    private bool ShouldUseFinishingMove => FinishingMovePvE.IsEnabled && FinishingMovePvE.EnoughLevel && !HasLastDance;

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
            if (!HasTechnicalFinish
                || (!DisableStandardInBurst && HasTechnicalFinish))
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
            ? strategy is HoldStrategy.HoldFinishOnly && ((HasTillana && TillanaPvE.EnoughLevel) || HasTechnicalStep)
            : strategy is HoldStrategy.HoldFinishOnly && ((HasFinishingMove && FinishingMovePvE.EnoughLevel) || HasStandardStep);

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
            var noFinishBuff = (StandardStepPvE.CanUse(out _) || (HasFinishingMove && FinishingMovePvE.CanUse(out _))) && !HasStandardFinish;

            if (!ShouldUseTechStep
                || IsDancing && HasTechnicalStep
                || HasTillana
                || noFinishBuff
                || devilmentRemain - WeaponTotal > 7f)
            {
                _techStepReady = false;
                return false;
            }

            if (TechnicalStepPvE.Cooldown.IsCoolingDown)
            {
                if (technicalRemain <= WeaponTotal && (WeaponElapsed <= 1f || WeaponRemain >= 2f))
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
            var burstSoon = InCombat && HasStandardFinish && CanUseTechnicalStep && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5);

            if (IsDancing
                || standardDisabled
                || finishingDisabled
                || !CanUseStandardBasedOnEsprit
                || burstSoon)
            {
                _standardReady = false;
                return false;
            }

            if (((!HasFinishingMove || !FinishingMovePvE.EnoughLevel) && StandardStepPvE.Cooldown.IsCoolingDown)
                || (HasFinishingMove && FinishingMovePvE.Cooldown.IsCoolingDown))
            {
                if ((standardRemain <= WeaponTotal || finishingRemain <= WeaponTotal)
                    && (WeaponElapsed <= 0.5f || WeaponRemain >= 2f))
                {
                    _standardReady = true;
                }
            }

            if (((!HasFinishingMove|| !FinishingMovePvE.EnoughLevel) && StandardStepPvE.CanUse(out _))
                || (HasFinishingMove && FinishingMovePvE.CanUse(out _)))
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
    public static DancerOpener ChosenOpener { get; set; } = DancerOpener.Standard;

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

        var act = ChosenOpener switch
        {
            DancerOpener.Standard => CountDownStandardOpener(remainTime),
            DancerOpener.Tech     => CountDownTechOpener(remainTime),
            _                     => null
        };

        return act ?? base.CountDownAction(remainTime);
    }

    private IAction? CountDownStandardOpener(float remainTime)
    {
        if (TryUseClosedPosition(out var act)
            || remainTime <= OpenerStandardStepTime && StandardStepPvE.CanUse(out act)
            || ExecuteStepGCD(out act)
            || remainTime <= OpenerFinishTime && DoubleStandardFinishPvE.CanUse(out act))
        {
            return act;
        }

        return null;
    }

    private IAction? CountDownTechOpener(float remainTime)
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
        return null;
    }

    #endregion

    #region oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        IsSaberDancePrimed();
        if (ChurinPotions.ShouldUsePotion(this, out act)) return true;
        if (TryUseDevilment(out act)) return true;
        if (SwapDancePartner(out act)) return true;
        if (TryUseClosedPosition(out act)) return true;

        if ((!CanUseStandardStep || !CanUseTechnicalStep) && !IsDancing)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        return false;

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
        act = null;
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

        var hasDPSCandidate = PartyMembers.Any(p =>
            p.GetRole() is CombatRole.DPS && p.DistanceToPlayer() <= 30 &&
            !p.HasStatus(false, HasWeaknessOrDamageDown));

        // Already have a dance partner or no party members
        if (StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition)
            || !PartyMembers.Any()
            || !ClosedPositionPvE.IsEnabled
            || IsDancing)
        {
            return false;
        }

        return hasDPSCandidate && ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        act = null;
        var hasDPSCandidate = PartyMembers.Any(p =>
            p.GetRole() is CombatRole.DPS && p.DistanceToPlayer() <= 30 &&
            !p.HasStatus(false, HasWeaknessOrDamageDown));

        if (!StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition)
        || !ShouldSwapDancePartner
        || !ClosedPositionPvE.IsEnabled
        || IsDancing)
        {
            return false;
        }

        if ((StandardStepPvE.Cooldown.WillHaveOneCharge(3f)
        || FinishingMovePvE.Cooldown.WillHaveOneCharge(3f)
        || TechnicalStepPvE.Cooldown.WillHaveOneCharge(3f))
        && ShouldSwapDancePartner)
        {
            return hasDPSCandidate && EndingPvE.CanUse(out act);
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

        if (ChurinPotions.ShouldUsePotion(this, out act))
        {
            return true;
        }

        var shouldFinish = HasStandardStep && CompletedSteps == 2 && CanUseStepHoldCheck(StandardHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.StandardStep);

        if (!shouldFinish && !aboutToTimeOut && !IsMedicated) return false;

        act = DoubleStandardFinishPvE;
        return true;
    }

    private bool TryFinishTech(out IAction? act)
    {
        act = null;
        if (!HasTechnicalStep || HasTillana || !IsDancing) return false;

        if (CompletedSteps < 4) return ExecuteStepGCD(out act);

        if (ChurinPotions.ShouldUsePotion(this, out act))
        {
            return true;
        }

        var shouldFinish = HasTechnicalStep && CompletedSteps == 4 && CanUseStepHoldCheck(TechHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.TechnicalStep);

        if (!shouldFinish && !aboutToTimeOut && !IsMedicated) return false;

        act = QuadrupleTechnicalFinishPvE;
        return true;

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
            || !HasDanceOfTheDawn)
        {
            return false;
        }

        return IsLastGCD(ActionID.TillanaPvE) || DanceOfTheDawnPvE.CanUse(out act);
    }

    private bool TryUseTillana(out IAction? act)
    {
        act = null;

        if (!HasTillana
            || Esprit >= RiskyEspritThreshold)
        {
            return false;
        }

        var gcdsUntilStandard = 0;
        for (uint i = 1; i <= 5; i++)
        {
            if (!StandardStepPvE.Cooldown.WillHaveOneChargeGCD(i, 0.5f)) continue;
            gcdsUntilStandard = (int)i;
            break;
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

        return Esprit < RiskyEspritThreshold && TillanaPvE.CanUse(out act);
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

        if ((IsBurstPhase && !HasLastDance && Esprit >= SaberDanceEspritCost)
            || (IsMedicated && Esprit >= SaberDanceEspritCost))
        {
            return SaberDancePvE.CanUse(out act);
        }

        if (Esprit > HighEspritThreshold) return false;

        if (Feathers < 4 && HasAnyProc)
        {
            if (BloodshowerPvE.CanUse(out act)) return true;
            if (FountainfallPvE.CanUse(out act)) return true;
            if (RisingWindmillPvE.CanUse(out act)) return true;
            if (ReverseCascadePvE.CanUse(out act)) return true;
        }

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

        if (IsAttackableBoss && CurrentTarget?.GetHealthRatio() < 0.07)
        {
            return true;
        }

        if (IsBurstPhase)
        {
            return willHaveOneCharge switch
            {
                false => Esprit >= SaberDanceEspritCost,
                true when HasLastDance => Esprit >= HighEspritThreshold,
                _ => false
            };
        }

        if (IsMedicated)
        {
            return Esprit >= SaberDanceEspritCost;
        }

        return Esprit >= MidEspritThreshold;

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
        var canUseTech = TechnicalStepPvE.EnoughLevel && (HasTechnicalFinish
                                                          || IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE));

        var cantUseTech = !TechnicalStepPvE.EnoughLevel && (HasStandardFinish || IsLastGCD(ActionID.DoubleStandardFinishPvE));

        if (IsDancing || !DevilmentPvE.EnoughLevel || DevilmentPvE.Cooldown.IsCoolingDown)
        {
            return false;
        }

        if (!canUseTech && !cantUseTech) return false;

        act = DevilmentPvE;
        return true;

    }

    private bool TryUseFlourish(out IAction? act)
    {
        act = null;
        if (!InCombat || HasThreefoldFanDance || !FlourishPvE.IsEnabled || !FlourishPvE.EnoughLevel || FlourishPvE.Cooldown.IsCoolingDown) return false;

        if (IsBurstPhase || (!TechnicalStepPvE.EnoughLevel && HasStandardFinish))
        {
            return FlourishPvE.CanUse(out act);
        }

        switch (ShouldUseTechStep)
        {
            case true when TechnicalStepPvE.Cooldown.IsCoolingDown && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(15):
            case false:
                act = FlourishPvE;
                return true;
        }
        return false;
    }

    private bool TryUseFeathers(out IAction? act)
    {
        act = null;

        var shouldDumpFeathers =
            HasEnoughFeathers
            && (HasAnyProc || FlourishPvE.Cooldown.WillHaveOneChargeGCD(1))
            && !CanUseTechnicalStep;

        var shouldHoldFeathers =
            !IsBurstPhase
            && (!HasEnoughFeathers || !HasAnyProc)
            && (!IsMedicated || (TechnicalStepPvE.Cooldown.WillHaveOneCharge(10) && ShouldUseTechStep))
            && !IsAttackableBoss
            && CurrentTarget?.GetHealthRatio() > 0.07;

        if (shouldDumpFeathers)
        {
            if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        if (HasFourfoldFanDance && FanDanceIvPvE.CanUse(out act)) return true;
        if (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act)) return true;

        if (shouldHoldFeathers) return false;

        return FanDanceIiPvE.CanUse(out act) || FanDancePvE.CanUse(out act);
}

    #endregion

    #endregion

    /// <summary>
    /// DNC-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinDNCPotions : Potions
    {
        private static bool IsOddMinuteWindow(float timing)
        {
            var minute = (int)(timing / 60f);
            return minute % 2 == 1;

        }

        public override bool IsConditionMet()
        {
            var danceSteps = new[] {ActionID.JetePvE, ActionID.EntrechatPvE, ActionID.PirouettePvE, ActionID.EmboitePvE};

            if (HasTechnicalStep && IsLastGCD(danceSteps) && CompletedSteps > 3)
            {
                return true;
            }

            return HasStandardStep && IsLastGCD(danceSteps) && CompletedSteps > 1;
        }

        protected override bool IsTimingValid(float timing)
        {
            var lateTiming = DataCenter.CombatTimeRaw >= timing;
            var lateTimingDiff = DataCenter.CombatTimeRaw - timing;

            const float earlyTimingWindow = 15f;

            if (timing > 0)
            {
                var timingDiff = MathF.Abs(DataCenter.CombatTimeRaw - timing);

                switch (ChosenOpener)
                {
                    case DancerOpener.Standard:
                    default:
                    {
                        if (!IsOddMinuteWindow(timing))
                        {
                            return lateTiming && lateTimingDiff <= TimingWindowSeconds;
                        }

                        // Odd-minute special handling: allow both sides within earlyTimingWindow.
                        return timingDiff <= earlyTimingWindow;
                    }

                    case DancerOpener.Tech:
                    {
                        return timingDiff <= earlyTimingWindow;
                    }
                }
            }

            // Check opener timing: OpenerPotionTime == 0 means disabled
            var countDown = Service.CountDownTime;

            if (!IsOpenerPotion(timing)) return false;
            if (ChurinDNC.OpenerPotionTime == 0f) return false;
            return countDown > 0f && countDown <= ChurinDNC.OpenerPotionTime;
        }
    }

}
