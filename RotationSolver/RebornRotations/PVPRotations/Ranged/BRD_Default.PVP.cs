namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.5")]
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

		if (PurifyPvP.CanUse(out action))
		{
			if (Service.Config.PvpPurifyStun && StatusHelper.PlayerHasStatus(false, StatusID.Stun_1343))
			{
				return true;
			}

			if (Service.Config.PvpPurifyHeavy && StatusHelper.PlayerHasStatus(false, StatusID.Heavy_1344))
			{
				return true;
			}

			if (Service.Config.PvpPurifyBind && StatusHelper.PlayerHasStatus(false, StatusID.Bind_1345))
			{
				return true;
			}

			if (Service.Config.PvpPurifySilence && StatusHelper.PlayerHasStatus(false, StatusID.Silence_1347))
			{
				return true;
			}

			if (Service.Config.PvpPurifyDeepFreeze && StatusHelper.PlayerHasStatus(false, StatusID.DeepFreeze_3219))
			{
				return true;
			}

			if (Service.Config.PvpPurifyMiracleOfNature && StatusHelper.PlayerHasStatus(false, StatusID.MiracleOfNature))
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
	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? action)
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

		if (PurifyPvP.CanUse(out action))
		{
			if (Service.Config.PvpPurifyStun && StatusHelper.PlayerHasStatus(false, StatusID.Stun_1343))
			{
				return true;
			}

			if (Service.Config.PvpPurifyHeavy && StatusHelper.PlayerHasStatus(false, StatusID.Heavy_1344))
			{
				return true;
			}

			if (Service.Config.PvpPurifyBind && StatusHelper.PlayerHasStatus(false, StatusID.Bind_1345))
			{
				return true;
			}

			if (Service.Config.PvpPurifySilence && StatusHelper.PlayerHasStatus(false, StatusID.Silence_1347))
			{
				return true;
			}

			if (Service.Config.PvpPurifyDeepFreeze && StatusHelper.PlayerHasStatus(false, StatusID.DeepFreeze_3219))
			{
				return true;
			}

			if (Service.Config.PvpPurifyMiracleOfNature && StatusHelper.PlayerHasStatus(false, StatusID.MiracleOfNature))
			{
				return true;
			}
		}

		return base.GeneralGCD(out action);
	}

	protected override bool GeneralGCD(out IAction? action)
	{
		if (!StatusHelper.PlayerHasStatus(true, StatusID.FrontlinersMarch))
		{
			if (ApexArrowPvP.CanUse(out action, skipStatusProvideCheck: true))
			{
				return true;
			}
		}

		if (HarmonicArrowPvP.CanUse(out action))
		{
			return true;
		}

		if (PitchPerfectPvP.CanUse(out action, skipAoeCheck: true))
		{
			return true;
		}

		if (BlastArrowPvP.CanUse(out action))
		{
			return true;
		}

		if (ApexArrowPvP.CanUse(out action, skipStatusProvideCheck: true))
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