using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;

namespace RotationSolver.Basic.Helpers;

internal static class EncounterImmunity
{
	private sealed record Rule(
		uint[]? NameIds = null,
		StatusID[]? TargetHas = null,
		StatusID[]? TargetLacks = null,
		StatusID[]? PlayerHas = null,
		StatusID[]? PlayerLacks = null,
		JobRole[]? PlayerRoles = null);

	private sealed record Encounter(string Name, Func<bool> IsActive, Rule[] Rules);

	private static readonly Rule[] VillainHeroRules =
	[
		new(TargetHas: [StatusID.EpicVillain], PlayerHas: [StatusID.VauntedHero, StatusID.FatedHero]),
		new(TargetHas: [StatusID.VauntedVillain], PlayerHas: [StatusID.EpicHero, StatusID.FatedHero]),
		new(TargetHas: [StatusID.FatedVillain], PlayerHas: [StatusID.EpicHero, StatusID.VauntedHero]),
	];

	private static readonly Encounter[] Encounters =
	[
		new("Crucible Ymir", static () => Service.Config.CrucibleYmir && DataCenter.IsinThirdBoard,
		[
			new(NameIds: [14569], TargetHas: [StatusID.VulnerabilityDown_2198]),
		]),
		new("Forked Tower heads", static () => Service.Config.ForkedtowerFirstBossVillianHero && DataCenter.IsInNorthHorn,
		[
			new(NameIds: [14490, 14491], TargetHas: [StatusID.EpicVillain_5400], PlayerHas: [StatusID.FatedHero]),
			new(NameIds: [14490, 14491], TargetHas: [StatusID.FatedVillain_5401], PlayerHas: [StatusID.EpicHero]),
		]),
		new("North Horn math CE", static () => DataCenter.IsInNorthHorn,
		[
			new(NameIds: [3915, 14521, 14522, 14528]),
		]),
		new("DMU boss", static () => Service.Config.DmuBossImmune && DataCenter.IsInDMU, VillainHeroRules),
		new("Jeuno boss", static () => Service.Config.JeunoBossImmune && DataCenter.TerritoryID == 1248, VillainHeroRules),
		new("The Unmaking gauntlets", static () => Service.Config.TheUnmakingShadow && DataCenter.IsTheUnmaking, GauntletRules()),
		new("Windurst Alexander", static () => Service.Config.AlexanderImmune && DataCenter.IsInWindurst,
		[
			new(TargetHas: [StatusID.PerfectDefense, StatusID.PerfectDefense_5377]),
		]),
		new("Orbonne", static () => DataCenter.Orbonne,
		[
			new(TargetHas: [StatusID.VulnerabilityDown_1782, StatusID.VulnerabilityDown_350]),
		]),
		new("M9S cells", static () => Service.Config.M9SCellTargeting && DataCenter.IsInM9S, CellRules()),
		new("Eminent Grief", static () => Service.Config.Eminent && DataCenter.TerritoryID is 1311 or 1333 or 1290,
		[
			new(NameIds: [14037], PlayerLacks: [StatusID.LightVengeance]),
			new(NameIds: [14038], PlayerLacks: [StatusID.DarkVengeance]),
		]),
		new("LotA Thanatos", static () => Service.Config.ThanatosImmune && DataCenter.TerritoryID == 174,
		[
			new(NameIds: [710], PlayerLacks: [StatusID.AstralRealignment]),
		]),
		new("Meso jailers", static () => Service.Config.JailerImmune && DataCenter.TerritoryID == 1292,
		[
			new(TargetHas: [(StatusID)4546], PlayerHas: [(StatusID)4543, (StatusID)4544, (StatusID)4545]),
			new(TargetHas: [(StatusID)4547], PlayerHas: [(StatusID)4542, (StatusID)4544, (StatusID)4545]),
			new(TargetHas: [(StatusID)4548], PlayerHas: [(StatusID)4542, (StatusID)4543, (StatusID)4545]),
			new(TargetHas: [(StatusID)4549], PlayerHas: [(StatusID)4542, (StatusID)4543, (StatusID)4544]),
		]),
		new("TEA True Heart", static () => Service.Config.TeaTrueHeart && DataCenter.TerritoryID == 887,
		[
			new(NameIds: [9223]),
		]),
		new("FRU Crystal of Darkness", static () => Service.Config.FruCrystalOfDarkness && DataCenter.TerritoryID == 1238,
		[
			new(NameIds: [13556]),
		]),
		new("Dohn Mheg Lyre", static () => Service.Config.DohnMhegLyre && DataCenter.TerritoryID == 821,
		[
			new(NameIds: [8958], PlayerLacks: [StatusID.Unfooled]),
		]),
		new("M8S wolves", static () => Service.Config.M8SWindStone && DataCenter.TerritoryID == 1263,
		[
			new(NameIds: [13846], PlayerHas: [StatusID.Windpack]),
			new(NameIds: [13847], PlayerHas: [StatusID.Stonepack]),
		]),
		new("Irminsul and Sawtooth", static () => Service.Config.IrminsulSawtoothImmune && DataCenter.TerritoryID == 508,
		[
			new(TargetHas: [StatusID.RangedResistance], PlayerRoles: [JobRole.RangedPhysical]),
			new(TargetHas: [StatusID.MagicResistance], PlayerRoles: [JobRole.RangedMagical, JobRole.Healer]),
		]),
		new("Superior Flight Unit", static () => Service.Config.SuperiorFlightUnitImmune && DataCenter.TerritoryID == 917,
		[
			new(TargetHas: [StatusID.ProcessOfEliminationA], PlayerHas: [StatusID.ShieldProtocolB, StatusID.ShieldProtocolC]),
			new(TargetHas: [StatusID.ProcessOfEliminationB], PlayerHas: [StatusID.ShieldProtocolA, StatusID.ShieldProtocolC]),
			new(TargetHas: [StatusID.ProcessOfEliminationC], PlayerHas: [StatusID.ShieldProtocolA, StatusID.ShieldProtocolB]),
		]),
		new("Dead Stars", static () => Service.Config.ForkedtowerDeadStar && DataCenter.IsInForkedTowerBlood,
		[
			new(NameIds: [(uint)NPCName.Triton], PlayerHas: [StatusID.NereidicGravity, StatusID.PhobosicGravity]),
			new(NameIds: [(uint)NPCName.Nereid], PlayerHas: [StatusID.TritonicGravity, StatusID.PhobosicGravity]),
			new(NameIds: [(uint)NPCName.Phobos], PlayerHas: [StatusID.TritonicGravity, StatusID.NereidicGravity]),
		]),
		new("Cloud of Darkness", static () => Service.Config.CodImmune && DataCenter.TerritoryID == 1241,
		[
			new(TargetHas: [StatusID.VeilOfDarkness], PlayerHas: [StatusID.OuterDarkness]),
			new(TargetHas: [StatusID.UnnamedStatus_4388], PlayerHas: [StatusID.InnerDarkness]),
		]),
		new("Limitless Blue", static () => Service.Config.LimitlessBlueTargeting && DataCenter.TerritoryID is 436 or 447,
		[
			new(NameIds: [3656, 3657], PlayerLacks: [StatusID.Whaleback]),
			new(NameIds: [3655], PlayerHas: [StatusID.WillOfTheWater]),
			new(NameIds: [3654], PlayerHas: [StatusID.WillOfTheWind]),
		]),
		new("Cinder Drift", static () => Service.Config.CinderDriftPallTargeting && DataCenter.TerritoryID == 912,
		[
			new(TargetHas: [StatusID.BlindToGrief], PlayerHas: [StatusID.PallOfGrief]),
			new(TargetHas: [StatusID.BlindToRage], PlayerHas: [StatusID.PallOfRage]),
		]),
		new("Resistance statuses", static () => DataCenter.TerritoryID is 508 or 281 or 359,
		[
			new(TargetHas: [StatusID.MagicResistance, StatusID.MantleOfTheWhorl], PlayerRoles: [JobRole.RangedMagical, JobRole.Healer]),
			new(TargetHas: [StatusID.RangedResistance, StatusID.VeilOfTheWhorl], PlayerRoles: [JobRole.RangedPhysical]),
		]),
		new("TOP Omega", static () => Service.Config.TopOmegaMf && DataCenter.TerritoryID == 1122,
		[
			new(TargetHas: [StatusID.OmegaF], PlayerHas: [StatusID.PacketFilterF_3500]),
			new(TargetHas: [StatusID.Omega, StatusID.OmegaM_3454], PlayerHas: [StatusID.PacketFilterM_3499]),
		]),
		new("O12S Omega", static () => Service.Config.O12SOmegaMf && DataCenter.TerritoryID is 801 or 805,
		[
			new(TargetHas: [StatusID.OmegaF], PlayerHas: [StatusID.PacketFilterF, StatusID.PacketFilterF_3500]),
			new(TargetHas: [StatusID.OmegaM, StatusID.OmegaM_3454], PlayerHas: [StatusID.PacketFilterM, StatusID.PacketFilterM_3499]),
		]),
	];

