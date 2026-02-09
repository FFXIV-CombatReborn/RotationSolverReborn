namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/SGE_Default.PVP.cs")]

public class SGE_DefaultPVP : SageRotation
{
    #region Configurations

    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (KardiaPvP.CanUse(out action))
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

        if (ToxikonPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool DefenseSingleGCD(out IAction? action)
    {
        if (StoneskinIiPvP.CanUse(out action))
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

        return base.HealSingleGCD(out action);
    }

    protected override bool GeneralGCD(out IAction? action)
    {

        if (PneumaPvP.CanUse(out action))
        {
            return true;
        }

        if (PsychePvP.CanUse(out action))
        {
            return true;
        }

        if (PhlegmaIiiPvP.CanUse(out action))
        {
            return true;
        }

        if (EukrasiaPvP.CanUse(out action, usedUp: true) && InCombat && !Target.HasStatus(true, StatusID.EukrasianDosisIii_3108))
        {
            return true;
        }

        if (DosisIiiPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}