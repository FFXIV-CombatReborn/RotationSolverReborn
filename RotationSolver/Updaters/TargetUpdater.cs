using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;

namespace RotationSolver.Updaters;

internal static partial class TargetUpdater
{
	private static readonly ObjectListDelay<IBattleChara>
		_raisePartyTargets = new(() => Service.Config.RaiseDelay2),
		_raiseAllTargets = new(() => Service.Config.RaiseDelay2),
		_dispelPartyTargets = new(() => Service.Config.EsunaDelay);
	private static readonly List<IBattleChara> _raiseCandidates = [];
	private static readonly List<IBattleChara> _dispelCandidates = [];
	private static readonly List<IBattleChara> _allTargetsCache = [];
	private static readonly List<IBattleChara> _partyMembersCache = [];
	private static readonly List<IBattleChara> _allianceMembersCache = [];
	private static readonly List<IBattleChara> _hostileTargetsCache = [];
	private static readonly List<IBattleChara> _deathTanksCache = [];
	private static readonly List<IBattleChara> _deathHealersCache = [];
	private static readonly List<IBattleChara> _deathOffHealersCache = [];
	private static readonly List<IBattleChara> _deathOthersCache = [];

	private static readonly List<IBattleChara> _outOfDutyPlayers = [];

	private static long _lastUpdateTimeToKill;
	private const long TimeToKillUpdateIntervalMs = 1000;

	internal static void UpdateTargets()
	{
		DataCenter.TargetsByRange.Clear();
		DataCenter.AllTargets = GetAllTargets();

		if (DataCenter.AllTargets == null || DataCenter.AllTargets.Count == 0)
		{
			DataCenter.PartyMembers.Clear();
			DataCenter.AllianceMembers.Clear();
			DataCenter.AllHostileTargets.Clear();
			DataCenter.AllHostileTargetIds.Clear();
			_outOfDutyPlayers.Clear();
			DataCenter.DeathTarget = null;
			DataCenter.DispelTarget = null;
			DataCenter.ProvokeTarget = null;
			DataCenter.InterruptTarget = null;
			UpdateTimeToKill();
			return;
		}

		UpdateLists();
		DataCenter.DeathTarget = GetDeathTarget();
		DataCenter.DispelTarget = GetDispelTarget();
		DataCenter.ProvokeTarget = (DataCenter.Role == JobRole.Tank || StatusHelper.PlayerHasStatus(true, StatusID.VariantUltimatumSet))
			? GetFirstHostileTarget(ObjectHelper.CanProvoke) : null;
		DataCenter.InterruptTarget = GetFirstHostileTarget(ObjectHelper.CanInterrupt);

		if (DataCenter.IsInOccultCrescentOp && Service.Config.ElementalWeaknessTracking)
		{
			UpdateOccultWeaknesses();
		}

		UpdateTimeToKill();
	}

