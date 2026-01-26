namespace RotationSolver.RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/BLM_Default.PVP.cs")]

public class BLM_DefaultPVP : BlackMageRotation
{
    #region Configurations
    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Upper HP threshold you need to be to use Xenoglossy as a damage oGCD")]
    public float XenoglossyHighHP { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Lower HP threshold you need to be to use Xenoglossy as a heal oGCD")]
    public float XenoglossyLowHP { get; set; } = 0.5f;
    #endregion

    #region oGCDs
    [RotationDesc(ActionID.WreathOfIcePvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (WreathOfIcePvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        //if (CometPvP.CanUse(out action)) return true;
        if (RustPvP.CanUse(out action))
        {
            return true;
        }

        if (PhantomDartPvP.CanUse(out action))
        {
            return true;
        }

        if (LethargyPvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? action)
    {
        if (WreathOfFirePvP.CanUse(out action) && InCombat)
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (XenoglossyPvP.CanUse(out action, usedUp: true)
            && (Player?.GetHealthRatio() < XenoglossyLowHP || Player?.GetHealthRatio() > XenoglossyHighHP))
        {
            return true;
        }

        if (FlareStarPvP.CanUse(out action))
        {
            return true;
        }

        if (FrostStarPvP.CanUse(out action))
        {
            return true;
        }

        if (ParadoxPvP.CanUse(out action))
        {
            return true;
        }

        if (NumberOfHostilesInRangeOf(6) > 0 && BurstPvP.CanUse(out action))
        {
            return true;
        }

        if (FirePvP.CanUse(out action))
        {
            return true;
        }

        if (BlizzardIiiPvP.CanUse(out action))
        {
            return true;
        }

        if (BlizzardPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}