using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace RotationSolver.Basic.Helpers
{
    /// <summary>
    /// Target helper to get calculated targets within range with additional caching within a data refresh interval.
    /// </summary>
    public static class TargetHelper
    {
        private static readonly HashSet<long> s_emptyStopTargets = [];

        /// <summary>
        /// Retrieves a collection of valid battle characters that can be targeted based on the specified criteria.
        /// </summary>
        /// <param name="range">The range to check for targets.</param>
        /// <param name="getFriendly">If set to <c>true</c>, gets party members; if set to <c>false</c> gets hostiles. Set to null gets all.</param>
        /// <returns>
        /// A <see cref="List{IBattleChara}"/> containing the valid targets.
        /// </returns>
        public static List<IBattleChara> GetTargetsByRange(float range, bool? getFriendly = null)
        {
            if (DataCenter.AllTargets == null) return [];

            float searchRange = range;

            // Build a non-colliding cache key without mutating input range
            float groupBias = getFriendly == true ? 1000f : getFriendly == false ? 2000f : 0f;
            float cacheKey = MathF.Round(searchRange, 0) + groupBias;

            if (DataCenter.TargetsByRange.TryGetValue(cacheKey, out List<IBattleChara>? cachedTargets))
            {
                return cachedTargets;
            }

            // Pre-size target list based on expected source set
            int capacity = getFriendly == true
                ? (DataCenter.PartyMembers?.Count ?? 0)
                : getFriendly == false
                    ? (DataCenter.AllHostileTargets?.Count ?? 0)
                    : (DataCenter.AllTargets?.Count ?? 0);
            List<IBattleChara> targets = capacity > 0 ? new List<IBattleChara>(capacity) : [];

            var blacklisted = new HashSet<uint>(DataCenter.BlacklistedNameIds);
            HashSet<long> stopTargets = Service.Config.FilterStopMark
                ? [.. MarkingHelper.GetStopTargets()]
                : s_emptyStopTargets;

            if (getFriendly == true)
            {
                if (DataCenter.PartyMembers != null)
                {
                    foreach (IBattleChara target in DataCenter.PartyMembers.GetObjectInRadius(searchRange))
                    {
                        if (!ValidityCheck(target, blacklisted, stopTargets)) continue;
                        targets.Add(target);
                    }
                }
            }
            else if (getFriendly == false)
            {
                if (DataCenter.AllHostileTargets != null)
                {
                    foreach (IBattleChara target in DataCenter.AllHostileTargets.GetObjectInRadius(searchRange))
                    {
                        if (!ValidityCheck(target, blacklisted, stopTargets)) continue;
                        targets.Add(target);
                    }
                }
            }
            else
            {
                if (DataCenter.AllTargets != null)
                {
                    foreach (IBattleChara target in DataCenter.AllTargets.GetObjectInRadius(searchRange))
                    {
                        if (!ValidityCheck(target, blacklisted, stopTargets)) continue;
                        targets.Add(target);
                    }
                }
            }

            DataCenter.TargetsByRange[cacheKey] = targets;
            return targets;
        }

        /// <summary>
        /// Retrieves targets for an action considering both cast range and effect range.
        /// If castRange is 0 (self-centered AoE), effectRange is used to gather candidates.
        /// </summary>
        public static List<IBattleChara> GetTargetsByCastAndEffect(float castRange, float effectRange, bool? getFriendly = null)
        {
            float searchRange = castRange > 0 ? castRange : MathF.Max(effectRange, 0);
            return GetTargetsByRange(searchRange, getFriendly);
        }

        /// <summary>
        /// Get targets currently usable by a specific action (defers to ActionManager)
        /// </summary>
        public static unsafe List<IBattleChara> GetTargetsUsableByAction(float range, uint actionId, bool? getFriendly = null, bool checkRangeAndLoS = true)
        {
            var candidates = GetTargetsByRange(range, getFriendly);
            if (candidates.Count == 0) return candidates;

            List<IBattleChara> usable = [];
            foreach (var t in candidates)
            {
                if (IsUsableByAction(t, actionId, checkRangeAndLoS))
                {
                    usable.Add(t);
                }
            }
            return usable;
        }

        /// <summary>
        /// Get targets currently usable by a specific action, considering both cast range and effect range.
        /// Uses effectRange when castRange is 0 (self-centered AoE).
        /// </summary>
        public static unsafe List<IBattleChara> GetTargetsUsableByAction(float castRange, float effectRange, uint actionId, bool? getFriendly = null, bool checkRangeAndLoS = true)
        {
            float searchRange = castRange > 0 ? castRange : MathF.Max(effectRange, 0);
            var candidates = GetTargetsByRange(searchRange, getFriendly);
            if (candidates.Count == 0) return candidates;

            List<IBattleChara> usable = [];
            foreach (var t in candidates)
            {
                if (IsUsableByAction(t, actionId, checkRangeAndLoS))
                {
                    usable.Add(t);
                }
            }
            return usable;
        }

        /// <summary>
        /// Performs a general check on the specified battle character to determine if it meets the criteria for targeting.
        /// </summary>
        /// <param name="battleChara">The battle character to check.</param>
        /// <param name="blacklisted"></param>
        /// <param name="stopTargets"></param>
        /// <returns>
        /// <c>true</c> if the battle character meets the criteria for targeting; otherwise, <c>false</c>.
        /// </returns>
        private static bool ValidityCheck(IBattleChara battleChara, HashSet<uint> blacklisted, HashSet<long> stopTargets)
        {
            if (battleChara == null) return false;

            unsafe
            {
                if (battleChara.Struct() == null) return false;
            }

            if (!battleChara.IsTargetable) return false;

            if (!battleChara.IsEnemy() && battleChara.IsConditionCannotTarget()) return false;

            try
            {
                if (battleChara.StatusList == null) return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception accessing StatusList for {battleChara?.NameId}: {ex.Message}");
                return false;
            }

            if (blacklisted.Contains(battleChara.NameId)) return false;

            if (battleChara.IsEnemy() && !battleChara.IsAttackable()) return false;

            // Respect stop marks only when configured
            if (Service.Config.FilterStopMark && stopTargets.Contains((long)battleChara.GameObjectId) && battleChara.IsEnemy())
                return false;

            return true;
        }

        private static unsafe bool IsUsableByAction(IBattleChara target, uint actionId, bool checkRangeAndLoS)
        {
            if (target == null) return false;

            var am = ActionManager.Instance();
            if (am == null) return false;

            uint adjusted = am->GetAdjustedActionId(actionId);

            if (!am->IsActionOffCooldown(ActionType.Action, adjusted))
                return false;

            var go = (GameObject*)target.Struct();
            if (go == null) return false;

            if (checkRangeAndLoS)
            {
                var player = Svc.Objects.LocalPlayer;
                if (player == null) return false;
                var playerPtr = (GameObject*)player.Address;
                var err = ActionManager.GetActionInRangeOrLoS(adjusted, playerPtr, go);
                if (err != 0 && err != 565)
                    return false;
            }

            return ActionManager.CanUseActionOnTarget(adjusted, go);
        }
    }
}