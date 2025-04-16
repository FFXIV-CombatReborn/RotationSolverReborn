namespace RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/RDM_Default.PVP.cs")]
[Api(4)]
public class RDM_DefaultPvP : RedMageRotation
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

    [RotationDesc(ActionID.FortePvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (FortePvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.EmboldenPvP)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        // cast embolden yourself
        // if (EmboldenPvP.CanUse(out action)) return true;

        return base.DefenseAreaAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.DisplacementPvP)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        // displace yourself
        // if (DisplacementPvP.CanUse(out action)) return true;

        return base.MoveBackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.CorpsacorpsPvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        // corpse yourself
        // if (CorpsacorpsPvP.CanUse(out action)) return true;

        return base.MoveForwardAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (ViceOfThornsPvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (ScorchPvP.CanUse(out action)) return true;
        if (EnchantedRedoublementPvP.CanUse(out action)) return true;
        if (EnchantedZwerchhauPvP.CanUse(out action)) return true;
        if (EnchantedRipostePvP.CanUse(out action)) return true;

        if (PrefulgencePvP.CanUse(out action)) return true;

        if (ResolutionPvP.CanUse(out action)) return true;

        if (GrandImpactPvP.CanUse(out action)) return true;
        if (JoltIiiPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion

}