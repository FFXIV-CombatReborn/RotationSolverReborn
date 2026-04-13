using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Class for getting ActionCategory rows from the game data.
/// </summary>
internal class ActionCategoryGetter(Lumina.GameData gameData)
	: ExcelRowGetter<ActionCategory>(gameData)
{
	private readonly HashSet<string> _addedNames = [];

	/// <summary>
	/// Called before creating the list of items. Clears the list of added names.
	/// </summary>
	protected override void BeforeCreating()
	{
		_addedNames.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified ActionCategory item should be added to the list.
	/// </summary>
	/// <param name="item">The ActionCategory item to check.</param>
	/// <returns>True if the item should be added; otherwise, false.</returns>
	protected override bool AddToList(ActionCategory item)
	{
		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name)) return false;
		bool allAscii = true;
		foreach (char c in name)
		{
			if (!char.IsAscii(c)) { allAscii = false; break; }
		}
		if (!allAscii) return false;
		return true;
	}

	/// <summary>
	/// Converts the specified ActionCategory item to its code representation.
	/// </summary>
	/// <param name="item">The ActionCategory item to convert.</param>
	/// <returns>The code representation of the item.</returns>
	protected override string ToCode(ActionCategory item)
	{
		var name = item.Name.ToString().ToPascalCase();

		if (!_addedNames.Add(name))
		{
			name += "_" + item.RowId.ToString();
		}

		return $"""
        /// <summary>
        /// 
        /// </summary>
        {name} = {item.RowId},
        """;
	}
}
