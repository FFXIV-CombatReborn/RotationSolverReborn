namespace RotationSolver.Engine.Helpers;

public static partial class StatusHelper
{
	/// <summary>
	/// 
	/// </summary>
	public static readonly Dictionary<uint, StatusID[]> NorthHornWeaknessByNameId = new()
	{
		{ 13876, [StatusID.LightningWeakness] },
		{ 13939, [StatusID.IceWeakness] },
		{ 13940, [StatusID.WindWeakness] },
		{ 13941, [StatusID.FireWeakness] },
		{ 13942, [StatusID.LightningWeakness] },
		{ 14490, [StatusID.WindWeakness] },
		{ 14491, [StatusID.FireWeakness] },
		{ 14503, [StatusID.IceWeakness] },
		{ 14505, [StatusID.LightningWeakness] },
		{ 14508, [StatusID.LightningWeakness] },
		{ 14509, [StatusID.LightningWeakness] },
		{ 14511, [StatusID.WindWeakness] },
		{ 14512, [StatusID.WindWeakness] },
		{ 14517, [StatusID.WindWeakness] },
		{ 14520, [StatusID.FireWeakness] },
		{ 14523, [StatusID.IceWeakness] },
		{ 14714, [StatusID.FireWeakness] },
		{ 14717, [StatusID.FireWeakness, StatusID.WindWeakness] },
		{ 14719, [StatusID.IceWeakness] },
		{ 14720, [StatusID.IceWeakness] },
		{ 14726, [StatusID.FireWeakness] },
		{ 14728, [StatusID.LightningWeakness] },
		{ 14735, [StatusID.FireWeakness] },
		{ 14736, [StatusID.IceWeakness] },
		{ 14738, [StatusID.FireWeakness] },
		{ 14747, [StatusID.FireWeakness] },
		{ 14762, [StatusID.FireWeakness] },
		{ 14764, [StatusID.LightningWeakness, StatusID.WindWeakness] },
		{ 14765, [StatusID.FireWeakness] },
		{ 14767, [StatusID.WindWeakness] },
		{ 14771, [StatusID.FireWeakness] },
		{ 14772, [StatusID.IceWeakness] },
		{ 14774, [StatusID.LightningWeakness] },
		{ 14775, [StatusID.LightningWeakness] },
		{ 14776, [StatusID.FireWeakness] },
		{ 14785, [StatusID.FireWeakness] },
		{ 14787, [StatusID.FireWeakness] },
		{ 14789, [StatusID.FireWeakness] },
		{ 14790, [StatusID.FireWeakness] },
		{ 14791, [StatusID.IceWeakness] },
		{ 14795, [StatusID.LightningWeakness] },
		{ 14799, [StatusID.LightningWeakness] },
		{ 14800, [StatusID.LightningWeakness] },
		{ 14801, [StatusID.WindWeakness] },
		{ 14802, [StatusID.LightningWeakness] },
		{ 14804, [StatusID.LightningWeakness] },
		{ 14805, [StatusID.IceWeakness] },
		{ 14806, [StatusID.LightningWeakness] },
		{ 14809, [StatusID.LightningWeakness] },
		{ 14817, [StatusID.LightningWeakness] },
		{ 14820, [StatusID.LightningWeakness] },
		{ 14840, [StatusID.IceWeakness] },
		{ 14841, [StatusID.IceWeakness] },
		{ 14857, [StatusID.LightningWeakness] },
		{ 14858, [StatusID.LightningWeakness] },
		{ 14859, [StatusID.LightningWeakness] },
		{ 14860, [StatusID.FireWeakness] },
		{ 14861, [StatusID.FireWeakness] },
		{ 14862, [StatusID.FireWeakness] },
		{ 14863, [StatusID.IceWeakness] },
		{ 14864, [StatusID.WindWeakness] },
		{ 14865, [StatusID.FireWeakness] },
		{ 14866, [StatusID.FireWeakness] },
		{ 14867, [StatusID.IceWeakness] },
		{ 14868, [StatusID.LightningWeakness] },
		{ 14869, [StatusID.FireWeakness, StatusID.IceWeakness] },
		{ 14870, [StatusID.FireWeakness] },
		{ 14871, [StatusID.FireWeakness] },
		{ 14872, [StatusID.WindWeakness] },
		{ 14873, [StatusID.WindWeakness] },
		{ 14874, [StatusID.LightningWeakness] },
		{ 14875, [StatusID.LightningWeakness, StatusID.IceWeakness] },
		{ 14876, [StatusID.LightningWeakness] },
		{ 14877, [StatusID.LightningWeakness] },
		{ 14878, [StatusID.FireWeakness, StatusID.WindWeakness] },
		{ 14879, [StatusID.IceWeakness] },
		{ 14880, [StatusID.FireWeakness] },
		{ 14881, [StatusID.IceWeakness] },
		{ 14882, [StatusID.FireWeakness] },
		{ 14883, [StatusID.IceWeakness, StatusID.WindWeakness] },
		{ 14884, [StatusID.WindWeakness] },
		{ 14885, [StatusID.FireWeakness] },
		{ 14886, [StatusID.FireWeakness] },
		{ 14887, [StatusID.FireWeakness] },
		{ 14888, [StatusID.FireWeakness] },
		{ 14889, [StatusID.IceWeakness] },
		{ 14890, [StatusID.LightningWeakness] },
		{ 14891, [StatusID.LightningWeakness] },
		{ 14892, [StatusID.FireWeakness] },
		{ 14893, [StatusID.FireWeakness] },
		{ 14894, [StatusID.FireWeakness] },
		{ 14895, [StatusID.IceWeakness] },
		{ 14896, [StatusID.WindWeakness] },
		{ 14897, [StatusID.IceWeakness] },
		{ 14898, [StatusID.IceWeakness] },
		{ 14899, [StatusID.WindWeakness] },
		{ 14900, [StatusID.LightningWeakness] },
		{ 14901, [StatusID.LightningWeakness] },
		{ 14902, [StatusID.IceWeakness] },
		{ 14903, [StatusID.WindWeakness] },
		{ 14904, [StatusID.WindWeakness] },
		{ 14905, [StatusID.LightningWeakness] },
		{ 14906, [StatusID.IceWeakness] },
		{ 14907, [StatusID.WindWeakness] },
		{ 14908, [StatusID.IceWeakness, StatusID.WindWeakness] },
		{ 14909, [StatusID.FireWeakness] },
		{ 14910, [StatusID.WindWeakness] },
		{ 14911, [StatusID.WindWeakness] },
		{ 14912, [StatusID.WindWeakness] },
		{ 14913, [StatusID.FireWeakness] },
		{ 14914, [StatusID.IceWeakness] },
		{ 14915, [StatusID.FireWeakness] },
		{ 14917, [StatusID.WindWeakness] },
		{ 14918, [StatusID.WindWeakness] },
		{ 14919, [StatusID.FireWeakness] },
		{ 14920, [StatusID.IceWeakness] },
		{ 14921, [StatusID.WindWeakness] },
		{ 14922, [StatusID.LightningWeakness] },
		{ 14923, [StatusID.WindWeakness] },
		{ 14929, [StatusID.FireWeakness] },
		{ 14930, [StatusID.WindWeakness] },
		{ 14931, [StatusID.FireWeakness] },
		{ 14932, [StatusID.IceWeakness] },
	};

