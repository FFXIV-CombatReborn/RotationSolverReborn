namespace RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/DRK_Default.PvP.cs")]
[Api(4)]
public sealed class DRK_DefaultPvP : DarkKnightRotation
{
    #region Configurations

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "Shadowbringer Threshold")]
    public int ShadowbringerThreshold { get; set; } = 50;

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
        if (InCombat && TheBlackestNightPvP.CanUse(out action) && TheBlackestNightPvP.Cooldown.CurrentCharges == 2) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (TheBlackestNightPvP.CanUse(out act)) return true;

        return base.DefenseSingleAbility(nextGCD, out act);
    }
    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (!IsMoving && SaltedEarthPvP.CanUse(out action)) return true;

        if (PlungePvP.CanUse(out action)) return true;

        if (!Player.HasStatus(true, StatusID.Blackblood) && ((Player.GetHealthRatio() * 100) > ShadowbringerThreshold || Player.HasStatus(true, StatusID.DarkArts_3034)) && ShadowbringerPvP.CanUse(out action)) return true;

        if (SaltAndDarknessPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if ((Player.GetHealthRatio() * 100) < 60 && ImpalementPvP.CanUse(out action)) return true;

        if (TorcleaverPvP.CanUse(out action)) return true;
        if (ComeuppancePvP.CanUse(out action)) return true;
        if (ScarletDeliriumPvP.CanUse(out action)) return true;

        if (SouleaterPvP.CanUse(out action)) return true;
        if (SyphonStrikePvP.CanUse(out action)) return true;
        if (HardSlashPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);

    }
    #endregion
}