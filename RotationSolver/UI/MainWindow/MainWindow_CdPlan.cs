// CD Plan section of the rotation page: the plan's actions, the [CD Plan] settings and the plan picker.
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
		DataCenter.CurrentRotation is ICdPlanRotation
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
		if (rotation is not ICdPlanRotation option || !Player.Available)
		{
			return;
		}

		var job = Player.Job.ToString();
		var here = CdPlanCatalog.CurrentDutyFolders(job);
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
		DrawPlanPicker(rotation, job, here, option);
	}

	private const string FeintFromConfig = "FeintFrom";
	private const string AddleFromConfig = "AddleFrom";

	#region Actions

	private static void DrawCdPlanActions(ICustomRotation rotation, ICdPlanRotation option, string job,
		List<CdPlanFolder> here, List<CdPlanFolder> shown)
	{
		// The loaded plan in the duty; otherwise the picked plan of the browsed duty.
		var planPath = here.Count > 0 && !string.IsNullOrEmpty(TimelineUpdater.LoadedPlanPath)
			? TimelineUpdater.LoadedPlanPath
			: FirstChosenPlanPath(shown, job);

		var planNames = new HashSet<string>(PlanActionNames(planPath), StringComparer.OrdinalIgnoreCase);
		var optional = OptionalActionsToShow(rotation, option, planNames);
		var optionalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (id, _) in option.OptionalPlannedActions)
		{
			foreach (var a in rotation.AllBaseActions)
			{
				if (a.ID == id)
				{
					_ = optionalNames.Add(a.Name);
					break;
				}
			}
		}

		List<IAction> planned = [];
		var seen = new HashSet<uint>();
		foreach (var name in PlanActionNames(planPath))
		{
			if (TimelineUpdater.FindAction(name) is { } action &&
				!optionalNames.Contains(action.Name) &&
				seen.Add(action.ID))
			{
				planned.Add(action);
			}
		}

		// Feint / Addle from a shared profile (M1 / M2, R2 / Fake Melee) counts as planned even without a job plan.
		if (!string.IsNullOrEmpty(option.MitProfileAction) && option.MitProfile != MitProfileChoice.JobPlan &&
			shown.Count > 0 && TimelineUpdater.ProfileNames(shown[0].Path, option.MitProfileAction).Count > 0 &&
			TimelineUpdater.FindAction(option.MitProfileAction) is { } profileAction && seen.Add(profileAction.ID))
		{
			planned.Add(profileAction);
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
		foreach (var (action, configName) in optional)
		{
			var id = action.ID;
			var config = FindRotationConfig(rotation, configName);
			if (!action.GetTexture(out var texture))
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

	// Optional actions with their setting. When several share one setting (e.g. NIN's downtime ninjutsu), only the ones
	// the plan has are shown, or just the first when the plan has none of them.
	private static List<(IAction Action, string Config)> OptionalActionsToShow(ICustomRotation rotation, ICdPlanRotation option,
		HashSet<string> planNames)
	{
		List<(IAction Action, string Config)> all = [];
		foreach (var (id, config) in option.OptionalPlannedActions)
		{
			foreach (var a in rotation.AllBaseActions)
			{
				if (a.ID == id)
				{
					all.Add((a, config));
					break;
				}
			}
		}

		var count = new Dictionary<string, int>();
		var inPlan = new HashSet<string>();
		foreach (var (action, config) in all)
		{
			count[config] = count.GetValueOrDefault(config) + 1;
			if (planNames.Contains(action.Name))
			{
				_ = inPlan.Add(config);
			}
		}

		List<(IAction Action, string Config)> shown = [];
		var firstShown = new HashSet<string>();
		foreach (var (action, config) in all)
		{
			if (count[config] > 1 &&
				(inPlan.Contains(config) ? !planNames.Contains(action.Name) : !firstShown.Add(config)))
			{
				continue;
			}

			shown.Add((action, config));
		}

		return shown;
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
			foreach (var cols in TimelineUpdater.ReadCsv(path))
			{
				if (cols.Length >= 4 && !string.IsNullOrEmpty(cols[3]) &&
					!TimelineUpdater.IsPlanMetaAction(cols[3]) && seen.Add(cols[3]))
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

	private static string FirstChosenPlanPath(List<CdPlanFolder> folders, string job)
	{
		foreach (var f in folders)
		{
			var file = TimelineUpdater.ChosenPlanFile(f.Path, job);
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

			// Feint / Addle from is picked in the plan picker below, next to the job plan.
			if (config.Name is FeintFromConfig or AddleFromConfig)
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

	private static void DrawPlanPicker(ICustomRotation rotation, string job, List<CdPlanFolder> here, ICdPlanRotation option)
	{
		if (here.Count > 0)
		{
			var f = here[0];
			DrawPickerInfoRow("Current duty", $"{f.Expansion} > {f.Category} > {f.DutyName}",
				"You are in this duty, so its plan is shown here. Leave the duty to browse other fights.");
			DrawPlanRows(rotation, job, here, option, inDuty: true);
			DrawOpenFolderRow(here);
			return;
		}

		var all = CdPlanCatalog.Folders(job);
		if (all.Count == 0)
		{
			DrawPickerInfoRow("Plans", "No timelines found",
				$"Put timeline folders in {TimelineUpdater.TimelineRoot}.");
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
		DrawPlanRows(rotation, job, browsed, option, inDuty: false);
		DrawOpenFolderRow(browsed);
	}

	// Opens the shown duty's timeline folder, or the Timelines root when there isn't exactly one.
	private static void DrawOpenFolderRow(List<CdPlanFolder> folders)
	{
		var path = folders.Count == 1 ? folders[0].Path : TimelineUpdater.TimelineRoot;
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
	private static List<CdPlanFolder> BrowsedFolders(string job)
	{
		List<CdPlanFolder> result = [];
		foreach (var f in CdPlanCatalog.Folders(job))
		{
			if (f.Expansion == _pickExpansion && f.Category == _pickCategory && f.DutyName == _pickDuty)
			{
				result.Add(f);
			}
		}

		return result;
	}

	// One plan combo per timeline folder (a duty can have several, e.g. M12S part 1 and part 2).
	// Melee / casters: which shared Feint / Addle plan (M1 / M2, R2 / Fake Melee), or the job plan's own. Same setting as
	// "Feint from" / "Addle from", shown here so it sits next to the job plan.
	private static void DrawProfileRow(ICustomRotation rotation, string job, CdPlanFolder f, ICdPlanRotation option, bool inDuty)
	{
		var action = option.MitProfileAction;
		var profiles = string.IsNullOrEmpty(action) ? [] : TimelineUpdater.ProfileNames(f.Path, action);
		if (profiles.Count == 0 ||
			FindRotationConfig(rotation, action == "Feint" ? FeintFromConfig : AddleFromConfig) is not RotationConfigCombo config)
		{
			return;
		}

		var labels = new string[config.EnumNames.Length];
		var index = 0;
		for (var i = 0; i < labels.Length; i++)
		{
			labels[i] = config.EnumNames[i] switch
			{
				"Auto" => profiles.Count > 1 ? $"Auto ({profiles[0]}, then {profiles[1]})" : $"Auto ({profiles[0]})",
				"JobPlan" => $"{job} plan",
				"FakeMelee" => "Fake Melee",
				var n => n,
			};
			if (config.EnumNames[i].Equals(config.Value, StringComparison.OrdinalIgnoreCase))
			{
				index = i;
			}
		}

		var usingNow = inDuty && TimelineUpdater.ActiveProfileName is { } active ? $" Now using {active}." : string.Empty;
		var supporting = option.MitProfile == MitProfileChoice.JobPlan
			? $"{action} comes from the {job} plan below."
			: $"{action} comes from this shared plan; everything else from the {job} plan below. Auto starts on {profiles[0]} and moves on if another {(action == "Feint" ? "melee" : "caster")} already covered it.{usingNow}";
		var width = ComboWidthFor(labels);
		var row = M3SettingRow.Begin($"{action} plan", supporting, new Vector2(width, M3Widgets.ComboHeight));
		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.Combo($"##cdplan_profile_{f.Folder}", ref index, labels, width) && index < config.EnumNames.Length)
		{
			config.Value = config.EnumNames[index];
		}

		M3SettingRow.End(row);
	}

	private static void DrawPlanRows(ICustomRotation rotation, string job, List<CdPlanFolder> folders, ICdPlanRotation option, bool inDuty)
	{
		var locked = TimelineUpdater.PlanLocked;
		foreach (var f in folders)
		{
			var title = folders.Count > 1 ? $"{job} plan ({f.Folder})" : $"{job} plan";
			DrawProfileRow(rotation, job, f, option, inDuty);

			if (f.Plans.Count == 0)
			{
				DrawPickerInfoRow(title, "None", $"No *.{job}.plan.csv in {f.Folder}.");
				continue;
			}

			var names = new string[f.Plans.Count];
			var chosen = TimelineUpdater.ChosenPlanFile(f.Path, job);
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
					TimelineUpdater.ChoosePlan(f.Path, job, f.Plans[index].FileName);
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

	private static List<string> Distinct(List<CdPlanFolder> folders, Func<CdPlanFolder, bool> where, Func<CdPlanFolder, string> select)
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
