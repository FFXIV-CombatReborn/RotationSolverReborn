using ECommons.Logging;
using RotationSolver.Basic.Rotations.Duties;
using System.Collections;

namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a set of rotation configurations.
/// </summary>
public class RotationConfigSet : IRotationConfigSet
{
	/// <summary>
	/// Gets the collection of rotation configurations.
	/// </summary>
	public HashSet<IRotationConfig> Configs { get; } = new HashSet<IRotationConfig>(new RotationConfigComparer());

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigSet"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotation"/> is <c>null</c>.</exception>
	public RotationConfigSet(ICustomRotation rotation)
	{
		if (rotation == null)
		{
			throw new ArgumentNullException(nameof(rotation));
		}

		foreach (PropertyInfo prop in rotation.GetType().GetRuntimeProperties())
		{
			RotationConfigAttribute? attr = prop.GetCustomAttribute<RotationConfigAttribute>();
			if (attr == null)
			{
				continue;
			}

			Type type = prop.PropertyType;
			if (type == null)
			{
				continue;
			}

			if (type == typeof(bool))
			{
				_ = Configs.Add(new RotationConfigBoolean(rotation, prop));
			}
			else if (type.IsEnum)
			{
				_ = Configs.Add(new RotationConfigCombo(rotation, prop));
			}
			else if (type == typeof(float))
			{
				_ = Configs.Add(new RotationConfigFloat(rotation, prop));
			}
			else if (type == typeof(int))
			{
				_ = Configs.Add(new RotationConfigInt(rotation, prop));
			}
			else if (type == typeof(string))
			{
				_ = Configs.Add(new RotationConfigString(rotation, prop));
			}
			else
			{
				PluginLog.Error($"Failed to find the rotation config type for property '{prop.Name}' with type '{type.FullName ?? type.Name}'");
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigSet"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotation"/> is <c>null</c>.</exception>
	public RotationConfigSet(DutyRotation rotation)
	{
		if (rotation == null)
		{
			throw new ArgumentNullException(nameof(rotation));
		}

		foreach (PropertyInfo prop in rotation.GetType().GetRuntimeProperties())
		{
			RotationConfigAttribute? attr = prop.GetCustomAttribute<RotationConfigAttribute>();
			if (attr == null)
			{
				continue;
			}

			Type type = prop.PropertyType;
			if (type == null)
			{
				continue;
			}

			if (type == typeof(bool))
			{
				_ = Configs.Add(new RotationConfigBoolean(rotation, prop));
			}
			else if (type.IsEnum)
			{
				_ = Configs.Add(new RotationConfigCombo(rotation, prop));
			}
			else if (type == typeof(float))
			{
				_ = Configs.Add(new RotationConfigFloat(rotation, prop));
			}
			else if (type == typeof(int))
			{
				_ = Configs.Add(new RotationConfigInt(rotation, prop));
			}
			else if (type == typeof(string))
			{
				_ = Configs.Add(new RotationConfigString(rotation, prop));
			}
			else
			{
				PluginLog.Error($"Failed to find the rotation config type for property '{prop.Name}' with type '{type.FullName ?? type.Name}'");
			}
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public IEnumerator<IRotationConfig> GetEnumerator()
	{
		return Configs.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through a collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return Configs.GetEnumerator();
	}
}