namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/MCH_Default.PvP.cs")]

public sealed class MCH_DefaultPvP : MachinistRotation
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

        if (InCombat && BraveryPvP.CanUse(out action))
        {
            return true;
        }

        if (InCombat && DervishPvP.CanUse(out action))
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

        if (AnalysisPvP.CanUse(out action, usedUp: true))
        {
            if (nextGCD.IsTheSameTo(false, ActionID.DrillPvP, ActionID.BioblasterPvP, ActionID.AirAnchorPvP, ActionID.ChainSawPvP))
            {
                return true;
            }
        }

        if (WildfirePvP.CanUse(out action))
        {
            if (StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149))
            {
                return true;
            }
        }

        if (BishopAutoturretPvP.CanUse(out action))
        {
            return true;
        }

        if (EagleEyeShotPvP.CanUse(out action))
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

        if (FullMetalFieldPvP.CanUse(out action))
        {
            return true;
        }

        if (BlazingShotPvP.CanUse(out action))
        {
            if (StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149) && !StatusHelper.PlayerHasStatus(true, StatusID.Analysis))
            {
                return true;
            }
        }

        if (DrillPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (BioblasterPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (AirAnchorPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (ChainSawPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (ScattergunPvP.CanUse(out action))
        {
            if (!StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149))
            {
                return true;
            }
        }

        if (BlastChargePvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}