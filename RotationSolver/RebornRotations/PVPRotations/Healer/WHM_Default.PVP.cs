namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/WHM_Default.PVP.cs")]

public class WHM_DefaultPVP : WhiteMageRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Aquaveil on other players")]
    public bool AquaveilEsuna { get; set; } = false;

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

        if (AquaveilEsuna && AquaveilPvP.CanUse(out action))
        {
            return true;
        }
        if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
        {
            if (AquaveilPvP.CanUse(out action))
            {
                if (AquaveilPvP.Target.Target == Player)
                {
                    return true;
                }
            }
        }

        if (PurifyPvP.CanUse(out action))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        return base.DefenseSingleAbility(nextGCD, out action);
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

        if (CureIiiPvP.CanUse(out action))
        {
            return true;
        }

        if (CureIiPvP.CanUse(out action))
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

        if (AfflatusMiseryPvP.CanUse(out action))
        {
            return true;
        }

        if (SeraphStrikePvP.CanUse(out action))
        {
            return true;
        }

        if (MiracleOfNaturePvP.CanUse(out action))
        {
            return true;
        }

        if (GlareIvPvP.CanUse(out action))
        {
            return true;
        }

        if (GlareIiiPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}