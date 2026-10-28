namespace RotationSolver.Basic.Data;

/// <summary>
/// The target a queued command action was issued with: a game object, or a targeting type picked on use.
/// </summary>
/// <param name="ObjectId">The game object, or 0.</param>
/// <param name="TargetingType">The targeting type, or null.</param>
public readonly record struct CommandTarget(ulong ObjectId, TargetingType? TargetingType)
{
	/// <summary>Use the action on a game object.</summary>
	public static CommandTarget FromObject(ulong objectId) => new(objectId, null);

	/// <summary>Pick the target with a targeting type.</summary>
	public static CommandTarget FromTargetingType(TargetingType targetingType) => new(0, targetingType);
}
