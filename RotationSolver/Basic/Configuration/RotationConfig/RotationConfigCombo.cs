using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.Basic.Configuration.RotationConfig;

internal class RotationConfigCombo : RotationConfigBase
{
	/// <summary>
	/// Gets the display values for the combo box.
	/// </summary>
	public string[] DisplayValues { get; }

	/// <summary>
	/// Gets the enum names for the combo box.
	/// </summary>
	private string[] EnumNames { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigCombo"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigCombo(ICustomRotation rotation, PropertyInfo property)
	: base(rotation, property)
	{
		if (!property.PropertyType.IsEnum)
		{
			throw new ArgumentException("Property type must be an enum", nameof(property));
		}

		List<string> names = [];
		List<string> enumNames = [];
		foreach (Enum v in Enum.GetValues(property.PropertyType))
		{
			var fieldInfo = property.PropertyType.GetField(v.ToString());
			var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
			names.Add(descriptionAttribute?.Description ?? v.ToString());
			enumNames.Add(v.ToString());
		}

		DisplayValues = [.. names];
		EnumNames = [.. enumNames];

		var defaultEnumValue = property.GetValue(rotation);
		if (defaultEnumValue is Enum defaultEnum)
		{
			var defaultFieldInfo = property.PropertyType.GetField(defaultEnum.ToString());
			var defaultDescriptionAttribute = defaultFieldInfo?.GetCustomAttribute<DescriptionAttribute>();
			Value = defaultDescriptionAttribute?.Description ?? defaultEnum.ToString();
		}
		else
		{
			Value = DisplayValues[0];
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigCombo"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigCombo(DutyRotation rotation, PropertyInfo property)
	: base(rotation, property)
	{
		if (!property.PropertyType.IsEnum)
		{
			throw new ArgumentException("Property type must be an enum", nameof(property));
		}

		List<string> names = [];
		List<string> enumNames = [];
		foreach (Enum v in Enum.GetValues(property.PropertyType))
		{
			var fieldInfo = property.PropertyType.GetField(v.ToString());
			var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
			names.Add(descriptionAttribute?.Description ?? v.ToString());
			enumNames.Add(v.ToString());
		}

		DisplayValues = [.. names];
		EnumNames = [.. enumNames];

		var defaultEnumValue = property.GetValue(rotation);
		if (defaultEnumValue is Enum defaultEnum)
		{
			var defaultFieldInfo = property.PropertyType.GetField(defaultEnum.ToString());
			var defaultDescriptionAttribute = defaultFieldInfo?.GetCustomAttribute<DescriptionAttribute>();
			Value = defaultDescriptionAttribute?.Description ?? defaultEnum.ToString();
		}
		else
		{
			Value = DisplayValues[0];
		}
	}

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString()
	{
		return Value;
	}

	/// <summary>
	/// Executes a command to update the combo box configuration.
	/// </summary>
	/// <param name="set">The rotation config set.</param>
	/// <param name="str">The command string.</param>
	/// <returns><c>true</c> if the command was executed; otherwise, <c>false</c>.</returns>
	public override bool DoCommand(IRotationConfigSet set, string str)
	{
		if (!base.DoCommand(set, str))
		{
			return false;
		}

		Value = DisplayValues[ResolveIndex(DisplayValues, EnumNames, Value, str[Name.Length..].Trim())];
		return true;
	}

	internal static int ResolveIndex(string[] displayValues, string[] enumNames, string current, string arg)
	{
		var length = displayValues.Length;

		if (int.TryParse(arg, out var num))
		{
			return ((num % length) + length) % length;
		}

		for (var i = 0; i < length; i++)
		{
			if (displayValues[i].Equals(arg, StringComparison.OrdinalIgnoreCase) ||
				enumNames[i].Equals(arg, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return (Math.Max(Array.IndexOf(displayValues, current), 0) + 1) % length;
	}
}