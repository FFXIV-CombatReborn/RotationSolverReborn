using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace RotationSolver.Basic.Helpers
{
	/// <summary>
	/// Enum representing different head markers.
	/// </summary>
	internal enum HeadMarker : byte
	{
		Attack1,
		Attack2,
		Attack3,
		Attack4,
		Attack5,
		Bind1,
		Bind2,
		Bind3,
		Stop1,
		Stop2,
		Square,
		Circle,
		Cross,
		Triangle,
		Attack6,
		Attack7,
		Attack8,
	}

	/// <summary>
	/// Helper class for managing head markers.
	/// </summary>
	internal class MarkingHelper
	{
		private static readonly object _lock = new();

		/// <summary>
		/// Gets the marker for the specified head marker index.
		/// </summary>
		/// <param name="index">The head marker index.</param>
		/// <returns>The object ID of the marker.</returns>
		internal static unsafe long GetMarker(HeadMarker index)
		{
			var instance = MarkingController.Instance();
			return instance == null || instance->Markers.Length == 0 ? 0 : instance->Markers[(int)index].ObjectId;
		}

		/// <summary>
		/// Gets a value indicating whether there are any attack characters.
		/// </summary>
		internal static bool HaveAttackChara
		{
			get
			{
				var targets = GetAttackSignTargets();
				for (var i = 0; i < targets.Length; i++)
				{
					if (targets[i] != 0)
					{
						return true;
					}
				}
				return false;
			}
		}

		private static readonly long[] _attackSignTargets = new long[8];
		private static long _attackSignTargetsTick = long.MinValue;
		private static readonly long[] _stopTargets = new long[2];
		private static long _stopTargetsTick = long.MinValue;
		private const long MarkerCacheTtlMs = 15;

		/// <summary>
		/// Gets the attack sign targets. Re-read from the game at most once per frame.
		/// </summary>
		internal static long[] GetAttackSignTargets()
		{
			var now = Environment.TickCount64;
			if (_attackSignTargetsTick != long.MinValue && now - _attackSignTargetsTick < MarkerCacheTtlMs)
			{
				return _attackSignTargets;
			}

			_attackSignTargets[0] = GetMarker(HeadMarker.Attack1);
			_attackSignTargets[1] = GetMarker(HeadMarker.Attack2);
			_attackSignTargets[2] = GetMarker(HeadMarker.Attack3);
			_attackSignTargets[3] = GetMarker(HeadMarker.Attack4);
			_attackSignTargets[4] = GetMarker(HeadMarker.Attack5);
			_attackSignTargets[5] = GetMarker(HeadMarker.Attack6);
			_attackSignTargets[6] = GetMarker(HeadMarker.Attack7);
			_attackSignTargets[7] = GetMarker(HeadMarker.Attack8);
			_attackSignTargetsTick = now;
			return _attackSignTargets;
		}

		/// <summary>
		/// Gets the stop targets. Re-read from the game at most once per frame.
		/// </summary>
		internal static long[] GetStopTargets()
		{
			var now = Environment.TickCount64;
			if (_stopTargetsTick != long.MinValue && now - _stopTargetsTick < MarkerCacheTtlMs)
			{
				return _stopTargets;
			}

			_stopTargets[0] = GetMarker(HeadMarker.Stop1);
			_stopTargets[1] = GetMarker(HeadMarker.Stop2);
			_stopTargetsTick = now;
			return _stopTargets;
		}

		/// <summary>
		/// Filters out characters that have stop markers, but keeps player characters.
		/// </summary>
		/// <param name="charas">The characters to filter.</param>
		/// <returns>The filtered characters.</returns>
		internal static unsafe IEnumerable<IBattleChara> FilterStopCharacters(IEnumerable<IBattleChara> charas)
		{
			var stopTargets = GetStopTargets();

			var capacity = (charas as ICollection<IBattleChara>)?.Count ?? 0;
			var result = capacity > 0 ? new List<IBattleChara>(capacity) : [];
			foreach (var b in charas)
			{
				// Keep all player characters even if they are marked with stop markers
				if (!b.IsEnemy())
				{
					result.Add(b);
					continue;
				}

				var isStopTarget = false;
				var charaId = (long)b.GameObjectId;
				for (var i = 0; i < stopTargets.Length; i++)
				{
					if (stopTargets[i] != 0 && stopTargets[i] == charaId)
					{
						isStopTarget = true;
						break;
					}
				}

				if (!isStopTarget)
				{
					result.Add(b);
				}
			}
			return result;
		}
	}
}