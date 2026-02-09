namespace RotationSolver.RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/WAR_Default.PvP.cs")]

public sealed class WAR_DefaultPvP : WarriorRotation
{
    #region Configurations
    #endregion

    #region oGCDs
    [RotationDesc(ActionID.BloodwhettingPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
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