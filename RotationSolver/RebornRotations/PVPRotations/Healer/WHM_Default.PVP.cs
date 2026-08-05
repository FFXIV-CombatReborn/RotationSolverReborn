namespace RotationSolver.RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.55")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/WHM_Default.PVP.cs")]

public class WHM_DefaultPVP : WhiteMageRotation
{
	#region Configurations

	[RotationConfig(CombatType.PvP, Name = "Use Aquaveil on other players")]
	public bool AquaveilEsuna { get; set; } = false;
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (AquaveilEsuna && AquaveilPvP.CanUse(out action))
		{
			return true;
		}
		if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
		{
			if (AquaveilPvP.CanUse(out action, targetOverride: TargetType.Self))
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

		return base.EmergencyAbility(nextGCD, out action);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (DiabrosisPvP.CanUse(out action))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}
	#endregion

	#region GCDs
	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? action)
	{
		if (AquaveilEsuna && AquaveilPvP.CanUse(out action))
		{
			return true;
		}
		if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
		{
			if (AquaveilPvP.CanUse(out action, targetOverride: TargetType.Self))
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

	protected override bool DefenseSingleGCD(out IAction? action)
	{
		if (StoneskinIiPvP.CanUse(out action))
		{
			return true;
		}

		return base.DefenseSingleGCD(out action);
	}

	protected override bool HealSingleGCD(out IAction? action)
	{
		if (HaelanPvP.CanUse(out action))
		{
			return true;
		}

		if (CureIiiPvP.CanUse(out action))
		{
			return true;
		}

		if (CureIiPvP.CanUse(out action))
		{
			return true;
		}

		return base.HealSingleGCD(out action);
	}

	protected override bool GeneralGCD(out IAction? action)
	{
		if (AfflatusMiseryPvP.CanUse(out action))
		{
			return true;
		}

		if (SeraphStrikePvP.CanUse(out action))
		{
			return true;
		}

		if (MiracleOfNaturePvP.CanUse(out action))
		{
			return true;
		}

		if (GlareIvPvP.CanUse(out action))
		{
			return true;
		}

		if (GlareIiiPvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}
	#endregion
}