namespace RotationSolver.Basic.Tweaks;

/// <summary>
/// Framerate-dependent cooldown reduction.
/// Imagine game is running at exactly 100fps (10ms frame time), and action is queued when remaining cooldown is 5ms.
/// On next frame (+10ms), cooldown will be reduced and clamped to 0, action will be executed and it's cooldown set to X ms - so next time it can be pressed at X+10 ms.
/// If we were running with infinite fps, cooldown would be reduced to 0 and action would be executed slightly (5ms) earlier.
/// We can't fix that easily, but at least we can fix the cooldown after action execution - so that next time it can be pressed at X+5ms.
/// We do that by reducing actual cooldown by difference between previously-remaining cooldown and frame delta, if action is executed at first opportunity.
/// </summary>
public sealed class CooldownDelayTweak
{
	/// <summary>
	/// If > 0 while using an action, cooldown/animation lock will be reduced by this amount as if action was used a bit in the past
	/// </summary>
	public float Adjustment { get; private set; }
	private static float MaxCooldownAdjust => Math.Clamp(Service.Config.CooldownAdjustMaxMs, 10, 150) * 0.001f;

	/// <summary>
	/// Start adjustment calculation based on previous conditions
	/// </summary>
	/// <param name="prevAnimLock">Previous animation lock value</param>
	/// <param name="prevRemainingCooldown">Previous remaining cooldown value</param>
	/// <param name="deltaTime">Frame delta time</param>
	public void StartAdjustment(float prevAnimLock, float prevRemainingCooldown, float deltaTime)
		=> Adjustment = CalculateAdjustment(prevAnimLock, prevRemainingCooldown, deltaTime);

	/// <summary>
	/// Stop the current adjustment
	/// </summary>
	public void StopAdjustment() => Adjustment = 0;

	/// <summary>
	/// Calculate the adjustment value based on previous conditions
	/// </summary>
	/// <param name="prevAnimLock">Previous animation lock value</param>
	/// <param name="prevRemainingCooldown">Previous remaining cooldown value</param>
	/// <param name="deltaTime">Frame delta time</param>
	/// <returns>Calculated adjustment value</returns>
	private static float CalculateAdjustment(float prevAnimLock, float prevRemainingCooldown, float deltaTime)
	{
		if (!Service.Config.RemoveCooldownDelay)
			return 0; // Tweak is disabled, so no adjustment

		// Clamp inputs to avoid negative values
		prevAnimLock = Math.Max(0, prevAnimLock);
		prevRemainingCooldown = Math.Max(0, prevRemainingCooldown);
		deltaTime = Math.Max(0, deltaTime);

		var maxDelay = Math.Max(prevAnimLock, prevRemainingCooldown);
		if (maxDelay <= 0)
			return 0; // Nothing prevented us from executing the action on previous frame, so no adjustment

		var overflow = deltaTime - maxDelay; // Both cooldown and animation lock should expire this much before current frame start
		return Math.Clamp(overflow, 0, MaxCooldownAdjust); // Use upper limit for time adjustment (if you have very low fps, adjusting too much could be suspicious)
	}
}
