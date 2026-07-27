namespace RotationSolver.Basic.Rotations.Basic;

public partial class BeastmasterRotation
{
	/// <summary>
	/// 
	/// </summary>
	public override MedicineType MedicineType => MedicineType.Strength;

	//private protected sealed override IBaseAction Raise => ;
	//private protected sealed override IBaseAction TankStance => ;

	#region Job Gauge

	//MasteredInstinct - up to 3 stacks
	//NaturalInstinct - up to 3 stacks

	//TPGauge - up to 200
	//FamiliarTPGauge - up to 250

	//Affinity - Eldritch, Volant, Durant, or Rampant
	#endregion

	#region Status Tracking

	//OneWithNature
	//LingeringVantage
	//Moonstalker
	//DurantHeart
	//Sunstrider

	#endregion

	#region Draw Debug

	/// <inheritdoc/>
	public override void DisplayBaseStatus()
	{
		ImGui.TextWrapped($"Beastmaster Data");
	}
	#endregion

	#region Actions

	//Duty Actions - Crucible exclusive
	//Snarl - Provoke for beast
	//Challange - Provoke for self

	//GCDWeaponskill combo
	//static partial void ModifySmashAxePvE(ref ActionSetting setting)
	//{

	//}
	//SmashAxePvE
	//AxebladeBitePvE
	//ShieldsplitterPvE

	//oGCD Abilty
	//FirstBattlehornPvE
	//SecondBattlehornPvE
	//ThirdBattlehornPvE

	//ShieldChargePvE
	//RallyPvE - Spend all stacks of MasteredInstinct to increase TPGauge by 70 per stack, must be in combat, TP gauge cost 40
	//RallyingCheerPvE - Must be in combat, TP gauge cost 30, spend all Natural Instinct to increase Familiar TP Gauge, each stack by 70
	//GaugePvE - Check if target beast can be captured, can be used while mounted, must be grounded

	//InstinctualSkills - oGCD Abilities. Eldritch, Volant, Durant, Rampant
	//AvalancheAxePvE - Rampant - prefers Volant Heart for Sunstrider combo, TP cost 100
	//MistralAxePvE - Durant - Prefers Rampant Heart for Moonstalker combo, TP cost 100
	//SpinningAxePvE - Eldritch - prefers Durant Heart for Sunstrider combo, TP cost 100,
	//GaleAxePvE - Volant - prefers Eldritch Heart for Moonstalker combo, TP cost 100

	//PartingBlow
	//--Orders beast to execute a powerful area of effect attack and then retreat
	//-Ability, requires OneWithNature and being in combat, cleave attack
	//Increases FamiliarTPGauge by 24

	//Trick
	//--Orders beast to perform its instinctual skill
	//-Rake

	//TemperedRelease
	//--Orders beast to perform a unique action
	//--Requires OneWithNature and being in combat

	//Borrow
	//--Draws power from beast to use another beastmaster action
	//--Requires OneWithNature and turns another button into the below actions depending on the partner type

	//-Beastskin
	//-Vileskin
	//-CloudSkim
	//-Seedsower
	//-QuellingWave - Deals water damage, increases TP Gauge by 10, removes benefinial status, increases TP Gauge by 50
	//-Scaleskin
	//-SoulCrush - Interrupts target, deals damage, increases TP Gauge by 100 if interrupts
	//-ScouringAsh - 

	//Beast1 - Cu Sith
	//Classification: Beastkin
	//Auto-Attack: Piercing

	//TemperedRelease
	//--RelentlessRakePvE

	//Trick
	//--RakePvE

	//Beast2 -
	//Classification:
	//Auto-Attack:

	//Beast3 -
	//Classification:
	//Auto-Attack:

	//Beast4 - Pugil
	//Classification: Wavekin
	//Auto-Attack: Water

	//TemperedRelease
	//--WaterWallPvE
	//-Reduces nearby party members magic vulnerability by 20%, increases paralysis resistance by 80%

	//Trick
	//--ScrewdriverPvE
	//-TP Gauge cost 100, Durant, cone cleave, grants Durant Heart

	//Beast5 -
	//Classification:
	//Auto-Attack:

	//Beast6 - Dodo
	//Classification: Cloudkin
	//Auto-Attack: Earth

	//TemperedRelease
	//--StrutPvE

	//Trick
	//--FowlStenchPvE