	private static Rule[] GauntletRules()
	{
		uint[] gauntlets = [14752, 14755, 14756, 14757];
		var rules = new List<Rule>();
		for (var i = 0u; i < 8; i++)
		{
			var taken = (StatusID)(5357 + i);
			var thrown = (StatusID)(5365 + i);
			rules.Add(new(NameIds: gauntlets, TargetHas: [thrown], PlayerLacks: [taken]));
			rules.Add(new(NameIds: gauntlets, TargetLacks: [thrown], PlayerHas: [taken]));
		}

		return [.. rules];
	}

	private static Rule[] CellRules()
	{
		var rules = new Rule[StatusHelper.HeelOfTheCellStatus.Length];
		for (var i = 0; i < rules.Length; i++)
		{
			rules[i] = new(NameIds: [14304], TargetHas: [StatusHelper.HeelOfTheCellStatus[i]], PlayerLacks: [StatusHelper.HellInACellStatus[i]]);
		}

		return rules;
	}

	internal static bool IsImmune(IBattleChara target)
	{
		if (Player.Object == null)
		{
			return false;
		}

		foreach (var encounter in Encounters)
		{
			if (!encounter.IsActive())
			{
				continue;
			}

			foreach (var rule in encounter.Rules)
			{
				if (Matches(rule, target))
				{
					LogImmune(encounter.Name, target);
					return true;
				}
			}
		}

		return IsTinyMageImmune(target) || IsJagdDollImmune(target) || IsColossusRubricatusImmune(target)
			|| IsDrakeImmune(target) || IsHanselOrGretelShielded(target) || IsSpikedSahaginImmune(target);
	}

