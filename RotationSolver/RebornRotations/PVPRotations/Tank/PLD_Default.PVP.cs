namespace RotationSolver.RebornRotations.PVPRotations.Tank;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Tank/PLD_Default.PvP.cs")]

public sealed class PLD_DefaultPvP : PaladinRotation
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

    [RotationDesc(ActionID.BloodwhettingPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        if (RespectGuard && HasPVPGuard)
        {
            return base.DefenseSingleAbility(nextGCD, out action);
        }

        if (RampartPvP.CanUse(out action))
        {
            return true;
        }

        if (HolySheltronPvP.CanUse(out action))
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

        if (RampagePvP.CanUse(out action))
        {
            return true;
        }

        if (FullSwingPvP.CanUse(out action))
        {
            return true;
        }

        if (ImperatorPvP.CanUse(out action))
        {
            return true;
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

        if (BladeOfFaithPvP.CanUse(out action))
        {
            return true;
        }

        if (BladeOfTruthPvP.CanUse(out action))
        {
            return true;
        }

        if (BladeOfValorPvP.CanUse(out action))
        {
            return true;
        }

        if (ConfiteorPvP.CanUse(out action))
        {
            return true;
        }

        if (ShieldSmitePvP.CanUse(out action))
        {
            return true;
        }

        if (HolySpiritPvP.CanUse(out action))
        {
            return true;
        }

        if (AtonementPvP.CanUse(out action))
        {
            return true;
        }

        if (SupplicationPvP.CanUse(out action))
        {
            return true;
        }

        if (SepulchrePvP.CanUse(out action))
        {
            return true;
        }

        if (RoyalAuthorityPvP.CanUse(out action))
        {
            return true;
        }

        if (RiotBladePvP.CanUse(out action))
        {
            return true;
        }

        if (FastBladePvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}