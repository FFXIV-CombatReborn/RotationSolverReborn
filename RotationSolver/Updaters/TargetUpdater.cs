using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Updaters;

internal static partial class TargetUpdater
{
	private static readonly ObjectListDelay<IBattleChara>
		_raisePartyTargets = new(() => Service.Config.RaiseDelay2),
		_raiseAllTargets = new(() => Service.Config.RaiseDelay2),
		_dispelPartyTargets = new(() => Service.Config.EsunaDelay);


	private static DateTime _lastUpdateTimeToKill = DateTime.MinValue;
	private static readonly TimeSpan TimeToKillUpdateInterval = TimeSpan.FromSeconds(1);

	internal static void UpdateTargets()
	{
		DataCenter.TargetsByRange.Clear();
		DataCenter.AllTargets = GetAllTargets();

		// Early-out: avoid downstream work and stale data when there are no potential targets
		if (DataCenter.AllTargets == null || DataCenter.AllTargets.Count == 0)
		{
			DataCenter.PartyMembers.Clear();
			DataCenter.AllianceMembers.Clear();
			DataCenter.AllHostileTargets.Clear();
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
			? GetFirstHostileTarget(ObjectHelper.CanProvoke)
			: null; // Calculating this per frame rather than on-demand is actually a fair amount worse
		DataCenter.InterruptTarget = GetFirstHostileTarget(ObjectHelper.CanInterrupt); // Tanks, Melee, RDM, and various phantom and duty actions can interrupt so just deal with it

		if (DataCenter.IsInOccultCrescentOp && Service.Config.ElementalWeaknessTracking)
		{
			UpdateOccultWeaknesses();
		}

		UpdateTimeToKill();
	}

	private static unsafe void UpdateLists()
	{
		var allTargets = DataCenter.AllTargets;
		if (allTargets == null || allTargets.Count == 0)
		{
			// Defensive: ensure lists are empty if no targets
			DataCenter.PartyMembers.Clear();
			DataCenter.AllianceMembers.Clear();
			DataCenter.AllHostileTargets.Clear();
			return;
		}

		// Pre-size lists to reduce reallocations
		List<IBattleChara> partyMembers = new(capacity: allTargets.Count);
		List<IBattleChara> allianceMembers = new(capacity: allTargets.Count);
		List<IBattleChara> hostileTargets = new(capacity: allTargets.Count);

		// Track party membership by id to avoid O(n^2) Contains checks
		HashSet<ulong> partyIds = new(capacity: 32);

		var raisetype = Service.Config.RaiseType;

		// Compute player eye position once
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
					// Only proceed if playerEye is available
					if (playerEye == null)
					{
						continue;
					}

					if (member.IsTargetable && member.DistanceToPlayer() < 48 && member.CanSeeFrom(playerEye.Value))
					{
						// Valid hostile target
						var hasInvincible = false;
						var statusList = member.StatusList;

						if (statusList != null)
						{
							var statusCount = statusList.Length;
							for (var i = 0; i < statusCount; i++)
							{
								var status = statusList[i];
								if (status != null && status.StatusId != 0 && status.IsInvincible())
								{
									hasInvincible = true;
									break;
								}
							}
						}
						if (hasInvincible && ((DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility) || !DataCenter.IsPvP))
						{
							continue; // Invincible enemy doesn't get added to any lists
						}
						hostileTargets.Add(member);
					}
				}
				else if (member.IsPet())
				{
					continue; // We never target these
				}
				else if (member.IsParty())
				{
					var character = member.Character();
					if (character != null)
					{
						partyMembers.Add(member);
						partyIds.Add(member.GameObjectId);
						continue; // Party members are only added to the party list
					}
				}
				else // Not a party member or hostile, so check alliance status
				{
					// Alliance members are based on the raise settings
					if (raisetype == RaiseType.PartyOnly)
					{
						// No alliance member checks
					}
					else if (raisetype == RaiseType.AllOutOfDuty)
					{
						if (member.IsOtherPlayerOutOfDuty() && !partyIds.Contains(member.GameObjectId)) // Avoid O(n) Contains on list
						{
							var character = member.Character();
							if (character != null)
							{
								allianceMembers.Add(member);
								continue;
							}
						}
					}
					else if (member.IsAllianceMember() && !partyIds.Contains(member.GameObjectId))
					{
						var character = member.Character();
						if (character != null)
						{
							allianceMembers.Add(member);
						}
					}
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Error in Updating Member Lists: {ex.Message}");
			}
		}

