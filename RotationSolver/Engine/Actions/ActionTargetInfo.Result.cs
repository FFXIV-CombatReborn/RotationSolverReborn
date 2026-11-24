using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using static RotationSolver.Configuration.ConfigTypes;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver.Engine.Actions;

public partial struct ActionTargetInfo
{
	#region Target Result

	internal readonly TargetResult? FindTarget(bool skipAoeCheck, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType targetOverride, Func<IBattleChara, bool>? targetFilter = null)
	{
		if (action == null || action.Setting == null || action.Config == null)
		{
			return null;
		}

		if (Player.Object == null)
		{
			return null;
		}

		if (Range == 0 && EffectRange == 0)
		{
			return new TargetResult(Player.Object, [], Player.Object.Position);
		}

		if (action.Setting.SpecialType == SpecialActionType.ObjectBasedMovement)
		{
			if (ShouldCheckMoveSafety() && action.Setting.ObjectBasedMovementObjectOID != 0)
			{
				if (!ObjectHelper.TryGetOwnedObjectPosition(action.Setting.ObjectBasedMovementObjectOID, out var position))
				{
					return null;
				}

				if (!CheckMovementSafety(position))
				{
					return null;
				}

				return new TargetResult(Player.Object, [], position);
			}
		}

		var type = action.Setting.TargetType;

		if (Range == 0 && EffectRange > 0 && !IsSingleTarget && !IsTargetArea && !action.Setting.IsFriendly)
		{
			if (Service.Config.AoEType == AoEType.Off)
			{
				return null;
			}

			var required = skipAoeCheck ? 0 : action.Config.AoeCount;

			if (required > 1 && (Service.Config.AoEType == AoEType.Cleave || (DataCenter.IsInM9S && Service.Config.M9SCleaveOnly)))
			{
				return null;
			}

			var selfAffects = GetCanAffects(skipStatusProvideCheck, skipTargetStatusNeedCheck, type, targetOverride);
			selfAffects = FilterAffectsByFacing(selfAffects);
			if (selfAffects.Count < Math.Max(1, required))
			{
				return null;
			}

			return new TargetResult(Player.Object, [.. selfAffects], Player.Object.Position);
		}

		IEnumerable<IBattleChara> canTargets = GetCanTargets(skipStatusProvideCheck, skipTargetStatusNeedCheck, type, targetOverride, targetFilter);
		var canAffects = GetCanAffects(skipStatusProvideCheck, skipTargetStatusNeedCheck, type, targetOverride);

		if (canTargets == null || canAffects == null)
		{
			return null;
		}

		if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off && !IsSingleTarget)
		{
			return null;
		}

		var commandTarget = DataCenter.GetCommandTarget(action);
		if (commandTarget is { ObjectId: not 0 } objectTarget
			&& FindCommandObjectTarget(objectTarget.ObjectId, canAffects, skipStatusProvideCheck, skipTargetStatusNeedCheck, targetFilter) is { } commandResult)
		{
			return commandResult;
		}

		if (IsTargetArea)
		{
			return FindTargetArea(canTargets, canAffects, Range, Player.Object, targetOverride);
		}

		List<IBattleChara> targetsList = [.. GetMostCanTargetObjects(canTargets, canAffects, skipAoeCheck ? 0 : action.Config.AoeCount)];

		var target = FindTargetByType(targetsList, type, action.Config.AutoHealRatio, action.Setting.SpecialType, targetOverride, action.Setting.IsFriendly, commandTarget?.TargetingType);
		if (target == null)
		{
			return null;
		}

		if (ShouldCheckMoveSafety() && !CheckMovementSafety(target.Position, target))
		{
			return null;
		}

