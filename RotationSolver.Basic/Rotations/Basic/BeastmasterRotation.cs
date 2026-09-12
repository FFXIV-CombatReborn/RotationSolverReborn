using FFXIVClientStructs.FFXIV.Client.Game;
using System.Runtime.InteropServices;
using Buddy = FFXIVClientStructs.FFXIV.Client.Game.UI.Buddy;

namespace RotationSolver.Basic.Rotations.Basic;

/// <summary>
///
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public struct TempGauge
{
	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x08)] public byte BSTTP;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x09)] public byte FamiliarTPGauge;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0A)] public byte FamiliarTPAtLastUse;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0B)] public byte ActiveBattlehorn;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0C)] public BeastmasterAffinity InstinctualComboState;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0D)] public BeastmasterAffinity CurrentAffinity;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0E)] public byte ChainCount;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x0F)] public byte KinshipState;

	/// <summary>
	///
	/// </summary>
	[FieldOffset(0x10)] public byte Instinct;
}

/// <summary>
///
/// </summary>
public enum BeastmasterPet : byte
{
	/// <summary>
	///
	/// </summary>
	None = 0,

	/// <summary>
	///
	/// </summary>
	CuSith = 1,

	/// <summary>
	///
	/// </summary>
	Squirrel = 2,

	/// <summary>
	///
	/// </summary>
	Lamb = 3,

	/// <summary>
	///
	/// </summary>
	Pugil = 4,

	/// <summary>
	///
	/// </summary>
	Opoopo = 5,

	/// <summary>
	///
	/// </summary>
	Dodo = 6,

	/// <summary>
	///
	/// </summary>
	Coblyn = 7,

	/// <summary>
	///
	/// </summary>
	Diremite = 8,

	/// <summary>
	///
	/// </summary>
	Megalocrab = 9,

	/// <summary>
	///
	/// </summary>
	Wespe = 10,

	/// <summary>
	///
	/// </summary>
	Vulture = 11,

	/// <summary>
	///
	/// </summary>
	Mandragora = 12,

	/// <summary>
	///
	/// </summary>
	Geshunpest = 13,

	/// <summary>
	///
	/// </summary>
	Puk = 14,

	/// <summary>
	///
	/// </summary>
	Crab = 15,

	/// <summary>
	///
	/// </summary>
	Mantis = 16,

	/// <summary>
	///
	/// </summary>
	Slime = 17,

	/// <summary>
	///
	/// </summary>
	Dullahan = 18,

	/// <summary>
	///
	/// </summary>
	Bat = 19,

	/// <summary>
	///
	/// </summary>
	FlyingTrap = 20,

	/// <summary>
	///
	/// </summary>
	Ziz = 21,

	/// <summary>
	///
	/// </summary>
	Sabotender = 22,

	/// <summary>
	///
	/// </summary>
	Golem = 23,

	/// <summary>
	///
	/// </summary>
	Apkallu = 24,

	/// <summary>
	///
	/// </summary>
	Adamantoise = 25,

	/// <summary>
	///
	/// </summary>
	Buffalo = 26,

	/// <summary>
	///
	/// </summary>
	Uragnite = 27,

	/// <summary>
	///
	/// </summary>
	Worm = 28,

	/// <summary>
	///
	/// </summary>
	Spriggan = 29,

	/// <summary>
	///
	/// </summary>
	Goobbue = 30,

	/// <summary>
	///
	/// </summary>
	Gigantoad = 31,

	/// <summary>
	///
	/// </summary>
	Colibri = 32,

	/// <summary>
	///
	/// </summary>
	Coeurl = 33,

	/// <summary>
	///
	/// </summary>
	Raptor = 34,

	/// <summary>
	///
	/// </summary>
	Drake = 35,

	/// <summary>
	///
	/// </summary>
	Treant = 36,

	/// <summary>
	///
	/// </summary>
	Antling = 37,

	/// <summary>
	///
	/// </summary>
	Chimera = 38,

	/// <summary>
	///
	/// </summary>
	Morbol = 39,

	/// <summary>
	///
	/// </summary>
	Ghost = 40,

	/// <summary>
	///
	/// </summary>
	Salamander = 41,

	/// <summary>
	///
	/// </summary>
	Cobra = 42,

