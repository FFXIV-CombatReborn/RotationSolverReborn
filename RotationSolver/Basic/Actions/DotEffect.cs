namespace RotationSolver.Basic.Actions;

/// <summary>
/// The damage over time effect an action applies through <see cref="ActionSetting.TargetStatusProvide"/>, used to time refreshes from the target's DoT ticks and time to kill.
/// </summary>
public class DotEffect
{
	/// <summary>
	/// Seconds the status lasts when applied.
	/// </summary>
	public float Duration { get; set; }

	/// <summary>
	/// Longest the status can last when reapplying extends it instead of resetting it, such as Death's Design. 0 when reapplying resets it to <see cref="Duration"/>.
	/// </summary>
	public float MaxDuration { get; set; }

	/// <summary>
	/// Potency of the hit dealt when it is applied.
	/// </summary>
	public Func<float>? HitPotency { get; set; }

	/// <summary>
	/// Potency of each 3 second tick.
	/// </summary>
	public Func<float>? TickPotency { get; set; }

	/// <summary>
	/// Extra damage the target takes from you while it lasts, as a fraction (0.1 for 10%). Your damage is valued at <see cref="FillerPotency"/> per GCD.
	/// </summary>
	public float DamageTakenBonus { get; set; }

	/// <summary>
	/// Potency of the GCD this replaces on the same target. When set, the action is skipped while it would deal less than that before the target dies.
	/// </summary>
	public Func<float>? FillerPotency { get; set; }

	/// <summary>
	/// GCDs spent before this action can be used, such as Sage's Eukrasia. They delay the application and add to its cost.
	/// </summary>
	public Func<float>? ExtraGcds { get; set; }
}
