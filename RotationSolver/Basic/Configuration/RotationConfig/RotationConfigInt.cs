using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.Basic.Configuration.RotationConfig;

internal class RotationConfigInt : RotationConfigBase
{
	/// <summary>
	/// Gets or sets the minimum value.
	/// </summary>
	public int Min { get; set; }

	/// <summary>
	/// Gets or sets the maximum value.
	/// </summary>
	public int Max { get; set; }

	/// <summary>
	/// Gets or sets the speed value.
	/// </summary>
	public int Speed { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigInt"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigInt(ICustomRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
		var attr = property.GetCustomAttribute<RangeAttribute>();
		if (attr != null)
		{
			Min = (int)attr.MinValue;
			Max = (int)attr.MaxValue;
			Speed = (int)attr.Speed;
		}
		else
		{
			Min = 0;
			Max = 10;
			Speed = 1;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigInt"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigInt(DutyRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
		var attr = property.GetCustomAttribute<RangeAttribute>();
		if (attr != null)
		{
			Min = (int)attr.MinValue;
			Max = (int)attr.MaxValue;
			Speed = (int)attr.Speed;
		}
		else
		{
			Min = 0;
			Max = 10;
			Speed = 1;
		}
	}

	/// <summary>
	/// Executes a command to update the integer configuration.
	/// </summary>
	/// <param name="set">The rotation config set.</param>
	/// <param name="str">The command string.</param>
	/// <returns><c>true</c> if the command was executed; otherwise, <c>false</c>.</returns>
	public override bool DoCommand(IRotationConfigSet set, string str)
	{
		if (str == null)
		{
			return false;
		}

		if (!base.DoCommand(set, str))
		{
			return false;
		}

		if (str.Length <= Name.Length)
		{
			return false;
		}

		var numStr = str[Name.Length..].Trim();

		if (int.TryParse(numStr, out var parsedValue))
		{
			Value = parsedValue.ToString();
			return true;
		}

		return false;
	}
}