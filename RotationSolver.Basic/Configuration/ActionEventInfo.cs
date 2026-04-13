namespace RotationSolver.Basic.Configuration;

/// <summary>
/// Represents information about a special action event.
/// </summary>
public class ActionEventInfo : MacroInfo
{
	/// <summary>
	/// Gets or sets the name of the action.
	/// </summary>
	public string Name { get; set; } = string.Empty;
}