	/// <summary>
	///
	/// </summary>
	Hydra = 43,

	/// <summary>
	///
	/// </summary>
	Damselfly = 44,

	/// <summary>
	///
	/// </summary>
	RottingGoobbue = 45,

	/// <summary>
	///
	/// </summary>
	Zu = 46,

	/// <summary>
	///
	/// </summary>
	IceGolem = 47,

	/// <summary>
	///
	/// </summary>
	Karlabos = 48,

	/// <summary>
	///
	/// </summary>
	Rafflesia = 49,

	/// <summary>
	///
	/// </summary>
	Behemoth = 50,
}

/// <summary>
///
/// </summary>
public enum BeastmasterAffinity : byte
{
	/// <summary>
	///
	/// </summary>
	None = 0,

	/// <summary>
	///
	/// </summary>
	Volant = 1,

	/// <summary>
	///
	/// </summary>
	Rampant = 2,

	/// <summary>
	///
	/// </summary>
	Durant = 3,

	/// <summary>
	///
	/// </summary>
	Eldritch = 4,

	/// <summary>
	///
	/// </summary>
	Sunstrider = 5,

	/// <summary>
	///
	/// </summary>
	Moonstalker = 6,
}

/// <summary>
///
/// </summary>
public enum BeastmasterKinType : byte
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
///
/// </summary>
public enum KinshipBorrowed : byte
{
	/// <summary>
	///
	/// </summary>
	None = 0,

	/// <summary>
	///
	/// </summary>
	BeastkinFirstHorn = 17,

	/// <summary>
	///
	/// </summary>
	BeastkinSecondHorn = 18,

	/// <summary>
	///
	/// </summary>
	BeastkinThirdHorn = 19,

	/// <summary>
	///
	/// </summary>
	VilkskinFirstHorn = 33,

	/// <summary>
	///
	/// </summary>
	VilkskinSecondHorn = 34,

	/// <summary>
	///
	/// </summary>
	VilkskinThirdHorn = 35,

	/// <summary>
	///
	/// </summary>
	CloudSkinFirstHorn = 49,

	/// <summary>
	///
	/// </summary>
	CloudSkinSecondHorn = 50,

	/// <summary>
	///
	/// </summary>
	CloudSkinThirdHorn = 51,

	/// <summary>
	///
	/// </summary>
	SeedsowerFirstHorn = 65,

	/// <summary>
	///
	/// </summary>
	SeedsowerSecondHorn = 66,

	/// <summary>
	///
	/// </summary>
	SeedsowerThirdHorn = 67,

	/// <summary>
	///
	/// </summary>
	QuellingWaveFirstHorn = 81,

	/// <summary>
	///
	/// </summary>
	QuellingWaveSecondHorn = 82,

	/// <summary>
	///
	/// </summary>
	QuellingWaveThirdHorn = 83,

	/// <summary>
	///
	/// </summary>
	ScaleskinFirstHorn = 97,

	/// <summary>
	///
	/// </summary>
	ScaleskinSecondHorn = 98,

	/// <summary>
	///
	/// </summary>
	ScaleskinThirdHorn = 99,

	/// <summary>
	///
	/// </summary>
	SoulCrushFirstHorn = 113,

	/// <summary>
	///
	/// </summary>
	SoulCrushSecondHorn = 114,

	/// <summary>
	///
	/// </summary>
	SoulCrushThirdHorn = 115,

	/// <summary>
	///
	/// </summary>
	ScouringAshFirstHorn = 129,

	/// <summary>
	///
	/// </summary>
	ScouringAshSecondHorn = 130,

	/// <summary>
	///
	/// </summary>
	ScouringAshThirdHorn = 131,
}

/// <summary>
/// 
/// </summary>
public enum HornOrder : byte
{
	/// <summary>
	/// 
	/// </summary>
	None = 0,

	/// <summary>
	/// 
	/// </summary>
	FirstBattlehorn = 1,

	/// <summary>
	/// 
	/// </summary>
	SecondBattlehorn = 2,

	/// <summary>
	/// 
	/// </summary>
	ThirdBattlehorn = 3,
}

/// <summary>
/// 
/// </summary>
public enum OneWithNatureOrder : byte
{
	/// <summary>
	/// 
	/// </summary>
	Neither = 0,

