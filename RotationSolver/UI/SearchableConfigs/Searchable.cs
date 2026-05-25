using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal readonly struct JobFilter
{
	public JobFilter(JobFilterType type)
	{
		switch (type)
		{
			case JobFilterType.NoJob:
				JobRoles = [];
				break;

			case JobFilterType.NoHealer:
				JobRoles =
				[
					JobRole.Tank,
					JobRole.Melee,
					JobRole.RangedMagical,
					JobRole.RangedPhysical,
				];
				break;

			case JobFilterType.Healer:
				JobRoles =
				[
					JobRole.Healer,
				];
				break;

			case JobFilterType.Raise:
				JobRoles =
				[
					JobRole.Healer,
				];
				Jobs =
				[
					Job.RDM,
					Job.SMN,
				];
				break;

			case JobFilterType.Interrupt:
				JobRoles =
				[
					JobRole.Tank,
					JobRole.Melee,
					JobRole.RangedPhysical,
				];
				Jobs =
				[
					Job.BLU,
				];
				break;

			case JobFilterType.Dispel:
				JobRoles =
				[
					JobRole.Healer,
				];
				Jobs =
				[
					Job.BRD,
				];
				break;

			case JobFilterType.Tank:
				JobRoles =
				[
					JobRole.Tank,
				];
				break;

			case JobFilterType.Melee:
				JobRoles =
				[
					JobRole.Melee,
				];
				break;
		}
	}

	/// <summary>
	/// Only these job roles can get this setting.
	/// </summary>
	public JobRole[]? JobRoles { get; init; }

	/// <summary>
	/// Or these jobs.
	/// </summary>
	public Job[]? Jobs { get; init; }

	public bool CanDraw
	{
		get
		{
			var canDraw = true;

			if (JobRoles is { Length: > 0 })
			{
				var role = DataCenter.CurrentRotation?.Role;
				if (role.HasValue)
				{
					canDraw = JobRoles.Contains(role.Value);
				}
			}

			if (Jobs is { Length: > 0 })
			{
				canDraw |= Jobs.Contains(DataCenter.Job);
			}

			return canDraw;
		}
	}

	public Job[] AllJobs
	{
		get
		{
			List<Job> jobs = [];

			// Add jobs from JobRoles via JobRoleExtension.ToJobs
			if (JobRoles != null)
			{
				foreach (var role in JobRoles)
				{
					var roleJobs = JobRoleExtension.ToJobs(role);
					if (roleJobs != null)
					{
						foreach (var job in roleJobs)
						{
							if (!jobs.Contains(job))
							{
								jobs.Add(job);
							}
						}
					}
				}
			}

			// Add jobs from Jobs array
			if (Jobs != null)
			{
				foreach (var job in Jobs)
				{
					if (!jobs.Contains(job))
					{
						jobs.Add(job);
					}
				}
			}

			return [.. jobs];
		}
	}

	public string Description
	{
		get
		{
			var sb = new System.Text.StringBuilder();
			var firstLine = true;
			foreach (var job in AllJobs)
			{
				var sheet = Svc.Data.GetExcelSheet<ClassJob>();
				var name = sheet?.GetRow((uint)job).Name ?? job.ToString();
				if (!firstLine)
				{
					sb.Append('\n');
				}

				sb.Append(name);
				firstLine = false;
			}
			var roleOrJob = sb.ToString();
			return string.Format(UiString.NotInJob.GetDescription(), roleOrJob);
		}
	}
}

internal abstract class Searchable(PropertyInfo property) : ISearchable
{
	protected readonly PropertyInfo _property = property;

	public const float DRAG_WIDTH = 150;
	protected static float Scale => ImGuiHelpers.GlobalScale;
	public CheckBoxSearch? Parent { get; set; } = null;

	public virtual string SearchingKeys => Name + " " + Description;
	public virtual string Name
	{
		get
		{
			var ui = _property.GetCustomAttribute<UIAttribute>();
			return ui == null ? string.Empty : ui.Name;
		}
	}

	public virtual string Description
	{
		get
		{
			var ui = _property.GetCustomAttribute<UIAttribute>();
			return ui == null || string.IsNullOrEmpty(ui.Description) ? string.Empty : ui.Description;
		}
	}

