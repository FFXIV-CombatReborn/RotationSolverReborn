namespace RotationSolver.RebornRotations.PVPRotations.Melee;

[Rotation("Default", CombatType.PvP, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Melee/MNK_Default.PVP.cs")]

public sealed class MNK_DefaultPvP : MonkRotation
{
	#region Configurations
	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Player health threshold needed for Bloodbath use")]
	public float BloodBathPvPPercent { get; set; } = 0.75f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Enemy health threshold needed for Smite use")]
	public float SmitePvPPercent { get; set; } = 0.25f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvP, Name = "Personal health threshold needed for Earth's Reply use without hostiles nearby")]
	public float EarthsReplyPercent { get; set; } = 0.5f;

	[RotationConfig(CombatType.PvP, Name = "Use Earth's Reply if a hositle is within range")]
	public bool EarthsReplyAttack { get; set; } = true;

	[RotationConfig(CombatType.PvP, Name = "Use Earth's Reply if the status will end within the next GCD")]
	public bool EarthsReplyStatusEnd { get; set; } = true;
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (EarthsReplyStatusEnd && StatusHelper.PlayerHasStatus(true, StatusID.EarthResonance) && StatusHelper.PlayerWillStatusEndGCD(1, 0, true, StatusID.EarthResonance))
		{
			if (EarthsReplyPvP.CanUse(out action, usedUp: true, skipAoeCheck: true, skipStatusNeed: true))
			{
				return true;
			}
		}

		if (Player?.GetHealthRatio() <= EarthsReplyPercent && StatusHelper.PlayerHasStatus(true, StatusID.EarthResonance))
		{
			if (EarthsReplyPvP.CanUse(out action, usedUp: true, skipAoeCheck: true, skipStatusNeed: true))
			{
				return true;
			}
		}

		if (RiddleOfEarthPvP.CanUse(out action) && InCombat && Player?.GetHealthRatio() < 0.8)
		{
			return true;
		}

		if (BloodbathPvP.CanUse(out action) && Player?.GetHealthRatio() < BloodBathPvPPercent)
		{
			return true;
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

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (NumberOfHostilesInRangeOf(6) > 0 && RisingPhoenixPvP.CanUse(out action, usedUp: true) && InCombat)
		{
			return true;
		}

		if (EarthsReplyAttack && EarthsReplyPvP.CanUse(out action, usedUp: true) && HasHostilesInRange)
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}
	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (PhantomRushPvP.CanUse(out action))
		{
			return true;
		}

		if (FiresReplyPvP.CanUse(out action, usedUp: true, skipAoeCheck: true))
		{
			return true;
		}

		if (WindsReplyPvP.CanUse(out action))
		{
			return true;
		}

		if (PouncingCoeurlPvP.CanUse(out action))
		{
			return true;
		}

		if (RisingRaptorPvP.CanUse(out action))
		{
			return true;
		}

		if (LeapingOpoPvP.CanUse(out action))
		{
			return true;
		}

		if (DemolishPvP.CanUse(out action))
		{
			return true;
		}

		if (TwinSnakesPvP.CanUse(out action))
		{
			return true;
		}

		if (DragonKickPvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}
	#endregion
}