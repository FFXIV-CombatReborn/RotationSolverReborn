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