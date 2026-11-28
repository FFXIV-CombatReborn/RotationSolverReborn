// Lists the timeline folders and plan files in the plugin's Timelines folder, grouped by expansion / category / duty for the CD Plan picker.
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Updaters;

internal sealed record CdPlanFile(string FileName, string Name);

internal sealed record CdPlanFolder(
	string Path,
	string Folder,
	string Duty,
	string DutyName,
	string Expansion,
	uint ExpansionOrder,
	string Category,
	List<CdPlanFile> Plans);

internal static class CdPlanCatalog
{
	private const string Other = "Other";
	private const uint UltimateContentType = 28;

	private sealed record DutyInfo(string Name, string Expansion, uint ExpansionOrder, string Category);

	private static Dictionary<string, DutyInfo>? _duties;
	private static List<CdPlanFolder> _folders = [];
	private static string _foldersJob = string.Empty;
	private static DateTime _nextScan;

	// Every timeline folder, with the job's plan files in each. Rescanned every few seconds so new files show up.
	public static List<CdPlanFolder> Folders(string job)
	{
		if (job == _foldersJob && DateTime.Now < _nextScan)
		{
			return _folders;
		}

		_foldersJob = job;
		_nextScan = DateTime.Now.AddSeconds(3);
		_folders = Scan(job);
		return _folders;
	}

	// The folders for the duty the player is in, or none.
	public static List<CdPlanFolder> CurrentDutyFolders(string job)
	{
		var duty = TimelineUpdater.Normalize(DataCenter.Territory?.ContentFinderName);
		List<CdPlanFolder> result = [];
		if (string.IsNullOrEmpty(duty))
		{
			return result;
		}

		foreach (var f in Folders(job))
		{
			if (f.Duty.Equals(duty, StringComparison.OrdinalIgnoreCase) ||
				f.Folder.Equals(duty, StringComparison.OrdinalIgnoreCase))
			{
				result.Add(f);
			}
		}

		return result;
	}

	private static List<CdPlanFolder> Scan(string job)
	{
		List<CdPlanFolder> result = [];
		var root = TimelineUpdater.TimelineRoot;
		if (!Directory.Exists(root))
		{
			return result;
		}

		foreach (var dir in TimelineUpdater.TimelineFolders())
		{
			var timeline = System.IO.Path.Combine(dir, "timeline.csv");
			var folder = System.IO.Path.GetFileName(dir);
			string duty;
			try
			{
				duty = TimelineUpdater.TimelineHeader(timeline, "Duty");
			}
			catch (IOException)
			{
				continue;
			}

			if (string.IsNullOrEmpty(duty))
			{
				duty = TimelineUpdater.Normalize(folder);
			}

			var info = DutyLookup().TryGetValue(duty, out var d) ? d : new DutyInfo(folder, Other, 0, Other);

			List<CdPlanFile> plans = [];
			if (!string.IsNullOrEmpty(job))
			{
				foreach (var file in TimelineUpdater.PlanFiles(dir, $"*.{job}.plan.csv"))
				{
					string name;
					try
					{
						name = TimelineUpdater.PlanName(file);
					}
					catch (IOException)
					{
						name = System.IO.Path.GetFileNameWithoutExtension(file);
					}

					plans.Add(new CdPlanFile(System.IO.Path.GetFileName(file), name));
				}
			}

			result.Add(new CdPlanFolder(dir, folder, duty, info.Name, info.Expansion, info.ExpansionOrder, info.Category, plans));
		}

		result.Sort(static (a, b) =>
		{
			var c = b.ExpansionOrder.CompareTo(a.ExpansionOrder);
			if (c != 0)
			{
				return c;
			}

			c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
			if (c != 0)
			{
				return c;
			}

			c = string.Compare(a.DutyName, b.DutyName, StringComparison.OrdinalIgnoreCase);
			return c != 0 ? c : string.Compare(a.Folder, b.Folder, StringComparison.OrdinalIgnoreCase);
		});
		return result;
	}

	// Normalized duty name (as in timeline.csv '# Duty:') -> display name, expansion and category, from the game's duty list.
	private static Dictionary<string, DutyInfo> DutyLookup()
	{
		if (_duties != null)
		{
			return _duties;
		}

		_duties = new Dictionary<string, DutyInfo>(StringComparer.OrdinalIgnoreCase);
		var sheet = Svc.Data.GetExcelSheet<ContentFinderCondition>();
		if (sheet == null)
		{
			return _duties;
		}

		foreach (var row in sheet)
		{
			var name = row.Name.ExtractText();
			var key = TimelineUpdater.Normalize(name);
			if (string.IsNullOrEmpty(key) || _duties.ContainsKey(key))
			{
				continue;
			}

			var territory = row.TerritoryType.ValueNullable;
			var ex = territory?.ExVersion.ValueNullable;
			var expansion = ex?.Name.ExtractText();
			_duties[key] = new DutyInfo(
				Capitalize(name),
				string.IsNullOrEmpty(expansion) ? Other : expansion,
				ex?.RowId ?? 0,
				Category(row, name));
		}

		return _duties;
	}

	private static string Category(ContentFinderCondition row, string name)
	{
		if (row.ContentType.RowId == UltimateContentType)
		{
			return "Ultimate";
		}

		foreach (var tag in (ReadOnlySpan<string>)["Savage", "Extreme", "Unreal", "Chaotic"])
		{
			if (name.EndsWith($"({tag})", StringComparison.OrdinalIgnoreCase))
			{
				return tag;
			}
		}

		var type = row.ContentType.ValueNullable?.Name.ExtractText();
		return string.IsNullOrEmpty(type) ? Other : type;
	}

	private static string Capitalize(string s) =>
		string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