	//Beast7 - Coblyn
	//Classification: Soulkin
	//Auto-Attack: Lightning

	//TemperedRelease
	//--VulcanizePvE
	//-Increases poison resistance of all nearby party members

	//Beast8 -
	//Classification:
	//Auto-Attack:

	//Beast9 -
	//Classification:
	//Auto-Attack:

	//Beast10 - Wespe
	//Classification: Vilekin
	//Auto-Attack: Piercing

	//TemperedRelease
	//--FinalStingPvE

	//Trick
	//--SharpStingPvE

	//Beast11 -
	//Classification:
	//Auto-Attack:

	//Beast12 -
	//Classification:
	//Auto-Attack:

	//Beast13 -
	//Classification:
	//Auto-Attack:

	//Beast14 -
	//Classification:
	//Auto-Attack:

	//Beast15 -
	//Classification:
	//Auto-Attack:

	//Beast16 -
	//Classification:
	//Auto-Attack:

	//Beast17 - Slime
	//Classification: Ashkin
	//Auto-Attack: Unaspected

	//TemperedRelease
	//--SyrupPvE
	//-Afflicts targets with Blind+ that cannot be nullified

	//Trick
	//--DigestPvE

	//Beast18 -
	//Classification:
	//Auto-Attack:

	//Beast19 -
	//Classification:
	//Auto-Attack:

	//Beast20 - Flying Trap
	//Classification: Seedkin
	//Auto-Attack: Ice

	//TemperedRelease
	//--NecroticNectarPvE

	//Trick
	//--SourSoughPvE

	//Beast21 - Ziz
	//Classification: Scalekin
	//Auto-Attack: Ice

	//TemperedRelease
	//--PetribreathPvE

	//Trick
	//--IceBreathPvE

	//Beast22 -
	//Classification:
	//Auto-Attack:

	//Beast23 -
	//Classification:
	//Auto-Attack:

	//Beast24 -
	//Classification:
	//Auto-Attack:

	//Beast25 -
	//Classification:
	//Auto-Attack:

	//Beast26 -
	//Classification:
	//Auto-Attack:

	//Beast27 -
	//Classification:
	//Auto-Attack:

	//Beast28 -
	//Classification:
	//Auto-Attack:

	//Beast29 -
	//Classification:
	//Auto-Attack:

	//Beast30 - Goobue
	//Classification: Beastkin
	//Auto-Attack: Blunt

	//TemperedRelease
	//--MoldySneezePvE

	//Trick
	//--BeatdownPvE

	//Beast31 -
	//Classification:
	//Auto-Attack:

	//Beast32 -
	//Classification:
	//Auto-Attack:

	//Beast33 - Coeurl
	//Classification: Beastkin
	//Auto-Attack: Lightning

	//TemperedRelease
	//--ChargedWhiskerPvE

	//Trick
	//--BlasterPvE

	//Beast34 -
	//Classification:
	//Auto-Attack:

	//Beast35 -
	//Classification:
	//Auto-Attack:

	//Beast36 -
	//Classification:
	//Auto-Attack:

	//Beast37 -
	//Classification:
	//Auto-Attack:

	//Beast38 - Chimera
	//Classification: Beastkin
	//Auto-Attack: Slashing

	//TemperedRelease
	//--TheRamsVoicePvE

	//Trick
	//--TheLionsBreathPvE

	//Beast39 -
	//Classification:
	//Auto-Attack:

	//Beast40 -
	//Classification:
	//Auto-Attack:

	//Beast41 -
	//Classification:
	//Auto-Attack:

	//Beast42 -
	//Classification:
	//Auto-Attack:

	//Beast43 -
	//Classification:
	//Auto-Attack:

	//Beast44 -
	//Classification:
	//Auto-Attack:

	//Beast45 -
	//Classification:
	//Auto-Attack:

	//Beast46 -
	//Classification:
	//Auto-Attack:

	//Beast47 -
	//Classification:
	//Auto-Attack:

	//Beast48 -
	//Classification:
	//Auto-Attack:

	//Beast49 - 
	//Classification:
	//Auto-Attack:

	//Beast50 - Behemoth
	//Classification: Beastkin
	//Auto-Attack: Lightning

	//TemperedRelease
	//--MeteorPvE

	//Trick
	//--Thunderbolt

	#endregion
}