	private static bool IsAlive(uint nameId)
	{
		foreach (var hostile in DataCenter.AllHostileTargets)
		{
			if (hostile?.CurrentHp > 0 && hostile.NameId == nameId)
			{
				return true;
			}
		}

		return false;
	}

	private static bool Matches(Rule rule, IBattleChara target)
	{
		return (rule.NameIds == null || Array.IndexOf(rule.NameIds, target.NameId) >= 0)
			&& (rule.TargetHas == null || target.HasStatus(false, rule.TargetHas))
			&& (rule.TargetLacks == null || !target.HasStatus(false, rule.TargetLacks))
			&& (rule.PlayerHas == null || StatusHelper.PlayerHasStatus(false, rule.PlayerHas))
			&& (rule.PlayerLacks == null || !StatusHelper.PlayerHasStatus(false, rule.PlayerLacks))
			&& (rule.PlayerRoles == null || Array.IndexOf(rule.PlayerRoles, Player.Object?.ClassJob.Value.GetJobRole() ?? JobRole.None) >= 0);
	}

	private static void LogImmune(string encounter, IBattleChara target)
	{
		if (Service.Config.InDebug)
		{
			PluginLog.Debug($"{encounter}: treating {target.Name} as immune.");
		}
	}

	private static bool IsTinyMageImmune(IBattleChara target)
	{
		const uint MeteorCastID = 48327;
		if (!Service.Config.NorthHornTinyMage || !DataCenter.IsInNorthHorn || target.CastActionId != MeteorCastID)
		{
			return false;
		}

		IBattleChara? soonest = null;
		var soonestRemaining = float.MaxValue;
		foreach (var hostile in DataCenter.AllHostileTargets)
		{
			if (hostile != null && hostile.CastActionId == MeteorCastID && hostile.RemainingCastTime < soonestRemaining)
			{
				soonestRemaining = hostile.RemainingCastTime;
				soonest = hostile;
			}
		}

		if (soonest == null || target.Equals(soonest) || target.DistanceToPlayer() <= 5)
		{
			return false;
		}

		LogImmune("North Horn Tiny Mage", target);
		return true;
	}

	private static bool IsJagdDollImmune(IBattleChara target)
	{
		return Service.Config.TeaJagdDoll && DataCenter.TerritoryID == 887 && target.NameId == 9214 && target.GetEffectiveHpPercent() < 25f;
	}

	private static bool IsColossusRubricatusImmune(IBattleChara target)
	{
		return Service.Config.ColossusRubricatusImmune && DataCenter.TerritoryID == 1174 && target.NameId == 9511 && target.CastActionId == 14574;
	}

	private static bool IsDrakeImmune(IBattleChara target)
	{
		if (!Service.Config.DrakeImmune || DataCenter.TerritoryID != 1069)
		{
			return false;
		}

		uint[] chain = [11463, 11464, 11465, 11466, 11467];
		var index = Array.IndexOf(chain, target.NameId);
		return index > 0 && IsAlive(chain[index]) && IsAlive(chain[index - 1]);
	}

	private static bool IsSpikedSahaginImmune(IBattleChara target)
	{
		return Service.Config.CrucibleYmir && DataCenter.IsinThirdBoard && target.NameId == 14571
			&& target.HasStatus(false, StatusID.ParalyzingSpikes) && IsAlive(14569);
	}

	private static bool IsHanselOrGretelShielded(IBattleChara target)
	{
		return Service.Config.HanselorGretelShieldedImmune && DataCenter.TerritoryID == 966
			&& target.HasStatus(false, StatusID.StrongOfShield) && target.FindEnemyPositional() != EnemyPositional.Front;
	}
}