	/// <summary>
	/// 
	/// </summary>
	Tempered = 1,

	/// <summary>
	/// 
	/// </summary>
	Borrow = 2,
}

public partial class BeastmasterRotation
{
	/// <summary>
	/// 
	/// </summary>
	public override MedicineType MedicineType => MedicineType.Strength;

	#region Job Gauge

	/// <summary>
	///
	/// </summary>
	public unsafe int MasteredInstinct => (TjobGauge->Instinct & 0xC) >> 2;

	/// <summary>
	///
	/// </summary>
	public static byte MaxMasteredInstinct()
	{
		if (!WildHeartIiiTrait.EnoughLevel)
		{
			return 0;
		}

		return 3;
	}

	/// <summary>
	///
	/// </summary>
	public unsafe int NaturalInstinct => TjobGauge->Instinct & 0x3;

	/// <summary>
	///
	/// </summary>
	public static byte MaxNaturalInstinct()
	{
		if (!WildHeartIvTrait.EnoughLevel)
		{
			return 0;
		}

		return 3;
	}

	/// <summary>
	///
	/// </summary>
	public static unsafe byte FamiliarTPAtLastUse => TjobGauge->FamiliarTPAtLastUse;

	/// <summary>
	///
	/// </summary>
	public static unsafe byte ActiveBattlehorn => TjobGauge->ActiveBattlehorn;

	/// <summary>
	/// 
	/// </summary>
	public static unsafe byte KinshipState => TjobGauge->KinshipState;

	/// <summary>
	/// 
	/// </summary>
	public static unsafe Span<byte> CurrentPets => ActionManager.Instance()->BeastmasterPets;

	/// <summary>
	/// 
	/// </summary>
	public static unsafe KinshipBorrowed KinshipStateText => (KinshipBorrowed)TjobGauge->KinshipState;

	/// <summary>
	///
	/// </summary>
	public static BeastmasterKinType KinshipKinType => (BeastmasterKinType)(KinshipState >> 4);

	/// <summary>
	///
	/// </summary>
	public static byte KinshipBattlehorn => (byte)(KinshipState & 0x0F);

	/// <summary>
	///
	/// </summary>
	public static unsafe byte TPCount => TjobGauge->BSTTP;

	/// <summary>
	///
	/// </summary>
	public static unsafe byte PetTPCount => TjobGauge->FamiliarTPGauge;

	/// <summary>
	///
	/// </summary>
	public static unsafe BeastmasterAffinity InstinctualComboState => TjobGauge->InstinctualComboState;

	/// <summary>
	///
	/// </summary>
	public static unsafe BeastmasterAffinity CurrentAffinity => TjobGauge->CurrentAffinity;

	/// <summary>
	///
	/// </summary>
	public static unsafe byte ChainCount => TjobGauge->ChainCount;

	/// <summary>
	///
	/// </summary>
	public unsafe static TempGauge* TjobGauge => (TempGauge*)((nint)FFXIVClientStructs.FFXIV.Client.Game.JobGaugeManager.StaticAddressPointers.pInstance + 0x08);

	/// <summary>
	///
	/// </summary>
	public unsafe static TempGauge JobGauge => *TjobGauge;

	/// <summary>
	///
	/// </summary>
	public static Buddy.BuddyMember? ActivePet => DataCenter.ActivePet;

	/// <summary>
	///
	/// </summary>
	public static bool BMPet => DataCenter.BMPet;

	/// <summary>
	///
	/// </summary>
	public static bool AnyBattlehornReady => FirstBattleHornReady || SecondBattleHornReady || ThirdBattleHornReady;

	/// <summary>
	///
	/// </summary>
	public static unsafe bool FirstBattleHornReady => ActionManager.Instance()->GetActionStatus(ActionType.Action, (uint)ActionID.FirstBattlehornPvE) == 0;

	/// <summary>
	///
	/// </summary>
	public static unsafe bool SecondBattleHornReady => ActionManager.Instance()->GetActionStatus(ActionType.Action, (uint)ActionID.SecondBattlehornPvE) == 0;

	/// <summary>
	///
	/// </summary>
	public static unsafe bool ThirdBattleHornReady => ActionManager.Instance()->GetActionStatus(ActionType.Action, (uint)ActionID.ThirdBattlehornPvE) == 0;