	/// <summary>
	/// 
	/// </summary>
	public static readonly Dictionary<uint, StatusID[]> SouthHornWeaknessByNameId = new()
	{
		{ 13638, [StatusID.FireWeakness] },
		{ 13646, [StatusID.FireWeakness] },
		{ 13656, [StatusID.IceWeakness] },
		{ 13666, [StatusID.IceWeakness] },
		{ 13668, [StatusID.IceWeakness] },
		{ 13703, [StatusID.FireWeakness] },
		{ 13717, [StatusID.LightningWeakness] },
		{ 13718, [StatusID.LightningWeakness] },
		{ 13726, [StatusID.LightningWeakness] },
		{ 13728, [StatusID.LightningWeakness] },
		{ 13729, [StatusID.LightningWeakness] },
		{ 13740, [StatusID.FireWeakness] },
		{ 13747, [StatusID.LightningWeakness] },
		{ 13748, [StatusID.LightningWeakness] },
		{ 13853, [StatusID.WindWeakness] },
		{ 13855, [StatusID.WindWeakness] },
		{ 13871, [StatusID.FireWeakness] },
		{ 13872, [StatusID.WindWeakness] },
		{ 13873, [StatusID.IceWeakness] },
		{ 13874, [StatusID.FireWeakness] },
		{ 13875, [StatusID.FireWeakness] },
		{ 13877, [StatusID.FireWeakness] },
		{ 13878, [StatusID.FireWeakness] },
		{ 13879, [StatusID.LightningWeakness] },
		{ 13880, [StatusID.IceWeakness] },
		{ 13881, [StatusID.LightningWeakness] },
		{ 13882, [StatusID.IceWeakness] },
		{ 13883, [StatusID.LightningWeakness] },
		{ 13884, [StatusID.FireWeakness] },
		{ 13885, [StatusID.IceWeakness] },
		{ 13886, [StatusID.FireWeakness] },
		{ 13887, [StatusID.FireWeakness] },
		{ 13888, [StatusID.IceWeakness] },
		{ 13890, [StatusID.FireWeakness] },
		{ 13891, [StatusID.LightningWeakness] },
		{ 13892, [StatusID.WindWeakness] },
		{ 13893, [StatusID.FireWeakness] },
		{ 13894, [StatusID.FireWeakness] },
		{ 13895, [StatusID.FireWeakness] },
		{ 13896, [StatusID.IceWeakness] },
		{ 13897, [StatusID.IceWeakness] },
		{ 13898, [StatusID.WindWeakness] },
		{ 13900, [StatusID.FireWeakness] },
		{ 13901, [StatusID.WindWeakness] },
		{ 13902, [StatusID.IceWeakness] },
		{ 13903, [StatusID.LightningWeakness] },
		{ 13904, [StatusID.IceWeakness] },
		{ 13906, [StatusID.FireWeakness] },
		{ 13907, [StatusID.IceWeakness] },
		{ 13908, [StatusID.IceWeakness] },
		{ 13909, [StatusID.IceWeakness] },
		{ 13911, [StatusID.FireWeakness] },
		{ 13912, [StatusID.LightningWeakness] },
		{ 13913, [StatusID.LightningWeakness] },
		{ 13915, [StatusID.LightningWeakness] },
		{ 13916, [StatusID.IceWeakness] },
		{ 13917, [StatusID.LightningWeakness] },
		{ 13918, [StatusID.IceWeakness] },
		{ 13919, [StatusID.FireWeakness] },
		{ 13922, [StatusID.WindWeakness] },
		{ 13924, [StatusID.LightningWeakness] },
		{ 13925, [StatusID.FireWeakness] },
		{ 13926, [StatusID.FireWeakness] },
		{ 13928, [StatusID.LightningWeakness] },
		{ 13930, [StatusID.IceWeakness] },
		{ 13931, [StatusID.FireWeakness] },
		{ 13932, [StatusID.FireWeakness] },
		{ 13933, [StatusID.LightningWeakness] },
		{ 13934, [StatusID.LightningWeakness] },
		{ 13935, [StatusID.WindWeakness] },
		{ 13936, [StatusID.LightningWeakness] },
		{ 13937, [StatusID.LightningWeakness] },
		{ 13938, [StatusID.IceWeakness] },
	};

