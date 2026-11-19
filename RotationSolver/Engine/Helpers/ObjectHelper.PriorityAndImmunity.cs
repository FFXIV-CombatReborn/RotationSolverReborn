using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace RotationSolver.Engine.Helpers;

public static partial class ObjectHelper
{
	internal static int GetNamePlateIconPriority(uint icon)
	{
		return icon switch
		{
			60687 => 1, // Treasure hunt icon 1
			60688 => 2, // Treasure hunt icon 2
			60689 => 3, // Treasure hunt icon 3
			60690 => 4, // Treasure hunt icon 4
			60691 => 5, // Treasure hunt icon 5
			_ => int.MaxValue
		};
	}

	internal static bool IsTopPriorityNamedHostile(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		foreach (var id in DataCenter.PrioritizedNameIds)
		{
			if (battleChara.NameId == id)
			{
				return true;
			}
		}
		return false;
	}

	private const long TopPriorityTtlMs = 15;
	private static readonly Dictionary<ulong, (long Tick, bool IsPriority)> _topPriorityCache = [];

	internal static bool IsTopPriorityHostile(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		var id = battleChara.GameObjectId;
		var now = Environment.TickCount64;
		if (_topPriorityCache.TryGetValue(id, out var cached) && now - cached.Tick < TopPriorityTtlMs)
		{
			return cached.IsPriority;
		}

		if (_topPriorityCache.Count > 512)
		{
			_topPriorityCache.Clear();
		}

		var isPriority = IsTopPriorityHostileCore(battleChara);
		_topPriorityCache[id] = (now, isPriority);
		return isPriority;
	}