	// Expose the owning UI filter so callers can navigate to the correct menu
	public virtual string Filter
	{
		get
		{
			var ui = _property.GetCustomAttribute<UIAttribute>();
			return ui == null ? string.Empty : ui.Filter ?? string.Empty;
		}
	}

	public virtual string Command
	{
		get
		{
			var result = Service.COMMAND + " " + OtherCommandType.Settings.ToString() + " " + _property.Name;
			var extra = _property.GetValue(Service.ConfigDefault)?.ToString();
			if (!string.IsNullOrEmpty(extra))
			{
				result += " " + extra;
			}

			return result;
		}
	}
	public virtual string ID => _property.Name;
	private string Popup_Key => $"Rotation Solver RightClicking##{ID}_{GetHashCode()}";
	protected bool IsJob => _property.GetCustomAttribute<JobConfigAttribute>() != null
		|| _property.GetCustomAttribute<JobChoiceConfigAttribute>() != null;

	public uint Color { get; set; } = 0;

	public JobFilter PvPFilter { get; set; }
	public JobFilter PvEFilter { get; set; }

	public virtual bool ShowInChild => true;

	public virtual unsafe void Draw()
	{
		// Determine the appropriate filter based on the context (PvP or PvE)
		var filter = DataCenter.IsPvP ? PvPFilter : PvEFilter;

		// Check if the filter allows drawing
		if (!filter.CanDraw)
		{
			// If no jobs are available in the filter, return early
			if (filter.AllJobs.Length == 0)
			{
				return;
			}

			// Get the text color for disabled text
			var textColor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);

			// Push the disabled text color style
			ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));

			// Calculate the cursor position
			var cursor = ImGui.GetCursorPos() + ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());

			// Ensure Name is not null before using it
			if (!string.IsNullOrEmpty(Name))
			{
				ImGui.TextWrapped(Name);
			}

			// Pop the disabled text color style
			ImGui.PopStyleColor();

			// Calculate the text size and item rectangle size
			var step = ImGui.CalcTextSize(Name ?? string.Empty);
			var size = ImGui.GetItemRectSize();
			var height = step.Y / 2;
			var wholeWidth = step.X;

			// Draw lines to indicate disabled state
			while (height < size.Y)
			{
				var pt = cursor + new Vector2(0, height);
				ImGui.GetWindowDrawList().AddLine(pt, pt + new Vector2(Math.Min(wholeWidth, size.X), 0), ImGui.ColorConvertFloat4ToU32(textColor));
				height += step.Y;
				wholeWidth -= size.X;
			}

			// Show a tooltip with the filter description
			ImguiTooltips.HoveredTooltip(filter.Description);
			return;
		}

		// Draw the main content
		DrawMain();

		// Prepare the group for the popup menu
		ImGuiHelper.PrepareGroup(Popup_Key, Command, ResetToDefault);
	}

	protected abstract void DrawMain();

	protected void ShowTooltip(bool showHand = true)
	{
		var showDesc = !string.IsNullOrEmpty(Description);
		if (showDesc)
		{
			ImguiTooltips.ShowTooltip(() =>
			{
				if (showDesc)
				{
					ImGui.BulletText(Description);
				}
				if (showDesc)
				{
					ImGui.Separator();
				}
				var wholeWidth = ImGui.GetWindowWidth();

			});
		}

		ImGuiHelper.ReactPopup(Popup_Key, Command, ResetToDefault, showHand);
	}

	protected static void DrawJobIcon()
	{
		ImGui.SameLine();

		if (IconSet.GetTexture(IconSet.GetJobIcon(DataCenter.Job, IconType.Framed), out var texture))
		{
			ImGui.Image(texture.Handle, Vector2.One * 24 * ImGuiHelpers.GlobalScale);
			ImguiTooltips.HoveredTooltip(UiString.JobConfigTip.GetDescription());
		}
	}

	public virtual void ResetToDefault()
	{
		var v = _property.GetValue(Service.ConfigDefault);
		if (v != null)
		{
			_property.SetValue(Service.Config, v);
		}
	}
}
