namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Classification of a PvP limit break for target-selection scoring.
/// </summary>
public enum LBCategory
{
    /// <summary>Healer LB (party heal, raises, large shields). Highest selection weight.</summary>
    Healing = 0,

    /// <summary>Offensive LB (party-wipe damage, lockout windows). Moderate selection weight.</summary>
    Offensive = 1,

    /// <summary>Utility LB (mobility, dispel, debuff). Low selection weight.</summary>
    Utility = 2,
}
