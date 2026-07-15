namespace RotationSolver.Basic;

/// <summary>
/// Mirrors RotationSolver.TrainingModeGate for code living in this project, which cannot
/// reference the RotationSolver project (the project reference only goes the other direction).
/// This fork is locked to highlight-only training mode: it must compute and display the
/// recommended action but never execute it. Both gates are hardcoded to true and kept in sync
/// manually — there is nothing to synchronize at runtime since neither ever toggles.
/// </summary>
internal static class TrainingModeGate
{
    public const bool ExecutionLocked = true;
}
