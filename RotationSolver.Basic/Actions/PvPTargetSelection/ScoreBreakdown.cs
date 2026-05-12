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
/// </summary>
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
