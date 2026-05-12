namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Immutable per-call snapshot of everything the scorer needs from outside.
/// Constructed at the boundary in <c>FindHostileRaw()</c>; scorer and factors are pure on this.
/// </summary>
public sealed record ScoringContext(
    ScoringWeights Weights,
    IMitigationDatabase MitigationDatabase,
    ILBDatabase LBDatabase,
    ulong? PreviousTargetId,
    ulong? CrystalCarrierObjectId,
    float EffectiveRangeYalms);
