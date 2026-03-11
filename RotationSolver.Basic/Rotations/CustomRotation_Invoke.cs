using ECommons.Logging;

namespace RotationSolver.Basic.Rotations;

public partial class CustomRotation
{
    /// <inheritdoc/>
    public bool TryInvoke(out IAction? newAction, out IAction? gcdAction)
    {
        newAction = gcdAction = null;
        if (!IsEnabled)
        {
            return false;
        }

        try
        {
            UpdateInfo(); // Rotation specific info updates
            IBaseAction.ActionPreview = true;
            if (DataCenter.DrawingActions)
            {
                UpdateActions(Role);
            }
            IBaseAction.ActionPreview = false;

            CountingOfLastUsing = CountingOfCombatTimeUsing = 0;
            newAction = Invoke(out gcdAction);
            // Track when the next GCD action (IBaseAction) returned by Invoke changes.
            try
            {
                uint gcdId = 0;
                if (gcdAction is IBaseAction ib)
                    gcdId = ib.AdjustedID;
                if (gcdId != _lastNextGCDId)
                {
                    _lastNextGCDId = gcdId;
                    _lastNextGCDChange = DateTime.Now;
                }
            }
            catch
            {
                // ignore any errors during tracking
            }
            if (InCombat || CountOfTracking == 0)
            {
                AverageCountOfLastUsing =
                    ((AverageCountOfLastUsing * CountOfTracking) + CountingOfLastUsing)
                    / ++CountOfTracking;
                MaxCountOfLastUsing = Math.Max(MaxCountOfLastUsing, CountingOfLastUsing);

                AverageCountOfCombatTimeUsing =
                    ((AverageCountOfCombatTimeUsing * (CountOfTracking - 1)) + CountingOfCombatTimeUsing)
                    / CountOfTracking;
                MaxCountOfCombatTimeUsing = Math.Max(MaxCountOfCombatTimeUsing, CountingOfCombatTimeUsing);
            }

            if (!IsValid)
            {
                IsValid = true;
            }
        }
        catch (Exception? ex)
        {
            WhyNotValid = "Failed to invoke the next action, please contact support.";

            while (ex != null)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    WhyNotValid += "\n" + ex.Message;
                }

                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    WhyNotValid += "\n" + ex.StackTrace;
                }

