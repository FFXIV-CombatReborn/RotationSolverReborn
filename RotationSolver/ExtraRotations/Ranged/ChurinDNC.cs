using System.ComponentModel;
using Dalamud.Interface.Colors;
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
	/// <summary>
    /// Defines strategies for holding dance steps and finishes based on target presence and type.
    /// Each strategy determines whether to hold Step and/or Finish actions when no targets are in range, with specific conditions for Technical and Standard steps.
    /// </summary>
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

	///<summary>
	///Defines the available opener strategies for Dancer
	///</summary>
    public enum DancerOpener
    {
        [Description("Standard Opener")] Standard,
        [Description("Tech Opener")] Tech
    }

	///<summary>
	///Defines when to use potions in relation to dance steps during combat, allowing for strategic timing of potion effects either before or after executing dance steps.
	///</summary>
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
    private const int MaxEsprit = 100;
    private const int SafeEspritThreshold = 30;
    private const float DanceTargetRange = 15f;
    private const float DanceAllyRange = 30f;
    private const float MedicatedDuration = 30f;
    private const float SecondsToCompleteTech = 7f;
    private const float SecondsToCompleteStandard = 5f;
    private const float EstimatedAnimationLock = 0.6f;

    #endregion

    #region Player Status Checks

    /// <summary>
    /// Defines an array of StatusIDs that represent negative conditions (such as Weakness, Damage Down, Brink of Death) that would make a party member an invalid dance partner.
    /// </summary>
    private static readonly StatusID[] HasWeaknessOrDamageDown =
        [StatusID.Weakness, StatusID.DamageDown, StatusID.BrinkOfDeath, StatusID.DamageDown_2911];

    /// <summary>
    /// Defines arrays of StatusIDs that represent the procs for Silken Flow/Symmetry and Flourishing Flow/Symmetry, which are important for determining when to use certain dance finishers and procs during combat.
    /// </summary>
    private static readonly StatusID[] SilkenProcs = [StatusID.SilkenFlow, StatusID.SilkenSymmetry];
    private static readonly StatusID[] FlourishingProcs = [StatusID.FlourishingFlow, StatusID.FlourishingSymmetry];

    /// <summary>
    /// Checks if the player currently has any active status from the provided array of StatusIDs,
    /// and ensures that the status is not about to expire,
    /// </summary>
    /// <param name="id">
    /// An array of StatusIDs to check for active status on the player.
    /// The method will return true if the player has any of these statuses active,
    /// and they are not about to expire; otherwise, it will return false.
    /// </param>
    /// <returns>
    /// True if the player has any of the specified statuses active, and they are not about to expire; otherwise, false.
    /// </returns>
    private static bool HasActiveStatus(StatusID[] id)
    {
        return StatusHelper.PlayerHasStatus(true, id) && !StatusHelper.PlayerWillStatusEnd(0, true, id);
    }
    /// <summary>
    /// Checks if the player currently has an active status from the provided StatusID,
    /// </summary>
    /// <param name="id">
    /// A StatusID to check for active status on the player.
    /// </param>
    /// <returns>
    /// True if the player has the specified status active, and it is not about to expire; otherwise, false.
    /// </returns>
    private static bool HasActiveStatus(StatusID id)
    {
        return StatusHelper.PlayerHasStatus(true, id) && !StatusHelper.PlayerWillStatusEnd(0, true, id);
    }

    /// <summary>
	/// Checks if player can execute the requisite Dancer burst skills
    /// by verifying if they have the necessary level or if they are in a low-level burst scenario, and ensuring they have Devilment ready.
    /// </summary>
    private bool IsBurstPhase => ((HasEnoughLevelForBurst && HasTechnicalFinish) || IsLowLevelBurst) && HasDevilment;
    /// <summary>
    /// Determines if the player has the necessary level to execute both Devilment and Technical Finish.
    /// </summary>
    private bool HasEnoughLevelForBurst => DevilmentPvE.EnoughLevel && TechnicalStepPvE.EnoughLevel;
    /// <summary>
    /// Checks if the player is in a low-level burst scenario,
    /// </summary>
    private bool IsLowLevelBurst => !HasEnoughLevelForBurst && HasStandardFinish;
    private static bool HasTillana => HasActiveStatus(StatusID.FlourishingFinish);
    private static bool IsMedicated => HasActiveStatus(StatusID.Medicated);

    /// <summary>
    /// Determines if the player's current medication status is due to using the correct Potion
    /// </summary>
    private bool IsBurstMedicine
    {
        get
        {
            if (Medicines.Length == 0) return false;

            if (!IsMedicated) return false;

            foreach (var medicine in Medicines)
            {
                if (medicine.Type != MedicineType)
                    return false;
                if (!IsLastAction(false, new IAction[medicine.ID]))
                    return false;
            }

            return true;
        }
    }
    private bool JustMedicated => IsMedicated && IsBurstMedicine;
    private static bool HasSilkenProcs => HasActiveStatus(SilkenProcs);
    private static bool HasFlourishingProcs => HasActiveStatus(FlourishingProcs);
    private static bool HasAnyProc => HasSilkenProcs || HasFlourishingProcs;
    private static bool HasFinishingMove => HasActiveStatus(StatusID.FinishingMoveReady);
    private static bool HasStarfall => HasActiveStatus(StatusID.FlourishingStarfall);
    private static bool HasDanceOfTheDawn => HasActiveStatus(StatusID.DanceOfTheDawnReady);

    /// <summary>
    /// Calculates the effective animation lock duration taking the maximum of the base AnimationLock and an estimated value,
    /// </summary>
    private static float CalculatedAnimationLock => Math.Max(AnimationLock, EstimatedAnimationLock);

    /// <summary>
    /// Calculates the remaining time on the weapon skill lock by subtracting the calculated animation lock from the total weapon lock duration,
    /// </summary>
    private static float WeaponLock => WeaponTotal - CalculatedAnimationLock;

    #endregion

    #region Job Gauge

    private static bool HasEnoughFeathers => Feathers > 3;
    private static bool HasFeatherProcs => HasThreefoldFanDance || HasFourfoldFanDance;
    private static bool CanStandardFinish => HasStandardStep && CompletedSteps > 1;
    private static bool CanTechnicalFinish => HasTechnicalStep && CompletedSteps > 3;
    private static bool CanSaberDance => Esprit >= SaberDanceEspritCost;
    private int EspritThreshold
    {
        get
        {
            if (!IsBurstPhase && !IsMedicated) return MidEspritThreshold;

            if (HasDanceOfTheDawn && (CanSaberDance || IsLastGCD(ActionID.TillanaPvE))) return SaberDanceEspritCost;

            if (HasLastDance || HasFinishingMove)
            {
                if (ActiveStandardWillHaveCharge) return MaxEsprit;
            }

            if (HasStarfall && StarfallEndingSoon) return HighEspritThreshold;

            return SaberDanceEspritCost;
        }
    }
    private bool CanSpendEspritNow => Esprit >= EspritThreshold;

    #endregion

    #region Target Info

    #region Hostiles
    /// <summary>
    /// Determines if there are any hostile targets within the effective range for dance steps,
    /// which is crucial for deciding whether to hold or use dance actions based on the presence of valid targets.
    /// </summary>
    /// <returns>
    /// True if there is at least one hostile target within the effective range for dance steps;
    /// otherwise, false.
    /// </returns>>
    private static bool AreDanceTargetsInRange
    {
        get
        {
            if (!InCombat && !IsDancing) return false;

            if (AllHostileTargets == null) return false;

            foreach (var target in AllHostileTargets)
            {
                if (target.DistanceToPlayer() <= DanceTargetRange) return true;
            }

            return false;
        }

    }

    #endregion

    #region Friendlies

    private static bool ShouldSwapDancePartner => CurrentDancePartner != null
                                                  && !IsValidDancePartner(CurrentDancePartner)
                                                  && HasAvailableDancePartner(RestrictDPTarget);

    /// <summary>
    /// Checks if there is an available dance partner within range that meets the criteria for being a valid partner,
    /// optionally restricting to only DPS targets if specified.
    /// </summary>
    /// <param name="restrictToDps">
    /// A boolean value indicating whether to restrict the search for dance partners to only those with a DPS combat role.
    /// </param>
    /// <returns>
    /// True if there is at least one valid dance partner within range that meets the specified criteria; otherwise, false.
    /// </returns>
    private static bool HasAvailableDancePartner(bool restrictToDps)
    {
        if (PartyMembers == null) return false;

        foreach (var member in PartyMembers)
        {
            if (IsValidDancePartnerInRange(member)
                && (!restrictToDps || IsDPSinParty(member)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the specified party member is a valid DPS target for dancing,
    /// ensuring they are alive, in the party, and have the appropriate combat role.
    /// </summary>
    /// <param name="p">
    /// The party member to check for validity as a DPS target.
    /// This should be an instance of IBattleChara representing a character in the player's party.
    /// </param>
    /// <returns>
    /// True if the specified party member is a valid DPS target for dance partner; otherwise, false.
    /// </returns>
    private static bool IsDPSinParty(IBattleChara? p)
    {
        if (p == null) return false;
        if (!p.IsParty()) return false;
        return p.GetRole() == CombatRole.DPS;
    }
    /// <summary>
    /// Checks if the specified party member is a valid dance partner,
    /// ensuring they are alive and do not have any negative statuses that would prevent them from being an effective partner.
    /// </summary>
    /// <param name="p">
    /// The party member to check for validity as a dance partner. This should be an instance of IBattleChara representing a character in the player's party.
    /// </param>
    /// <returns>
    /// True if the specified party member is a valid dance partner; otherwise, false.
    /// A valid dance partner is one who is alive and does not have any of the negative statuses defined in HasWeaknessOrDamageDown.
    /// </returns>
    private static bool IsValidDancePartner(IBattleChara? p)
    {
        if (p == null) return false;
        if (p.IsDead) return false;
        return !p.HasApplyStatus(HasWeaknessOrDamageDown);
    }
    /// <summary>
    /// Checks if the specified party member is within the effective range to receive dance buffs,
    /// </summary>
    /// <param name="p">
    /// The party member to check for being within dance buff range.
    /// This should be an instance of IBattleChara representing a character in the player's party.
    /// </param>
    /// <returns>
    /// True if the specified party member is within the effective range to receive dance buffs; otherwise, false.
    /// </returns>
    private static bool IsValidDancePartnerInRange(IBattleChara? p)
    {
        if (p == null) return false;
        if (!IsValidDancePartner(p)) return false;
        return p.DistanceToPlayer() <= DanceAllyRange;
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
            if (StandardStepPvE.CanUse(out act))
                return act;

        if (!CanStandardFinish)
            if (ExecuteStepGCD(out act))
                return act;

        if (!(remainTime <= OpenerStandardFinishTime) || !CanStandardFinish) return null;

        return TryFinishDance(out act, false) ? act : null;
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
        {
            if (TechnicalStepPvE.CanUse(out act))
                return act;
        }

        if (IsDancing && !CanTechnicalFinish)
        {
            if (ExecuteStepGCD(out act))
                return act;
        }

        var finishStandard = remainTime > OpenerTechTime
                             && IsDancing
                             && HasStandardStep
                             && !AreDanceTargetsInRange;
        if (finishStandard)
        {
            if (DoubleStandardFinishPvE.CanUse(out act))
                return act;
        }

        var readyToTechFinish = CanTechnicalFinish
                                && remainTime <= OpenerTechFinishTime;

        if (!readyToTechFinish) return null;

        return TryFinishDance(out act, true) ? act : null;
    }

    #endregion

    #region Main oGCD Logic

    /// Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!IsDancing || !Showtime)
            return TryUseDevilment(out act)
                   || SwapDancePartner(out act)
                   || TryUseClosedPosition(out act);

        if (JustMedicated)
            return TryFinishDance(out act, true)
                   ||TryFinishDance(out act, false)
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
        if (IsDancing)
        {
            return TryFinishDance(out act, true)
                || TryFinishDance(out act, false);
        }
        if (TryUseStep(out act)) return true;
        if (IsBurstPhase) return TryUseBurstGCD(out act);
        return (!Showtime && TryUseFillerGCD(out act))
               || base.GeneralGCD(out act);
    }

    #endregion

    #endregion

    #region Extra Methods

    #region Action Helpers

    #region Dance Helpers

    private readonly ActionID[] _danceSteps = [ActionID.StandardStepPvE, ActionID.TechnicalStepPvE];
    private IBaseAction ActiveStandard => CanFinishingMove ? FinishingMovePvE : StandardStepPvE;
    private IAction UseActiveStandard => ActiveStandard;
    private bool AboutToDance => CanUseTechStep || CanUseActiveStandard;

    /// <summary>
    /// Determines if the conditions are met to use either
    /// Technical Step or Standard Step based on the player's current status,
    /// available resources, and configuration settings,
    /// </summary>
    private bool Showtime => IsDancing || IsLastGCD(_danceSteps) || AboutToDance;

    /// <summary>
    /// Determines if the player can use Finishing Move as a finisher for their dance
    /// based on various conditions such as level requirements, combat status, cooldowns,
    /// and whether they have the necessary buffs active.
    /// </summary>
    private bool CanFinishingMove
    {
        get
        {
            if (!FinishingMovePvE.EnoughLevel || !FinishingMovePvE.IsEnabled) return false;
            if (!InCombat || !FlourishPvE.IsEnabled) return false;
            if (FlourishPvE.Cooldown.IsCoolingDown && !HasFinishingMove) return false;
            return HasFinishingMove;
        }
    }

    /// <summary>
    /// Determines if the player needs to refresh their Standard Finish status based on
    /// various conditions such as combat status.
    /// </summary>
    private bool HasToRefreshStandardFinish
    {
        get
        {
            if (!InCombat && (IsDancing || HasStandardStep)) return false;

            if (HasStandardFinish && (IsDancing || !ActiveStandardWillHaveCharge || HasStandardStep)) return false;

            if (HasStandardFinish && TechnicalRecastRemain < SecondsToCompleteTech && ShouldUseTechStep) return false;

            return (StatusHelper.PlayerWillStatusEnd(ActiveStandardRecastRemain + WeaponTotal, true, StatusID.StandardFinish)
                    || !HasStandardFinish) && ActiveStandard.CanUse(out _);
        }
    }
    private float ActiveStandardRecastRemain => ActiveStandard.Cooldown.RecastTimeRemain;
    private float TechnicalRecastRemain => TechnicalStepPvE.Cooldown.RecastTimeRemain;
    private bool ActiveStandardWillHaveCharge =>
        ActiveStandard.Cooldown.WillHaveOneCharge(SecondsToCompleteStandard + WeaponLock);
    private bool CanUseStandardBasedOnEsprit => !HasLastDance && !CanSpendEspritNow;
    private bool CanUseStandardStepInBurst => !DisableStandardInBurst || HasFinishingMove;
    private bool DevilmentReady
    {
        get
        {
            var devilmentRemain = DevilmentPvE.Cooldown.RecastTimeRemain;
            if (!ShouldUseTechStep) return false;

            if (DevilmentPvE.Cooldown.IsCoolingDown
                && Math.Abs(devilmentRemain - TechnicalRecastRemain) > SecondsToCompleteTech + WeaponLock) return false;

            return DevilmentPvE.Cooldown.WillHaveOneCharge(SecondsToCompleteTech + WeaponLock);
        }
    }
    private bool ShouldUseTechStep => TechnicalStepPvE.IsEnabled && TechnicalStepPvE.EnoughLevel &&
                                      MergedStatus.HasFlag(AutoStatus.Burst);

    /// <summary>
    /// Decide if the recast timing is acceptable to attempt a step given weapon/animation locks.
    /// </summary>
    private static bool IsTimingOk(float recastRemain, IBaseAction action)
    {
        if (recastRemain > WeaponTotal && action.Cooldown.IsCoolingDown)
        {
            return false;
        }

        if (WeaponRemain < DataCenter.CalculatedActionAhead
            || WeaponElapsed > CalculatedAnimationLock)
        {
            return action.Cooldown.WillHaveOneCharge(CalculatedAnimationLock);
        }

        if (action.CanUse(out _))
        {
            return true;
        }

        return recastRemain - WeaponTotal <= Math.Max(CalculatedAnimationLock, WeaponLock )
               || action.Cooldown.HasOneCharge;
    }

    /// <summary>
    /// Determines whether to hold the Step and/or Finish actions based on the specified HoldStrategy and the presence of targets in range.
    /// </summary>
    /// <param name="strategy">
    /// The HoldStrategy to evaluate, which defines the conditions under which to hold Step and/or Finish actions when no targets are in range.
    /// </param>
    /// <param name="isTechnical">
    /// A boolean value indicating whether the dance step being evaluated is a Technical Step (true) or a Standard Step (false), which may affect the conditions for holding actions based on the strategy.
    /// </param>
    /// <returns>
    /// True if the conditions for holding Step and/or Finish actions are met based on the specified strategy and the presence of targets in range; otherwise, false.
    /// </returns>
    private bool CanUseStepHoldCheck(HoldStrategy strategy, bool isTechnical) => strategy switch
    {
        HoldStrategy.DontHoldStepAndFinish => true,
        HoldStrategy.HoldStepAndFinish => AreDanceTargetsInRange,
        HoldStrategy.HoldStepOnly => CanHoldStepOnly(isTechnical),
        HoldStrategy.HoldFinishOnly => CanHoldFinishOnly(isTechnical),
        _ => true
    };
    private bool CanHoldStepOnly(bool isTechnical)
    {
        var shouldHoldTechStep = isTechnical && ShouldUseTechStep && !HasTechnicalStep && !HasTillana;
        var shouldHoldStandardStep = !isTechnical && ActiveStandard.IsEnabled
                                                  && (!CanFinishingMove || !HasFinishingMove) && !HasStandardStep;

        if (!shouldHoldTechStep && !shouldHoldStandardStep) return false;
        return (shouldHoldTechStep || shouldHoldStandardStep) && AreDanceTargetsInRange;
    }
    private bool CanHoldFinishOnly(bool isTechnical)
    {
        var shouldHoldTechFinish = isTechnical && (HasTillana || HasTechnicalStep);
        var shouldHoldStandardFinish = !isTechnical && ActiveStandard.IsEnabled && (CanFinishingMove || HasStandardStep);

        if (!shouldHoldTechFinish && !shouldHoldStandardFinish) return false;

        return (shouldHoldTechFinish|| shouldHoldStandardFinish) && AreDanceTargetsInRange;
    }

    /// <summary>
    /// Determines whether the player can use a dance step (Technical Step or Standard Step)
    /// based on the current step status, player status, and configuration settings,
    /// </summary>
    /// <param name="isTechnical">
    /// A boolean value indicating the type of dance step being evaluated: true for Technical Step and false for Standard Step.
    /// </param>
    /// <returns></returns>
    private bool CanUseStep(bool isTechnical)
    {
        if (isTechnical)
        {
            if (!ShouldUseTechStep
                || (IsDancing && HasTechnicalStep)
                || HasTillana
                || HasToRefreshStandardFinish
                || !DevilmentReady)
            {
                return false;
            }

            return IsTimingOk(TechnicalRecastRemain, TechnicalStepPvE)
                   && CanUseStepHoldCheck(TechHoldStrategy, true);
        }

        if (!ActiveStandard.IsEnabled) return false;

        if ((IsBurstPhase && !CanUseStandardStepInBurst)
            || !CanUseStandardBasedOnEsprit) return false;

        if (ShouldUseTechStep && TechnicalStepPvE.CanUse(out _)) return false;

        return IsTimingOk(ActiveStandardRecastRemain, ActiveStandard)
               && CanUseStepHoldCheck(StandardHoldStrategy, false);
    }
    private bool CanUseTechStep => CanUseStep(true);
    private bool CanUseActiveStandard => CanUseStep(false);

    #endregion

    #region General Helpers
    private static bool StarfallEndingSoon =>
        HasStarfall && StatusHelper.PlayerWillStatusEnd(7f, true, StatusID.FlourishingStarfall);
    private bool IsSaberDancePrimed => CanSpendEspritNow && CanSaberDance;

    private bool ShouldUseLastDance
    {
        get
        {
            if (CanUseTechStep || (TechnicalStepPvE.Cooldown.WillHaveOneCharge(15f)
                                     && ShouldUseTechStep)) return false;

            if (ActiveStandardWillHaveCharge) return !IsSaberDancePrimed;

            return !IsSaberDancePrimed && !HasStarfall && !HasTillana;
        }
    }

    #endregion

    #endregion

    #region GCD Weaponskills

    #region Dance GCD Logic

    /// <summary>
    /// Determines whether to use a dance step (Technical or Standard)
    /// based on the current combat situation, player status, and configuration settings.
    /// </summary>
    /// <param name="act">
    /// An output parameter that will hold the IAction representing the dance step to be used if the method returns true; otherwise, it will be null.
    /// </param>
    /// <returns>
    /// True if a dance step action is determined to be used based on the current conditions and configuration; otherwise, false.
    /// </returns>
    private bool TryUseStep(out IAction? act)
    {
        act = null;
        if (!CanUseTechStep && !CanUseActiveStandard) return false;

        if (CanUseTechStep && act != TechnicalStepPvE)
        {
            act = TechnicalStepPvE;
            return true;
        }

        if (CanUseActiveStandard && act != ActiveStandard)
        {
            act = ActiveStandard;
        }
        return true;


    }

    /// <summary>
    /// Determines whether to finish a dance with the appropriate finisher (Technical Finish or Standard Finish)
    /// based on the current combat situation, player status, and configuration settings.
    /// </summary>
    /// <param name="act">
    /// An output parameter that will hold the IAction representing the dance finisher to be used if the method returns true; otherwise, it will be null.
    /// </param>
    /// <param name="isTechnical">
    /// A boolean value indicating whether to attempt finishing with a Technical Finish (true) or a Standard Finish (false).
    /// </param>
    /// <returns>
    /// True if a dance finisher action is determined to be used based on the current conditions and configuration; otherwise, false.
    /// </returns>
    private bool TryFinishDance(out IAction? act, bool isTechnical)
    {
        act = null;

        if (isTechnical)
        {
            if (!HasTechnicalStep || (HasTillana && !IsDancing))
            {
                return false;
            }

            if (CompletedSteps < 4)
            {
                return ExecuteStepGCD(out act);
            }

            if (CanTechnicalFinish && CanUseStepHoldCheck(TechHoldStrategy, true))
            {
                return QuadrupleTechnicalFinishPvE.CanUse(out act);
            }

            return StatusHelper.PlayerWillStatusEnd(1, true, StatusID.TechnicalStep)
                   && QuadrupleTechnicalFinishPvE.CanUse(out act);
        }

        if (!HasStandardStep && !HasFinishingMove)
        {
            return false;
        }

        if (HasStandardStep)
        {
            if (CompletedSteps < 2)
            {
                return ExecuteStepGCD(out act);
            }

            if (CanStandardFinish && CanUseStepHoldCheck(StandardHoldStrategy, false))
            {
                return DoubleStandardFinishPvE.CanUse(out act);
            }

            return StatusHelper.PlayerWillStatusEnd(1, true, StatusID.StandardStep)
                   && DoubleStandardFinishPvE.CanUse(out act);
        }

        if (CanUseStepHoldCheck(StandardHoldStrategy, false)
            || StatusHelper.PlayerWillStatusEnd(1, true, StatusID.FinishingMoveReady))
        {
            return FinishingMovePvE.CanUse(out act);
        }

        return false;
    }

    #endregion

    #region Burst GCD Logic
    /// <summary>
    /// Determines the optimal Global Cooldown (GCD) action to use during a burst phase,
    /// prioritizing dance finishers and procs based on the player's current status, available resources, and configuration settings.
    /// </summary>
    /// <param name="act">
    /// An output parameter that will hold the IAction representing the chosen GCD action to be used during the burst phase if the method returns true; otherwise, it will be null.
    /// </param>
    /// <returns>
    /// True if a GCD action is determined to be used during the burst phase based on the current conditions and configuration; otherwise, false.
    /// </returns>
    private bool TryUseBurstGCD(out IAction? act)
    {
        act = null;
        if (!IsBurstPhase) return false;
        if (Showtime) return TryUseStep(out act);
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
        if (!IsSaberDancePrimed || !HasDanceOfTheDawn || Showtime) return false;

        return DanceOfTheDawnPvE.CanUse(out act);
    }

    private bool TryUseTillana(out IAction? act)
    {
        act = null;
        if (!HasTillana || Showtime) return false;
        var blockTillana = false;
        if (ActiveStandardWillHaveCharge && HasLastDance)
        {
            if (Esprit >= SafeEspritThreshold)
            {
                blockTillana = true;
            }
        }

        if (IsSaberDancePrimed || Esprit >= RiskyEspritThreshold)
        {
            blockTillana = true;
        }

        return !blockTillana && TillanaPvE.CanUse(out act);
    }

    private bool TryUseLastDance(out IAction? act)
    {
        act = null;
        if (!HasLastDance) return false;

        return ShouldUseLastDance && LastDancePvE.CanUse(out act);
    }

    private bool TryUseStarfallDance(out IAction? act)
    {
        act = null;
        if (!HasStarfall
            || CanUseActiveStandard
            || (ActiveStandardWillHaveCharge && HasLastDance)
            || Showtime) return false;

        return !IsSaberDancePrimed && StarfallDancePvE.CanUse(out act);
    }

    #endregion

    #region Regular GCD Logic

    private bool TryUseFillerGCD(out IAction? act)
    {
        act = null;
        if (Showtime) return TryUseStep(out act);
        if (TryUseProcs(out act)) return true;
        if (TryUseSaberDance(out act)) return true;
        if (TryUseTillana(out act)) return true;
        if (TryUseFeatherGCD(out act)) return true;
        if (HasLastDance && TryUseLastDance(out act)) return true;
        return TryUseBasicGCD(out act);
    }

    private bool TryUseBasicGCD(out IAction? act)
    {
        act = null;
        if (Showtime) return TryUseStep(out act);
        if (BloodshowerPvE.CanUse(out act)) return true;
        if (FountainfallPvE.CanUse(out act)) return true;
        if (RisingWindmillPvE.CanUse(out act)) return true;
        if (ReverseCascadePvE.CanUse(out act)) return true;
        if (BladeshowerPvE.CanUse(out act)) return true;
        if (FountainPvE.CanUse(out act)) return true;
        if (WindmillPvE.CanUse(out act)) return true;
        return CascadePvE.CanUse(out act)
               || base.GeneralGCD(out act);
    }

    private bool TryUseFeatherGCD(out IAction? act)
    {
        act = null;
        if (!HasEnoughFeathers || Showtime) return false;

        var hasSilkenProcs = HasSilkenFlow || HasSilkenSymmetry;
        var hasFlourishingProcs = HasFlourishingFlow || HasFlourishingSymmetry;

        if (hasSilkenProcs || !hasFlourishingProcs || CanSaberDance) return SaberDancePvE.CanUse(out act);
        if (FountainPvE.CanUse(out act)) return true;
        return CascadePvE.CanUse(out act) || SaberDancePvE.CanUse(out act);
    }

    private bool TryUseSaberDance(out IAction? act)
    {
        act = null;
        if (Showtime || !CanSaberDance) return false;

        return IsSaberDancePrimed && SaberDancePvE.CanUse(out act);
    }

    private bool TryUseProcs(out IAction? act)
    {
        act = null;

        if (IsBurstPhase || !ShouldUseTechStep || Showtime) return false;

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
            case 3:
                return IsSaberDancePrimed ? TryUseSaberDance(out act) : TryUseBasicGCD(out act);
            case 2:
            case 1:
                if (HasAnyProc) return TryUseBasicGCD(out act);
                if (CanSaberDance) return SaberDancePvE.CanUse(out act);
                if (HasLastDance) return LastDancePvE.CanUse(out act);
                break;
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
                          !CanUseTechStep;

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
            if (!IsDancing && !InCombat) return false;

            var timing = GetTimingsArray();
            if (timing.Length == 0) return false;

            return PotsDuringStep switch
            {
                PotsDuringStepStrategy.BeforeStep => HasTechnicalStep || HasStandardStep,
                PotsDuringStepStrategy.AfterStep => CanTechnicalFinish || CanStandardFinish,
                _ => false
            };
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

            ColoredTextRow("Is Burst Phase", IsBurstPhase);
            ColoredTextRow("Is Dancing", IsDancing);
            ColoredTextRow("Can Weave", CanWeave);
        }

        if (ImGui.CollapsingHeader("Step Logic"))
        {
            ValueRow("Tech Hold Strategy", TechHoldStrategy);
            BoolRow("Tech Hold Check", CanUseStepHoldCheck(TechHoldStrategy, true));

            if (ImGui.TreeNode("Technical Step Blocking Reasons"))
            {
                var canUseTechStep = CanUseTechStep;
                ColoredTextRow("Can Use Technical Step", canUseTechStep);

                if (!canUseTechStep)
                {
                    ImGui.Indent();
                    ColoredTextRow("Should Use Tech Step", ShouldUseTechStep);
                    ColoredTextRow("Is Dancing", IsDancing);
                    ColoredTextRow("Has Tillana", HasTillana);
                    ColoredTextRow("Has To Refresh Standard", HasToRefreshStandardFinish);
                    ColoredTextRow("Devilment Ready", DevilmentReady);
                    ColoredTextRow("Timing OK", IsTimingOk(TechnicalRecastRemain, TechnicalStepPvE));
                    ImGui.Unindent();
                }
                ImGui.TreePop();
            }

            ImGui.Separator();

            ValueRow("Standard Hold Strategy", StandardHoldStrategy);
            BoolRow("Standard Hold Check", CanUseStepHoldCheck(StandardHoldStrategy, false));

            if (ImGui.TreeNode("Standard Step Blocking Reasons"))
            {
                var canUseStandard = CanUseActiveStandard;
                ColoredTextRow("Can Use Standard Step or Finishing Move", canUseStandard);

                if (!canUseStandard)
                {
                    ImGui.Indent();
                    ColoredTextRow("Active Standard Enabled", ActiveStandard.IsEnabled);
                    ColoredTextRow("In Burst Phase", IsBurstPhase);
                    ColoredTextRow("Can Use Standard In Burst", CanUseStandardStepInBurst);
                    ColoredTextRow("Can Use Based On Esprit", CanUseStandardBasedOnEsprit);
                    ColoredTextRow("Has Last Dance", HasLastDance);
                    ColoredTextRow("Can Spend Esprit Now", CanSpendEspritNow);
                    ValueRow("Esprit Threshold", EspritThreshold);
                    ValueRow("Current Esprit", Esprit);
                    ColoredTextRow("Timing OK", IsTimingOk(ActiveStandardRecastRemain, ActiveStandard));
                    ImGui.Unindent();
                }
                ImGui.TreePop();
            }
        }

        if (ImGui.CollapsingHeader("Saber Dance Blocking"))
        {
            var isSaberPrimed = IsSaberDancePrimed;
            ColoredTextRow("Saber Dance Primed", isSaberPrimed);

            if (!isSaberPrimed)
            {
                ImGui.Indent();
                BoolRow("Can Spend Esprit Now", CanSpendEspritNow);
                BoolRow("Can Saber Dance", CanSaberDance);
                BoolRow("Is Last GCD Tillana", IsLastGCD(ActionID.TillanaPvE));
                BoolRow("Active Standard Will Have Charge", ActiveStandardWillHaveCharge);
                BoolRow("Has Last Dance", HasLastDance);
                ImGui.Unindent();
            }

            var showtime = Showtime;
            if (showtime)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.Text("Saber Dance blocked by Showtime (active dance/recent dance action)");
                ImGui.PopStyleColor();
            }
        }

        if (ImGui.CollapsingHeader("Burst / Proc"))
        {
            BoolRow("Saber Dance Primed", IsSaberDancePrimed);
            BoolRow("Has Any Proc", HasAnyProc);
            BoolRow("Has Enough Feathers", HasEnoughFeathers);

            ImGui.Separator();
            BoolRow("TryUseSaberDance - Enough Esprit", Esprit >= SaberDanceEspritCost);
            BoolRow("TryUseSaberDance - Blocked (Tech/Dancing)", CanUseTechStep || IsDancing);
        }

        if (ImGui.CollapsingHeader("Potions"))
        {
            BoolRow("Potion Usage Enabled", PotionUsageEnabled);
            ValueRow("Potion Usage Preset", PotionUsagePresets);
            try
            {
                ColoredTextRow("Potion Condition Met", ChurinPotions.IsConditionMet());
                ColoredTextRow("Potion Can Use At Time", ChurinPotions.CanUseAtTime());
            }
            catch (Exception ex)
            {
                ImGui.Text($"Error evaluating potion conditions: {ex.Message}");
            }
        }

        if (ImGui.CollapsingHeader("Method Checks"))
        {
            ColoredTextRow("GeneralGCD -> Burst Path", IsBurstPhase);
            ColoredTextRow("GeneralGCD -> Step Path", !IsDancing && (CanUseTechStep || CanUseActiveStandard));
            ColoredTextRow("GeneralGCD -> Finish Dance Path", IsDancing);
            ColoredTextRow("GeneralGCD -> Filler Path", !IsBurstPhase && !IsDancing && !CanUseTechStep && !CanUseActiveStandard);
        }

        ImGui.Separator();

        ColoredTextRow("TryUseStep - Can Tech", CanUseTechStep);
        ColoredTextRow("TryUseStep - Can Standard", CanUseActiveStandard);
        ColoredTextRow("TryUseStep - Has Finishing Move", HasFinishingMove);
    }

    private static void BoolRow(string label, bool value)
    {
        ImGui.Text($"{label}: {(value ? "Yes" : "No")}");
    }
    private static void ColoredTextRow(string label, bool value)
    {
        var color = value ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text($"{label}: {(value ? "Yes" : "No")}");
        ImGui.PopStyleColor();
    }
    private static void ValueRow<T>(string label, T value)
    {
        if (value == null)
        {
            ImGui.Text($"{label}: N/A");
            return;
        }

        ImGui.Text($"{label}: {value}");
    }

    #endregion

}