	private static bool IsTopPriorityHostileCore(IBattleChara battleChara)
	{
		var icon = battleChara.GetNamePlateIcon();

		if (battleChara.IsAllianceMember() || battleChara.IsParty())
		{
			return false;
		}

		if (Service.Config.Treasuredungeonnumbered && DataCenter.IsInTreasureHunt)
		{
			if (icon == 60687 || icon == 60688 || icon == 60689 || icon == 60690 || icon == 60691)
			{
				return true;
			}
		}

		if (Service.Config.Treasuredungeontimed && battleChara.TreasureDungeonPrio())
		{
			return true;
		}

		if (DataCenter.IsInFate && battleChara.IsForlorn())
		{
			return true;
		}

		if (battleChara.IsM10SavagePriority())
		{
			return true;
		}

		if (battleChara.IsM9SavagePriority())
		{
			return true;
		}

		if (battleChara.IsCrucibleThirdBoardPriority())
		{
			return true;
		}

		if (battleChara.IsSpecialInclusionPriority())
		{
			return true;
		}

		if (DataCenter.IsInOccultCrescentOp)
		{
			if (battleChara.IsOccultCEMob() && battleChara.NameId != 14717)
			{
				return true;
			}

			if (DataCenter.IsInNorthHorn)
			{
				if (battleChara.NameId == 14719)
				{
					return true;
				}
			}
		}

		// MCH prio targeting for Wildfire
		if (Player.Job == Job.MCH && (battleChara.HasStatus(true, StatusID.Wildfire) || battleChara.HasStatus(true, StatusID.Wildfire_1323)))
		{
			return true;
		}

		if (Service.Config.PrioAtomelith && DataCenter.IsPvP)
		{
			var IceBoundTomeLithA1 = battleChara.NameId == 4822;
			var IceBoundTomeLithA2 = battleChara.NameId == 4823;
			var IceBoundTomeLithA3 = battleChara.NameId == 4824;
			var IceBoundTomeLithA4 = battleChara.NameId == 4825;
			if (IceBoundTomeLithA1 || IceBoundTomeLithA2 || IceBoundTomeLithA3 || IceBoundTomeLithA4)
			{
				return true;
			}
		}

		if (Service.Config.PrioBtomelith && DataCenter.IsPvP)
		{
			var IceBoundTomeLithB1 = battleChara.NameId == 4826;
			var IceBoundTomeLithB2 = battleChara.NameId == 4827;
			var IceBoundTomeLithB3 = battleChara.NameId == 4828;
			var IceBoundTomeLithB4 = battleChara.NameId == 4829;
			var IceBoundTomeLithB5 = battleChara.NameId == 4830;
			var IceBoundTomeLithB6 = battleChara.NameId == 4831;
			var IceBoundTomeLithB7 = battleChara.NameId == 4832;
			var IceBoundTomeLithB8 = battleChara.NameId == 4833;
			var IceBoundTomeLithB9 = battleChara.NameId == 4834;
			var IceBoundTomeLithB10 = battleChara.NameId == 4835;
			var IceBoundTomeLithB11 = battleChara.NameId == 4836;
			var IceBoundTomeLithB12 = battleChara.NameId == 4837;
			var IceBoundTomeLithB13 = battleChara.NameId == 4840;
			var IceBoundTomeLithB14 = battleChara.NameId == 4841;
			var IceBoundTomeLithB15 = battleChara.NameId == 4842;
			var IceBoundTomeLithB16 = battleChara.NameId == 4843;
			var IceBoundTomeLithB17 = battleChara.NameId == 4844;
			var IceBoundTomeLithB18 = battleChara.NameId == 4845;

			if (IceBoundTomeLithB1 || IceBoundTomeLithB2 || IceBoundTomeLithB3 || IceBoundTomeLithB4 ||
				IceBoundTomeLithB5 || IceBoundTomeLithB6 || IceBoundTomeLithB7 || IceBoundTomeLithB8 ||
				IceBoundTomeLithB9 || IceBoundTomeLithB10 || IceBoundTomeLithB11 || IceBoundTomeLithB12 ||
				IceBoundTomeLithB13 || IceBoundTomeLithB14 || IceBoundTomeLithB15 || IceBoundTomeLithB16 ||
				IceBoundTomeLithB17 || IceBoundTomeLithB18)
			{
				return true;
			}
		}

		if (battleChara.HasPriorityStatus())
		{
			return true;
		}

		if (Service.Config.ChooseAttackMark)
		{
			var targets = MarkingHelper.GetAttackSignTargets();
			if (targets != null)
			{
				foreach (var id in targets)
				{
					if (id != 0 && id == (long)battleChara.GameObjectId && battleChara.IsEnemy())
					{
						return true;
					}
				}
			}
		}

		if (Service.Config.TargetFatePriority && DataCenter.PlayerFateId != 0 && battleChara.FateId() == DataCenter.PlayerFateId)
		{
			return true;
		}

		if (Service.Config.TargetHuntingRelicLevePriority && (icon == 60092 || icon == 60094 || icon == 60096 || icon == 60097 || icon == 60098 || icon == 71244))
		{
			return true;
		}
		//60092 Hunt Log
		//60094 Treasure Mob
		//60096 Relic Weapon
		//60097 Hunt Bill
		//60098 Crescent
		//71244 Leve Target

		// Quest
		if (Service.Config.TargetQuestPriority && (icon == 71204 || icon == 71144 || icon == 71224 || icon == 71344 || (battleChara.GetEventType() == EventHandlerContent.Quest) && icon != 0))
		{
			return true;
		}
		//71204 Main Quest
		//71144 Major Quest
		//71224 Other Quest
		//71344 Major Quest

		if (Service.Config.PrioEnemyParts && battleChara.GetBattleNPCSubKind() == BattleNpcSubKind.BNpcPart)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// True if a Deadly Doornail (NameId 14303) is currently in AllHostileTargets.
	/// </summary>
	public static bool HasDeadlyDoornail => AnyHostileHasNameId(14303);

	/// <summary>
	/// True if a Fatal Flail (NameId 14302) is currently in AllHostileTargets.
	/// </summary>
	public static bool HasFatalFlail => AnyHostileHasNameId(14302);

	/// <summary>
	/// M9S add priority: the Charnel Cell matching the player's Hell in a Cell, and Deadly Doornail or Fatal Flail depending on role.
	/// </summary>
	public static bool IsM9SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null || !Service.Config.M9SAdsTargeting || !DataCenter.IsInM9S)
		{
			return false;
		}

		var role = DataCenter.Role;
		var isMeleeOrTank = role is JobRole.Melee or JobRole.Tank;
		var priority = battleChara.NameId switch
		{
			14304 => IsPlayersCharnelCell(battleChara),
			// Deadly Doornail: ranged and healers always, melee and tanks only when it's close and no Fatal Flail is up.
			14303 => role is JobRole.RangedPhysical or JobRole.RangedMagical or JobRole.Healer
				|| (isMeleeOrTank && battleChara.DistanceToPlayer() < 5f && !HasFatalFlail),
			// Fatal Flail: melee and tanks always, everyone else only when no Deadly Doornail is up.
			14302 => isMeleeOrTank || !HasDeadlyDoornail,
			_ => false,
		};

		if (priority && Service.Config.InDebug)
		{
			PluginLog.Information($"IsM9SavagePriority: {battleChara.NameId} is a priority target for {role}");
		}

