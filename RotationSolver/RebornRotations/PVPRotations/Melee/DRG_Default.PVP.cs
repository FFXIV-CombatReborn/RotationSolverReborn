namespace RotationSolver.RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/DRG_Default.PvP.cs")]

public sealed class DRG_DefaultPvP : DragoonRotation
{
    #region Configurations
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
    public float BloodBathPvPPercent { get; set; } = 0.75f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
    public float SmitePvPPercent { get; set; } = 0.25f;

    [RotationConfig(CombatType.PvP, Name = "Allow the use of high jump if there are enemies in melee range.")]
    public bool JumpYeet { get; set; } = true;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (BloodbathPvP.CanUse(out action) && Player?.GetHealthRatio() < BloodBathPvPPercent)
        {
            return true;
        }

        if (SwiftPvP.CanUse(out action))
        {
            return true;
        }

        if (SmitePvP.CanUse(out action) && SmitePvP.Target.Target.GetHealthRatio() <= SmitePvPPercent)
        {
            return false;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (HorridRoarPvP.CanUse(out action))
        {
            return true;
        }

        if (GeirskogulPvP.CanUse(out action))
        {
            return true;
        }

        if (NastrondPvP.CanUse(out action))
        {
            return true;
        }

        if (HighJumpPvP.CanUse(out action) && HasHostilesInRange && JumpYeet)
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        if (HighJumpPvP.CanUse(out action))
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out action);
    }

    protected override bool MoveBackAbility(IAction nextGCD, out IAction? action)
    {
        if (ElusiveJumpPvP.CanUse(out action))
        {
            return true;
        }

        return base.MoveBackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (WyrmwindThrustPvP.CanUse(out action))
        {
            return true;
        }

        if (HeavensThrustPvP.CanUse(out action))
        {
            return true;
        }

        if (StarcrossPvP.CanUse(out action))
        {
            return true;
        }

        if (ChaoticSpringPvP.CanUse(out action))
        {
            return true;
        }

        if (DrakesbanePvP.CanUse(out action))
        {
            return true;
        }

        if (WheelingThrustPvP.CanUse(out action))
        {
            return true;
        }

        if (FangAndClawPvP.CanUse(out action))
        {
            return true;
        }

        if (RaidenThrustPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}