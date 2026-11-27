using Lumina.Excel.Sheets;
using System.Security;

namespace RotationSolver.GameData.Getters;

internal class ItemGetter(Lumina.GameData gameData)
	: ExcelRowGetter<Item>(gameData)
{
	/// <summary>
	/// Gets the list of added item names.
	/// </summary>
	public List<string> AddedNames { get; } = [];

	private readonly HashSet<string> _addedNamesSet = [];

	/// <summary>
	/// Clears the list of added item names before creating the list of items.
	/// </summary>
	protected override void BeforeCreating()
	{
		AddedNames.Clear();
		_addedNamesSet.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified item should be added to the list.
	/// </summary>
	/// <param name="item">The item to check.</param>
	/// <returns>True if the item should be added; otherwise, false.</returns>
	protected override bool AddToList(Item item)
	{
		if (item.ItemSearchCategory.RowId != 43)
		{
			return false;
		}

		if (item.FilterGroup is not 10 and not 16 and not 19)
		{
			return false;
		}

		return !string.IsNullOrEmpty(item.Singular.ToString().ToPascalCase());
	}

	/// <summary>
	/// Converts the specified item to its code representation.
	/// </summary>
	/// <param name="item">The item to convert.</param>
	/// <returns>The code representation of the item.</returns>
	protected override string ToCode(Item item)
	{
		var name = item.Singular.ToString().ToPascalCase();
		if (!_addedNamesSet.Add(name))
		{
			name += $"_{item.RowId}";
			_addedNamesSet.Add(name);
		}
		AddedNames.Add(name);

		var desc = item.Description.ToString() ?? string.Empty;

		desc = Util.SanitizeXmlDescription(desc);

		desc = $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";

		var escapedName = SecurityElement.Escape(item.Name.ToString());
		var descName = $"<see href=\"https://garlandtools.org/db/#item/{item.RowId}\"><strong>{escapedName}</strong></see> [{item.RowId}]";

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
