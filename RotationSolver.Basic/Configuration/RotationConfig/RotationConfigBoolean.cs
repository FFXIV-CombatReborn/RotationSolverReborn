using ECommons.Logging;
using RotationSolver.Basic.Rotations.Duties;

namespace RotationSolver.Basic.Configuration.RotationConfig;

/// <summary>
/// Represents a boolean rotation configuration.
/// </summary>
internal class RotationConfigBoolean : RotationConfigBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigBoolean"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigBoolean(ICustomRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationConfigBoolean"/> class.
	/// </summary>
	/// <param name="rotation">The custom rotation instance.</param>
	/// <param name="property">The property information.</param>
	public RotationConfigBoolean(DutyRotation rotation, PropertyInfo property)
		: base(rotation, property)
	{
	}

	/// <summary>
	/// Executes a command to update the boolean configuration.
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

		string numStr = str[Name.Length..].Trim();

		if (bool.TryParse(numStr, out bool result))
		{
			Value = result.ToString();
		}
		else
		{
			try
			{
				Value = (!bool.Parse(Value)).ToString();
			}
			catch (Exception ex)
			{
				PluginLog.Error($"Failed to parse boolean value: {ex.Message}");
				return false;
			}
		}
		return true;
	}
}