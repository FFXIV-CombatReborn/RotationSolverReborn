namespace RotationSolver.RebornRotations.PVPRotations.Magical;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Magical/RDM_Default.PVP.cs")]

public class RDM_DefaultPvP : RedMageRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
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

    [RotationDesc(ActionID.FortePvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        if (FortePvP.CanUse(out action))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.DisplacementPvP)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.MoveBackAbility(nextGCD, out action);
        }

        // displace yourself
        // if (DisplacementPvP.CanUse(out action)) return true;

        return base.MoveBackAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.CorpsacorpsPvP)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.MoveForwardAbility(nextGCD, out action);
        }

        // corpse yourself
        // if (CorpsacorpsPvP.CanUse(out action)) return true;

        return base.MoveForwardAbility(nextGCD, out action);
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

        if (ViceOfThornsPvP.CanUse(out action))
        {
            return true;
        }

        if (nextGCD.IsTheSameTo(false, ActionID.ResolutionPvP, ActionID.EnchantedRedoublementPvP, ActionID.ScorchPvP))
        {
            if (EmboldenPvP.CanUse(out action))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.GeneralGCD(out action);
        }

        if (PrefulgencePvP.CanUse(out action))
        {
            return true;
        }

        if (ResolutionPvP.CanUse(out action))
        {
            return true;
        }

        if (ScorchPvP.CanUse(out action))
        {
            return true;
        }

		if (EnchantedRedoublementPvP.CanUse(out action))
		{
			return true;
		}

		if (EnchantedZwerchhauPvP.CanUse(out action))
		{
			return true;
		}

        if (EnchantedRipostePvP.CanUse(out action))
        {
            return true;
        }

        if (GrandImpactPvP.CanUse(out action))
        {
            return true;
        }

        if (JoltIiiPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}