	private static unsafe void UpdateLists()
	{
		var allTargets = DataCenter.AllTargets;

		_partyMembersCache.Clear();
		_allianceMembersCache.Clear();
		_hostileTargetsCache.Clear();
		_outOfDutyPlayers.Clear();

		var raisetype = Service.Config.RaiseType;
		var ignoreInvincibility = DataCenter.IsPvP && Service.Config.IgnorePvPInvincibility;

		var playerEye = Player.Object?.Position;
		if (playerEye != null)
		{
			playerEye = new Vector3(playerEye.Value.X, playerEye.Value.Y + 2.0f, playerEye.Value.Z);
		}

		foreach (var member in allTargets)
		{
			try
			{
				if (member.IsEnemy())
				{
					if (playerEye == null)
					{
						continue;
					}

					if (member.DistanceToPlayer() < 48 && member.CanSeeFrom(playerEye.Value)
						&& (ignoreInvincibility || !member.HasInvincibleStatus()))
					{
						_hostileTargetsCache.Add(member);
					}
				}
				else if (member.IsParty())
				{
					if (member.Character() != null)
					{
						_partyMembersCache.Add(member);
					}
				}
				else if (member.Character() != null)
				{
					if (member.IsAllianceMember())
					{
						_allianceMembersCache.Add(member);
					}
					else if (raisetype == RaiseType.AllOutOfDuty && member.IsOtherPlayerOutOfDuty())
					{
						_outOfDutyPlayers.Add(member);
					}
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Error in Updating Member Lists: {ex.Message}");
			}
		}

		DataCenter.PartyMembers = _partyMembersCache;
		DataCenter.AllianceMembers = _allianceMembersCache;
		DataCenter.AllHostileTargets = _hostileTargetsCache;

		DataCenter.AllHostileTargetIds.Clear();
		foreach (var hostile in _hostileTargetsCache)
		{
			_ = DataCenter.AllHostileTargetIds.Add(hostile.GameObjectId);
		}
	}

	private static List<IBattleChara> GetAllTargets()
	{
		_allTargetsCache.Clear();
		var skipDummyCheck = !Service.Config.DisableTargetDummys;

		var objects = Svc.Objects;
		if (objects != null)
		{
			var count = objects.Length;
			for (var i = 0; i < count; i++)
			{
				var obj = objects[i];
				if (obj is IBattleChara battleChara)
				{
					if ((skipDummyCheck || !battleChara.IsDummy())
						&& battleChara.IsTargetable
						&& !battleChara.IsPet())
					{
						_allTargetsCache.Add(battleChara);
					}
				}
			}
		}

		return _allTargetsCache;
	}

	private static IBattleChara? GetFirstHostileTarget(Func<IBattleChara, bool> predicate)
	{
		var hostileTargets = DataCenter.AllHostileTargets;
		if (hostileTargets == null)
		{
			return null;
		}

		foreach (var target in hostileTargets)
		{
			try
			{
				if (predicate(target))
				{
					return target;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Error in GetFirstHostileTarget: {ex.Message}");
			}
		}
		return null;
	}

	private static IBattleChara? GetDeathTarget()
	{
		if (!DataCenter.CanRaise())
		{
			return null;
		}

		try
		{
			var raisetype = Service.Config.RaiseType;

			var validRaiseTargets = _raiseCandidates;
			validRaiseTargets.Clear();
			if (DataCenter.PartyMembers != null)
			{
				foreach (var target in DataCenter.PartyMembers.GetDeath())
				{
					if (raisetype != RaiseType.PartyHealersOnly || target.IsJobCategory(JobRole.Healer))
					{
						validRaiseTargets.Add(target);
					}
				}
			}

			if (DataCenter.AllianceMembers != null)
			{
				if (raisetype == RaiseType.PartyAndAllianceSupports || raisetype == RaiseType.PartyAndAllianceHealers)
				{
					foreach (var member in DataCenter.AllianceMembers.GetDeath())
					{
						if (raisetype == RaiseType.PartyAndAllianceHealers)
						{
							if (member.IsJobCategory(JobRole.Healer))
							{
								validRaiseTargets.Add(member);
							}
						}
						else
						{
							if (member.IsJobCategory(JobRole.Healer) || member.IsJobCategory(JobRole.Tank))
							{
								validRaiseTargets.Add(member);
							}
						}
					}
				}
				else if (raisetype == RaiseType.All || raisetype == RaiseType.AllOutOfDuty)
				{
					foreach (var target in DataCenter.AllianceMembers.GetDeath())
					{
						validRaiseTargets.Add(target);
					}
				}
			}

			if (raisetype == RaiseType.AllOutOfDuty)
			{
				foreach (var target in _outOfDutyPlayers.GetDeath())
				{
					validRaiseTargets.Add(target);
				}
			}

			if (raisetype == RaiseType.PartyOnly)
			{
				_raisePartyTargets.Delay(validRaiseTargets);
				validRaiseTargets.Clear();
				foreach (var p in _raisePartyTargets)
				{
					validRaiseTargets.Add(p);
				}
			}
			else
			{
				_raiseAllTargets.Delay(validRaiseTargets);
				validRaiseTargets.Clear();
				foreach (var p in _raiseAllTargets)
				{
					validRaiseTargets.Add(p);
				}
			}

			return GetPriorityDeathTarget(validRaiseTargets, raisetype);
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Error in GetDeathTarget: {ex.Message}");
			return null;
		}
	}

	private static IBattleChara? GetPriorityDeathTarget(List<IBattleChara> validRaiseTargets, RaiseType raiseType = RaiseType.PartyOnly)
	{
		if (validRaiseTargets.Count == 0)
		{
			return null;
		}

		_deathTanksCache.Clear();
		_deathHealersCache.Clear();
		_deathOffHealersCache.Clear();
		_deathOthersCache.Clear();

		var deathTanks = _deathTanksCache;
		var deathHealers = _deathHealersCache;
		var deathOffHealers = _deathOffHealersCache;
		var deathOthers = _deathOthersCache;

		foreach (var chara in validRaiseTargets)
		{
			if (chara.IsJobCategory(JobRole.Tank))
			{
				deathTanks.Add(chara);
			}
			else if (chara.IsJobCategory(JobRole.Healer))
			{
				deathHealers.Add(chara);
			}
			else if (Service.Config.OffRaiserRaise && chara.IsJobs(Job.SMN))
			{
				deathOffHealers.Add(chara);
			}
			else if (Service.Config.OffRaiserRaise && chara.IsJobs(Job.RDM))
			{
				deathOffHealers.Add(chara);
			}
			else
			{
				deathOthers.Add(chara);
			}
		}

		if (Service.Config.H2)
		{
			deathTanks.Reverse();
			deathHealers.Reverse();
			deathOffHealers.Reverse();
			deathOthers.Reverse();
		}

		if (raiseType == RaiseType.PartyAndAllianceHealers && deathHealers.Count > 0)
		{
			return deathHealers[0];
		}

		if (deathTanks.Count > 1)
		{
			return deathTanks[0];
		}

		return deathHealers.Count > 0
			? deathHealers[0]
			: deathTanks.Count > 0
			? deathTanks[0]
			: Service.Config.OffRaiserRaise && deathOffHealers.Count > 0
			? deathOffHealers[0]
			: deathOthers.Count > 0 ? deathOthers[0] : null;
	}

	private static IBattleChara? GetDispelTarget()
	{
		if (Player.Job is Job.WHM or Job.SCH or Job.AST or Job.SGE or Job.BRD or Job.CNJ)
		{
			var weakenPeople = _dispelCandidates;
			weakenPeople.Clear();
			AddDispelTargets(DataCenter.PartyMembers, weakenPeople);

			_dispelPartyTargets.Delay(weakenPeople);

			const AutoStatus busyStatus = AutoStatus.HealAreaAbility | AutoStatus.HealAreaSpell
				| AutoStatus.HealSingleAbility | AutoStatus.HealSingleSpell
				| AutoStatus.DefenseArea | AutoStatus.DefenseSingle;
			var canDispelNonDangerous = (DataCenter.MergedStatus & busyStatus) == 0;

			IBattleChara? closestDangerous = null;
			var closestDangerousDist = float.MaxValue;
			IBattleChara? closestNonDangerous = null;
			var closestNonDangerousDist = float.MaxValue;
			var minRemaining = 1 + DataCenter.DefaultGCDRemain;

			foreach (var person in _dispelPartyTargets)
			{
				var hasDangerous = false;
				foreach (ref readonly var status in person.GetStatuses())
				{
					if (StatusHelper.IsDangerous(status, minRemaining))
					{
						hasDangerous = true;
						break;
					}
				}

				var dist = ObjectHelper.DistanceToPlayer(person);
				if (hasDangerous)
				{
					if (dist < closestDangerousDist)
					{
						closestDangerousDist = dist;
						closestDangerous = person;
					}
				}
				else if (dist < closestNonDangerousDist)
				{
					closestNonDangerousDist = dist;
					closestNonDangerous = person;
				}
			}

			var allowNonDangerous = canDispelNonDangerous
									 || !DataCenter.HasHostilesInRange
									 || Service.Config.DispelAll
									 || DataCenter.IsPvP;

			if (!allowNonDangerous)
			{
				return closestDangerous;
			}

			return closestDangerous ?? closestNonDangerous;
		}
		return null;
	}

	private static void AddDispelTargets(List<IBattleChara>? members, List<IBattleChara> targetList)
	{
		if (members == null)
		{
			return;
		}

		var minRemaining = 1 + DataCenter.DefaultGCDRemain;
		foreach (var member in members)
		{
			foreach (ref readonly var status in member.GetStatuses())
			{
				if (StatusHelper.CanDispel(status, minRemaining))
				{
					targetList.Add(member);
					break;
				}
			}
		}
	}

	private static void UpdateTimeToKill()
	{
		var now = Environment.TickCount64;
		if (now - _lastUpdateTimeToKill < TimeToKillUpdateIntervalMs)
		{
			return;
		}

		_lastUpdateTimeToKill = now;
		TimeToKillTracker.Record(DataCenter.AllHostileTargets, now);
	}

	private static void UpdateOccultWeaknesses()
	{
		var hostiles = DataCenter.AllHostileTargets;
		if (hostiles == null || hostiles.Count == 0)
		{
			return;
		}

		var foundNew = false;

		for (var i = 0; i < hostiles.Count; i++)
		{
			var hostile = hostiles[i];
			if (hostile == null || hostile.NameId == 0)
			{
				continue;
			}

			if (StatusHelper.HasKnownOccultWeakness(hostile.NameId))
			{
				continue;
			}

			foreach (var weakness in StatusHelper.OccultWeaknessStatuses)
			{
				if (hostile.HasStatus(false, weakness))
				{
					if (OtherConfiguration.RecordOccultWeakness(hostile.NameId, weakness))
					{
						foundNew = true;
					}
				}
			}
		}

		if (foundNew)
		{
			if (DataCenter.IsInNorthHorn)
			{
				_ = OtherConfiguration.SaveNorthHornWeaknessRecords();
			}
			else if (DataCenter.IsInSouthHorn)
			{
				_ = OtherConfiguration.SaveSouthHornWeaknessRecords();
			}
		}
	}
}