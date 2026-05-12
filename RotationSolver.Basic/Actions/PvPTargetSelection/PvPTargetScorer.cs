using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;
using RotationSolver.Basic.Helpers;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Composes Phase 1, Phase 2, and Phase 3 factors into a single scalar score for a
/// candidate target. Pure: reads properties off the passed-in <see cref="IBattleChara"/>
/// but makes no Dalamud function calls. Uses <see cref="ScoringContext"/> for everything
/// beyond the target itself. Invuln statuses on the target force
/// <see cref="double.NegativeInfinity"/>, which overrides every other contribution
/// including hysteresis.
/// </summary>
public static class PvPTargetScorer
{
    /// <summary>
    /// Score a candidate. Higher is better. <see cref="double.NegativeInfinity"/> means "do not select".
    ///
    /// <para>Phase 1 terms: role baseline, finish-kill sigmoid, mitigation penalty, distance penalty, sticky bonus.</para>
    /// <para>Phase 2 terms: crystal-carrier promotion, PvP-LB-cast promotion.</para>
    /// <para>Phase 3 terms: spatial isolation, threat-to-our-ally promotion.</para>
    /// </summary>
    public static double Score(IBattleChara target, ScoringContext context) => Compose(target, context).Total;

    /// <summary>
    /// Return the per-term breakdown of a target's score. Use this when per-factor attribution
    /// is needed — the debug overlay (Task 4), empirical weight tuning, or future diagnostics.
    /// For the hot target-selection path, use <see cref="Score"/> instead; it returns only the
    /// composed scalar and discards the breakdown.
    /// </summary>
    public static ScoreBreakdown Explain(IBattleChara target, ScoringContext context) => Compose(target, context);

    private static ScoreBreakdown Compose(IBattleChara target, ScoringContext context)
    {
        // Invuln short-circuit first: saves work and guarantees the override.
        if (HasInvulnStatus(target, context.MitigationDatabase))
        {
            return new ScoreBreakdown(
                Role: 0.0, Finish: 0.0, Mitigation: 0.0, Distance: 0.0, Sticky: 0.0,
                Carrier: 0.0, LB: 0.0, Isolation: 0.0, Threat: 0.0,
                Invuln: true, Total: double.NegativeInfinity);
        }

        var ehp = EffectiveHpCalculator.Compute(target, context.MitigationDatabase);
        // FinishFactor midpoint scales with MaxHp so the sigmoid is in the right ballpark
        // regardless of HP pool size. Falls back to 1 if MaxHp is zero.
        var midpoint = target.MaxHp > 0 ? (double)target.MaxHp * 0.5 : 1.0;

        var role = ResolveRole(target);
        var roleTerm      = context.Weights.RoleWeight                * RoleValueFactor.Compute(role);
        var finishTerm    = context.Weights.FinishWeight              * FinishFactor.Compute(ehp, midpoint);
        var mitigTerm     = context.Weights.MitigationPenaltyWeight   * MitigationPenalty.Compute(target, context.MitigationDatabase);
        var distanceTerm  = context.Weights.DistancePenaltyWeight     * DistancePenalty.Compute(target.DistanceToPlayer(), context.EffectiveRangeYalms);
        var stickyTerm    = context.Weights.StickyBonus               * HysteresisBonus.Compute(target.GameObjectId, context.PreviousTargetId);
        var carrierTerm   = context.Weights.CarrierWeight             * CrystalCarrierFactor.Compute(target.GameObjectId, context.CrystalCarrierObjectId);
        var lbTerm        = context.Weights.LBWeight                  * LBCastFactor.Compute(target, context.LBDatabase);
        var isolationTerm = context.Weights.IsolationWeight           * IsolationFactor.Compute(target, context.Hostiles);
        var threatTerm    = context.Weights.ThreatWeight              * ThreatFactor.Compute(target, context.ThreatenedAllyIds);

        var total = roleTerm + finishTerm - mitigTerm - distanceTerm + stickyTerm + carrierTerm + lbTerm + isolationTerm + threatTerm;

        return new ScoreBreakdown(
            Role: roleTerm,
            Finish: finishTerm,
            Mitigation: mitigTerm,
            Distance: distanceTerm,
            Sticky: stickyTerm,
            Carrier: carrierTerm,
            LB: lbTerm,
            Isolation: isolationTerm,
            Threat: threatTerm,
            Invuln: false,
            Total: total);
    }

    private static bool HasInvulnStatus(IBattleChara target, IMitigationDatabase database)
    {
        var statusList = target.StatusList;
        if (statusList == null) return false;
        foreach (var status in statusList)
        {
            if (database.TryGet((StatusID)status.StatusId, out var entry) && entry.Kind == MitigationKind.Invuln)
            {
                return true;
            }
        }
        return false;
    }

    private static JobRole ResolveRole(IBattleChara target)
    {
        var classJob = target.ClassJob;
        if (classJob.RowId == 0) return JobRole.None;
        return classJob.Value.GetJobRole();
    }
}
