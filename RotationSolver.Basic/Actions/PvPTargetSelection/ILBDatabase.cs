namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Read-only lookup for PvP LB data. Implementations are expected to be immutable
/// after construction.
/// </summary>
public interface ILBDatabase
{
    /// <summary>
    /// Look up a known PvP LB by cast action ID.
    /// </summary>
    /// <param name="actionId">Cast action ID to look up.</param>
    /// <param name="entry">Entry if known; <c>default</c> otherwise.</param>
    /// <returns><c>true</c> if the action is a recognized PvP LB.</returns>
    bool TryGet(uint actionId, out LBEntry entry);
}
