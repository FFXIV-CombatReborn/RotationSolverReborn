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
	private readonly Dictionary<ulong, DateTime> _revealTime = [];
	private readonly Dictionary<ulong, DateTime> _lastSeen = [];
	private readonly Random _ran = new();

	/// <summary>
	/// How long an object's reveal timer is retained after it briefly disappears from
	/// <see cref="Delay(IEnumerable{T})"/>'s input (e.g. a single-frame filter flicker),
	/// so it is not mistakenly treated as a brand-new object and re-delayed.
	/// </summary>
	private static readonly TimeSpan StaleGracePeriod = TimeSpan.FromSeconds(2);

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
			var vec = getRange();
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
		var now = DateTime.Now;

		foreach (var item in originData)
		{
			var id = item.GameObjectId;
			_lastSeen[id] = now;

			if (!_revealTime.TryGetValue(id, out var time))
			{
				(var min, var max) = _getRange();
				var delaySecond = min + ((float)_ran.NextDouble() * (max - min));
				time = now + TimeSpan.FromMilliseconds(delaySecond * 1000);
				_revealTime[id] = time;
			}

			if (now > time)
			{
				outList.Add(item);
			}
		}

		// Prune objects that have genuinely been absent for a while so this doesn't grow
		// unbounded. Anything within the grace period is kept even if it wasn't present in
		// this particular call, so a single-frame absence doesn't reset its delay timer.
		if (_lastSeen.Count > 0)
		{
			List<ulong>? stale = null;
			foreach (var kvp in _lastSeen)
			{
				if (now - kvp.Value > StaleGracePeriod)
				{
					(stale ??= []).Add(kvp.Key);
				}
			}

			if (stale != null)
			{
				foreach (var id in stale)
				{
					_lastSeen.Remove(id);
					_revealTime.Remove(id);
				}
			}
		}

		_list = outList;
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