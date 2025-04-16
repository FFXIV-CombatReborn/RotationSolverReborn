using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Configuration.Conditions;

namespace RotationSolver.Updaters;
internal static class StateUpdater
{
    private static bool CanUseHealAction =>
        // PvP
        DataCenter.IsPvP
        // Job
        || ((DataCenter.Role == JobRole.Healer || Service.Config.UseHealWhenNotAHealer)
        && Service.Config.AutoHeal
        && (DataCenter.InCombat && CustomRotation.IsLongerThan(Service.Config.AutoHealTimeToKill)
            || Service.Config.HealOutOfCombat));

    public static void UpdateState()
    {
        DataCenter.CommandStatus = StatusFromCmdOrCondition();
        DataCenter.AutoStatus = StatusFromAutomatic();
    }

    private static AutoStatus StatusFromAutomatic()
    {
        AutoStatus status = AutoStatus.None;

        // Get the user-defined order of AutoStatus flags
        var autoStatusOrder = OtherConfiguration.AutoStatusOrder?.ToArray() ?? Array.Empty<uint>();

        foreach (var autoStatus in autoStatusOrder)
        {
            switch (autoStatus)
            {
                case (uint)AutoStatus.NoCasting:
                    if (ShouldAddNoCasting())
                        status |= AutoStatus.NoCasting;
                    break;

                case (uint)AutoStatus.Dispel:
                    if (ShouldAddDispel())
                        status |= AutoStatus.Dispel;
                    break;

                case (uint)AutoStatus.Interrupt:
                    if (ShouldAddInterrupt())
                        status |= AutoStatus.Interrupt;
                    break;

                case (uint)AutoStatus.AntiKnockback:
                    if (ShouldAddAntiKnockback())
                        status |= AutoStatus.AntiKnockback;
                    break;

                case (uint)AutoStatus.Positional:
                    if (ShouldAddPositional())
                        status |= AutoStatus.Positional;
                    break;

                case (uint)AutoStatus.HealAreaAbility:
                    if (ShouldAddHealAreaAbility())
                        status |= AutoStatus.HealAreaAbility;
                    break;

                case (uint)AutoStatus.HealAreaSpell:
                    if (ShouldAddHealAreaSpell())
                        status |= AutoStatus.HealAreaSpell;
                    break;

                case (uint)AutoStatus.HealSingleAbility:
                    if (ShouldAddHealSingleAbility())
                        status |= AutoStatus.HealSingleAbility;
                    break;

                case (uint)AutoStatus.HealSingleSpell:
                    if (ShouldAddHealSingleSpell())
                        status |= AutoStatus.HealSingleSpell;
                    break;

                case (uint)AutoStatus.DefenseArea:
                    if (ShouldAddDefenseArea())
                        status |= AutoStatus.DefenseArea;
                    break;

                case (uint)AutoStatus.DefenseSingle:
                    if (ShouldAddDefenseSingle())
                        status |= AutoStatus.DefenseSingle;
                    break;

                case (uint)AutoStatus.Raise:
                    if (ShouldAddRaise())
                        status |= AutoStatus.Raise;
                    break;

                case (uint)AutoStatus.Provoke:
                    if (ShouldAddProvoke())
                        status |= AutoStatus.Provoke;
                    break;

                case (uint)AutoStatus.TankStance:
                    if (ShouldAddTankStance())
                        status |= AutoStatus.TankStance;
                    break;

                case (uint)AutoStatus.Speed:
                    if (ShouldAddSpeed())
                        status |= AutoStatus.Speed;
                    break;

                // Add other cases as needed
                default:
                    break;
            }
        }

        return status;
    }

    // Condition methods for each AutoStatus flag

    private static bool ShouldAddNoCasting()
    {
        return DataCenter.IsHostileCastingStop;
    }

