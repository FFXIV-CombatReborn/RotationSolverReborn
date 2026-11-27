using ECommons.Logging;
using static RotationSolver.Rotations.Duties.DutyRotation;

namespace RotationSolver.Configuration.RotationConfig;

/// <summary>
/// Base class for rotation configuration.
/// </summary>
internal abstract class RotationConfigBase : IRotationConfig
{
	private readonly PropertyInfo _property;
	private readonly ICustomRotation? _rotation;
	private readonly DutyRotation? _dutyRotation;

	/// <summary>
	/// Gets the name of the configuration.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the default value of the configuration.
	/// </summary>
	public string DefaultValue { get; }

	/// <summary>
	/// Gets the display name of the configuration.
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// Gets the combat type of the configuration.
	/// </summary>
	public CombatType Type { get; }

	/// <summary>
	/// Gets the Phantom Job for the configuration.
	/// </summary>
	public PhantomJob PhantomJob { get; }

	/// <summary>
	/// Gets the parent of this configuration.
	/// </summary>
	public string Parent { get; }

	/// <summary>
	/// Gets the parent value of this configuration.
	/// </summary>
	public object? ParentValue { get; private set; }

	/// <summary>
	/// Gets or sets the value of the configuration.
	/// </summary>
	public string Value
	{
		get
		{
			if (_dutyRotation != null)
			{
				var dutyKey = GetDutyConfigKey(_dutyRotation);
				if (Service.Config.DutyRotationConfigurations.TryGetValue(dutyKey, out var dutyConfigs))
				{
					lock (dutyConfigs)
					{
						if (dutyConfigs.TryGetValue(Name, out var dutyConfig))
						{
							return dutyConfig;
						}
					}
				}

				return DefaultValue;
			}

			return !Service.Config.RotationConfigurations.TryGetValue(Name, out var config) ? DefaultValue : config;
		}
		set
		{
			if (_dutyRotation != null)
			{
				var dutyKey = GetDutyConfigKey(_dutyRotation);
				var dutyConfigs = Service.Config.DutyRotationConfigurations.GetOrAdd(dutyKey, static _ => []);
				lock (dutyConfigs)
				{
					dutyConfigs[Name] = value;
				}
			}
			else
			{
				Service.Config.RotationConfigurations[Name] = value;
			}

			SetValue(value);
		}
	}

	private static string GetDutyConfigKey(DutyRotation rotation)
	{
		var type = rotation.GetType();
		return type.FullName ?? type.Name;
	}

	/// <summary>
	/// Gets or sets the collection of rotation configurations.
	/// </summary>
	public IEnumerable<IRotationConfig>? Configs { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigBase"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	protected RotationConfigBase(ICustomRotation rotation, PropertyInfo property)
	{
		_property = property ?? throw new ArgumentNullException(nameof(property));
		_rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));

		Name = property.Name;
		DefaultValue = property.GetValue(rotation)?.ToString() ?? string.Empty;
		var attr = property.GetCustomAttribute<RotationConfigAttribute>();
		if (attr != null)
		{
			DisplayName = attr.Name;
			Type = attr.Type;
			Parent = attr.Parent;

			if (attr.ParentValue != null)
			{
				ParentValue = attr.ParentValue;
			}
			else if (!string.IsNullOrEmpty(Parent))
			{
				var parentProperty = rotation.GetType().GetProperty(Parent, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (parentProperty != null)
				{
					ParentValue = parentProperty.GetValue(rotation);
				}
			}
		}
		else
		{
			DisplayName = Name;
			Type = CombatType.None;
			Parent = string.Empty;
		}

		if (Service.Config.RotationConfigurations.TryGetValue(Name, out var value))
		{
			SetValue(value);
		}
	}
	protected RotationConfigBase(DutyRotation rotation, PropertyInfo property)
	{
		_property = property ?? throw new ArgumentNullException(nameof(property));
		_dutyRotation = rotation ?? throw new ArgumentNullException(nameof(rotation));

		Name = property.Name;
		DefaultValue = property.GetValue(rotation)?.ToString() ?? string.Empty;
		var attr = property.GetCustomAttribute<RotationConfigAttribute>();
		if (attr != null)
		{
			DisplayName = attr.Name;
			Type = attr.Type;
			PhantomJob = attr.PhantomJob;
			Parent = attr.Parent;

			if (attr.ParentValue != null)
			{
				ParentValue = attr.ParentValue;
			}
			else if (!string.IsNullOrEmpty(Parent))
			{
				var parentProperty = rotation.GetType().GetProperty(Parent, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (parentProperty != null)
				{
					ParentValue = parentProperty.GetValue(rotation);
				}
			}
		}
		else
		{
			DisplayName = Name;
			Type = CombatType.None;
			Parent = string.Empty;
		}

		var dutyKey = GetDutyConfigKey(rotation);
		if (Service.Config.DutyRotationConfigurations.TryGetValue(dutyKey, out var dutyConfigs))
		{
			string? value;
			lock (dutyConfigs)
			{
				_ = dutyConfigs.TryGetValue(Name, out value);
			}

			if (value != null)
			{
				SetValue(value);
			}
		}
	}

	private void SetValue(string value)
	{
		var type = _property.PropertyType;
		if (type == null)
		{
			return;
		}

		object? currentRotation = _rotation == null ? _dutyRotation : _rotation;
		if (currentRotation == null)
		{
			return;
		}

		try
		{
			_property.SetValue(currentRotation, ChangeType(value, type));
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to convert type: {ex.Message}");
			try
			{
				_property.SetValue(currentRotation, ChangeType(DefaultValue, type));
			}
			catch (Exception fallbackEx)
			{
				PluginLog.Error($"Failed to apply default rotation config '{Name}': {fallbackEx.Message}");
			}
		}
	}

	private static object ChangeType(string value, Type type)
	{
		if (type.IsEnum)
		{
			if (Enum.IsDefined(type, value))
			{
				return Enum.Parse(type, value);
			}

			foreach (var field in type.GetFields())
			{
				var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
				if (descriptionAttribute != null && descriptionAttribute.Description == value)
				{
					return Enum.Parse(type, field.Name);
				}
			}

			PluginLog.Warning($"Invalid enum value '{value}' for type '{type.Name}'. Using default value.");
			return Enum.GetValues(type).GetValue(0) ?? throw new InvalidOperationException($"No default value available for enum type '{type.Name}'.");
		}
		else if (type == typeof(bool))
		{
			return bool.Parse(value);
		}
		else if (type == typeof(float))
		{
			return RotationConfigFloat.TryParseValue(value, out var number)
				? number
				: throw new FormatException($"'{value}' is not a valid number.");
		}

		return Convert.ChangeType(value, type) ?? throw new InvalidOperationException($"Failed to convert value '{value}' to type '{type.Name}'.");
	}

	/// <summary>
	/// Executes a command.
	/// </summary>
	/// <param name="set">The rotation config set.</param>
	/// <param name="str">The command string.</param>
	/// <returns><c>true</c> if the command was executed; otherwise, <c>false</c>.</returns>
	public virtual bool DoCommand(IRotationConfigSet set, string str)
	{
		return IsCommandFor(Name, str);
	}

	internal static bool IsCommandFor(string name, string? str)
	{
		return str != null
			&& str.StartsWith(name, StringComparison.Ordinal)
			&& (str.Length == name.Length || char.IsWhiteSpace(str[name.Length]));
	}

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return Value;
	}
}