// CD Plan section of the rotation page (Beiruta rotations): the plan's actions, the [CD Plan] settings and the plan picker.
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using RebornMaterial;
using RotationSolver.UI.SearchableConfigs;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private const string CdPlanConfigTag = "[CD Plan]";

	// Picker state while browsing outside the duty.
	private static string _pickExpansion = string.Empty;
	private static string _pickCategory = string.Empty;
	private static string _pickDuty = string.Empty;

	private static readonly Dictionary<string, (long Stamp, List<string> Actions)> _planActionCache = [];

	private static string GetCdPlanHead() =>
		DataCenter.CurrentRotation is IBeirutaTimelineOption
			? UiString.ConfigWindow_Rotation_CdPlan.GetDescription()
			: string.Empty;

	// A config belongs to the CD Plan section when its top parent is a "[CD Plan] ..." setting.
	private static bool IsCdPlanConfig(IRotationConfig config, IRotationConfigSet set)
	{
		var current = config;
		for (var depth = 0; depth < 16 && !string.IsNullOrEmpty(current.Parent); depth++)
		{
			IRotationConfig? parent = null;
			foreach (var c in set.Configs)
			{
				if (c.Name == current.Parent)
				{
					parent = c;
					break;
				}
			}

			if (parent == null)
			{
				break;
			}

			current = parent;
		}

		return current.DisplayName.TrimStart().StartsWith(CdPlanConfigTag, StringComparison.OrdinalIgnoreCase);
	}

	private static void DrawRotationCdPlan()
	{
		var rotation = DataCenter.CurrentRotation;
		if (rotation is not IBeirutaTimelineOption option || !Player.Available)
		{
			return;
		}

		var job = Player.Job.ToString();
		var here = BeirutaPlanCatalog.CurrentDutyFolders(job);
		var shownFolders = here.Count > 0 ? here : BrowsedFolders(job);

		// Planned / optional actions and the picker only matter for the built-in plan.
		if (option.UseTimelinePlan)
		{
			DrawCdPlanActions(rotation, option, job, here, shownFolders);
			M3Widgets.Divider(6f);
		}

		DrawCdPlanConfigs(rotation);

		if (!option.UseTimelinePlan)
		{
			return;
		}

		M3Widgets.Divider(6f);
		DrawPlanPicker(job, here);
	}

	#region Actions

	private static void DrawCdPlanActions(ICustomRotation rotation, IBeirutaTimelineOption option, string job,
		List<BeirutaPlanFolder> here, List<BeirutaPlanFolder> shown)
	{
		// The loaded plan in the duty; otherwise the picked plan of the browsed duty.
		var planPath = here.Count > 0 && !string.IsNullOrEmpty(BeirutaTimelineUpdater.LoadedPlanPath)
			? BeirutaTimelineUpdater.LoadedPlanPath
			: FirstChosenPlanPath(shown, job);

		var optional = new List<(uint ActionId, string Config)>(option.OptionalPlannedActions);
		var optionalIds = new HashSet<uint>();
		foreach (var (id, _) in optional)
		{
			_ = optionalIds.Add(id);
		}

		List<IAction> planned = [];
		var seen = new HashSet<uint>();
		foreach (var name in PlanActionNames(planPath))
		{
			if (BeirutaTimelineUpdater.FindAction(name) is { } action &&
				!optionalIds.Contains(action.ID) &&
				seen.Add(action.ID))
			{
				planned.Add(action);
			}
		}

		using var table = ImRaii.Table("CD Plan Actions", 2, ImGuiTableFlags.Borders
			| ImGuiTableFlags.Resizable
			| ImGuiTableFlags.SizingStretchProp);
		if (!table)
		{
			return;
		}

		var size = DESC_SIZE * Scale;

		BeginCdPlanRow(FontAwesomeIcon.CalendarCheck, "Planned Action", size);
		if (planned.Count == 0)
		{
			using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.8f));
			CenterInRow(ImGui.GetTextLineHeight(), size);
			ImGui.Text(string.IsNullOrEmpty(planPath) ? "No plan picked." : "This plan has no actions.");
		}
		else
		{
			var rowY = ImGui.GetCursorPosY();
			var first = true;
			foreach (var action in planned)
			{
				if (!first)
				{
					ImGui.SameLine();
				}

				first = false;
				if (action.GetTexture(out var texture))
				{
					ImGui.SetCursorPosY(rowY);
					var cursor = ImGui.GetCursorPos();
					_ = ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * size);
					ImGuiHelper.DrawActionOverlay(cursor, size, 1);
					ImguiTooltips.HoveredTooltip(action.Name);
				}
			}
		}

		if (optional.Count == 0)
		{
			return;
		}

		BeginCdPlanRow(FontAwesomeIcon.ToggleOn, "Optional Action", size);

		var optionalY = ImGui.GetCursorPosY();
		var firstOptional = true;
		foreach (var (id, configName) in optional)
		{
			var config = FindRotationConfig(rotation, configName);
			IAction? action = null;
			foreach (var a in rotation.AllBaseActions)
			{
				if (a.ID == id)
				{
					action = a;
					break;
				}
			}

			if (action == null || !action.GetTexture(out var texture))
			{
				continue;
			}

			if (!firstOptional)
			{
				ImGui.SameLine();
			}

			firstOptional = false;
			var on = config != null && bool.TryParse(config.Value, out var v) && v;
			ImGui.SetCursorPosY(optionalY);
			var cursor = ImGui.GetCursorPos();
			using (ImRaii.PushId($"cdplan_opt_{id}"))
			{
				using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, on ? 1f : 0.35f);
				if (ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * size) && config != null)
				{
					config.Value = (!on).ToString();
				}
			}

			ImGuiHelper.DrawActionOverlay(cursor, size, 1);
			ImguiTooltips.HoveredTooltip($"{action.Name}: {(on ? "on" : "off")}. Click to turn it {(on ? "off" : "on")}.\n" +
				(on ? "Cast in its planned window." : "Planned casts are skipped; the rotation decides."));
		}
	}

	private const float CdPlanRowPad = 4f;

	// Starts a table row tall enough for the icons, draws the label centred in the first cell, and moves to the icon cell.
	private static void BeginCdPlanRow(FontAwesomeIcon icon, string text, float iconSize)
	{
		ImGui.TableNextRow(ImGuiTableRowFlags.None, iconSize + (CdPlanRowPad * 2f * Scale));
		_ = ImGui.TableNextColumn();
		CenterInRow(ImGui.GetTextLineHeight(), iconSize);
		using (ImRaii.PushFont(UiBuilder.IconFont))
		{
			ImGui.Text(icon.ToIconString());
		}

		ImGui.SameLine();
		ImGui.Text(" " + text);

		_ = ImGui.TableNextColumn();
		CenterInRow(iconSize, iconSize);
	}

	// Moves the cursor down so an item of this height sits in the middle of the padded row.
	private static void CenterInRow(float itemHeight, float iconSize)
	{
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (CdPlanRowPad * Scale) + MathF.Max(0f, (iconSize - itemHeight) / 2f));
	}

	internal static IRotationConfig? FindRotationConfig(ICustomRotation rotation, string name)
	{
		foreach (var c in rotation.Configs.Configs)
		{
			if (c.Name == name)
			{
				return c;
			}
		}

		return null;
	}

	// Action names a plan file schedules, in order, without flag / Keep rows.
	private static List<string> PlanActionNames(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return [];
		}

		var stamp = File.GetLastWriteTimeUtc(path).Ticks;
		if (_planActionCache.TryGetValue(path, out var cached) && cached.Stamp == stamp)
		{
			return cached.Actions;
		}

		List<string> names = [];
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (var cols in BeirutaTimelineUpdater.ReadCsv(path))
			{
				if (cols.Length >= 4 && !string.IsNullOrEmpty(cols[3]) &&
					!BeirutaTimelineUpdater.IsPlanMetaAction(cols[3]) && seen.Add(cols[3]))
				{
					names.Add(cols[3]);
				}
			}
		}
		catch (IOException)
		{
			return [];
		}

		_planActionCache[path] = (stamp, names);
		return names;
	}

	private static string FirstChosenPlanPath(List<BeirutaPlanFolder> folders, string job)
	{
		foreach (var f in folders)
		{
			var file = BeirutaTimelineUpdater.ChosenPlanFile(f.Path, job);
			if (!string.IsNullOrEmpty(file))
			{
				return Path.Combine(f.Path, file);
			}
		}

		return string.Empty;
	}

	#endregion

	#region Settings

	private static void DrawCdPlanConfigs(ICustomRotation rotation)
	{
		var set = rotation.Configs;
		var requiredType = DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE;
		foreach (var config in set.Configs)
		{
			if (!config.Type.HasFlag(requiredType) || !IsCdPlanConfig(config, set) || !ShouldShowRotationConfig(config, set))
			{
				continue;
			}

			// The section is already called CD Plan, so "[CD Plan] Mitigation" shows as "Mitigation".
			var title = config.DisplayName;
			var tag = title.IndexOf(CdPlanConfigTag, StringComparison.OrdinalIgnoreCase);
			if (tag >= 0)
			{
				title = title.Remove(tag, CdPlanConfigTag.Length).Trim();
			}

			DrawRotationConfigRow(config, rotation, OtherCommandType.Rotations, GetRotationConfigTooltip(rotation, config), title);
		}
	}

	#endregion

	#region Plan picker

	private static void DrawPlanPicker(string job, List<BeirutaPlanFolder> here)
	{
		if (here.Count > 0)
		{
			var f = here[0];
			DrawPickerInfoRow("Current duty", $"{f.Expansion} > {f.Category} > {f.DutyName}",
				"You are in this duty, so its plan is shown here. Leave the duty to browse other fights.");
			DrawPlanRows(job, here);
			DrawOpenFolderRow(here);
			return;
		}

		var all = BeirutaPlanCatalog.Folders(job);
		if (all.Count == 0)
		{
			DrawPickerInfoRow("Plans", "No timelines found",
				$"Put timeline folders in {BeirutaTimelineUpdater.TimelineRoot}.");
			DrawOpenFolderRow([]);
			return;
		}

		var expansions = Distinct(all, static f => true, static f => f.Expansion);
		_pickExpansion = PickerCombo("Expansion", "cdplan_expansion", expansions, _pickExpansion);

		var categories = Distinct(all, static f => f.Expansion == _pickExpansion, static f => f.Category);
		_pickCategory = PickerCombo("Category", "cdplan_category", categories, _pickCategory);

		var duties = Distinct(all, static f => f.Expansion == _pickExpansion && f.Category == _pickCategory, static f => f.DutyName);
		_pickDuty = PickerCombo("Duty", "cdplan_duty", duties, _pickDuty);

		var browsed = BrowsedFolders(job);
		DrawPlanRows(job, browsed);
		DrawOpenFolderRow(browsed);
	}

	// Opens the shown duty's timeline folder, or the Timelines root when there isn't exactly one.
	private static void DrawOpenFolderRow(List<BeirutaPlanFolder> folders)
	{
		var path = folders.Count == 1 ? folders[0].Path : BeirutaTimelineUpdater.TimelineRoot;
		ImGui.Dummy(new Vector2(0f, M3.Space2));
		if (M3Widgets.Button("##cdplan_open_folder", "Open Plan folder", M3ButtonStyle.Tonal, FontAwesomeIcon.FolderOpen))
		{
			try
			{
				_ = Directory.CreateDirectory(path);
				_ = System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
			}
			catch (Exception ex)
			{
				ECommons.Logging.PluginLog.Warning($"Failed to open plan folder: {ex.Message}");
			}
		}
	}

	// The folders of the duty picked in the browser (outside the duty).
	private static List<BeirutaPlanFolder> BrowsedFolders(string job)
	{
		List<BeirutaPlanFolder> result = [];
		foreach (var f in BeirutaPlanCatalog.Folders(job))
		{
			if (f.Expansion == _pickExpansion && f.Category == _pickCategory && f.DutyName == _pickDuty)
			{
				result.Add(f);
			}
		}

		return result;
	}

	// One plan combo per timeline folder (a duty can have several, e.g. M12S part 1 and part 2).
	private static void DrawPlanRows(string job, List<BeirutaPlanFolder> folders)
	{
		var locked = BeirutaTimelineUpdater.PlanLocked;
		foreach (var f in folders)
		{
			var title = folders.Count > 1 ? $"Plan ({f.Folder})" : "Plan";
			if (f.Plans.Count == 0)
			{
				DrawPickerInfoRow(title, $"No {job} plan", $"No *.{job}.plan.csv in {f.Folder}.");
				continue;
			}

			var names = new string[f.Plans.Count];
			var chosen = BeirutaTimelineUpdater.ChosenPlanFile(f.Path, job);
			var index = 0;
			for (var i = 0; i < f.Plans.Count; i++)
			{
				names[i] = f.Plans[i].Name;
				if (f.Plans[i].FileName.Equals(chosen, StringComparison.OrdinalIgnoreCase))
				{
					index = i;
				}
			}

			var supporting = locked
				? "Can't change the plan during a pull."
				: $"{f.Plans[index].FileName}. Takes effect out of combat.";
			var width = ComboWidthFor(names);
			var row = M3SettingRow.Begin(title, supporting, new Vector2(width, M3Widgets.ComboHeight));
			ImGui.SetCursorScreenPos(row.ControlPosition);
			using (ImRaii.Disabled(locked))
			{
				if (M3Widgets.Combo($"##cdplan_plan_{f.Folder}", ref index, names, width) && index < f.Plans.Count)
				{
					BeirutaTimelineUpdater.ChoosePlan(f.Path, job, f.Plans[index].FileName);
				}
			}

			M3SettingRow.End(row);
		}
	}

	private static string PickerCombo(string title, string id, List<string> items, string current)
	{
		if (items.Count == 0)
		{
			return string.Empty;
		}

		var index = Math.Max(0, items.IndexOf(current));
		var names = items.ToArray();
		var width = ComboWidthFor(names);
		var row = M3SettingRow.Begin(title, null, new Vector2(width, M3Widgets.ComboHeight));
		ImGui.SetCursorScreenPos(row.ControlPosition);
		_ = M3Widgets.Combo($"##{id}", ref index, names, width);
		M3SettingRow.End(row);
		return items[Math.Clamp(index, 0, items.Count - 1)];
	}

	private static void DrawPickerInfoRow(string title, string value, string? supporting)
	{
		var size = ImGui.CalcTextSize(value);
		var row = M3SettingRow.Begin(title, supporting, size);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		ImGui.Text(value);
		M3SettingRow.End(row);
	}

	private static float ComboWidthFor(string[] names)
	{
		var width = Searchable.DRAG_WIDTH * Scale;
		foreach (var n in names)
		{
			width = MathF.Max(width, M3Widgets.ComboWidthFor(n));
		}

		return MathF.Min(width, M3SettingRow.MaxControlWidth());
	}

	private static List<string> Distinct(List<BeirutaPlanFolder> folders, Func<BeirutaPlanFolder, bool> where, Func<BeirutaPlanFolder, string> select)
	{
		List<string> result = [];
		foreach (var f in folders)
		{
			if (where(f) && !result.Contains(select(f)))
			{
				result.Add(select(f));
			}
		}

		return result;
	}

	#endregion
}
