namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/BRD_Default.PvP.cs")]

public sealed class BRD_DefaultPvP : BardRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Warden's Paean on other players")]
    public bool BRDEsuna2 { get; set; } = false;
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        if (BRDEsuna2 && TheWardensPaeanPvP.CanUse(out action))
        {
            return true;
        }
        if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
        {
            if (TheWardensPaeanPvP.CanUse(out action, targetOverride: TargetType.Self))
            {
				return true;
			}
        }

        if (BraveryPvP.CanUse(out action))
        {
            if (InCombat)
            {
                return true;
            }
        }

        if (DervishPvP.CanUse(out action))
        {
            if (InCombat)
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        if (RepellingShotPvP.CanUse(out action))
        {
            if (!StatusHelper.PlayerHasStatus(true, StatusID.Repertoire))
            {
                return true;
            }
        }

        if (SilentNocturnePvP.CanUse(out action))
        {
            if (!StatusHelper.PlayerHasStatus(true, StatusID.Repertoire))
            {
                return true;
            }
        }

        if (EagleEyeShotPvP.CanUse(out action))
        {
            return true;
        }

        if (EncoreOfLightPvP.CanUse(out action, skipAoeCheck: true))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        if (HarmonicArrowPvP.CanUse(out action))
        {
            return true;
        }

        if (PitchPerfectPvP.CanUse(out action))
        {
            return true;
        }

        if (BlastArrowPvP.CanUse(out action))
        {
            return true;
        }

        if (ApexArrowPvP.CanUse(out action))
        {
            return true;
        }

        if (PowerfulShotPvP.CanUse(out action))
        {
            return true;
        }

        return base.GeneralGCD(out action);
    }
    #endregion
}