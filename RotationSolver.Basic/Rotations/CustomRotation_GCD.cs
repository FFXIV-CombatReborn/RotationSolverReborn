namespace RotationSolver.Basic.Rotations;

public partial class CustomRotation
{
    /// <summary>
    /// Whether the player is currently doing nothing (and healing).
    /// </summary>
    public static bool HealingWhileDoingNothing =>
        _nextTimeToHeal + TimeSpan.FromSeconds(DataCenter.DefaultGCDTotal) > DateTime.Now;
    
    private static DateTime _nextTimeToHeal = DateTime.MinValue;
    private static readonly Random _random = new();

    private IAction? GCD()
    {
        IAction? act = DataCenter.CommandNextAction;

        IBaseAction.ForceEnable = true;
        if (act is IBaseAction a && a.Info.IsRealGCD
            && a.CanUse(out _, usedUp: true, skipAoeCheck: true, skipStatusProvideCheck: true))
        {
            return act;
        }

        IBaseAction.ForceEnable = false;

        if (StatusHelper.PlayerHasStatus(true, StatusID.Mudra) && DataCenter.DefaultGCDRemain >= 0.625f)
        {
            return null;
        }

        try
        {
            IBaseAction.ShouldEndSpecial = false;
            if (DataCenter.CurrentDutyRotation?.EmergencyGCD(out act) == true)
            {
                return act;
            }
            if (EmergencyGCD(out act))
            {
                return act;
            }

            if (DataCenter.CommandStatus.HasFlag(AutoStatus.Interrupt))
            {
                if (DataCenter.CurrentDutyRotation?.MyInterruptGCD(out act) == true)
                {
                    return act;
                }
                if (MyInterruptGCD(out IAction? action))
                {
                    return action;
                }
            }

            IBaseAction.TargetOverride = TargetType.Dispel;
            if (DataCenter.MergedStatus.HasFlag(AutoStatus.Dispel))
            {
                if (DataCenter.CurrentDutyRotation?.DispelGCD(out act) == true)
                {
                    return act;
                }
                if (DispelGCD(out IAction? action))
                {
                    return action;
                }
            }

            IBaseAction.TargetOverride = TargetType.Death;

            HardCastRaiseType hardcastraisetype = Service.Config.HardCastRaiseType;

            if (Service.Config.RaisePlayerFirst)
            {                
                if (RaiseSpell(out act, false))
                {
                    return act;
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastNormal && SwiftcastPvE.Cooldown.IsCoolingDown)
                {
                    if (RaiseSpell(out act, true))
                    {
                        return act;
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastSwiftCooldown)
                {
                    if (SwiftcastPvE.Cooldown.IsCoolingDown && Raise != null && Raise.Info.CastTime < SwiftcastPvE.Cooldown.RecastTimeRemainOneCharge)
                    {
                        if (RaiseSpell(out act, true))
                        {
                            return act;
                        }
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastOnlyHealer)
                {
                    var deadhealers = new HashSet<IBattleChara>();
                    if (DataCenter.PartyMembers != null)
                    {
                        foreach (var battleChara in DataCenter.PartyMembers.GetDeath())
                        {
                            if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                            {
                                deadhealers.Add(battleChara);
                            }
                        }
                    }

                    var allhealers = new HashSet<IBattleChara>();
                    if (DataCenter.PartyMembers != null)
                    {
                        foreach (var battleChara in DataCenter.PartyMembers)
                        {
                            if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                            {
                                allhealers.Add(battleChara);
                            }
                        }
                    }
                    if (RaiseSpell(out act, true) && deadhealers.Count == allhealers.Count && deadhealers.Count > 0)
                    {
                        return act;
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastOnlyHealerSwiftCooldown)
                {
                    if (SwiftcastPvE.Cooldown.IsCoolingDown && Raise != null && Raise.Info.CastTime < SwiftcastPvE.Cooldown.RecastTimeRemainOneCharge)
                    {
                        var deadhealers = new HashSet<IBattleChara>();
                        if (DataCenter.PartyMembers != null)
                        {
                            foreach (var battleChara in DataCenter.PartyMembers.GetDeath())
                            {
                                if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                                {
                                    deadhealers.Add(battleChara);
                                }
                            }
                        }

                        var allhealers = new HashSet<IBattleChara>();
                        if (DataCenter.PartyMembers != null)
                        {
                            foreach (var battleChara in DataCenter.PartyMembers)
                            {
                                if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                                {
                                    allhealers.Add(battleChara);
                                }
                            }
                        }
                        if (RaiseSpell(out act, true) && deadhealers.Count == allhealers.Count && deadhealers.Count > 0)
                        {
                            return act;
                        }
                    }
                }
            }

            IBaseAction.TargetOverride = null;

            if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward))
            {
                if (DataCenter.CurrentDutyRotation?.MoveForwardGCD(out act) == true)
                {
                    if (act is IBaseAction b && ObjectHelper.DistanceToPlayer(b.Target.Target) > 5)
                    {
                        return act;
                    }
                }
                if (MoveForwardGCD(out IAction? action))
                {
                    if (action is IBaseAction b && ObjectHelper.DistanceToPlayer(b.Target.Target) > 5)
                    {
                        return action;
                    }
                }
            }

            IBaseAction.TargetOverride = TargetType.Heal;

            if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealAreaSpell))
            {
                IBaseAction.AutoHealCheck = true;
                if (DataCenter.CurrentDutyRotation?.HealAreaGCD(out act) == true)
                    return act;

                if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
                {
                    if (HealAreaGCD(out IAction? action))
                    {
                        return action;
                    }
                }
                IBaseAction.AutoHealCheck = false;
            }
            if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealAreaSpell))
            {
                IBaseAction.AutoHealCheck = true;
                if (DataCenter.IsInOccultCrescentOp || HasVariantCure)
                {
                    if (DataCenter.CurrentDutyRotation?.HealAreaGCD(out act) == true)
                        return act;
                }

                if (CanHealAreaSpell)
                {
                    if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
                    {
                        if (HealAreaGCD(out IAction? action))
                        {
                            return action;
                        }
                    }
                }

                IBaseAction.AutoHealCheck = false;
            }

            if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealSingleSpell))
            {
                IBaseAction.AutoHealCheck = true;
                if (DataCenter.CurrentDutyRotation?.HealSingleGCD(out act) == true)
                    return act;

                if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
                {
                    if (HealSingleGCD(out IAction? action))
                    {
                        return action;
                    }
                }
                IBaseAction.AutoHealCheck = false;
            }
            if (DataCenter.AutoStatus.HasFlag(AutoStatus.HealSingleSpell))
            {
                IBaseAction.AutoHealCheck = true;
                if (DataCenter.CurrentDutyRotation?.HealSingleGCD(out act) == true)
                    return act;

                if (DataCenter.IsInOccultCrescentOp || HasVariantCure)
                {
                    if (DataCenter.CurrentDutyRotation?.HealSingleGCD(out act) == true)
                        return act;
                }

                if (CanHealSingleSpell)
                {
                    if (!StatusHelper.PlayerHasStatus(false, StatusID.Scalebound) && (!StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) || StatusHelper.PlayerHasStatus(false, StatusID.ShackledHealing) && DataCenter.NumberOfPartyMembersInRangeOf(21) == 1))
                    {
                        if (HealSingleGCD(out IAction? action))
                        {
                            return action;
                        }
                    }
                }

                IBaseAction.AutoHealCheck = false;
            }

            IBaseAction.TargetOverride = null;

            if (DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea))
            {
                if (DataCenter.CurrentDutyRotation?.DefenseAreaGCD(out act) == true)
                    return act;

                if (DefenseAreaGCD(out IAction? action))
                {
                    return action;
                }
            }

            IBaseAction.TargetOverride = TargetType.BeAttacked;

            if (DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseSingle))
            {
                if (DataCenter.CurrentDutyRotation?.DefenseSingleGCD(out act) == true)
                    return act;

                if (DefenseSingleGCD(out IAction? action))
                {
                    return action;
                }
            }

            IBaseAction.TargetOverride = TargetType.Death;

            if (!Service.Config.RaisePlayerFirst)
            {
                if (RaiseSpell(out act, false))
                {
                    return act;
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastNormal && SwiftcastPvE.Cooldown.IsCoolingDown)
                {
                    if (RaiseSpell(out act, true))
                    {
                        return act;
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastSwiftCooldown)
                {
                    if (SwiftcastPvE.Cooldown.IsCoolingDown && Raise != null && Raise.Info.CastTime < SwiftcastPvE.Cooldown.RecastTimeRemainOneCharge)
                    {
                        if (RaiseSpell(out act, true))
                        {
                            return act;
                        }
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastOnlyHealer)
                {
                    var deadhealers = new HashSet<IBattleChara>();
                    if (DataCenter.PartyMembers != null)
                    {
                        foreach (var battleChara in DataCenter.PartyMembers.GetDeath())
                        {
                            if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                            {
                                deadhealers.Add(battleChara);
                            }
                        }
                    }

                    var allhealers = new HashSet<IBattleChara>();
                    if (DataCenter.PartyMembers != null)
                    {
                        foreach (var battleChara in DataCenter.PartyMembers)
                        {
                            if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                            {
                                allhealers.Add(battleChara);
                            }
                        }
                    }
                    if (RaiseSpell(out act, true) && deadhealers.Count == allhealers.Count && deadhealers.Count > 0)
                    {
                        return act;
                    }
                }

                if (hardcastraisetype == HardCastRaiseType.HardCastOnlyHealerSwiftCooldown)
                {
                    if (SwiftcastPvE.Cooldown.IsCoolingDown && Raise != null && Raise.Info.CastTime < SwiftcastPvE.Cooldown.RecastTimeRemainOneCharge)
                    {
                        var deadhealers = new HashSet<IBattleChara>();
                        if (DataCenter.PartyMembers != null)
                        {
                            foreach (var battleChara in DataCenter.PartyMembers.GetDeath())
                            {
                                if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                                {
                                    deadhealers.Add(battleChara);
                                }
                            }
                        }

                        var allhealers = new HashSet<IBattleChara>();
                        if (DataCenter.PartyMembers != null)
                        {
                            foreach (var battleChara in DataCenter.PartyMembers)
                            {
                                if (TargetFilter.IsJobCategory(battleChara, JobRole.Healer) && !battleChara.IsPlayer())
                                {
                                    allhealers.Add(battleChara);
                                }
                            }
                        }
                        if (RaiseSpell(out act, true) && deadhealers.Count == allhealers.Count && deadhealers.Count > 0)
                        {
                            return act;
                        }
                    }
                }
            }

            IBaseAction.TargetOverride = null;

            IBaseAction.ShouldEndSpecial = false;
            IBaseAction.TargetOverride = null;

            if (!DataCenter.MergedStatus.HasFlag(AutoStatus.NoCasting))
            {
                if (DataCenter.CurrentDutyRotation?.GeneralGCD(out act) == true)
                    return act;

                if (GeneralGCD(out IAction? action))
                {
                    return action;
                }
            }

            if (Service.Config.HealWhenNothingTodo && InCombat)
            {
                // Please don't tell me someone's fps is less than 1!!
                if (DateTime.Now - _nextTimeToHeal > TimeSpan.FromSeconds(1))
                {
                    float min = Service.Config.HealWhenNothingTodoDelay.X;
                    float max = Service.Config.HealWhenNothingTodoDelay.Y;
                    _nextTimeToHeal = DateTime.Now + TimeSpan.FromSeconds((_random.NextDouble() * (max - min)) + min);
                }
                else if (_nextTimeToHeal < DateTime.Now)
                {
                    _nextTimeToHeal = DateTime.Now;

                    if (PartyMembersMinHP < Service.Config.HealWhenNothingTodoBelow)
                    {
                        IBaseAction.TargetOverride = TargetType.Heal;

                        if (DataCenter.PartyMembersDifferHP < Service.Config.HealthDifference)
                        {
                            int count = 0;
                            foreach (float hp in DataCenter.PartyMembersHP)
                            {
                                if (hp < 1)
                                {
                                    count++;
                                }
                            }
                            if (count > 2 && DataCenter.CurrentDutyRotation?.HealAreaGCD(out act) == true)
                            {
                                return act;
                            }
                            if (count > 2 && HealAreaGCD(out act))
                            {
                                return act;
                            }
                        }
                        if (DataCenter.CurrentDutyRotation?.HealSingleGCD(out act) == true)
                        {
                            return act;
                        }
                        if (HealSingleGCD(out act))
                        {
                            return act;
                        }

                        IBaseAction.TargetOverride = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"Exception in GCD method: {ex.Message}");
        }
        finally
        {
            // Ensure these are reset
            IBaseAction.ShouldEndSpecial = false;
            IBaseAction.TargetOverride = null;
        }

        return null;
    }

    private bool RaiseSpell(out IAction? act, bool mustUse)
    {
        act = null;

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Raise))
        {
            IBaseAction.ShouldEndSpecial = true;
            if (DataCenter.CurrentDutyRotation?.RaiseGCD(out act) == true)
            {
                return true;
            }
            if (StatusHelper.PlayerHasStatus(true, StatusID.PhantomChemist))
            {
                if (StatusHelper.PlayerStatusStack(true, StatusID.PhantomChemist) > 2)
                {
                    return false;
                }
            }
            if (RaiseGCD(out act))
            {
                return true;
            }
            if (RaiseAction(out act, false))
            {
                return true;
            }
        }
        IBaseAction.ShouldEndSpecial = false;

        if (!DataCenter.AutoStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (DataCenter.CurrentDutyRotation?.RaiseGCD(out act) == true)
        {
            return true;
        }
        if (StatusHelper.PlayerHasStatus(true, StatusID.PhantomChemist))
        {
            if (StatusHelper.PlayerStatusStack(true, StatusID.PhantomChemist) > 2)
            {
                return false;
            }
        }
        if (RaiseGCD(out act))
        {
            return true;
        }

        if (RaiseAction(out act, true))
        {
            if (HasSwift || IsLastAction(ActionID.SwiftcastPvE))
            {
                return true;
            }

            if (Service.Config.RaisePlayerBySwift && !SwiftcastPvE.Cooldown.IsCoolingDown && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }

            if (mustUse && !IsMoving)
            {
                return true;
            }
        }

        return false;

        bool RaiseAction(out IAction act, bool ignoreCastingCheck)
        {
            if (Raise?.CanUse(out act, skipCastingCheck: ignoreCastingCheck) ?? false)
            {
                return true;
            }

            act = null!;
            return false;
        }
    }

    /// <summary>
    /// Attempts to use the Interrupt GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    protected virtual bool MyInterruptGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        act = null; return false;
    }

    /// <summary>
    /// Attempts to use the Raise GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    protected virtual bool RaiseGCD(out IAction? act)
    {
        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Raise))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        act = null; return false;
    }

    /// <summary>
    /// Attempts to use the Dispel GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    protected virtual bool DispelGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.Dispel))
        {
            IBaseAction.ShouldEndSpecial = true;
        }
        if (!HasSwift && EsunaPvE.CanUse(out act))
        {
            return true;
        }

        IBaseAction.ShouldEndSpecial = false;
        return false;
    }

    /// <summary>
    /// Attempts to use the Emergency GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    protected virtual bool EmergencyGCD(out IAction? act)
    {
        #region PvP
        if (GuardPvP.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.UndeadRedemption) && !StatusHelper.PlayerHasStatus(true, StatusID.InnerRelease_1303) && (Player?.GetHealthRatio() <= Service.Config.HealthForGuard || DataCenter.CommandStatus.HasFlag(AutoStatus.Raise | AutoStatus.Shirk)))
        {
            return true;
        }

        if (StandardissueElixirPvP.CanUse(out act))
        {
            return true;
        }
        #endregion

        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        act = null!; return false;
    }

    /// <summary>
    /// Attempts to use the Move Forward GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    [RotationDesc(DescType.MoveForwardGCD)]
    protected virtual bool MoveForwardGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.MoveForward))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        act = null; return false;
    }

    /// <summary>
    /// Attempts to use the Heal Single GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    [RotationDesc(DescType.HealSingleGCD)]
    protected virtual bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealSingleSpell))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        act = null; return false;
    }

    /// <summary>
    /// Attempts to use the Heal Area GCD action.
    /// </summary>
    /// <param name="action">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    [RotationDesc(DescType.HealAreaGCD)]
    protected virtual bool HealAreaGCD(out IAction? action)
    {
        action = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.HealAreaSpell))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        action = null!; return false;
    }

    /// <summary>
    /// Attempts to use the Defense Single GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    [RotationDesc(DescType.DefenseSingleGCD)]
    protected virtual bool DefenseSingleGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.DefenseSingle))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        act = null!; return false;
    }

    /// <summary>
    /// Attempts to use the Defense Area GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    [RotationDesc(DescType.DefenseAreaGCD)]
    protected virtual bool DefenseAreaGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        if (DataCenter.CommandStatus.HasFlag(AutoStatus.DefenseArea))
        {
            IBaseAction.ShouldEndSpecial = true;
        }

        IBaseAction.ShouldEndSpecial = false;
        act = null; return false;
    }

    /// <summary>
    /// Attempts to use the General GCD action.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if the action can be used; otherwise, false.</returns>
    protected virtual bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (ShouldSkipAction())
        {
            return false;
        }

        act = null; return false;
    }

    private bool ShouldSkipAction()
    {
        return DataCenter.CommandStatus.HasFlag(AutoStatus.Raise) && Role is JobRole.Healer && (HasSwift || IsLastAction(ActionID.SwiftcastPvE));
    }
}
