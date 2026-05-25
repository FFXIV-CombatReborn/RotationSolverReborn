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
	static partial void ModifyLostFocusPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.StatusProvide = [StatusID.Boost_1656];
	}

	static partial void ModifyLostFontOfMagicPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.StatusProvide = [StatusID.LostFontOfMagic];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostFontOfPowerPvE(ref ActionSetting setting)
	{
		setting.TargetType = TargetType.Self;
		setting.IsFriendly = true;
		setting.StatusProvide = [StatusID.LostFontOfPower];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostSlashPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostDeathPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.TargetType = TargetType.LowHPPercent;
	}

	static partial void ModifyBannerOfNobleEndsPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfNobleEnds];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyBannerOfHonoredSacrificePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfHonoredSacrifice];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyBannerOfHonedAcuityPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfHonedAcuity];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostFairTradePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
	}

	static partial void ModifyLostFlareStarPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = [StatusID.LostFlareStar];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostChainspellPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = StatusHelper.SwiftcastStatus;
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostAssassinationPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.TargetType = TargetType.LowHPPercent;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostManawallPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.Heavy, StatusID.LostManawall];
	}

	static partial void ModifyBannerOfTirelessConvictionPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfTirelessConviction];
	}

	static partial void ModifyBannerOfFirmResolvePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfFirmResolve];
	}

	static partial void ModifyLostIncensePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusProvide = [StatusID.LostIncense];
	}

	static partial void ModifyLostExcellencePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusNeed = [StatusID.Weakness];
		setting.StatusProvide = [StatusID.LostExcellence];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostBloodRagePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.LostBloodRage];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyBannerOfSolemnClarityPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.BannerOfSolemnClarity];
		setting.ActionCheck = () => InCombat;
	}

	static partial void ModifyLostCurePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
	}

	static partial void ModifyLostCureIiPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
	}

	static partial void ModifyLostCureIiiPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostCureIvPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostArisePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Death;
	}

	static partial void ModifyLostSacrificePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Death;
	}

	static partial void ModifyLostReraisePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = [StatusID.Reraise];
	}

	static partial void ModifyLostFullCurePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostSpellforgePvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusNeed = [StatusID.MagicalAversion];
		setting.TargetStatusProvide = [StatusID.LostSpellforge, StatusID.LostSteelsting];
	}

	static partial void ModifyLostSteelstingPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusNeed = [StatusID.PhysicalAversion];
		setting.TargetStatusProvide = [StatusID.LostSpellforge, StatusID.LostSteelsting];
	}

	static partial void ModifyLostProtectPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostProtect, StatusID.LostProtectIi];
	}

	static partial void ModifyLostShellPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostShell, StatusID.LostShellIi];
	}

	static partial void ModifyLostReflectPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostReflect];
	}

	static partial void ModifyLostStoneskinPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.Stoneskin];
	}

	static partial void ModifyLostBraveryPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostBravery];
	}

	static partial void ModifyLostAethershieldPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.LostAethershield];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostDervishPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.Dervish];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostStoneskinIiPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.Stoneskin];
	}

	static partial void ModifyLostProtectIiPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostProtectIi];
	}

	static partial void ModifyLostShellIiPvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostShellIi];
	}

	static partial void ModifyLostBubblePvE(ref ActionSetting setting)
	{
		setting.StatusFromSelf = false;
		setting.IsFriendly = true;
		setting.TargetStatusProvide = [StatusID.LostBubble];
	}

	static partial void ModifyLostStealthPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.LostStealth];
		setting.ActionCheck = () => !InCombat;
	}

	static partial void ModifyLostSwiftPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.LostSwift];
	}

	static partial void ModifyLostFontOfSkillPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyLostPerceptionPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.ActionCheck = () => DataCenter.IsInDelubrumNormal || DataCenter.IsInDelubrumSavage;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostImpetusPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
		setting.StatusProvide = [StatusID.LostSwift];
		setting.SpecialType = SpecialActionType.FixedDistanceMoveForward;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostParalyzeIiiPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusProvide = [StatusID.Paralysis];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostBanishIiiPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostDispelPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
	}

	static partial void ModifyLostRendArmorPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.TargetStatusProvide	= [StatusID.LostRendArmor];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostSeraphStrikePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusProvide = [StatusID.ClericStance_2484];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyLostBurstPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = [StatusID.MagicalAversion];
		setting.TargetStatusProvide = [StatusID.LostBurst];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 2,
		};
	}

	static partial void ModifyLostRampagePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.StatusFromSelf = false;
		setting.TargetStatusNeed = [StatusID.PhysicalAversion];
		setting.TargetStatusProvide = [StatusID.LostRampage];
		setting.CreateConfig = () => new ActionConfig
		{
			AoeCount = 2,
		};
	}
}