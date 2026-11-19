using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;

namespace RotationSolver.Engine.State;

internal static partial class DataCenter
{
	#region Territory Info Tracking

	public static Data.TerritoryInfo? Territory { get; set; }
	public static uint TerritoryID => (ushort)Svc.ClientState.TerritoryType;

	public static bool IsPvP => Territory?.IsPvP ?? false;

	public static bool TargetFreelyOverride { get; set; }

	public static bool IsInMaskedCarnivale => Territory?.ContentType == TerritoryContentType.TheMaskedCarnivale;

	public static bool IsInDuty => Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56];

	public static bool IsInQuestBattle => Territory?.ContentType == TerritoryContentType.QuestBattles;

	private static readonly ushort[] _allianceTerritoryIds =
	[
		151, 174, 372, 508, 556, 627, 734, 776, 826, 882, 917, 966, 1054, 1118, 1178, 1248, 1304, 1368
	];

	public static bool IsInAllianceRaid
	{
		get
		{
			var territoryId = TerritoryID;
			for (var i = 0; i < _allianceTerritoryIds.Length; i++)
			{
				if (_allianceTerritoryIds[i] == territoryId)
				{
					return true;
				}
			}

			return false;
		}
	}

	public static bool IsInTerritory(ushort territoryId)
	{
		return TerritoryID == territoryId;
	}

	#endregion

	#region Ultimate
	public static bool IsInUCoB => TerritoryID == 733;
	public static bool IsInUwU => TerritoryID == 777;
	public static bool IsInTEA => TerritoryID == 887;
	public static bool IsInDSR => TerritoryID == 968;
	public static bool IsInTOP => TerritoryID == 1122;
	public static bool IsInFRU => TerritoryID == 1238;
	public static bool IsInDMU => TerritoryID == 1363;
	#endregion

	#region Chaotic
	public static bool IsInCOD => TerritoryID == 1241;
	#endregion

	#region Savage
	public static bool IsInM9S => TerritoryID == 1321;
	public static bool IsInM10S => TerritoryID == 1323;
	public static bool IsInM11S => TerritoryID == 1327;
	public static bool IsInM12S => TerritoryID == 1325;
	#endregion

	#region Crucible
	public static bool IsInCrucible => IsinFirstBoard || IsinSecondBoard || IsinThirdBoard || IsinFirstMasterBoard || IsinSecondMasterBoard;
	public static bool IsinFirstBoard => TerritoryID == 1339;
	public static bool IsinSecondBoard => TerritoryID == 1340;
	public static bool IsinThirdBoard => TerritoryID == 1341;
	public static bool IsinFirstMasterBoard => TerritoryID == 1342;
	public static bool IsinSecondMasterBoard => TerritoryID == 1343;
	#endregion

	#region Extreme
	public static bool IsTheUnmaking => TerritoryID == 1362;
	#endregion

	#region Alliance Raid
	public static bool IsInWindurst => TerritoryID == 1368;
	#endregion

	#region FATE
	public static unsafe ushort PlayerFateId
	{
		get
		{
			try
			{
				if ((IntPtr)FateManager.Instance() != IntPtr.Zero
					&& (IntPtr)FateManager.Instance()->CurrentFate != IntPtr.Zero
					&& PlayerSyncedLevel() <= FateManager.Instance()->CurrentFate->MaxLevel)
				{
					return FateManager.Instance()->CurrentFate->FateId;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex.StackTrace ?? ex.Message);
			}

			return 0;
		}
	}

	public static bool IsInFate => PlayerFateId != 0 && !IsInBozja && !IsInOccultCrescentOp;

	#endregion

	#region Treasure Hunt
	public static bool IsInTreasureHunt => Territory?.ContentType == TerritoryContentType.TreasureHunt;

	public static bool IsInTheAquapolis => TerritoryID == 558;

	public static bool IsInTheLostCanalsofUznair => TerritoryID == 712;

	public static bool IsInTheShiftingAltarsofUznair => TerritoryID == 794;

	public static bool IsInTheHiddenCanalsofUznair => TerritoryID == 725;

	public static bool IsInTheDungeonsofLyheGhiah => TerritoryID == 879;

	public static bool IsInTheShiftingOubliettesofLyheGhiah => TerritoryID == 924;

	public static bool IsInTheExcitatron6000 => TerritoryID == 1000;

	public static bool IsInTheShiftingGymnasionAgonon => TerritoryID == 1123;

	public static bool IsInCenoteJaJaGural => TerritoryID == 1209;

	public static bool IsInVaultOneiron => TerritoryID == 1279;
	#endregion

	#region Bozja
	public static bool IsInBozjanFieldOp => Content.ContentType == ECommons.GameHelpers.ContentType.FieldOperations
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	public static bool IsInBozjanFieldOpCE => IsInBozjanFieldOp
		&& StatusHelper.PlayerHasStatus(false, StatusID.DutiesAsAssigned);

	public static bool IsInDelubrumNormal => Content.ContentType == ECommons.GameHelpers.ContentType.FieldRaid
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	public static bool IsInDelubrumSavage => Content.ContentType == ECommons.GameHelpers.ContentType.FieldRaid
		&& Content.ContentDifficulty == ContentDifficulty.FieldRaidsSavage
		&& Territory?.ContentType == TerritoryContentType.SaveTheQueen;

	public static bool IsInBozja => IsInBozjanFieldOp || IsInDelubrumNormal || IsInDelubrumSavage;
	#endregion

	public static bool IsInFieldOperations => Content.ContentType == ECommons.GameHelpers.ContentType.FieldOperations;

	public static bool IsInFieldRaid => Content.ContentType == ECommons.GameHelpers.ContentType.FieldRaid;

	#region Occult Crescent
	public static bool IsInOccultCrescentOp => Territory?.ContentType == TerritoryContentType.OccultCrescent;

	public static bool IsInNorthHorn => IsInOccultCrescentOp && TerritoryID == 1346;

	public static bool IsInSouthHorn => IsInOccultCrescentOp && TerritoryID == 1252;

	public static bool IsInForkedTowerBlood => IsInOccultCrescentOp && StatusHelper.PlayerHasStatus(false, StatusID.DutiesAsAssigned_4228);
	#endregion

	#region Variant Dungeon
	public static bool TheMerchantsTaleAdvanced => IsInTerritory(1316);

	public static bool TheMerchantsTale => IsInTerritory(1315);

	public static bool SildihnSubterrane => IsInTerritory(1069);

	public static bool MountRokkon => IsInTerritory(1137);

	public static bool AloaloIsland => IsInTerritory(1176);

	public static bool InVariantDungeon => TheMerchantsTaleAdvanced || TheMerchantsTale || AloaloIsland || MountRokkon || SildihnSubterrane;
	#endregion

	#region Misc Duty Info

	public static bool IsInEurekaFieldOp => Territory?.ContentType == TerritoryContentType.Eureka;

	public static bool IsInDeepDungeons => Territory?.ContentType == TerritoryContentType.DeepDungeons;

	public static bool IsInPilgrimsTraverse => IsInTerritory(1281)
	|| IsInTerritory(1282) || IsInTerritory(1283)
	|| IsInTerritory(1284) || IsInTerritory(1285)
	|| IsInTerritory(1286) || IsInTerritory(1287)
	|| IsInTerritory(1288) || IsInTerritory(1289)
	|| IsInTerritory(1290);

	public static bool IsInPalaceOfTheDead => IsInTerritory(561)
	|| IsInTerritory(562) || IsInTerritory(563)
	|| IsInTerritory(564) || IsInTerritory(565)
	|| IsInTerritory(593) || IsInTerritory(594)
	|| IsInTerritory(595) || IsInTerritory(596)
	|| IsInTerritory(597) || IsInTerritory(598)
	|| IsInTerritory(599) || IsInTerritory(600)
	|| IsInTerritory(601) || IsInTerritory(602)
	|| IsInTerritory(603) || IsInTerritory(604)
	|| IsInTerritory(605) || IsInTerritory(606)
	|| IsInTerritory(607);

	public static bool IsInTheFinalVerse => IsInTerritory(1311) || IsInTerritory(1333);

	public static bool IsInMonsterHunterDuty => RathalosNormal || RathalosEX || ArkveldNormal || ArkveldEX;

	public static bool RathalosNormal => IsInTerritory(761);

	public static bool RathalosEX => IsInTerritory(762);

	public static bool ArkveldNormal => IsInTerritory(1300);

	public static bool ArkveldEX => IsInTerritory(1306);

	public static bool Orbonne => IsInTerritory(826);

	public static bool Emanation => IsInTerritory(719);

	public static bool EmanationEX => IsInTerritory(720);
	#endregion
}
