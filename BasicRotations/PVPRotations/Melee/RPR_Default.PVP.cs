namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/RPR_Default.PvP.cs")]
[Api(4)]
public sealed class RPR_DefaultPvP : ReaperRotation
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

        if (ArcaneCrestPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (DeathWarrantPvP.CanUse(out action)) return true;
        if (LemuresSlicePvP.CanUse(out action)) return true;
        if (GrimSwathePvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (LemuresSlicePvP.CanUse(out action)) return true;
        if (CommunioPvP.CanUse(out action)) return true;

        if (CrossReapingPvP.CanUse(out action)) return true;
        if (VoidReapingPvP.CanUse(out action)) return true;

        if (PerfectioPvP.CanUse(out action)) return true;

        if (Player.StatusList.Any(Status => (Status.GameData.Value.Name == "Immortal Sacrifice") && Status.Param > 3))
        {
            if (PlentifulHarvestPvP.CanUse(out action)) return true;
        }

        if (HarvestMoonPvP.CanUse(out action, usedUp: true)) return true;
        if (ExecutionersGuillotinePvP.CanUse(out action)) return true;

        if (InfernalSlicePvP.CanUse(out action)) return true;
        if (WaxingSlicePvP.CanUse(out action)) return true;
        if (SlicePvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}