namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/NIN_Default.PvP.cs")]
[Api(4)]
public sealed class NIN_DefaultPvP : NinjaRotation
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
        if (Player.HasStatus(true, StatusID.Hidden_1316)) return false;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (DoPurify(out action)) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (Player.HasStatus(true, StatusID.Hidden_1316)) return false;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (Player.HasStatus(true, StatusID.Hidden_1316)) return false;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (DokumoriPvP.CanUse(out action)) return true;

        if (HasHostilesInMaxRange && !Player.HasStatus(true, StatusID.ThreeMudra) && BunshinPvP.CanUse(out action)) return true;
        if (HasHostilesInMaxRange && !Player.HasStatus(true, StatusID.ThreeMudra) && ThreeMudraPvP.CanUse(out action, usedUp: true)) return true;

        return base.AttackAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (Player.HasStatus(true, StatusID.Hidden_1316))
        {
            if (AssassinatePvP.CanUse(out action)) return true;
            return false;
        }
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (ZeshoMeppoPvP.CanUse(out action)) return true;

        if (Player.GetHealthRatio() < .5)
        {
            if (MeisuiPvP.CanUse(out action)) return true;
        }

        if (ForkedRaijuPvP.CanUse(out action)) return true;
        if (FleetingRaijuPvP.CanUse(out action)) return true;
        if (GokaMekkyakuPvP.CanUse(out action)) return true;
        if (HyoshoRanryuPvP.CanUse(out action)) return true;

        if (Player.WillStatusEnd(1, true, StatusID.ThreeMudra) && HutonPvP.CanUse(out action)) return true;

        if (AeolianEdgePvP.CanUse(out action)) return true;
        if (GustSlashPvP.CanUse(out action)) return true;
        if (SpinningEdgePvP.CanUse(out action)) return true;

        if (FumaShurikenPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}