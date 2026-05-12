namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Per-target, per-term breakdown of a <see cref="PvPTargetScorer"/> computation.
/// Each field holds the <em>weighted</em> contribution (i.e., factor output multiplied
/// by the corresponding <see cref="ScoringWeights"/> entry). Penalty terms
/// (<see cref="Mitigation"/>, <see cref="Distance"/>) are stored as their positive
/// magnitude; the scorer subtracts them. <see cref="Total"/> is the composed scalar.
///
/// <para>
/// Consumed by the debug overlay (<see cref="PvPTargetScorer.Explain"/>). Not used on
/// the hot path; <see cref="PvPTargetScorer.Score"/> reads only <see cref="Total"/>.
/// </para>
/// <para>
/// When <see cref="Invuln"/> is <c>true</c>, all per-term fields are <c>0.0</c> by
/// convention (not computed because the Invuln short-circuit fires before composition);
/// only <see cref="Total"/> (which holds <see cref="double.NegativeInfinity"/>) is meaningful.
/// </para>
/// </summary>
// All call sites construct this with named arguments, never positional. With 11 fields
// silently swapping values would be undetectable; the named-args discipline is load-bearing.
public readonly record struct ScoreBreakdown(
    double Role,
    double Finish,
    double Mitigation,
    double Distance,
    double Sticky,
    double Carrier,
    double LB,
    double Isolation,
    double Threat,
    bool Invuln,
    double Total);
