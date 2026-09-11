using Lumina.Excel.Sheets;
using System.Collections.Frozen;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// 
/// </summary>
public class BestiaryHelper
{
	/// <summary>
	/// XBMElement
	/// </summary>
	public enum Element : byte
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,

		/// <summary>
		/// 
		/// </summary>
		Fire = 1,

		/// <summary>
		/// 
		/// </summary>
		Wind = 2,

		/// <summary>
		/// 
		/// </summary>
		Earth = 3,

		/// <summary>
		/// 
		/// </summary>
		Lightning = 4,

		/// <summary>
		/// 
		/// </summary>
		Ice = 5,

		/// <summary>
		/// 
		/// </summary>
		Water = 6,

		/// <summary>
		/// 
		/// </summary>
		Blunt = 7,

		/// <summary>
		/// 
		/// </summary>
		Piercing = 8,

		/// <summary>
		/// 
		/// </summary>
		Slashing = 9,
	}

	/// <summary>
	/// XBMItemType
	/// </summary>
	public enum ItemType : byte
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,

		/// <summary>
		/// 
		/// </summary>
		BeastGear = 1,

		/// <summary>
		/// 
		/// </summary>
		CrucibleItem = 2,

		/// <summary>
		/// 
		/// </summary>
		Feed = 3,
	}

	/// <summary>
	/// 
	/// </summary>
	public enum PetClassification : byte
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,

		/// <summary>
		/// 
		/// </summary>
		Beastkin = 1,

		/// <summary>
		/// 
		/// </summary>
		Vilekin = 2,

		/// <summary>
		/// 
		/// </summary>
		Cloudkin = 3,

		/// <summary>
		/// 
		/// </summary>
		Seedkin = 4,

		/// <summary>
		/// 
		/// </summary>
		Wavekin = 5,

		/// <summary>
		/// 
		/// </summary>
		Scalekin = 6,

		/// <summary>
		/// 
		/// </summary>
		Soulkin = 7,

		/// <summary>
		/// 
		/// </summary>
		Ashkin = 8,
	}

	/// <summary>
	/// XBMActionEffectType
	/// </summary>
	public enum ActionEffectType : byte
	{
		/// <summary>
		/// 
		/// </summary>
		None = 0,

		/// <summary>
		/// 
		/// </summary>
		SingleTarget = 1,

		/// <summary>
		/// 
		/// </summary>
		Front = 2,

		/// <summary>
		/// 
		/// </summary>
		Rear = 3,

		/// <summary>
		/// 
		/// </summary>
		FrontRear = 4,

		/// <summary>
		/// 
		/// </summary>
		Lateral = 5,

		/// <summary>
		/// 
		/// </summary>
		Circle = 6,

		/// <summary>
		/// 
		/// </summary>
		Ring = 7,

		/// <summary>
		/// 
		/// </summary>
		CircleRing = 8,

		/// <summary>
		/// 
		/// </summary>
		Universal = 9,

		/// <summary>
		/// 
		/// </summary>
		Cross = 10,
	}

	// Unknown7 - Classification, corrosponds to PetClassification enum
	//Inflict status are bools
	// Unknown 11 - Inflict Status Slow
	// Unknown 12 - Inflict Status Petrification
	// Unknown 13 - Inflict Status Paralysis
	// Unknown 14 - Inflict Status Silence
	// Unknown 15 - Inflict Status Blind
	// Unknown 16 - Inflict Status Stun
	// Unknown 17 - Inflict Status Sleep
	// Unknown 18 - Inflict Status Heavy
	// Unknown 19 - Inflict Status Doom
	// Unknown 20 - Inflict Status Poison
	private sealed record PetData(byte Classification,
	bool InflictsSlow, bool InflictsPetrification, bool InflictsParalysis,
	bool InflictsSilence, bool InflictsBlind, bool InflictsStun,
	bool InflictsSleep, bool InflictsHeavy, bool InflictsDoom, bool InflictsPoison);

	private static FrozenDictionary<int, PetData> BuildPetDataDictionary()
	{
		var sheet = Service.GetSheet<XBMPet>();
		var dict = new Dictionary<int, PetData>();

		foreach (var row in sheet)
		{
			var Name = row.Unknown4;
			if (row.RowId == 0)
			{
				continue;
			}

			dict[Name] = new PetData(row.Unknown7,
			row.Unknown11, row.Unknown12, row.Unknown13,
			row.Unknown14, row.Unknown15, row.Unknown16,
			row.Unknown17, row.Unknown18, row.Unknown19, row.Unknown20);
		}

		return dict.ToFrozenDictionary();
	}

	//private sealed record CrucibleEnemyData();

	//private static FrozenDictionary<uint, CrucibleEnemyData> BuildCrucibleEnemyDataDictionary()
	//{
	//	var sheet = Service.GetSheet<XBMBattleDetail>();
	//	var dict = new Dictionary<uint, CrucibleEnemyData>();

	//	foreach (var row in sheet)
	//	{
	//		var rowId = row.RowId;
	//		if (rowId == 0)
	//		{
	//			continue;
	//		}

	//		dict[rowId] = new CrucibleEnemyData();
	//	}

	//	return dict.ToFrozenDictionary();
	//}

	//private sealed record CrucibleEnemyActionData();

	//private static FrozenDictionary<uint, CrucibleEnemyActionData> BuildCrucibleEnemyActionDataDictionary()
	//{
	//	var sheet = Service.GetSheet<XBMBattleDetailAction>();
	//	var dict = new Dictionary<uint, CrucibleEnemyActionData>();

	//	foreach (var row in sheet)
	//	{
	//		var nameId = row.RowId;
	//		if (nameId == 0)
	//		{
	//			continue;
	//		}

	//		dict[nameId] = new CrucibleEnemyActionData();
	//	}

	//	return dict.ToFrozenDictionary();
	//}
}