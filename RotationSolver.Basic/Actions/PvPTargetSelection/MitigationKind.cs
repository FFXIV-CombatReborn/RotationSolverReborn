namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Classification of a defensive status for target-selection scoring.
/// </summary>
public enum MitigationKind
{
    /// <summary>Effective invulnerability — attacking is wasted GCDs.</summary>
    Invuln = 0,

    /// <summary>Heavy damage reduction with significant duration.</summary>
    HeavyDR = 1,

    /// <summary>Damage shield modeled as DR-equivalent.</summary>
    Shield = 2,
}
