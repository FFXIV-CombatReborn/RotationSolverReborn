using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters;

/// <summary>
/// Class responsible for getting trait rotations.
/// </summary>
internal class TraitRotationGetter : ExcelRowGetter<Trait>
{
	private readonly ClassJob _job;

	/// <summary>
	/// Initializes a new instance of the <see cref="TraitRotationGetter"/> class.
	/// </summary>
	/// <param name="gameData">The game data.</param>
	/// <param name="job">The class job.</param>
	public TraitRotationGetter(Lumina.GameData gameData, ClassJob job)
		: base(gameData)
	{
		_job = job;
	}

	/// <summary>
	/// Gets the list of added names.
	/// </summary>
	public List<string> AddedNames { get; } = new List<string>();

	/// <summary>
	/// Called before creating the list of items.
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
	protected override bool AddToList(Trait item)
	{
		if (item.ClassJob.RowId == 0) return false;
		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name)) return false;
		bool allAscii = true;
		foreach (char c in name)
		{
			if (!char.IsAscii(c)) { allAscii = false; break; }
		}
		if (!allAscii) return false;
		if (item.Icon == 0) return false;

		var category = item.ClassJob.Value;
		var jobName = _job.Abbreviation.ToString();
		return category.Abbreviation.ToString() == jobName;
	}

	/// <summary>
	/// Converts the specified item to code.
	/// </summary>
	/// <param name="item">The item to convert.</param>
	/// <returns>The code representation of the item.</returns>
	protected override string ToCode(Trait item)
	{
		var name = item.Name.ToString().ToPascalCase() + "Trait";

		if (AddedNames.Contains(name))
		{
			name += "_" + item.RowId.ToString();
		}
		else
		{
			AddedNames.Add(name);
		}

		return $$"""
        /// <summary>
        /// {{GetDescName(item)}}
        /// {{GetDesc(item)}}
        /// </summary>
        public static IBaseTrait {{name}} { get; } = new BaseTrait({{item.RowId}});
        """;
	}

	/// <summary>
	/// Gets the description name for the specified item.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>The description name.</returns>
	private static string GetDescName(Trait item)
	{
		var jobs = item.ClassJobCategory.IsValid ? item.ClassJobCategory.Value.Name.ToString() : string.Empty;
		jobs = string.IsNullOrEmpty(jobs) ? string.Empty : $" ({jobs})";

		return $"<see href=\"https://garlandtools.org/db/#action/{50000 + item.RowId}\"><strong>{item.Name.ToString()}</strong></see>{jobs} [{item.RowId}]";
	}

	/// <summary>
	/// Gets the description for the specified item.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>The description.</returns>
	private string GetDesc(Trait item)
	{
		var transient = _gameData.GetExcelSheet<TraitTransient>()?.GetRow(item.RowId);
		var desc = transient?.Description.ToString() ?? string.Empty;

		// Sanitize the description to remove invalid XML tags
		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
