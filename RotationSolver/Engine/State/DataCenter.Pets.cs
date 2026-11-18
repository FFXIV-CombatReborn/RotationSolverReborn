using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using RotationSolver.Rotations.Limited.Beastmaster;
using Buddy = FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy;
using CharacterManager = FFXIVClientStructs.FFXIV.Client.Game.Character.CharacterManager;

namespace RotationSolver.Engine.State;

internal static partial class DataCenter
{
	public unsafe static Buddy.BuddyMember? ActivePet
	{
		get
		{
			var uiState = UIState.Instance();
			if (uiState == null || uiState->Buddy.PetInfo.Pet == null)
			{
				return null;
			}

			return *uiState->Buddy.PetInfo.Pet;
		}
	}

	private static Dictionary<int, XBMPet>? _xbmPetByDataId;

	private static Dictionary<int, XBMPet> XbmPetByDataId
	{
		get
		{
			if (_xbmPetByDataId != null)
			{
				return _xbmPetByDataId;
			}

			Dictionary<int, XBMPet> map = [];
			foreach (var x in Svc.Data.GetExcelSheet<XBMPet>())
			{
				_ = map.TryAdd(x.Unknown4, x);
			}
			return _xbmPetByDataId = map;
		}
	}

	public static bool BMPet
	{
		get
		{
			var dataId = ActivePet?.DataId;
			return dataId > 0 && XbmPetByDataId.ContainsKey(dataId.Value);
		}
	}

	public static BeastmasterKinType BMPetKinType
	{
		get
		{
			var dataId = ActivePet?.DataId;
			if (dataId is null or <= 0 || !XbmPetByDataId.TryGetValue(dataId.Value, out var row))
			{
				return BeastmasterKinType.None;
			}

			return row.Unknown7 switch
			{
				1 => BeastmasterKinType.Beastkin,
				2 => BeastmasterKinType.Vilekin,
				3 => BeastmasterKinType.Cloudkin,
				4 => BeastmasterKinType.Seedkin,
				5 => BeastmasterKinType.Wavekin,
				6 => BeastmasterKinType.Scalekin,
				7 => BeastmasterKinType.Soulkin,
				8 => BeastmasterKinType.Ashkin,
				_ => BeastmasterKinType.None
			};
		}
	}

	private static readonly BeastmasterAffinity[] PetAffinities =
	[
		BeastmasterAffinity.None,     // 0  (no pet)
		BeastmasterAffinity.Rampant,  // 1  Cu Sith
		BeastmasterAffinity.Rampant,  // 2  Squirrel
		BeastmasterAffinity.Rampant,  // 3  Lamb
		BeastmasterAffinity.Durant,   // 4  Pugil
		BeastmasterAffinity.Rampant,  // 5  Opo-opo
		BeastmasterAffinity.Eldritch, // 6  Dodo
		BeastmasterAffinity.Eldritch, // 7  Coblyn
		BeastmasterAffinity.Rampant,  // 8  Diremite
		BeastmasterAffinity.Durant,   // 9  Megalocrab
		BeastmasterAffinity.Volant,   // 10 Wespe
		BeastmasterAffinity.Volant,   // 11 Vulture
		BeastmasterAffinity.Rampant,  // 12 Mandragora
		BeastmasterAffinity.Eldritch, // 13 Geshunpest
		BeastmasterAffinity.Rampant,  // 14 Puk
		BeastmasterAffinity.Durant,   // 15 Crab
		BeastmasterAffinity.Durant,   // 16 Mantis
		BeastmasterAffinity.Eldritch, // 17 Slime
		BeastmasterAffinity.Durant,   // 18 Dullahan
		BeastmasterAffinity.Volant,   // 19 Bat
		BeastmasterAffinity.Volant,   // 20 Flying Trap
		BeastmasterAffinity.Durant,   // 21 Ziz
		BeastmasterAffinity.Rampant,  // 22 Sabotender
		BeastmasterAffinity.Eldritch, // 23 Golem
		BeastmasterAffinity.Durant,   // 24 Apkallu
		BeastmasterAffinity.Eldritch, // 25 Adamantoise
		BeastmasterAffinity.Rampant,  // 26 Buffalo
		BeastmasterAffinity.Durant,   // 27 Uragnite
		BeastmasterAffinity.Eldritch, // 28 Worm
		BeastmasterAffinity.Rampant,  // 29 Spriggan
		BeastmasterAffinity.Rampant,  // 30 Goobbue
		BeastmasterAffinity.Eldritch, // 31 Gigantoad
		BeastmasterAffinity.Volant,   // 32 Colibri
		BeastmasterAffinity.Eldritch, // 33 Coeurl
		BeastmasterAffinity.Durant,   // 34 Raptor
		BeastmasterAffinity.Rampant,  // 35 Drake
		BeastmasterAffinity.Eldritch, // 36 Treant
		BeastmasterAffinity.Rampant,  // 37 Antling
		BeastmasterAffinity.Rampant,  // 38 Chimera
		BeastmasterAffinity.Rampant,  // 39 Morbol
		BeastmasterAffinity.Volant,   // 40 Ghost
		BeastmasterAffinity.Durant,   // 41 Salamander
		BeastmasterAffinity.Durant,   // 42 Cobra
		BeastmasterAffinity.Durant,   // 43 Hydra
		BeastmasterAffinity.Volant,   // 44 Damselfly
		BeastmasterAffinity.Eldritch, // 45 Rotting Goobbue
		BeastmasterAffinity.Volant,   // 46 Zu
		BeastmasterAffinity.Durant,   // 47 Ice Golem
		BeastmasterAffinity.Durant,   // 48 Karlabos
		BeastmasterAffinity.Eldritch, // 49 Rafflesia
		BeastmasterAffinity.Eldritch, // 50 Behemoth
	];

	public static BeastmasterAffinity AffinityOf(BeastmasterPet pet)
	{
		var index = (int)pet;
		return index > 0 && index < PetAffinities.Length ? PetAffinities[index] : BeastmasterAffinity.None;
	}

	public static BeastmasterAffinity BMPetAffinity
	{
		get
		{
			var dataId = ActivePet?.DataId;
			return dataId is null or <= 0 || !XbmPetByDataId.TryGetValue(dataId.Value, out var row)
				? BeastmasterAffinity.None
				: AffinityOf((BeastmasterPet)row.RowId);
		}
	}

	#region Pet Tracking
	public static bool HasPet()
	{
		return Svc.Buddies.PetBuddy != null;
	}

	public static unsafe bool HasCompanion
	{
		get
		{
			var playerBattleChara = Player.BattleChara;
			if (playerBattleChara == null)
			{
				return false;
			}

			var characterManager = CharacterManager.Instance();
			if (characterManager == null)
			{
				return false;
			}

			var companion = characterManager->LookupBuddyByOwnerObject(playerBattleChara);
			return (IntPtr)companion != IntPtr.Zero;
		}
	}

	public static unsafe BattleChara* GetCompanion()
	{
		var playerBattleChara = Player.BattleChara;
		if (playerBattleChara == null)
		{
			return null;
		}

		var characterManager = CharacterManager.Instance();
		return characterManager == null ? (BattleChara*)null : characterManager->LookupBuddyByOwnerObject(playerBattleChara);
	}

	/// <summary>
	/// Gets the players pet
	/// </summary>
	/// <returns>IBattleChara? pet</returns>
	public static IBattleChara? GetPet()
	{
		return Svc.Buddies.PetBuddy?.GameObject as IBattleChara;
	}
	#endregion
}
