namespace RotationSolver.Basic.Data;

internal sealed class FrameCached<T>(Func<T> compute, long ttlMs = 15)
{
	private T _value = default!;
	private long _tick = long.MinValue;

	public T Value
	{
		get
		{
			var now = Environment.TickCount64;
			if (_tick == long.MinValue || now - _tick >= ttlMs)
			{
				_value = compute();
				_tick = now;
			}

			return _value;
		}
	}

	public void Reset()
	{
		_tick = long.MinValue;
	}
}
