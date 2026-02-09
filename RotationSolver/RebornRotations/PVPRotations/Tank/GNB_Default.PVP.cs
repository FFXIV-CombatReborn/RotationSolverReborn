namespace RotationSolver.RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/GNB_Default.PvP.cs")]

public sealed class GNB_DefaultPvP : GunbreakerRotation
{
    #region Configurations
    #endregion

    #region Gunbreaker Utilities

    [RotationDesc(ActionID.HeartOfCorundumPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (HeartOfCorundumPvP.CanUse(out action))
        {
            return true;
        }

        if (RampartPvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    private static bool ReadyToRock()
    {
        if (SavageClawPvPReady)
        {
            return true;
        }

        if (WickedTalonPvPReady)
        {
            return true;
        }

        if (HypervelocityPvPReady)
        {
            return true;
        }

        return false;
    }
    private static bool ReadyToRoll()
    {
        if (EyeGougePvPReady)
        {
            return true;
        }

        if (AbdomenTearPvPReady)
        {
            return true;
        }

        if (JugularRipPvPReady)
        {
            return true;
        }

        if (FatedBrandPvPReady)
        {
            return true;
        }

        return false;
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        //You WILL try to save yourself. Configs be damned!
        if (HeartOfCorundumPvP.CanUse(out action) && Player?.GetHealthRatio() * 100 <= 30)
        {
            return true;
        }
        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (!StatusHelper.PlayerHasStatus(true, StatusID.NoMercy_3042) && RoughDividePvP.CanUse(out action, usedUp: true))
        {
            return true;
        }

        if (Target.GetHealthRatio() * 100 <= 50 && BlastingZonePvP.CanUse(out action))
        {
            return true;
        }

        if (RampagePvP.CanUse(out action))
        {
            return true;
        }

        if (FullSwingPvP.CanUse(out action))
        {
            return true;
        }

        if (EyeGougePvP.CanUse(out action))
        {
            return true;
        }

        if (AbdomenTearPvP.CanUse(out action))
        {
            return true;
        }

        if (JugularRipPvP.CanUse(out action))
        {
            return true;
        }

        if (HypervelocityPvP.CanUse(out action))
        {
            return true;
        }

        if (FatedBrandPvP.CanUse(out action))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        // I could totally collapse these into one function but *dab*
        if (!ReadyToRoll())
        {
            if (SavageClawPvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (WickedTalonPvP.CanUse(out action, usedUp: true))
            {
                return true;
            }

            if (GnashingFangPvP.CanUse(out action, usedUp: true))
            {
                return true;
            }
        }

        if (!ReadyToRoll() && FatedCirclePvP.CanUse(out action))
        {
            return true;
        }

        if (!ReadyToRock())
        {
            if (BurstStrikePvP.CanUse(out action))
            {
                return true;
            }

            if (SolidBarrelPvP.CanUse(out action))
            {
                return true;
            }

            if (BrutalShellPvP.CanUse(out action))
            {
                return true;
            }

            if (KeenEdgePvP.CanUse(out action))
            {
                return true;
            }
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}