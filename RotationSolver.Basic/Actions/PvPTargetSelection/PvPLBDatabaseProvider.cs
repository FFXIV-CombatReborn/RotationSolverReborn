namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Process-wide holder for the single <see cref="LBDatabase"/> instance.
/// Pre-init reads return <see cref="LBDatabase.Empty"/> so the scorer's LB term
/// contributes zero rather than throwing.
/// </summary>
public static class PvPLBDatabaseProvider
{
    private static LBDatabase _current = LBDatabase.Empty;
    private static bool _initialized;

    /// <summary>
    /// The active LB database. Returns <see cref="LBDatabase.Empty"/> until
    /// <see cref="Initialize"/> completes.
    /// </summary>
    public static ILBDatabase Current => _current;

    /// <summary>
    /// Read the embedded JSON resource and cache the result. Idempotent: a second
    /// call is a no-op regardless of whether the first call's parse succeeded.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _current = LBDatabase.WithEmbeddedJson();
        _initialized = true;
    }
}