		return priority;
	}

	private static bool IsPlayersCharnelCell(IBattleChara cell)
	{
		for (var i = 0; i < StatusHelper.HeelOfTheCellStatus.Length; i++)
		{
			if (cell.HasStatus(false, StatusHelper.HeelOfTheCellStatus[i]) && StatusHelper.PlayerHasStatus(false, StatusHelper.HellInACellStatus[i]))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// M10 (Normal and Savage) priority: Red Hot while the player has Firesnaking, Deep Blue while the player has Watersnaking.
	/// </summary>
	public static bool IsM10SavagePriority(this IBattleChara battleChara)
	{
		if (Player.Object == null || !Service.Config.M10SBroTargeting || !(DataCenter.IsInM10S || DataCenter.TerritoryID == 1322))
		{
			return false;
		}

		var priority = battleChara.NameId switch
		{
			14370 => StatusHelper.PlayerHasStatus(false, StatusID.Firesnaking),
			14369 => StatusHelper.PlayerHasStatus(false, StatusID.Watersnaking),
			_ => false,
		};

		if (priority && Service.Config.InDebug)
		{
			PluginLog.Information($"IsM10SavagePriority: {battleChara.NameId} is a priority target");
		}

		return priority;
	}

	internal static bool IsCrucibleThirdBoardPriority(this IBattleChara battleChara)
	{
		if (!DataCenter.IsinThirdBoard)
		{
			return false;
		}

		return battleChara.NameId switch
		{
			14571 => Service.Config.CrucibleYmir && !battleChara.HasStatus(false, StatusID.ParalyzingSpikes), // Sahagin Piece
			14570 => Service.Config.CrucibleYmir && !HasUnspikedSahagin(), // Ymir Shell
			14569 => Service.Config.CrucibleYmir && !HasUnspikedSahagin() && !AnyLivingHostile(14570), // Ymir Piece
			14586 => Service.Config.CrucibleSiren, // Crawling Piece
			_ => false,
		};

		static bool HasUnspikedSahagin()
		{
			return AnyLivingHostile(14571, static s => !s.HasStatus(false, StatusID.ParalyzingSpikes));
		}
	}

	private static bool AnyLivingHostile(uint nameId, Func<IBattleChara, bool>? predicate = null)
	{
		foreach (var hostile in DataCenter.AllHostileTargets)
		{
			if (hostile != null && hostile.NameId == nameId && hostile.CurrentHp > 0 && (predicate == null || predicate(hostile)))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool IsSpecialInclusionPriority(this IBattleChara battleChara)
	{
		if (battleChara.NameId == 8145
			|| battleChara.NameId == 10259
			|| battleChara.NameId == 12704)
		{
			return true;
		}
		//8145 Root in Dohn Meg boss 2
		//10259 Cinduruva in The Tower of Zot
		//12704 Crystalline Debris

		//14052 hellmaker, only for the Cell Block C and D prisoners
		if (DataCenter.TerritoryID == 1292 && battleChara.NameId == 14052
			&& StatusHelper.PlayerHasStatus(false, (StatusID)4544, (StatusID)4545))
		{
			return true;
		}

		// striking shrublet - Floor 10 boss ads
		if (DataCenter.TerritoryID == 1281)
		{
			if (battleChara.NameId == 13980)
			{
				return true;
			}
		}

		// forgiven adulation - Floor 30 boss ads
		if (DataCenter.TerritoryID == 1284)
		{
			if (battleChara.NameId == 13978)
			{
				return true;
			}
		}

		return false;
	}

	internal static bool TreasureDungeonPrio(this IBattleChara battleChara)
	{
		if (DataCenter.IsInTreasureHunt)
		{
			if (DataCenter.IsInTheLostCanalsofUznair)
			{
				if (battleChara.IsNamed(NPCName.NamazuStickywhisker))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingAltarsofUznair)
			{
				if (battleChara.IsNamed(NPCName.GoldWhisker))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldWhisker_7625))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheHiddenCanalsofUznair)
			{
				if (battleChara.IsNamed(NPCName.NamazuStickywhisker))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.Abharamu))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheDungeonsofLyheGhiah)
			{
				if (battleChara.IsNamed(NPCName.FuathTrickster))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.TheKeeperOfTheKeys))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingOubliettesofLyheGhiah)
			{
				if (battleChara.IsNamed(NPCName.FuathTrickster_9774))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.TheKeeperOfTheKeys_9773))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheExcitatron6000)
			{
				if (battleChara.IsNamed(NPCName.RainbowGolem))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldenSupporter))
				{
					return true;
				}
			}

			if (DataCenter.IsInTheShiftingGymnasionAgonon)
			{
				if (battleChara.IsNamed(NPCName.GymnasiouLampas))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GymnasiouLyssa))
				{
					return true;
				}
			}

			if (DataCenter.IsInCenoteJaJaGural)
			{
				if (battleChara.IsNamed(NPCName.AlpacaOfFortune))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.UolonOfFortune))
				{
					return true;
				}
			}

			if (DataCenter.IsInVaultOneiron)
			{
				if (battleChara.IsNamed(NPCName.Vaultkeeper))
				{
					return true;
				}

				if (battleChara.IsNamed(NPCName.GoldyCat))
				{
					return true;
				}
			}
		}

		return false;
	}

	internal static bool IsForlorn(this IBattleChara battleChara)
	{
		if (battleChara.NameId == 6737
			|| battleChara.NameId == 6738)
		{
			return true;
		}
		//6737 forlorn maiden
		//6738 forlorn maiden

		return false;
	}

	/// <summary>
	/// List of NameIds that Undead enemies in Occult Crecent.
	/// </summary>
	private static readonly HashSet<uint> IsOCUndeadSet =
	[
		13741, //Lifereaper
        13924, //Armor
        13922, //Ghost
        13921, //Caoineag
        13926, //Gourmand
        13925, //Troubadour
        13923, //Geshunpest
        13927, //Dullahan
    ];

	/// <summary>
	/// Check to see if Occult Crecent target is Undead.
	/// </summary>
	public static bool IsOCUndeadTarget(this IBattleChara battleChara)
	{
		return IsOCUndeadSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCSlowgaImmuneSet =
	[
		13933, //Marolith
        13893, //AnimatedDoll
        13894, //AnimatedDoll
        13905, //Meraevis
        13888, //Triceratops

        13918, //Diplocaulus

        13936, //Zaratan
        13902, //Cetus
        13879, //Monk
        13911, //Panther

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Slowga.
	/// </summary>
	public static bool IsOCSlowgaImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCSlowgaImmuneSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCDoomImmuneSet =
	[
		13893, //AnimatedDoll
        13894, //AnimatedDoll
        13917, //Sculpture
        13888, //Triceratops
        13887, //Rosebear
        13935, //Harpuia
        13886, //Aetherscab
        13912, //Havoc

        13937, //Apa
        13743, //Goobbue
        13878, //Goobbue
        13892, //Dirty Eye
        13898, //Blackguard
        13901, //Demon Pawn
        13745, //Headstone
        13881, //Headstone
        13908, //Blood Demon

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Phantom Doom.
	/// </summary>
	public static bool IsOCDoomImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCDoomImmuneSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCStunImmuneSet =
	[
		13873, //Tormentor
        13891, //LionStatant
        13916, //Brachiosaur
        13887, //Rosebear
        13895, //Byblos
        13912, //Havoc

        13937, //Apa
        13928, //Crecent Golem
        13745, //Headstone
        13881, //Headstone
        13879, //Monk

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Stun.
	/// </summary>
	public static bool IsOCStunImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCStunImmuneSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCFreezeImmuneSet =
	[
		13876, //Fan
        13917, //Sculpture
        13916, //Brachiosaur
        13887, //Rosebear
        13744, //Taurus
        13880, //Taurus
        13909, //Dahak

        13919, //Zaghnal
        13934, //Uragnite
        13898, //Blackguard
        13901, //Demon Pawn
        13745, //Headstone
        13881, //Headstone

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Freeze.
	/// </summary>
	public static bool IsOCFreezeImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCFreezeImmuneSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCBlindImmuneSet =
	[
		13931, //Chaochu
        13874, //Snapweed
        13932, //Leshy
        13933, //Marolith
        13893, //AnimatedDoll
        13894, //AnimatedDoll
        13887, //Rosebear

        13930, //Flame
        13928, //Crecent Golem
        13745, //Headstone
        13881, //Headstone

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Blind.
	/// </summary>
	public static bool IsOCBlindImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCBlindImmuneSet.Contains(battleChara.NameId);
	}

	private static readonly HashSet<uint> IsOCParalysisImmuneSet =
	[
		13931, //Chaochu
        13874, //Snapweed
        13932, //Leshy
        13933, //Marolith
        13917, //Sculpture
        13916, //Brachiosaur
        13871, //Foper
        13909, //Dahak

        13930, //Flame
        13919, //Zaghnal
        13928, //Crecent Golem
        13904, //Bachelor
        13898, //Blackguard
        13745, //Headstone
        13881, //Headstone
        13879, //Monk
        13911, //Panther

        13666, //Cloister Demon
        13668, //Cloister Torch
        13729, //Megaloknight
    ];

	/// <summary>
	/// Check to see if target is immune to Paralysis.
	/// </summary>
	public static bool IsOCParalysisImmuneTarget(this IBattleChara battleChara)
	{
		return IsOCParalysisImmuneSet.Contains(battleChara.NameId);
	}

	internal static unsafe uint GetNamePlateIcon(this IBattleChara battleChara)
	{
		return battleChara.Struct()->NamePlateIconId;
	}

	internal static unsafe EventHandlerContent GetEventType(this IBattleChara battleChara)
	{
		return battleChara.Struct()->EventId.ContentId;
	}

	internal static unsafe BattleNpcSubKind GetBattleNPCSubKind(this IBattleChara battleChara)
	{
		return (BattleNpcSubKind)battleChara.Struct()->SubKind;
	}

	internal static unsafe uint FateId(this IBattleChara battleChara)
	{
		return battleChara.Struct()->FateId;
	}
}
