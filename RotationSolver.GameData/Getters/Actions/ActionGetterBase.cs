using Lumina.Excel.Sheets;
using Action = Lumina.Excel.Sheets.Action;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Abstract base class for getting action rows from the Excel sheet.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ActionGetterBase"/> class.
/// </remarks>
/// <param name="gameData">The game data.</param>
internal abstract class ActionGetterBase(Lumina.GameData gameData) : ExcelRowGetter<Action>(gameData)
{

	/// <summary>
	/// Gets the list of added names.
	/// </summary>
	public List<string> AddedNames { get; } = [];

	private string[] _notCombatJobs = [];

	/// <summary>
	/// Called before creating the list of items.
	/// </summary>
	protected override void BeforeCreating()
	{
		AddedNames.Clear();
		var classJobs = _gameData.GetExcelSheet<ClassJob>()!;
		var tmp = new List<string>();
		foreach (var c in classJobs)
		{
			if (c.ClassJobCategory.RowId is 32 or 33)
			{
				tmp.Add(c.Abbreviation.ToString());
			}
		}
		_notCombatJobs = tmp.ToArray();
		base.BeforeCreating();
	}

	/// <summary>
	/// Determines whether the specified action should be added to the list.
	/// </summary>
	/// <param name="item">The action to check.</param>
	/// <returns>True if the action should be added; otherwise, false.</returns>
	protected override bool AddToList(Action item)
	{
		if (item.RowId is 3 or 120) return true; // Sprint and cure.
		if (item.RowId is 16538 or 16537) return true;
		if (item.ClassJobCategory.RowId == 0) return false;

		var name = item.Name.ToString();
		if (string.IsNullOrEmpty(name)) return false;
		bool allAscii = true;
		foreach (char c in name)
		{
			if (!char.IsAscii(c)) { allAscii = false; break; }
		}
		if (!allAscii) return false;
		if (item.Icon is 0 or 405 or 784) return false;

		if (item.ActionCategory.RowId is 6 or 7 or 8 or 12 or > 14 or 9) return false;

		if (item.CooldownGroup == 0 && item.AdditionalCooldownGroup == 0 && item.ClassJobCategory.RowId == 29) return false;
		if (!item.ClassJobCategory.IsValid) return false;
		var category = item.ClassJobCategory.Value;

		if (category.RowId == 1) return true;

		bool isNotCombat = false;
		for (int i = 0; i < _notCombatJobs.Length; i++)
		{
			string jobName = _notCombatJobs[i];
			bool val = (bool?)category.GetType().GetRuntimeProperty(jobName)?.GetValue(category) ?? false;
			if (val)
			{
				isNotCombat = true;
				break;
			}
		}
		if (isNotCombat)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the name of the specified action.
	/// </summary>
	/// <param name="item">The action.</param>
	/// <returns>The name of the action.</returns>
	protected string GetName(Action item)
	{
		var name = item.Name.ToString().ToPascalCase() + (item.IsPvP ? "PvP" : "PvE");

		if (AddedNames.Contains(name))
		{
			name += "_" + item.RowId;
		}
		else
		{
			AddedNames.Add(name);
		}
		return name;
	}

	/// <summary>
	/// Gets the description of the specified action.
	/// </summary>
	/// <param name="item">The action.</param>
	/// <returns>The description of the action.</returns>
	protected string GetDesc(Action item)
	{
		var transient = _gameData.GetExcelSheet<ActionTransient>()?.GetRow(item.RowId);
		var desc = transient?.Description.ToString() ?? string.Empty;

		// Sanitize the description to remove invalid XML tags
		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
