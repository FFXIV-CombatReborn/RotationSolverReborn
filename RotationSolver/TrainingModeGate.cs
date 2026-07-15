namespace RotationSolver;

/// <summary>
/// This fork is locked to highlight-only training mode: it must compute and display the
/// recommended action but never execute it, auto-target, auto-attack-toggle, or accept
/// external-plugin commands that would auto-execute. This is the single choke-point flag
/// every execution-adjacent call site checks; see TrainingModeGate usages for the full list.
/// </summary>
internal static class TrainingModeGate
{
    public const bool ExecutionLocked = true;
}
