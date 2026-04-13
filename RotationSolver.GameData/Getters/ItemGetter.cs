using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters;

/// <summary>
/// Class responsible for getting and processing items from the game data.
/// </summary>
internal class ItemGetter(Lumina.GameData gameData)
	: ExcelRowGetter<Item>(gameData)
{
	/// <summary>
	/// Gets the list of added item names.
	/// </summary>
	public List<string> AddedNames { get; } = new List<string>();

	/// <summary>
	/// Clears the list of added item names before creating the list of items.
	/// </summary>
	protected override void BeforeCreating()
	{
		AddedNames.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified item should be added to the list.
	/// </summary>
	/// <param name="item">The item to check.</param>
	/// <returns>True if the item should be added; otherwise, false.</returns>
	protected override bool AddToList(Item item)
	{
		if (item.ItemSearchCategory.RowId != 43) return false;
		if (item.FilterGroup is not 10 and not 16 and not 19) return false;

		return true;
	}

	/// <summary>
	/// Converts the specified item to its code representation.
	/// </summary>
	/// <param name="item">The item to convert.</param>
	/// <returns>The code representation of the item.</returns>
	protected override string ToCode(Item item)
	{
		var name = item.Singular.ToString().ToPascalCase();
		if (AddedNames.Contains(name))
		{
			name += $"_{item.RowId}";
		}
		else
		{
			AddedNames.Add(name);
		}

		var desc = item.Description.ToString() ?? string.Empty;

		// Sanitize the description to remove invalid XML tags
		desc = Util.SanitizeXmlDescription(desc);

		desc = $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";

		var descName = $"<see href=\"https://garlandtools.org/db/#item/{item.RowId}\"><strong>{item.Name.ToString()}</strong></see> [{item.RowId}]";

		return $$"""
        private readonly Lazy<IBaseItem> _{{name}}Creator = new(() => new BaseItem({{item.RowId}}));

        /// <summary>
        /// {{descName}}
        /// {{desc}}
        /// </summary>
        public IBaseItem {{name}} => _{{name}}Creator.Value;
        """;
	}
}
