namespace RebornRotations.Ranged;

[Rotation("Default", CombatType.PvE, GameVersion = "7.20")]
[SourceCode(Path = "main/BasicRotations/Ranged/DNC_Default.cs")]
[Api(4)]
public sealed class DNC_Default : DancerRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    public bool HoldTechForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Holds Standard Step if no targets in range (Warning, will drift & Buff may fall off)")]
    public bool HoldStepForTargets { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Dance Partner Name (If empty or not found uses default dance partner priority)")]
    public string DancePartnerName { get; set; } = "";

    [RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilties during burst")]
    private bool BurstDefense { get; set; } = true;
    #endregion
    bool shouldUseLastDance = true;

    private static bool InBurstStatus => !Player.WillStatusEnd(0, true, StatusID.Devilment);

    #region Countdown Logic
    // Override the method for actions to be taken during countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        // If there are 15 or fewer seconds remaining in the countdown 
        if (remainTime <= 15)
        {
            // Attempt to use Standard Step if applicable
            if (StandardStepPvE.CanUse(out var act, skipAoeCheck: true)) return act;
            // Fallback to executing step GCD action if Standard Step is not used
            if (ExecuteStepGCD(out act)) return act;
        }
        // If none of the above conditions are met, fallback to the base class method
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    // Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.TechnicalFinish))
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }

        // Special handling if the last action was Quadruple Technical Finish and level requirement is met
        if (IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE) && TechnicalStepPvE.EnoughLevel)
        {
            // Attempt to use Devilment ignoring clipping checks
            if (DevilmentPvE.CanUse(out act)) return true;
        }
        // Similar handling for Double Standard Finish when level requirement is not met
        else if (IsLastGCD(ActionID.DoubleStandardFinishPvE) && !TechnicalStepPvE.EnoughLevel)
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }

        // Use burst medicine if cooldown for Technical Step has elapsed sufficiently
        if (TechnicalStepPvE.Cooldown.ElapsedAfter(115)
            && UseBurstMedicine(out act)) return true;

        //If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance, except for Devilment
        if (!IsDancing && !(StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118)))
            return base.EmergencyAbility(nextGCD, out act); // Fallback to base class method if none of the above conditions are met

        act = null;
        return false;
    }

    [RotationDesc(ActionID.CuringWaltzPvE, ActionID.ImprovisationPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (CuringWaltzPvE.CanUse(out act, usedUp: true)) return true;
        if (ImprovisationPvE.CanUse(out act, usedUp: true)) return true;
        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShieldSambaPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && ShieldSambaPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.EnAvantPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction act)
    {
        if (EnAvantPvE.CanUse(out act, usedUp: true)) return true;
        return false;
    }

    // Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        //If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance
        if (IsDancing || StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118)) return false;

        // Prevent triple weaving by checking if an action was just used
        if (nextGCD.AnimationLockTime > 0.75f) return false;

        // Skip using Flourish if Technical Step is about to come off cooldown
        if (!TechnicalStepPvE.Cooldown.ElapsedAfter(116) || TillanaPvE.CanUse(out act))
        {
            // Check for conditions to use Flourish
            if (((Player.HasStatus(true, StatusID.Devilment)) && (Player.HasStatus(true, StatusID.TechnicalFinish))) || ((!Player.HasStatus(true, StatusID.Devilment)) && (!Player.HasStatus(true, StatusID.TechnicalFinish))))
            {
                if (!Player.HasStatus(true, StatusID.ThreefoldFanDance) && FlourishPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        // Attempt to use Fan Dance III if available
        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;

        var hasProcs = Player.HasStatus(true, StatusID.SilkenFlow) || Player.HasStatus(true, StatusID.SilkenSymmetry) ||
                       Player.HasStatus(true, StatusID.FlourishingFlow) || Player.HasStatus(true, StatusID.FlourishingSymmetry);

        //Use all feathers on burst or if about to overcap
        if ((!DevilmentPvE.EnoughLevel || Player.HasStatus(true, StatusID.Devilment) || (Feathers > 3 && hasProcs)) && !Player.HasStatus(true, StatusID.ThreefoldFanDance))
        {
            if (FanDanceIiPvE.CanUse(out act)) return true;
            if (FanDancePvE.CanUse(out act)) return true;
        }

        // Other attacks
        if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (UseClosedPosition(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        // Attempt to use Closed Position if applicable
        if (!InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act))
        {

            if (DancePartnerName != "")
                foreach (var player in PartyMembers)
                    if (player.Name.ToString() == DancePartnerName)
                        ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);

            return true;
        }

        // Try to finish the dance if applicable
        if (FinishTheDance(out act))
        {
            return true;
        }

        // Execute a Step GCD if available
        if (ExecuteStepGCD(out act))
        {
            return true;
        }

        // Use Technical Step in burst mode if applicable
        if (HoldTechForTargets)
        {
            if (HasHostilesInMaxRange && IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))

            {
                return true;
            }
        }
        else
        {
            if (IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        // Attempt to use a general attack GCD if none of the above conditions are met
        if (AttackGCD(out act, Player.HasStatus(true, StatusID.Devilment)))
        {
            return true;
        }

        // Fallback to the base method if no custom GCD actions are found
        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Helper method to handle attack actions during GCD based on certain conditions
    private bool AttackGCD(out IAction? act, bool burst)
    {
        act = null;

        if (IsDancing) return false;
        // Use Dance of the Dawn immediately if available
        if (burst && DanceOfTheDawnPvE.CanUse(out act)) return true;

        if (HandleTillana(out act)) return true;

        if (TechnicalStepPvE.Cooldown.ElapsedAfter(103))
        {
            shouldUseLastDance = false;
        }

        if (TechnicalStepPvE.Cooldown.ElapsedAfter(1) && !TechnicalStepPvE.Cooldown.ElapsedAfter(103))
        {
            shouldUseLastDance = true;
        }

        if (burst)
        {
            // Make sure Starfall gets used before end of burst
            if (DevilmentPvE.Cooldown.ElapsedAfter(15) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

            // Make sure to FM with enough time left in burst window to LD and SFD while leaving a GCD for a Sabre if needed
            if (DevilmentPvE.Cooldown.ElapsedAfter(10) && FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (shouldUseLastDance && Esprit <= 90)
        {
            if (LastDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (HoldStepForTargets)
        {
            if (HasHostilesInRange && UseStandardStep(out act)) return true;
        }
        else
        {
            if (UseStandardStep(out act)) return true;
        }

        if (FinishingMovePvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (Esprit < 100 && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (Player.WillStatusEndGCD(3, 0, true, StatusID.Devilment) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

        // Further prioritized GCD abilities
        if ((burst || (Esprit >= 70 && !TechnicalStepPvE.Cooldown.ElapsedAfter(115))) && SaberDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

        bool standardReady = StandardStepPvE.Cooldown.ElapsedAfter(28);
        bool technicalReady = TechnicalStepPvE.Cooldown.ElapsedAfter(118);

        if (!(standardReady || technicalReady) &&
            (!shouldUseLastDance || !LastDancePvE.CanUse(out act, skipAoeCheck: true)))
        {
            if (BloodshowerPvE.CanUse(out act)) return true;
            if (FountainfallPvE.CanUse(out act)) return true;
            if (RisingWindmillPvE.CanUse(out act)) return true;
            if (ReverseCascadePvE.CanUse(out act)) return true;
            if (BladeshowerPvE.CanUse(out act)) return true;
            if (WindmillPvE.CanUse(out act)) return true;
            if (FountainPvE.CanUse(out act)) return true;
            if (CascadePvE.CanUse(out act)) return true;
        }

        return false;
    }

    // Method for Standard Step Logic
    private bool UseStandardStep(out IAction act)
    {
        // Attempt to use Standard Step if available and certain conditions are met
        if (!StandardStepPvE.CanUse(out act, skipAoeCheck: true)) return false;
        if (Player.WillStatusEnd(5f, true, StatusID.StandardFinish)) return true;

        // Check for hostiles in range and technical step conditions
        if (!HasHostilesInRange) return false;
        if (Player.HasStatus(true, StatusID.TechnicalFinish) && Player.WillStatusEndGCD(2, 0, true, StatusID.TechnicalFinish) || (TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5))) return false;

        return true;
    }

    // Helper method to decide usage of Closed Position based on specific conditions
    private bool UseClosedPosition(out IAction act)
    {
        // Attempt to use Closed Position if available and certain conditions are met
        if (!ClosedPositionPvE.CanUse(out act)) return false;

        if (InCombat && Player.HasStatus(true, StatusID.ClosedPosition))
        {
            // Check for party members with Closed Position status
            foreach (var friend in PartyMembers)
            {
                if (friend.HasStatus(true, StatusID.ClosedPosition_2026))
                {
                    // Use Closed Position if target is not the same as the friend with the status
                    if (ClosedPositionPvE.Target.Target != friend) return true;
                    break;
                }
            }
        }
        return false;
    }
    // Rewrite of method to hold dance finish until target is in range 14 yalms
    private bool FinishTheDance(out IAction? act)
    {
        bool areDanceTargetsInRange = AllHostileTargets.Any(hostile => hostile.DistanceToPlayer() < 14);

        // Check for Standard Step if targets are in range or status is about to end.
        if (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.StandardStep)) &&
            DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Check for Technical Step if targets are in range or status is about to end.
        if (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4 &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.TechnicalStep)) &&
            QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        act = null;
        return false;
    }

    // Handles the logic for using the Tillana.
    private bool HandleTillana(out IAction? act)
    {
        // Check if Devilment cannot be used
        if (!DevilmentPvE.CanUse(out _, skipComboCheck: true))
        {
            // Attempt to use Tillana, skipping the AoE check
            if (TillanaPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        // No Tillana action was performed
        act = null;
        return false;
    }
    #endregion
}
