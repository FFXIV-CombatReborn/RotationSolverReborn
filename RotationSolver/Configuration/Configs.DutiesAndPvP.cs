namespace RotationSolver.Configuration;

internal partial class Configs
{
	#region Duty Specific
	[ConditionBool, UI("Don't use flat damage or death actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockflatdamagedeathimmuneBLU = false;

	[ConditionBool, UI("Don't use Slow actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockslowimmuneBLU = false;

	[ConditionBool, UI("Don't use Petrification actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockpetrificationimmuneBLU = false;

	[ConditionBool, UI("Don't use Paralysis actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockparalysisimmuneBLU = false;

	[ConditionBool, UI("Don't use Interrupt actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockinterruptimmuneBLU = false;

	[ConditionBool, UI("Don't use Blind actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockblindimmuneBLU = false;

	[ConditionBool, UI("Don't use Stun actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockstunimmuneBLU = false;

	[ConditionBool, UI("Don't use Sleep actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blocksleepimmuneBLU = false;

	[ConditionBool, UI("Don't use Bind actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockbindimmuneBLU = false;

	[ConditionBool, UI("Don't use Heavy actions on immune enemies", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockheavyimmuneBLU = false;

	[ConditionBool, UI("Don't use aspected actions on enemies immune to that aspect", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blockimmuneBLU = true;

	[ConditionBool, UI("Only use actions of an aspect the enemy is weak to", Filter = DutySpecificTheMaskedCarnivale)]
	private static readonly bool _blocknonweakBLU = false;

	[ConditionBool, UI("Still allow unaspected actions", Filter = DutySpecificTheMaskedCarnivale, Parent = nameof(BlocknonweakBlu))]
	private static readonly bool _allowunaspectedBLU = true;

	[ConditionBool, UI("O12S - Packet Filter logic",
	Description = "Treat OmegaM/OmegaF as immune if you have their corresponding Packet Filter status (also applies to Normal).",
	Filter = DutySpecificSavage)]
	private static readonly bool _o12sOmegaMF = true;

	[ConditionBool, UI("M8S - Wolf Pack/Stone Pack logic",
	Description = "Treat Wolf of Wind/Wolf of Stone as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificSavage)]
	private static readonly bool _m8sWindStone = true;

	[ConditionBool, UI("M9S - Only use cleave logic",
	Description = "This should clear up any targeting issues when a nail, the boss, and a flail are near each other at the same time.",
	Filter = DutySpecificSavage)]
	private static readonly bool _m9sCleaveOnly = true;

	[ConditionBool, UI("M9S - Cell targeting logic",
	Description = "Treat Cells as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificSavage)]
	private static readonly bool _m9sCellTargeting = true;

	[ConditionBool, UI("M9S - Ads targeting logic",
	Description = "Prioritize Doornail or Flail based on role and distance to target.",
	Filter = DutySpecificSavage)]
	private static readonly bool _m9sAdsTargeting = true;

	[ConditionBool, UI("M10S - Firesnaking/Watersnaking targeting logic",
	Description = "Prioritize Red Hot if you have the firesnaking buff, and Deep Blue if you have the watersnaking buff (also applies to Normal).",
	Filter = DutySpecificSavage)]
	private static readonly bool _m10sBroTargeting = true;

	[ConditionBool, UI("Limitless Blue Extreme - Whaleback logic",
	Description = "Treat Bismarck Shell/Bismarck Corona as immune if you don't have the Whaleback status.",
	Filter = DutySpecificExtreme)]
	private static readonly bool _limitlessBlueTargeting = true;

	[ConditionBool, UI("Cinder Drift Extreme - Pall targeting logic",
	Description = "Treat Pall of Rage/Pall of Grief as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificExtreme)]
	private static readonly bool _cinderDriftPallTargeting = true;

	[ConditionBool, UI("The Unmaking Extreme - Shadow logic",
	Description = "Treat Shadow as immune if you don't have the corresponding status.",
	Filter = DutySpecificExtreme)]
	private static readonly bool _theUnmakingShadow = true;

	[ConditionBool, UI("The Epic of Alexander (Ultimate) - Jagd Doll logic",
	Description = "Treat Jagd Doll ads as immune when HP is less than 25%.",
	Filter = DutySpecificUltimate)]
	private static readonly bool _teaJagdDoll = true;

	[ConditionBool, UI("The Epic of Alexander (Ultimate) - True Heart logic",
	Description = "Treat True Heart ad as immune.",
	Filter = DutySpecificUltimate)]
	private static readonly bool _teaTrueHeart = true;

	[ConditionBool, UI("The Omega Protocol (Ultimate) - Packet Filter logic",
	Description = "Treat OmegaM/OmegaF as immune if you have their corresponding Packet Filter status.",
	Filter = DutySpecificUltimate)]
	private static readonly bool _topOmegaMF = true;

	[ConditionBool, UI("Futures Rewritten (Ultimate) - Crystal of Darkness logic",
	Description = "Treat Crystal of Darkness ad as immune.",
	Filter = DutySpecificUltimate)]
	private static readonly bool _fruCrystalOfDarkness = true;

	[ConditionBool, UI("Dancing Mad (Ultimate) - Hero/Villain logic",
	Description = "Treat bosses as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificUltimate)]
	private static readonly bool _dmuBossImmune = true;

	[ConditionBool, UI("The Ghimlyt Dark - Colossus Rubricatus logic",
	Description = "Treat Colossus Rubricatus as immune while it's casting the scripted action that leads to its death.",
	Filter = DutySpecificDungeon)]
	private static readonly bool _colossusRubricatusImmune = true;

	[ConditionBool, UI("Dohn Mheg - Liar's Lyre logic",
	Description = "Treat Liar's Lyre as immune if you don't have the Unfooled status.",
	Filter = DutySpecificDungeon)]
	private static readonly bool _dohnMhegLyre = true;

	[ConditionBool, UI("The Meso Terminal - Jailer logic",
	Description = "Treat Jailers in the second boss fight as immune if you don't have the corresponding buff.",
	Filter = DutySpecificDungeon)]
	private static readonly bool _jailerImmune = true;

	[ConditionBool, UI("Forked Tower: Blood - Dead Stars logic",
	Description = "Treat Triton/Nereid/Phobos as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificFieldOps)]
	private static readonly bool _forkedtowerDeadStar = true;

	[ConditionBool, UI("Forked Tower: Magic - First boss Hero/Villain logic",
	Description = "Treat the first boss of Forked Tower as immune if you don't have the corresponding status for it.",
	Filter = DutySpecificFieldOps)]
	private static readonly bool _forkedtowerFirstBossVillianHero = true;

	[ConditionBool, UI("North Horn CE - Tiny Mage targeting logic",
	Description = "Treat Tiny Mage CE mobs as immune if you're more than 5 yalms away and they are not the farthest along their cast.",
	Filter = DutySpecificFieldOps)]
	private static readonly bool _northHornTinyMage = true;

	[ConditionBool, UI("Occult Crescent - Track elemental weaknesses",
	Description = "Tracks enemies' elemental weaknesses so you can report them to the developers. See Debug > Occult Crescent Weaknesses.",
	Filter = DutySpecificFieldOps)]
	private static readonly bool _elementalWeaknessTracking = false;

	[ConditionBool, UI("Pilgrim's Traverse - Eminent Grief logic",
	Description = "Treat Eminent Grief as immune if you don't have the Light Vengeance buff, and treat Devoured Eater as immune if you don't have the Dark Vengeance buff.",
	Filter = DutySpecificDeepDungeon)]
	private static readonly bool _eminent = true;

	[ConditionBool, UI("The Labyrinth of the Ancients - Thanatos logic",
	Description = "Treat Thanatos as immune if you don't have the Astral Realignment buff.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _thanatosImmune = true;

	[ConditionBool, UI("The Void Ark - Irminsul and Sawtooth logic",
	Description = "Treat Irminsul and Sawtooth as immune if you don't have the corresponding buff.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _irminsulSawtoothImmune = true;

	[ConditionBool, UI("The Puppets' Bunker - Superior Flight Unit logic",
	Description = "Treat each Superior Flight Unit as immune if you don't have the corresponding buff.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _superiorFlightUnitImmune = true;

	[ConditionBool, UI("The Tower at Paradigm's Breach - Hansel and Gretel logic",
	Description = "Treat each Hansel/Gretel as immune if you are at an angle that would cause you to take rebound damage from the shield mechanic.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _hanselorGretelShieldedImmune = true;

	[ConditionBool, UI("Jeuno: The First Walk - The Ark Angels logic",
	Description = "Treat each Ark Angel as immune if you don't have the corresponding buff.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _jeunoBossImmune = true;

	[ConditionBool, UI("Windurst: The Third Walk - Alexander Resurrected logic",
	Description = "Treat Alexander and/or Gordius Systems as immune if they have Perfect Defense.",
	Filter = DutySpecificAlliance)]
	private static readonly bool _alexanderImmune = true;

	[ConditionBool, UI("Cloud of Darkness - Ads phase logic",
	Description = "Treat Cloud of Darkness/Stygian as immune if you don't have the corresponding buff.",
	Filter = DutySpecificChaoticAlliance)]
	private static readonly bool _codImmune = true;

	[ConditionBool, UI("The Sil'dihn Subterrane - Drake ads logic",
	Description = "Treat certain drakes as immune so they die in the order needed for Variant path 12.",
	Filter = DutySpecificVariantDungeon)]
	private static readonly bool _drakeImmune = true;

	[ConditionBool, UI("Treasure dungeons - Timed mob logic",
	Description = "Prioritizes limited-time mobs in treasure dungeons to resolve the extra loot mechanic.",
	Filter = DutySpecificTreasureDungeon)]
	private static readonly bool _treasuredungeontimed = true;

	[ConditionBool, UI("Treasure dungeons - Numbered mob logic",
	Description = "Sets mobs with number plates as top priority, from lowest to highest, to resolve the extra loot mechanic.",
	Filter = DutySpecificTreasureDungeon)]
	private static readonly bool _treasuredungeonnumbered = true;

	[ConditionBool, UI("Crucible of the Unbroken - Ymir logic",
	Description = "Prioritize the Sahagin Piece until it has Paralyzing Spikes, then the Ymir Shell, then the Ymir Piece once its shell is gone. Ymir is treated as immune while it has Vulnerability Down, and the spiked Sahagin while Ymir is up.",
	Filter = DutySpecificCrucibleOfTheUnbroken)]
	private static readonly bool _crucibleYmir = true;

	[ConditionBool, UI("Crucible of the Unbroken - Siren logic",
	Description = "Prioritize Crawling Pieces, which bind you in place.",
	Filter = DutySpecificCrucibleOfTheUnbroken)]
	private static readonly bool _crucibleSiren = true;
	#endregion

	#region PvP
	[JobConfig, UI("Use Guard below this HP",
		Filter = DutySpecificPvP)]
	[Range(0, 1, ConfigUnitType.Percent, 0.02f)]
	private readonly float _healthForGuard = 0.15f;

	[ConditionBool, UI("Ignore time to kill in PvP", Filter = DutySpecificPvP)]
	private static readonly bool _ignorePvPTTK = true;

	[ConditionBool, UI("Prioritize A-tier tomeliths in Shatter", Filter = DutySpecificPvP, Order = 1)]
	private static readonly bool _prioAtomelith = false;

	[ConditionBool, UI("Prioritize B-tier tomeliths in Shatter", Filter = DutySpecificPvP, Order = 1)]
	private static readonly bool _prioBtomelith = false;

	[JobConfig, UI("Ignore invincibility in PvP", Filter = DutySpecificPvP)]
	private static readonly bool _ignorePvPInvincibility = false;

	[ConditionBool, UI("Turn off when you die in PvP",
		 Filter = DutySpecificPvP, Section = 1)]
	private static readonly bool _autoOffWhenDeadPvP = true;

	[ConditionBool, UI("Turn off when a PvP match ends",
		 Filter = DutySpecificPvP, Section = 1)]
	private static readonly bool _autoOffPvPMatchEnd = true;

	[ConditionBool, UI("Turn on when a PvP match starts",
		 Filter = DutySpecificPvP, Section = 1)]
	private static readonly bool _autoOnPvPMatchStart = true;

	[ConditionBool, UI("Use the PvP state when turning RSR on in PvP",
		 Filter = DutySpecificPvP, Section = 1)]
	private static readonly bool _pvpStateControl = false;

	[ConditionBool, UI("Don't use actions while Guard is up",
		 Filter = DutySpecificPvP)]
	private static readonly bool _pvpGuardControl = true;

	[ConditionBool, UI("Cancel casts when the target gains Guard",
		Description = "Actions that ignore Guard keep casting.",
		Filter = DutySpecificPvP)]
	private static readonly bool _pvpGuardCancel = false;

	[ConditionBool, UI("Use Purify on Stun",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifyStun = true;

	[ConditionBool, UI("Use Purify on Silence",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifySilence = true;

	[ConditionBool, UI("Use Purify on Deep Freeze",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifyDeepFreeze = true;

	[ConditionBool, UI("Use Purify on Miracle of Nature",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifyMiracleOfNature = true;

	[ConditionBool, UI("Use Purify on Heavy",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifyHeavy = false;

	[ConditionBool, UI("Use Purify on Bind",
		 Filter = DutySpecificPvP, Section = 2)]
	private static readonly bool _pvpPurifyBind = false;

	[ConditionBool, UI("Hold GCDs for heals below 50% HP (Experimental)",
		Description = "Stops the GCD rotation while you're below 50% HP and have over 2000 MP, so the MP goes to heals.",
		Filter = DutySpecificPvP)]
	private static readonly bool _pvpGCDLockControl = true;

	[ConditionBool, UI("Sprint with no target, even near enemies (Experimental)",
		 Filter = DutySpecificPvP)]
	private static readonly bool _pvpAllowSprintWithoutTarget = true;

	#endregion
}
