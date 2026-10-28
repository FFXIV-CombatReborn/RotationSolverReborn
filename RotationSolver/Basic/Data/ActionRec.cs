using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.Basic.Data;
/// <summary>
/// Represents an action record.
/// </summary>
/// <param name="UsedTime">The time the action was used.</param>
/// <param name="Action">The action.</param>
public record ActionRec(DateTime UsedTime, Action Action);

/// <summary>
/// Represents a record of damage received.
/// </summary>
/// <param name="ReceiveTime">The time the damage was received.</param>
/// <param name="Ratio">The ratio of HP lost.</param>
public record DamageRec(DateTime ReceiveTime, float Ratio);