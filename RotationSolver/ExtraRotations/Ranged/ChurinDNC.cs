using System.ComponentModel;
using ECommons.GameFunctions;
using CombatRole = ECommons.GameFunctions.CombatRole;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("Churin DNC", CombatType.PvE, GameVersion = "7.5",
    Description =
        "Candles lit, runes drawn upon the floor, sacrifice prepared. Everything is ready for the summoning. I begin the incantation: \"Shakira, Shakira!\"")]
[SourceCode(Path = "main/ExtraRotations/Ranged/ChurinDNC.cs")]
[ExtraRotation]
public sealed class ChurinDNC : DancerRotation
{
    #region Properties

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
        [Description("Standard Opener")] Standard,
        [Description("Tech Opener")] Tech
    }

    private enum PotsDuringStepStrategy
    {
        [Description("Use potion before dance steps, right after Tech/Standard step is used")]
        BeforeStep,

        [Description("Use potion after dance steps, when the step finish is ready")]
        AfterStep
    }

    #endregion

    #region Constants

    private const int SaberDanceEspritCost = 50;
    private const int RiskyEspritThreshold = 40;
    private const int HighEspritThreshold = 80;
    private const int MidEspritThreshold = 70;
    private const int SafeEspritThreshold = 30;
    private const float DanceTargetRange = 15f;
    private const float DanceAllyRange = 30f;
    private const float MedicatedDuration = 30f;
    private const float SecondsToCompleteTech = 7f;
    private const float SecondsToCompleteStandard = 5f;
    private const float EstimatedAnimationLock = 0.6f;

    #endregion

    #region Player Status

    private bool IsBurstPhase => HasEnoughLevelForBurst;

    private bool HasEnoughLevelForBurst => DevilmentPvE.EnoughLevel
        ? TechnicalStepPvE.EnoughLevel && HasTechnicalFinish && HasDevilment
        : HasDevilment && HasStandardFinish;

    private static bool HasTillana => StatusHelper.PlayerHasStatus(true, StatusID.FlourishingFinish)
                                      && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingFinish);

    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated)
                                       && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.Medicated);

    private static bool JustMedicated => IsMedicated && StatusHelper.PlayerStatusTime(true, StatusID.Medicated) >
        MedicatedDuration - (WeaponTotal + WeaponRemain);

    private static bool HasAnyProc => StatusHelper.PlayerHasStatus(true, StatusID.SilkenFlow, StatusID.SilkenSymmetry,
        StatusID.FlourishingFlow, StatusID.FlourishingSymmetry);

    private static bool HasFinishingMove => StatusHelper.PlayerHasStatus(true, StatusID.FinishingMoveReady)
                                            && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FinishingMoveReady);

    private static bool HasStarfall => HasFlourishingStarfall &&
                                       !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.FlourishingStarfall);

    private static bool HasDanceOfTheDawn => StatusHelper.PlayerHasStatus(true, StatusID.DanceOfTheDawnReady)
                                             && !StatusHelper.PlayerWillStatusEnd(0, true,
                                                 StatusID.DanceOfTheDawnReady);

    private static readonly StatusID[] HasWeaknessOrDamageDown =
        [StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911];



    private static float CalculatedAnimationLock => Math.Max(AnimationLock, EstimatedAnimationLock);
    private static float WeaponLock => WeaponTotal - CalculatedAnimationLock;

    #endregion

    #region Job Gauge

    private static bool HasEnoughFeathers => Feathers > 3;
    private static bool HasFeatherProcs => HasThreefoldFanDance || HasFourfoldFanDance;
    private static bool CanStandardFinish => HasStandardStep && CompletedSteps > 1;
    private static bool CanTechnicalFinish => HasTechnicalStep && CompletedSteps > 3;
    private static bool CanSaberDance => Esprit >= SaberDanceEspritCost;
    private int EspiritThreshold => IsBurstPhase || IsMedicated
        ? (!StandardWillHaveCharge && !HasLastDance) || !StarfallEndingSoon
            ? SaberDanceEspritCost
            : HighEspritThreshold
        : MidEspritThreshold;

    private bool CanSpendEspritNow => Esprit >= EspiritThreshold;

    #endregion

    #region Target Info

    #region Hostiles

    private static bool AreDanceTargetsInRange =>
        (InCombat || IsDancing) && AllHostileTargets.Any(target => target.DistanceToPlayer() <= DanceTargetRange);

    #endregion

    #region Friendlies

    private static bool ShouldSwapDancePartner => CurrentDancePartner != null
                                                  && !IsValidDancePartner(CurrentDancePartner)
                                                  && HasAvailableDancePartner(RestrictDPTarget);

    private static bool HasAvailableDancePartner(bool restrictToDps)
    {
        return PartyMembers.Any(p => IsValidDancePartner(p) && (!restrictToDps || IsDPSinParty(p)));
    }

    private static bool IsDPSinParty(IBattleChara? p)
    {
        return p is not null
               && p.IsParty()
               && p.GetRole() is CombatRole.DPS;
    }

    private static bool IsValidDancePartner(IBattleChara? p)
    {
        return p is not null
               && !p.HasStatus(false, HasWeaknessOrDamageDown)
               && InDanceBuffRange(p);
    }

    private static bool InDanceBuffRange(IBattleChara? p)
    {
        return p is not null
               && p.DistanceToPlayer() <= DanceAllyRange;
    }

    #endregion

    #endregion

    #region Action Helpers

    #region Dance Helpers

    #region Standard Step and Finishing Move Helpers

    private IAction UseStandard => CanUseStandardStep && !CanFinishingMove
        ? StandardStepPvE
        : FinishingMovePvE;

    private bool CanFinishingMove => HasFinishingMove && FinishingMovePvE.EnoughLevel;

    private bool HasToRefreshStandardFinish
    {
        get
        {
            if (!InCombat && (!IsDancing || !HasStandardStep)) return false;

            if (IsDancing && HasStandardStep && CanStandardFinish) return false;

            if (HasStandardFinish && !StandardWillHaveCharge) return false;

            return (StatusHelper.PlayerWillStatusEnd(StandardRecast + WeaponTotal, true, StatusID.StandardFinish)
                    || !HasStandardFinish) && StandardCanUse;
        }
    }

    private float StandardRecast => !CanFinishingMove
        ? StandardStepPvE.Cooldown.RecastTimeRemain
        : FinishingMovePvE.Cooldown.RecastTimeRemain;

    private bool StandardCanUse => !CanFinishingMove
        ? StandardStepPvE.CanUse(out _)
        : FinishingMovePvE.CanUse(out _);

    private bool StandardWillHaveCharge => !CanFinishingMove
        ? StandardStepPvE.Cooldown.WillHaveOneCharge(7f)
        : FinishingMovePvE.Cooldown.WillHaveOneCharge(7f);

    private bool StandardIsCoolingDown => !CanFinishingMove
        ? StandardStepPvE.Cooldown.IsCoolingDown
        : FinishingMovePvE.Cooldown.IsCoolingDown;

    private bool CanUseStandardBasedOnEsprit => !HasLastDance && !CanSpendEspritNow;


    private bool CanUseStandardStepInBurst => !DisableStandardInBurst || !CanFinishingMove;

    private static bool ShouldHoldStandardStep(HoldStrategy strategy)
    {
        return strategy is HoldStrategy.HoldStepOnly && !HasStandardStep && !HasFinishingMove;
    }

    private bool ShouldHoldStandardFinish(HoldStrategy strategy)
    {
        return strategy is HoldStrategy.HoldFinishOnly
               && (CanFinishingMove || HasStandardStep);
    }

    private bool CanUseStandardStep
    {
        get
        {
            var blockedByBurst = IsBurstPhase && !CanUseStandardStepInBurst && !HasFinishingMove;

            if (blockedByBurst || !CanUseStandardBasedOnEsprit) return false;


            return  CanUseStepHoldCheck(StandardHoldStrategy)
                    && (StandardIsCoolingDown
                        ? IsTimingOk(StandardRecast)
                        : StandardCanUse);
        }
    }

    #endregion

    #region Technical Step and Tillana Helpers

    private static bool IsTimingOk(float recast) => recast < WeaponLock && WeaponRemain > CalculatedAnimationLock;

    private float TechnicalRecast => TechnicalStepPvE.Cooldown.RecastTimeRemain;

    private bool DevilmentReady
    {
        get
        {
            var devilmentRemain = DevilmentPvE.Cooldown.RecastTimeRemain;

            if (devilmentRemain > SecondsToCompleteTech) return false;

            var calculatedDevilmentRemain
                = devilmentRemain - WeaponTotal + CalculatedAnimationLock;

            return DevilmentPvE.Cooldown.WillHaveOneCharge(calculatedDevilmentRemain);
        }
    }

    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled && TechnicalStepPvE.EnoughLevel &&
                                      MergedStatus.HasFlag(AutoStatus.Burst);

    private static bool ShouldHoldTechStep(HoldStrategy strategy)
    {
        return strategy is HoldStrategy.HoldStepOnly && !HasTillana && !HasTechnicalStep;
    }

    private bool ShouldHoldTechFinish(HoldStrategy strategy)
    {
        return strategy is HoldStrategy.HoldFinishOnly
               && ((HasTillana && TillanaPvE.EnoughLevel) || HasTechnicalStep);
    }

    private bool CanUseStepHoldCheck(HoldStrategy strategy)
    {
        var isTech = strategy == TechHoldStrategy;
        var isStandard = strategy == StandardHoldStrategy;

        if (!isTech && !isStandard) return false;

        var shouldHoldStep = isTech ? ShouldHoldTechStep(strategy) : ShouldHoldStandardStep(strategy);
        var shouldHoldFinish = isTech ? ShouldHoldTechFinish(strategy) : ShouldHoldStandardFinish(strategy);

        return strategy switch
        {
            HoldStrategy.DontHoldStepAndFinish => true,
            HoldStrategy.HoldStepAndFinish => AreDanceTargetsInRange,
            _ when shouldHoldStep || shouldHoldFinish => AreDanceTargetsInRange,
            _ => true
        };
    }

    private bool CanUseTechnicalStep
    {
        get
        {
            if (!ShouldUseTechStep
                || IsDancing
                || HasTillana
                || HasToRefreshStandardFinish
                || !DevilmentReady)
                return false;

            return CanUseStepHoldCheck(TechHoldStrategy)
                   && TechnicalStepPvE.Cooldown.IsCoolingDown
                ? IsTimingOk(TechnicalRecast)
                : TechnicalStepPvE.CanUse(out _);
        }
    }

    #endregion

    #endregion

    #region General Helpers

    private static bool StarfallEndingSoon =>
        HasStarfall && StatusHelper.PlayerWillStatusEnd(7f, true, StatusID.FlourishingStarfall);
    private bool IsSaberDancePrimed => CanSpendEspritNow && CanSaberDance;
    private bool ShouldUseStarfallDance => !IsSaberDancePrimed;
    private bool ShouldUseLastDance
    {
        get
        {
            if (CanUseTechnicalStep
                || (TechnicalStepPvE.Cooldown.WillHaveOneCharge(15f)
                && ShouldUseTechStep
                && !HasTillana)) return false;

            return StandardWillHaveCharge
                ? !IsSaberDancePrimed
                : !IsSaberDancePrimed && !HasStarfall && !HasTillana;
        }
    }

    #endregion

    #endregion

    #endregion

    #region Config Options

    private static readonly ChurinDNCPotions ChurinPotions = new();

    #region Dance Partner Configs

    [RotationConfig(CombatType.PvE, Name = "Restrict Dance Partner to only DPS targets if any")]
    private static bool RestrictDPTarget { get; set; } = true;

    #endregion

    #region Dance Configs

    #region Opener Step Configs

    [RotationConfig(CombatType.PvE, Name = "Select an opener")]
    public static DancerOpener ChosenOpener { get; set; } = DancerOpener.Standard;

    #endregion

    #region Tech Step Configs

    [RotationConfig(CombatType.PvE, Name = "Technical Step, Technical Finish & Tillana Hold Strategy")]
    private HoldStrategy TechHoldStrategy { get; set; } = HoldStrategy.HoldStepAndFinish;

    [Range(0, 16, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Technical Step?",
        Parent = nameof(ChosenOpener),
        ParentValue = "Tech Opener",
        Tooltip = "If countdown is set above 13 seconds, " +
                  "it will start with Standard Step before initiating Tech Step, " +
                  "please go out of range of any enemies before the countdown reaches your configured time")]
    private float OpenerTechTime { get; set; } = 7f;

    [Range(0, 1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Technical Finish?",
        Parent = nameof(ChosenOpener),
        ParentValue = "Tech Opener")]
    private float OpenerTechFinishTime { get; set; } = 0.5f;

    #endregion

    #region Standard Step Configs

    [RotationConfig(CombatType.PvE, Name = "Standard Step, Standard Finish & Finishing Move Hold Strategy")]
    private HoldStrategy StandardHoldStrategy { get; set; } = HoldStrategy.HoldStepAndFinish;

    [Range(0, 16, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Step?",
        Parent = nameof(ChosenOpener),
        ParentValue = "Standard Opener")]
    private float OpenerStandardStepTime { get; set; } = 15.5f;

    [Range(0, 1, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "How many seconds before combat starts to use Standard Finish?",
        Parent = nameof(ChosenOpener),
        ParentValue = "Standard Opener")]
    private float OpenerStandardFinishTime { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE,
        Name = "Disable Standard Step in Burst - Ignored if not high enough level for Finishing Move")]
    private bool DisableStandardInBurst { get; set; } = true;

    #endregion

    #endregion

    #region Potion Configs

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    {
        get => ChurinPotions.Enabled;
        set => ChurinPotions.Enabled = value;
    }

    [RotationConfig(CombatType.PvE, Name = "Define potion usage behavior for Dancer",
        Parent = nameof(PotionUsageEnabled))]
    private static PotsDuringStepStrategy PotsDuringStep { get; set; } = PotsDuringStepStrategy.BeforeStep;

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    {
        get => ChurinPotions.Strategy;
        set => ChurinPotions.Strategy = value;
    }

    [Range(0, 20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus (value in seconds)",
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
    [RotationConfig(CombatType.PvE,
        Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets),
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

    #endregion

    #endregion

    #region Main Combat Logic

    #region Countdown Logic

    // Override the method for actions to be taken during the countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        if (!HasClosedPosition && TryUseClosedPosition(out var act)) return act;
        if (ChurinPotions.ShouldUsePotion(this, out var potionAct, false)) return potionAct;

        if (remainTime > OpenerStandardStepTime) return base.CountDownAction(remainTime);

        act = ChosenOpener switch
        {
            DancerOpener.Standard => CountDownStandardOpener(remainTime),
            DancerOpener.Tech => CountDownTechOpener(remainTime),
            _ => null
        };

        return act ?? base.CountDownAction(remainTime);
    }

    private bool ShouldStandardBeforeTech(float remainTime)
    {
        return remainTime > OpenerTechTime
               && remainTime > 13f;
    }

    private IAction? CountDownStandardOpener(float remainTime)
    {
        IAction? act;
        if (remainTime <= OpenerStandardStepTime && !IsDancing)
        {
            if (StandardStepPvE.CanUse(out act)) return act;
        }

        if (!CanStandardFinish)
        {
            if (ExecuteStepGCD(out act)) return act;
        }

        if (!(remainTime <= OpenerStandardFinishTime) || !CanStandardFinish) return null;

        return TryFinishTheDance(out act) ? act : null;
    }

    private IAction? CountDownTechOpener(float remainTime)
    {
        IAction? act;

        var preparingStandard = ShouldStandardBeforeTech(remainTime)
                                && !IsDancing
                                && HasStandardFinish;

        if (preparingStandard)
            if (StandardStepPvE.CanUse(out act))
                return act;

        var readyToTechStep = remainTime <= OpenerTechTime
                              && !IsDancing
                              && !HasTechnicalStep;
        if (readyToTechStep)
            if (TechnicalStepPvE.CanUse(out act))
                return act;

        if (IsDancing && !CanTechnicalFinish)
            if (ExecuteStepGCD(out act))
                return act;

        var finishStandard = remainTime > OpenerTechTime
                             && IsDancing
                             && HasStandardStep
                             && !AreDanceTargetsInRange;
        if (finishStandard)
            if (DoubleStandardFinishPvE.CanUse(out act))
                return act;

        var readyToTechFinish = CanTechnicalFinish
                                && remainTime <= OpenerTechFinishTime;

        if (!readyToTechFinish) return null;

        return TryFinishTheDance(out act) ? act : null;
    }

    #endregion

    #region Main oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!IsDancing)
            return TryUseDevilment(out act)
                   || SwapDancePartner(out act)
                   || TryUseClosedPosition(out act);

        if (JustMedicated)
            return TryFinishTheDance(out act)
                   || base.EmergencyAbility(nextGCD, out act);

        if (!ChurinPotions.ShouldUsePotion(this, out var potionAct)) return false;

        act = potionAct;
        return true;
    }

    /// Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (TryUseFlourish(out act)) return true;

        return TryUseFeatherProcs(out act)
               || TryUseFeathers(out act)
               || base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region Main GCD Logic

    /// Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (IsDancing) return TryFinishTheDance(out act);

        if (IsBurstPhase) return TryUseBurstGCD(out act);

        if (TryUseStep(out act)) return true;

        return TryUseFillerGCD(out act)
               || base.GeneralGCD(out act);
    }

    #endregion

    #endregion

    #region Extra Methods

    #region GCD Weaponskills

    #region Dance GCD Logic

    private bool TryUseStep(out IAction? act)
    {
        act = null;

        act = CanUseTechnicalStep && !CanUseStandardStep
            ? TechnicalStepPvE
            : UseStandard;

        return CanUseTechnicalStep || CanUseStandardStep;
    }

    private bool TryFinishStandard(out IAction? act)
    {
        act = null;
        if (!HasStandardStep || HasFinishingMove || !IsDancing) return false;

        if (CompletedSteps < 2) return ExecuteStepGCD(out act);

        var shouldFinish = CanStandardFinish && CanUseStepHoldCheck(StandardHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.StandardStep);

        if (!shouldFinish && !aboutToTimeOut) return false;

        act = DoubleStandardFinishPvE;
        return true;
    }

    private bool TryFinishTech(out IAction? act)
    {
        act = null;
        if (!HasTechnicalStep || HasTillana || !IsDancing) return false;

        if (CompletedSteps < 4) return ExecuteStepGCD(out act);

        var shouldFinish = CanTechnicalFinish && CanUseStepHoldCheck(TechHoldStrategy);
        var aboutToTimeOut = StatusHelper.PlayerWillStatusEnd(1, true, StatusID.TechnicalStep);
        if (!shouldFinish && !aboutToTimeOut) return false;

        act = QuadrupleTechnicalFinishPvE;
        return true;
    }

    private bool TryFinishTheDance(out IAction? act)
    {
        act = null;
        if (!IsDancing || HasFinishingMove || HasTillana) return false;

        return (HasStandardStep && TryFinishStandard(out act))
               || (HasTechnicalStep && TryFinishTech(out act));
    }

    #endregion

    #region Burst GCD Logic

    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;
        if (!IsBurstPhase) return false;
        if (TryUseStep(out act)) return true;
        if (TryUseDanceOfTheDawn(out act)) return true;
        if (TryUseTillana(out act)) return true;
        if (TryUseLastDance(out act)) return true;
        if (TryUseStarfallDance(out act)) return true;
        return TryUseSaberDance(out act)
               || TryUseFillerGCD(out act);
    }

    private bool TryUseDanceOfTheDawn(out IAction? act)
    {
        act = null;
        if (!IsSaberDancePrimed || !HasDanceOfTheDawn) return false;

        return DanceOfTheDawnPvE.CanUse(out act);
    }

    private bool TryUseTillana(out IAction? act)
    {
        act = null;
        if (!HasTillana) return false;

        var blockTillana = StandardWillHaveCharge
            ? Esprit < SafeEspritThreshold && !HasLastDance
            : Esprit < RiskyEspritThreshold;

        return !blockTillana && TillanaPvE.CanUse(out act);
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance) return false;

        return LastDancePvE.CanUse(out act) && ShouldUseLastDance;
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall
            || CanUseStandardStep
            || (StandardWillHaveCharge && HasLastDance)) return false;

        return ShouldUseStarfallDance && StarfallDancePvE.CanUse(out act);
    }

    #endregion

    #region Regular GCD Logic

    private bool TryUseFillerGCD(out IAction? act)
    {
        act = null;
        if (TryUseStep(out act)) return true;
        if (IsDancing || CanUseStandardStep || CanUseTechnicalStep) return false;
        if (TryUseProcs(out act)) return true;
        if (TryUseSaberDance(out act)) return true;
        if (TryUseTillana(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        return HasLastDance
            ? TryUseLastDance(out act)
            : TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        act = null;
        if (TryUseStep(out act)) return true;
        if (IsDancing || CanUseStandardStep || CanUseTechnicalStep) return false;
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        if (WindmillPvE.CanUse(out act)) return true;
        return CascadePvE.CanUse(out act) || base.GeneralGCD(out act);
    }

    private bool TryUseFeatherGCD(out IAction? act)
    {
        act = null;
        if (!HasEnoughFeathers) return false;

        var hasSilkenProcs = HasSilkenFlow || HasSilkenSymmetry;
        var hasFlourishingProcs = HasFlourishingFlow || HasFlourishingSymmetry;

        if (hasSilkenProcs || !hasFlourishingProcs || CanSaberDance) return SaberDancePvE.CanUse(out act);
        if (FountainPvE.CanUse(out act)) return true;
        return CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act);
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        act = null;
        if (IsDancing || CanUseStandardStep || CanUseTechnicalStep) return false;

        return IsSaberDancePrimed && SaberDancePvE.CanUse(out act);
    }

    private bool TrySaberOrBasic(out IAction? act)
{
    return SaberDancePvE.CanUse(out act) || TryUseBasicGCD(out act);
}

    private bool TryUseProcs(out IAction? act)
    {
        act = null;

        if (IsBurstPhase || !ShouldUseTechStep || CanUseStandardStep || CanUseTechnicalStep || IsDancing)
            return false;

        var gcdsUntilTech = 0;
        for (var i = 1; i <= 5; i++)
        {
            if (!TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD((uint)i, 0.5f)) continue;
            gcdsUntilTech = i;
            break;
        }

        if (gcdsUntilTech == 0) return false;

        switch (gcdsUntilTech)
        {
            case 5:
            case 4:
                return !HasAnyProc || Esprit < HighEspritThreshold
                    ? TryUseBasicGCD(out act)
                    : TrySaberOrBasic(out act);

            case 3:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);

                return FountainPvE.CanUse(out act)
                       || CascadePvE.CanUse(out act)
                       || TrySaberOrBasic(out act);

            case 2:
                return Esprit >= SaberDanceEspritCost && !HasAnyProc
                    ? TrySaberOrBasic(out act)
                    : Esprit < SaberDanceEspritCost
                        ? TryUseBasicGCD(out act)
                        : TrySaberOrBasic(out act);

            case 1:
                if (HasAnyProc && Esprit < HighEspritThreshold) return TryUseBasicGCD(out act);

                if (HasAnyProc) return TrySaberOrBasic(out act);

                if (Esprit < SaberDanceEspritCost)
                    return FountainPvE.CanUse(out act)
                           || LastDancePvE.CanUse(out act)
                           || TryUseBasicGCD(out act);

                return TrySaberOrBasic(out act);
        }

        return false;
    }

    #endregion

    #endregion

    #region oGCD Abilities

    #region Burst oGCDs

    private bool TryUseDevilment(out IAction? act)
    {
        act = null;
        var canUseTech = TechnicalStepPvE.EnoughLevel && (HasTechnicalFinish
                                                          || IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE));

        var cantUseTech = !TechnicalStepPvE.EnoughLevel &&
                          (HasStandardFinish || IsLastGCD(ActionID.DoubleStandardFinishPvE));

        if (!DevilmentPvE.EnoughLevel || DevilmentPvE.Cooldown.IsCoolingDown || HasDevilment) return false;

        if (!canUseTech && !cantUseTech) return false;

        act = DevilmentPvE;
        return true;
    }

    private bool TryUseFlourish(out IAction? act)
    {
        act = null;

        if (HasThreefoldFanDance || !EnoughWeaveTime || IsDancing) return false;

        if (!FlourishPvE.CanUse(out act)) return false;

        if (IsBurstPhase) return true;

        if (CanStandardFinish || CanTechnicalFinish) return false;

        if (!ShouldUseTechStep) return true;

        return TechnicalStepPvE.Cooldown.IsCoolingDown
               && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(35);
    }

    #endregion

    #region Feathers

    private bool TryUseFeatherProcs(out IAction? act)
    {
        act = null;
        if (!HasFeatherProcs) return false;

        if (!EnoughWeaveTime) return false;

        return (HasThreefoldFanDance && FanDanceIiiPvE.CanUse(out act))
               || (HasFourfoldFanDance && FanDanceIvPvE.CanUse(out act));
    }

    private bool TryUseFeathers(out IAction? act)
    {
        act = null;
        if (Feathers <= 0 || !EnoughWeaveTime) return false;

        var overcapRisk = HasEnoughFeathers && (HasAnyProc || FlourishPvE.Cooldown.WillHaveOneChargeGCD(1)) &&
                          !CanUseTechnicalStep;

        var medicatedOutsideBurst = IsMedicated
                                    && !TechnicalStepPvE.Cooldown.WillHaveOneCharge(30)
                                    && ShouldUseTechStep;

        var shouldDumpFeathers = IsBurstPhase || overcapRisk || medicatedOutsideBurst;


        return shouldDumpFeathers && (FanDanceIiPvE.CanUse(out act)
                                      || FanDancePvE.CanUse(out act));
    }

    #endregion

    #region Dance Partner

    private bool TryUseClosedPosition(out IAction? act)
    {
        act = null;
        if (HasClosedPosition
            || IsDancing
            || !HasAvailableDancePartner(RestrictDPTarget))
            return false;

        return ClosedPositionPvE.CanUse(out act);
    }

    private bool SwapDancePartner(out IAction? act)
    {
        act = null;
        if (!HasClosedPosition
            || !ShouldSwapDancePartner
            || !ClosedPositionPvE.IsEnabled
            || IsDancing)
            return false;
        return EndingPvE.CanUse(out act);
    }

    #endregion

    #endregion

    #endregion

    #region Potions

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
            try
            {
                if (!IsDancing || JustMedicated) return false;

                var timing = GetTimingsArray();
                if (timing.Length == 0) return false;

                if (timing.Any(IsOpenerPotion)) return CanTechnicalFinish || CanStandardFinish;

                return PotsDuringStep switch
                {
                    PotsDuringStepStrategy.BeforeStep => HasTechnicalStep || HasStandardStep,
                    PotsDuringStepStrategy.AfterStep => CanTechnicalFinish || CanStandardFinish,
                    _ => false
                };
            }

            catch
            {
                return false;
            }
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
                        if (!IsOddMinuteWindow(timing)) return lateTiming && lateTimingDiff <= TimingWindowSeconds;

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

    private void UpdateCustomTimings()
    {
        ChurinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }

    #endregion

    #region Debug Tracking

    public override void DisplayRotationStatus()
    {
        if (ImGui.CollapsingHeader("Core"))
        {
            ValueRow("Weapon Total", $"{WeaponTotal:F2}");
            ValueRow("Completed Steps", CompletedSteps);
            ValueRow("Esprit", Esprit);
            ValueRow("Feathers", Feathers);

            BoolRow("Is Burst Phase", IsBurstPhase);
            BoolRow("Is Dancing", IsDancing);
            BoolRow("Can Weave", CanWeave);
        }

        if (ImGui.CollapsingHeader("Step Logic"))
        {
            ValueRow("Tech Hold Strategy", TechHoldStrategy);
            BoolRow("Tech Hold Check", CanUseStepHoldCheck(TechHoldStrategy));
            BoolRow("Can Use Technical Step", CanUseTechnicalStep);

            ImGui.Separator();

            ValueRow("Standard Hold Strategy", StandardHoldStrategy);
            BoolRow("Standard Hold Check", CanUseStepHoldCheck(StandardHoldStrategy));
            BoolRow("Can Use Standard Step", CanUseStandardStep);
        }

        if (ImGui.CollapsingHeader("Burst / Proc"))
        {
            BoolRow("Saber Dance Primed", IsSaberDancePrimed);
            BoolRow("Has Any Proc", HasAnyProc);
            BoolRow("Has Enough Feathers", HasEnoughFeathers);

            ImGui.Separator();
            BoolRow("TryUseSaberDance - Enough Esprit", Esprit >= SaberDanceEspritCost);
            BoolRow("TryUseSaberDance - Blocked (Tech/Dancing)", CanUseTechnicalStep || IsDancing);
        }

        if (ImGui.CollapsingHeader("Potions"))
        {
            BoolRow("Potion Usage Enabled", PotionUsageEnabled);
            ValueRow("Potion Usage Preset", PotionUsagePresets);
            BoolRow("Potion Condition Met", ChurinPotions.IsConditionMet());
            BoolRow("Potion Can Use At Time", ChurinPotions.CanUseAtTime());
        }

        if (ImGui.CollapsingHeader("Method Checks"))
        {
            BoolRow("GeneralGCD -> Burst Path", IsBurstPhase);
            BoolRow("GeneralGCD -> Step Path", !IsDancing && (CanUseStandardStep || CanUseTechnicalStep));
            BoolRow("GeneralGCD -> Finish Dance Path", IsDancing);
            BoolRow("GeneralGCD -> Filler Path",
                !IsBurstPhase && !IsDancing && !CanUseStandardStep && !CanUseTechnicalStep);
        }

        ImGui.Separator();

        BoolRow("TryUseStep - Can Tech", CanUseTechnicalStep);
        BoolRow("TryUseStep - Can Standard", CanUseStandardStep);
        BoolRow("TryUseStep - Has Finishing Move", HasFinishingMove);
    }

    private static void BoolRow(string label, bool value)
    {
        ImGui.Text($"{label}: {(value ? "Yes" : "No")}");
    }

    private static void ValueRow<T>(string label, T value)
    {
        ImGui.Text($"{label}: {value}");
    }

    #endregion
}