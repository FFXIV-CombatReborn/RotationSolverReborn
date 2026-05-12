namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Penalty score from active damage-reduction statuses. Additive: a distinct surface from
/// <see cref="EffectiveHpCalculator"/>'s multiplicative stacking. Invuln statuses contribute 0
/// here; the scorer handles invuln as a top-level short-circuit.
/// </summary>
public static class MitigationPenalty
{
    public static double Compute(IBattleChara target, IMitigationDatabase database)
    {
        var statusList = target.StatusList;
        if (statusList == null) return 0.0;

        var total = 0.0;
        foreach (var status in statusList)
        {
            if (!database.TryGet((StatusID)status.StatusId, out var entry)) continue;
            if (entry.Kind == MitigationKind.Invuln) continue;
            total += entry.DamageReductionPercent;
        }
        return total;
    }
}
