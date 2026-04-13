using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a float rotation configuration.
/// </summary>
internal class RotationConfigFloat : RotationConfigBase
{
	/// <summary>
	/// Gets or sets the minimum value.
	/// </summary>
	public float Min { get; set; }

	/// <summary>
	/// Gets or sets the maximum value.
	/// </summary>
	public float Max { get; set; }

	/// <summary>
	/// Gets or sets the speed value.
	/// </summary>
	public float Speed { get; set; }

	/// <summary>
	/// Gets or sets the unit type.
	/// </summary>
	public ConfigUnitType UnitType { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigFloat"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigFloat(ICustomRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
		RangeAttribute? attr = property.GetCustomAttribute<RangeAttribute>();
		if (attr != null)
		{
			Min = attr.MinValue;
			Max = attr.MaxValue;
			Speed = attr.Speed;
			UnitType = attr.UnitType;
		}
		else
		{
			Min = 0.0f;
			Max = 1.0f;
			Speed = 0.005f;
			UnitType = ConfigUnitType.Percent;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigFloat"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigFloat(DutyRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
		RangeAttribute? attr = property.GetCustomAttribute<RangeAttribute>();
		if (attr != null)
		{
			Min = attr.MinValue;
			Max = attr.MaxValue;
			Speed = attr.Speed;
			UnitType = attr.UnitType;
		}
		else
		{
			Min = 0.0f;
			Max = 1.0f;
			Speed = 0.005f;
			UnitType = ConfigUnitType.Percent;
		}
	}

	/// <summary>
	/// Executes a command to update the float configuration.
	/// </summary>
	/// <param name="set">The rotation config set.</param>
	/// <param name="str">The command string.</param>
	/// <returns><c>true</c> if the command was executed; otherwise, <c>false</c>.</returns>
	public override bool DoCommand(IRotationConfigSet set, string str)
	{
		if (str == null || !base.DoCommand(set, str) || str.Length <= Name.Length)
		{
			return false;
		}

		string numStr = str[Name.Length..].Trim();

		if (float.TryParse(numStr, out float parsedValue))
		{
			if (UnitType == ConfigUnitType.Percent)
			{
				parsedValue /= 100.0f;
			}
			Value = parsedValue.ToString();
			return true;
		}

		return false;
	}
}