namespace RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/PLD_Default.PvP.cs")]
[Api(4)]
public sealed class PLD_DefaultPvP : PaladinRotation
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

        if (HolySheltronPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }
    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (ImperatorPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (BladeOfFaithPvP.CanUse(out action)) return true;
        if (BladeOfTruthPvP.CanUse(out action)) return true;
        if (BladeOfValorPvP.CanUse(out action)) return true;
        if (ConfiteorPvP.CanUse(out action)) return true;

        if (ShieldSmitePvP.CanUse(out action)) return true;

        if (HolySpiritPvP.CanUse(out action)) return true;

        if (AtonementPvP.CanUse(out action)) return true;
        if (SupplicationPvP.CanUse(out action)) return true;
        if (SepulchrePvP.CanUse(out action)) return true;

        if (RoyalAuthorityPvP.CanUse(out action)) return true;
        if (RiotBladePvP.CanUse(out action)) return true;
        if (FastBladePvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);

    }
    #endregion
}