namespace RotationSolver.RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/WAR_Default.PvP.cs")]

public sealed class WAR_DefaultPvP : WarriorRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        if (PurifyPvP.CanUse(out action))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.BloodwhettingPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        if (RampartPvP.CanUse(out action))
        {
            return true;
        }

        if (BloodwhettingPvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.AttackAbility(nextGCD, out action);
        }

        if (PrimalWrathPvP.CanUse(out action))
        {
            return true;
        }

        if (RampagePvP.CanUse(out action))
        {
            return true;
        }

        if (FullSwingPvP.CanUse(out action))
        {
            return true;
        }

        if (BlotaPvP.CanUse(out action))
        {
            return true;
        }

        if (OnslaughtPvP.CanUse(out action))
        {
            return true;
        }

        if (OrogenyPvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.GeneralGCD(out action);
        }

        if (InnerChaosPvP.CanUse(out action))
        {
            return true;
        }

        if (PrimalRuinationPvP.CanUse(out action))
        {
            return true;
        }

        if (PrimalRendPvP.CanUse(out action))
        {
            return true;
        }

        if (ChaoticCyclonePvP.CanUse(out action))
        {
            return true;
        }

        if (FellCleavePvP.CanUse(out action))
        {
            return true;
        }

        if (StormsPathPvP.CanUse(out action))
        {
            return true;
        }

        if (MaimPvP.CanUse(out action))
        {
            return true;
        }

        if (HeavySwingPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}