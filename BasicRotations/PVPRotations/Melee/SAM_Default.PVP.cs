namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/SAM_Default.PvP.cs")]
[Api(4)]
public sealed class SAM_DefaultPvP : SamuraiRotation
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

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (HissatsuChitenPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (ZanshinPvP.CanUse(out action, usedUp: true)) return true;
        if (Target.HasStatus(true, StatusID.Kuzushi) && MineuchiPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.HissatsuSotenPvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (HissatsuSotenPvP.CanUse(out action)) return true;

        return base.MoveForwardAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (TendoKaeshiSetsugekkaPvP.CanUse(out action)) return true;
        if (TendoSetsugekkaPvP.CanUse(out action)) return true;

        if (KaeshiNamikiriPvP.CanUse(out action)) return true;
        if (OgiNamikiriPvP.CanUse(out action)) return true;

        if (KashaPvP.CanUse(out action)) return true;
        if (GekkoPvP.CanUse(out action)) return true;
        if (YukikazePvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}