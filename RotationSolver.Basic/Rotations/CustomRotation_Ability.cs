namespace RotationSolver.Basic.Rotations;

partial class CustomRotation
{
    /// <summary>
    /// Determines if an ability can be used based on various conditions.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if an ability can be used; otherwise, false.</returns>
    private bool Ability(IAction nextGCD, out IAction? act)
    {
        act = DataCenter.CommandNextAction;

        IBaseAction.ForceEnable = true;
        if (act is IBaseAction a && a != null && !a.Info.IsRealGCD && a.CanUse(out _, usedUp: true, skipAoeCheck: true))
        {
            return true;
        }
        IBaseAction.ForceEnable = false;

        if (act is IBaseItem i && i.CanUse(out _, true))
        {
            return true;
        }

        if (!Service.Config.UseAbility || Player.TotalCastTime > 0)
        {
            act = null;
            return false;
        }

        if (EmergencyAbility(nextGCD, out act))
        {
            return true;
        }

        var role = DataCenter.Role;

        IBaseAction.TargetOverride = TargetType.Interrupt;
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Interrupt) && !Player.HasStatus(true, StatusID.Mudra) && MyInterruptAbility(role, nextGCD, out act))
        {
            return true;
        }

        IBaseAction.TargetOverride = TargetType.Tank;
        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Shirk))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Shirk) && ShirkPvE.CanUse(out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        IBaseAction.TargetOverride = null;
        if (DataCenter.CommandStatus.HasFlag(AutoStatus.TankStance))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.TankStance) && (TankStance?.CanUse(out act) ?? false))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.AntiKnockback))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.AntiKnockback) && !Player.HasStatus(true, StatusID.Mudra) && AntiKnockback(role, nextGCD, out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Positional))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Positional) && !Player.HasStatus(true, StatusID.Mudra) && TrueNorthPvE.Cooldown.CurrentCharges > 0 && !IsLastAbility(true, TrueNorthPvE) && TrueNorthPvE.CanUse(out act, skipComboCheck: true, usedUp: true))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        IBaseAction.TargetOverride = TargetType.Heal;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealAreaAbility))
        {
            IBaseAction.AllEmpty = true;
            IBaseAction.ShouldEndSpecial = true;
            if (HealAreaAbility(nextGCD, out act))
            {
                return true;
            }
            IBaseAction.AllEmpty = false;
            IBaseAction.ShouldEndSpecial = false;
        }

        if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealAreaAbility) && CanHealAreaAbility)
        {
            IBaseAction.AutoHealCheck = true;
            if (HealAreaAbility(nextGCD, out act))
            {
                return true;
            }
            IBaseAction.AutoHealCheck = false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealSingleAbility))
        {
            IBaseAction.AllEmpty = true;
            IBaseAction.ShouldEndSpecial = true;
            if (HealSingleAbility(nextGCD, out act))
            {
                return true;
            }
            IBaseAction.AllEmpty = false;
            IBaseAction.ShouldEndSpecial = false;
        }

        if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealSingleAbility) && CanHealSingleAbility)
        {
            IBaseAction.AutoHealCheck = true;
            if (HealSingleAbility(nextGCD, out act))
            {
                return true;
            }
            IBaseAction.AutoHealCheck = false;
        }

        IBaseAction.TargetOverride = null;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Speed))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Speed) && SpeedAbility(nextGCD, out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Provoke))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Provoke))
        {
            if (!HasTankStance && (TankStance?.CanUse(out act) ?? false))
            {
                return true;
            }

            IBaseAction.TargetOverride = TargetType.Provoke;
            if (ProvokePvE.CanUse(out act) || ProvokeAbility(nextGCD, out act))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        IBaseAction.TargetOverride = TargetType.BeAttacked;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.DefenseArea))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea))
        {
            if (DefenseAreaAbility(nextGCD, out act) || (role is JobRole.Melee or JobRole.RangedPhysical or JobRole.RangedMagical && DefenseSingleAbility(nextGCD, out act)))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.DefenseSingle))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseSingle))
        {
            if (DefenseSingleAbility(nextGCD, out act) || (!DataCenter.IsHostileCastingToTank && ArmsLengthPvE.CanUse(out act)))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        IBaseAction.TargetOverride = null;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.MoveForward))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        IBaseAction.AllEmpty = true;
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward) && Player != null && !Player.HasStatus(true, StatusID.Bind) && MoveForwardAbility(nextGCD, out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.MoveBack))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveBack) && MoveBackAbility(nextGCD, out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;
        IBaseAction.AllEmpty = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealSingleAbility))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.HealSingleAbility) && UseHpPotion(nextGCD, out act))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        if (HasHostilesInRange && AttackAbility(nextGCD, out act))
        {
            return true;
        }

        if (GeneralAbility(nextGCD, out act))
        {
            return true;
        }

        if (UseMpPotion(nextGCD, out act))
        {
            return true;
        }

        if (GeneralUsingAbility(role, nextGCD, out act))
        {
            return true;
        }

        if (DataCenter.AutoStatus.HasFlag(AutoStatus.Speed) && SpeedAbility(nextGCD, out act))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if an interrupt ability can be used based on the job role.
    /// </summary>
    /// <param name="role">The job role of the player.</param>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the interrupt ability can be used; otherwise, false.</returns>
    private bool MyInterruptAbility(JobRole role, IAction nextGCD, out IAction? act)
    {
        act = null;
        switch (role)
        {
            case JobRole.Tank:
                if (InterjectPvE.CanUse(out act)) return true;
                break;

            case JobRole.Melee:
                if (LegSweepPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Mudra)) return true;
                break;

            case JobRole.RangedPhysical:
                if (HeadGrazePvE.CanUse(out act)) return true;
                break;

            default:
                // Handle unexpected job roles if necessary
                break;
        }
        return InterruptAbility(nextGCD, out act);
    }

    /// <summary>
    /// Determines if an interrupt ability can be used.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the interrupt ability can be used; otherwise, false.</returns>
    protected virtual bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.InterruptAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// Determines if an anti-knockback ability can be used based on the job role.
    /// </summary>
    /// <param name="role">The job role of the player.</param>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if an anti-knockback ability can be used; otherwise, false.</returns>
    private bool AntiKnockback(JobRole role, IAction nextGCD, out IAction? act)
    {
        act = null;
        switch (role)
        {
            case JobRole.Tank:
            case JobRole.Melee:
                if (ArmsLengthPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Mudra)) return true;
                break;
            case JobRole.Healer:
            case JobRole.RangedMagical:
                if (SurecastPvE.CanUse(out act)) return true;
                break;
            case JobRole.RangedPhysical:
                if (ArmsLengthPvE.CanUse(out act)) return true;
                break;
            default:
                // Handle unexpected job roles if necessary
                break;
        }

        return AntiKnockbackAbility(nextGCD, out act);
    }

    /// <summary>
    /// Determines if an anti-knockback ability can be used.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the anti-knockback ability can be used; otherwise, false.</returns>
    protected virtual bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.AntiKnockbackAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// Determines if a provoke ability can be used.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the provoke ability can be used; otherwise, false.</returns>
    protected virtual bool ProvokeAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.ProvokeAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// Determines if a general ability can be used based on the job role.
    /// </summary>
    /// <param name="role">The job role of the player.</param>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if a general ability can be used; otherwise, false.</returns>
    private bool GeneralUsingAbility(JobRole role, IAction nextGCD, out IAction? act)
    {
        act = null;
        switch (role)
        {
            case JobRole.Tank:
                if (LowBlowPvE.CanUse(out act)) return true;
                break;

            case JobRole.Melee:
                if (SecondWindPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Mudra)) return true;
                if (BloodbathPvE.CanUse(out act) && !Player.HasStatus(true, StatusID.Mudra)) return true;
                break;

            case JobRole.Healer:
            case JobRole.RangedMagical:
                if (Job == ECommons.ExcelServices.Job.BLM) break;
                if (LucidDreamingPvE.CanUse(out act)) return true;
                break;

            case JobRole.RangedPhysical:
                if (SecondWindPvE.CanUse(out act)) return true;
                break;

            default:
                // Handle unexpected job roles if necessary
                break;
        }
        return false;
    }


    /// <summary>
    /// Determines if an emergency ability can be used.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the emergency ability can be used; otherwise, false.</returns>
    protected virtual bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting))
        {
            act = null;
        }

        if (nextGCD is BaseAction action)
        {
            if (Role is JobRole.RangedMagical &&
                action.Info.CastTime >= 5 && IActionHelper.IsLastActionGCD() && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Raise))
        {
            if (Role is JobRole.Healer && IActionHelper.IsLastActionGCD() && (DataCenter.DefaultGCDRemain > Service.Config.SwiftcastBuffer) && nextGCD.IsTheSameTo(true, ActionID.RaisePvE, ActionID.EgeiroPvE, ActionID.ResurrectionPvE, ActionID.AscendPvE) && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (DataCenter.CurrentDutyRotation?.EmergencyAbility(nextGCD, out act) ?? false)
        {
            return true;
        }

        #region PvP
        if (GuardPvP.CanUse(out act) && Player.GetHealthRatio() <= Service.Config.HealthForGuard)
        {
            return true;
        }
        #endregion

        act = null;
        return false;
    }

    /// <summary>
    /// The ability that makes the character move forward.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.MoveForwardAbility)]
    protected virtual bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.MoveForwardAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that makes the character move back.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.MoveBackAbility)]
    protected virtual bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.MoveBackAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that heals a single character.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.HealSingleAbility)]
    protected virtual bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (RecuperatePvP.CanUse(out act)) return true;
        if (DataCenter.CurrentDutyRotation?.HealSingleAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that heals an area.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.HealAreaAbility)]
    protected virtual bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.HealAreaAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that defends a single character.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.DefenseSingleAbility)]
    protected virtual bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.DefenseSingleAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that defends an area.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.DefenseAreaAbility)]
    protected virtual bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.CurrentDutyRotation?.DefenseAreaAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that speeds up the character.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    [RotationDesc(DescType.SpeedAbility)]
    [RotationDesc(ActionID.SprintPvE)]
    protected virtual bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        if (PelotonPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (SprintPvE.CanUse(out act)) return true;

        if (DataCenter.CurrentDutyRotation?.SpeedAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }

    /// <summary>
    /// The ability that can be used anywhere.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    protected virtual bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting))
        {
            act = null;
        }

        if (DataCenter.CurrentDutyRotation?.GeneralAbility(nextGCD, out act) ?? false) return true;

        act = null;
        return false;
    }

    /// <summary>
    /// The ability that attacks an enemy.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the ability can be used; otherwise, false.</returns>
    protected virtual bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting))
        {
            act = null;
        }

        if (DataCenter.CurrentDutyRotation?.AttackAbility(nextGCD, out act) ?? false) return true;
        act = null;
        return false;
    }
}