	/// <summary>
	/// The elemental weakness statuses used in Occult Crescent
	/// </summary>
	public static StatusID[] OccultWeaknessStatuses { get; } =
	[
		StatusID.LightningWeakness,
		StatusID.FireWeakness,
		StatusID.IceWeakness,
		StatusID.WindWeakness,
	];

	/// <summary>
	/// 
	/// </summary>
	public static bool IsOccultWeaknessStatus(StatusID status)
	{
		return Array.IndexOf(OccultWeaknessStatuses, status) >= 0;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool HasKnownOccultWeakness(uint nameId)
	{
		if (DataCenter.IsInNorthHorn)
		{
			if (nameId != 0 && NorthHornWeaknessByNameId.ContainsKey(nameId))
			{
				return true;
			}
		}

		if (DataCenter.IsInSouthHorn)
		{
			if (nameId != 0 && SouthHornWeaknessByNameId.ContainsKey(nameId))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool HasKnownOccultWeakness(uint nameId, StatusID status)
	{
		if (DataCenter.IsInNorthHorn)
		{
			if (nameId != 0 && NorthHornWeaknessByNameId.TryGetValue(nameId, out var known) && known != null && Array.IndexOf(known, status) >= 0)
			{
				return true;
			}
		}

		if (DataCenter.IsInSouthHorn)
		{
			if (nameId != 0 && SouthHornWeaknessByNameId.TryGetValue(nameId, out var known) && known != null && Array.IndexOf(known, status) >= 0)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] RangePhysicalDefense { get; } =
	[
		StatusID.Troubadour,
		StatusID.Tactician_1951,
		StatusID.Tactician_2177,
		StatusID.ShieldSamba,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] PhysicalResistance { get; } =
	[
		StatusID.IceSpikes_1720,
		StatusID.IceSpikes_1307
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] PhysicalRangedResistance { get; } =
	[
		StatusID.RangedResistance,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] MagicResistance { get; } =
	[
		StatusID.MagicResistance,
		StatusID.MagicResistance_3621,
		StatusID.RepellingSpray_556,
		StatusID.MagitekField_2166,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] AreaHots { get; } =
	[
		StatusID.AspectedHelios,
		StatusID.HeliosConjunction,
		StatusID.MedicaIi,
		StatusID.TrueMedicaIi,
		StatusID.PhysisIi,
		StatusID.Physis,
		StatusID.SacredSoil_1944,
		StatusID.WhisperingDawn,
		StatusID.AngelsWhisper,
		StatusID.Seraphism_3885,
		StatusID.Asylum_1911,
		StatusID.DivineAura,
		StatusID.MedicaIii_3986,
		StatusID.MedicaIii
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] SingleHots { get; } =
	[
		StatusID.AspectedBenefic,
		StatusID.Regen,
		StatusID.Regen_897,
		StatusID.Regen_1330,
		StatusID.TheEwer_3891,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] TankStanceStatus { get; } =
	[
		StatusID.Grit,
		StatusID.RoyalGuard_1833,
		StatusID.IronWill,
		StatusID.Defiance,
		StatusID.Defiance_3124,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] NoNeedHealingStatus { get; } =
	[
		StatusID.Holmgang_409,
		StatusID.LivingDead,
		//StatusID.WalkingDead,
		StatusID.Superbolide,
		StatusID.HallowedGround,
		StatusID.Invulnerability,
		StatusID.HpRecoveryDown,
		StatusID.Mounted,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] SwiftcastStatus { get; } =
	[
		StatusID.Swiftcast,
		StatusID.Triplecast,
		StatusID.Dualcast,
		StatusID.Dualcast_5438,
		StatusID.OccultQuick,
		StatusID.LostChainspell
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] AstCardStatus { get; } =
	[
		StatusID.TheBalance_3887,
		StatusID.TheSpear_3889,
		StatusID.Weakness,
		StatusID.BrinkOfDeath,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] RampartStatus { get; } =
	[
		StatusID.Rampart,
		StatusID.Rampart_1191,
		StatusID.Rampart_1978,
		StatusID.Rampart_4168,
		StatusID.Bulwark,
		StatusID.Bloodwhetting,

		StatusID.Vengeance,
		StatusID.Damnation,

		StatusID.Sentinel,
		StatusID.Guardian,

		StatusID.ShadowWall,
		StatusID.ShadowedVigil,

		StatusID.Nebula,
		StatusID.GreatNebula,

		StatusID.Superbolide,
		StatusID.HallowedGround,
		StatusID.Holmgang_409,
		StatusID.LivingDead,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] NoPositionalStatus { get; } =
	[
		StatusID.TrueNorth,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] DoomHealStatus { get; } =
	[
		StatusID.Doom_1769,
		StatusID.Doom_5473
	];

	/// <summary>
	/// M9S Hell in a Cell statuses on the player. Each one pairs with the <see cref="HeelOfTheCellStatus"/> at the same index.
	/// </summary>
	public static StatusID[] HellInACellStatus { get; } =
	[
		StatusID.HellInACell,
		StatusID.HellInACell_4732,
		StatusID.HellInACell_4733,
		StatusID.HellInACell_4734,
		StatusID.HellInACell_4735,
		StatusID.HellInACell_4736,
		StatusID.HellInACell_4737,
		StatusID.HellInACell_4738,
	];

	/// <summary>
	/// M9S Heel in a Cell statuses on the Charnel Cells. Each one pairs with the <see cref="HellInACellStatus"/> at the same index.
	/// </summary>
	public static StatusID[] HeelOfTheCellStatus { get; } =
	[
		StatusID.HeelOfTheCell,
		StatusID.HeelOfTheCell_4740,
		StatusID.HeelOfTheCell_4741,
		StatusID.HeelOfTheCell_4742,
		StatusID.HeelOfTheCell_4743,
		StatusID.HeelOfTheCell_4744,
		StatusID.HeelOfTheCell_4745,
		StatusID.HeelOfTheCell_4746,
	];

	/// <summary>
	/// Statuses for the Phantom Oracle spell PredictPvE.
	/// </summary>
	public static StatusID[] OracleStatuses { get; } =
	[
		StatusID.PredictionOfCleansing,
		StatusID.PredictionOfStarfall,
		StatusID.PredictionOfJudgment,
		StatusID.PredictionOfBlessing
	];

	/// <summary>
	/// Statuses that can be dispelled by Occult Dispel.
	/// </summary>
	public static StatusID[] PhantomDispellable { get; } =
	[
		StatusID.DamageUp_1161,
		StatusID.DamageUp,
		StatusID.DarkDefenses,
		StatusID.MagicDamageUp_2556,
		StatusID.EvasionUp_1706
        //StatusID.Invincibility_4539 maybe this, need verification, seems to be on guardian knight in Rooms & Lockwards part of forked tower
    ];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] PurifyPvPStatuses { get; } =
	[
		StatusID.Stun_1343,
		StatusID.Heavy_1344,
		StatusID.Bind_1345,
		StatusID.Silence_1347,
		StatusID.DeepFreeze_3219,
		StatusID.MiracleOfNature,
	];

	/// <summary>
	/// 
	/// </summary>
	public static StatusID[] RotationLockoutStatus { get; } =
	[
		StatusID.Reawakened,
		StatusID.Overheated,
		StatusID.InnerRelease,
		StatusID.Eukrasia,
		StatusID.Mudra,
		StatusID.TenChiJin,
		StatusID.FullMetalMachinist
	];
}