	/// <summary>
	///
	/// </summary>
	public static unsafe bool TemperedReleaseReady => ActionManager.Instance()->GetActionStatus(ActionType.Action, (uint)ActionID.TemperedReleasePvE) == 0;

	/// <summary>
	///
	/// </summary>
	public static unsafe bool BorrowReady => ActionManager.Instance()->GetActionStatus(ActionType.Action, (uint)ActionID.BorrowPvE) == 0;

	/// <summary>
	///
	/// </summary>
	public static BeastmasterKinType BMPetKinType => DataCenter.BMPetKinType;

	/// <summary>
	///
	/// </summary>
	public static BeastmasterAffinity BMPetAffinity => DataCenter.BMPetAffinity;

	/// <summary>
	///
	/// </summary>
	public static BeastmasterAffinity CurrentPetAffinity()
	{
		if (CurrentPet == BeastmasterPet.None)
		{
			return BeastmasterAffinity.None;
		}

		if (CurrentPet == BeastmasterPet.CuSith)
		{
			return BeastmasterAffinity.Volant;
		}

		if (CurrentPet == BeastmasterPet.None)
		{
			return BeastmasterAffinity.Rampant;
		}

		if (CurrentPet == BeastmasterPet.None)
		{
			return BeastmasterAffinity.Durant;
		}

		if (CurrentPet == BeastmasterPet.None)
		{
			return BeastmasterAffinity.Eldritch;
		}

		return BeastmasterAffinity.None;
	}

