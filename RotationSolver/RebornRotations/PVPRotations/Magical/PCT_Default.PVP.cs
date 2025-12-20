namespace RotationSolver.RebornRotations.PVPRotations.Magical;

[Rotation("Default PvP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/PCT_Default.PVP.cs")]

public class PCT_DefaultPvP : PictomancerRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Health threshold needed to use Tempura Coat")]
    public float TempuraThreshold { get; set; } = 0.8f;

    [RotationConfig(CombatType.PvP, Name = "Freely use burst damage oGCDs")]
    public bool FreeBurst { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvP, Name = "Enemy HP threshold needed to use burst oGCDs on if previous config disabled")]
    public float BurstThreshold { get; set; } = 0.55f;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.EmergencyAbility(nextGCD, out action);
        }

        if (PurifyPvP.CanUse(out action))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        if (TemperaCoatPvP.CanUse(out action) && Player?.GetHealthRatio() <= TempuraThreshold)
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.AttackAbility(nextGCD, out action);
        }

        //if (CometPvP.CanUse(out action)) return true;
        if (RustPvP.CanUse(out action))
        {
            return true;
        }

        if (PhantomDartPvP.CanUse(out action))
        {
            return true;
        }

        if (FreeBurst || CurrentTarget?.GetHealthRatio() <= BurstThreshold)
        {
            // Use all Muses in sequence for maximum burst
            if (PomMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (WingedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (ClawedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (FangedMusePvP.CanUse(out action, usedUp: true))
            {
                return true;
            }
        }

        switch (IsMoving)
        {
            case true:
                if (ReleaseSubtractivePalettePvP.CanUse(out action))
                {
                    return true;
                }

                break;
            case false:
                if (SubtractivePalettePvP.CanUse(out action))
                {
                    return true;
                }

                break;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (RespectGuard && Player != null && HasPVPGuard)
        {
            return base.GeneralGCD(out action);
        }

        if (StarPrismPvP.CanUse(out action))
        {
            return true;
        }

        if (MogOfTheAgesPvP.CanUse(out action))
        {
            return true;
        }

        if (RetributionOfTheMadeenPvP.CanUse(out action))
        {
            return true;
        }

        if (CometInBlackPvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (PomMotifPvP.CanUse(out action))
        {
            return true;
        }

        if (WingMotifPvP.CanUse(out action))
        {
            return true;
        }

        if (ClawMotifPvP.CanUse(out action))
        {
            return true;
        }

        if (MawMotifPvP.CanUse(out action))
        {
            return true;
        }

        if (FireInRedPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}