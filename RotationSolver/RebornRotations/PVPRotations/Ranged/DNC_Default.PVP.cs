namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/DNC_Default.PvP.cs")]

public sealed class DNC_DefaultPvP : DancerRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        if (Player.HasStatus(true, StatusID.HoningDance))
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        if (PurifyPvP.CanUse(out action))
        {
            return true;
        }

        if (ClosedPositionPvP.CanUse(out action) && !Player.HasStatus(true, StatusID.ClosedPosition_2026))
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
        if (!RespectGuard || !Player.HasStatus(true, StatusID.Guard))
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return base.HealAreaAbility(nextGCD, out action);
        }

        if (Player.HasStatus(true, StatusID.HoningDance))
        {
            return base.HealAreaAbility(nextGCD, out action);
        }

        if (CuringWaltzPvP.CanUse(out action))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out action);
    }

    protected override bool MoveBackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return base.MoveBackAbility(nextGCD, out action);
        }

        // if (EnAvantPvP.CanUse(out action)) return true;

        return base.MoveBackAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return base.AttackAbility(nextGCD, out action);
        }

        if (Player.HasStatus(true, StatusID.HoningDance))
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
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard))
        {
            return base.GeneralGCD(out action);
        }

        if (Player.HasStatus(true, StatusID.HoningDance))
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

        if (NumberOfHostilesInRangeOf(6) > 0 && HoningDancePvP.CanUse(out action) && !Player.HasStatus(true, StatusID.EnAvant))
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