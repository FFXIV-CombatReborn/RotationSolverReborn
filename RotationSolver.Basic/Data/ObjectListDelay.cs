using System.Collections;

namespace RotationSolver.Basic.Data;

/// <summary>
/// A class to delay the object list checking.
/// </summary>
/// <typeparam name="T">The type of objects in the list.</typeparam>
public class ObjectListDelay<T> : IEnumerable<T> where T : IGameObject
{
	private IEnumerable<T> _list = [];
	private readonly Func<(float min, float max)> _getRange;
	private Dictionary<ulong, DateTime> _revealTime = [];
	private readonly Random _ran = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectListDelay{T}"/> class.
	/// </summary>
	/// <param name="getRange">The function to get the range of delay times.</param>
	public ObjectListDelay(Func<(float min, float max)> getRange)
	{
		_getRange = getRange;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectListDelay{T}"/> class.
	/// </summary>
	/// <param name="getRange">The function to get the range of delay times as a <see cref="Vector2"/>.</param>
	public ObjectListDelay(Func<Vector2> getRange)
		: this(() =>
		{
			Vector2 vec = getRange();
			return (vec.X, vec.Y);
		})
	{
	}

	/// <summary>
	/// Delays the list of objects.
	/// </summary>
	/// <param name="originData">The original list of objects.</param>
	public void Delay(IEnumerable<T> originData)
	{
		List<T> outList = [];
		Dictionary<ulong, DateTime> revealTime = [];
		DateTime now = DateTime.Now;

		foreach (T item in originData)
		{
			if (!_revealTime.TryGetValue(item.GameObjectId, out DateTime time))
			{
				(float min, float max) = _getRange();
				float delaySecond = min + ((float)_ran.NextDouble() * (max - min));
				time = now + TimeSpan.FromMilliseconds(delaySecond * 1000);
			}
			revealTime[item.GameObjectId] = time;

			if (now > time)
			{
				outList.Add(item);
			}
		}

		_list = outList;
		_revealTime = revealTime;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}
}