		return new TargetResult(target, [.. GetAffectsTarget(target, canAffects)], target.Position);
	}

	private readonly TargetResult? FindCommandObjectTarget(ulong objectId, IEnumerable<IBattleChara> canAffects, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, Func<IBattleChara, bool>? targetFilter = null)
	{
		if (Player.Object == null || Svc.Objects.SearchById(objectId) is not IBattleChara target)
		{
			return null;
		}

		if (IsTargetArea)
		{
			if (Vector3.Distance(Player.Object.Position, target.Position) > Range
				|| !TryGetGroundPosition(target.Position, Range, Player.Object, out var position)
				|| (ShouldCheckMoveSafety() && !CheckMovementSafety(position)))
			{
				return null;
			}

			return new TargetResult(target, [.. GetAffectsVector(position, canAffects)], position);
		}

		if (target.DistanceToPlayer() > Range
			|| !IsInPvPPlayerRange(target)
			|| !CanUseTo(target)
			|| !CheckStatus(target, skipStatusProvideCheck, skipTargetStatusNeedCheck)
			|| !action.Setting.CanTarget(target)
			|| (targetFilter != null && !targetFilter(target))
			|| (ShouldCheckMoveSafety() && !CheckMovementSafety(target.Position, target)))
		{
			return null;
		}

		return new TargetResult(target, [.. GetAffectsTarget(target, canAffects)], target.Position);
	}

	private readonly TargetResult? FindTargetArea(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects,
	float range, IPlayerCharacter player, TargetType targetOverride)
	{
		if (player == null || canTargets == null || canAffects == null)
		{
			return null;
		}

		if (action.Setting.TargetType == TargetType.Move)
		{
			return FindTargetAreaMove(range, targetOverride);
		}
		else
		{
			if (action.Setting.IsFriendly)
			{
				if (!Service.Config.UseGroundBeneficialAbility)
				{
					return null;
				}
				else
				{
					if (!Service.Config.UseGroundBeneficialAbilityWhenMoving && DataCenter.IsMoving)
					{
						return null;
					}
					else
					{
						return FindTargetAreaFriend(range, canAffects, player, targetOverride);
					}
				}
			}
			else
			{
				return FindTargetAreaHostile(canTargets, canAffects, action.Config.AoeCount);
			}
		}
	}

	private readonly TargetResult? FindTargetAreaHostile(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
	{
		if (canAffects == null || canTargets == null)
		{
			return null;
		}

		IBattleChara? target = null;
		var targetHealthRatio = 0f;
		var mostCanTargetObjects = GetMostCanTargetObjects(canTargets, canAffects, aoeCount);
		using var enumerator = mostCanTargetObjects.GetEnumerator();

		while (enumerator.MoveNext())
		{
			var t = enumerator.Current;
			var tHealthRatio = ObjectHelper.GetHealthRatio(t);
			if (target == null || tHealthRatio > targetHealthRatio)
			{
				target = t;
				targetHealthRatio = tHealthRatio;
			}
		}

		if (target == null)
		{
			return null;
		}

		List<IBattleChara> affectedTargets = [];
		foreach (var t in canAffects)
		{
			if (Vector3.Distance(target.Position, t.Position) - t.HitboxRadius <= EffectRange)
			{
				affectedTargets.Add(t);
			}
		}

		var affectedTargetsArray = new IBattleChara[affectedTargets.Count];
		for (var i = 0; i < affectedTargets.Count; i++)
		{
			affectedTargetsArray[i] = affectedTargets[i];
		}
		return new TargetResult(target, affectedTargetsArray, target.Position);
	}

	private readonly TargetResult? FindTargetAreaMove(float range, TargetType targetOverride)
	{
		if (Player.Object == null)
		{
			return null;
		}

		if (Service.Config.MoveAreaActionFarthest)
		{
			var pPosition = Player.Object.Position;
			if (Service.Config.MoveTowardsScreenCenter)
			{
				unsafe
				{
					var cameraManager = CameraManager.Instance();
					if (cameraManager == null)
					{
						return null;
					}

					var camera = cameraManager->CurrentCamera;
					var tar = camera->LookAtVector - camera->Object.Position;
					tar.Y = 0;
					var length = ((Vector3)tar).Length();
					if (length == 0)
					{
						return null;
					}

					tar = tar / length * range;
					var dest1 = new Vector3(pPosition.X + tar.X, pPosition.Y, pPosition.Z + tar.Z);
					if (ShouldCheckMoveSafety() && !CheckMovementSafety(dest1))
					{
						return null;
					}

					return new TargetResult(Player.Object, [], dest1);
				}
			}
			else
			{
				var rotation = Player.Object.Rotation;
				var dest2 = new Vector3(pPosition.X + ((float)Math.Sin(rotation) * range), pPosition.Y, pPosition.Z + ((float)Math.Cos(rotation) * range));
				if (ShouldCheckMoveSafety() && !CheckMovementSafety(dest2))
				{
					return null;
				}

				return new TargetResult(Player.Object, [], dest2);
			}
		}
		else
		{
			List<IBattleChara> availableCharas = [];
			foreach (var availableTarget in DataCenter.AllTargets)
			{
				if (availableTarget.GameObjectId != Player.Object.GameObjectId)
				{
					availableCharas.Add(availableTarget);
				}
			}

			var targetList = TargetFilter.GetObjectInRadius(availableCharas, range);
			var target = FindTargetByType(targetList, TargetType.Move, action.Config.AutoHealRatio, action.Setting.SpecialType, targetOverride, true);
			if (target == null)
			{
				return null;
			}

			if (ShouldCheckMoveSafety() && !CheckMovementSafety(target.Position))
			{
				return null;
			}

			return new TargetResult(target, [], target.Position);
		}
	}

	/// <summary>
	/// Determines whether the movement safety check should be performed for the current action.
	/// </summary>
	/// <returns><c>true</c> if the movement safety check should be performed; otherwise, <c>false</c>.</returns>
	public readonly bool ShouldCheckMoveSafety()
		=> (IsMovingSpecialType(action.Setting.SpecialType))
		   && !action.Config.SkipPositionSafetyCheck && Service.Config.BmrSafetyCheckAuto;

	private static bool IsMovingSpecialType(SpecialActionType type)
		=> type is SpecialActionType.FixedDistanceMoveForward
				or SpecialActionType.FixedDistanceMoveBackward
				or SpecialActionType.HostileMovingForward
				or SpecialActionType.FriendlyMovingForward
				or SpecialActionType.HostileFriendlyMovingForward
				or SpecialActionType.HostileMovingAttack
				or SpecialActionType.ObjectBasedMovement;

	private readonly bool CheckMovementSafety(Vector3 destination, IBattleChara? target = null)
	{
		if (Player.Object == null)
		{
			return false;
		}

		var playerPos = Player.Object.Position;
		var specialType = action.Setting.SpecialType;

		switch (specialType)
		{
			case SpecialActionType.FixedDistanceMoveForward:
			case SpecialActionType.FixedDistanceMoveBackward:
				return DataCenter.IsFixedDashSafe(action.Setting.FixedMoveDistance, specialType == SpecialActionType.FixedDistanceMoveBackward);

			case SpecialActionType.HostileMovingForward:
			case SpecialActionType.FriendlyMovingForward:
			case SpecialActionType.HostileFriendlyMovingForward:
			case SpecialActionType.HostileMovingAttack:
				if (target != null)
				{
					if (target.DistanceToPlayer() <= 0)
					{
						return true;
					}

					var toTarget = target.Position - playerPos;
					var distance = toTarget.Length();
					if (distance > target.HitboxRadius)
					{
						var direction = toTarget / distance;
						var finalDestination = target.Position - direction * target.HitboxRadius;
						return DataCenter.IsDashSafe(playerPos, finalDestination);
					}
					return DataCenter.IsDashSafe(playerPos, target.Position);
				}
				return false;

			case SpecialActionType.ObjectBasedMovement:
				return DataCenter.IsDashSafe(playerPos, destination);

			default:
				return true;
		}
	}

	private const float GroundRangeMargin = 0.5f;

	// TODO: maybe make this a config 
	private const long GroundRejectedFallbackMs = 5000;

	private readonly TargetResult? FindTargetAreaFriend(float range, IEnumerable<IBattleChara> canAffects, IPlayerCharacter player, TargetType targetOverride)
	{
		if (canAffects == null)
		{
			return null;
		}

		if (range == 0 || Service.Config.UseGroundBeneficialAbilityOnlySelf
			|| DataCenter.WasActionRejectedRecently(action.Info.ID, GroundRejectedFallbackMs))
		{
			return new TargetResult(player, [.. GetAffectsVector(player.Position, canAffects)], player.Position);
		}

		if (Service.Config.UseTargetTankForGroundHeal && FindTargetAreaTank(range, canAffects, player) is { } tankResult)
		{
			return tankResult;
		}

		if (FindBeneficialPosition(range, player) is { } desired && TryGetGroundPosition(desired, range, player, out var preset))
		{
			List<IBattleChara> affectsList = [.. GetAffectsVector(preset, canAffects)];
			if (affectsList.Count > 0)
			{
				return new TargetResult(player, [.. affectsList], preset);
			}
		}

		IBattleChara? bossTarget = null;
		if (Svc.Targets.Target is IBattleChara b && b.DistanceToPlayer() <= range && b.IsBossFromIcon()
			&& (b.HasPositional() || b.HitboxRadius <= 8))
		{
			bossTarget = b;
		}

		if (bossTarget == null && DataCenter.AllHostileTargets != null)
		{
			foreach (var hostile in DataCenter.AllHostileTargets)
			{
				if (hostile != null && hostile.IsBossFromIcon() && hostile.DistanceToPlayer() <= range
					&& (hostile.HasPositional() || hostile.HitboxRadius <= 8))
				{
					bossTarget = hostile;
					break;
				}
			}
		}

		if (bossTarget != null && TryGetGroundPosition(bossTarget.Position, range, player, out var bossPos))
		{
			return new TargetResult(player, [.. GetAffectsVector(bossPos, canAffects)], bossPos);
		}

		var effectRange = EffectRange;
		List<IBattleChara> partyMembersInRadius = [];
		if (DataCenter.PartyMembers != null)
		{
			foreach (var member in DataCenter.PartyMembers)
			{
				if (member != null && Vector3.Distance(member.Position, player.Position) <= range + effectRange)
				{
					partyMembersInRadius.Add(member);
				}
			}
		}

		var attackT = FindTargetByType(partyMembersInRadius,
			TargetType.BeAttacked, action.Config.AutoHealRatio, action.Setting.SpecialType, targetOverride, true);

		if (attackT != null)
		{
			var disToTankRound = Vector3.Distance(player.Position, attackT.Position) + attackT.HitboxRadius;
			var directionToTank = attackT.Position - player.Position;
			var dirLength = directionToTank.Length();
			if (disToTankRound >= effectRange && disToTankRound <= (2 * effectRange) - player.HitboxRadius && dirLength >= 0.001f)
			{
				var finalPos = player.Position + (directionToTank / dirLength * Math.Max(0, disToTankRound - effectRange));
				if (TryGetGroundPosition(finalPos, range, player, out var placed))
				{
					return new TargetResult(player, [.. GetAffectsVector(placed, canAffects)], placed);
				}
			}
		}

		return new TargetResult(player, [.. GetAffectsVector(player.Position, canAffects)], player.Position);
	}

	private readonly TargetResult? FindTargetAreaTank(float range, IEnumerable<IBattleChara> canAffects, IPlayerCharacter player)
	{
		if (DataCenter.PartyMembers == null)
		{
			return null;
		}

		for (var pass = 0; pass < 2; pass++)
		{
			var wantStance = pass == 0;
			foreach (var member in DataCenter.PartyMembers)
			{
				if (member == null || member.GameObjectId == player.GameObjectId || member.IsDead
					|| !member.IsJobCategory(JobRole.Tank) || member.HasStatus(false, StatusHelper.TankStanceStatus) != wantStance)
				{
					continue;
				}

				if (TryGetGroundPosition(member.Position, range, player, out var pos)
					&& Vector3.Distance(pos, member.Position) - member.HitboxRadius <= EffectRange)
				{
					return new TargetResult(member, [.. GetAffectsVector(pos, canAffects)], pos);
				}
			}
		}

		return null;
	}

	private readonly Vector3? FindBeneficialPosition(float range, IPlayerCharacter player)
	{
		var playerPos = player.Position;
		if (!OtherConfiguration.BeneficialPositions.TryGetValue(Svc.ClientState.TerritoryType, out var pts) || pts == null || pts.Length == 0)
		{
			if (!UseArenaCenterFallback())
			{
				return null;
			}

			pts = [new Vector3(0, playerPos.Y, 0), new Vector3(100, playerPos.Y, 100)];
		}

		var closest = pts[0];
		var minDistance = Vector3.Distance(playerPos, closest);
		for (var i = 1; i < pts.Length; i++)
		{
			var distance = Vector3.Distance(playerPos, pts[i]);
			if (distance < minDistance)
			{
				closest = pts[i];
				minDistance = distance;
			}
		}

		if (FlatDistance(playerPos, closest) > range + EffectRange)
		{
			return null;
		}

		var rotation = Random.Shared.NextDouble() * Math.Tau;
		var jitterRadius = (float)(Random.Shared.NextDouble() * MathF.Min(EffectRange * 0.5f, 2f));
		closest.X += (float)(Math.Sin(rotation) * jitterRadius);
		closest.Z += (float)(Math.Cos(rotation) * jitterRadius);
		return closest;
	}

	private static bool UseArenaCenterFallback()
	{
		var contentType = DataCenter.Territory?.ContentType;
		if (contentType == TerritoryContentType.Trials)
		{
			return true;
		}

		if (contentType != TerritoryContentType.Raids || DataCenter.PartyMembers == null)
		{
			return false;
		}

		var partyCount = 0;
		foreach (var p in DataCenter.PartyMembers)
		{
			if (p is IPlayerCharacter)
			{
				partyCount++;
			}
		}

		return partyCount >= 8;
	}

	private static float FlatDistance(Vector3 a, Vector3 b)
	{
		var dx = b.X - a.X;
		var dz = b.Z - a.Z;
		return MathF.Sqrt((dx * dx) + (dz * dz));
	}

	private static bool TryGetGroundPosition(Vector3 desired, float range, IPlayerCharacter player, out Vector3 position)
	{
		var playerPos = player.Position;
		var maxDistance = MathF.Max(0, range - GroundRangeMargin);
		var flatDistance = FlatDistance(playerPos, desired);
		if (flatDistance > maxDistance)
		{
			var scale = maxDistance / flatDistance;
			desired = playerPos + ((desired - playerPos) * scale);
		}

		position = ObjectHelper.SnapToGround(desired);
		return float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z)
			&& MathF.Abs(position.Y - playerPos.Y) <= range
			&& ObjectHelper.CanSeePosition(position);
	}

	private readonly IEnumerable<IBattleChara> GetAffectsVector(Vector3? point, IEnumerable<IBattleChara> canAffects)
	{
		if (canAffects == null)
		{
			yield break;
		}

		if (point == null)
		{
			yield break;
		}

		foreach (var t in canAffects)
		{
			if (Vector3.Distance(point.Value, t.Position) - t.HitboxRadius <= EffectRange)
			{
				yield return t;
			}
		}
	}

	private readonly List<IBattleChara> FilterAffectsByFacing(List<IBattleChara> candidates)
	{
		if (Player.Object == null || candidates.Count == 0)
		{
			return candidates;
		}

		var castType = (CastType)action.Action.CastType;
		if (castType != CastType.Cone && castType != CastType.StraightLine && castType != CastType.Donut)
		{
			return candidates;
		}

		var playerPos = Player.Object.Position;
		var faceVec = Vector3.Normalize(Player.Object.GetFaceVector());
		faceVec.Y = 0;

		List<IBattleChara> result = [];
		foreach (var t in candidates)
		{
			var toTarget = t.Position - playerPos;
			toTarget.Y = 0;
			var dist = toTarget.Length();
			if (dist <= 0.001f)
			{
				result.Add(t);
				continue;
			}

			if (castType == CastType.Cone)
			{
				var cos = Vector3.Dot(faceVec, toTarget / dist);
				if (cos >= (float)CosAlpha)
				{
					result.Add(t);
				}
			}
			else if (castType == CastType.StraightLine)
			{
				if (Vector3.Dot(faceVec, toTarget) < 0)
				{
					continue;
				}

				var faceXZ = Vector3.Normalize(new Vector3(faceVec.X, 0, faceVec.Z));
				Vector3 toTargetXZ = new(toTarget.X, 0, toTarget.Z);
				var perp = Vector3.Cross(faceXZ, toTargetXZ).Length();
				var halfWidth = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier / 2f : 2f;
				var width = halfWidth + t.HitboxRadius;
				if (perp <= width)
				{
					result.Add(t);
				}
			}
			else if (castType == CastType.Donut)
			{
				var innerRadius = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier : 8f;
				var d = dist - t.HitboxRadius;
				if (d >= innerRadius)
				{
					result.Add(t);
				}
			}
		}
		return result;
	}

	private readonly IEnumerable<IBattleChara> GetAffectsTarget(IBattleChara tar, IEnumerable<IBattleChara> canAffects)
	{
		if (tar == null)
		{
			yield break;
		}

		if (canAffects == null)
		{
			yield break;
		}

		foreach (var t in canAffects)
		{
			if (GetCanTarget(tar, t))
			{
				yield return t;
			}
		}
	}

	#endregion
}
