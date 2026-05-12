namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Baseline desirability per <see cref="JobRole"/>. Higher = more valuable to focus.
/// Healer first because sustain denial has the highest team impact in CC.
/// Tank last because high eHP and active mitigations make solo focusing a tank inefficient.
/// </summary>
public static class RoleValueFactor
{
    public static double Compute(JobRole role) => role switch
    {
        JobRole.Healer         => 1.00,
        JobRole.RangedMagical  => 0.90,
        JobRole.RangedPhysical => 0.80,
        JobRole.Melee          => 0.55,
        JobRole.Tank           => 0.30,
        _                      => 0.00,
    };
}