                ex = ex.InnerException;
            }

            // Log the exception details
            PluginLog.Error(WhyNotValid);

            IsValid = false;
        }

        return newAction != null;
    }

    private void UpdateActions(JobRole role)
    {
        ActionMoveForwardGCD = MoveForwardGCD(out IAction? act) ? act : null;

        UpdateHealingActions(role, out _);
        UpdateDefenseActions(out _);
        UpdateDispelAndRaiseActions(role, out _);
        UpdatePositionalActions(role, out _);
        UpdateMovementActions(out _);
    }

    private void UpdateHealingActions(JobRole role, out IAction? act)
    {
        act = null; // Ensure 'act' is assigned before any return

        try
        {
            if (!DataCenter.HPNotFull && role == JobRole.Healer)
            {
                ActionHealAreaGCD = ActionHealAreaAbility = ActionHealSingleGCD = ActionHealSingleAbility = null;
            }
            else
            {
                ActionHealAreaGCD = HealAreaGCD(out act) ? act : null;
                ActionHealSingleGCD = HealSingleGCD(out act) ? act : null;

                ActionHealAreaAbility = HealAreaAbility(AddlePvE, out act) ? act : null;
                ActionHealSingleAbility = HealSingleAbility(AddlePvE, out act) ? act : null;
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            PluginLog.Error($"Exception in UpdateHealingActions method: {ex.Message}");
            // Optionally, set actions to null in case of an exception
            ActionHealAreaGCD = ActionHealAreaAbility = ActionHealSingleGCD = ActionHealSingleAbility = null;
        }
    }

    private void UpdateDefenseActions(out IAction? act)
    {
        IBaseAction.TargetOverride = TargetType.BeAttacked;
        ActionDefenseAreaGCD = DefenseAreaGCD(out act) ? act : null;
        ActionDefenseSingleGCD = DefenseSingleGCD(out act) ? act : null;
        IBaseAction.TargetOverride = null;

        try
        {
            ActionDefenseAreaAbility = DefenseAreaAbility(AddlePvE, out act) ? act : null;
            ActionDefenseSingleAbility = DefenseSingleAbility(AddlePvE, out act) ? act : null;
        }
        catch (MissingMethodException ex)
        {
            // Log the exception or handle it as needed
            _ = BasicWarningHelper.AddSystemWarning($"Exception in UpdateDefenseActions method: {ex.Message}");
            // Optionally, set actions to null in case of an exception
            ActionDefenseAreaAbility = ActionDefenseSingleAbility = null;
        }
    }

    private void UpdateDispelAndRaiseActions(JobRole role, out IAction? act)
    {
        act = null; // Ensure 'act' is assigned before any return

        IBaseAction.TargetOverride = TargetType.Dispel;
        ActionDispelStancePositionalGCD = role switch
        {
            JobRole.Healer => DataCenter.DispelTarget != null && DispelGCD(out act) ? act : null,
            _ => null,
        };
        IBaseAction.TargetOverride = null;

        IBaseAction.TargetOverride = TargetType.Death;
        ActionRaiseShirkGCD = role switch
        {
            JobRole.Healer => DataCenter.DeathTarget != null && RaiseSpell(out act, true) ? act : null,
            _ => null,
        };
        IBaseAction.TargetOverride = null;
    }

    private void UpdatePositionalActions(JobRole role, out IAction? act)
    {
        act = null; // Ensure 'act' is assigned before any return

        ActionDispelStancePositionalAbility = role switch
        {
            JobRole.Melee => TrueNorthPvE.CanUse(out act) ? act : null,
            JobRole.Tank => TankStance?.CanUse(out act) ?? false ? act : null,
            _ => null,
        };

        ActionRaiseShirkAbility = role switch
        {
            JobRole.Tank => ShirkPvE.CanUse(out act) ? act : null,
            _ => null,
        };

        ActionAntiKnockbackAbility = AntiKnockback(role, AddlePvE, out act) ? act : null;
    }

    private void UpdateMovementActions(out IAction? act)
    {
        IBaseAction.TargetOverride = TargetType.Move;
        bool movingTarget = MoveForwardAbility(AddlePvE, out act);
        IBaseAction.TargetOverride = null;
        ActionMoveForwardAbility = movingTarget ? act : null;

        ActionMoveBackAbility = MoveBackAbility(AddlePvE, out act) ? act : null;
        ActionSpeedAbility = SpeedAbility(AddlePvE, out act) ? act : null;
    }

    private IAction? Invoke(out IAction? gcdAction)
    {
        // Initialize the output parameter
        gcdAction = null;

        // Reset special action flags
        IBaseAction.ShouldEndSpecial = false;
        IBaseAction.IgnoreClipping = true;

        try
        {
            // Check for countdown and return the appropriate action if not in combat
            float countDown = Service.CountDownTime;
            if (countDown > 0 && !DataCenter.InCombat)
            {
                return CountDownAction(countDown);
            }

            // Reset target override
            IBaseAction.TargetOverride = null;

            // Attempt to get the GCD action
            gcdAction = GCD();
            IBaseAction.IgnoreClipping = false;

            // If a GCD action is available, determine if it can be used or if an ability should be used instead
            if (gcdAction != null)
            {
                return ActionHelper.CanUseGCD ? gcdAction : Ability(gcdAction, out IAction? ability) ? ability : gcdAction;
            }
            else
            {
                // If no GCD action is available, attempt to use an ability
                IBaseAction.IgnoreClipping = true;
                if (Ability(AddlePvE, out IAction? ability))
                {
                    return ability;
                }
                IBaseAction.IgnoreClipping = false;

                return null;
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"Exception in Invoke method: {ex.Message}");
            return null;
        }
        finally
        {
            // Ensure IgnoreClipping is reset
            IBaseAction.IgnoreClipping = false;
        }
    }

    /// <summary>
    /// The action in countdown.
    /// </summary>
    /// <param name="remainTime"></param>
    /// <returns></returns>
    protected virtual IAction? CountDownAction(float remainTime)
    {
        return null;
    }
}
