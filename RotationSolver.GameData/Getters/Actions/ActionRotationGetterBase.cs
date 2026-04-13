namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Abstract base class for getting action rotation rows from the Excel sheet.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ActionRotationGetterBase"/> class.
/// </remarks>
/// <param name="gameData">The game data.</param>
internal abstract class ActionRotationGetterBase(Lumina.GameData gameData) : ActionGetterBase(gameData)
{

	/// <summary>
	/// Converts the specified action item to its code representation.
	/// </summary>
	/// <param name="item">The action item to convert.</param>
	/// <returns>The code representation of the action item.</returns>
	protected override string ToCode(Lumina.Excel.Sheets.Action item)
	{
		var name = GetName(item);
		var descName = item.GetDescName();

		return item.ToCode(name, descName, GetDesc(item), IsDutyAction);
	}

	/// <summary>
	/// Gets a value indicating whether the action is a duty action.
	/// </summary>
	public abstract bool IsDutyAction { get; }
}
