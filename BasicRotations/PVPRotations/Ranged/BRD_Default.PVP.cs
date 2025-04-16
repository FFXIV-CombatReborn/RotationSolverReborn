namespace RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/BRD_Default.PvP.cs")]
[Api(4)]
public sealed class BRD_DefaultPvP : BardRotation
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

    [RotationDesc(ActionID.TheWardensPaeanPvP)]
    protected override bool DispelGCD(out IAction? act)
    {
        if (TheWardensPaeanPvP.CanUse(out act)) return true;
        return base.DispelGCD(out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (RepellingShotPvP.CanUse(out action) && !Player.HasStatus(true, StatusID.Repertoire)) return true;
        if (EncoreOfLightPvP.CanUse(out action)) return true;
        if (SilentNocturnePvP.CanUse(out action) && !Player.HasStatus(true, StatusID.Repertoire)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion
    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (HarmonicArrowPvP_41964.CanUse(out action)) return true;

        if (PitchPerfectPvP.CanUse(out action)) return true;

        if (BlastArrowPvP.CanUse(out action)) return true;
        if (ApexArrowPvP.CanUse(out action)) return true;

        if (PowerfulShotPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);

    }
    #endregion
}