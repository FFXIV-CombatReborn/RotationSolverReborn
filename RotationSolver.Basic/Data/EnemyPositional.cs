namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents the positional relationship of an enemy.
/// </summary>
public enum EnemyPositional : byte
{
	/// <summary>
	/// No specific positional relationship.
	/// </summary>
	None,

	/// <summary>
	/// In the rear of the enemy.
	/// </summary>
	Rear,

	/// <summary>
	/// In the flank of the enemy.
	/// </summary>
	Flank,

	/// <summary>
	/// In front of the enemy.
	/// </summary>
	Front,
}