namespace RotationSolver.Basic.Data;

/// <summary>
/// Randomly delays the change of a boolean value.
/// </summary>
public struct RandomDelay
{
	private DateTime _startDelayTime;
	private float _delayTime;
	private readonly Random _ran;
	private bool _lastValue;

	/// <summary>
	/// Gets the function that returns the range of delay times.
	/// </summary>
	public readonly Func<(float min, float max)> GetRange;

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomDelay"/> struct.
	/// </summary>
	/// <param name="getRange">A function that returns the range of delay times.</param>
	public RandomDelay(Func<(float min, float max)> getRange)
	{
		_startDelayTime = DateTime.Now;
		_delayTime = -1;
		_ran = new Random(DateTime.Now.Millisecond);
		_lastValue = false;
		GetRange = getRange ?? throw new ArgumentNullException(nameof(getRange));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomDelay"/> struct.
	/// </summary>
	/// <param name="getRange">A function that returns the range of delay times as a <see cref="Vector2"/>.</param>
	public RandomDelay(Func<Vector2> getRange)
		: this(() =>
		{
			Vector2 vec = getRange();
			return (vec.X, vec.Y);
		})
	{
	}

	/// <summary>
	/// Delays the change of the boolean value.
	/// </summary>
	/// <param name="originData">The original boolean value.</param>
	/// <returns>The delayed boolean value.</returns>
	public bool Delay(bool originData)
	{
		(float min, float max) = GetRange();
		if (min <= 0 || max <= 0)
		{
			return originData;
		}

		if (!originData)
		{
			_lastValue = false;
			_delayTime = -1;
			return false;
		}

		// Not started and changed.
		if (_delayTime < 0 && !_lastValue)
		{
			_lastValue = true;
			_startDelayTime = DateTime.Now;
			_delayTime = min + ((float)_ran.NextDouble() * (max - min));
		}
		// Time's up
		else if ((DateTime.Now - _startDelayTime).TotalSeconds >= _delayTime)
		{
			_delayTime = -1;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Delays the retrieval of an item.
	/// </summary>
	/// <typeparam name="T">The type of the item.</typeparam>
	/// <param name="originData">The original item.</param>
	/// <returns>The delayed item.</returns>
	public T? Delay<T>(T? originData) where T : class
	{
		bool b = originData != null;

		return Delay(b) ? originData : null;
	}
}