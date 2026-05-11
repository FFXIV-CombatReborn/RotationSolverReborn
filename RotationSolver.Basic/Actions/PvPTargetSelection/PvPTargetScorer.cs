using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;
using RotationSolver.Basic.Helpers;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Composes Phase 1 factors into a single scalar score for a candidate target.
/// Pure: reads properties off the passed-in <see cref="IBattleChara"/> but makes no Dalamud calls.
/// Uses <see cref="ScoringContext"/> for everything beyond the target itself.
/// Invuln statuses on the target force <see cref="double.NegativeInfinity"/>, which overrides
/// every other contribution including hysteresis.
/// </summary>
public static class PvPTargetScorer
{
    /// <summary>
    /// Score a candidate. Higher is better. <see cref="double.NegativeInfinity"/> means "do not select".
    /// </summary>
    public static double Score(IBattleChara target, ScoringContext context)
    {
        // Invuln short-circuit first: saves work and guarantees the override.
        if (HasInvulnStatus(target, context.MitigationDatabase))
        {
            return double.NegativeInfinity;
        }

        var ehp = EffectiveHpCalculator.Compute(target, context.MitigationDatabase);
        // FinishFactor midpoint scales with MaxHp so the sigmoid is in the right ballpark
        // regardless of HP pool size. Falls back to 1 if MaxHp is zero.
        var midpoint = target.MaxHp > 0 ? (double)target.MaxHp * 0.5 : 1.0;

        var role = ResolveRole(target);
        var roleTerm     = context.Weights.RoleWeight                * RoleValueFactor.Compute(role);
        var finishTerm   = context.Weights.FinishWeight              * FinishFactor.Compute(ehp, midpoint);
        var mitigTerm    = context.Weights.MitigationPenaltyWeight   * MitigationPenalty.Compute(target, context.MitigationDatabase);
        var distanceTerm = context.Weights.DistancePenaltyWeight     * DistancePenalty.Compute(target.DistanceToPlayer(), context.EffectiveRangeYalms);
        var stickyTerm   = context.Weights.StickyBonus               * HysteresisBonus.Compute(target.GameObjectId, context.PreviousTargetId);

        return roleTerm + finishTerm - mitigTerm - distanceTerm + stickyTerm;
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
