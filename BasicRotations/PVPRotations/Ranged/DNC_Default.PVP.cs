namespace RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/DNC_Default.PvP.cs")]
[Api(4)]
public sealed class DNC_DefaultPvP : DancerRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
    #endregion

    #region Standard PVP Utilities
    private bool DoPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP) return false;

        var purifiableStatusesIDs = new List<int>
        {
            // Stun, DeepFreeze, HalfAsleep, Sleep, Bind, Heavy, Silence
            1343, 3219, 3022, 1348, 1345, 1344, 1347
        };

        if (purifiableStatusesIDs.Any(id => Player.HasStatus(false, (StatusID)id)))
        {
            return PurifyPvP.CanUse(out action);
        }

        return false;
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;
        if (DoPurify(out action)) return true;

        if (ClosedPositionPvP.CanUse(out action) && !Player.HasStatus(true, StatusID.ClosedPosition_2026)) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool HealAreaAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (CuringWaltzPvP.CanUse(out action)) return true;

        return base.HealAreaAbility(nextGCD, out action);
    }

    protected override bool MoveBackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        // if (EnAvantPvP.CanUse(out action)) return true;

        return base.MoveBackAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (FanDancePvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (DanceOfTheDawnPvP.CanUse(out action)) return true;

        if (StarfallDancePvP.CanUse(out action)) return true;

        if (HoningDancePvP.CanUse(out action) && !Player.HasStatus(true, StatusID.EnAvant)) return true;

        if (SaberDancePvP.CanUse(out action)) return true;

        if (FountainPvP.CanUse(out action)) return true;
        if (CascadePvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion

}