	/// <summary>
	///
	/// </summary>
	public static bool IsPetActive(BeastmasterPet petId)
	{
		if (petId == 0)
		{
			return false;
		}

		var pet = (byte)petId;
		var pets = CurrentPets;

		for (var i = 0; i < pets.Length; i++)
		{
			if (pets[i] == pet)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///
	/// </summary>
	public static string GetPetName(byte rowId)
	{
		if (rowId == 0)
		{
			return string.Empty;
		}

		var pet = (BeastmasterPet)rowId;

		return pet.ToString();
	}

	/// <summary>
	/// The pet currently active, derived from which Battlehorn slot (<see cref="ActiveBattlehorn"/>) is selected and the pet assigned to that slot in <see cref="CurrentPets"/>.
	/// </summary>
	public static BeastmasterPet CurrentPet
	{
		get
		{
			var slot = ActiveBattlehorn;
			var pets = CurrentPets;

			if (slot < 1 || slot > pets.Length)
			{
				return BeastmasterPet.None;
			}

			return (BeastmasterPet)pets[slot - 1];
		}
	}

	#endregion

	#region Status Tracking
	/// <summary>
	///
	/// </summary>
	public static bool HasVolantHeart => StatusHelper.PlayerHasStatus(false, StatusID.VolantHeart);

	/// <summary>
	///
	/// </summary>
	public static bool HasRampantHeart => StatusHelper.PlayerHasStatus(false, StatusID.RampantHeart);

	/// <summary>
	///
	/// </summary>
	public static bool HasDurantHeart => StatusHelper.PlayerHasStatus(false, StatusID.DurantHeart);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasEldritchHeart => StatusHelper.PlayerHasStatus(false, StatusID.EldritchHeart);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasWaveringHeart => StatusHelper.PlayerHasStatus(false, StatusID.WaveringHeart);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSunstrider => StatusHelper.PlayerHasStatus(false, StatusID.Sunstrider);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasMoonstalker => StatusHelper.PlayerHasStatus(false, StatusID.Moonstalker);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasOneWithNature => StatusHelper.PlayerHasStatus(true, StatusID.OneWithNature);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasBeastKinship => StatusHelper.PlayerHasStatus(true, StatusID.BeastKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasBeastKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.BeastKinship_4644);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasVileKinship => StatusHelper.PlayerHasStatus(true, StatusID.VileKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasVileKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.VileKinship_4645);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasCloudKinship => StatusHelper.PlayerHasStatus(true, StatusID.CloudKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasCloudKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.CloudKinship_4646);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSeedKinship => StatusHelper.PlayerHasStatus(true, StatusID.SeedKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSeedKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.SeedKinship_4647);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasWaveKinship => StatusHelper.PlayerHasStatus(true, StatusID.WaveKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasWaveKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.WaveKinship_4648);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasScaleKinship => StatusHelper.PlayerHasStatus(true, StatusID.ScaleKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasScaleKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.ScaleKinship_4649);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSoulKinship => StatusHelper.PlayerHasStatus(true, StatusID.SoulKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSoulKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.SoulKinship_4650);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasAshKinship => StatusHelper.PlayerHasStatus(true, StatusID.AshKinship);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasAshKinship2 => StatusHelper.PlayerHasStatus(true, StatusID.AshKinship_4651);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasLingeringVantage => StatusHelper.PlayerHasStatus(true, StatusID.LingeringVantage);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasVileskin => StatusHelper.PlayerHasStatus(true, StatusID.Vileskin);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasBeastskin => StatusHelper.PlayerHasStatus(true, StatusID.Beastskin);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasScaleskin => StatusHelper.PlayerHasStatus(true, StatusID.Scaleskin);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasSeedsSown => StatusHelper.PlayerHasStatus(true, StatusID.SeedsSown);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasAstralAscendancy => StatusHelper.PlayerHasStatus(true, StatusID.AstralAscendancy);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasUmbralAscendancy => StatusHelper.PlayerHasStatus(true, StatusID.UmbralAscendancy);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasCapturingInterest => StatusHelper.PlayerHasStatus(true, StatusID.CapturingInterest);

	/// <summary>
	/// 
	/// </summary>
	public static bool HasInterestCaptured => StatusHelper.PlayerHasStatus(true, StatusID.InterestCaptured);
	#endregion

	#region Draw Debug

	/// <inheritdoc/>
	public override void DisplayBaseStatus()
	{
		ImGui.Text("First Battlehorn: " + GetPetName(CurrentPets[0]));
		ImGui.Text("Second Battlehorn: " + GetPetName(CurrentPets[1]));
		ImGui.Text("Third Battlehorn: " + GetPetName(CurrentPets[2]));
		ImGui.Text("ActiveBattlehorn: " + ActiveBattlehorn.ToString());
		ImGui.Spacing();
		ImGui.Text("Current Pet: " + CurrentPet.ToString());
		//ImGui.Text("Current Pet Affinity: " + BMPetAffinity.ToString());
		//ImGui.Text("Current Pet Kintype: " + BMPetKinType.ToString());
		ImGui.Spacing();
		ImGui.Text("TPCount: " + TPCount.ToString());
		ImGui.Text("PetTPCount: " + PetTPCount.ToString());
		ImGui.Spacing();
		ImGui.Text("MasteredInstinct: " + MasteredInstinct.ToString());
		ImGui.Text("NaturalInstinct: " + NaturalInstinct.ToString());
		ImGui.Spacing();
		ImGui.Text("FamiliarTPAtLastUse: " + FamiliarTPAtLastUse.ToString());
		ImGui.Text("InstinctualComboState: " + InstinctualComboState.ToString());
		ImGui.Text("CurrentAffinity: " + CurrentAffinity.ToString());
		ImGui.Text("ChainCount: " + ChainCount.ToString());
		ImGui.Spacing();
		ImGui.Text("KinshipState: " + KinshipStateText.ToString());
		ImGui.Text("KinshipKinType : " + KinshipKinType.ToString());
		ImGui.Text("KinshipBattlehorn: " + KinshipBattlehorn.ToString());
	}
	#endregion

	#region Actions

	static partial void ModifySnarlPvE(ref ActionSetting setting)
	{
		//shirk
		setting.IsFriendly = false;
		//setting.TargetType = TargetType.Provoke;
		setting.TargetStatusNeed = [StatusID.UnnamedStatus_2552];
		setting.StatusFromSelf = false;
	}

	static partial void ModifyChallengePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.TargetType = TargetType.Provoke;
	}

	static partial void ModifySmashAxePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
	}

