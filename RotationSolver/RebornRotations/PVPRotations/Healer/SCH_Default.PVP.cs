namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/SCH_Default.PVP.cs")]

public class SCH_DefaultPVP : ScholarRotation
{
    #region Configurations

    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (ChainStratagemPvP.CanUse(out action) && Target.HasStatus(false, StatusID.Guard))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (DiabrosisPvP.CanUse(out action))
        {
            return true;
        }

        if (DeploymentTacticsPvP.CanUse(out action, usedUp: true) && Target.HasStatus(true, StatusID.Biolysis_3089))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? action)
    {
        if (ExpedientPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out action);
    }

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? action)
    {
        if (SummonSeraphPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool DefenseSingleGCD(out IAction? action)
    {
        if (StoneskinIiPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.DefenseSingleGCD(out action);
    }

    protected override bool HealSingleGCD(out IAction? action)
    {
        if (HaelanPvP.CanUse(out action))
        {
            return true;
        }

        if (AdloquiumPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.HealSingleGCD(out action);
    }

    protected override bool GeneralGCD(out IAction? action)
    {
        if (BiolysisPvP.CanUse(out action) && StatusHelper.PlayerHasStatus(true, StatusID.Recitation_3094))
        {
            return true;
        }

        if (AccessionPvP.CanUse(out action))
        {
            return true;
        }

        if (SeraphicHaloPvP.CanUse(out action))
        {
            return true;
        }

        if (BroilIvPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}