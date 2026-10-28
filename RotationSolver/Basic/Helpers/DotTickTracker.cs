namespace RotationSolver.Basic.Helpers;

internal static class DotTickTracker
{
	internal const long TickMs = 3000;

	private const long StaleMs = 300_000;
	private const int PruneAt = 256;

	private static readonly Dictionary<ulong, long> _lastTicks = [];

	internal static void RecordTick(ulong id, long now)
	{
		if (_lastTicks.Count >= PruneAt)
		{
			foreach ((var key, var tick) in _lastTicks)
			{
				if (now - tick > StaleMs)
				{
					_ = _lastTicks.Remove(key);
				}
			}
		}

		_lastTicks[id] = now;
	}

	internal static bool TryGetNextTick(ulong id, long now, out long nextTick)
	{
		if (!_lastTicks.TryGetValue(id, out var last))
		{
			nextTick = 0;
			return false;
		}

		nextTick = last + ((FloorDiv(now - last, TickMs) + 1) * TickMs);
		return true;
	}

	internal static float CountTicks(ulong id, long from, long to)
	{
		if (to <= from)
		{
			return 0;
		}

		return _lastTicks.TryGetValue(id, out var last)
			? FloorDiv(to - last, TickMs) - FloorDiv(from - last, TickMs)
			: (to - from) / (float)TickMs;
	}

	internal static void Clear()
	{
		_lastTicks.Clear();
	}

	private static long FloorDiv(long value, long divisor)
	{
		return (long)Math.Floor((double)value / divisor);
	}
}
