namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Spatial isolation factor: sigmoid on the distance from the candidate to its
/// nearest <em>other</em> hostile. High when the candidate is alone (peel-worthy);
/// low when grouped with their team (focus-firing them gets contested).
///
/// <para>
/// Calibration: <c>~0.1</c> at <see cref="LowEndYalms"/> (tightly grouped),
/// <c>~0.9</c> at <see cref="HighEndYalms"/> (peel range). The midpoint is the
/// arithmetic mean and the slope is chosen so the (0.1, 0.9) endpoints align
/// with (<see cref="LowEndYalms"/>, <see cref="HighEndYalms"/>).
/// </para>
///
/// <para>
/// Returns <c>0.0</c> when the hostile list is empty, when it contains only the
/// candidate, or when no other hostile is found after filtering. A candidate
/// who is the only hostile alive is "isolated" in a trivial sense but not in
/// the team-fighting sense the factor models; zero is the conservative output.
/// </para>
///
/// <para>
/// Uses raw center-to-center 3D distance (no hitbox-radius subtraction). The
/// peel-range thresholds (<see cref="LowEndYalms"/>/<see cref="HighEndYalms"/>)
/// are intuitive model-to-model spacings, not hitbox-edge distances; this
/// differs deliberately from <c>ObjectHelper.DistanceToPlayer</c>, which
/// subtracts hitbox radii because action range checks use that convention.
/// </para>
/// </summary>
public static class IsolationFactor
{
    /// <summary>Distance below which the candidate is considered grouped with their team.</summary>
    public const double LowEndYalms = 8.0;

    /// <summary>Distance above which the candidate is considered isolated and peelable.</summary>
    public const double HighEndYalms = 15.0;

    /// <summary>Midpoint of the sigmoid; the function value here is 0.5.</summary>
    public const double Midpoint = (LowEndYalms + HighEndYalms) / 2.0;

    /// <summary>
    /// Slope coefficient. Derived: solve <c>0.1 = 1 / (1 + exp(-k * (LowEndYalms - Midpoint)))</c>
    /// for <c>k</c>, giving <c>k = ln(9) / (Midpoint - LowEndYalms) ≈ 0.628</c>.
    /// </summary>
    public const double SlopeK = 0.628;

    /// <summary>
    /// Return the sigmoid-of-nearest-hostile-distance for <paramref name="candidate"/>.
    /// </summary>
    public static double Compute(IBattleChara candidate, IReadOnlyList<IBattleChara> hostiles)
    {
        if (hostiles.Count == 0) return 0.0;

        var candidatePos = candidate.Position;
        var candidateId = candidate.GameObjectId;
        var nearestSq = double.PositiveInfinity;

        for (var i = 0; i < hostiles.Count; i++)
        {
            var other = hostiles[i];
            if (other == null) continue;
            if (other.GameObjectId == candidateId) continue;

            var d2 = Vector3.DistanceSquared(candidatePos, other.Position);
            if (d2 < nearestSq) nearestSq = d2;
        }

        if (double.IsPositiveInfinity(nearestSq)) return 0.0;

        var distance = Math.Sqrt(nearestSq);
        return 1.0 / (1.0 + Math.Exp(-SlopeK * (distance - Midpoint)));
    }
}
