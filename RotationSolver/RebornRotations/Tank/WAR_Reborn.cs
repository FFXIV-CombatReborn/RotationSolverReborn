namespace RotationSolver.RebornRotations.Tank;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.35")]
[SourceCode(Path = "main/RebornRotations/Tank/WAR_Reborn.cs")]

public sealed class WAR_Reborn : WarriorRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Only use Nascent Flash if Tank Stance is off")]
    public bool NeverscentFlash { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Bloodwhetting/Raw intuition on single enemies")]
    public bool SoloIntuition { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Bloodwhetting/Raw intuition heal threshold")]
    public float HealIntuition { get; set; } = 0.7f;

    [RotationConfig(CombatType.PvE, Name = "Use both stacks of Onslaught during burst while standing still")]
    public bool YEETBurst { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use a stack of Onslaught when its about to overcap while standing still")]
    public bool YEETCooldown { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Primal Rend while moving (Dangerous)")]
    public bool YEET { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Primal Rend while standing still outside of configured melee range (Dangerous)")]
    public bool YEETStill { get; set; } = false;

    [Range(1, 20, ConfigUnitType.Yalms)]
    [RotationConfig(CombatType.PvE, Name = "Max distance you can be from the boss for Primal Rend use (Danger, setting too high will get you killed)")]
    public float PrimalRendDistance2 { get; set; } = 3.5f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Nascent Flash Heal Threshold")]
    public float FlashHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Thrill Of Battle Heal Threshold")]
    public float ThrillOfBattleHeal { get; set; } = 0.6f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Equilibrium Heal Threshold")]
    public float EquilibriumHeal { get; set; } = 0.6f;

    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < 0.54f && TomahawkPvE.CanUse(out IAction? act))
        {
            return act;
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (InfuriatePvE.CanUse(out act, gcdCountForAbility: 3))
        {
            return true;
        }

        if (!InnerReleasePvE.EnoughLevel && Player.HasStatus(true, StatusID.Berserk) && InfuriatePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (CombatElapsedLessGCD(1))
        {
            return false;
        }

        if (!Player.WillStatusEndGCD(2, 0, true, StatusID.SurgingTempest)
            || !StormsEyePvE.EnoughLevel)
        {
            if (InnerReleasePvE.CanUse(out act))
            {
                return true;
            }
            if (!InnerReleasePvE.Info.EnoughLevelAndQuest() && BerserkPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (IsBurstStatus && (InnerReleaseStacks == 0 || InnerReleaseStacks == 3))
        {
            if (InfuriatePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        if (CombatElapsedLessGCD(4))
        {
            return false;
        }

        if (OrogenyPvE.CanUse(out act))
        {
            return true;
        }

        if (UpheavalPvE.CanUse(out act))
        {
            return true;
        }

        if (Player.HasStatus(false, StatusID.Wrathful) && PrimalWrathPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (YEETBurst && OnslaughtPvE.CanUse(out act, usedUp: IsBurstStatus) &&
           !IsMoving &&
           !IsLastAction(false, OnslaughtPvE) &&
           !IsLastAction(false, UpheavalPvE) &&
            Player.HasStatus(true, StatusID.SurgingTempest))
        {
            return true;
        }

        if (YEETCooldown && OnslaughtPvE.CanUse(out act, usedUp: true) &&
           !IsMoving &&
           !IsLastAction(false, OnslaughtPvE) &&
           OnslaughtPvE.Cooldown.WillHaveXChargesGCD(OnslaughtMax, 1) &&
            Player.HasStatus(true, StatusID.SurgingTempest))
        {
            return true;
        }

        if (MergedStatus.HasFlag(AutoStatus.MoveForward) && MoveForwardAbility(nextGCD, out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if ((InCombat && Player.GetHealthRatio() < HealIntuition && NumberOfHostilesInRange > 0) || (InCombat && PartyMembers.Count() is 1 && NumberOfHostilesInRange > 0))
        {
            if (BloodwhettingPvE.CanUse(out act))
            {
                return true;
            }
            if (!BloodwhettingPvE.Info.EnoughLevelAndQuest() && RawIntuitionPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (Player.GetHealthRatio() < ThrillOfBattleHeal)
        {
            if (ThrillOfBattlePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!Player.HasStatus(true, StatusID.Holmgang_409))
        {
            if (Player.GetHealthRatio() < EquilibriumHeal)
            {
                if (EquilibriumPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        if (Player.HasStatus(true, StatusID.PrimalRendReady) && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }
        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShakeItOffPvE, ActionID.ReprisalPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (ShakeItOffPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RawIntuitionPvE, ActionID.VengeancePvE, ActionID.RampartPvE, ActionID.RawIntuitionPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        bool RawSingleTargets = SoloIntuition;
        act = null;

        if (Player.HasStatus(true, StatusID.Holmgang_409) && Player.GetHealthRatio() < 0.3f)
        {
            return false;
        }

        if (RawIntuitionPvE.CanUse(out act) && (RawSingleTargets || NumberOfHostilesInRange > 2))
        {
            return true;
        }

        if (!Player.WillStatusEndGCD(0, 0, true, StatusID.Bloodwhetting, StatusID.RawIntuition))
        {
            return false;
        }

        if (ReprisalPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && VengeancePvE.CanUse(out act))
        {
            return true;
        }

        if (((VengeancePvE.Cooldown.IsCoolingDown && VengeancePvE.Cooldown.ElapsedAfter(60)) || !VengeancePvE.EnoughLevel) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ShakeItOffPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (ShakeItOffPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (!Player.WillStatusEndGCD(3, 0, true, StatusID.SurgingTempest))
        {
            if (ChaoticCyclonePvE.CanUse(out act))
            {
                return true;
            }

            if (InnerChaosPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!Player.WillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) && !Player.HasStatus(true, StatusID.NascentChaos) && InnerReleaseStacks > 0)
        {
            if (DecimatePvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
            if (!DecimatePvE.Info.EnoughLevelAndQuest() && SteelCyclonePvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }

            if (FellCleavePvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
            if (!FellCleavePvE.Info.EnoughLevelAndQuest() && InnerBeastPvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
        }

        if (!Player.WillStatusEndGCD(3, 0, true, StatusID.SurgingTempest) && InnerReleaseStacks == 0)
        {
            if (PrimalRendPvE.CanUse(out act, skipAoeCheck: true))
            {
                if (PrimalRendPvE.Target.Target != null && PrimalRendPvE.Target.Target.DistanceToPlayer() <= PrimalRendDistance2)
                {
                    return true;
                }
                if (YEET || (YEETStill && !IsMoving))
                {
                    return true;
                }
            }
            if (PrimalRuinationPvE.CanUse(out act))
            {
                return true;
            }
        }

        // AOE
        if (!Player.WillStatusEndGCD(3, 0, true, StatusID.SurgingTempest))
        {
            if (DecimatePvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
            if (!DecimatePvE.Info.EnoughLevelAndQuest() && SteelCyclonePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (MythrilTempestPvE.CanUse(out act))
        {
            return true;
        }

        if (OverpowerPvE.CanUse(out act))
        {
            return true;
        }

        // Single Target
        if (!Player.WillStatusEndGCD(3, 0, true, StatusID.SurgingTempest))
        {
            if (FellCleavePvE.CanUse(out act, skipStatusProvideCheck: true))
            {
                return true;
            }
            if (!FellCleavePvE.Info.EnoughLevelAndQuest() && InnerBeastPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (StormsEyePvE.CanUse(out act))
        {
            return true;
        }

        if (StormsPathPvE.CanUse(out act))
        {
            return true;
        }

        if (MaimPvE.CanUse(out act))
        {
            return true;
        }

        if (HeavySwingPvE.CanUse(out act))
        {
            return true;
        }

        // Ranged
        if (TomahawkPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    [RotationDesc(ActionID.NascentFlashPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        if (!NeverscentFlash && NascentFlashPvE.CanUse(out act)
            && (InCombat && NascentFlashPvE.Target.Target?.GetHealthRatio() < FlashHeal))
        {
            return true;
        }

        if (NeverscentFlash && NascentFlashPvE.CanUse(out act)
            && (InCombat && !Player.HasStatus(true, StatusID.Defiance) && NascentFlashPvE.Target.Target?.GetHealthRatio() < FlashHeal))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }
    #endregion

    #region Extra Methods
    private static bool IsBurstStatus => !Player.WillStatusEndGCD(0, 0, false, StatusID.InnerStrength);
    #endregion
}