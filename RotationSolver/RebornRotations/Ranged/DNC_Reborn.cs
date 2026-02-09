using Dalamud.Interface.Colors;

namespace RotationSolver.RebornRotations.Ranged;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Ranged/DNC_Reborn.cs")]

public sealed class DNC_Reborn : DancerRotation
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
    private bool shouldUseLastDance = true;

    private static bool InBurstStatus => StatusHelper.PlayerHasStatus(true, StatusID.Devilment);

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text("InBurstStatus: " + InBurstStatus.ToString());
        ImGui.Text("ShouldUseLastDance Logic: " + shouldUseLastDance.ToString());
        ImGui.Text($"UseStandardStep Logic: {UseStandardStep(out _)}");
        ImGui.Text($"UseClosedPosition Logic: {UseClosedPosition(out _)}");
        ImGui.Text($"FinishTheDance Logic: {FinishTheDance(out _)}");
        ImGui.Text($"HandleTillana Logic: {HandleTillana(out _)}");
    }
    #endregion

    #region Countdown Logic
    // Override the method for actions to be taken during countdown phase of combat
    protected override IAction? CountDownAction(float remainTime)
    {
        // If there are 15 or fewer seconds remaining in the countdown 
        if (remainTime <= 15)
        {
            // Attempt to use Standard Step if applicable
            if (StandardStepPvE.CanUse(out IAction? act, skipAoeCheck: true))
            {
                return act;
            }
            // Fallback to executing step GCD action if Standard Step is not used
            if (ExecuteStepGCD(out act))
            {
                return act;
            }
        }
        // If none of the above conditions are met, fallback to the base class method
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    // Override the method for handling emergency abilities
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (UseClosedPosition(out act))
        {
            return true;
        }

        if (IsBurst && DevilmentPvE.CanUse(out act))
        {
            if (HasTechnicalFinish)
            {
                return true;
            }

            // Special handling if the last action was Quadruple Technical Finish and level requirement is met
            if (IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE) && TechnicalStepPvE.EnoughLevel)
            {
                // Attempt to use Devilment ignoring clipping checks
                return true;
            }
            // Similar handling for Double Standard Finish when level requirement is not met
            else if (IsLastGCD(ActionID.DoubleStandardFinishPvE) && !TechnicalStepPvE.EnoughLevel)
            {
                return true;
            }
        }

        // Use burst medicine if cooldown for Technical Step has elapsed sufficiently
        if (TechnicalStepPvE.Cooldown.ElapsedAfter(115)
            && UseBurstMedicine(out act))
        {
            return true;
        }

        //If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance, except for Devilment
        if (!IsDancing && !(StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118)))
        {
            return base.EmergencyAbility(nextGCD, out act); // Fallback to base class method if none of the above conditions are met
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CuringWaltzPvE, ActionID.ImprovisationPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (CuringWaltzPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (ImprovisationPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShieldSambaPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && ShieldSambaPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.EnAvantPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (EnAvantPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out act);
    }

    // Override the method for handling attack abilities
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        //If dancing or about to dance avoid using abilities to avoid animation lock delaying the dance
        if (IsDancing || StandardStepPvE.Cooldown.ElapsedAfter(28) || TechnicalStepPvE.Cooldown.ElapsedAfter(118))
        {
            return base.AttackAbility(nextGCD, out act);
        }

        // Prevent triple weaving by checking if an action was just used
        if (AnimationLock > 0.75f)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (IsBurst)
        {
            // Skip using Flourish if Technical Step is about to come off cooldown
            if (!TechnicalStepPvE.Cooldown.ElapsedAfter(116) || TillanaPvE.CanUse(out _))
            {
                // Check for conditions to use Flourish
                if ((HasDevilment && HasTechnicalFinish) || ((!HasDevilment) && (!HasTechnicalFinish)))
                {
                    if (!HasThreefoldFanDance && FlourishPvE.CanUse(out act))
                    {
                        return true;
                    }
                }
            }
        }

        // Attempt to use Fan Dance III if available
        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        bool hasProcs = HasSilkenFlow || HasSilkenSymmetry ||
                       HasFlourishingFlow || HasFlourishingSymmetry;

        //Use all feathers on burst or if about to overcap
        if ((!DevilmentPvE.EnoughLevel || HasDevilment || (Feathers > 3 && hasProcs)) && !HasThreefoldFanDance)
        {
            if (FanDanceIiPvE.CanUse(out act))
            {
                return true;
            }

            if (FanDancePvE.CanUse(out act))
            {
                return true;
            }
        }

        // Other attacks
        if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // Override the method for handling general Global Cooldown (GCD) actions
    protected override bool GeneralGCD(out IAction? act)
    {
        if (ClosedPositionPvE.EnoughLevel && !HasClosedPosition && ClosedPositionPvE.CanUse(out _))
        {
            return base.GeneralGCD(out act);
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
        if (AttackGCD(out act, HasDevilment))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    // Helper method to handle attack actions during GCD based on certain conditions
    private bool AttackGCD(out IAction? act, bool burst)
    {
        act = null;

        if (IsDancing)
        {
            return false;
        }
        // Use Dance of the Dawn immediately if available
        if (burst && DanceOfTheDawnPvE.CanUse(out act))
        {
            return true;
        }

        if (HandleTillana(out act))
        {
            return true;
        }

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
            if (DevilmentPvE.Cooldown.ElapsedAfter(15) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }

            // Make sure to FM with enough time left in burst window to LD and SFD while leaving a GCD for a Sabre if needed
            if (DevilmentPvE.Cooldown.ElapsedAfter(10) && FinishingMovePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (shouldUseLastDance && Esprit <= 90)
        {
            if (LastDancePvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        if (HoldStepForTargets)
        {
            if (HasHostilesInRange && UseStandardStep(out act))
            {
                return true;
            }
        }
        else
        {
            if (UseStandardStep(out act))
            {
                return true;
            }
        }

        if (FinishingMovePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (Esprit <= 100 && StarfallDancePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Devilment) && StarfallDancePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Further prioritized GCD abilities
        if ((burst || (Esprit >= 70 && !TechnicalStepPvE.Cooldown.ElapsedAfter(115))) && SaberDancePvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        bool standardReady = StandardStepPvE.Cooldown.ElapsedAfter(28);
        bool technicalReady = TechnicalStepPvE.Cooldown.ElapsedAfter(118);

        if (!(standardReady || technicalReady) &&
            (!shouldUseLastDance || !LastDancePvE.CanUse(out act, skipAoeCheck: true)))
        {
            if (BloodshowerPvE.CanUse(out act))
            {
                return true;
            }

            if (FountainfallPvE.CanUse(out act))
            {
                return true;
            }

            if (RisingWindmillPvE.CanUse(out act))
            {
                return true;
            }

            if (ReverseCascadePvE.CanUse(out act))
            {
                return true;
            }

            if (BladeshowerPvE.CanUse(out act))
            {
                return true;
            }

            if (WindmillPvE.CanUse(out act))
            {
                return true;
            }

            if (FountainPvE.CanUse(out act))
            {
                return true;
            }

            if (CascadePvE.CanUse(out act))
            {
                return true;
            }
        }

        return false;
    }

    // Method for Standard Step Logic
    private bool UseStandardStep(out IAction act)
    {
        // Attempt to use Standard Step if available and certain conditions are met
        if (!StandardStepPvE.CanUse(out act, skipAoeCheck: true))
        {
            return false;
        }

        if (StatusHelper.PlayerWillStatusEnd(5f, true, StatusID.StandardFinish))
        {
            return true;
        }

        // Check for hostiles in range and technical step conditions
        if (!HasHostilesInRange)
        {
            return false;
        }

        if (HasTechnicalFinish && StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.TechnicalFinish) || (TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneCharge(5)))
        {
            return false;
        }

        return true;
    }

    // Helper method to decide usage of Closed Position based on specific conditions
    private bool UseClosedPosition(out IAction act)
    {
        // Attempt to use Closed Position if available and certain conditions are met
        if (!ClosedPositionPvE.CanUse(out act))
        {
            return false;
        }

        // Attempt to use Closed Position with name if applicable
        if (!InCombat && !HasClosedPosition && ClosedPositionPvE.CanUse(out act))
        {
            if (DancePartnerName != "")
            {
                foreach (IBattleChara player in PartyMembers)
                {
                    if (player.Name.ToString() == DancePartnerName)
                    {
                        ClosedPositionPvE.Target = new TargetResult(player, [player], player.Position);
                    }
                }
            }

            return true;
        }

        if (!HasClosedPosition)
        {
            if (ClosedPositionPvE.CanUse(out act))
            {
                return true;
            }
        }
        return false;
    }

    // Rewrite of method to hold dance finish until target is in range 14 yalms
    private bool FinishTheDance(out IAction? act)
    {
        bool areDanceTargetsInRange = false;
        foreach (IBattleChara hostile in AllHostileTargets)
        {
            if (hostile.DistanceToPlayer() < 14)
            {
                areDanceTargetsInRange = true;
                break;
            }
        }

        // Check for Standard Step if targets are in range or status is about to end.
        if (HasStandardStep && CompletedSteps == 2 &&
            (areDanceTargetsInRange || StatusHelper.PlayerWillStatusEnd(1f, true, StatusID.StandardStep)) &&
            DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Check for Technical Step if targets are in range or status is about to end.
        if (HasTechnicalStep && CompletedSteps == 4 &&
            (areDanceTargetsInRange || StatusHelper.PlayerWillStatusEnd(1f, true, StatusID.TechnicalStep)) &&
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
            if (TillanaPvE.CanUse(out act, skipAoeCheck: true))
            {
                return true;
            }
        }

        // No Tillana action was performed
        act = null;
        return false;
    }
    #endregion
}
