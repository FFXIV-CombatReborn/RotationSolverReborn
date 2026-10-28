namespace RotationSolver.Basic;

/// <summary>
/// Represents an entity that can determine if the player's level is sufficient for an action.
/// </summary>
public interface IEnoughLevel
{
	/// <summary>
	/// Gets a value indicating whether the player's level is sufficient for this action's usage.
	/// </summary>
	bool EnoughLevel { get; }

	/// <summary>
	/// Gets the required level for the action.
	/// </summary>
	byte Level { get; }
}