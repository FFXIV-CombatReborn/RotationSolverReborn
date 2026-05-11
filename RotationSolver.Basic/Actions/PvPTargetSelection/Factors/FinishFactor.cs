namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Logistic sigmoid on effective HP. Returns ~1 for very low eHP (finish-killable),
/// 0.5 at the midpoint, ~0 for very high eHP. Returns 0 for infinite eHP (invuln).
/// Slope chosen so a 10x eHP swing around the midpoint produces ~0.85 to ~0.15.
/// </summary>
public static class FinishFactor
{
    private const double SlopeK = 0.005;

    public static double Compute(double effectiveHp, double midpoint)
    {
        if (double.IsInfinity(effectiveHp)) return 0.0;
        // Guard against midpoint == 0: clamp the effective midpoint to 1 to avoid 0/0 cases.
        var safeMidpoint = midpoint > 0.0 ? midpoint : 1.0;
        var exponent = SlopeK * (effectiveHp - safeMidpoint);
        return 1.0 / (1.0 + Math.Exp(exponent));
    }
}
