using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using RotationSolver.Rotations.Ranged.Dancer;
using CombatRole = RotationSolver.Engine.Data.CombatRole;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver.Engine.Actions;

public partial struct ActionTargetInfo
{
	#region TargetFind

	/// <summary>
	/// Finds the target based on the specified type and criteria.
	/// </summary>IBattleChara battleChara
	/// <param name="battleChara"></param>
	/// <param name="type"></param>
	/// <param name="healRatio"></param>
	/// <param name="actionType"></param>
	/// <param name="targetOverride">Overrides the default target type for the action.</param>
	/// <param name="isFriendly">Indicates whether the target is friendly.</param>
	/// <returns></returns>
	public static IBattleChara? FindTargetByType(IEnumerable<IBattleChara> battleChara, TargetType type, float healRatio, SpecialActionType actionType, TargetType targetOverride, bool isFriendly)
	{
		return FindTargetByType(battleChara, type, healRatio, actionType, targetOverride, isFriendly, null);
	}

	private static IBattleChara? FindTargetByType(IEnumerable<IBattleChara> battleChara, TargetType type, float healRatio, SpecialActionType actionType, TargetType targetOverride, bool isFriendly, TargetingType? commandTargeting)
	{
		if (battleChara == null)
		{
			return null;
		}

		if (Player.Object == null)
		{
			return null;
		}

		if (type == TargetType.Self)
		{
			return Player.Object;
		}

		if (targetOverride == TargetType.Self)
		{
			return Player.Object;
		}

		switch (actionType)
		{
			case SpecialActionType.MeleeRangedAttack:
				{
					var filtered = new List<IBattleChara>();
					foreach (var t in battleChara)
					{
						if (t.DistanceToPlayer() >= 3f + (Service.Config?.MeleeRangedOffset ?? 0f))
						{
							filtered.Add(t);
						}
					}
					battleChara = filtered;
				}
				break;

			case SpecialActionType.HostileMovingForward:
				if (Service.Config != null)
				{
					if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward) || Service.CountDownTime > 0)
					{
						if (Svc.Targets.Target is IBattleChara IsHostiletarg && IsHostiletarg.IsHostile())
						{
							return IsHostiletarg;
						}
					}
					if (DataCenter.MergedStatus.HasFlag(AutoStatus.Intercepting))
					{
						if (Svc.Targets.Target is IBattleChara IsHostiletarg && IsHostiletarg.IsHostile())
						{
							return IsHostiletarg;
						}
					}
					else
					{
						var filtered = new List<IBattleChara>();
						foreach (var t in battleChara)
						{
							if (t.IsHostile() && t.DistanceToPlayer() < Service.Config.DistanceForMoving2)
							{
								filtered.Add(t);
							}
						}
						battleChara = filtered;
					}
				}
				break;

			case SpecialActionType.FriendlyMovingForward:
				if (Svc.Targets.FocusTarget is IBattleChara focus && focus.IsParty() && IsInCandidates(battleChara, focus))
				{
					return focus;
				}

				if (Svc.Targets.Target is IBattleChara isPartyTarget && isPartyTarget.IsParty() && IsInCandidates(battleChara, isPartyTarget))
				{
					return isPartyTarget;
				}
				break;

			case SpecialActionType.HostileFriendlyMovingForward:
				if (Svc.Targets.FocusTarget is IBattleChara focusParty && focusParty.IsParty() && IsInCandidates(battleChara, focusParty))
				{
					return focusParty;
				}

				if (Svc.Targets.Target is IBattleChara selectedTarget && (selectedTarget.IsParty() || selectedTarget.IsHostile()) && IsInCandidates(battleChara, selectedTarget))
				{
					return selectedTarget;
				}
				break;

			case SpecialActionType.HostileMovingAttack:
				{
					var filtered = new List<IBattleChara>();
					foreach (var t in battleChara)
					{
						if (ObjectHelper.IsAlive(t))
						{
							filtered.Add(t);
						}
					}
					battleChara = filtered;
				}
				break;

			case SpecialActionType.FixedDistanceMoveForward:
				return Player.Object;

