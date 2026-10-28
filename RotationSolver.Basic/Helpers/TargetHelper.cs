using ECommons.GameFunctions;
using ECommons.Logging;

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
			var source = getFriendly switch
			{
				true => DataCenter.PartyMembers,
				false => DataCenter.AllHostileTargets,
				null => DataCenter.AllTargets,
			};

			if (source == null)
			{
				return [];
			}

			// Build a non-colliding cache key without mutating input range
			var groupBias = getFriendly == true ? 1000f : getFriendly == false ? 2000f : 0f;
			var cacheKey = MathF.Round(range, 0) + groupBias;

			if (DataCenter.TargetsByRange.TryGetValue(cacheKey, out var cachedTargets))
			{
				return cachedTargets;
			}

			var candidates = getFriendly.HasValue ? GetValidTargets(source, groupBias) : source;
			var targets = new List<IBattleChara>(candidates.Count);
			HashSet<uint>? blacklisted = null;
			HashSet<long>? stopTargets = null;
			foreach (var target in candidates.GetObjectInRadius(range))
			{
				if (getFriendly.HasValue || ValidityCheck(target, blacklisted ??= GetBlacklisted(), stopTargets ??= GetStopTargets()))
				{
					targets.Add(target);
				}
			}

			DataCenter.TargetsByRange[cacheKey] = targets;
			return targets;
		}

		private static List<IBattleChara> GetValidTargets(List<IBattleChara> source, float groupBias)
		{
			var validKey = -1f - groupBias;
			if (DataCenter.TargetsByRange.TryGetValue(validKey, out var valid))
			{
				return valid;
			}

			var blacklisted = GetBlacklisted();
			var stopTargets = GetStopTargets();
			valid = new List<IBattleChara>(source.Count);
			foreach (var target in source)
			{
				if (ValidityCheck(target, blacklisted, stopTargets))
				{
					valid.Add(target);
				}
			}

			DataCenter.TargetsByRange[validKey] = valid;
			return valid;
		}

		private static HashSet<uint> GetBlacklisted()
		{
			return [.. DataCenter.BlacklistedNameIds];
		}

		private static HashSet<long> GetStopTargets()
		{
			return Service.Config.FilterStopMark2 ? [.. MarkingHelper.GetStopTargets()] : s_emptyStopTargets;
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
			if (battleChara == null)
			{
				return false;
			}

			unsafe
			{
				if (battleChara.Struct() == null)
				{
					return false;
				}
			}

			if (!battleChara.IsTargetable)
			{
				return false;
			}

			// IsEnemy asks the game, so it is only asked once.
			var isEnemy = battleChara.IsEnemy();
			if (!isEnemy && battleChara.IsConditionCannotTarget())
			{
				return false;
			}

			try
			{
				if (battleChara.StatusList == null)
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Exception accessing StatusList for {battleChara?.NameId}: {ex.Message}");
				return false;
			}

			if (blacklisted.Contains(battleChara.NameId))
			{
				return false;
			}

			if (isEnemy && !battleChara.IsAttackable())
			{
				return false;
			}

			// Respect stop marks only when configured
			return !Service.Config.FilterStopMark2 || !isEnemy || !stopTargets.Contains((long)battleChara.GameObjectId);
		}
	}
}
