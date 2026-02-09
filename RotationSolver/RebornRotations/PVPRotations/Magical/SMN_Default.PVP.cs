namespace RotationSolver.RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.41")]
[SourceCode(Path = "main/BasicRotations/PVPRotations/Magical/SMN_Default.PVP.cs")]

public class SMN_DefaultPvP : SummonerRotation
{
    #region Configurations

    #endregion

    #region oGCDs
    [RotationDesc(ActionID.RadiantAegisPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RadiantAegisPvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (DeathflarePvP.CanUse(out action))
        {
            return true;
        }

        if (BrandOfPurgatoryPvP.CanUse(out action))
        {
            return true;
        }

        if (NecrotizePvP.CanUse(out action) && !StatusHelper.PlayerHasStatus(true, StatusID.FirebirdTrance) && !StatusHelper.PlayerHasStatus(true, StatusID.DreadwyrmTrance_3228))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.CrimsonCyclonePvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
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

        if (CrimsonCyclonePvP.CanUse(out action) && Target.DistanceToPlayer() < 5)
        {
            return true;
        }

        return base.MoveForwardAbility(nextGCD, out action);
    }

    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (AstralImpulsePvP.CanUse(out action))
        {
            return true;
        }

        if (FountainOfFirePvP.CanUse(out action))
        {
            return true;
        }

        if (CrimsonStrikePvP.CanUse(out action))
        {
            return true;
        }

        if (CrimsonCyclonePvP.CanUse(out action))
        {
            return true;
        }

        if (MountainBusterPvP.CanUse(out action))
        {
            return true;
        }

        if (SlipstreamPvP.CanUse(out action))
        {
            return true;
        }

        if (RuinIiiPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}