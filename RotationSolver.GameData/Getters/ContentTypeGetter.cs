using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters;

internal class ContentTypeGetter(Lumina.GameData gameData)
	: ExcelRowGetter<ContentType>(gameData)
{
	private readonly HashSet<string> _addedNames = [];

	protected override void BeforeCreating()
	{
		_addedNames.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified item should be added to the list.
	/// </summary>
	/// <param name="item">The content type item.</param>
	/// <returns><c>true</c> if the item should be added; otherwise, <c>false</c>.</returns>
	protected override bool AddToList(ContentType item)
	{
		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		var allAscii = true;
		foreach (var c in name)
		{
			if (!char.IsAscii(c))
			{ allAscii = false; break; }
		}
		if (!allAscii)
		{
			return false;
		}
		return !string.IsNullOrEmpty(name.ToPascalCase());
	}

	/// <summary>
	/// Converts the specified item to code.
	/// </summary>
	/// <param name="item">The content type item.</param>
	/// <returns>The generated code for the item.</returns>
	protected override string ToCode(ContentType item)
	{
		var name = item.Name.ToString().ToPascalCase();
		if (!_addedNames.Add(name))
		{
			name += "_" + item.RowId;
			_addedNames.Add(name);
		}

		return $"""
        /// <summary>
        /// 
        /// </summary>
        {name} = {item.RowId},
        """;
	}
}
