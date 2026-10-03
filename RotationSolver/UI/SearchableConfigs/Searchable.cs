using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using RotationSolver.Data;
using RotationSolver.UI.Material;

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

	public JobRole[]? JobRoles { get; init; }

	public Job[]? Jobs { get; init; }

	public bool CanDraw
	{
		get
		{
			var hasRoles = JobRoles is { Length: > 0 };
			var hasJobs = Jobs is { Length: > 0 };
			if (!hasRoles && !hasJobs)
			{
				return true;
			}

			// With no rotation loaded there's no role to check, so the role filter can't hide anything.
			var role = DataCenter.CurrentRotation?.Role;
			return (hasRoles && (!role.HasValue || JobRoles!.Contains(role.Value)))
				|| (hasJobs && Jobs!.Contains(DataCenter.Job));
		}
	}

	public Job[] AllJobs
	{
		get
		{
			List<Job> jobs = [];

			if (JobRoles != null)
			{
				foreach (var role in JobRoles)
				{
					var roleJobs = JobRoleExtension.ToJobs(role);
					if (roleJobs == null)
					{
						continue;
					}

					foreach (var job in roleJobs)
					{
						if (!jobs.Contains(job))
						{
							jobs.Add(job);
						}
					}
				}
			}

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
			var sheet = Svc.Data.GetExcelSheet<ClassJob>();
			var sb = new System.Text.StringBuilder();
			foreach (var job in AllJobs)
			{
				if (sb.Length > 0)
				{
					_ = sb.Append('\n');
				}

				_ = sb.Append(sheet?.GetRow((uint)job).Name ?? job.ToString());
			}

			return string.Format(UiString.NotInJob.GetDescription(), sb.ToString());
		}
	}
}

internal abstract class Searchable(PropertyInfo property) : ISearchable
{
	protected readonly PropertyInfo _property = property;

	// GetCustomAttribute builds a new attribute instance on every call, and these are read several
	// times per frame for every visible setting, so look them up once.
	private readonly UIAttribute? _ui = property.GetCustomAttribute<UIAttribute>();
	private readonly bool _isJob = property.GetCustomAttribute<JobConfigAttribute>() != null
		|| property.GetCustomAttribute<JobChoiceConfigAttribute>() != null;
	private string? _popupKey;

	public const float DRAG_WIDTH = 150;

	protected static float Scale => M3.Scale;

	public CheckBoxSearch? Parent { get; set; }
	public JobFilter PvPFilter { get; set; }
	public JobFilter PvEFilter { get; set; }

	public virtual string SearchingKeys => Name + " " + Description;
	public virtual string Name => _ui?.Name ?? string.Empty;
	public virtual string Description => string.IsNullOrEmpty(_ui?.Description) ? string.Empty : _ui.Description;

	public virtual string Filter => _ui?.Filter ?? string.Empty;

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
	protected bool IsJob => _isJob;
	protected string PopupKey => _popupKey ??= $"Rotation Solver RightClicking##{ID}_{GetHashCode()}";

	protected string? SupportingText
		=> Service.Config.UiInlineDescriptions && !string.IsNullOrEmpty(Description) ? Description : null;

	protected FontAwesomeIcon RowIcon => IsJob ? FontAwesomeIcon.UserCog : FontAwesomeIcon.None;

	public virtual void Draw()
	{
		var filter = DataCenter.IsPvP ? PvPFilter : PvEFilter;

		if (!filter.CanDraw)
		{
			if (filter.AllJobs.Length == 0)
			{
				return;
			}

			DrawUnavailable(filter);
			return;
		}

		DrawMain();
		PreparePopup();
	}

	protected abstract void DrawMain();

	protected virtual void PreparePopup()
	{
		if (ImGui.IsPopupOpen(PopupKey))
		{
			ImGuiHelper.PrepareGroup(PopupKey, Command, ResetToDefault);
		}
	}

	private void DrawUnavailable(JobFilter filter)
	{
		var row = M3SettingRow.Begin(Name, null, Vector2.Zero,
			leadingIcon: FontAwesomeIcon.Ban, disabled: true, strikeThrough: true);

		if (row.Hovered)
		{
			ImguiTooltips.ShowTooltip(filter.Description);
		}

		M3SettingRow.End(row);
	}

	protected void RowTooltip(in M3RowInfo row, string hint)
	{
		var description = Description;
		if (!row.Hovered || string.IsNullOrEmpty(description) || SupportingText != null)
		{
			return;
		}

		ImguiTooltips.ShowTooltip(() =>
		{
			ImGui.BulletText(description);
			ImGui.Separator();
			ImGui.TextDisabled(hint);
		});
	}

	protected void RowInteractions(in M3RowInfo row)
	{
		RowTooltip(row, "Right-click for the matching chat command.");
		ImGuiHelper.ReactPopupAt(row.Hovered, PopupKey, Command, ResetToDefault, false);
	}

	protected void ShowTooltip(bool showHand = true)
	{
		var description = Description;
		if (!string.IsNullOrEmpty(description))
		{
			ImguiTooltips.ShowTooltip(() =>
			{
				ImGui.BulletText(description);
				ImGui.Separator();
			});
		}

		ImGuiHelper.ReactPopup(PopupKey, Command, ResetToDefault, showHand);
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

internal abstract class UnitSearchable(PropertyInfo property) : Searchable(property)
{
	public ConfigUnitType Unit { get; } = property.GetCustomAttribute<RangeAttribute>()?.UnitType ?? ConfigUnitType.None;

	public override string Description
	{
		get
		{
			var baseDesc = base.Description;
			return string.IsNullOrEmpty(baseDesc) ? Unit.ToString() : $"{baseDesc}\n{Unit}";
		}
	}

	// Percentages are stored as fractions but dragged and typed as whole percents.
	protected float SliderScale => Unit == ConfigUnitType.Percent ? 100f : 1f;

	protected string Format(float value)
	{
		return Unit == ConfigUnitType.Percent
			? $"{value * 100f:F1}{Unit.ToSymbol()}"
			: $"{value:F2}{Unit.ToSymbol()}";
	}
}
