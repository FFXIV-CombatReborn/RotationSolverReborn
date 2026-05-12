namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Binary factor: 1.0 if the candidate <em>is</em> the crystal carrier; 0.0 otherwise.
/// Weighted by <see cref="ScoringWeights.CarrierWeight"/> at composition time.
/// </summary>
public static class CrystalCarrierFactor
{
    /// <summary>
    /// Return 1.0 when <paramref name="targetId"/> matches a non-null
    /// <paramref name="carrierId"/>; 0.0 otherwise. A null <paramref name="carrierId"/>
    /// (no carrier detected) yields 0.0.
    /// </summary>
    public static double Compute(ulong targetId, ulong? carrierId)
    {
        if (carrierId is null) return 0.0;
        return targetId == carrierId.Value ? 1.0 : 0.0;
    }
}
