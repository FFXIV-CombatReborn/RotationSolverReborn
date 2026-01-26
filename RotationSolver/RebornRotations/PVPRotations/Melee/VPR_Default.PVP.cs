namespace RotationSolver.RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/VPR_Default.PvP.cs")]

public sealed class VPR_DefaultPvP : ViperRotation
{
    #region Configurations
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
    public float BloodBathPvPPercent { get; set; } = 0.75f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
    public float SmitePvPPercent { get; set; } = 0.25f;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (StatusHelper.PlayerHasStatus(true, StatusID.HardenedScales))
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        //these have to stay in Emergency because action weirdness with Serpent's Tail adjust ID
        if (UncoiledTwinbloodPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledTwinfangPvP.CanUse(out action))
        {
            return true;
        }

        if (RattlingCoilPvP.CanUse(out action))
        {
            if (SnakeScalesPvP.Cooldown.IsCoolingDown && UncoiledFuryPvP.Cooldown.IsCoolingDown)
            {
                return true;
            }
        }

        if (BloodbathPvP.CanUse(out action))
        {
            if (Player?.GetHealthRatio() < BloodBathPvPPercent)
            {
                return true;
            }
        }

        if (SwiftPvP.CanUse(out action))
        {
            return true;
        }

        if (SmitePvP.CanUse(out action) && SmitePvP.Target.Target.GetHealthRatio() <= SmitePvPPercent)
        {
            return false;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (StatusHelper.PlayerHasStatus(true, StatusID.HardenedScales))
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (StatusHelper.PlayerHasStatus(true, StatusID.HardenedScales))
        {
            return base.AttackAbility(nextGCD, out action);
        }

        if (FourthLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (ThirdLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (SecondLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (FirstLegacyPvP.CanUse(out action))
        {
            return true;
        }

        if (TwinbloodBitePvP.CanUse(out action))
        {
            return true;
        }

        if (TwinfangBitePvP.CanUse(out action))
        {
            return true;
        }

        if (DeathRattlePvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (StatusHelper.PlayerHasStatus(true, StatusID.HardenedScales))
        {
            return base.MoveForwardAbility(nextGCD, out action);
        }

        return base.MoveForwardAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (StatusHelper.PlayerHasStatus(true, StatusID.HardenedScales))
        {
            return base.GeneralGCD(out action);
        }

        if (FourthGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (ThirdGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (SecondGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (FirstGenerationPvP.CanUse(out action))
        {
            return true;
        }

        if (OuroborosPvP.CanUse(out action))
        {
            return true;
        }

        if (SanguineFeastPvP.CanUse(out action))
        {
            return true;
        }

        if (BloodcoilPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledFuryPvP.CanUse(out action))
        {
            return true;
        }

        if (RavenousBitePvP.CanUse(out action))
        {
            return true;
        }

        if (SwiftskinsStingPvP.CanUse(out action))
        {
            return true;
        }

        if (PiercingFangsPvP.CanUse(out action))
        {
            return true;
        }

        if (BarbarousBitePvP.CanUse(out action))
        {
            return true;
        }

        if (HuntersStingPvP.CanUse(out action))
        {
            return true;
        }

        if (SteelFangsPvP.CanUse(out action))
        {
            return true;
        }

        if (UncoiledFuryPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}