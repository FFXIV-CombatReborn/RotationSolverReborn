namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// User-facing preset selector for PvP smart targeting.
/// </summary>
public enum ScoringPreset
{
    /// <summary>Casual CC tuning: role-heavy, forgiving.</summary>
    Casual = 0,

    /// <summary>Ranked CC tuning: mitigation-heavy, more reactive.</summary>
    Ranked = 1,

    /// <summary>User-defined weights via config.</summary>
    Custom = 2,
}
