namespace RotationSolver.Basic.Rotations.Duties;

/// <summary>
/// The bozja action.
/// </summary>
[DutyTerritory(920, 975)] // TODO: Verify the bozja territory IDs.
public abstract class BozjaRotation : DutyRotation
{
}

public partial class DutyRotation
{
	static partial void ModifyLostSpellforgePvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Physical;
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = new[] { StatusID.MagicalAversion };
		setting.TargetStatusProvide = new[] { StatusID.LostSpellforge, StatusID.LostSteelsting };
	}

	static partial void ModifyLostSteelstingPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Magical;
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = new[] { StatusID.PhysicalAversion };
		setting.TargetStatusProvide = new[] { StatusID.LostSpellforge, StatusID.LostSteelsting };
	}

	static partial void ModifyLostRampagePvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = new[] { StatusID.PhysicalAversion };
		setting.StatusProvide = new[] { StatusID.LostRampage };
	}

	static partial void ModifyLostBurstPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = new[] { StatusID.MagicalAversion };
		setting.StatusProvide = new[] { StatusID.LostBurst };
	}

	static partial void ModifyLostBloodRagePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = new[] { StatusID.LostBravery };
	}

	static partial void ModifyLostProtectPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = new[] { StatusID.LostProtect, StatusID.LostProtectIi };
	}

	static partial void ModifyLostProtectIiPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = new[] { StatusID.LostProtectIi };
	}

	static partial void ModifyLostShellPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = new[] { StatusID.LostShell, StatusID.LostShellIi };
	}

	static partial void ModifyLostShellIiPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = new[] { StatusID.LostShellIi };
	}

	static partial void ModifyLostBubblePvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = new[] { StatusID.LostBubble };
	}

	static partial void ModifyLostStoneskinPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = new[] { StatusID.Stoneskin };
	}

	static partial void ModifyLostStoneskinIiPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = new[] { StatusID.Stoneskin };
	}

	static partial void ModifyLostFlareStarPvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = new[] { StatusID.LostFlareStar };
	}

	static partial void ModifyLostSeraphStrikePvE(ref ActionSetting setting)
	{
		setting.TargetStatusProvide = new[] { StatusID.ClericStance_2484 };
	}
}