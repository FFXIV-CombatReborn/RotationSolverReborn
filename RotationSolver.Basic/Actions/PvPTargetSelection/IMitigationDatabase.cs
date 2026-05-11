namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Read-only lookup for mitigation data. Implementations are expected to be immutable after construction.
/// </summary>
public interface IMitigationDatabase
{
    /// <summary>
    /// Look up a mitigation entry by status ID.
    /// </summary>
    /// <param name="id">Status ID to look up.</param>
    /// <param name="entry">Entry if found; <c>default</c> otherwise.</param>
    /// <returns><c>true</c> if the status is known to the database.</returns>
    bool TryGet(StatusID id, out MitigationEntry entry);
}
