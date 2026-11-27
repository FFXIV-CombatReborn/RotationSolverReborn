using Lumina.Excel.Sheets;
using System.Security;

namespace RotationSolver.GameData.Getters;

internal class TraitRotationGetter : ExcelRowGetter<Trait>
{
	private readonly ClassJob _job;
	private readonly HashSet<string> _addedNamesSet = [];

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
		_addedNamesSet.Clear();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified item should be added to the list.
	/// </summary>
	/// <param name="item">The item to check.</param>
	/// <returns>True if the item should be added; otherwise, false.</returns>
	protected override bool AddToList(Trait item)
	{
		if (item.ClassJob.RowId == 0 || !item.ClassJob.IsValid)
		{
			return false;
		}

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
		if (!allAscii || string.IsNullOrEmpty(name.ToPascalCase()))
		{
			return false;
		}

		if (item.Icon == 0)
		{
			return false;
		}

		var category = item.ClassJob.Value;
		var jobName = _job.Abbreviation.ToString();
		if (category.Abbreviation.ToString() == jobName)
		{
			return true;
		}

		if (item.ClassJobCategory.IsValid && RotationGetter.IsJobCompatible(item.ClassJobCategory.Value, _job))
		{
			return true;
		}

		return _job.RowId != 28
			&& _job.ClassJobParent.IsValid
			&& category.RowId == _job.ClassJobParent.RowId;
	}

	/// <summary>
	/// Converts the specified item to code.
	/// </summary>
	/// <param name="item">The item to convert.</param>
	/// <returns>The code representation of the item.</returns>
	protected override string ToCode(Trait item)
	{
		var name = item.Name.ToString().ToPascalCase() + "Trait";

		if (!_addedNamesSet.Add(name))
		{
			name += "_" + item.RowId.ToString();
			_addedNamesSet.Add(name);
		}
		AddedNames.Add(name);

		return $$"""
        /// <summary>
        /// {{GetDescName(item)}}
        /// {{GetDesc(item)}}
        /// </summary>
        public static IBaseTrait {{name}} { get; } = new BaseTrait({{item.RowId}});
        """;
	}

	private static string GetDescName(Trait item)
	{
		var jobs = item.ClassJobCategory.IsValid ? SecurityElement.Escape(item.ClassJobCategory.Value.Name.ToString()) : string.Empty;
		jobs = string.IsNullOrEmpty(jobs) ? string.Empty : $" ({jobs})";
		var escapedName = SecurityElement.Escape(item.Name.ToString());

		return $"<see href=\"https://garlandtools.org/db/#action/{50000 + item.RowId}\"><strong>{escapedName}</strong></see>{jobs} [{item.RowId}]";
	}

	private string GetDesc(Trait item)
	{
		var transientSheet = _gameData.GetExcelSheet<TraitTransient>();
		var desc = transientSheet != null && transientSheet.TryGetRow(item.RowId, out var transient)
			? transient.Description.ToString()
			: string.Empty;

		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
