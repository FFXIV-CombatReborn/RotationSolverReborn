namespace RotationSolver.Basic.Actions;

/// <summary>
/// The cd information.
/// </summary>
public interface ICooldown
{
	internal float RecastTimeOneChargeRaw { get; }
	internal float RecastTimeElapsedRaw { get; }

	/// <summary>
	/// Is still in cooling down.
	/// </summary>
	bool IsCoolingDown { get; }

	/// <summary>
	/// The mac charges.
	/// </summary>
	ushort MaxCharges { get; }

	/// <summary>
	/// The current charges.
	/// </summary>
	ushort CurrentCharges { get; }
}
