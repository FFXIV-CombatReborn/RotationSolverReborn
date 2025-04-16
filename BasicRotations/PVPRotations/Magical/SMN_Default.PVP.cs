namespace RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/PVPRotations/Magical/SMN_Default.PVP.cs")]
[Api(4)]
public class SMN_DefaultPvP : SummonerRotation
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

    [RotationDesc(ActionID.RadiantAegisPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (RadiantAegisPvP.CanUse(out action)) return true;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (DeathflarePvP.CanUse(out action)) return true;
        if (BrandOfPurgatoryPvP.CanUse(out action)) return true;
        if (NecrotizePvP.CanUse(out action) && !Player.HasStatus(true, StatusID.FirebirdTrance) && !Player.HasStatus(true, StatusID.DreadwyrmTrance_3228)) return true;

        return base.AttackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.CrimsonCyclonePvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (CrimsonCyclonePvP.CanUse(out action) && Target.DistanceToPlayer() < 5) return true;

        return base.MoveForwardAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (AstralImpulsePvP.CanUse(out action)) return true;
        if (FountainOfFirePvP.CanUse(out action)) return true;

        if (CrimsonStrikePvP.CanUse(out action)) return true;
        if (CrimsonCyclonePvP.CanUse(out action)) return true;

        if (MountainBusterPvP.CanUse(out action)) return true;

        if (SlipstreamPvP.CanUse(out action)) return true;

        if (RuinIiiPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion

}