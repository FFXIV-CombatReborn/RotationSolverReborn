namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/DNC_Default.PvP.cs")]

public sealed class DNC_DefaultPvP : DancerRotation
{
    #region Configurations
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (StatusHelper.PlayerHasStatus(true, StatusID.HoningDance))
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        if (ClosedPositionPvP.CanUse(out action) && !StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition_2026))
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

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? action)
    {
        if (HasHoningDance)
        {
            return base.HealAreaAbility(nextGCD, out action);
        }

        if (CuringWaltzPvP.CanUse(out action))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (HasHoningDance)
        {
            return base.AttackAbility(nextGCD, out action);
        }

        if (FanDancePvP.CanUse(out action))
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
        if (HasHoningDance)
        {
            return base.GeneralGCD(out action);
        }

        if (DanceOfTheDawnPvP.CanUse(out action))
        {
            return true;
        }

        if (StarfallDancePvP.CanUse(out action))
        {
            return true;
        }

        if (NumberOfHostilesInRangeOf(6) > 0 && HoningDancePvP.CanUse(out action) && !StatusHelper.PlayerHasStatus(true, StatusID.EnAvant))
        {
            return true;
        }

        if (SaberDancePvP.CanUse(out action))
        {
            return true;
        }

        if (FountainPvP.CanUse(out action))
        {
            return true;
        }

        if (CascadePvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}