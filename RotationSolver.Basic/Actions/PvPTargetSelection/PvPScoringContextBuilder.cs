namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Boundary-layer factory that materializes a per-call <see cref="ScoringContext"/>
/// from the live game/config state. Centralized so the production target selector
/// in <c>FindHostileRaw</c> and the debug overlay use byte-identical inputs;
/// drift between the two would silently invalidate empirical tuning observations.
///
/// <para>
/// Pure on its single <paramref name="hostiles"/> input from the caller's perspective —
/// all other state is sourced from <see cref="Service.Config"/>, the static provider
/// singletons (mitigation DB, LB DB), and the per-frame DataCenter snapshots.
/// </para>
/// </summary>
public static class PvPScoringContextBuilder
{
    /// <summary>
    /// Effective range used by the distance-penalty factor. Centralized here so the
    /// production scorer and the debug overlay agree on the cutoff; tuning the value
    /// in one place updates both call sites.
    /// </summary>
    public const float DefaultEffectiveRangeYalms = 25f;

    /// <summary>
    /// Build a fresh <see cref="ScoringContext"/> snapshot for the current frame.
    /// </summary>
    public static ScoringContext BuildCurrent(IReadOnlyList<IBattleChara> hostiles)
    {
        var preset = Service.Config.PvPScoringPreset;
        var weights = preset == ScoringPreset.Custom
            ? Service.Config.PvPScoringWeights
            : ScoringWeights.ForPreset(preset);

        return new ScoringContext(
            Weights: weights,
            MitigationDatabase: PvPMitigationDatabaseProvider.Current,
            LBDatabase: PvPLBDatabaseProvider.Current,
            PreviousTargetId: DataCenter.LastPvPSmartTargetId,
            CrystalCarrierObjectId: CrystalCarrierState.GetCurrentCarrierId(),
            Hostiles: hostiles,
            ThreatenedAllyIds: ThreatenedAllyState.BuildThreatenedAllyIds(),
            EffectiveRangeYalms: DefaultEffectiveRangeYalms);
    }
}
