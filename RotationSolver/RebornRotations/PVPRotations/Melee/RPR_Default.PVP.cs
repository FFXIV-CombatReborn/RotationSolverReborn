namespace RotationSolver.RebornRotations.PVPRotations.Melee;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.45")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/RPR_Default.PvP.cs")]

public sealed class RPR_DefaultPvP : ReaperRotation
{
	#region Configurations
	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
	public float BloodBathPvPPercent { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
	public float SmitePvPPercent { get; set; } = 0.25f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Perfectio use")]
	public float PerfectioPvPPercent { get; set; } = 0.25f;

	[RotationConfig(CombatType.PvP, Name = "Use Communio immediately after Enshroud (For frontline)")]
	public bool UseCommunioImmediately { get; set; } = false;
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (FateSealedPvP.CanUse(out action) && HasDeathWarrantPvP && WillDeathWarrantPvPEnd)
		{
			return true;
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

		if (SmitePvP.CanUse(out action, usedUp: true) && SmitePvP.Target.Target.GetHealthRatio() <= SmitePvPPercent)
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out action);
	}

	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
	{
		if (ArcaneCrestPvP.CanUse(out action))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out action);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (HasEnshroudedPvP)
		{
			if (LemuresSlicePvP.CanUse(out action))
			{
				return true;
			}
		}

		if (DeathWarrantPvP.CanUse(out action))
		{
			return true;
		}

		if (!HasEnshroudedPvP)
		{
			if (GrimSwathePvP.CanUse(out action))
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
		if (HasEnshroudedPvP)
		{
			if (CommunioPvP.CanUse(out action))
			{
				if (UseCommunioImmediately ||
					StatusHelper.PlayerStatusStack(true, StatusID.Enshrouded_2863) == 1 ||
					StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.Enshrouded_2863))
				{
					return true;
				}
			}
		}

		if (PerfectioPvP.CanUse(out action) && PerfectioPvP.Target.Target.GetHealthRatio() <= PerfectioPvPPercent)
		{
			return true;
		}

		if (CrossReapingPvP.CanUse(out action))
		{
			return true;
		}

		if (VoidReapingPvP.CanUse(out action))
		{
			return true;
		}

		if (HasImmortalSacrificePvP)
		{
			if (PlentifulHarvestPvP.CanUse(out action))
			{
				if (StatusHelper.PlayerStatusStack(true, StatusID.ImmortalSacrifice_3204) > 3)
				{
					return true;
				}
			}
		}

		if (HarvestMoonPvP.CanUse(out action, usedUp: true))
		{
			return true;
		}

		if (ExecutionersGuillotinePvP.CanUse(out action))
		{
			return true;
		}

		if (InfernalSlicePvP.CanUse(out action))
		{
			return true;
		}

		if (WaxingSlicePvP.CanUse(out action))
		{
			return true;
		}

		if (SlicePvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}
	#endregion
}