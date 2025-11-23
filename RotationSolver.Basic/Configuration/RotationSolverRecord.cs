namespace RotationSolver.Basic.Configuration;

/// <summary>
/// Records usage statistics for the Rotation Solver.
/// </summary>
public class RotationSolverRecord
{
    /// <summary>
    /// Gets or sets the number of times the Rotation Solver has clicked for you.
    /// </summary>
    public uint ClickingCount { get; set; } = 0;

    /// <summary>
    /// True once the user has defeated the Tic‑tac‑toe AI (unlocks gold star).
    /// </summary>
    public bool TicTacToeWinStar { get; set; } = false;
}