    private static bool ShouldAddDispel()
    {
        if (DataCenter.DispelTarget != null)
        {
            foreach (var status in DataCenter.DispelTarget.StatusList)
            {
                if (StatusHelper.IsDangerous(status))
                {
                    return true;
                }
            }

            if (!DataCenter.HasHostilesInRange || Service.Config.DispelAll || DataCenter.IsPvP)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ShouldAddRaise()
    {
        return DataCenter.DeathTarget != null;
    }

    private static bool ShouldAddPositional()
    {
        if (DataCenter.Role == JobRole.Melee && ActionUpdater.NextGCDAction != null && Service.Config.AutoUseTrueNorth)
        {
            var id = ActionUpdater.NextGCDAction.ID;
            var target = ActionUpdater.NextGCDAction.Target.Target;

            if (ConfigurationHelper.ActionPositional.TryGetValue((ActionID)id, out var positional)
                && positional != target?.FindEnemyPositional()
                && target?.HasPositional() == true
                && !target.HasStatus(true, StatusID.DirectionalDisregard))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ShouldAddDefenseArea()
    {
        return DataCenter.InCombat && Service.Config.UseDefenseAbility && DataCenter.IsHostileCastingAOE;
    }

    private static bool ShouldAddDefenseSingle()
    {
        if (!DataCenter.InCombat || !Service.Config.UseDefenseAbility)
            return false;

        if (DataCenter.Role == JobRole.Healer)
        {
            foreach (var tank in DataCenter.PartyMembers)
            {
                int attackingTankCount = 0;
                foreach (var hostile in DataCenter.AllHostileTargets)
                {
                    if (hostile.TargetObjectId == tank.GameObjectId)
                    {
                        attackingTankCount++;
                    }
                }

                if (attackingTankCount == 1 && DataCenter.IsHostileCastingToTank)
                {
                    return true;
                }
            }
        }

        if (DataCenter.Role == JobRole.Tank)
        {
            bool movingHere = (float)DataCenter.NumberOfHostilesInRange / DataCenter.NumberOfHostilesInMaxRange > 0.3f;

            int tarOnMeCount = 0;
            int attackedCount = 0;
            foreach (var hostile in DataCenter.AllHostileTargets)
            {
                if (hostile.DistanceToPlayer() <= 3 && hostile.TargetObject == Player.Object)
                {
                    tarOnMeCount++;
                    if (ObjectHelper.IsAttacked(hostile))
                    {
                        attackedCount++;
                    }
                }
            }

            bool attacked = (float)attackedCount / tarOnMeCount > 0.7f;

            if (tarOnMeCount >= Service.Config.AutoDefenseNumber
                && Player.Object.GetHealthRatio() <= Service.Config.HealthForAutoDefense
                && movingHere && attacked)
            {
                return true;
            }

            if (DataCenter.IsHostileCastingToTank)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldAddHealAreaAbility()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
            return false;

        int singleAbility = ShouldHealSingle(StatusHelper.SingleHots,
            Service.Config.HealthSingleAbility,
            Service.Config.HealthSingleAbilityHot);

        bool canHealAreaAbility = singleAbility > 2;

        if (DataCenter.PartyMembers.Count > 2)
        {
            float ratio = GetHealingOfTimeRatio(Player.Object, StatusHelper.AreaHots);

            if (!canHealAreaAbility)
                canHealAreaAbility = DataCenter.PartyMembersDifferHP < Service.Config.HealthDifference
                && DataCenter.PartyMembersAverHP < Lerp(Service.Config.HealthAreaAbility, Service.Config.HealthAreaAbilityHot, ratio);
        }

        return canHealAreaAbility;
    }

    private static bool ShouldAddHealAreaSpell()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
            return false;

        int singleSpell = ShouldHealSingle(StatusHelper.SingleHots,
            Service.Config.HealthSingleSpell,
            Service.Config.HealthSingleSpellHot);

        bool canHealAreaSpell = singleSpell > 2;

        if (DataCenter.PartyMembers.Count > 2)
        {
            float ratio = GetHealingOfTimeRatio(Player.Object, StatusHelper.AreaHots);

            if (!canHealAreaSpell)
                canHealAreaSpell = DataCenter.PartyMembersDifferHP < Service.Config.HealthDifference
                && DataCenter.PartyMembersAverHP < Lerp(Service.Config.HealthAreaSpell, Service.Config.HealthAreaSpellHot, ratio);
        }

        return canHealAreaSpell;
    }

    private static bool ShouldAddHealSingleAbility()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
            return false;

        bool onlyHealSelf = Service.Config.OnlyHealSelfWhenNoHealer
            && DataCenter.Role != JobRole.Healer;

        if (onlyHealSelf)
        {
            return ShouldHealSingle(Player.Object, StatusHelper.SingleHots,
                Service.Config.HealthSingleAbility, Service.Config.HealthSingleAbilityHot);
        }
        else
        {
            int singleAbility = ShouldHealSingle(StatusHelper.SingleHots,
                Service.Config.HealthSingleAbility,
                Service.Config.HealthSingleAbilityHot);

            return singleAbility > 0;
        }
    }

    private static bool ShouldAddHealSingleSpell()
    {
        if (!DataCenter.HPNotFull || !CanUseHealAction)
            return false;

        bool onlyHealSelf = Service.Config.OnlyHealSelfWhenNoHealer
            && DataCenter.Role != JobRole.Healer;

        if (onlyHealSelf)
        {
            return ShouldHealSingle(Player.Object, StatusHelper.SingleHots,
                Service.Config.HealthSingleSpell, Service.Config.HealthSingleSpellHot);
        }
        else
        {
            int singleSpell = ShouldHealSingle(StatusHelper.SingleHots,
                Service.Config.HealthSingleSpell,
                Service.Config.HealthSingleSpellHot);

            return singleSpell > 0;
        }
    }

    private static bool ShouldAddAntiKnockback()
    {
        return DataCenter.InCombat && Service.Config.UseKnockback && DataCenter.AreHostilesCastingKnockback;
    }

    private static bool ShouldAddProvoke()
    {
        if (!DataCenter.InCombat)
            return false;

        if (DataCenter.Role == JobRole.Tank
            && (Service.Config.AutoProvokeForTank
                || CountAllianceTanks() < 2)
            && DataCenter.ProvokeTarget != null)
        {
            return true;
        }

        return false;
    }

    private static bool ShouldAddInterrupt()
    {
        return DataCenter.InCombat && DataCenter.InterruptTarget != null && Service.Config.InterruptibleMoreCheck;
    }

    private static bool ShouldAddTankStance()
    {
        if (!Service.Config.AutoTankStance || DataCenter.Role != JobRole.Tank)
            return false;

        if (!AnyAllianceTankWithStance() && !CustomRotation.HasTankStance)
        {
            return true;
        }

        return false;
    }

    private static bool ShouldAddSpeed()
    {
        return DataCenter.IsMoving && DataCenter.NotInCombatDelay && Service.Config.AutoSpeedOutOfCombat;
    }

    // Helper methods used in condition methods

    static float GetHealingOfTimeRatio(IBattleChara target, params StatusID[] statusIds)
    {
        const float buffWholeTime = 15;

        float buffTime = target.StatusTime(false, statusIds);

        return Math.Min(1, buffTime / buffWholeTime);
    }

    static int ShouldHealSingle(StatusID[] hotStatus, float healSingle, float healSingleHot)
    {
        int count = 0;
        foreach (var member in DataCenter.PartyMembers)
        {
            if (ShouldHealSingle(member, hotStatus, healSingle, healSingleHot))
            {
                count++;
            }
        }
        return count;
    }

    static bool ShouldHealSingle(IBattleChara target, StatusID[] hotStatus, float healSingle, float healSingleHot)
    {
        if (target == null) return false;

        float ratio = GetHealingOfTimeRatio(target, hotStatus);

        float h = target.GetHealthRatio();
        if (h == 0 || !target.NeedHealing()) return false;

        return h < Lerp(healSingle, healSingleHot, ratio);
    }

    static float Lerp(float a, float b, float ratio)
    {
        return a + (b - a) * ratio;
    }

    private static AutoStatus StatusFromCmdOrCondition()
    {
        var status = DataCenter.SpecialType switch
        {
            SpecialCommandType.NoCasting => AutoStatus.NoCasting,
            SpecialCommandType.HealArea => AutoStatus.HealAreaSpell
                                | AutoStatus.HealAreaAbility,
            SpecialCommandType.HealSingle => AutoStatus.HealSingleSpell
                                | AutoStatus.HealSingleAbility,
            SpecialCommandType.DefenseArea => AutoStatus.DefenseArea,
            SpecialCommandType.DefenseSingle => AutoStatus.DefenseSingle,
            SpecialCommandType.DispelStancePositional => AutoStatus.Dispel
                                | AutoStatus.TankStance
                                | AutoStatus.Positional,
            SpecialCommandType.RaiseShirk => AutoStatus.Raise
                                | AutoStatus.Shirk,
            SpecialCommandType.MoveForward => AutoStatus.MoveForward,
            SpecialCommandType.MoveBack => AutoStatus.MoveBack,
            SpecialCommandType.AntiKnockback => AutoStatus.AntiKnockback,
            SpecialCommandType.Burst => AutoStatus.Burst,
            SpecialCommandType.Speed => AutoStatus.Speed,
            _ => AutoStatus.None,
        };

        AddStatus(ref status, AutoStatus.HealAreaSpell | AutoStatus.HealAreaAbility, DataCenter.CurrentConditionValue.HealAreaConditionSet);
        AddStatus(ref status, AutoStatus.HealSingleSpell | AutoStatus.HealSingleAbility, DataCenter.CurrentConditionValue.HealSingleConditionSet);
        AddStatus(ref status, AutoStatus.DefenseArea, DataCenter.CurrentConditionValue.DefenseAreaConditionSet);
        AddStatus(ref status, AutoStatus.DefenseSingle, DataCenter.CurrentConditionValue.DefenseSingleConditionSet);

        AddStatus(ref status, AutoStatus.Dispel | AutoStatus.TankStance | AutoStatus.Positional,
            DataCenter.CurrentConditionValue.DispelStancePositionalConditionSet);
        AddStatus(ref status, AutoStatus.Raise | AutoStatus.Shirk, DataCenter.CurrentConditionValue.RaiseShirkConditionSet);
        AddStatus(ref status, AutoStatus.MoveForward, DataCenter.CurrentConditionValue.MoveForwardConditionSet);
        AddStatus(ref status, AutoStatus.MoveBack, DataCenter.CurrentConditionValue.MoveBackConditionSet);
        AddStatus(ref status, AutoStatus.AntiKnockback, DataCenter.CurrentConditionValue.AntiKnockbackConditionSet);

        if (!status.HasFlag(AutoStatus.Burst) && Service.Config.AutoBurst)
        {
            status |= AutoStatus.Burst;
        }
        AddStatus(ref status, AutoStatus.Speed, DataCenter.CurrentConditionValue.SpeedConditionSet);
        AddStatus(ref status, AutoStatus.NoCasting, DataCenter.CurrentConditionValue.NoCastingConditionSet);

        return status;
    }

    private static void AddStatus(ref AutoStatus status, AutoStatus flag, ConditionSet set)
    {
        AddStatus(ref status, flag, () => set.IsTrue(DataCenter.CurrentRotation));
    }

    private static void AddStatus(ref AutoStatus status, AutoStatus flag, Func<bool> getValue)
    {
        if (status.HasFlag(flag) || !getValue()) return;

        status |= flag;
    }

    private static int CountAllianceTanks()
    {
        int count = 0;
        foreach (var member in DataCenter.AllianceMembers)
        {
            if (member.IsJobCategory(JobRole.Tank))
            {
                count++;
            }
        }
        return count;
    }

    private static bool AnyAllianceTankWithStance()
    {
        foreach (var member in DataCenter.AllianceMembers)
        {
            if (member.IsJobCategory(JobRole.Tank) && member.CurrentHp != 0 && member.HasStatus(false, StatusHelper.TankStanceStatus))
            {
                return true;
            }
        }
        return false;
    }
}