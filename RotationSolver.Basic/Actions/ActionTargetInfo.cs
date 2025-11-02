using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
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
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// A <see cref="List{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private readonly List<IBattleChara> GetCanTargets(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type)
    {
        if (DataCenter.AllTargets == null)
        {
            return [];
        }

        List<IBattleChara> validTargets = [];
        foreach (IBattleChara target in TargetHelper.GetTargetsByRange(Range))
        {
            if (type == TargetType.Heal && target.GetHealthRatio() == 1)
            {
                continue;
            }

            var statusCheck = CheckStatus(target, skipStatusProvideCheck, skipTargetStatusNeedCheck);
            var ttkCheck = CheckTimeToKill(target);
            var resistanceCheck = CheckResistance(target);

            if (!statusCheck || !ttkCheck || !resistanceCheck)
            {
                continue;
            }

            // If auto targeting is enabled
            // or the skill is friendly
            // or the target is the currently selected hard-target
            // or the target is ourselves
            // then => Check target is in the view && ActionManager.CanUse on target && CanSee Target && action.setting predicate is true of target
            if (!DataCenter.IsManual || IsTargetFriendly || target.GameObjectId == Svc.Targets.Target?.GameObjectId || target.GameObjectId == Player.Object.GameObjectId)
            {
                var view = TargetOnScreen(target);
                var canUse = CanUseTo(target);
                var asCanTarget = action.Setting.CanTarget(target);
                var MinHPPass = !action.MinHPFeature || (action.MinHPFeature && target.GetHealthRatio() > action.MinHPPercent);

                if (view && canUse && asCanTarget && MinHPPass)
                {
                    validTargets.Add(target);
                }
            }
        }

        return validTargets;
    }

    /// <summary>
    /// Retrieves a list of battle characters that can be affected based on the specified criteria.
    /// </summary>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <param name="type">The type of target to filter (e.g., Heal).</param>
    /// <returns>
    /// A <see cref="List{IBattleChara}"/> containing the valid targets.
    /// </returns>
    private readonly List<IBattleChara> GetCanAffects(bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck, TargetType type)
    {
        if (EffectRange == 0)
        {
            return [];
        }

        if ((action.Setting.IsFriendly
            ? DataCenter.PartyMembers
            : DataCenter.AllHostileTargets) == null)
        {
            return [];
        }

        IEnumerable<IBattleChara> items = TargetHelper.GetTargetsByRange(Range + EffectRange, action.Setting.IsFriendly);

        List<IBattleChara> validTargets = [];
        foreach (IBattleChara tar in items)
        {
            if (type == TargetType.Heal && tar.GetHealthRatio() >= 1)
            {
                continue;
            }

            if (CheckStatus(tar, skipStatusProvideCheck, skipTargetStatusNeedCheck) && CheckTimeToKill(tar) && CheckResistance(tar))
            {
                validTargets.Add(tar);
            }
        }
        return validTargets;
    }

    /// <summary>
    /// Determines whether the specified battle character is within the player's view and vision cone based on the configuration settings.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the battle character is within the player's view and vision cone; otherwise, <c>false</c>.
    /// </returns>
    private static bool TargetOnScreen(IBattleChara battleChara)
    {
        if (Service.Config.OnlyAttackInView)
        {
            if (!Svc.GameGui.WorldToScreen(battleChara.Position, out _))
            {
                return false;
            }
        }

        if (Service.Config.OnlyAttackInVisionCone && Player.Object != null)
        {
            Vector3 dir = battleChara.Position - Player.Object.Position;
            Vector3 faceVec = Player.Object.GetFaceVector();
            dir = Vector3.Normalize(dir);
            faceVec = Vector3.Normalize(faceVec);

            // Compare cosines instead of acos
            double dot = Vector3.Dot(faceVec, dir);
            double thresholdCos = Math.Cos(Math.PI * Service.Config.AngleOfVisionCone / 360);
            if (dot < thresholdCos)
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
    private readonly unsafe bool CanUseTo(IBattleChara tar)
    {
        if (tar == null)
        {
            return false;
        }

        if (!Player.AvailableThreadSafe)
        {
            return false;
        }

        if (!(tar.GameObjectId != 0 && tar.Struct() != null))
        {
            return false;
        }

        var specialAbilityCheck = IsSpecialAbility(action.Info.ID);
        var actionmanagerCheck = ActionManager.CanUseActionOnTarget(action.Info.AdjustedID, (GameObject*)tar.Struct());

        if (!(specialAbilityCheck || actionmanagerCheck))
        {
            return false;
        }
        if (!IsTargetFriendly) // canSee check is already done as part of finding hostile targets
        {
            return tar.GameObjectId == Player.Object.GameObjectId || DataCenter.AllHostileTargets.Contains(tar);
        }
        else // For friendly targets we still need to check CanSee
        {
            return tar.CanSee();
        }
    }

    private readonly List<ActionID> _specialActions =
    [
        ActionID.AethericMimicryPvE,
        ActionID.EruptionPvE,
        ActionID.BishopAutoturretPvP,
        ActionID.CometPvP,
        ActionID.DotonPvE,
        ActionID.DotonPvE_18880,
        ActionID.FeatherRainPvE,
        ActionID.SaltAndDarknessPvP,
    ];

    private readonly bool IsSpecialAbility(uint iD)
    {
        return _specialActions.Contains((ActionID)iD);
    }

    /// <summary>
    /// Checks the status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the status criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckStatus(IBattleChara battleChara, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
    {
        if (battleChara == null)
        {
            return false;
        }

        if (!action.Config.ShouldCheckTargetStatus && !action.Config.ShouldCheckStatus)
        {
            return true;
        }

        if (action.Setting.TargetStatusNeed != null && !skipTargetStatusNeedCheck)
        {
            if (battleChara.WillStatusEndGCD(action.Config.StatusGcdCount, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusNeed))
            {
                return false;
            }
        }

        if (action.Setting.TargetStatusProvide != null && !skipStatusProvideCheck)
        {
            if (!battleChara.WillStatusEndGCD(action.Config.StatusGcdCount, 0, action.Setting.StatusFromSelf, action.Setting.TargetStatusProvide) || (Service.Config.Statuscap2 && StatusHelper.IsStatusCapped(battleChara)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks the resistance status of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the resistance criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckResistance(IBattleChara battleChara)
    {
        if (battleChara == null)
        {
            return false;
        }
        if (battleChara.StatusList == null)
        {
            return false;
        }

        try
        {
            if (action.Info.AttackType == AttackType.Magic)
            {
                if (battleChara.HasStatus(false, StatusHelper.MagicResistance))
                {
                    return false;
                }
            }
            else if (action.Info.Aspect != Aspect.Piercing) // Physical
            {
                if (battleChara.HasStatus(false, StatusHelper.PhysicalResistance))
                {
                    return false;
                }
            }
            if (Range >= 20) // Range
            {
                if (battleChara.HasStatus(false, StatusID.EnergyField))
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            PluginLog.Error($"Error checking resistance: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks the time to kill of the specified game object to determine if it meets the criteria for the action.
    /// </summary>
    /// <param name="battleChara">The game object to check.</param>
    /// <returns>
    /// <c>true</c> if the game object meets the time to kill criteria for the action; otherwise, <c>false</c>.
    /// </returns>
    private readonly bool CheckTimeToKill(IBattleChara battleChara)
    {
        if (battleChara == null)
        {
            return false;
        }

        if (battleChara is not IBattleChara b)
        {
            return false;
        }

        float time = b.GetTTK();
        return float.IsNaN(time) || time >= action.Config.TimeToKill;
    }

    #endregion

    #region Target Result

    /// <summary>
    /// Finds the target based on the specified criteria.
    /// </summary>
    /// <param name="skipAoeCheck">If set to <c>true</c>, skips the AoE check.</param>
    /// <param name="skipStatusProvideCheck">If set to <c>true</c>, skips the status provide check.</param>
    /// <param name="skipTargetStatusNeedCheck">If set to <c>true</c>, skips the target status need check.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target and affected characters, or <c>null</c> if no target is found.
    /// </returns>
    internal readonly TargetResult? FindTarget(bool skipAoeCheck, bool skipStatusProvideCheck, bool skipTargetStatusNeedCheck)
    {
        if (action == null || action.Setting == null || action.Config == null)
        {
            return null;
        }

        if (Range == 0 && EffectRange == 0)
        {
            return new TargetResult(Player.Object, [], Player.Object.Position);
        }

        TargetType type = action.Setting.TargetType;

        IEnumerable<IBattleChara> canTargets = GetCanTargets(skipStatusProvideCheck, skipTargetStatusNeedCheck, type);
        List<IBattleChara> canAffects = GetCanAffects(skipStatusProvideCheck, skipTargetStatusNeedCheck, type);

        if (canTargets == null || canAffects == null)
        {
            return null;
        }

        // If AoE is disabled, block all hostile non-single-target actions (including ground-targeted)
        if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off && !IsSingleTarget)
        {
            return null;
        }

        if (IsTargetArea)
        {
            return FindTargetArea(canTargets, canAffects, Range, Player.Object);
        }

        List<IBattleChara> targetsList = [.. GetMostCanTargetObjects(canTargets, canAffects, skipAoeCheck ? 0 : action.Config.AoeCount)];

        IBattleChara? target = targetsList.Count > 0
            ? FindTargetByType(targetsList, type, action.Config.AutoHealRatio, action.Setting.SpecialType)
            : FindTargetByType([], type, action.Config.AutoHealRatio, action.Setting.SpecialType);

        IBattleChara[] affectedTargets;
        if (target != null)
        {
            var affectsEnum = GetAffectsTarget(target, canAffects);
            List<IBattleChara> affectsList = [];
            if (affectsEnum != null)
            {
                foreach (var a in affectsEnum) affectsList.Add(a);
            }
            affectedTargets = [.. affectsList];
        }
        else
        {
            affectedTargets = [];
        }

        return target == null ? null : new TargetResult(target, affectedTargets, target.Position);
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
    private readonly TargetResult? FindTargetArea(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects,
    float range, IPlayerCharacter player)
    {
        if (player == null || canTargets == null || canAffects == null)
        {
            return null;
        }

        if (action.Setting.TargetType == TargetType.Move)
        {
            return FindTargetAreaMove(range);
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
                        return FindTargetAreaFriend(range, canAffects, player);
                    }
                }
            }
            else
            {
                return FindTargetAreaHostile(canTargets, canAffects, action.Config.AoeCount);
            }
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
    private readonly TargetResult? FindTargetAreaHostile(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canAffects == null || canTargets == null)
        {
            return null;
        }

        IBattleChara? target = null;
        IEnumerable<IBattleChara> mostCanTargetObjects = GetMostCanTargetObjects(canTargets, canAffects, aoeCount);
        using var enumerator = mostCanTargetObjects.GetEnumerator();

        while (enumerator.MoveNext())
        {
            IBattleChara t = enumerator.Current;
            if (target == null || ObjectHelper.GetHealthRatio(t) > ObjectHelper.GetHealthRatio(target))
            {
                target = t;
            }
        }

        if (target == null)
        {
            return null;
        }

        List<IBattleChara> affectedTargets = [];
        foreach (IBattleChara t in canAffects)
        {
            if (Vector3.Distance(target.Position, t.Position) - t.HitboxRadius <= EffectRange)
            {
                affectedTargets.Add(t);
            }
        }

        IBattleChara[] affectedTargetsArray = new IBattleChara[affectedTargets.Count];
        for (int i = 0; i < affectedTargets.Count; i++)
        {
            affectedTargetsArray[i] = affectedTargets[i];
        }
        return new TargetResult(target, affectedTargetsArray, target.Position);
    }

    /// <summary>
    /// Finds the target area for movement based on the specified range.
    /// </summary>
    /// <param name="range">The range within which to find the target area.</param>
    /// <returns>
    /// A <see cref="TargetResult"/> containing the target area and affected characters, or <c>null</c> if no target area is found.
    /// </returns>
    private readonly TargetResult? FindTargetAreaMove(float range)
    {
        if (Service.Config.MoveAreaActionFarthest)
        {
            Vector3 pPosition = Player.Object.Position;
            if (Service.Config.MoveTowardsScreenCenter)
            {
                unsafe
                {
                    CameraManager* cameraManager = CameraManager.Instance();
                    if (cameraManager == null)
                    {
                        return null;
                    }

                    FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera* camera = cameraManager->CurrentCamera;
                    FFXIVClientStructs.FFXIV.Common.Math.Vector3 tar = camera->LookAtVector - camera->Object.Position;
                    tar.Y = 0;
                    float length = ((Vector3)tar).Length();
                    if (length == 0)
                    {
                        return null;
                    }

                    tar = tar / length * range;
                    return new TargetResult(Player.Object, [], new Vector3(pPosition.X + tar.X, pPosition.Y, pPosition.Z + tar.Z));
                }
            }
            else
            {
                float rotation = Player.Object.Rotation;
                return new TargetResult(Player.Object, [], new Vector3(pPosition.X + ((float)Math.Sin(rotation) * range), pPosition.Y, pPosition.Z + ((float)Math.Cos(rotation) * range)));
            }
        }
        else
        {
            List<IBattleChara> availableCharas = [];
            foreach (IBattleChara availableTarget in DataCenter.AllTargets)
            {
                if (availableTarget.GameObjectId != Player.Object.GameObjectId)
                {
                    availableCharas.Add(availableTarget);
                }
            }

            IEnumerable<IBattleChara> targetList = TargetFilter.GetObjectInRadius(availableCharas, range);
            IBattleChara? target = FindTargetByType(targetList, TargetType.Move, action.Config.AutoHealRatio, action.Setting.SpecialType);
            return target == null ? null : new TargetResult(target, Array.Empty<IBattleChara>(), target.Position);
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
    private readonly TargetResult? FindTargetAreaFriend(float range, IEnumerable<IBattleChara> canAffects, IPlayerCharacter player)
    {
        if (canAffects == null)
        {
            return null;
        }

        // If range is zero, always target self
        if (range == 0 || Service.Config.UseGroundBeneficialAbilityOnlySelf)
        {
            return new TargetResult(player, [.. GetAffectsVector(player.Position, canAffects)], player.Position);
        }

        // --- Try OnLocations logic first ---
        _ = OtherConfiguration.BeneficialPositions.TryGetValue(Svc.ClientState.TerritoryType, out Vector3[]? pts);
        pts ??= [];

        // Use fallback points if no beneficial positions are found
        if (pts.Length == 0)
        {
            bool isTrial = DataCenter.Territory?.ContentType == TerritoryContentType.Trials;
            bool isRaid = DataCenter.Territory?.ContentType == TerritoryContentType.Raids;
            int partyCount = 0;
            if (DataCenter.PartyMembers != null)
            {
                foreach (var p in DataCenter.PartyMembers)
                {
                    if (p is IPlayerCharacter)
                    {
                        partyCount++;
                    }
                }
            }
            if (isTrial || (isRaid && partyCount >= 8))
            {
                Vector3[] fallbackPoints = [Vector3.Zero, new Vector3(100, 0, 100)];
                Vector3 closestFallback = fallbackPoints[0];
                float minDistance = Vector3.Distance(player.Position, fallbackPoints[0]);

                for (int i = 1; i < fallbackPoints.Length; i++)
                {
                    float distance = Vector3.Distance(player.Position, fallbackPoints[i]);
                    if (distance < minDistance)
                    {
                        closestFallback = fallbackPoints[i];
                        minDistance = distance;
                    }
                }

                pts = [closestFallback];
            }
        }

        // Find the closest point and apply a small random offset, then clamp within cast range
        if (pts.Length > 0)
        {
            Vector3 closest = pts[0];
            float minDistance = Vector3.Distance(player.Position, pts[0]);

            for (int i = 1; i < pts.Length; i++)
            {
                float distance = Vector3.Distance(player.Position, pts[i]);
                if (distance < minDistance)
                {
                    closest = pts[i];
                    minDistance = distance;
                }
            }

            // Small random jitter to avoid stacking perfectly; keep it within a reasonable local radius
            double rotation = Random.Shared.NextDouble() * Math.Tau;
            float jitterRadius = (float)(Random.Shared.NextDouble() * MathF.Min(EffectRange * 0.5f, 2f));
            closest.X += (float)(Math.Sin(rotation) * jitterRadius);
            closest.Z += (float)(Math.Cos(rotation) * jitterRadius);

            // Clamp desired placement within cast range from player
            Vector3 toDesired = closest - player.Position;
            float len = toDesired.Length();
            if (len > range)
            {
                closest = player.Position + toDesired / len * range;
            }

            // Only accept if within cast range; then compute affected list
            if (Vector3.Distance(player.Position, closest) <= range + player.HitboxRadius)
            {
                List<IBattleChara> affectsList = [.. GetAffectsVector(closest, canAffects)];
                if (affectsList.Count > 0)
                {
                    return new TargetResult(player, [.. affectsList], closest);
                }
            }
        }

        // --- OnCalculated logic (fallback) ---
        if (Svc.Targets.Target is IBattleChara b && b.DistanceToPlayer() < range &&
            b.IsBossFromIcon() && b.HasPositional() && b.HitboxRadius <= 8)
        {
            if (Vector3.Distance(player.Position, b.Position) <= range)
            {
                List<IBattleChara> affectsList = [.. GetAffectsVector(b.Position, canAffects)];
                return new TargetResult(b, [.. affectsList], b.Position);
            }
            else
            {
                // Adjust the position to be within the cast range
                Vector3 directionToTarget = b.Position - player.Position;
                Vector3 adjustedPosition = player.Position + (directionToTarget / directionToTarget.Length() * range);
                List<IBattleChara> affectsList = [.. GetAffectsVector(adjustedPosition, canAffects)];
                return new TargetResult(b, [.. affectsList], adjustedPosition);
            }
        }
        else
        {
            float effectRange = EffectRange;
            // Remove LINQ: manually build the list for GetObjectInRadius
            List<IBattleChara> partyMembersInRadius = [];
            if (DataCenter.PartyMembers != null)
            {
                foreach (var member in DataCenter.PartyMembers)
                {
                    float dist = Vector3.Distance(member.Position, player.Position);
                    if (dist <= range + effectRange)
                    {
                        partyMembersInRadius.Add(member);
                    }
                }
            }
            IBattleChara? attackT = FindTargetByType(partyMembersInRadius,
                TargetType.BeAttacked, action.Config.AutoHealRatio, action.Setting.SpecialType);

            if (attackT == null)
            {
                List<IBattleChara> affectsList = [.. GetAffectsVector(player.Position, canAffects)];
                return new TargetResult(player, [.. affectsList], player.Position);
            }
            else
            {
                float disToTankRound = Vector3.Distance(player.Position, attackT.Position) + attackT.HitboxRadius;

                Vector3 finalPos;
                if (disToTankRound < effectRange
                    || disToTankRound > (2 * effectRange) - player.HitboxRadius)
                {
                    finalPos = player.Position;
                }
                else
                {
                    Vector3 directionToTank = attackT.Position - player.Position;
                    Vector3 moveDirection = directionToTank / directionToTank.Length() * Math.Max(0, disToTankRound - effectRange);
                    finalPos = player.Position + moveDirection;
                }

                // Clamp finalPos within cast range
                Vector3 toFinal = finalPos - player.Position;
                float toFinalLen = toFinal.Length();
                if (toFinalLen > range)
                {
                    finalPos = player.Position + toFinal / toFinalLen * range;
                }

                List<IBattleChara> affectsList = [.. GetAffectsVector(finalPos, canAffects)];
                return new TargetResult(player, [.. affectsList], finalPos);
            }
        }
    }

    /// <summary>
    /// Gets the characters that are affected within the specified range from a given point.
    /// </summary>
    /// <param name="point">The point from which to measure the effect range.</param>
    /// <param name="canAffects">The potential characters that can be affected.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IBattleChara}"/> containing the characters that are within the effect range.
    /// </returns>
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

        foreach (IBattleChara t in canAffects)
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

        foreach (IBattleChara t in canAffects)
        {
            if (GetCanTarget(tar, t))
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
    private readonly IEnumerable<IBattleChara> GetMostCanTargetObjects(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
    {
        if (canTargets == null || canAffects == null)
        {
            yield break;
        }

        // Single target or no AoE radius: just pass candidates through
        if (IsSingleTarget || EffectRange <= 0)
        {
            foreach (IBattleChara target in canTargets)
                yield return target;
            yield break;
        }

        // For hostile actions: if AoE is disabled, block all non-single-target actions here
        if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off)
        {
            yield break;
        }

        // Cleave mode
        if (aoeCount > 1 && Service.Config.AoEType == AoEType.Cleave)
        {
            yield break;
        }

        // Materialize once to avoid multiple enumerations
        List<IBattleChara> targets = canTargets as List<IBattleChara> ?? [.. canTargets];

        List<IBattleChara> best = [];
        int bestCount = Math.Max(0, aoeCount);

        foreach (IBattleChara t in targets)
        {
            int count = GetCanTargetCount(t, canAffects);
            if (count < bestCount)
                continue;

            if (count > bestCount)
            {
                bestCount = count;
                best.Clear();
            }
            best.Add(t);
        }

        for (int i = 0; i < best.Count; i++)
            yield return best[i];
    }

    /// <summary>
    /// Counts the number of objects that can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The target object.</param>
    /// <param name="canAffects">The potential objects that can be affected.</param>
    /// <returns>The count of objects that can be targeted.</returns>
    private readonly int GetCanTargetCount(IBattleChara target, IEnumerable<IBattleChara> canAffects)
    {
        if (target == null || canAffects == null)
        {
            return 0;
        }

        int count = 0;
        foreach (IBattleChara t in canAffects)
        {
            if (target != t && !GetCanTarget(target, t))
            {
                continue;
            }

            if (Service.Config.NoNewHostiles && t.TargetObject == null)
            {
                return 0;
            }
            count++;
        }

        return count;
    }

    private const double _alpha = Math.PI / 3;
    /// <summary>
    /// Determines if the sub-target can be targeted based on the specified criteria.
    /// </summary>
    /// <param name="target">The main target object.</param>
    /// <param name="subTarget">The sub-target object.</param>
    /// <returns>True if the sub-target can be targeted; otherwise, false.</returns>
    private readonly bool GetCanTarget(IBattleChara target, IBattleChara subTarget)
    {
        if (target == null || subTarget == null || Player.Object == null)
        {
            return false;
        }

        Vector3 pPos = Player.Object.Position;
        Vector3 dir = target.Position - pPos;   // Aim direction (player -> main target)
        Vector3 tdir = subTarget.Position - pPos; // Vector to sub target

        float dirLen = dir.Length();
        float tdirLen = tdir.Length();

        // If the main target is essentially on top of the player, avoid divide-by-zero and
        // approximate using radial checks so we don't drop AoE entirely in degenerate cases.
        if (dirLen <= 0.001f)
        {
            switch (action.Action.CastType)
            {
                case 2: // Circle (centered at target)
                    return Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius <= EffectRange;

                case 10: // Donut (centered at target)
                    {
                        float d = Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius;
                        return d <= EffectRange && d >= 8f;
                    }

                case 3: // Sector (fallback: radial gate from player)
                case 4: // Line (fallback: radial gate from player)
                    return (tdirLen - subTarget.HitboxRadius) <= EffectRange;

                default:
                    PluginLog.Debug($"{action.Action.Name.ExtractText()}'s CastType is not valid! The value is {action.Action.CastType}");
                    return false;
            }
        }

        switch (action.Action.CastType)
        {
            case 2: // Circle (centered at target)
                return Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius <= EffectRange;

            case 3: // Sector (cone from player toward target)
                {
                    // Quick radial gate with hitbox
                    if (tdirLen - subTarget.HitboxRadius > EffectRange)
                    {
                        return false;
                    }

                    // Normalize once
                    Vector3 dirN = dir / dirLen;

                    // Widen by target hitbox along aim direction to be more permissive near the apex
                    Vector3 tdirAdj = tdir + dirN * (target.HitboxRadius / (float)Math.Sin(_alpha));

                    float tlen = tdirAdj.Length();
                    if (tlen <= 0.001f)
                    {
                        return true; // on top of player after adjustment: treat as inside
                    }

                    float cos = Vector3.Dot(dir, tdirAdj) / (dirLen * tlen);
                    return cos >= Math.Cos(_alpha);
                }

            case 4: // Line (beam from player toward target)
                {
                    // Distance gate with hitbox allowance
                    if (tdirLen - subTarget.HitboxRadius > EffectRange)
                    {
                        return false;
                    }

                    // Perpendicular distance from line
                    float perp = Vector3.Cross(dir, tdir).Length() / dirLen;

                    // Allow a bit of thickness: base 2 + both hitboxes
                    float width = 2f + target.HitboxRadius + subTarget.HitboxRadius;

                    return perp <= width && Vector3.Dot(dir, tdir) >= 0;
                }

            case 10: // Donut (centered at target)
                {
                    float dis = Vector3.Distance(target.Position, subTarget.Position) - subTarget.HitboxRadius;
                    return dis <= EffectRange && dis >= 8f;
                }

            default:
                PluginLog.Debug($"{action.Action.Name.ExtractText()}'s CastType is not valid! The value is {action.Action.CastType}");
                return false;
        }
    }

    ///// <summary>
    ///// Counts how many units would be hit if this action were used on the current hard target.
    ///// - When applyFilters is true, respects status/TTK/resistance filtering (same as GetCanAffects).
    ///// - When applyFilters is false, uses the general AoE counting logic.
    ///// </summary>
    //public readonly int CountAffectedAtCurrentTarget(IBaseAction action, bool applyFilters = true)
    //{
    //    var player = Player.Object;
    //    if (player == null) return 0;

    //    IBattleChara? currentTarget = action.Target.Target;

    //    // Single target or no AoE radius -> 1 (anchor)
    //    if (IsSingleTarget || EffectRange <= 0)
    //        return currentTarget != null ? 1 : 1;

    //    // If this action has a non-zero cast range and we do have a target, enforce range gate.
    //    if (Range > 0 && currentTarget != null)
    //    {
    //        float dist = Vector3.Distance(player.Position, currentTarget.Position) - currentTarget.HitboxRadius;
    //        if (dist > Range) return 0;
    //    }

    //    // Fast path: unfiltered geometry-based count using AoE logic (friendly/hostile + self-centered AoE).
    //    if (!applyFilters)
    //        return AoeCount(currentTarget, action);

    //    // Filtered path: build candidates the same way the action would (statuses, ttk, resistances),
    //    // then count using the same AoE geometry with an anchored center (current target or player for self-AoE).
    //    IBattleChara anchor = (currentTarget ?? player);

    //    // Build candidate set consistent with GetCanAffects and IsFriendly (heal ratio/status/ttk/resist).
    //    IEnumerable<IBattleChara> candidates = GetCanAffects(skipStatusProvideCheck: false, skipTargetStatusNeedCheck: false, action.Setting.TargetType);

    //    // For cast-range actions, if we have a target center ensure it is still in range.
    //    if (Range > 0 && currentTarget != null)
    //    {
    //        float dist = Vector3.Distance(player.Position, currentTarget.Position) - currentTarget.HitboxRadius;
    //        if (dist > Range) return 0;
    //    }

    //    int count = 0;
    //    foreach (var t in candidates)
    //    {
    //        if (t == null) continue;
    //        if (t.GameObjectId == anchor.GameObjectId || GetCanTarget(anchor, t))
    //            count++;
    //    }
    //    return count;
    //}

    ///// <summary>
    ///// Counts AoE hits for this action. For self-centered AoE (cast range == 0), anchors at player.
    ///// If a target is provided, always anchor at that target; otherwise, finds the best cluster.
    ///// </summary>
    //public static int AoeCount(IBattleChara? target, IBaseAction action)
    //{
    //    // Defensive checks
    //    if (action == null || Player.Object == null)
    //        return 0;

    //    // Pull action geometry via ActionTargetInfo to respect overrides (e.g. Liturgy)
    //    var ti = new ActionTargetInfo(action);
    //    float castRange = MathF.Max(0, ti.Range);
    //    float effectRange = MathF.Max(0, ti.EffectRange);
    //    bool isFriendly = ti.IsTargetFriendly;

    //    // No AoE radius -> nothing to count
    //    if (effectRange <= 0)
    //        return 0;

    //    // Candidate set: friendly actions -> party; hostile actions -> hostiles
    //    List<IBattleChara>? group = isFriendly ? DataCenter.PartyMembers : DataCenter.AllHostileTargets;
    //    if (group == null || group.Count == 0)
    //        return 0;

    //    // Helper to count hits around a center position
    //    static int CountAround(Vector3 center, IEnumerable<IBattleChara> objs, float radius)
    //    {
    //        int count = 0;
    //        foreach (var o in objs)
    //        {
    //            if (o == null) continue;
    //            // Standard circle hit test: distance minus target hitbox within radius
    //            if (Vector3.Distance(center, o.Position) - o.HitboxRadius <= radius)
    //                count++;
    //        }
    //        return count;
    //    }

    //    // Always anchor when a target is provided, or when the action is self-centered (castRange == 0).
    //    if (target != null || castRange == 0)
    //    {
    //        Vector3 centerPos;
    //        if (castRange == 0 || target == null)
    //        {
    //            centerPos = Player.Object.Position;
    //        }
    //        else
    //        {
    //            centerPos = target.Position;

    //            // If we do have a cast range, ensure the chosen center is within range
    //            float toCenter = Vector3.Distance(Player.Object.Position, centerPos) - target.HitboxRadius;
    //            if (toCenter > castRange)
    //                return 0;
    //        }

    //        return CountAround(centerPos, group, effectRange);
    //    }

    //    // No target provided and not self-centered: find the best cluster center within cast range when applicable
    //    int maxAoeCount = 0;

    //    foreach (var centerTarget in group)
    //    {
    //        if (centerTarget == null) continue;

    //        Vector3 centerPos = centerTarget.Position;

    //        // Enforce cast range when applicable (range == 0 = self-centered, always OK)
    //        if (castRange > 0)
    //        {
    //            float toCenter = Vector3.Distance(Player.Object.Position, centerPos) - centerTarget.HitboxRadius;
    //            if (toCenter > castRange)
    //                continue;
    //        }

    //        int current = CountAround(centerPos, group, effectRange);
    //        if (current > maxAoeCount)
    //            maxAoeCount = current;
    //    }

    //    return maxAoeCount;
    //}
    #endregion

    #region TargetFind

    /// <summary>
    /// Finds the target based on the specified type and criteria.
    /// </summary>IBattleChara battleChara
    /// <param name="battleChara"></param>
    /// <param name="type"></param>
    /// <param name="healRatio"></param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    public static IBattleChara? FindTargetByType(IEnumerable<IBattleChara> battleChara, TargetType type, float healRatio, SpecialActionType actionType)
    {
        if (battleChara == null)
        {
            return null;
        }

        if (type == TargetType.Self)
        {
            return Player.Object;
        }

        switch (actionType)
        {
            case SpecialActionType.MeleeRange:
                {
                    var filtered = new List<IBattleChara>();
                    foreach (var t in battleChara)
                    {
                        if (t.DistanceToPlayer() >= 3 + (Service.Config?.MeleeRangeOffset ?? 0))
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
                            if (t.DistanceToPlayer() < Service.Config.DistanceForMoving2)
                            {
                                filtered.Add(t);
                            }
                        }
                        battleChara = filtered;
                    }
                }
                break;

            case SpecialActionType.FriendlyMovingForward:
                if (Svc.Targets.FocusTarget != null)
                {
                    if (Svc.Targets.FocusTarget is IBattleChara focus && focus.IsParty())
                    {
                        return focus;
                    }
                }
                else if (Svc.Targets.FocusTarget == null && Svc.Targets.Target != null)
                {
                    if (Svc.Targets.Target is IBattleChara IsPartytarg && IsPartytarg.IsParty())
                    {
                        return IsPartytarg;
                    }
                }
                break;

            case SpecialActionType.HostileFriendlyMovingForward:
                if (Svc.Targets.FocusTarget != null)
                {
                    if (Svc.Targets.FocusTarget is IBattleChara focus && focus.IsParty())
                    {
                        return focus;
                    }
                }
                else if (Svc.Targets.FocusTarget == null && Svc.Targets.Target != null)
                {
                    if (Svc.Targets.Target is IBattleChara IsPartytarg && IsPartytarg.IsParty())
                    {
                        return IsPartytarg;
                    }
                    if (Svc.Targets.Target is IBattleChara IsHostiletarg && IsHostiletarg.IsHostile())
                    {
                        return IsHostiletarg;
                    }
                }
                break;
        }

        switch (type) // Filter the objects.
        {
            case TargetType.Death:
                {
                    if (DataCenter.DeathTarget != null)
                    {
                        return DataCenter.DeathTarget;
                    }
                }
                break;

            case TargetType.Move:
                // No filtering needed for Move type
                break;

            case TargetType.FriendMove:
                {
                    if (Svc.Targets.FocusTarget != null)
                    {
                        if (Svc.Targets.FocusTarget is IBattleChara focus && focus.IsParty())
                        {
                            return focus;
                        }
                    }
                    else if (Svc.Targets.Target != null)
                    {
                        if (Svc.Targets.Target is IBattleChara targ && targ.IsParty())
                        {
                            return targ;
                        }
                    }
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

        return type switch // Find the object.
        {
            TargetType.BeAttacked => FindBeAttackedTarget(),
            TargetType.Provoke => FindProvokeTarget(),
            TargetType.Dispel => FindDispelTarget(),
            TargetType.Move => FindTargetForMoving(),
            TargetType.Heal => FindHealTarget(healRatio),
            TargetType.Interrupt => FindInterruptTarget(),
            TargetType.Tank => FindTankTarget(),
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
            _ => FindHostile(),
        };

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
                    if (x != null && !x.IsEnemy() && (!x.HasStatus(true, StatusID.BattleBell) || !x.HasStatus(false, StatusID.BattleBell)))
                    {
                        allMembers.Add(x);
                    }
                }
            }
            if (partyMembers != null && partyMembers.Count > 0)
            {
                var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                // Compare by reference or GameObjectId (safer for IBattleChara)
                if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
                    bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
                {
                    return bePartyAttackedTarget;
                }
            }

            // Fallback: only return self if self does NOT have BattleBell
            if (!Player.Object.HasStatus(true, StatusID.BattleBell) || !Player.Object.HasStatus(false, StatusID.BattleBell))
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
                partyMembers = [];
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
                // Use FindTargetByType to get the BeAttacked target from party members
                var bePartyAttackedTarget = FindTargetByType(partyMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                var beAllAttackedTarget = FindTargetByType(allMembers, TargetType.BeAttacked, 0, SpecialActionType.None);
                // Compare by reference or GameObjectId (safer for IBattleChara)
                if (bePartyAttackedTarget != null && beAllAttackedTarget != null &&
                    bePartyAttackedTarget.GameObjectId == beAllAttackedTarget.GameObjectId)
                {
                    return bePartyAttackedTarget;
                }
            }

            // Fallback: return self
            return Player.Object;
        }

        IBattleChara? FindDancePartner()
        {
            List<Job> dancePartnerPriority = OtherConfiguration.DancePartnerPriority;

            if (!Player.Object.IsJobs(Job.DNC))
            {
                return null;
            }

            if (Player.Object.HasStatus(true, StatusID.ClosedPosition))
            {
                return null;
            }

            if (DancerRotation.IsDancing)
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count < 2)
            {
                return null;
            }

            foreach (Job job in dancePartnerPriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member == Player.Object) continue;
                    if (member.IsJobs(job) && !member.IsDead && !member.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath, StatusID.DancePartner, StatusID.ClosedPosition))
                    {
                        if (member.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindDancePartner: {member.Name} selected target.");
                            return null;
                        }
                        if (!member.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindDancePartner: {member.Name} selected target.");
                            return member;
                        }
                    }
                }
            }

            foreach (Job job in dancePartnerPriority)
            {
                foreach (IBattleChara member in DataCenter.PartyMembers)
                {
                    if (member == Player.Object) continue;
                    if (member.IsJobs(job) && !member.IsDead && !member.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath))
                    {
                        if (member.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindDancePartner: {member.Name} secondary logic target.");
                            return null;
                        }
                        if (!member.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindDancePartner: {member.Name} secondary logic target.");
                            return member;
                        }
                    }
                }
            }

            PluginLog.Debug($"FindDancePartner: No target found, using fallback.");

            IBattleChara? result = RandomMeleeTarget(battleChara);
            if (result != null) return result;
            result = RandomRangeTarget(battleChara);
            if (result != null) return result;
            result = RandomMagicalTarget(battleChara);
            if (result != null) return result;
            result = RandomPhysicalTarget(battleChara);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindTheSpear()
        {
            // The Spear priority based on the info from The Balance Discord (user-configurable)
            List<Job> TheSpearPriority = OtherConfiguration.TheSpearPriority;

            if (!Player.Object.IsJobs(Job.AST))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            // Build filtered candidate list
            List<IBattleChara> candidates = [];
            foreach (IBattleChara m in DataCenter.PartyMembers)
            {
                if (m == null) continue;
                if (m == Player.Object) continue; // never card self
                if (m.IsDead) continue;
                if (m.IsConditionCannotTarget()) continue;
                //if (StatusHelper.IsStatusCapped(m)) continue; // cannot receive more statuses
                if (m.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath)) continue; // poor target value
                if (m.HasStatus(false, StatusID.TheSpear_3889)) continue; // already has a card
                candidates.Add(m);
            }

            // Primary: find the highest-priority match across the whole party
            IBattleChara? best = null;
            int bestIndex = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var member = candidates[i];
                int idx = -1;
                for (int j = 0; j < TheSpearPriority.Count; j++)
                {
                    if (member.IsJobs(TheSpearPriority[j]))
                    {
                        idx = j;
                        break;
                    }
                }
                if (idx >= 0)
                {
                    if (idx < bestIndex || best == null)
                    {
                        bestIndex = idx;
                        best = member;
                    }
                    else if (idx == bestIndex)
                    {
                        // Tie-breakers: higher MaxHp, then higher CurrentHp
                        uint bestMaxHp = best?.MaxHp ?? 0;
                        uint memMaxHp = member.MaxHp;
                        if (memMaxHp > bestMaxHp)
                        {
                            best = member;
                        }
                        else if (memMaxHp == bestMaxHp)
                        {
                            uint bestHp = best?.CurrentHp ?? 0;
                            uint memHp = member.CurrentHp;
                            if (memHp > bestHp)
                            {
                                best = member;
                            }
                        }
                    }
                }
            }
            if (best != null)
            {
                PluginLog.Debug($"FindTheSpear: {best.Name} selected by priority index {bestIndex} with tie-breakers.");
                return best;
            }

            // Fallback: prefer ranged for Spear, then other DPS buckets (aligns with 6% on ranged/healer)
            IBattleChara? result = null;
            result = RandomRangeTarget(candidates);
            if (result != null) return result;
            result = RandomMeleeTarget(candidates);
            if (result != null) return result;
            result = RandomMagicalTarget(candidates);
            if (result != null) return result;
            result = RandomPhysicalTarget(candidates);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindTheBalance()
        {
            // The Balance priority based on the info from The Balance Discord (user-configurable)
            List<Job> TheBalancePriority = OtherConfiguration.TheBalancePriority;

            if (!Player.Object.IsJobs(Job.AST))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            // Build filtered candidate list
            List<IBattleChara> candidates = [];
            foreach (IBattleChara m in DataCenter.PartyMembers)
            {
                if (m == null) continue;
                if (m == Player.Object) continue; // never card self
                if (m.IsDead) continue;
                if (m.IsConditionCannotTarget()) continue;
                //if (StatusHelper.IsStatusCapped(m)) continue; // cannot receive more statuses
                if (m.HasStatus(false, StatusID.DamageDown_2911, StatusID.DamageDown, StatusID.Weakness, StatusID.BrinkOfDeath)) continue; // poor target value
                if (m.HasStatus(false, StatusID.TheBalance_3887)) continue; // already has a card
                candidates.Add(m);
            }

            // Primary: find the highest-priority match across the whole party
            IBattleChara? best = null;
            int bestIndex = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var member = candidates[i];
                int idx = -1;
                for (int j = 0; j < TheBalancePriority.Count; j++)
                {
                    if (member.IsJobs(TheBalancePriority[j]))
                    {
                        idx = j;
                        break;
                    }
                }
                if (idx >= 0)
                {
                    if (idx < bestIndex || best == null)
                    {
                        bestIndex = idx;
                        best = member;
                    }
                    else if (idx == bestIndex)
                    {
                        // Tie-breakers: higher MaxHp, then higher CurrentHp
                        uint bestMaxHp = best?.MaxHp ?? 0;
                        uint memMaxHp = member.MaxHp;
                        if (memMaxHp > bestMaxHp)
                        {
                            best = member;
                        }
                        else if (memMaxHp == bestMaxHp)
                        {
                            uint bestHp = best?.CurrentHp ?? 0;
                            uint memHp = member.CurrentHp;
                            if (memHp > bestHp)
                            {
                                best = member;
                            }
                        }
                    }
                }
            }
            if (best != null)
            {
                PluginLog.Debug($"FindTheBalance: {best.Name} selected by priority index {bestIndex} with tie-breakers.");
                return best;
            }

            // Fallback: prefer melee for Balance, then other DPS buckets (aligns with 6% on melee/tank)
            IBattleChara? result = null;
            result = RandomMeleeTarget(candidates);
            if (result != null) return result;
            result = RandomRangeTarget(candidates);
            if (result != null) return result;
            result = RandomMagicalTarget(candidates);
            if (result != null) return result;
            result = RandomPhysicalTarget(candidates);
            if (result != null) return result;
            return null;
        }

        IBattleChara? FindKardia()
        {
            List<Job> KardiaTankPriority = OtherConfiguration.KardiaTankPriority;

            if (!Player.Object.IsJobs(Job.SGE))
            {
                return null;
            }

            if (DataCenter.PartyMembers == null)
            {
                return null;
            }

            if (DataCenter.PartyMembers.Count == 1)
            {
                return Player.Object;
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 1. Tanks with tank stance and without Kardion
                        if (m.HasStatus(false, StatusHelper.TankStanceStatus) && !m.HasStatus(false, StatusID.Kardion))
                        {
                            if (m.IsConditionCannotTarget())
                            {
                                PluginLog.Debug($"FindKardia 1: {m.Name} is a tank with TankStanceStatus and without Kardion.");
                                return null;
                            }
                            if (!m.IsConditionCannotTarget())
                            {
                                PluginLog.Debug($"FindKardia 1: {m.Name} is a tank with TankStanceStatus and without Kardion.");
                                return m;
                            }
                        }
                    }
                }
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 2. Tanks with tank stance (regardless of Kardion)
                        if (m.HasStatus(false, StatusHelper.TankStanceStatus))
                        {
                            if (m.IsConditionCannotTarget())
                            {
                                PluginLog.Debug($"FindKardia 2: {m.Name} is a tank with TankStanceStatus.");
                                return null;
                            }
                            if (!m.IsConditionCannotTarget())
                            {
                                PluginLog.Debug($"FindKardia 2: {m.Name} is a tank with TankStanceStatus.");
                                return m;
                            }
                        }
                    }
                }
            }

            foreach (Job job in KardiaTankPriority)
            {
                foreach (IBattleChara m in DataCenter.PartyMembers)
                {
                    if (m.IsJobCategory(JobRole.Tank) && m.IsJobs(job) && !m.IsDead)
                    {
                        // 3. Any alive tank in priority order
                        if (m.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindKardia 3: {m.Name} is a tank fallback.");
                            return null;
                        }
                        if (!m.IsConditionCannotTarget())
                        {
                            PluginLog.Debug($"FindKardia 3: {m.Name} is a tank fallback.");
                            return m;
                        }
                    }
                }
            }

            PluginLog.Debug($"FindKardia: No target found, using fallback.");
            IBattleChara? fallback = null;
            fallback = FindTankTarget();
            if (fallback != null) return fallback;
            fallback = RandomMeleeTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomPhysicalTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomRangeTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            fallback = RandomMagicalTarget(DataCenter.PartyMembers);
            if (fallback != null) return fallback;
            return null;
        }

        IBattleChara? FindDeploymentTacticsTarget()
        {
            if (!Player.Object.IsJobs(Job.SCH))
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

            //const float spreadRadius = 30f; // Deployment Tactics spreads within 30y of the target

            foreach (IBattleChara battleChara in DataCenter.PartyMembers)
            {
                if (battleChara == null || battleChara.IsDead)
                {
                    continue;
                }

                uint shield = ObjectHelper.GetObjectShield(battleChara);
                if (shield == 0)
                {
                    // No effective shield to spread
                    continue;
                }

                // Require at least one other party member in spread radius
                //int neighbors = 0;
                //foreach (IBattleChara member in DataCenter.PartyMembers)
                //{
                //    if (member == null || member.IsDead || member.GameObjectId == battleChara.GameObjectId)
                //    {
                //        continue;
                //    }
                //    if (Vector3.Distance(member.Position, battleChara.Position) <= spreadRadius)
                //    {
                //        neighbors++;
                //        if (neighbors >= 1) break;
                //    }
                //}
                //if (neighbors == 0)
                //{
                //    // Nothing to spread to
                //    continue;
                //}

                if (battleChara.HasStatus(true, StatusID.Catalyze))
                {
                    if (bestCatalyze == null || shield > bestCatalyzeShield)
                    {
                        bestCatalyze = battleChara;
                        bestCatalyzeShield = shield;
                    }
                }
                else if (battleChara.HasStatus(true, StatusID.Galvanize))
                {
                    if (bestGalvanize == null || shield > bestGalvanizeShield)
                    {
                        bestGalvanize = battleChara;
                        bestGalvanizeShield = shield;
                    }
                }
            }

            if (bestCatalyze != null)
            {
                PluginLog.Debug($"FindDeploymentTacticsTarget: {bestCatalyze.Name} is a valid target with Catalyze, largest shield, and nearby allies.");
                return bestCatalyze;
            }

            if (bestGalvanize != null && bestCatalyze == null)
            {
                PluginLog.Debug($"FindDeploymentTacticsTarget: {bestGalvanize.Name} is a valid target with Galvanize, largest shield, and nearby allies.");
                return bestGalvanize;
            }

            return null;
        }

        IBattleChara? FindProvokeTarget()
        {
            if (battleChara == null || DataCenter.ProvokeTarget == null)
            {
                return null;
            }

            if (DataCenter.ProvokeTarget != null)
            {
                return DataCenter.ProvokeTarget;
            }

            return null;
        }

        IBattleChara? FindTargetForMoving()
        {
            return Service.Config == null || battleChara == null
                ? null
                : Service.Config.MoveTowardsScreenCenter ? FindMoveTargetScreenCenter() : FindMoveTargetFaceDirection();
            IBattleChara? FindMoveTargetScreenCenter()
            {
                Vector3 pPosition = Player.Object.Position;
                if (!Svc.GameGui.WorldToScreen(pPosition, out Vector2 playerScrPos))
                {
                    return null;
                }

                List<IBattleChara> filteredTargets = [];
                foreach (var t in battleChara)
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving2)
                    {
                        continue;
                    }

                    if (!Svc.GameGui.WorldToScreen(t.Position, out Vector2 scrPos))
                    {
                        continue;
                    }

                    Vector2 dir = scrPos - playerScrPos;

                    if (dir.Y <= 0 && Math.Abs(dir.X / dir.Y) <= Math.Tan(Math.PI * Service.Config.MoveTargetAngle / 360))
                    {
                        filteredTargets.Add(t);
                    }
                }

                filteredTargets.Sort((a, b) => ObjectHelper.DistanceToPlayer(b).CompareTo(ObjectHelper.DistanceToPlayer(a)));

                return filteredTargets.Count > 0 ? filteredTargets[0] : null;
            }

            IBattleChara? FindMoveTargetFaceDirection()
            {
                Vector3 pPosition = Player.Object.Position;
                Vector3 faceVec = Player.Object.GetFaceVector();

                List<IBattleChara> filteredTargets = [];
                foreach (var t in battleChara)
                {
                    if (t.DistanceToPlayer() > Service.Config.DistanceForMoving2)
                    {
                        continue;
                    }

                    Vector3 dir = t.Position - pPosition;
                    float angle = Vector3.Dot(faceVec, Vector3.Normalize(dir));
                    if (angle >= Math.Cos(Math.PI * Service.Config.MoveTargetAngle / 360))
                    {
                        filteredTargets.Add(t);
                    }
                }

                // Sort filteredTargets by ObjectHelper.DistanceToPlayer descending
                filteredTargets.Sort((a, b) => ObjectHelper.DistanceToPlayer(b).CompareTo(ObjectHelper.DistanceToPlayer(a)));

                return filteredTargets.Count > 0 ? filteredTargets[0] : null;
            }
        }

        IBattleChara? FindHealTarget(float healRatio)
        {
            if (battleChara == null)
            {
                return null;
            }

            bool hasAny = false;
            foreach (var _ in battleChara)
            {
                hasAny = true;
                break;
            }
            if (!hasAny || Service.Config == null)
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

            IBattleChara? result = GeneralHealTarget(partyMembers);
            if (result != null) return result;

            result = GeneralHealTarget(filteredGameObjects);
            if (result != null) return result;

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
                List<IBattleChara> healingNeededObjs = [];
                foreach (var o in objs)
                {
                    if (!o.NoNeedHealingInvuln())
                    {
                        healingNeededObjs.Add(o);
                    }
                }
                healingNeededObjs.Sort((a, b) => ObjectHelper.GetHealthRatio(a).CompareTo(ObjectHelper.GetHealthRatio(b)));

                List<IBattleChara> healerTars = [];
                foreach (var o in healingNeededObjs)
                {
                    var enumHealer = TargetFilter.GetJobCategory([o], JobRole.Healer).GetEnumerator();
                    bool isHealer = enumHealer.MoveNext();
                    enumHealer.Dispose();
                    if (isHealer)
                    {
                        healerTars.Add(o);
                    }
                }

                List<IBattleChara> tankTars = [];
                foreach (var o in healingNeededObjs)
                {
                    var enumTank = TargetFilter.GetJobCategory([o], JobRole.Tank).GetEnumerator();
                    bool isTank = enumTank.MoveNext();
                    enumTank.Dispose();
                    if (isTank)
                    {
                        tankTars.Add(o);
                    }
                }

                if (Player.Object.GetHealthRatio() <= Service.Config.HealthSelfRatio)
                {
                    return Player.Object;
                }

                IBattleChara? healerTar = healerTars.Count > 0 ? healerTars[0] : null;
                if (healerTar != null && healerTar.GetHealthRatio() <= Service.Config.HealthHealerRatio)
                {
                    return healerTar;
                }

                IBattleChara? tankTar = tankTars.Count > 0 ? tankTars[0] : null;
                if (tankTar != null && tankTar.GetHealthRatio() <= Service.Config.HealthTankRatio)
                {
                    return tankTar;
                }

                IBattleChara? tar = healingNeededObjs.Count > 0 ? healingNeededObjs[0] : null;
                return tar != null && tar.GetHealthRatio() < 1 ? tar : null;
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
            bool hasAny = false;
            foreach (var _ in battleChara)
            {
                hasAny = true;
                break;
            }
            if (!hasAny)
            {
                return null;
            }

            // Filter out characters marked with stop markers
            if (Service.Config.FilterStopMark && !DataCenter.IsPvP)
            {
                IEnumerable<IBattleChara> filteredCharacters = MarkingHelper.FilterStopCharacters(battleChara);
                // Manual Any() check
                bool filteredHasAny = false;
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

            // Filter high priority hostiles
            var highPriorityHostiles = new List<IBattleChara>();
            foreach (var b in battleChara)
            {
                if (ObjectHelper.IsTopPriorityHostile(b))
                {
                    highPriorityHostiles.Add(b);
                }
            }
            if (highPriorityHostiles.Count > 0)
            {
                battleChara = highPriorityHostiles;
            }

            return FindHostileRaw();
        }

        IBattleChara? FindHostileRaw()
        {
            if (battleChara == null)
            {
                return null;
            }

            // Prepare a list to sort manually
            List<IBattleChara> objects = [.. battleChara];

            List<IBattleChara> filtered;
            switch (DataCenter.TargetingType)
            {
                case TargetingType.Small:
                    if (Service.Config.SmallHp)
                    {
                        // Order by HitboxRadius ascending, then by CurrentHp ascending
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = a.HitboxRadius.CompareTo(b.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : float.MaxValue;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : float.MaxValue;
                            return aHp.CompareTo(bHp);
                        });
                    }
                    else
                    {
                        // Order by HitboxRadius ascending, then by CurrentHp descending
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = a.HitboxRadius.CompareTo(b.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                            return bHp.CompareTo(aHp);
                        });
                    }
                    break;
                case TargetingType.HighHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                        return bHp.CompareTo(aHp);
                    });
                    break;
                case TargetingType.LowHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                        return aHp.CompareTo(bHp);
                    });
                    break;
                case TargetingType.HighHPPercent:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        float aPct = a is IBattleChara ba && ba.MaxHp != 0 ? (float)ba.CurrentHp / ba.MaxHp : 0;
                        float bPct = b is IBattleChara bb && bb.MaxHp != 0 ? (float)bb.CurrentHp / bb.MaxHp : 0;
                        return bPct.CompareTo(aPct);
                    });
                    break;
                case TargetingType.LowHPPercent:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        float aPct = a is IBattleChara ba && ba.MaxHp != 0 ? (float)ba.CurrentHp / ba.MaxHp : 0;
                        float bPct = b is IBattleChara bb && bb.MaxHp != 0 ? (float)bb.CurrentHp / bb.MaxHp : 0;
                        return aPct.CompareTo(bPct);
                    });
                    break;
                case TargetingType.HighMaxHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.MaxHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.MaxHp : 0;
                        return bHp.CompareTo(aHp);
                    });
                    break;
                case TargetingType.LowMaxHP:
                    filtered = [.. objects];
                    filtered.Sort((a, b) =>
                    {
                        uint aHp = a is IBattleChara ba ? ba.MaxHp : 0;
                        uint bHp = b is IBattleChara bb ? bb.MaxHp : 0;
                        return aHp.CompareTo(bHp);
                    });
                    break;
                case TargetingType.Nearest:
                    filtered = [.. objects];
                    filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                    break;
                case TargetingType.Farthest:
                    filtered = [.. objects];
                    filtered.Sort((a, b) => b.DistanceToPlayer().CompareTo(a.DistanceToPlayer()));
                    break;
                case TargetingType.PvPHealers:
                    {
                        // Filter for healers
                        List<IBattleChara> healers = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.Healer.ToJobs()))
                                healers.Add(p);
                        }
                        if (healers.Count > 0)
                        {
                            healers.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = healers;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                case TargetingType.PvPTanks:
                    {
                        List<IBattleChara> tanks = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.Tank.ToJobs()))
                                tanks.Add(p);
                        }
                        if (tanks.Count > 0)
                        {
                            tanks.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = tanks;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                case TargetingType.PvPDPS:
                    {
                        List<IBattleChara> dps = [];
                        foreach (var p in objects)
                        {
                            if (p.IsJobs(JobRole.AllDPS.ToJobs()))
                                dps.Add(p);
                        }
                        if (dps.Count > 0)
                        {
                            dps.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                            filtered = dps;
                        }
                        else
                        {
                            filtered = [.. objects];
                            filtered.Sort((a, b) => a.DistanceToPlayer().CompareTo(b.DistanceToPlayer()));
                        }
                        break;
                    }
                default:
                    if (Service.Config.SmallHp)
                    {
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = b.HitboxRadius.CompareTo(a.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : float.MaxValue;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : float.MaxValue;
                            return aHp.CompareTo(bHp);
                        });
                    }
                    else
                    {
                        filtered = [.. objects];
                        filtered.Sort((a, b) =>
                        {
                            int cmp = b.HitboxRadius.CompareTo(a.HitboxRadius);
                            if (cmp != 0) return cmp;
                            float aHp = a is IBattleChara ba ? ba.CurrentHp : 0;
                            float bHp = b is IBattleChara bb ? bb.CurrentHp : 0;
                            return bHp.CompareTo(aHp);
                        });
                    }
                    break;
            }

            foreach (var obj in filtered)
            {
                if (obj is IBattleChara bc)
                    return bc;
            }
            return null;
        }

        IBattleChara? FindBeAttackedTarget()
        {
            if (battleChara == null)
            {
                return null;
            }

            bool hasAny = false;
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
                // If attachedT is empty, try tanks with TankStanceStatus
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

                // If still empty, try tanks by job category
                if (attachedT.Count == 0)
                {
                    foreach (var tank in battleChara.GetJobCategory(JobRole.Tank))
                    {
                        attachedT.Add(tank);
                    }
                }

                // If still empty, fallback to all
                if (attachedT.Count == 0)
                {
                    foreach (var t in battleChara)
                    {
                        attachedT.Add(t);
                    }
                }
            }

            // Select target based on Priolowtank config
            if (attachedT.Count == 0)
            {
                return null;
            }

            if (Service.Config.Priolowtank)
            {
                IBattleChara? lowest = null;
                float minHealth = float.MaxValue;
                foreach (var t in attachedT)
                {
                    float health = ObjectHelper.GetHealthRatio(t);
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
                IBattleChara? lowest = null;
                float minHealth = float.MaxValue;
                foreach (var t in attachedT)
                {
                    float health = ObjectHelper.GetHealthRatio(t);
                    if (health < minHealth)
                    {
                        minHealth = health;
                        lowest = t;
                    }
                }
                return lowest;
            }
        }

        IBattleChara? FindDispelTarget()
        {
            if (battleChara == null || DataCenter.DispelTarget == null)
            {
                return null;
            }

            return DataCenter.DispelTarget;
        }

        IBattleChara? FindTankTarget()
        {
            return battleChara == null ? null : RandomPickByJobs(battleChara, JobRole.Tank);
        }
    }

    private static IBattleChara? FindMimicryTarget()
    {
        if (DataCenter.AllTargets == null)
        {
            return null;
        }

        IBattleChara? bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (var target in DataCenter.AllTargets)
        {
            if (target != null && IsNeededRole(target))
            {
                float distance = Player.Object != null ? Player.DistanceTo(target.Position) : float.MaxValue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = target;
                }
            }
        }

        return bestTarget;
    }

    private static bool IsNeededRole(IBattleChara character)
    {
        if (character.GameObjectId == Player.Object.GameObjectId)
        {
            return false;
        }

        if (character is not IPlayerCharacter player || player.ClassJob.Value.IsLimitedJob)
        {
            return false;
        }

        CombatRole? neededRole = DataCenter.BluRole;
        return neededRole != null && neededRole != CombatRole.NonCombat && (int)neededRole == (int)player.GetRole();
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
        foreach (JobRole role in roles)
        {
            IBattleChara? tar = RandomPickByJobs(tars, role.ToJobs());
            if (tar != null)
            {
                if (tar.IsConditionCannotTarget()) continue;
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
            if (t.IsConditionCannotTarget()) continue;
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
            if (o == null) continue;
            if (o.IsConditionCannotTarget()) continue;
            list.Add(o);
        }

        int count = list.Count;
        if (count == 0) return null;

        int index = new Random().Next(count);
        return list[index];
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
    FriendMove,
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
    Kardia,
    Deployment,
    PhantomBell,
    PhantomRespite,
    DarkCannon,
    ShockCannon
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// The target result
/// </summary>
/// <param name="Target">the target.</param>
/// <param name="AffectedTargets">the targets that be affected by this action.</param>
/// <param name="Position">the position to use this action.</param>
public readonly record struct TargetResult(IBattleChara Target, IBattleChara[] AffectedTargets, Vector3? Position);