		DataCenter.PartyMembers = partyMembers;
		DataCenter.AllianceMembers = allianceMembers;
		DataCenter.AllHostileTargets = hostileTargets;
	}

	//internal static void OldUpdateTargets()
	//{
	//	//PluginLog.Debug("Updating targets");
	//	DataCenter.TargetsByRange.Clear();
	//	DataCenter.AllTargets = GetAllTargets();

	//	// Early-out: avoid downstream work when there are no potential targets
	//	if (DataCenter.AllTargets == null || DataCenter.AllTargets.Count == 0)
	//	{
	//		DataCenter.PartyMembers.Clear();
	//		DataCenter.AllianceMembers.Clear();
	//		DataCenter.AllHostileTargets.Clear();
	//		DataCenter.DeathTarget = null;
	//		DataCenter.DispelTarget = null;
	//		DataCenter.ProvokeTarget = null;
	//		DataCenter.InterruptTarget = null;
	//		UpdateTimeToKill();
	//		return;
	//	}

	//	DataCenter.PartyMembers = GetPartyMembers();
	//	DataCenter.AllianceMembers = GetAllianceMembers();
	//	DataCenter.AllHostileTargets = GetAllHostileTargets();
	//	DataCenter.DeathTarget = GetDeathTarget();
	//	DataCenter.DispelTarget = GetDispelTarget();
	//	DataCenter.ProvokeTarget = (DataCenter.Role == JobRole.Tank || StatusHelper.PlayerHasStatus(true, StatusID.VariantUltimatumSet)) ? GetFirstHostileTarget(ObjectHelper.CanProvoke) : null; // Calculating this per frame rather than on-demand is actually a fair amount worse
	//	DataCenter.InterruptTarget = GetFirstHostileTarget(ObjectHelper.CanInterrupt); // Tanks, Melee, RDM, and various phantom and duty actions can interrupt so just deal with it

	//	UpdateTimeToKill();
	//}

	private static List<IBattleChara> GetAllTargets()
	{
		List<IBattleChara> allTargets = [];
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
						&& battleChara.StatusList != null
						&& !battleChara.IsPet())
					{
						allTargets.Add(battleChara);
					}
				}
			}
		}

		return allTargets;
	}

	private static unsafe List<IBattleChara> GetPartyMembers()
	{
		return GetMembers(DataCenter.AllTargets, isParty: true);
	}

	private static unsafe List<IBattleChara> GetAllianceMembers()
	{
		var raisetype = Service.Config.RaiseType;

		if (raisetype == RaiseType.PartyOnly)
		{
			return [];
		}

		if (raisetype == RaiseType.AllOutOfDuty)
		{
			return GetMembers(DataCenter.AllTargets, isParty: false, isAlliance: false, IsOutDuty: true);
		}

		return GetMembers(DataCenter.AllTargets, isParty: false, isAlliance: true, IsOutDuty: false);
	}

	private static unsafe List<IBattleChara> GetMembers(List<IBattleChara> source, bool isParty, bool isAlliance = false, bool IsOutDuty = false)
	{
		List<IBattleChara> members = [];
		if (source == null)
		{
			return members;
		}

		foreach (var member in source)
		{
			try
			{
				if (member.IsPet())
				{
					continue;
				}

				if (isParty && !member.IsParty())
				{
					continue;
				}

				if (isAlliance && (!ObjectHelper.IsAllianceMember(member) || member.IsParty()))
				{
					continue;
				}

				if (IsOutDuty && (!ObjectHelper.IsOtherPlayerOutOfDuty(member) || member.IsParty()))
				{
					continue;
				}

				var character = member.Character();
				if (character == null)
				{
					continue;
				}

				members.Add(member);
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Error in GetMembers: {ex.Message}");
			}
		}
		return members;
	}

	private static List<IBattleChara> GetAllHostileTargets()
	{
		List<IBattleChara> hostileTargets = [];
		var allTargets = DataCenter.AllTargets;
		if (allTargets == null || allTargets.Count == 0)
		{
			return hostileTargets;
		}

		// Reserve capacity to minimize internal resizes
		if (hostileTargets.Capacity < allTargets.Count)
		{
			hostileTargets.Capacity = allTargets.Count;
		}

		// Fix: Check if Player.Object is not null before using its Position
		var playerEye = Player.Object?.Position;
		if (playerEye != null)
		{
			playerEye = new Vector3(playerEye.Value.X, playerEye.Value.Y + 2.0f, playerEye.Value.Z);
		}

		foreach (var target in allTargets)
		{
			// Only proceed if playerEye is available
			if (playerEye == null)
			{
				continue;
			}

			if (!target.IsEnemy() || !target.IsTargetable || !target.CanSeeFrom(playerEye.Value) || target.DistanceToPlayer() >= 48)
			{
				continue;
			}

			var hasInvincible = false;
			var statusList = target.StatusList;
			if (statusList != null)
			{
				var statusCount = statusList.Length;
				for (var i = 0; i < statusCount; i++)
				{
					var status = statusList[i];
					if (status != null && status.StatusId != 0 && StatusHelper.IsInvincible(status))
					{
						hasInvincible = true;
						break;
					}
				}
			}
			if (hasInvincible &&
				((DataCenter.IsPvP && !Service.Config.IgnorePvPInvincibility) || !DataCenter.IsPvP))
			{
				continue;
			}

			hostileTargets.Add(target);
		}

		return hostileTargets;
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

			// Collect party deaths and track by id for O(1) membership tests
			var validRaiseTargets = new List<IBattleChara>();
			var deathPartyIds = new HashSet<ulong>();
			if (DataCenter.PartyMembers != null)
			{
				if (raisetype != RaiseType.PartyHealersOnly)
				{
					foreach (var target in DataCenter.PartyMembers.GetDeath())
					{
						validRaiseTargets.Add(target);
						_ = deathPartyIds.Add(target.GameObjectId);
					}
				}

				if (raisetype == RaiseType.PartyHealersOnly)
				{
					foreach (var target in DataCenter.PartyMembers.GetDeath())
					{
						if (target.IsJobCategory(JobRole.Healer))
						{
							validRaiseTargets.Add(target);
							_ = deathPartyIds.Add(target.GameObjectId);
						}
					}
				}
			}

			// Add alliance candidates depending on raise mode without N^2 checks
			if (DataCenter.AllianceMembers != null)
			{
				if (raisetype == RaiseType.PartyAndAllianceSupports || raisetype == RaiseType.PartyAndAllianceHealers)
				{
					foreach (var member in DataCenter.AllianceMembers.GetDeath())
					{
						if (deathPartyIds.Contains(member.GameObjectId))
						{
							continue;
						}

						if (raisetype == RaiseType.PartyAndAllianceHealers)
						{
							if (member.IsJobCategory(JobRole.Healer))
							{
								validRaiseTargets.Add(member);
							}
						}
						else // PartyAndAllianceSupports
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
						if (!deathPartyIds.Contains(target.GameObjectId))
						{
							validRaiseTargets.Add(target);
						}
					}
				}
			}

			// Apply raise delay without allocating a new list per frame
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

		List<IBattleChara> deathTanks = [];
		List<IBattleChara> deathHealers = [];
		List<IBattleChara> deathOffHealers = [];
		List<IBattleChara> deathOthers = [];

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

		if (raiseType == RaiseType.PartyAndAllianceHealers && deathHealers.Count > 0)
		{
			return deathHealers[0];
		}

		if (Service.Config.H2)
		{
			deathTanks.Reverse();
			deathHealers.Reverse();
			deathOffHealers.Reverse();
			deathOthers.Reverse();
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
			List<IBattleChara> weakenPeople = [];
			AddDispelTargets(DataCenter.PartyMembers, weakenPeople);

			// Apply dispel delay to the candidate list
			_dispelPartyTargets.Delay(weakenPeople);

			var canDispelNonDangerous = !DataCenter.MergedStatus.HasFlag(AutoStatus.HealAreaAbility)
										&& !DataCenter.MergedStatus.HasFlag(AutoStatus.HealAreaSpell)
										&& !DataCenter.MergedStatus.HasFlag(AutoStatus.HealSingleAbility)
										&& !DataCenter.MergedStatus.HasFlag(AutoStatus.HealSingleSpell)
										&& !DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea)
										&& !DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseSingle);

			// Single-pass selection over the delayed set to avoid extra list allocations
			IBattleChara? closestDangerous = null;
			var closestDangerousDist = float.MaxValue;
			IBattleChara? closestNonDangerous = null;
			var closestNonDangerousDist = float.MaxValue;

			foreach (var person in _dispelPartyTargets)
			{
				var hasDangerous = false;
				var statusList = person.StatusList;
				if (statusList != null)
				{
					for (int i = 0, n = statusList.Length; i < n; i++)
					{
						var status = statusList[i];
						if (status != null && status.IsDangerous())
						{
							hasDangerous = true;
							break;
						}
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

		foreach (var member in members)
		{
			try
			{
				if (member.StatusList != null)
				{
					for (var i = 0; i < member.StatusList.Length; i++)
					{
						var status = member.StatusList[i];
						if (status != null && status.CanDispel())
						{
							targetList.Add(member);
							break; // Add only once per member if any status can be dispelled
						}
					}
				}
			}
			catch (NullReferenceException ex)
			{
				PluginLog.Error($"NullReferenceException in AddDispelTargets for member {member?.ToString()}: {ex.Message}");
			}
		}
	}

	private static IBattleChara? GetClosestTarget(List<IBattleChara> targets)
	{
		IBattleChara? closestTarget = null;
		var closestDistance = float.MaxValue;

		foreach (var target in targets)
		{
			var distance = ObjectHelper.DistanceToPlayer(target);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestTarget = target;
			}
		}

		return closestTarget;
	}

	// Recording new entries at 1/second and dequeuing old values to keep only the last DataCenter.HP_RECORD_TIME worth of combat time
	// Has performance implications for keeping too much data for too many targets as they're also all evaluated multiple times a frame for expected TTK
	private static void UpdateTimeToKill()
	{
		var now = DateTime.Now;
		if (now - _lastUpdateTimeToKill < TimeToKillUpdateInterval)
		{
			return;
		}

		_lastUpdateTimeToKill = now;

		var hostiles = DataCenter.AllHostileTargets;
		if (hostiles == null || hostiles.Count == 0)
		{
			return;
		}

		if (DataCenter.RecordedHP.Count >= DataCenter.HP_RECORD_TIME)
		{
			_ = DataCenter.RecordedHP.Dequeue();
		}

		Dictionary<ulong, float> currentHPs = new(hostiles.Count);
		for (var i = 0; i < hostiles.Count; i++)
		{
			var target = hostiles[i];
			if (target != null && target.CurrentHp != 0)
			{
				currentHPs[target.GameObjectId] = target.GetHealthRatio();
			}
		}

		DataCenter.RecordedHP.Enqueue((now, currentHPs));
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

			// Skip mobs whose weakness is already manually curated in StatusHelper.
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
			_ = OtherConfiguration.SaveOccultWeaknessRecords();
		}
	}
}