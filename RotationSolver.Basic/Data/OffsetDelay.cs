namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents a delay in changing a boolean value.
/// </summary>
public struct OffsetDelay
{
	private bool _lastValue;
	private bool _nowValue;
	private readonly Queue<DateTime> _changeTimes;

	/// <summary>
	/// Initializes a new instance of the <see cref="OffsetDelay"/> struct.
	/// </summary>
	/// <param name="getDelay">A function that returns the delay in seconds.</param>
	public OffsetDelay(Func<float> getDelay)
	{
		_lastValue = false;
		_nowValue = false;
		_changeTimes = new Queue<DateTime>();
		GetDelay = getDelay ?? throw new ArgumentNullException(nameof(getDelay));
	}

	/// <summary>
	/// Gets the function that returns the delay in seconds.
	/// </summary>
	public Func<float> GetDelay { get; }

	/// <summary>
	/// Delays the change of the boolean value.
	/// </summary>
	/// <param name="originData">The original boolean value.</param>
	/// <returns>The delayed boolean value.</returns>
	public bool Delay(bool originData)
	{
		if (originData != _lastValue)
		{
			_lastValue = originData;
			_changeTimes.Enqueue(DateTime.Now + TimeSpan.FromSeconds(GetDelay()));
		}

		if (_changeTimes.TryPeek(out DateTime time) && time < DateTime.Now)
		{
			_ = _changeTimes.Dequeue();
			_nowValue = !_nowValue;
		}

		return _nowValue;
	}
}