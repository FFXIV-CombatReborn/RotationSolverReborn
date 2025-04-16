namespace RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/AST_Default.PVP.cs")]
[Api(4)]
public class AST_DefaultPVP : AstrologianRotation
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

        if (AspectedBeneficPvP_29247.CanUse(out action, usedUp: true)) return true;

        if (Player.WillStatusEnd(1, true, StatusID.Macrocosmos_3104) && MicrocosmosPvP.CanUse(out action)) return true;
        if (Player.GetHealthRatio() < 0.5 && MicrocosmosPvP.CanUse(out action)) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (MinorArcanaPvP.CanUse(out action)) return true;
        if (LordOfCrownsPvP.CanUse(out action)) return true;

        if (MacrocosmosPvP.CanUse(out action)) return true;
        if (GravityIiPvP_29248.CanUse(out action, usedUp: true)) return true;
        if (FallMaleficPvP_29246.CanUse(out action, usedUp: true)) return true;

        if (OraclePvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs

    protected override bool HealSingleGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (AspectedBeneficPvP.CanUse(out action, usedUp: true)) return true;

        return base.HealSingleGCD(out action);
    }

    protected override bool HealAreaGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (LadyOfCrownsPvP.CanUse(out action)) return true;

        return base.HealAreaGCD(out action);
    }

    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (GravityIiPvP.CanUse(out action)) return true;
        if (FallMaleficPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}