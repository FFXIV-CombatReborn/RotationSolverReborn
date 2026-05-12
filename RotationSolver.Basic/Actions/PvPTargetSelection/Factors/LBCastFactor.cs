namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Category-weighted factor for a target currently casting a known PvP limit break.
/// Returns 0.0 when the target is not casting, when the cast action ID is not in
/// <see cref="ILBDatabase"/>, or when the entry's category is unrecognized.
/// </summary>
public static class LBCastFactor
{
    /// <summary>Bonus applied when the target is casting a healer LB.</summary>
    public const double HealingBonus = 1.0;

    /// <summary>Bonus applied when the target is casting an offensive LB.</summary>
    public const double OffensiveBonus = 0.6;

    /// <summary>Bonus applied when the target is casting a utility LB.</summary>
    public const double UtilityBonus = 0.3;

    /// <summary>
    /// Look up the target's current cast in the LB database and return the
    /// category-weighted bonus, or 0.0 if no match.
    /// </summary>
    public static double Compute(IBattleChara target, ILBDatabase database)
    {
        if (!target.IsCasting) return 0.0;
        if (!database.TryGet(target.CastActionId, out var entry)) return 0.0;

        return entry.Category switch
        {
            LBCategory.Healing   => HealingBonus,
            LBCategory.Offensive => OffensiveBonus,
            LBCategory.Utility   => UtilityBonus,
            _                    => 0.0,
        };
    }
}
