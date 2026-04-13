using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a string rotation configuration.
/// </summary>
internal class RotationConfigString : RotationConfigBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigString"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigString(ICustomRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigString"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigString(DutyRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
	}

	/// <summary>
	/// Executes a command to update the string configuration.
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

		// Ensure the string has sufficient length before slicing
		if (str.Length <= Name.Length)
		{
			return false;
		}

		Value = str[Name.Length..].Trim();

		return true;
	}
}