	static partial void ModifyAxebladeBitePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
	}

	static partial void ModifyShieldsplitterPvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
	}

	static partial void ModifyBrutalRagePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount == 250;
		setting.IsFriendly = false;
		setting.StatusNeed = [StatusID.Moonstalker];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.Sunstrider];
		setting.TargetType = TargetType.HighHP;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyHawkishTalonsPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount == 250;
		setting.IsFriendly = false;
		setting.StatusNeed = [StatusID.Sunstrider];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.Moonstalker];
		setting.TargetType = TargetType.HighHP;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyRisenFallPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount == 250;
		setting.IsFriendly = false;
		setting.StatusNeed = [StatusID.Moonstalker];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.Sunstrider];
		setting.TargetType = TargetType.HighHP;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyCalamityPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount == 250;
		setting.IsFriendly = false;
		setting.StatusNeed = [StatusID.Sunstrider];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.Moonstalker];
		setting.TargetType = TargetType.HighHP;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyFirstBattlehornPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => ActiveBattlehorn != 1 && CurrentPets[0] != 0;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifySecondBattlehornPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => ActiveBattlehorn != 2 && CurrentPets[1] != 0;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyThirdBattlehornPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => ActiveBattlehorn != 3 && CurrentPets[2] != 0;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyAvalancheAxePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount >= 100 && (TPCount < 250 || !InstinctualMasteryTrait.EnoughLevel);
		setting.StatusNeed = [StatusID.VolantHeart];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.RampantHeart];
		setting.IsFriendly = false;
		setting.TargetType = TargetType.HighHP;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyMistralAxePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount >= 100 && (TPCount < 250 || !InstinctualMasteryTrait.EnoughLevel);
		setting.StatusNeed = [StatusID.RampantHeart];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.DurantHeart];
		setting.IsFriendly = false;
		setting.TargetType = TargetType.HighHP;
		setting.MPOverride = () => 0;
	}

	static partial void ModifySpinningAxePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount >= 100 && (TPCount < 250 || !InstinctualMasteryTrait.EnoughLevel);
		setting.StatusNeed = [StatusID.DurantHeart];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.EldritchHeart];
		setting.IsFriendly = false;
		setting.TargetType = TargetType.HighHP;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyGaleAxePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => TPCount >= 100 && (TPCount < 250 || !InstinctualMasteryTrait.EnoughLevel);
		setting.StatusNeed = [StatusID.EldritchHeart];
		setting.StatusFromSelf = false;
		setting.StatusProvide = [StatusID.VolantHeart];
		setting.IsFriendly = false;
		setting.TargetType = TargetType.HighHP;
		setting.MPOverride = () => 0;
	}

	static partial void ModifyShieldChargePvE(ref ActionSetting setting)
	{
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyRallyPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => InCombat;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyRallyingCheerPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => InCombat;
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Self;
	}

	static partial void ModifyCapturePvE(ref ActionSetting setting)
	{
		setting.StatusProvide = [StatusID.CapturingInterest];
		setting.StatusFromSelf = false;
		setting.TargetStatusProvide = [StatusID.InterestCaptured];
		setting.TargetStatusNeed = [StatusID.InterestCaptured];
		setting.IsFriendly = false;
		//setting.TargetType = TargetType.Capture;
	}

	static partial void ModifyGaugePvE(ref ActionSetting setting)
	{

	}

	static partial void ModifyTemperedReleasePvE(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.OneWithNature];
		setting.MPOverride = () => 0;
		setting.ActionCheck = () => InCombat && Service.GetAdjustedActionId(ActionID.TemperedReleasePvE) == ActionID.TemperedReleasePvE;

	}

	static partial void ModifyTemperedReleasePvE_47092(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.OneWithNature];
		setting.MPOverride = () => 0;
		setting.ActionCheck = () => InCombat && Service.GetAdjustedActionId(ActionID.TemperedReleasePvE) == ActionID.TemperedReleasePvE_47092;

	}

	static partial void ModifyPartingBlowPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => ActiveBattlehorn != 0;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	//Fake Borrow
	static partial void ModifyBorrowPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
		setting.MPOverride = () => 0;
	}
	#region Borrow Variants

	//Beastkin Borrow
	static partial void ModifyBorrowPvE_47238(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Vilekin Borrow
	static partial void ModifyBorrowPvE_47239(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Cloudkin Borrow
	static partial void ModifyBorrowPvE_47240(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Seedkin Borrow
	static partial void ModifyBorrowPvE_47241(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Wavekin Borrow
	static partial void ModifyBorrowPvE_47242(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Scalekin Borrow
	static partial void ModifyBorrowPvE_47243(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Soulkin Borrow
	static partial void ModifyBorrowPvE_47244(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}

	//Ashkin Borrow
	static partial void ModifyBorrowPvE_47245(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn != ActiveBattlehorn;
		setting.StatusNeed = [StatusID.OneWithNature];
	}
	#endregion

	static partial void ModifyBeastModePvE(ref ActionSetting setting)
	{
		setting.IsFakeAction = true;
	}

	static partial void ModifyVileskinPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Vilekin;
		setting.StatusNeed = [StatusID.VileKinship, StatusID.VileKinship_4645];
		setting.StatusProvide = [StatusID.Vileskin];
		setting.IsFriendly = true;
	}

	static partial void ModifyCloudSkimPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Cloudkin;
		setting.StatusNeed = [StatusID.CloudKinship, StatusID.CloudKinship_4646];
		//setting.StatusProvide = [StatusID.LiquidEvasion];
		setting.IsFriendly = true;
	}
	#region CloudSkin Variants (Movement Directions)
	static partial void ModifyCloudSkimPvE_45038(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.CloudKinship, StatusID.CloudKinship_4646];
		//setting.StatusProvide = [StatusID.LiquidEvasion];
		setting.IsFriendly = true;
	}

	static partial void ModifyCloudSkimPvE_45039(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.CloudKinship, StatusID.CloudKinship_4646];
		//setting.StatusProvide = [StatusID.LiquidEvasion];
		setting.IsFriendly = true;
	}

	static partial void ModifyCloudSkimPvE_45040(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.CloudKinship, StatusID.CloudKinship_4646];
		//setting.StatusProvide = [StatusID.LiquidEvasion];
		setting.IsFriendly = true;
	}

	static partial void ModifyCloudSkimPvE_45041(ref ActionSetting setting)
	{
		setting.StatusNeed = [StatusID.CloudKinship, StatusID.CloudKinship_4646];
		//setting.StatusProvide = [StatusID.LiquidEvasion];
		setting.IsFriendly = true;
	}
	#endregion (

	static partial void ModifySeedsowerPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Seedkin;
		setting.StatusNeed = [StatusID.SeedKinship, StatusID.SeedKinship_4647];
		setting.TargetStatusProvide = [StatusID.SeedsSown];
		setting.IsFriendly = false;
	}
	static partial void ModifyQuellingWavePvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Wavekin;
		setting.StatusNeed = [StatusID.WaveKinship, StatusID.WaveKinship_4648];
		setting.TargetStatusNeed = [StatusID.PopotoSkin, StatusID.DamageUp_2550, StatusID.PhysicalDamageUp_2074, StatusID.DamageUp_1225];
		setting.StatusFromSelf = false;
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyScaleskinPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Scalekin;
		setting.StatusNeed = [StatusID.ScaleKinship, StatusID.ScaleKinship_4649];
		setting.StatusProvide = [StatusID.Scaleskin];
		setting.IsFriendly = true;
	}

	static partial void ModifySoulCrushPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Soulkin;
		setting.StatusNeed = [StatusID.SoulKinship, StatusID.SoulKinship_4650];
		setting.IsFriendly = false;
		setting.CreateConfig = () => new ActionConfig()
		{
			AoeCount = 1,
		};
	}

	static partial void ModifyScouringAshPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Ashkin;
		setting.StatusNeed = [StatusID.AshKinship, StatusID.AshKinship_4651];
		setting.IsFriendly = true;
		setting.TargetType = TargetType.Dispel;
	}

	static partial void ModifyBeastskinPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => KinshipBattlehorn > 0 && KinshipKinType == BeastmasterKinType.Beastkin;
		setting.StatusNeed = [StatusID.BeastKinship, StatusID.BeastKinship_4644];
		setting.StatusProvide = [StatusID.Beastskin];
		setting.IsFriendly = true;
	}

	//Trick
	static partial void ModifyTrickPvE(ref ActionSetting setting)
	{
		setting.ActionCheck = () => PetTPCount >= 100 && ActiveBattlehorn != 0;
		setting.StatusFromSelf = false;
		setting.StatusNeed = [StatusID.EldritchHeart, StatusID.VolantHeart, StatusID.DurantHeart, StatusID.RampantHeart];
	}
	#endregion
}
