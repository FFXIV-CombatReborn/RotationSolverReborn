using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using static RotationSolver.Basic.Configuration.ConfigTypes;
using AttackType = RotationSolver.Basic.Data.AttackType;
using CombatRole = RotationSolver.Basic.Data.CombatRole;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The target info
/// </summary>
/// <param name="action">the input action.</param>
public struct ActionTargetInfo(IBaseAction action)
{
    /// <summary>
    /// The range of this action.
    /// </summary>
    public readonly float Range => ActionManager.GetActionRange(action.Info.ID);

    /// <summary>
    /// The effect range of this action.
    /// </summary>
    public readonly float EffectRange => (ActionID)action.Info.ID == ActionID.LiturgyOfTheBellPvE ? 20 : action.Action.EffectRange;

    /// <summary>
    /// Is this action single target.
    /// </summary>
    public readonly bool IsSingleTarget => action.Action.CastType == 1;
    /// <summary>
    /// Is this action target area.
    /// </summary>
    public readonly bool IsTargetArea => action.Action.TargetArea;

    /// <summary>
    /// Is this action friendly.
    /// </summary>
    public readonly bool IsTargetFriendly => action.Setting.IsFriendly;

    #region Targetting Behaviour

    /// <summary>
    /// Retrieves a collection of valid battle characters that can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private IEnumerable<IBattleChara> GetCanTargets(bool skipStatusProvideCheck, TargetType type)
    {
        var allTargets = DataCenter.AllTargets;
        if (allTargets == null) return Enumerable.Empty<IBattleChara>();

        var filteredTargets = TargetFilter.GetObjectInRadius(allTargets, Range);
        var validTargets = new List<IBattleChara>(filteredTargets.Count());

        foreach (var target in filteredTargets)
        {
            if (type == TargetType.Heal && target.GetHealthRatio() == 1) continue;
            if (!GeneralCheck(target, skipStatusProvideCheck)) continue;
            validTargets.Add(target);
        }

        var isAuto = !DataCenter.IsManual || IsTargetFriendly;
        var playerObjectId = Player.Object?.GameObjectId;
        var targetObjectId = Svc.Targets.Target?.GameObjectId;

        var result = new List<IBattleChara>();
        foreach (var b in validTargets)
        {
            if (isAuto || b.GameObjectId == targetObjectId || b.GameObjectId == playerObjectId)
            {
                if (InViewTarget(b) && CanUseTo(b) && action.Setting.CanTarget(b))
                {
                    result.Add(b);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves a list of battle characters that can be affected based on the specified criteria.
    /// </summary>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// A <see cref="List{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private List<IBattleChara> GetCanAffects(bool skipStatusProvideCheck, TargetType type)
    {
        if (EffectRange == 0) return new List<IBattleChara>();

        var targets = action.Setting.IsFriendly
            ? DataCenter.PartyMembers
            : DataCenter.AllHostileTargets;

        if (targets == null) return new List<IBattleChara>();

        var items = TargetFilter.GetObjectInRadius(targets, Range + EffectRange);

        if (type == TargetType.Heal)
        {
            var filteredItems = new List<IBattleChara>();
            foreach (var i in items)
            {
                if (i.GetHealthRatio() < 1)
                {
                    filteredItems.Add(i);
                }
            }
            items = filteredItems;
        }

        var validTargets = new List<IBattleChara>(items.Count());

        foreach (var obj in items)
        {
            if (!GeneralCheck(obj, skipStatusProvideCheck)) continue;
            validTargets.Add(obj);
        }

        return validTargets;
    }

    /// <summary>
    /// Determines whether the specified battle character is within the player's view and vision cone based on the configuration settings.
    /// </summary>
    /// <param name="gameObject">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character is within the player's view and vision cone; otherwise, <c>false</c>.
    /// </returns>
    private static bool InViewTarget(IBattleChara gameObject)
    {
        if (Service.Config.OnlyAttackInView)
        {
            if (!Svc.GameGui.WorldToScreen(gameObject.Position, out _)) return false;
        }

        if (Service.Config.OnlyAttackInVisionCone && Player.Object != null)
        {
            Vector3 dir = gameObject.Position - Player.Object.Position;
            Vector3 faceVec = Player.Object.GetFaceVector();
            dir = Vector3.Normalize(dir);
            faceVec = Vector3.Normalize(faceVec);

            // Calculate the angle between the direction vector and the facing vector
            double dotProduct = Vector3.Dot(faceVec, dir);
            double angle = Math.Acos(dotProduct);

            if (angle > Math.PI * Service.Config.AngleOfVisionCone / 360)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified game object can be targeted and used for an action.
    /// </summary>
    /// <param name="tar">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object can be targeted and used for an action; otherwise, <c>false</c>.
    /// </returns>
    private unsafe bool CanUseTo(IGameObject tar)
    {
        if (tar == null || !Player.Available || tar.GameObjectId == 0) return false;

        var tarAddress = tar.Struct();
        if (tarAddress == null) return false;

        if (!IsSpecialAbility(action.Info.ID) && !ActionManager.CanUseActionOnTarget(action.Info.AdjustedID, tarAddress)) return false;

        return tar.CanSee();
    }

    private List<ActionID> _specialActions = new List<ActionID>()
    {
        ActionID.AethericMimicryPvE,
        ActionID.EruptionPvE,
        ActionID.BishopAutoturretPvP,
        ActionID.FeatherRainPvE,
    };

    private bool IsSpecialAbility(uint iD)
    {
        return _specialActions.Contains((ActionID)iD);
    }

    /// <summary>
    /// Performs a general check on the specified battle character to determine if it meets the criteria for targeting.
    /// </summary>
    /// <param name="gameObject">The battle character to check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <returns>
    /// <c>true</c> if the battle character meets the criteria for targeting; otherwise, <c>false</c>.
    /// </returns>
    public bool GeneralCheck(IBattleChara gameObject, bool skipStatusProvideCheck)
    {
        if (!gameObject.IsTargetable) return false;

        if (Service.Config.RaiseType == RaiseType.PartyOnly && gameObject.IsAllianceMember() && !gameObject.IsParty())
        {
            return false;
        }

        if (DataCenter.BlacklistedNameIds.Contains(gameObject.NameId))
        {
            return false;
        }

        if (gameObject.IsEnemy() && !gameObject.IsAttackable())
        {
            return false;
        }

        if (MarkingHelper.GetStopTargets().Contains((long)gameObject.GameObjectId) && Service.Config.FilterStopMark)
        {
            return false;
        }

        return CheckStatus(gameObject, skipStatusProvideCheck)
            && CheckTimeToKill(gameObject)
            && CheckResistance(gameObject);
    }

    /// <summary>
    /// Checks the status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the status criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private bool CheckStatus(IGameObject gameObject, bool skipStatusProvideCheck)
    {
        if (!action.Config.ShouldCheckStatus) return true;

        if (action.Setting.TargetStatusNeed != null)
        {
            if (gameObject.WillStatusEndGCD(0, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusNeed))
            {
                return false;
            }
        }

        if (action.Setting.TargetStatusProvide != null && !skipStatusProvideCheck)
        {
            if (!gameObject.WillStatusEndGCD(action.Config.StatusGcdCount, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusProvide))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks the resistance status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the resistance criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private bool CheckResistance(IGameObject gameObject)
    {
        if (gameObject == null) return false;

        try
        {
            if (action.Info.AttackType == AttackType.Magic)
            {
                if (gameObject.HasStatus(false, StatusHelper.MagicResistance))
                {
                    return false;
                }
            }
            else if (action.Info.Aspect != Aspect.Piercing) // Physical
            {
                if (gameObject.HasStatus(false, StatusHelper.PhysicalResistance))
                {
                    return false;
                }
            }
            if (Range >= 20) // Range
            {
                if (gameObject.HasStatus(false, StatusID.RangedResistance, StatusID.EnergyField))
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            Svc.Log.Error($"Error checking resistance: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks the time to kill of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the time to kill criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private bool CheckTimeToKill(IGameObject gameObject)
    {
        if (gameObject is not IBattleChara b) return false;
        var time = b.GetTTK();
        return float.IsNaN(time) || time >= action.Config.TimeToKill;
    }

    #endregion

    #region Target Result

    /// <summary>
    /// Finds the target based on the specified criteria.
    /// </summary>
    /// <param name="skipAoeCheck">If set to <c>true</c>, skips the AoE check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target and affected characters, or <c>null</c> if no target is found.
    /// </returns>
    internal TargetResult? FindTarget(bool skipAoeCheck, bool skipStatusProvideCheck)
    {
        var range = Range;
        var player = Player.Object;

        if (player == null)
        {
            return null;
        }

        if (range == 0 && EffectRange == 0)
        {
            return new TargetResult(player, Array.Empty<IBattleChara>(), player.Position);
        }

        var type = action.Setting.TargetType;

        var canTargets = GetCanTargets(skipStatusProvideCheck, type);
        var canAffects = GetCanAffects(skipStatusProvideCheck, type);

        if (IsTargetArea)
        {
            return FindTargetArea(canTargets, canAffects, range, player);
        }

        var targets = GetMostCanTargetObjects(canTargets, canAffects, skipAoeCheck ? 0 : action.Config.AoeCount);
        var target = FindTargetByType(targets, type, action.Config.AutoHealRatio, action.Setting.SpecialType);
        return target == null ? null : new TargetResult(target, GetAffects(target, canAffects).ToArray(), target.Position);
    }

    /// <summary>
    /// Finds the target area based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="range">The range within which to find the target area.</param>
    /// <param name="player">The player character.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private TargetResult? FindTargetArea(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects,
        float range, IPlayerCharacter player)
    {
        if (canTargets == null || canAffects == null || player == null) return null;

        if (action.Setting.TargetType == TargetType.Move)
        {
            return FindTargetAreaMove(range);
        }
        else if (action.Setting.IsFriendly)
        {
            if (!Service.Config.UseGroundBeneficialAbility) return null;
            if (!Service.Config.UseGroundBeneficialAbilityWhenMoving && DataCenter.IsMoving) return null;

            return FindTargetAreaFriend(range, canAffects, player);
        }
        else
        {
            return FindTargetAreaHostile(canTargets, canAffects, action.Config.AoeCount);
        }
    }

    /// <summary>
    /// Finds the hostile target area based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="aoeCount">The number of targets to consider for AoE.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target and affected characters, or <c>null</c> if no target is found.
    /// </returns>
    private TargetResult? FindTargetAreaHostile(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canTargets == null || canAffects == null) return null;

        IBattleChara? target = null;
        var mostCanTargetObjects = GetMostCanTargetObjects(canTargets, canAffects, aoeCount);
        var enumerator = mostCanTargetObjects.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var t = enumerator.Current;
            if (target == null || ObjectHelper.GetHealthRatio(t) > ObjectHelper.GetHealthRatio(target))
            {
                target = t;
            }
        }

        if (target == null) return null;

        var affectedTargets = new List<IBattleChara>();
        var affectsEnumerator = canAffects.GetEnumerator();
        while (affectsEnumerator.MoveNext())
        {
            var t = affectsEnumerator.Current;
            if (Vector3.Distance(target.Position, t.Position) - t.HitboxRadius <= EffectRange)
            {
                affectedTargets.Add(t);
            }
        }

        return new TargetResult(target, affectedTargets.ToArray(), target.Position);
    }

    /// <summary>
    /// Finds the target area for movement based on the specified range.
    /// </summary>
    /// <param name="range">The range within which to find the target area.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private TargetResult? FindTargetAreaMove(float range)
    {
        var player = Player.Object;
        if (player == null) return null;

        if (Service.Config.MoveAreaActionFarthest)
        {
            Vector3 pPosition = player.Position;
            if (Service.Config.MoveTowardsScreenCenter)
            {
                unsafe
                {
                    var cameraManager = CameraManager.Instance();
                    if (cameraManager == null) return null;

                    var camera = cameraManager->CurrentCamera;
                    var tar = camera->LookAtVector - camera->Object.Position;
                    tar.Y = 0;
                    var length = ((Vector3)tar).Length();
                    if (length == 0) return null;

                    tar = tar / length * range;
                    return new TargetResult(player, Array.Empty<IBattleChara>(), new Vector3(pPosition.X + tar.X, pPosition.Y, pPosition.Z + tar.Z));
                }
            }
            else
            {
                float rotation = player.Rotation;
                return new TargetResult(player, Array.Empty<IBattleChara>(), new Vector3(pPosition.X + (float)Math.Sin(rotation) * range, pPosition.Y, pPosition.Z + (float)Math.Cos(rotation) * range));
            }
        }
        else
        {
            var availableCharas = new List<IBattleChara>();
            foreach (var availableTarget in DataCenter.AllTargets)
            {
                if (availableTarget.GameObjectId != player.GameObjectId)
                {
                    availableCharas.Add(availableTarget);
                }
            }

            var targetList = TargetFilter.GetObjectInRadius(availableCharas, range);
            var target = FindTargetByType(targetList, TargetType.Move, action.Config.AutoHealRatio, action.Setting.SpecialType);
            if (target == null) return null;

            return new TargetResult(target, Array.Empty<IBattleChara>(), target.Position);
        }
    }

    /// <summary>
    /// Finds the target area for friendly actions based on the specified range and strategy.
    /// </summary>
    /// <param name="range">The range within which to find the target area.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="player">The player character.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private TargetResult? FindTargetAreaFriend(float range, IEnumerable<IBattleChara> canAffects, IPlayerCharacter player)
    {
        if (canAffects == null || player == null) return null;

        // Check if the action's range is zero and handle it as targeting self
        if (range == 0)
        {
            return new TargetResult(player, GetAffects(player.Position, canAffects).ToArray(), player.Position);
        }

        var strategy = Service.Config.BeneficialAreaStrategy2;
        switch (strategy)
        {
            case BeneficialAreaStrategy2.OnLocations: // Only the list
                OtherConfiguration.BeneficialPositions.TryGetValue(Svc.ClientState.TerritoryType, out var pts);
                pts ??= Array.Empty<Vector3>();

                // Use fallback points if no beneficial positions are found
                if (pts.Length == 0)
                {
                    if (DataCenter.Territory?.ContentType == TerritoryContentType.Trials ||
                        (DataCenter.Territory?.ContentType == TerritoryContentType.Raids &&
                         DataCenter.PartyMembers.Count(p => p is IPlayerCharacter) >= 8))
                    {
                        var fallbackPoints = new[] { Vector3.Zero, new Vector3(100, 0, 100) };
                        var closestFallback = fallbackPoints[0];
                        var minDistance = Vector3.Distance(player.Position, fallbackPoints[0]);

                        for (int i = 1; i < fallbackPoints.Length; i++)
                        {
                            var distance = Vector3.Distance(player.Position, fallbackPoints[i]);
                            if (distance < minDistance)
                            {
                                closestFallback = fallbackPoints[i];
                                minDistance = distance;
                            }
                        }

                        pts = new[] { closestFallback };
                    }
                }

                // Find the closest point and apply a random offset
                if (pts.Length > 0)
                {
                    var closest = pts[0];
                    var minDistance = Vector3.Distance(player.Position, pts[0]);

                    for (int i = 1; i < pts.Length; i++)
                    {
                        var distance = Vector3.Distance(player.Position, pts[i]);
                        if (distance < minDistance)
                        {
                            closest = pts[i];
                            minDistance = distance;
                        }
                    }

                    var random = new Random();
                    var rotation = random.NextDouble() * Math.Tau;
                    var radius = random.NextDouble();
                    closest.X += (float)(Math.Sin(rotation) * radius);
                    closest.Z += (float)(Math.Cos(rotation) * radius);

                    // Check if the closest point is within the effect range
                    if (Vector3.Distance(player.Position, closest) < player.HitboxRadius + EffectRange)
                    {
                        return new TargetResult(player, GetAffects(closest, canAffects).ToArray(), closest);
                    }
                }

                // Return null if strategy is OnLocations and no valid point is found
                if (strategy == BeneficialAreaStrategy2.OnLocations) return null;
                break;

            //case BeneficialAreaStrategy2.OnTarget: // Target
            //    if (Svc.Targets.Target != null && Svc.Targets.Target.DistanceToPlayer() < range)
            //    {
            //        var target = Svc.Targets.Target as IBattleChara;
            //        if (target != null && !target.HasPositional() && target.HitboxRadius <= 8)
            //        {
            //            return new TargetResult(player, GetAffects(player.Position, canAffects).ToArray(), player.Position);
            //        }
            //        return new TargetResult(target, GetAffects(target?.Position, canAffects).ToArray(), target?.Position);
            //    }
            //    break;

            case BeneficialAreaStrategy2.OnCalculated: // OnCalculated
                if (Svc.Targets.Target is IBattleChara b && b.DistanceToPlayer() < range &&
                b.IsBossFromIcon() && b.HasPositional() && b.HitboxRadius <= 8)
                {
                    // Ensure the player's position is within the range of the ability
                    if (Vector3.Distance(player.Position, b.Position) <= range)
                    {
                        return new TargetResult(b, GetAffects(b.Position, canAffects).ToArray(), b.Position);
                    }
                    else
                    {
                        // Adjust the position to be within the range
                        Vector3 directionToTarget = b.Position - player.Position;
                        Vector3 adjustedPosition = player.Position + directionToTarget / directionToTarget.Length() * range;
                        return new TargetResult(b, GetAffects(adjustedPosition, canAffects).ToArray(), adjustedPosition);
                    }
                }
                else
                {
                    var effectRange = EffectRange;
                    var attackT = FindTargetByType(DataCenter.PartyMembers.GetObjectInRadius(range + effectRange),
                        TargetType.BeAttacked, action.Config.AutoHealRatio, action.Setting.SpecialType);

                    if (attackT == null)
                    {
                        return new TargetResult(player, GetAffects(player.Position, canAffects).ToArray(), player.Position);
                    }
                    else
                    {
                        var disToTankRound = Vector3.Distance(player.Position, attackT.Position) + attackT.HitboxRadius;

                        if (disToTankRound < effectRange
                            || disToTankRound > 2 * effectRange - player.HitboxRadius)
                        {
                            return new TargetResult(player, GetAffects(player.Position, canAffects).ToArray(), player.Position);
                        }
                        else
                        {
                            Vector3 directionToTank = attackT.Position - player.Position;
                            var moveDirection = directionToTank / directionToTank.Length() * Math.Max(0, disToTankRound - effectRange);
                            return new TargetResult(player, GetAffects(player.Position, canAffects).ToArray(), player.Position + moveDirection);
                        }
                    }
                }
        }

        return null;
    }


    /// <summary>
    /// Gets the characters that are affected within the specified range from a given point.
    /// </summary>
    /// <param name="point">The point from which to measure the effect range.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the characters that are within the effect range.
    /// </returns>
    private IEnumerable<IBattleChara> GetAffects(Vector3? point, IEnumerable<IBattleChara> canAffects)
    {
        if (point == null || canAffects == null) yield break;

        foreach (var t in canAffects)
        {
            if (Vector3.Distance(point.Value, t.Position) - t.HitboxRadius <= EffectRange)
            {
                yield return t;
            }
        }
    }

    /// <summary>
    /// Gets the characters that are affected by the specified target.
    /// </summary>
    /// <param name="tar">The target character.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the characters that are affected by the target.
    /// </returns>
    private IEnumerable<IBattleChara> GetAffects(IBattleChara tar, IEnumerable<IBattleChara> canAffects)
    {
        if (tar == null || canAffects == null) yield break;

        foreach (var t in canAffects)
        {
            if (CanGetTarget(tar, t))
            {
                yield return t;
            }
        }
    }

    #endregion

    #region Get Most Target

    /// <summary>
    /// Gets the most targetable objects based on the specified criteria.
    /// </summary>
    /// <param name="canTargets">The potential targets that can be affected.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <param name="aoeCount">The number of targets to consider for AoE.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the most targetable objects based on the specified criteria.
    /// </returns>
    private IEnumerable<IBattleChara> GetMostCanTargetObjects(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canTargets == null || canAffects == null) yield break;
        if (IsSingleTarget || EffectRange <= 0)
        {
            foreach (var target in canTargets)
            {
                yield return target;
            }
            yield break;
        }
        if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off) yield break;
        if (aoeCount > 1 && Service.Config.AoEType == AoEType.Cleave) yield break;

        List<IBattleChara> objectMax = new(canTargets.Count());

        foreach (var t in canTargets)
        {
            int count = CanGetTargetCount(t, canAffects);

            if (count == aoeCount)
            {
                objectMax.Add(t);
            }
            else if (count > aoeCount)
            {
                aoeCount = count;
                objectMax.Clear();
                objectMax.Add(t);
            }
        }

        foreach (var obj in objectMax)
        {
            yield return obj;
        }
    }

    /// <summary>
    /// Counts the number of objects that can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="canAffects">The potential objects that can be affected.</param>
    /// <returns>The count of objects that can be targeted.</returns>
    private int CanGetTargetCount(IGameObject target, IEnumerable<IGameObject> canAffects)
    {
        if (target == null || canAffects == null) return 0;

        int count = 0;
        foreach (var t in canAffects)
        {
            if (target != t && !CanGetTarget(target, t)) continue;

            if (Service.Config.NoNewHostiles && t.TargetObject == null)
            {
                return 0;
            }
            count++;
        }

        return count;
    }

    const double _alpha = Math.PI / 3;
    /// <summary>
    /// Determines if the sub-target can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The main target object.</param>
    /// <param name="subTarget">The sub-target object.</param>
    /// <returns>True if the sub-target can be targeted; otherwise, false.</returns>
    private readonly bool CanGetTarget(IGameObject target, IGameObject subTarget)
    {
        if (target == null || subTarget == null) return false;

        var pPos = Player.Object.Position;
        Vector3 dir = target.Position - pPos;
        Vector3 tdir = subTarget.Position - pPos;

        switch (action.Action.CastType)
        {
            case 2: // Circle
                return Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius <= EffectRange;

            case 3: // Sector
                if (subTarget.DistanceToPlayer() > EffectRange) return false;
                tdir += dir / dir.Length() * target.HitboxRadius / (float)Math.Sin(_alpha);
                return Vector3.Dot(dir, tdir) / (dir.Length() * tdir.Length()) >= Math.Cos(_alpha);

            case 4: // Line
                if (subTarget.DistanceToPlayer() > EffectRange) return false;
                return Vector3.Cross(dir, tdir).Length() / dir.Length() <= 2 + target.HitboxRadius
                    && Vector3.Dot(dir, tdir) >= 0;

            case 10: // Donut
                var dis = Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius;
                return dis <= EffectRange && dis >= 8;

            default:
                Svc.Log.Debug($"{action.Action.Name.ExtractText().ToString()}'s CastType is not valid! The value is {action.Action.CastType}");
                return false;
        }
    }
    #endregion

    #region TargetFind

    private static IBattleChara? FindTargetByType(IEnumerable<IBattleChara> IGameObjects, TargetType type, float healRatio, SpecialActionType actionType)
    {

        if (IGameObjects == null) return null;

        if (type == TargetType.Self) return Player.Object;

        switch (actionType)
        {
            case SpecialActionType.MeleeRange:
                if (IGameObjects != null && Service.Config != null)
                {
                    IGameObjects = IGameObjects.Where(t => t.DistanceToPlayer() >= 3 + Service.Config.MeleeRangeOffset);
                }
                break;

            case SpecialActionType.MovingForward:
                if (Service.Config != null)
                {
                    if (DataCenter.MergedStatus.HasFlag(AutoStatus.MoveForward) || Service.CountDownTime > 0)
                    {
                        type = TargetType.Move;
                    }
                    else if (IGameObjects != null)
                    {
                        IGameObjects = IGameObjects.Where(t => t.DistanceToPlayer() < Service.Config.DistanceForMoving);
                    }
                }
                break;
        }

        switch (type) // Filter the objects.
        {
            case TargetType.Death:
                if (IGameObjects != null)
                {
                    IGameObjects = IGameObjects.Where(ObjectHelper.IsDeathToRaise);
                }
                break;

            case TargetType.Move:
                // No filtering needed for Move type
                break;

            default:
                if (IGameObjects != null)
                {
                    IGameObjects = IGameObjects.Where(ObjectHelper.IsAlive);
                }
                break;
        }

        return type switch // Find the object.
        {
            TargetType.BeAttacked => FindBeAttackedTarget(),
            TargetType.Provoke => FindProvokeTarget(),
            TargetType.Dispel => FindDispelTarget(),
            TargetType.Death => FindDeathPeople(),
            TargetType.Move => FindTargetForMoving(),
            TargetType.Heal => FindHealTarget(healRatio),
            TargetType.Interrupt => FindInterruptTarget(),
            TargetType.Tank => FindTankTarget(),
            TargetType.Melee => IGameObjects != null ? RandomMeleeTarget(IGameObjects) : null,
            TargetType.Range => IGameObjects != null ? RandomRangeTarget(IGameObjects) : null,
            TargetType.Magical => IGameObjects != null ? RandomMagicalTarget(IGameObjects) : null,
            TargetType.Physical => IGameObjects != null ? RandomPhysicalTarget(IGameObjects) : null,
            TargetType.DancePartner => FindDancePartner(),
            TargetType.MimicryTarget => FindMimicryTarget(),
            TargetType.TheSpear => FindTheSpear(),
            TargetType.TheBalance => FindTheBalance(),
            _ => FindHostile(),
        };

        IBattleChara? FindDancePartner()
        {
            // DancePartnerPriority based on the info from The Balance Discord for Level 100
            Job[] DancePartnerPriority = { Job.PCT, Job.SAM, Job.RPR, Job.VPR, Job.MNK, Job.NIN, Job.DRG, Job.BLM, Job.RDM, Job.SMN, Job.MCH, Job.BRD, Job.DNC };

            if (IGameObjects == null) return null;

            var partyMembers = new List<IBattleChara>();
            foreach (var obj in IGameObjects)
            {
                if (ObjectHelper.IsParty(obj))
                {
                    partyMembers.Add(obj);
                }
            }

            foreach (var job in DancePartnerPriority)
            {
                foreach (var member in partyMembers)
                {
                    if (member.IsJobs(job) && !member.IsDead)
                    {
                        return member;
                    }
                }
            }

            return RandomMeleeTarget(IGameObjects)
                ?? RandomRangeTarget(IGameObjects)
                ?? RandomMagicalTarget(IGameObjects)
                ?? RandomPhysicalTarget(IGameObjects)
                ?? null;
        }

        IBattleChara? FindTheSpear()
        {
            // The Spear priority based on the info from The Balance Discord for Level 100 Dance Partner
            Job[] TheSpearpriority = { Job.PCT, Job.MCH, Job.SMN, Job.RDM, Job.BRD, Job.DNC, Job.BLM, Job.SAM, Job.NIN, Job.VPR, Job.DRG, Job.MNK, Job.DRK, Job.RPR };

            if (IGameObjects == null) return null;

            var partyMembers = new List<IBattleChara>();
            foreach (var obj in IGameObjects)
            {
                if (ObjectHelper.IsParty(obj))
                {
                    partyMembers.Add(obj);
                }
            }

            foreach (var job in TheSpearpriority)
            {
                foreach (var member in partyMembers)
                {
                    if (member.IsJobs(job) && !member.IsDead)
                    {
                        return member;
                    }
                }
            }

            return RandomRangeTarget(IGameObjects)
                ?? RandomMeleeTarget(IGameObjects)
                ?? RandomMagicalTarget(IGameObjects)
                ?? RandomPhysicalTarget(IGameObjects)
                ?? null;
        }

        IBattleChara? FindTheBalance()
        {
            // The Balance priority based on the info from The Balance Discord for Level 100 Dance Partner
            Job[] TheBalancepriority = { Job.SAM, Job.NIN, Job.VPR, Job.DRG, Job.MNK, Job.RPR, Job.DRK, Job.PCT, Job.MCH, Job.SMN, Job.RDM, Job.BRD, Job.DNC, Job.BLM };

            if (IGameObjects == null) return null;

            var partyMembers = new List<IBattleChara>();
            foreach (var obj in IGameObjects)
            {
                if (ObjectHelper.IsParty(obj))
                {
                    partyMembers.Add(obj);
                }
            }

            foreach (var job in TheBalancepriority)
            {
                foreach (var member in partyMembers)
                {
                    if (member.IsJobs(job) && !member.IsDead)
                    {
                        return member;
                    }
                }
            }

            return RandomMeleeTarget(IGameObjects)
                ?? RandomRangeTarget(IGameObjects)
                ?? RandomMagicalTarget(IGameObjects)
                ?? RandomPhysicalTarget(IGameObjects)
                ?? null;
        }

        IBattleChara? FindProvokeTarget()
        {
            if (IGameObjects == null || DataCenter.ProvokeTarget == null)
            {
                return null;
            }

            if (IGameObjects.Any(o => o.GameObjectId == DataCenter.ProvokeTarget.GameObjectId))
            {
                return DataCenter.ProvokeTarget;
            }

            return null;
        }

        IBattleChara? FindDeathPeople()
        {
            if (IGameObjects == null || DataCenter.DeathTarget == null)
            {
                return null;
            }

            if (IGameObjects.Any(o => o.GameObjectId == DataCenter.DeathTarget.GameObjectId))
            {
                return DataCenter.DeathTarget;
            }

            return null;
        }

        IBattleChara? FindTargetForMoving()
        {
            if (Service.Config == null || Player.Object == null || IGameObjects == null)
            {
                return null;
            }

            return Service.Config.MoveTowardsScreenCenter ? FindMoveTargetScreenCenter() : FindMoveTargetFaceDirection();

            IBattleChara? FindMoveTargetScreenCenter()
            {
                var pPosition = Player.Object.Position;
                if (!Svc.GameGui.WorldToScreen(pPosition, out var playerScrPos)) return null;

                var tars = IGameObjects.Where(t =>
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving) return false;

                    if (!Svc.GameGui.WorldToScreen(t.Position, out var scrPos)) return false;

                    var dir = scrPos - playerScrPos;

                    if (dir.Y > 0) return false;

                    return Math.Abs(dir.X / dir.Y) <= Math.Tan(Math.PI * Service.Config.MoveTargetAngle / 360);
                }).OrderByDescending(ObjectHelper.DistanceToPlayer);

                return tars.FirstOrDefault();
            }

            IBattleChara? FindMoveTargetFaceDirection()
            {
                var pPosition = Player.Object.Position;
                var faceVec = Player.Object.GetFaceVector();

                var tars = IGameObjects.Where(t =>
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving) return false;

                    var dir = t.Position - pPosition;
                    var angle = Vector3.Dot(faceVec, Vector3.Normalize(dir));
                    return angle >= Math.Cos(Math.PI * Service.Config.MoveTargetAngle / 360);
                }).OrderByDescending(ObjectHelper.DistanceToPlayer);

                return tars.FirstOrDefault();
            }
        }

        IBattleChara? FindHealTarget(float healRatio)
        {
            if (IGameObjects == null || !IGameObjects.Any() || Service.Config == null)
            {
                return null;
            }

            IEnumerable<IBattleChara> filteredGameObjects = IGameObjects;

            if (IBaseAction.AutoHealCheck)
            {
                filteredGameObjects = filteredGameObjects.Where(o => o.GetHealthRatio() < healRatio);
            }

            var partyMembers = filteredGameObjects.Where(ObjectHelper.IsParty);

            return GeneralHealTarget(partyMembers)
                ?? GeneralHealTarget(filteredGameObjects)
                ?? partyMembers.FirstOrDefault(t => t.HasStatus(false, StatusHelper.TankStanceStatus))
                ?? partyMembers.FirstOrDefault()
                ?? filteredGameObjects.FirstOrDefault(t => t.HasStatus(false, StatusHelper.TankStanceStatus))
                ?? filteredGameObjects.FirstOrDefault();

            static IBattleChara? GeneralHealTarget(IEnumerable<IBattleChara> objs)
            {
                var healingNeededObjs = objs.Where(StatusHelper.NeedHealing).OrderBy(ObjectHelper.GetHealthRatio);

                var healerTars = healingNeededObjs.GetJobCategory(JobRole.Healer);
                var tankTars = healingNeededObjs.GetJobCategory(JobRole.Tank);

                var healerTar = healerTars.FirstOrDefault();
                if (healerTar != null && healerTar.GetHealthRatio() < Service.Config.HealthHealerRatio)
                {
                    return healerTar;
                }

                var tankTar = tankTars.FirstOrDefault();
                if (tankTar != null && tankTar.GetHealthRatio() < Service.Config.HealthTankRatio)
                {
                    return tankTar;
                }

                var tar = healingNeededObjs.FirstOrDefault();
                if (tar?.GetHealthRatio() < 1) return tar;

                return null;
            }
        }

        IBattleChara? FindInterruptTarget()
        {
            if (IGameObjects == null || DataCenter.InterruptTarget == null)
            {
                return null;
            }

            if (IGameObjects.Any(o => o.GameObjectId == DataCenter.InterruptTarget.GameObjectId))
            {
                return DataCenter.InterruptTarget;
            }

            return null;
        }

        IBattleChara? FindHostile()
        {
            if (IGameObjects == null || !IGameObjects.Any()) return null;

            // Filter out characters marked with stop markers
            if (Service.Config.FilterStopMark)
            {
                var filteredCharacters = MarkingHelper.FilterStopCharacters(IGameObjects);
                if (filteredCharacters != null && filteredCharacters.Any())
                {
                    IGameObjects = filteredCharacters;
                }
            }

            // Handle treasure characters
            if (DataCenter.TreasureCharas != null && DataCenter.TreasureCharas.Length > 0)
            {
                var treasureChara = IGameObjects.FirstOrDefault(b => b.GameObjectId == DataCenter.TreasureCharas[0]);
                if (treasureChara != null) return treasureChara;

                IGameObjects = IGameObjects.Where(b => !DataCenter.TreasureCharas.Contains(b.GameObjectId)).ToList();
            }

            // Filter high priority hostiles
            var highPriorityHostiles = IGameObjects.Where(ObjectHelper.IsTopPriorityHostile).ToList();
            if (highPriorityHostiles.Any())
            {
                IGameObjects = highPriorityHostiles;
            }

            return FindHostileRaw();
        }

        IBattleChara? FindHostileRaw()
        {
            if (IGameObjects == null)
            {
                return null;
            }
            
            var orderedGameObjects = DataCenter.TargetingType switch
                {
                    TargetingType.Small => IGameObjects.OrderBy<IGameObject, float>(p => p.HitboxRadius),
                    TargetingType.HighHP => IGameObjects.OrderByDescending<IGameObject, uint>(p => p is IBattleChara b ? b.CurrentHp : 0),
                    TargetingType.LowHP => IGameObjects.OrderBy<IGameObject, uint>(p => p is IBattleChara b ? b.CurrentHp : 0),
                    TargetingType.HighHPPercent => IGameObjects.OrderByDescending<IGameObject, float>(p => p is IBattleChara b ? b.CurrentHp / b.MaxHp : 0),
                    TargetingType.LowHPPercent => IGameObjects.OrderBy<IGameObject, float>(p => p is IBattleChara b ? b.CurrentHp / b.MaxHp : 0),
                    TargetingType.HighMaxHP => IGameObjects.OrderByDescending<IGameObject, uint>(p => p is IBattleChara b ? b.MaxHp : 0),
                    TargetingType.LowMaxHP => IGameObjects.OrderBy<IGameObject, uint>(p => p is IBattleChara b ? b.MaxHp : 0),
                    TargetingType.Nearest => IGameObjects.OrderBy<IGameObject, float>(p => p.DistanceToPlayer()),
                    TargetingType.Farthest => IGameObjects.OrderByDescending<IGameObject, float>(p => p.DistanceToPlayer()),
                    TargetingType.PvPHealers => IGameObjects.Where(p => p.IsJobs(JobRole.Healer.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer()).Any() 
                        ? IGameObjects.Where(p => p.IsJobs(JobRole.Healer.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer())
                        : IGameObjects.OrderBy<IGameObject, float>(p => p.DistanceToPlayer()),
                    TargetingType.PvPTanks => IGameObjects.Where(p => p.IsJobs(JobRole.Tank.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer()).Any()
                        ? IGameObjects.Where(p => p.IsJobs(JobRole.Tank.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer())
                        : IGameObjects.OrderBy<IGameObject, float>(p => p.DistanceToPlayer()),
                    TargetingType.PvPDPS => IGameObjects.Where(p => p.IsJobs(JobRole.AllDPS.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer()).Any()
                        ? IGameObjects.Where(p => p.IsJobs(JobRole.AllDPS.ToJobs())).OrderBy<IGameObject, float>(p => p.DistanceToPlayer())
                        : IGameObjects.OrderBy<IGameObject, float>(p => p.DistanceToPlayer()),
                    _ => IGameObjects.OrderByDescending<IGameObject, float>(p => p.HitboxRadius),
                };
                
                return orderedGameObjects.FirstOrDefault() as IBattleChara;
            };

        IBattleChara? FindBeAttackedTarget()
        {
            if (IGameObjects == null || !IGameObjects.Any())
            {
                return null;
            }

            var attachedT = IGameObjects.Where(ObjectHelper.IsTargetOnSelf);

            if (!DataCenter.AutoStatus.HasFlag(AutoStatus.DefenseSingle))
            {
                if (!attachedT.Any())
                {
                    attachedT = IGameObjects.Where(tank => tank.HasStatus(false, StatusHelper.TankStanceStatus));
                }

                if (!attachedT.Any())
                {
                    attachedT = IGameObjects.GetJobCategory(JobRole.Tank);
                }

                if (!attachedT.Any())
                {
                    attachedT = IGameObjects;
                }
            }

            return attachedT.OrderBy(ObjectHelper.GetHealthRatio).FirstOrDefault();
        }

        IBattleChara? FindDispelTarget()
        {
            if (IGameObjects == null || DataCenter.DispelTarget == null)
            {
                return null;
            }

            if (IGameObjects.Any(o => o.GameObjectId == DataCenter.DispelTarget.GameObjectId))
            {
                return DataCenter.DispelTarget;
            }

            return IGameObjects.FirstOrDefault(o => o is IBattleChara b && b.StatusList.Any(StatusHelper.CanDispel));
        }

        IBattleChara? FindTankTarget()
        {
            if (IGameObjects == null)
            {
                return null;
            }

            return RandomPickByJobs(IGameObjects, JobRole.Tank);
        }
    }

    private static IBattleChara? FindMimicryTarget()
    {
        var targetCandidates = DataCenter.AllTargets.Where(IsNeededRole).OrderBy(o => Player.DistanceTo(o.Position));
        return targetCandidates.FirstOrDefault();
    }

    private static bool IsNeededRole(IBattleChara character)
    {
        if (character.GameObjectId == Player.Object.GameObjectId)
            return false;
        var player = character as IPlayerCharacter;
        if (player == null || player.ClassJob.Value.IsLimitedJob)
            return false;

        var neededRole = DataCenter.BluRole;
        if (neededRole == null || neededRole == CombatRole.None)
            return false;

        return (int)neededRole == (int)player.GetRole();
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
            if (tar != null) return tar;
        }
        return null;
    }

    private static IBattleChara? RandomPickByJobs(IEnumerable<IBattleChara> tars, params Job[] jobs)
    {
        var targets = tars.Where(t => t.IsJobs(jobs));
        if (targets.Any()) return RandomObject(targets);

        return null;
    }

    private static IBattleChara? RandomObject(IEnumerable<IBattleChara> objs)
    {
        return objs.FirstOrDefault();
        //Random ran = new(DateTime.Now.Millisecond);
        //var count = objs.Count();
        //if (count == 0) return null;
        //return objs.ElementAt(ran.Next(count));
    }

    #endregion
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public enum TargetType : byte
{
    Big,
    Small,
    HighHP,
    LowHP,
    HighMaxHP,
    LowMaxHP,
    Interrupt,
    Provoke,
    Death,
    Dispel,
    Move,
    BeAttacked,
    Heal,
    Tank,
    Melee,
    Range,
    Physical,
    Magical,
    Self,
    DancePartner,
    MimicryTarget,
    TheBalance,
    TheSpear,
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The target result
/// </summary>
/// <param name="Target">the target.</param>
/// <param name="AffectedTargets">the targets that be affected by this action.</param>
/// <param name="Position">the position to use this action.</param>
public readonly record struct TargetResult(IBattleChara? Target, IBattleChara[] AffectedTargets, Vector3? Position);
