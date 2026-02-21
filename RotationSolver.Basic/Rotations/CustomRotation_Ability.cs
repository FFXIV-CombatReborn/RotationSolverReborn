using ECommons.DalamudServices;

namespace RotationSolver.Basic.Rotations;

public partial class CustomRotation
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

		if (Player == null)
		{
			return false;
		}

		if (WeaponRemain <= 0.5f && WeaponRemain > 0f)
		{
			return false;
		}

		if (DataCenter.Orbonne && IsLastAction(ActionID.HeavenlyShieldPvE) && DataCenter.IsAgriasCastingSpecialIndicator())
		{
			return false;
		}

        //if (DataCenter.Job == ECommons.ExcelServices.Job.PLD && !DataCenter.IsMoving && IsLastAction(ActionID.PassageOfArmsPvE) && StatusHelper.PlayerHasStatus(true, StatusID.PassageOfArms) && DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea))
        //{
        //    return false;
        //}

        //if (DataCenter.Job == ECommons.ExcelServices.Job.AST && !DataCenter.IsMoving && IsLastAction(ActionID.CollectiveUnconsciousPvE) && StatusHelper.PlayerHasStatus(true, StatusID.CollectiveUnconscious_848) && DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea))
        //{
        //    return false;
        //}

        if (DataCenter.Job == ECommons.ExcelServices.Job.BLU && !DataCenter.IsMoving && IsLastAction(ActionID.PhantomFlurryPvE) && StatusHelper.PlayerHasStatus(true, StatusID.PhantomFlurry) && !StatusHelper.PlayerWillStatusEnd(1, true, StatusID.PhantomFlurry))
		{
			return false;
		}

		IBaseAction.ForceEnable = true;
        if (act is IBaseAction a && a != null && !a.Info.IsRealGCD && a.CanUse(out _, usedUp: true, skipAoeCheck: true, skipStatusProvideCheck: true))
        {
            return true;
        }
        IBaseAction.ForceEnable = false;

        if (DataCenter.Job == ECommons.ExcelServices.Job.NIN && StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
		{
            return false;
        }

		if (DataCenter.IsPvP && Service.Config.PvpGuardControl && HasPVPGuard)
		{
			return false;
		}

		if (act is IBaseItem i && i.CanUse(out _, true))
        {
            return true;
        }

        if (!Service.Config.UseAbility || Player.TotalCastTime > 0 || (StatusHelper.PlayerHasStatus(false, StatusID.ShackledAbilities) && DataCenter.NumberOfPartyMembersInRangeOf(8) > 1))
        {
            act = null;
            return false;
        }

        if (!DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting) && DataCenter.CurrentDutyRotation?.EmergencyAbility(nextGCD, out act) == true)
        {
            return true;
        }
        if (!DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting) && EmergencyAbility(nextGCD, out act))
        {
            return true;
        }

        JobRole role = DataCenter.Role;

        IBaseAction.TargetOverride = TargetType.Interrupt;
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Interrupt) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
        {
            if (DataCenter.CurrentDutyRotation?.InterruptAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (MyInterruptAbility(role, nextGCD, out act))
            {
                return true;
            }
        }

        IBaseAction.TargetOverride = TargetType.Dispel;
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Dispel))
        {
            if (DataCenter.CurrentDutyRotation?.DispelAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (DispelAbility(nextGCD, out act))
            {
                return true;
            }
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
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.AntiKnockback) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
        {
            if (DataCenter.CurrentDutyRotation?.AntiKnockbackAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (AntiKnockback(role, nextGCD, out act))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Positional))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.Positional) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra) && !StatusHelper.PlayerHasStatus(true, StatusID.TrueNorth) && TrueNorthPvE.Cooldown.CurrentCharges > 0 && !IsLastAbility(false, TrueNorthPvE) && TrueNorthPvE.CanUse(out act, skipComboCheck: true, usedUp: true))
        {
            return true;
        }
        IBaseAction.ShouldEndSpecial = false;

        IBaseAction.TargetOverride = TargetType.Heal;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealAreaAbility))
        {
            IBaseAction.AllEmpty = true;
            IBaseAction.ShouldEndSpecial = true;
            if (DataCenter.CurrentDutyRotation?.HealAreaAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
            {
                if (HealAreaAbility(nextGCD, out act))
                {
                    return true;
                }
            }
            IBaseAction.AllEmpty = false;
            IBaseAction.ShouldEndSpecial = false;
        }

        if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealAreaAbility) && (CanHealAreaAbility || DataCenter.IsInOccultCrescentOp))
        {
            IBaseAction.AutoHealCheck = true;
            if (DataCenter.CurrentDutyRotation?.HealAreaAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
            {
                if (HealAreaAbility(nextGCD, out act))
                {
                    return true;
                }
            }
            IBaseAction.AutoHealCheck = false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealSingleAbility))
        {
            IBaseAction.AllEmpty = true;
            IBaseAction.ShouldEndSpecial = true;
            if (DataCenter.CurrentDutyRotation?.HealSingleAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
            {
                if (HealSingleAbility(nextGCD, out act))
                {
                    return true;
                }
            }
            IBaseAction.AllEmpty = false;
            IBaseAction.ShouldEndSpecial = false;
        }

        if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealSingleAbility) && (CanHealSingleAbility || DataCenter.IsInOccultCrescentOp))
        {
            IBaseAction.AutoHealCheck = true;
            if (DataCenter.CurrentDutyRotation?.HealSingleAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
            {
                if (HealSingleAbility(nextGCD, out act))
                {
                    return true;
                }
            }
            IBaseAction.AutoHealCheck = false;
        }

        IBaseAction.TargetOverride = null;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Speed))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Speed))
        {
            if (DataCenter.CurrentDutyRotation?.SpeedAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (SpeedAbility(nextGCD, out act))
            {
                return true;
            }
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
            if (DataCenter.CurrentDutyRotation?.DefenseAreaAbility(nextGCD, out act) == true)
            {
                return true;
            }
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
            if (DataCenter.CurrentDutyRotation?.DefenseSingleAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (DefenseSingleAbility(nextGCD, out act) 
                || (!DataCenter.IsHostileCastingToTank && !StatusHelper.PlayerHasStatus(true, StatusID.Vengeance) && !StatusHelper.PlayerHasStatus(true, StatusID.Damnation) && ArmsLengthPvE.CanUse(out act)))
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
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward) && Player != null && !StatusHelper.PlayerHasStatus(true, StatusID.Bind))
        {
            if (DataCenter.CurrentDutyRotation?.MoveForwardAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (MoveForwardAbility(nextGCD, out act))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.MoveBack))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveBack) && MoveBackAbility(nextGCD, out act))
        {
            if (DataCenter.CurrentDutyRotation?.MoveBackAbility(nextGCD, out act) == true)
            {
                return true;
            }
            if (MoveBackAbility(nextGCD, out act))
            {
                return true;
            }
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

        if (HasHostilesInRange && DataCenter.CurrentDutyRotation?.AttackAbility(nextGCD, out act) == true)
        {
            return true;
        }
        if (HasHostilesInRange && AttackAbility(nextGCD, out act))
        {
            return true;
        }

        if (DataCenter.CurrentDutyRotation?.GeneralAbility(nextGCD, out act) == true)
        {
            return true;
        }
        if (GeneralAbility(nextGCD, out act))
        {
            return true;
        }

        return UseMpPotion(nextGCD, out act) || GeneralUsingAbility(role, nextGCD, out act) || (DataCenter.AutoStatus.HasFlag(AutoStatus.Speed) && SpeedAbility(nextGCD, out act));
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
        switch (role)
        {
            case JobRole.Tank:
                if (InterjectPvE.CanUse(out act))
                {
                    return true;
                }

                break;

            case JobRole.Melee:
                if (LegSweepPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
                {
                    return true;
                }

                break;

            case JobRole.RangedPhysical:
                if (HeadGrazePvE.CanUse(out act))
                {
                    return true;
                }

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
        act = null;
        return false;
    }

    /// <summary>
    /// Determines if an interrupt ability can be used.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The resulting action.</param>
    /// <returns>True if the interrupt ability can be used; otherwise, false.</returns>
    protected virtual bool DispelAbility(IAction nextGCD, out IAction? act)
    {
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
        switch (role)
        {
            case JobRole.Tank:
                if (ArmsLengthPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.InnerStrength))
                {
                    return true;
                }

                break;
            case JobRole.Melee:
                if (ArmsLengthPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
                {
                    return true;
                }

                break;
            case JobRole.Healer:
            case JobRole.RangedMagical:
                if (SurecastPvE.CanUse(out act))
                {
                    return true;
                }

                break;
            case JobRole.RangedPhysical:
                if (ArmsLengthPvE.CanUse(out act))
                {
                    return true;
                }

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
                if (LowBlowPvE.CanUse(out act))
                {
                    return true;
                }

                break;

            case JobRole.Melee:
                if (SecondWindPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
                {
                    return true;
                }

                if (BloodbathPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.Mudra))
                {
                    return true;
                }

                break;

            case JobRole.Healer:
            case JobRole.RangedMagical:
                if (Job == ECommons.ExcelServices.Job.BLM)
                {
                    break;
                }

                if (LucidDreamingPvE.CanUse(out act))
                {
                    return true;
                }

                break;

            case JobRole.RangedPhysical:
                if (SecondWindPvE.CanUse(out act))
                {
                    return true;
                }

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
		#region PvP
		if (DataCenter.IsPvP)
		{
			if (PurifyPvP.CanUse(out act))
			{
				return true;
			}

			if (GuardPvP.CanUse(out act) && Player?.GetHealthRatio() <= Service.Config.HealthForGuard && !StatusHelper.PlayerHasStatus(true, StatusID.UndeadRedemption) && !StatusHelper.PlayerHasStatus(true, StatusID.InnerRelease_1303))
			{
				return true;
			}

			if (RecuperatePvP.CanUse(out act))
			{
				return true;
			}

			if (StandardissueElixirPvP.CanUse(out act))
			{
				return true;
			}
		}
		#endregion

		if (nextGCD is BaseAction action)
        {
            if (Role is JobRole.RangedMagical &&
                action.Info.CastTime >= 5 && IActionHelper.IsLastActionGCD() && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Role is JobRole.RangedMagical && !HasSwift && IActionHelper.IsLastActionGCD() && nextGCD.IsTheSameTo(true, ActionID.OccultCometPvE))
        {
            if (SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Service.Config.RaisePlayerBySwift && DataCenter.CanRaise() && IActionHelper.IsLastActionGCD() && nextGCD.IsTheSameTo(true, ActionID.RaisePvE, ActionID.EgeiroPvE, ActionID.ResurrectionPvE, ActionID.AscendPvE))
        {
            if (SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
        }

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
        if (PelotonPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (SprintPvE.CanUse(out act))
        {
            return true;
        }

        if (DataCenter.IsPvP && (!DataCenter.InCombat || (Service.Config.PvpAllowSprintWithoutTarget&& Svc.Targets.Target == null)) && SprintPvP.CanUse(out act))
        {
            return true;
        }

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
        }

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
        }

        act = null;
        return false;
    }
}
