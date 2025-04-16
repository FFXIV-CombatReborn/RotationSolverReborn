namespace RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/VPR_Default.PvP.cs")]
[Api(4)]
public sealed class VPR_DefaultPvP : ViperRotation
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
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard) || Player.HasStatus(true, StatusID.HardenedScales)) return false;
        if (DoPurify(out action)) return true;

        if (SnakeScalesPvP.Cooldown.IsCoolingDown && UncoiledFuryPvP.Cooldown.IsCoolingDown)
        {
            if (RattlingCoilPvP.CanUse(out action)) return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard) || Player.HasStatus(true, StatusID.HardenedScales)) return false;

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard) || Player.HasStatus(true, StatusID.HardenedScales)) return false;

        if (FourthLegacyPvP.CanUse(out action)) return true;
        if (ThirdLegacyPvP.CanUse(out action)) return true;
        if (SecondLegacyPvP.CanUse(out action)) return true;
        if (FirstLegacyPvP.CanUse(out action)) return true;

        if (TwinbloodBitePvP.CanUse(out action)) return true;
        if (TwinfangBitePvP.CanUse(out action)) return true;

        if (UncoiledTwinbloodPvP.CanUse(out action)) return true;
        if (UncoiledTwinfangPvP.CanUse(out action)) return true;

        if (DeathRattlePvP.CanUse(out action)) return true;

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard) || Player.HasStatus(true, StatusID.HardenedScales)) return false;

        return base.MoveForwardAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard) || Player.HasStatus(true, StatusID.HardenedScales)) return false;

        if (FourthGenerationPvP.CanUse(out action)) return true;
        if (ThirdGenerationPvP.CanUse(out action)) return true;
        if (SecondGenerationPvP.CanUse(out action)) return true;
        if (FirstGenerationPvP.CanUse(out action)) return true;

        if (OuroborosPvP.CanUse(out action)) return true;

        if (SanguineFeastPvP.CanUse(out action)) return true;
        if (BloodcoilPvP.CanUse(out action)) return true;

        if (UncoiledFuryPvP.CanUse(out action)) return true;

        if (RavenousBitePvP.CanUse(out action)) return true;
        if (SwiftskinsStingPvP.CanUse(out action)) return true;
        if (PiercingFangsPvP.CanUse(out action)) return true;
        if (BarbarousBitePvP.CanUse(out action)) return true;
        if (HuntersStingPvP.CanUse(out action)) return true;
        if (SteelFangsPvP.CanUse(out action)) return true;

        if (UncoiledFuryPvP.CanUse(out action, usedUp: true)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}