namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Stickiness factor: returns 1 when the candidate is the previously-selected target,
/// 0 otherwise. The scorer multiplies this by <c>ScoringWeights.StickyBonus</c>.
/// </summary>
public static class HysteresisBonus
{
    public static double Compute(ulong targetId, ulong? previousTargetId)
    {
        if (previousTargetId is null) return 0.0;
        return previousTargetId.Value == targetId ? 1.0 : 0.0;
    }
}
