namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Tunable weights for <see cref="PvPTargetScorer"/>. Phase 3 fields are present and default to 0
/// so factor wiring is forward-compatible without touching the struct shape later.
/// </summary>
public readonly record struct ScoringWeights(
    // Phase 1
    double RoleWeight,
    double FinishWeight,
    double MitigationPenaltyWeight,
    double DistancePenaltyWeight,
    double StickyBonus,
    // Phase 2
    double CarrierWeight,
    double LBWeight,
    // Phase 3 (zero until wired)
    double IsolationWeight,
    double ThreatWeight)
{
    /// <summary>
    /// Look up the preset values. <see cref="ScoringPreset.Custom"/> returns the Casual seed;
    /// callers replace via the user's <c>PvPScoringWeights</c> config when in Custom mode.
    /// </summary>
    public static ScoringWeights ForPreset(ScoringPreset preset) => preset switch
    {
        ScoringPreset.Ranked => new ScoringWeights(
            RoleWeight: 0.60,
            FinishWeight: 1.50,
            MitigationPenaltyWeight: 2.00,
            DistancePenaltyWeight: 0.10,
            StickyBonus: 0.03,
            CarrierWeight: 2.00,
            LBWeight: 1.50,
            IsolationWeight: 0.0,
            ThreatWeight: 0.0),

        // Casual is also the seed for Custom: user config overlays this if preset is Custom.
        _ => new ScoringWeights(
            RoleWeight: 1.00,
            FinishWeight: 1.00,
            MitigationPenaltyWeight: 1.00,
            DistancePenaltyWeight: 0.10,
            StickyBonus: 0.05,
            CarrierWeight: 0.50,
            LBWeight: 1.00,
            IsolationWeight: 0.0,
            ThreatWeight: 0.0),
    };
}
