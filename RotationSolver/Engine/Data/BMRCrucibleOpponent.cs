namespace RotationSolver.Engine.Data;

/// <summary>
///
/// </summary>
public sealed record BMRCrucibleOpponent(
	string Board,
	int BoardOrder,
	uint GroupID,
	string Opponent,
	int SortOrder,
	string ModuleType,
	List<BMRPreferredKinship> Kinships);

/// <summary>
///
/// </summary>
public readonly record struct BMRPreferredKinship(int Kinship, string Reason);
