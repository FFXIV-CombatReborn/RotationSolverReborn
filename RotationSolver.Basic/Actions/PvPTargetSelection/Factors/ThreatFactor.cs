namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Binary factor: <c>1.0</c> if the candidate is currently targeting one of our
/// threatened allies; <c>0.0</c> otherwise. "Threatened ally" is computed once
/// per frame by <see cref="ThreatenedAllyState.BuildThreatenedAllyIds"/> and
/// passed in via <see cref="ScoringContext.ThreatenedAllyIds"/>.
///
/// <para>
/// Models the "kill the BLM about to one-shot our healer" decision: prefer the
/// hostile actively threatening a peel-worthy ally over an unrelated hostile of
/// equal raw value.
/// </para>
/// </summary>
public static class ThreatFactor
{
    /// <summary>
    /// Return <c>1.0</c> when <paramref name="candidate"/>'s current
    /// <see cref="IBattleChara.TargetObjectId"/> is in
    /// <paramref name="threatenedAllyIds"/>; <c>0.0</c> otherwise. Uses
    /// <c>TargetObjectId</c> directly rather than dereferencing
    /// <c>TargetObject</c> — the former is a value property, no dereference cost.
    /// </summary>
    public static double Compute(IBattleChara candidate, IReadOnlySet<ulong> threatenedAllyIds)
    {
        if (threatenedAllyIds.Count == 0) return 0.0;

        var targetId = candidate.TargetObjectId;
        if (targetId == 0) return 0.0;

        return threatenedAllyIds.Contains(targetId) ? 1.0 : 0.0;
    }
}
