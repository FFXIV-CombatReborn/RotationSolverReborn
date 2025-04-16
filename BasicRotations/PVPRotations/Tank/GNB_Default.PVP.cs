namespace RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/GNB_Default.PvP.cs")]
[Api(4)]
public sealed class GNB_DefaultPvP : GunbreakerRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Recuperate")]
    public bool UseRecuperatePvP { get; set; } = true;

    [Range(1, 100, ConfigUnitType.Percent, 1)]
    [RotationConfig(CombatType.PvP, Name = "Recuperate Threshold")]
    public int RecuperateThreshold { get; set; } = 75;

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

    #region Gunbreaker Utilities

    [RotationDesc(ActionID.HeartOfCorundumPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;
        if (HeartOfCorundumPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    private static bool ReadyToRock()
    {
        if (SavageClawPvPReady) return true;
        if (WickedTalonPvPReady) return true;
        if (HypervelocityPvPReady) return true;

        return false;
    }
    private static bool ReadyToRoll()
    {
        if (EyeGougePvPReady) return true;
        if (AbdomenTearPvPReady) return true;
        if (JugularRipPvPReady) return true;
        if (FatedBrandPvPReady) return true;

        return false;
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;
        if (DoPurify(out action)) return true;
        //You WILL try to save yourself. Configs be damned!
        if (Player.GetHealthRatio() * 100 <= 30 && HeartOfCorundumPvP.CanUse(out action)) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (!Player.HasStatus(true, StatusID.NoMercy_3042) && RoughDividePvP.CanUse(out action, usedUp: true)) return true;
        if (Target.GetHealthRatio() * 100 <= 50 && BlastingZonePvP.CanUse(out action)) return true;

        if (EyeGougePvP.CanUse(out action)) return true;
        if (AbdomenTearPvP.CanUse(out action)) return true;
        if (JugularRipPvP.CanUse(out action)) return true;
        if (HypervelocityPvP.CanUse(out action)) return true;
        if (FatedBrandPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;

        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        // I could totally collapse these into one function but *dab*
        if (!ReadyToRoll())
        {
            if (SavageClawPvP.CanUse(out action, usedUp: true)) return true;
            if (WickedTalonPvP.CanUse(out action, usedUp: true)) return true;
            if (GnashingFangPvP.CanUse(out action, usedUp: true)) return true;
        }

        if (!ReadyToRoll() && FatedCirclePvP.CanUse(out action)) return true;

        if (!ReadyToRock())
        {
            if (BurstStrikePvP.CanUse(out action)) return true;
            if (SolidBarrelPvP.CanUse(out action)) return true;
            if (BrutalShellPvP.CanUse(out action)) return true;
            if (KeenEdgePvP.CanUse(out action)) return true;
        }

        return base.GeneralGCD(out action);

    }
    #endregion

}