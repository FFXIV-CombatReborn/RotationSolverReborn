namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Process-wide holder for the single <see cref="MitigationDatabase"/> instance.
/// Initialized once during plugin startup via <see cref="Initialize"/>; reads of
/// <see cref="Current"/> before initialization return the in-code <see cref="MitigationDatabase.EmbeddedDefaults"/>
/// so that early callers still get a working DB rather than null.
/// </summary>
public static class PvPMitigationDatabaseProvider
{
    private static readonly MitigationDatabase PreInitFallback = MitigationDatabase.WithEmbeddedDefaults();
    private static MitigationDatabase _current = PreInitFallback;
    private static bool _initialized;

    /// <summary>
    /// The active mitigation database. Returns the in-code defaults until
    /// <see cref="Initialize"/> completes, then the JSON-loaded instance.
    /// </summary>
    public static IMitigationDatabase Current => _current;

    /// <summary>
    /// Read the embedded JSON resource and cache the result. Idempotent: a second
    /// call is a no-op regardless of whether the first call's parse succeeded.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _current = MitigationDatabase.WithEmbeddedJson();
        _initialized = true;
    }
}
