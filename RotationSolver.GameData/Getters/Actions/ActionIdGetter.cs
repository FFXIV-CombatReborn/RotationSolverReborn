using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Class for getting action IDs from the Excel sheet.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ActionIdGetter"/> class.
/// </remarks>
/// <param name="gameData">The game data.</param>
internal class ActionIdGetter(Lumina.GameData gameData) : ActionGetterBase(gameData)
{

	/// <summary>
	/// Converts the specified action to its code representation.
	/// </summary>
	/// <param name="item">The action to convert.</param>
	/// <returns>The code representation of the action.</returns>
	protected override string ToCode(Action item)
	{
		var name = GetName(item);

		return $"""
        /// <summary>
        /// {item.GetDescName()}
        /// {GetDesc(item)}
        /// </summary>
        {name} = {item.RowId},
        """;
	}
}
