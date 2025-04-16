namespace RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/WAR_Default.PvP.cs")]
[Api(4)]
public sealed class WAR_DefaultPvP : WarriorRotation
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

        return base.EmergencyAbility(nextGCD, out action);
    }
    [RotationDesc(ActionID.BloodwhettingPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (BloodwhettingPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }
    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (BlotaPvP.CanUse(out action)) return true;
        if (OnslaughtPvP.CanUse(out action)) return true;
        if (OrogenyPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (InnerChaosPvP.CanUse(out action)) return true;

        if (PrimalRuinationPvP.CanUse(out action)) return true;
        if (PrimalRendPvP.CanUse(out action)) return true;

        if (ChaoticCyclonePvP.CanUse(out action)) return true;

        if (FellCleavePvP.CanUse(out action)) return true;

        if (StormsPathPvP.CanUse(out action)) return true;
        if (MaimPvP.CanUse(out action)) return true;
        if (HeavySwingPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);

    }
    #endregion
}