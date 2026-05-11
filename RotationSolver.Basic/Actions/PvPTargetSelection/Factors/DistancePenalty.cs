namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Linear penalty for distance beyond the player's effective range. Targets in range pay nothing.
/// Negative distances (shouldn't occur with <see cref="Helpers.ObjectHelper.DistanceToPlayer"/>
/// but defended against) are treated as zero.
/// </summary>
public static class DistancePenalty
{
    public static double Compute(float distance, float effectiveRange)
    {
        if (distance <= effectiveRange) return 0.0;
        return distance - effectiveRange;
    }
}
