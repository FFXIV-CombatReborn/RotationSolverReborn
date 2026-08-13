using Lumina.Excel.Sheets;
using System.Text;

namespace RotationSolver.GameData.Getters;

/// <summary>
/// Class for getting and processing BNpcName Excel rows.
/// </summary>
internal class BNpcNameGetter(Lumina.GameData gameData)
	: ExcelRowGetter<BNpcName>(gameData)
{
	private readonly HashSet<string> _addedNames = [];

	/// <summary>
	/// Called before creating the list of items. Clears the added names set.
	/// </summary>
	protected override void BeforeCreating()
	{
		_addedNames.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified BNpcName should be added to the list.
	/// </summary>
	/// <param name="item">The BNpcName item to check.</param>
	/// <returns>True if the BNpcName should be added; otherwise, false.</returns>
	protected override bool AddToList(BNpcName item)
	{
		var name = item.Singular.ToString();
		if (string.IsNullOrEmpty(name))
		{
			// Skip NPCs without a name
			return false;
		}

		// Check that the name is all ASCII characters
		var allAscii = true;
		foreach (var c in name)
		{
			if (!char.IsAscii(c))
			{
				allAscii = false;
				break;
			}
		}

		return allAscii;
	}

	/// <summary>
	/// Converts the specified BNpcName to its code representation.
	/// </summary>
	/// <param name="item">The BNpcName item to convert.</param>
	/// <returns>The code representation of the BNpcName.</returns>
	protected override string ToCode(BNpcName item)
	{
		var name = item.Singular.ToString();
		if (string.IsNullOrEmpty(name))
		{
			name = $"UnnamedNpc_{item.RowId}";
		}
		else
		{
			name = name.ToPascalCase();
		}

		// Skip entries that result in empty or invalid names
		if (string.IsNullOrWhiteSpace(name) || name == "_" || name.All(c => c == '_'))
		{
			return string.Empty;
		}

		if (!_addedNames.Add(name))
		{
			name += "_" + item.RowId.ToString();
		}

		// Ensure the name does not start with an underscore
		if (name.StartsWith('_'))
		{
			name = "Npc" + name;
		}

		var plural = item.Plural.ToString();
		var article = item.Article.ToString();

		var displayName = item.Singular.ToString().Replace("&", "and");

		var sb = new StringBuilder();
		sb.AppendLine($"""
        /// <summary>
        /// {item.RowId}
        /// </summary>
        """);
		sb.AppendLine($"{name} = {item.RowId},");

		return sb.ToString();
	}
}