			case SpecialActionType.FixedDistanceMoveBackward:
				return Player.Object;
		}

		var effectiveType = commandTargeting.HasValue ? ToTargetType(commandTargeting.Value)
			: targetOverride == default ? type : targetOverride;

		{
			switch (effectiveType)
			{
				case TargetType.Death:
					if (DataCenter.DeathTarget != null)
					{
						return DataCenter.DeathTarget;
					}

					battleChara = battleChara.GetDeath();
					break;

				case TargetType.Move:
					break;

				case TargetType.FriendMove:
					if (Svc.Targets.FocusTarget is IBattleChara focusFriend && focusFriend.IsParty() && IsInCandidates(battleChara, focusFriend))
					{
						return focusFriend;
					}

					if (Svc.Targets.Target is IBattleChara targ && targ.IsParty() && IsInCandidates(battleChara, targ))
					{
						return targ;
					}
					break;

				default:
					{
						var filtered = new List<IBattleChara>();
						foreach (var t in battleChara)
						{
							if (ObjectHelper.IsAlive(t))
							{
								filtered.Add(t);
							}
						}
						battleChara = filtered;
					}
					break;
			}

			return effectiveType switch
			{
				TargetType.BeAttacked => FindBeAttackedTarget(),
				TargetType.Provoke => FindProvokeTarget(),
				TargetType.Dispel => FindDispelTarget(),
				TargetType.Move => FindTargetForMoving(),
				TargetType.Heal => FindHealTarget(healRatio),
				TargetType.Interrupt => FindInterruptTarget(),
				TargetType.Tank => FindTankTarget(),
				TargetType.Tankbuster => FindTankbusterTarget(),
				TargetType.Melee => battleChara != null ? RandomMeleeTarget(battleChara) : null,
				TargetType.Range => battleChara != null ? RandomRangeTarget(battleChara) : null,
				TargetType.Magical => battleChara != null ? RandomMagicalTarget(battleChara) : null,
				TargetType.Physical => battleChara != null ? RandomPhysicalTarget(battleChara) : null,
				TargetType.DarkCannon => FindDarkCannonTarget(),
				TargetType.ShockCannon => FindShockCannonTarget(),
				TargetType.PhantomBell => FindPhantomBell(),
				TargetType.PhantomRespite => FindPhantomRespite(),
				TargetType.DancePartner => FindDancePartner(),
				TargetType.MimicryTarget => FindMimicryTarget(),
				TargetType.TheSpear => FindTheSpear(),
				TargetType.TheBalance => FindTheBalance(),
				TargetType.Kardia => FindKardia(),
				TargetType.Deployment => FindDeploymentTacticsTarget(),
				_ => isFriendly ? FindFriendly() : FindHostile(),
			};
		}

		IBattleChara? FindFriendly()
		{
			return battleChara == null ? null : PickByTargeting(battleChara, effectiveType);
		}

		IBattleChara? FindDarkCannonTarget()
		{
			if (battleChara != null)
			{
				if (PhantomRotation.CannoneerLevel < 4)
				{
					return FindHostile();
				}
				else
				{
					foreach (var hostile in battleChara)
					{
						if (!hostile.IsOCBlindImmuneTarget() && hostile.InCombat())
						{
							return hostile;
						}
					}
				}
			}
			return null;
		}

		IBattleChara? FindShockCannonTarget()
		{
			if (battleChara != null)
			{
				foreach (var hostile in battleChara)
				{
					if (!hostile.IsOCParalysisImmuneTarget() && hostile.InCombat())
					{
						return hostile;
					}
				}
			}
			return null;
		}

		IBattleChara? FindPhantomBell()
		{
			var partyMembers = DataCenter.PartyMembers;
			var allMembers = new List<IBattleChara>();
			if (DataCenter.AllTargets != null)
			{
				foreach (var x in DataCenter.AllTargets)
				{
					if (x != null && !x.IsEnemy() && !x.HasStatus(false, StatusID.BattleBell))
					{
						allMembers.Add(x);
					}
				}
			}
			if (partyMembers != null && partyMembers.Count > 0)
			{
				var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None, targetOverride, isFriendly);
				var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None, targetOverride, isFriendly);

				if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
					bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
				{
					return bePartyAttackedTarget;
				}
			}

			if (!StatusHelper.PlayerHasStatus(false, StatusID.BattleBell))
			{
				return Player.Object;
			}
			return null;
		}

		IBattleChara? FindPhantomRespite()
		{
			List<IBattleChara> partyMembers = [];
			if (DataCenter.PartyMembers != null)
			{
				foreach (var x in DataCenter.PartyMembers)
				{
					if (x != null && !x.HasStatus(false, StatusID.RingingRespite))
					{
						partyMembers.Add(x);
					}
				}
			}

			var allMembers = new List<IBattleChara>();
			if (DataCenter.AllTargets != null)
			{
				foreach (var x in DataCenter.AllTargets)
				{
					if (x != null && !x.IsEnemy() && !x.HasStatus(false, StatusID.RingingRespite))
					{
						allMembers.Add(x);
					}
				}
			}

			if (partyMembers != null && partyMembers.Count > 0)
			{
				var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None, targetOverride, isFriendly);
				var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None, targetOverride, isFriendly);

				if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
					bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
				{
					return bePartyAttackedTarget;
				}
			}

			return Player.Object;
		}

		IBattleChara? FindDancePartner()
		{
			if (!DataCenter.PlayerAvailable() || !TargetFilter.PlayerIsJobs(Job.DNC)
				|| StatusHelper.PlayerHasStatus(true, StatusID.ClosedPosition) || DancerRotation.IsDancing
				|| DataCenter.PartyMembers.Count < 2)
			{
				return null;
			}

			var priority = OtherConfiguration.DancePartnerPriority;
			var playerId = Player.Object!.GameObjectId;
			var partner = FirstByJobPriority(priority, m => m.GameObjectId != playerId && IsInCandidates(battleChara, m) && !m.HasStatus(false, PoorOrPartneredStatuses))
				?? FirstByJobPriority(priority, m => m.GameObjectId != playerId && IsInCandidates(battleChara, m) && !m.HasStatus(false, PoorTargetStatuses));
			if (partner != null)
			{
				return partner;
			}

			return RandomMeleeTarget(battleChara) ?? RandomRangeTarget(battleChara)
				?? RandomMagicalTarget(battleChara) ?? RandomPhysicalTarget(battleChara);
		}

		IBattleChara? FindTheSpear()
		{
			return FindCardTarget(OtherConfiguration.TheSpearPriority, StatusID.TheSpear_3889, true, battleChara);
		}

		IBattleChara? FindTheBalance()
		{
			return FindCardTarget(OtherConfiguration.TheBalancePriority, StatusID.TheBalance_3887, false, battleChara);
		}

		IBattleChara? FindKardia()
		{
			if (!TargetFilter.PlayerIsJobs(Job.SGE))
			{
				return null;
			}

			if (DataCenter.PartyMembers.Count == 1)
			{
				return IsInCandidates(battleChara, Player.Object) ? Player.Object : null;
			}

			var priority = OtherConfiguration.KardiaTankPriority;
			var tank = FirstByJobPriority(priority, m => IsInCandidates(battleChara, m) && m.IsJobCategory(JobRole.Tank) && m.HasStatus(false, StatusHelper.TankStanceStatus) && !m.HasStatus(false, StatusID.Kardion))
				?? FirstByJobPriority(priority, m => IsInCandidates(battleChara, m) && m.IsJobCategory(JobRole.Tank) && m.HasStatus(false, StatusHelper.TankStanceStatus))
				?? FirstByJobPriority(priority, m => IsInCandidates(battleChara, m) && m.IsJobCategory(JobRole.Tank));
			if (tank != null)
			{
				return tank;
			}

			return FindTankTarget() ?? RandomMeleeTarget(battleChara) ?? RandomPhysicalTarget(battleChara)
				?? RandomRangeTarget(battleChara) ?? RandomMagicalTarget(battleChara);
		}
		IBattleChara? FindDeploymentTacticsTarget()
		{
			if (!TargetFilter.PlayerIsJobs(Job.SCH))
			{
				return null;
			}

			if (DataCenter.PartyMembers == null)
			{
				return null;
			}

			IBattleChara? bestCatalyze = null;
			uint bestCatalyzeShield = 0;

			IBattleChara? bestGalvanize = null;
			uint bestGalvanizeShield = 0;

			foreach (var member in DataCenter.PartyMembers)
			{
				if (member == null || member.IsDead || !IsInCandidates(battleChara, member))
				{
					continue;
				}

				var shield = ObjectHelper.GetObjectShield(member);
				if (shield == 0)
				{
					continue;
				}

				if (member.HasStatus(true, StatusID.Catalyze))
				{
					if (bestCatalyze == null || shield > bestCatalyzeShield)
					{
						bestCatalyze = member;
						bestCatalyzeShield = shield;
					}
				}
				else if (member.HasStatus(true, StatusID.Galvanize))
				{
					if (bestGalvanize == null || shield > bestGalvanizeShield)
					{
						bestGalvanize = member;
						bestGalvanizeShield = shield;
					}
				}
			}

			if (bestCatalyze != null)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"FindDeploymentTacticsTarget: {bestCatalyze.Name} is a valid target with Catalyze, largest shield, and nearby allies.");
				}
				return bestCatalyze;
			}

			if (bestGalvanize != null)
			{
				if (Service.Config.InDebug)
				{
					PluginLog.Debug($"FindDeploymentTacticsTarget: {bestGalvanize.Name} is a valid target with Galvanize, largest shield, and nearby allies.");
				}
				return bestGalvanize;
			}

			return null;
		}

		IBattleChara? FindProvokeTarget()
		{
			if (battleChara != null && DataCenter.ProvokeTarget != null)
			{
				foreach (var o in battleChara)
				{
					if (o.GameObjectId == DataCenter.ProvokeTarget.GameObjectId)
					{
						return DataCenter.ProvokeTarget;
					}
				}
			}
			return null;
		}

		IBattleChara? FindTargetForMoving()
		{
			if (battleChara == null || Player.Object == null)
			{
				return null;
			}

			var pPosition = Player.Object.Position;
			var towardsScreenCenter = Service.Config.MoveTowardsScreenCenter;
			var playerScrPos = Vector2.Zero;
			if (towardsScreenCenter && !Svc.GameGui.WorldToScreen(pPosition, out playerScrPos))
			{
				return null;
			}

			var faceVec = Player.Object.GetFaceVector();
			var halfAngle = Math.PI * Service.Config.MoveTargetAngle / 360;
			IBattleChara? farthest = null;
			var farthestDistance = float.MinValue;
			foreach (var t in battleChara)
			{
				var distance = t.DistanceToPlayer();
				if (distance > Service.Config.DistanceForMoving2 || distance <= farthestDistance)
				{
					continue;
				}

				bool inDirection;
				if (towardsScreenCenter)
				{
					if (!Svc.GameGui.WorldToScreen(t.Position, out var scrPos))
					{
						continue;
					}

					var dir = scrPos - playerScrPos;
					inDirection = dir.Y <= 0 && Math.Abs(dir.X / dir.Y) <= Math.Tan(halfAngle);
				}
				else
				{
					inDirection = Vector3.Dot(faceVec, Vector3.Normalize(t.Position - pPosition)) >= Math.Cos(halfAngle);
				}

				if (inDirection)
				{
					farthest = t;
					farthestDistance = distance;
				}
			}

			return farthest;
		}

		IBattleChara? FindHealTarget(float healRatio)
		{
			if (battleChara == null)
			{
				return null;
			}

			List<IBattleChara> filteredGameObjects = [];
			foreach (var o in battleChara)
			{
				if (!IBaseAction.AutoHealCheck || o.GetHealthRatio() < healRatio)
				{
					filteredGameObjects.Add(o);
				}
			}

			List<IBattleChara> partyMembers = [];
			foreach (var o in filteredGameObjects)
			{
				if (ObjectHelper.IsParty(o))
				{
					partyMembers.Add(o);
				}
			}

			var result = GeneralHealTarget(partyMembers);
			if (result != null)
			{
				return result;
			}

			result = GeneralHealTarget(filteredGameObjects);
			if (result != null)
			{
				return result;
			}

			foreach (var t in partyMembers)
			{
				if (t.HasStatus(false, StatusHelper.TankStanceStatus))
				{
					return t;
				}
			}

			if (partyMembers.Count > 0)
			{
				return partyMembers[0];
			}

			foreach (var t in filteredGameObjects)
			{
				if (t.HasStatus(false, StatusHelper.TankStanceStatus))
				{
					return t;
				}
			}

			if (filteredGameObjects.Count > 0)
			{
				return filteredGameObjects[0];
			}

			return null;

			static IBattleChara? GeneralHealTarget(List<IBattleChara> objs)
			{
				var player = Player.Object;
				IBattleChara? self = null, lowest = null, lowestHealer = null, lowestTank = null;
				float lowestHp = float.MaxValue, healerHp = float.MaxValue, tankHp = float.MaxValue;
				foreach (var o in objs)
				{
					if (!o.CanBeHealed())
					{
						continue;
					}

					if (player != null && o.GameObjectId == player.GameObjectId)
					{
						self = player;
					}

					var hp = o.GetHealthRatio();
					if (hp < lowestHp)
					{
						lowest = o;
						lowestHp = hp;
					}

					if (hp < healerHp && o.IsJobCategory(JobRole.Healer))
					{
						lowestHealer = o;
						healerHp = hp;
					}
					else if (hp < tankHp && o.IsJobCategory(JobRole.Tank))
					{
						lowestTank = o;
						tankHp = hp;
					}
				}

				if (self != null && ObjectHelper.GetPlayerHealthRatio() <= Service.Config.HealthSelfRatio)
				{
					return self;
				}

				if (lowestHealer != null && healerHp <= Service.Config.HealthHealerRatio)
				{
					return lowestHealer;
				}

				if (lowestTank != null && tankHp <= Service.Config.HealthTankRatio)
				{
					return lowestTank;
				}

				return lowest != null && lowestHp < 1 ? lowest : null;
			}
		}

		IBattleChara? FindInterruptTarget()
		{
			if (battleChara == null || DataCenter.InterruptTarget == null)
			{
				return null;
			}

			foreach (var o in battleChara)
			{
				if (o.GameObjectId == DataCenter.InterruptTarget.GameObjectId)
				{
					return DataCenter.InterruptTarget;
				}
			}
			return null;
		}

		IBattleChara? FindHostile()
		{
			if (battleChara == null)
			{
				return null;
			}

			// Manual Any() check
			var hasAny = false;
			foreach (var _ in battleChara)
			{
				hasAny = true;
				break;
			}
			if (!hasAny)
			{
				return null;
			}

			if (Service.Config.FilterStopMark2 && !DataCenter.IsPvP)
			{
				var filteredCharacters = MarkingHelper.FilterStopCharacters(battleChara);
				var filteredHasAny = false;
				if (filteredCharacters != null)
				{
					foreach (var _ in filteredCharacters)
					{
						filteredHasAny = true;
						break;
					}
				}
				if (filteredCharacters != null && filteredHasAny)
				{
					battleChara = filteredCharacters;
				}
			}

			var highPriorityHostiles = new List<IBattleChara>();
			var bestIconPriority = int.MaxValue;
			foreach (var b in battleChara)
			{
				if (!ObjectHelper.IsTopPriorityHostile(b) && !ObjectHelper.IsTopPriorityNamedHostile(b))
				{
					continue;
				}

				var iconPriority = ObjectHelper.GetNamePlateIconPriority(b.GetNamePlateIcon());
				if (iconPriority > bestIconPriority)
				{
					continue;
				}

				if (iconPriority < bestIconPriority)
				{
					bestIconPriority = iconPriority;
					highPriorityHostiles.Clear();
				}

				highPriorityHostiles.Add(b);
			}
			if (highPriorityHostiles.Count > 0)
			{
				battleChara = highPriorityHostiles;
			}

			return FindHostileRaw();
		}

		IBattleChara? FindHostileRaw()
		{
			var sort = commandTargeting.HasValue || IsSortType(effectiveType) ? effectiveType : ToTargetType(DataCenter.TargetingType);
			return battleChara == null ? null : PickByTargeting(battleChara, sort);
		}

		IBattleChara? FindBeAttackedTarget()
		{
			if (battleChara == null)
			{
				return null;
			}

			var hasAny = false;
			foreach (var _ in battleChara)
			{
				hasAny = true;
				break;
			}
			if (!hasAny)
			{
				return null;
			}

			List<IBattleChara> attachedT = [];
			foreach (var t in battleChara)
			{
				if (ObjectHelper.IsTargetOnSelf(t))
				{
					attachedT.Add(t);
				}
			}

			if (!DataCenter.AutoStatus.HasFlag(AutoStatus.DefenseSingle))
			{
				if (attachedT.Count == 0)
				{
					foreach (var tank in battleChara)
					{
						if (tank.HasStatus(false, StatusHelper.TankStanceStatus))
						{
							attachedT.Add(tank);
						}
					}
				}

				if (attachedT.Count == 0)
				{
					foreach (var tank in battleChara.GetJobCategory(JobRole.Tank))
					{
						attachedT.Add(tank);
					}
				}

				if (attachedT.Count == 0)
				{
					foreach (var t in battleChara)
					{
						attachedT.Add(t);
					}
				}
			}

			if (attachedT.Count == 0)
			{
				return null;
			}

			if (Service.Config.Priolowtank)
			{
				IBattleChara? lowest = null;
				var minHealth = float.MaxValue;
				foreach (var t in attachedT)
				{
					var health = ObjectHelper.GetHealthRatio(t);
					if (health < minHealth)
					{
						minHealth = health;
						lowest = t;
					}
				}
				return lowest;
			}
			else
			{
				IBattleChara? highest = null;
				var maxHealth = float.MinValue;
				foreach (var t in attachedT)
				{
					var health = ObjectHelper.GetHealthRatio(t);
					if (health > maxHealth)
					{
						maxHealth = health;
						highest = t;
					}
				}
				return highest;
			}
		}

		IBattleChara? FindDispelTarget()
		{
			if (battleChara != null && DataCenter.DispelTarget != null)
			{
				foreach (var o in battleChara)
				{
					if (o.GameObjectId == DataCenter.DispelTarget.GameObjectId)
					{
						return DataCenter.DispelTarget;
					}
				}
			}
			return null;
		}

		IBattleChara? FindTankTarget()
		{
			if (DataCenter.PartyMembers == null)
			{
				return null;
			}

			var self = Player.Object?.GameObjectId;

			foreach (var m in DataCenter.PartyMembers)
			{
				if (m.IsJobCategory(JobRole.Tank) && !m.IsDead && m.GameObjectId != self && IsInCandidates(battleChara, m))
				{
					if (m.HasStatus(false, StatusHelper.TankStanceStatus))
					{
						if (m.IsConditionCannotTarget())
						{
							continue;
						}

						if (Service.Config.InDebug)
						{
							PluginLog.Debug($"FindTankTarget 1: {m.Name} is a tank with TankStanceStatus.");
						}
						return m;
					}
				}
			}

			foreach (var m in DataCenter.PartyMembers)
			{
				if (m.IsJobCategory(JobRole.Tank) && !m.IsDead && m.GameObjectId != self && IsInCandidates(battleChara, m))
				{
					if (m.IsConditionCannotTarget())
					{
						continue;
					}

					if (Service.Config.InDebug)
					{
						PluginLog.Debug($"FindTankTarget 2: {m.Name} is a tank fallback.");
					}
					return m;
				}
			}

			if (battleChara == null)
			{
				return null;
			}

			List<IBattleChara> others = [];
			foreach (var b in battleChara)
			{
				if (b.GameObjectId != self)
				{
					others.Add(b);
				}
			}

			return RandomPickByJobs(others, JobRole.Tank);
		}

		IBattleChara? FindTankbusterTarget()
		{
			var player = Player.Object;
			if (player == null)
			{
				return null;
			}

			DataCenter.CopyTankbusterTargetIds(_tankbusterIdBuffer);
			IBattleChara? first = null;
			foreach (var id in _tankbusterIdBuffer)
			{
				if (Svc.Objects.SearchById(id) is not IBattleChara t || !t.IsAlive() || t.IsConditionCannotTarget() || !IsInCandidates(battleChara, t))
				{
					continue;
				}

				if (t.GameObjectId == player.GameObjectId)
				{
					return t;
				}

				first ??= t;
			}

			return first;
		}
	}

	private static readonly List<ulong> _tankbusterIdBuffer = [];

	private static bool IsInCandidates(IEnumerable<IBattleChara>? candidates, IBattleChara? target)
	{
		if (candidates == null || target == null)
		{
			return false;
		}

		var targetId = target.GameObjectId;
		foreach (var c in candidates)
		{
			if (c != null && c.GameObjectId == targetId)
			{
				return true;
			}
		}

		return false;
	}

	private static IBattleChara? FindMimicryTarget()
	{
		if (DataCenter.AllTargets == null)
		{
			return null;
		}

		IBattleChara? bestTarget = null;
		var bestDistance = float.MaxValue;

		foreach (var target in DataCenter.AllTargets)
		{
			if (target != null && !target.IsConditionCannotTarget() && IsNeededRole(target))
			{
				var distance = target.DistanceToPlayer();
				if (distance <= 25f && distance < bestDistance && target.CanSee())
				{
					bestDistance = distance;
					bestTarget = target;
				}
			}
		}

		return bestTarget;
	}
	private static IBattleChara? PickByTargeting(IEnumerable<IBattleChara> candidates, TargetType type)
	{
		var role = type switch
		{
			TargetType.PvPHealers => JobRole.Healer,
			TargetType.PvPTanks => JobRole.Tank,
			TargetType.PvPDPS => JobRole.AllDPS,
			_ => JobRole.None,
		};

		if (role != JobRole.None)
		{
			var jobs = role.ToJobs();
			return MinBy(candidates, NearestFirst, c => c.IsJobs(jobs)) ?? MinBy(candidates, NearestFirst);
		}

		return MinBy(candidates, GetTargetingComparison(type));
	}

	private static IBattleChara? MinBy(IEnumerable<IBattleChara> candidates, Comparison<IBattleChara> comparison, Func<IBattleChara, bool>? filter = null)
	{
		IBattleChara? best = null;
		foreach (var candidate in candidates)
		{
			if (candidate != null && (filter == null || filter(candidate)) && (best == null || comparison(candidate, best) < 0))
			{
				best = candidate;
			}
		}

		return best;
	}

	private static readonly Comparison<IBattleChara> NearestFirst = static (a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer());
	private static readonly Comparison<IBattleChara> FarthestFirst = static (a, b) => b.DistanceToPlayer().CompareTo(a.DistanceToPlayer());
	private static readonly Comparison<IBattleChara> HighHpCompare = static (a, b) => b.CurrentHp.CompareTo(a.CurrentHp);
	private static readonly Comparison<IBattleChara> LowHpCompare = static (a, b) => a.CurrentHp.CompareTo(b.CurrentHp);
	private static readonly Comparison<IBattleChara> HighHpPercentCompare = static (a, b) => HpPercent(b).CompareTo(HpPercent(a));
	private static readonly Comparison<IBattleChara> LowHpPercentCompare = static (a, b) => HpPercent(a).CompareTo(HpPercent(b));
	private static readonly Comparison<IBattleChara> HighMaxHpCompare = static (a, b) => b.MaxHp.CompareTo(a.MaxHp);
	private static readonly Comparison<IBattleChara> LowMaxHpCompare = static (a, b) => a.MaxHp.CompareTo(b.MaxHp);
	private static readonly Comparison<IBattleChara> SmallMinHpCompare = static (a, b) => CompareHitboxThenHp(a, b, ascendingHitbox: true, lowerHpFirst: true);
	private static readonly Comparison<IBattleChara> SmallMaxHpCompare = static (a, b) => CompareHitboxThenHp(a, b, ascendingHitbox: true, lowerHpFirst: false);
	private static readonly Comparison<IBattleChara> BigMinHpCompare = static (a, b) => CompareHitboxThenHp(a, b, ascendingHitbox: false, lowerHpFirst: true);
	private static readonly Comparison<IBattleChara> BigMaxHpCompare = static (a, b) => CompareHitboxThenHp(a, b, ascendingHitbox: false, lowerHpFirst: false);

	private static float HpPercent(IBattleChara c)
	{
		return c.MaxHp == 0 ? 0 : (float)c.CurrentHp / c.MaxHp;
	}

	private static int CompareHitboxThenHp(IBattleChara a, IBattleChara b, bool ascendingHitbox, bool lowerHpFirst)
	{
		var cmp = ascendingHitbox ? a.HitboxRadius.CompareTo(b.HitboxRadius) : b.HitboxRadius.CompareTo(a.HitboxRadius);
		if (cmp != 0)
		{
			return cmp;
		}

		return lowerHpFirst ? a.CurrentHp.CompareTo(b.CurrentHp) : b.CurrentHp.CompareTo(a.CurrentHp);
	}

	private static Comparison<IBattleChara> GetTargetingComparison(TargetType type)
	{
		return type switch
		{
			TargetType.Small => Service.Config.SmallHp ? SmallMinHpCompare : SmallMaxHpCompare,
			TargetType.HighHP => HighHpCompare,
			TargetType.LowHP => LowHpCompare,
			TargetType.HighHPPercent => HighHpPercentCompare,
			TargetType.LowHPPercent => LowHpPercentCompare,
			TargetType.HighMaxHP => HighMaxHpCompare,
			TargetType.LowMaxHP => LowMaxHpCompare,
			TargetType.Nearest => NearestFirst,
			TargetType.Farthest => FarthestFirst,
			_ => Service.Config.BigHp ? BigMinHpCompare : BigMaxHpCompare,
		};
	}

	private static bool IsSortType(TargetType type) => type is TargetType.Small or TargetType.HighHP or TargetType.LowHP
		or TargetType.HighHPPercent or TargetType.LowHPPercent or TargetType.HighMaxHP or TargetType.LowMaxHP
		or TargetType.Nearest or TargetType.Farthest or TargetType.PvPHealers or TargetType.PvPTanks or TargetType.PvPDPS;

	private static TargetType ToTargetType(TargetingType targetingType) => targetingType switch
	{
		TargetingType.Small => TargetType.Small,
		TargetingType.HighHP => TargetType.HighHP,
		TargetingType.LowHP => TargetType.LowHP,
		TargetingType.HighHPPercent => TargetType.HighHPPercent,
		TargetingType.LowHPPercent => TargetType.LowHPPercent,
		TargetingType.HighMaxHP => TargetType.HighMaxHP,
		TargetingType.LowMaxHP => TargetType.LowMaxHP,
		TargetingType.Nearest => TargetType.Nearest,
		TargetingType.Farthest => TargetType.Farthest,
		TargetingType.PvPHealers => TargetType.PvPHealers,
		TargetingType.PvPTanks => TargetType.PvPTanks,
		TargetingType.PvPDPS => TargetType.PvPDPS,
		_ => TargetType.Big,
	};

	private static bool IsNeededRole(IBattleChara character)
	{
		if (Player.Object == null)
		{
			return false;
		}

		if (character.GameObjectId == Player.Object.GameObjectId)
		{
			return false;
		}

		if (character is not IPlayerCharacter player || player.ClassJob.Value.IsLimitedJob)
		{
			return false;
		}

		var neededRole = DataCenter.BluRole;
		return neededRole != null && neededRole != CombatRole.NonCombat && (int)neededRole == (int)player.GetRole();
	}

	private static readonly StatusID[] PoorTargetStatuses = [StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath];
	private static readonly StatusID[] PoorOrPartneredStatuses = [.. PoorTargetStatuses, StatusID.DancePartner, StatusID.ClosedPosition];

	private static IBattleChara? FirstByJobPriority(List<Job> priority, Func<IBattleChara, bool> predicate)
	{
		foreach (var job in priority)
		{
			foreach (var member in DataCenter.PartyMembers)
			{
				if (member.IsJobs(job) && !member.IsDead && !member.IsConditionCannotTarget() && predicate(member))
				{
					return member;
				}
			}
		}

		return null;
	}

	private static IBattleChara? FindCardTarget(List<Job> priority, StatusID cardStatus, bool preferRanged, IEnumerable<IBattleChara>? validCandidates)
	{
		var player = Player.Object;
		if (!TargetFilter.PlayerIsJobs(Job.AST) || player == null)
		{
			return null;
		}

		var party = DataCenter.PartyMembers;
		var playerId = player.GameObjectId;

		if (party.Count == 1 || (party.Count == 2 && party.Exists(m => m.GameObjectId == playerId) && party.Exists(ObjectHelper.IsPlayerCharacterChocobo)))
		{
			return IsInCandidates(validCandidates, player) ? player : null;
		}

		List<IBattleChara> candidates = [];
		foreach (var m in party)
		{
			if (m == null || m.GameObjectId == playerId || m.IsPlayerCharacterChocobo() || m.IsDead || m.IsConditionCannotTarget()
				|| !IsInCandidates(validCandidates, m)
				|| m.HasStatus(false, PoorTargetStatuses) || m.HasStatus(false, cardStatus))
			{
				continue;
			}

			candidates.Add(m);
		}

		IBattleChara? best = null;
		var bestIndex = int.MaxValue;
		foreach (var member in candidates)
		{
			var index = priority.FindIndex(job => member.IsJobs(job));
			if (index < 0)
			{
				continue;
			}

			if (best == null || index < bestIndex
				|| (index == bestIndex && (member.MaxHp > best.MaxHp || (member.MaxHp == best.MaxHp && member.CurrentHp > best.CurrentHp))))
			{
				best = member;
				bestIndex = index;
			}
		}

		if (best != null)
		{
			return best;
		}

		return preferRanged
			? RandomRangeTarget(candidates) ?? RandomMeleeTarget(candidates) ?? RandomMagicalTarget(candidates) ?? RandomPhysicalTarget(candidates)
			: RandomMeleeTarget(candidates) ?? RandomRangeTarget(candidates) ?? RandomMagicalTarget(candidates) ?? RandomPhysicalTarget(candidates);
	}
	internal static IBattleChara? RandomPhysicalTarget(IEnumerable<IBattleChara> tars)
	{
		return RandomPickByJobs(tars, Job.VPR, Job.WAR, Job.GNB, Job.MNK, Job.SAM, Job.DRG, Job.MCH, Job.DNC)
			?? RandomPickByJobs(tars, Job.PLD, Job.DRK, Job.NIN, Job.BRD, Job.RDM)
			?? RandomObject(tars);
	}

	internal static IBattleChara? RandomMagicalTarget(IEnumerable<IBattleChara> tars)
	{
		return RandomPickByJobs(tars, Job.PCT, Job.SCH, Job.AST, Job.SGE, Job.BLM, Job.SMN)
			?? RandomPickByJobs(tars, Job.PLD, Job.DRK, Job.NIN, Job.BRD, Job.RDM)
			?? RandomObject(tars);
	}

	internal static IBattleChara? RandomRangeTarget(IEnumerable<IBattleChara> tars)
	{
		return RandomPickByJobs(tars, JobRole.RangedMagical, JobRole.RangedPhysical, JobRole.Melee)
			?? RandomPickByJobs(tars, JobRole.Tank, JobRole.Healer)
			?? RandomObject(tars);
	}

	internal static IBattleChara? RandomMeleeTarget(IEnumerable<IBattleChara> tars)
	{
		return RandomPickByJobs(tars, JobRole.Melee, JobRole.RangedMagical, JobRole.RangedPhysical)
			?? RandomPickByJobs(tars, JobRole.Tank, JobRole.Healer)
			?? RandomObject(tars);
	}

	private static IBattleChara? RandomPickByJobs(IEnumerable<IBattleChara> tars, params JobRole[] roles)
	{
		foreach (var role in roles)
		{
			var tar = RandomPickByJobs(tars, role.ToJobs());
			if (tar != null)
			{
				if (tar.IsConditionCannotTarget())
				{
					continue;
				}

				return tar;
			}
		}
		return null;
	}

	private static IBattleChara? RandomPickByJobs(IEnumerable<IBattleChara> tars, params Job[] jobs)
	{
		List<IBattleChara> targets = [];
		foreach (var t in tars)
		{
			if (t.IsConditionCannotTarget())
			{
				continue;
			}

			if (t.IsJobs(jobs))
			{
				targets.Add(t);
			}
		}
		return targets.Count > 0 ? RandomObject(targets) : null;
	}

	private static IBattleChara? RandomObject(IEnumerable<IBattleChara> objs)
	{
		List<IBattleChara> list = [];
		foreach (var o in objs)
		{
			if (o == null)
			{
				continue;
			}

			if (o.IsConditionCannotTarget())
			{
				continue;
			}

			list.Add(o);
		}

		var count = list.Count;
		if (count == 0)
		{
			return null;
		}

		var index = Random.Shared.Next(count);
		return list[index];
	}

	#endregion
}
