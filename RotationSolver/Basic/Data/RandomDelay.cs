namespace RotationSolver.Basic.Data;

/// <summary>
/// Randomly delays the change of a boolean value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RandomDelay"/> struct.
/// </remarks>
/// <param name="getRange">A function that returns the range of delay times.</param>
public struct RandomDelay(Func<(float min, float max)> getRange)
{
	private DateTime _startDelayTime = DateTime.Now;
	private float _delayTime = -1;
	private readonly Random _ran = new Random(DateTime.Now.Millisecond);
	private bool _lastValue = false;

	/// <summary>
	/// Gets the function that returns the range of delay times.
	/// </summary>
	public readonly Func<(float min, float max)> GetRange = getRange ?? throw new ArgumentNullException(nameof(getRange));

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomDelay"/> struct.
	/// </summary>
	/// <param name="getRange">A function that returns the range of delay times as a <see cref="Vector2"/>.</param>
	public RandomDelay(Func<Vector2> getRange)
		: this(() =>
		{
			var vec = getRange();
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
		(var min, var max) = GetRange();
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

		if (_delayTime < 0 && !_lastValue)
		{
			_lastValue = true;
			_startDelayTime = DateTime.Now;
			_delayTime = min + ((float)_ran.NextDouble() * (max - min));
		}
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
		var b = originData != null;

		return Delay(b) ? originData : null;
	}
}