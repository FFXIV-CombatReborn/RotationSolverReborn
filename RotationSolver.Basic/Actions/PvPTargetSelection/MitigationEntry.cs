namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// One row in the mitigation database. <see cref="DamageReductionPercent"/> is ignored
/// when <see cref="Kind"/> is <see cref="MitigationKind.Invuln"/>.
/// </summary>
public readonly record struct MitigationEntry(
    StatusID Id,
    MitigationKind Kind,
    double DamageReductionPercent,
    string Description);
