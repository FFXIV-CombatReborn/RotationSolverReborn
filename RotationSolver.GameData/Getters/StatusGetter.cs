using Lumina.Excel.Sheets;
using System.Text;

namespace RotationSolver.GameData.Getters;

/// <summary>
/// Class for getting and processing Status Excel rows.
/// </summary>
internal class StatusGetter(Lumina.GameData gameData)
	: ExcelRowGetter<Status>(gameData)
{
	private readonly HashSet<string> _addedNames = new();

	/// <summary>
	/// Called before creating the list of items. Clears the added names set.
	/// </summary>
	protected override void BeforeCreating()
	{
		_addedNames.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified status should be added to the list.
	/// </summary>
	/// <param name="item">The status item to check.</param>
	/// <returns>True if the status should be added; otherwise, false.</returns>
	protected override bool AddToList(Status item)
	{
		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name))
		{
			// Allow statuses without a name
			return true;
		}

		// Perform usual checks for statuses with a name
		bool allAscii = true;
		foreach (char c in name)
		{
			if (!char.IsAscii(c)) { allAscii = false; break; }
		}
		return allAscii && item.Icon != 0;
	}

	/// <summary>
	/// Converts the specified status to its code representation.
	/// </summary>
	/// <param name="item">The status item to convert.</param>
	/// <returns>The code representation of the status.</returns>
	protected override string ToCode(Status item)
	{
		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name))
		{
			name = $"UnnamedStatus_{item.RowId}";
		}
		else
		{
			name = name.ToPascalCase();
		}

		if (!_addedNames.Add(name))
		{
			name += "_" + item.RowId.ToString();
		}

		// Ensure the name does not start with an underscore
		if (name.StartsWith("_"))
		{
			name = "Status" + name;
		}

		var desc = item.Description.ToString();

		// Sanitize the description to remove invalid XML tags
		desc = Util.SanitizeXmlDescription(desc);

		var jobs = item.ClassJobCategory.IsValid ? item.ClassJobCategory.Value.Name.ToString() : string.Empty;
		jobs = string.IsNullOrEmpty(jobs) ? string.Empty : $" ({jobs})";

		var cate = item.StatusCategory switch
		{
			1 => " ↑",
			2 => " ↓",
			_ => string.Empty,
		};

		var sb = new StringBuilder();
		if (!name.StartsWith("UnnamedStatus"))
		{
			sb.AppendLine($"""
    /// <summary>
    /// <see href="https://garlandtools.org/db/#status/{item.RowId}"><strong>{item.Name.ToString().Replace("&", "and")}</strong></see>{cate}{jobs}
    /// <para>{desc.Replace("\n", "</para>\n/// <para>")}</para>
    /// </summary>
    """);
		}
		else
		{
			sb.AppendLine($"""
    /// <summary>
    /// <para>{desc.Replace("\n", "</para>\n/// <para>")}</para>
    /// </summary>
    """);
		}
		sb.AppendLine($"{name} = {item.RowId},");

		return sb.ToString();
	}
}
