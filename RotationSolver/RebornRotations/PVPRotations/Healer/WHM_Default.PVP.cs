namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/WHM_Default.PVP.cs")]

public class WHM_DefaultPVP : WhiteMageRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Aquaveil on other players")]
    public bool AquaveilEsuna { get; set; } = false;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (AquaveilEsuna && AquaveilPvP.CanUse(out action))
        {
            return true;
        }
        if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
        {
            if (AquaveilPvP.CanUse(out action, targetOverride: TargetType.Self))
            {
				return true;
			}
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
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