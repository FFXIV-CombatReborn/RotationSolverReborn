namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/SGE_Default.PVP.cs")]

public class SGE_DefaultPVP : SageRotation
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

        if (KardiaPvP.CanUse(out action))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.AttackAbility(nextGCD, out action);
        }

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
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleGCD(out action);
        }

        if (StoneskinIiPvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleGCD(out action);
    }

    protected override bool HealSingleGCD(out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.HealSingleGCD(out action);
        }

        if (HaelanPvP.CanUse(out action))
        {
            return true;
        }

        return base.HealSingleGCD(out action);
    }

    protected override bool GeneralGCD(out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.GeneralGCD(out action);
        }

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