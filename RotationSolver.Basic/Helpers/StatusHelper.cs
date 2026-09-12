using Dalamud.Game.ClientState.Statuses;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// The helper for the status.
/// </summary>
public static class StatusHelper
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

	/// <summary>
	/// Determines if the specified battle character has reached the maximum number of status effects.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// <c>true</c> if the character's status list is at the cap (30 for players, 60 for NPCs); otherwise, <c>false</c>.
	/// </returns>
	public unsafe static bool IsStatusCapped(IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		try
		{
			if (battleChara.StatusList == null)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}

		if (battleChara.IsValid())
		{
			var count = 0;
			foreach (var x in battleChara.StatusList)
			{
				if (x.StatusId != 0)
				{
					count++;
				}
			}
			if (count == battleChara.Struct()->StatusManager.NumValidStatuses)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Check whether the Player needs to be healing.
	/// </summary>
	/// <returns></returns>
	public static bool PlayerNoNeedHealingInvuln()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerWillStatusEndGCD(2, 0, false, NoNeedHealingStatus);
	}

	/// <summary>
	/// Check whether the target needs to be healing.
	/// </summary>
	/// <param name="Invulnp"></param>
	/// <returns></returns>
	public static bool NoNeedHealingInvuln(this IBattleChara Invulnp)
	{
		return Invulnp.WillStatusEndGCD(2, 0, false, NoNeedHealingStatus);
	}

	/// <summary>
	/// Check if the Player needs to be healed because of Doomed To Heal status.
	/// </summary>
	/// <returns></returns>
	public static bool PlayerDoomNeedHealing()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerHasStatus(false, DoomHealStatus);
	}

	/// <summary>
	/// Check if the target needs to be healed because of Doomed To Heal status.
	/// </summary>
	/// <param name="Doomp"></param>
	/// <returns></returns>
	public static bool DoomNeedHealing(this IBattleChara Doomp)
	{
		if (Doomp == null)
		{
			return false;
		}

		if (!Doomp.IsValid())
		{
			return false;
		}

		// Extra defensive checks to shrink the window between the IsValid()
		// check above and the native StatusList access below. Objects can be
		// invalidated (despawned/removed from the object table) between the
		// two, and some entity kinds do not expose a usable StatusManager.
		if (Doomp.Address == nint.Zero || Doomp.EntityId == 0)
		{
			return false;
		}

		try
		{
			// Doomp may have been captured on an earlier frame (e.g. via DataCenter.PartyMembers,
			// populated by the Update tick) and could have been despawned/invalidated by the game
			// since - even though the checks above still pass on the held reference. Re-resolving
			// from the live object table immediately before the native StatusList access below
			// narrows that staleness window as much as possible.
			if (Svc.Objects.SearchById(Doomp.GameObjectId) is not IBattleChara fresh || !fresh.IsValid())
			{
				return false;
			}

			if (fresh.StatusList == null)
			{
				return false;
			}

			if (HasStatus(fresh, false, DoomHealStatus))
			{
				return true;
			}
		}
		catch (Exception)
		{
			return false;
		}

		return false;
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="gcdCount"/> GCDs plus <paramref name="offset"/> seconds?
	/// </summary>
	/// <param name="gcdCount"></param>
	/// <param name="offset"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerWillStatusEndGCD(uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerWillStatusEnd(DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="gcdCount"/> GCDs plus <paramref name="offset"/> seconds?
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="gcdCount"></param>
	/// <param name="offset"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool WillStatusEndGCD(this IBattleChara battleChara, uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		return WillStatusEnd(battleChara, DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
	/// </summary>
	/// <param name="time"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerWillStatusEnd(float time, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (PlayerHasApplyStatus(statusIDs))
		{
			return false;
		}

		var statusTime = PlayerStatusTime(isFromSelf, statusIDs);
		return (statusTime >= 0f || !PlayerHasStatus(isFromSelf, statusIDs)) && statusTime <= time;
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="time"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool WillStatusEnd(this IBattleChara battleChara, float time, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (HasApplyStatus(battleChara, statusIDs))
		{
			return false;
		}

		var statusTime = battleChara.StatusTime(isFromSelf, statusIDs);
		return (statusTime >= 0f || !battleChara.HasStatus(isFromSelf, statusIDs)) && statusTime <= time;
	}

	/// <summary>
	/// Get the remaining time of the status (raw remaining time of the earliest matching status). Returns 0 if none.
	/// NOTE: Previously this subtracted DefaultGCDRemain which caused premature refresh decisions.
	/// </summary>
	public static float PlayerStatusTime(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return 0f;
		}

		try
		{
			if (PlayerHasApplyStatus(statusIDs))
			{
				return float.MaxValue;
			}

			var min = MinStatusRemainingTime(Player.Object, isFromSelf, statusIDs, out var found);
			// Return 0 when not found (legacy behaviour expected by callers), otherwise raw remaining time.
			return !found ? 0f : min;
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to get status time: {ex.Message}");
			return 0f;
		}
	}

	/// <summary>
	/// Get the remaining time of the status (raw remaining time of the earliest matching status). Returns 0 if none.
	/// NOTE: Previously this subtracted DefaultGCDRemain which caused premature refresh decisions.
	/// </summary>
	public static float StatusTime(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		try
		{
			if (HasApplyStatus(battleChara, statusIDs))
			{
				return float.MaxValue;
			}

			var min = MinStatusRemainingTime(battleChara, isFromSelf, statusIDs, out var found);
			// Return 0 when not found (legacy behaviour expected by callers), otherwise raw remaining time.
			return !found ? 0f : min;
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to get status time: {ex.Message}");
			return 0f;
		}
	}

	/// <summary>
	/// Get the stack count of the status.
	/// </summary>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static byte PlayerStatusStack(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return 0;
		}

		if (PlayerHasApplyStatus(statusIDs))
		{
			return byte.MaxValue;
		}

		var min = MinStatusStack(Player.Object, isFromSelf, statusIDs, out var found);
		return found ? min : (byte)0;
	}

	/// <summary>
	/// Get the stack count of the status.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static byte StatusStack(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (HasApplyStatus(battleChara, statusIDs))
		{
			return byte.MaxValue;
		}

		var min = MinStatusStack(battleChara, isFromSelf, statusIDs, out var found);
		return found ? min : (byte)0;
	}

	/// <summary>
	/// Check if the player object has any of the specified statuses.
	/// </summary>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerHasStatus(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (Player.Object.StatusList == null)
		{
			return false;
		}

		if (PlayerHasApplyStatus(statusIDs))
		{
			return true;
		}

		return AnyStatusMatches(Player.Object, isFromSelf, statusIDs);
	}

	/// <summary>
	/// Check if the object has any of the specified statuses.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool HasStatus(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		try
		{
			if (!DataCenter.PlayerAvailable())
			{
				return false;
			}

			if (Player.Object == null)
			{
				return false;
			}

			if (Player.Object.StatusList == null)
			{
				return false;
			}

			if (battleChara == null)
			{
				return false;
			}

			if (!battleChara.IsValid())
			{
				return false;
			}

			if (battleChara.StatusList == null)
			{
				return false;
			}

			if (HasApplyStatus(battleChara, statusIDs))
			{
				return true;
			}

			return AnyStatusMatches(battleChara, isFromSelf, statusIDs);
		}
		catch
		{
			// StatusList threw, treat as unavailable
			return false;
		}
	}

	/// <summary>
	/// Checks if the player currently has any of the statuses being applied,
	/// as tracked by <c>DataCenter.ApplyStatus</c> during effect time.
	/// </summary>
	/// <param name="statusIDs">An array of status IDs to check against the applied status.</param>
	/// <returns>
	/// <c>true</c> if any of the specified statuses are currently being applied to the character; otherwise, <c>false</c>.
	/// </returns>
	public static bool PlayerHasApplyStatus(ReadOnlySpan<StatusID> statusIDs)
	{
		if (!DataCenter.PlayerAvailable())
		{
			return false;
		}

		if (Player.Object == null)
		{
			return false;
		}

		try
		{
			if (Player.Object.StatusList == null)
			{
				return false;
			}
		}
		catch
		{
			// StatusList threw, treat as unavailable
			return false;
		}

		if (DataCenter.InEffectTime && DataCenter.ApplyStatus.TryGetValue(Player.Object.GameObjectId, out var statusId))
		{
			foreach (var s in statusIDs)
			{
				if ((uint)s == statusId)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Checks if the specified <paramref name="battleChara"/> currently has any of the statuses being applied,
	/// as tracked by <c>DataCenter.ApplyStatus</c> during effect time.
	/// </summary>
	/// <param name="battleChara">The battle character to check for applied statuses.</param>
	/// <param name="statusIDs">An array of status IDs to check against the applied status.</param>
	/// <returns>
	/// <c>true</c> if any of the specified statuses are currently being applied to the character; otherwise, <c>false</c>.
	/// </returns>
	public static bool HasApplyStatus(this IBattleChara battleChara, ReadOnlySpan<StatusID> statusIDs)
	{
		try
		{
			if (battleChara.StatusList == null)
			{
				return false;
			}
		}
		catch
		{
			// StatusList threw, treat as unavailable
			return false;
		}

		if (DataCenter.InEffectTime && DataCenter.ApplyStatus.TryGetValue(battleChara.GameObjectId, out var statusId))
		{
			foreach (var s in statusIDs)
			{
				if ((uint)s == statusId)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Remove the specified status.
	/// </summary>
	/// <param name="status"></param>
	public static void StatusOff(StatusID status)
	{
		if (!DataCenter.IsActivated())
		{
			return;
		}

		if (!PlayerHasStatus(false, status))
		{
			return;
		}

		try
		{
			Chat.SendMessage($"/statusoff \"{GetStatusName(status)}\"");
			PluginLog.Information($"Status {GetStatusName(status)} removed successfully.");
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to remove status {GetStatusName(status)}: {ex.Message}");
		}
	}

	/// <summary>
	/// Get the name of the specified status.
	/// </summary>
	/// <param name="id">The status ID.</param>
	/// <returns>The name of the status.</returns>
	internal static string GetStatusName(StatusID id)
	{
		var sheet = Service.GetSheet<Lumina.Excel.Sheets.Status>();
		if (sheet == null)
		{
			return string.Empty;
		}

		var statusRow = sheet.GetRow((uint)id);
		return statusRow.RowId == 0 ? string.Empty : statusRow.Name.ToString() ?? string.Empty;
	}

	// Linear membership check to avoid HashSet allocation (statusIDs is small in practice)
	private static bool ContainsId(uint id, ReadOnlySpan<StatusID> ids)
	{
		for (var i = 0; i < ids.Length; i++)
		{
			if ((uint)ids[i] == id)
			{
				return true;
			}
		}
		return false;
	}

	// Shared scan core for HasStatus/StatusTime/StatusStack: params StatusID[] would allocate an
	// array on every call (these are some of the most frequently called methods in the plugin), and
	// ReadOnlySpan<T> can't cross a yield return, so the status list is scanned directly here instead
	// of through an IEnumerable<IStatus> iterator.
	private static bool TryGetStatusList(IBattleChara? battleChara, out StatusList? statusList)
	{
		statusList = null;
		if (battleChara == null)
		{
			return false;
		}

		try
		{
			statusList = battleChara.StatusList;
			return statusList != null;
		}
		catch
		{
			// StatusList threw, treat as unavailable
			return false;
		}
	}

	/// <summary>
	/// Returns true if any of <paramref name="statusIDs"/> is present on <paramref name="battleChara"/>.
	/// </summary>
	private static bool AnyStatusMatches(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs)
	{
		if (!TryGetStatusList(battleChara, out var statusList))
		{
			return false;
		}

		var playerId = Player.Object?.GameObjectId ?? 0;
		for (var i = 0; i < statusList!.Length; i++)
		{
			var status = statusList[i];
			if (status == null || status.StatusId == 0)
			{
				continue;
			}

			if (isFromSelf && status.SourceId != playerId && status.SourceObject?.OwnerId != playerId)
			{
				continue;
			}

			if (ContainsId(status.StatusId, statusIDs))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns the minimum remaining time among the statuses in <paramref name="statusIDs"/> present on
	/// <paramref name="battleChara"/>; <paramref name="found"/> is false when none match.
	/// </summary>
	internal static float MinStatusRemainingTime(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs, out bool found)
	{
		found = false;
		var min = float.MaxValue;
		if (!TryGetStatusList(battleChara, out var statusList))
		{
			return min;
		}

		var playerId = Player.Object?.GameObjectId ?? 0;
		for (var i = 0; i < statusList!.Length; i++)
		{
			var status = statusList[i];
			if (status == null || status.StatusId == 0)
			{
				continue;
			}

			if (isFromSelf && status.SourceId != playerId && status.SourceObject?.OwnerId != playerId)
			{
				continue;
			}

			if (ContainsId(status.StatusId, statusIDs))
			{
				var t = status.RemainingTime == 0f ? float.MaxValue : status.RemainingTime;
				if (t < min)
				{
					min = t;
				}
				found = true;
			}
		}
		return min;
	}

	/// <summary>
	/// Returns the minimum stack count among the statuses in <paramref name="statusIDs"/> present on
	/// <paramref name="battleChara"/>; <paramref name="found"/> is false when none match.
	/// </summary>
	private static byte MinStatusStack(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs, out bool found)
	{
		found = false;
		var min = byte.MaxValue;
		if (!TryGetStatusList(battleChara, out var statusList))
		{
			return min;
		}

		var playerId = Player.Object?.GameObjectId ?? 0;
		for (var i = 0; i < statusList!.Length; i++)
		{
			var status = statusList[i];
			if (status == null || status.StatusId == 0)
			{
				continue;
			}

			if (isFromSelf && status.SourceId != playerId && status.SourceObject?.OwnerId != playerId)
			{
				continue;
			}

			if (ContainsId(status.StatusId, statusIDs))
			{
				var s = (byte)(status.Param == 0 ? byte.MaxValue : status.Param);
				if (s < min)
				{
					min = s;
				}
				found = true;
			}
		}
		return min;
	}


	/// <summary>
	/// Check if the status is invincible.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status is invincible, otherwise false.</returns>
	public static bool IsInvincible(this IStatus status)
	{
		if (status == null)
		{
			return false;
		}

		if (status.GameData.Value.Icon == 15024)
		{
			return true;
		}

		if (OtherConfiguration.InvincibleStatus == null)
		{
			return false;
		}

		foreach (var id in OtherConfiguration.InvincibleStatus)
		{
			if (id == status.StatusId)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Check if the status is a priority.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status is a priority, otherwise false.</returns>
	public static bool IsPriority(this IStatus status)
	{
		if (status == null)
		{
			return false;
		}

		if (OtherConfiguration.PriorityStatus == null)
		{
			return false;
		}

		foreach (var id in OtherConfiguration.PriorityStatus)
		{
			if (id == status.StatusId)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Check if the status needs to be dispelled immediately.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status needs to be dispelled, otherwise false.</returns>
	public static bool IsDangerous(this IStatus status)
	{
		if (status == null)
		{
			return false;
		}

		if (!status.CanDispel())
		{
			return false;
		}

		// Catch all doom statuses that use the Doom icon
		if (status.GameData.Value.Icon == 215503 && status.RemainingTime > 3)
		{
			return true;
		}

		if (status.Param > 2)
		{
			return true;
		}

		if (status.RemainingTime > 20)
		{
			return true;
		}

		if (OtherConfiguration.DangerousStatus == null)
		{
			return false;
		}

		foreach (var id in OtherConfiguration.DangerousStatus)
		{
			if (id == status.StatusId)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Check if the status can be dispelled.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status can be dispelled, otherwise false.</returns>
	public static bool CanDispel(this IStatus status)
	{
		return status != null && status.GameData.Value.CanDispel == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool CanStatusOff(this IStatus status)
	{
		return status != null && status.GameData.Value.CanStatusOff == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockActions(this IStatus status)
	{
		return status != null && status.GameData.Value.LockActions == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockMovement(this IStatus status)
	{
		return status != null && status.GameData.Value.LockMovement == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockControl(this IStatus status)
	{
		return status != null && status.GameData.Value.LockControl == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// Unknown3 is used to determine if the status indicates a tether.
	/// </summary>
	public static bool IsTether(this IStatus status)
	{
		return status != null && status.GameData.Value.Unknown3 == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}
}
