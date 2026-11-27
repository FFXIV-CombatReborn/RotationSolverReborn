using System.Collections.Concurrent;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using RebornMaterial;
using RotationSolver.Commands;

namespace RotationSolver.UI.SearchableConfigs;

internal readonly struct JobFilter
{
	private static readonly ConcurrentDictionary<JobFilterType, string> _typeDescriptionCache = [];
	private readonly JobFilterType? _filterType;

	public JobFilter(JobFilterType type)
	{
		_filterType = type;
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
			if (JobRoles == null && Jobs == null)
			{
				return true;
			}

			var hasRoles = JobRoles is { Length: > 0 };
			var hasJobs = Jobs is { Length: > 0 };
			if (!hasRoles && !hasJobs)
			{
				return false;
			}

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
			if (_filterType is { } filterType)
			{
				var jobs = AllJobs;
				return _typeDescriptionCache.GetOrAdd(filterType, _ => BuildDescription(jobs));
			}

			return BuildDescription(AllJobs);
		}
	}

	private static string BuildDescription(Job[] allJobs)
	{
		var sheet = Svc.Data.GetExcelSheet<ClassJob>();
		var sb = new System.Text.StringBuilder();
		foreach (var job in allJobs)
		{
			if (sb.Length > 0)
			{
				_ = sb.Append('\n');
			}

			var name = sheet != null && sheet.TryGetRow((uint)job, out var row)
				? row.Name.ToString()
				: job.ToString();
			_ = sb.Append(name);
		}

		return string.Format(UiString.NotInJob.GetDescription(), sb.ToString());
	}
}

internal abstract class Searchable(PropertyInfo property) : ISearchable
{
	protected readonly PropertyInfo _property = property;
	private readonly UIAttribute? _ui = property.GetCustomAttribute<UIAttribute>();
	private readonly bool _isJob = property.GetCustomAttribute<JobConfigAttribute>() != null
		|| property.GetCustomAttribute<JobChoiceConfigAttribute>() != null;
	private string? _popupKey;
	private string? _searchingKeys;
	private string? _controlId;
	private string? _pvpFilterDescription;
	private string? _pveFilterDescription;

	public const float DRAG_WIDTH = 150;

	protected static float Scale => M3.Scale;

	public CheckBoxSearch? Parent { get; set; }
	public JobFilter PvPFilter { get; set; }
	public JobFilter PvEFilter { get; set; }

	public virtual string SearchingKeys => _searchingKeys ??= Name + " " + Description;
	public virtual string Name => _ui?.Name ?? string.Empty;
	public virtual string Description => string.IsNullOrEmpty(_ui?.Description) ? string.Empty : _ui.Description;

	public virtual string Filter => _ui?.Filter ?? string.Empty;

	public virtual string Command
	{
		get
		{
			var result = Service.COMMAND + " " + OtherCommandType.Settings.ToString() + " " + _property.Name;
			var extra = RSCommands.FormatSettingValue(_property.GetValue(Service.ConfigDefault));
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
	protected string ControlId => _controlId ??= $"##Config_{ID}{GetHashCode()}";

	internal static Func<Configs, T>? CreateGetter<T>(PropertyInfo property)
	{
		var getter = property.GetMethod;
		return getter != null && typeof(Configs).IsAssignableFrom(property.DeclaringType) && property.PropertyType == typeof(T)
			? getter.CreateDelegate<Func<Configs, T>>()
			: null;
	}

	internal static Action<Configs, T>? CreateSetter<T>(PropertyInfo property)
	{
		var setter = property.SetMethod;
		return setter != null && typeof(Configs).IsAssignableFrom(property.DeclaringType) && property.PropertyType == typeof(T)
			? setter.CreateDelegate<Action<Configs, T>>()
			: null;
	}

	protected string? SupportingText
		=> Service.Config.UiInlineDescriptions && !string.IsNullOrEmpty(Description) ? Description : null;

	protected FontAwesomeIcon RowIcon => IsJob ? FontAwesomeIcon.UserCog : FontAwesomeIcon.None;

	public virtual void Draw()
	{
		var isPvp = DataCenter.IsPvP;
		var filter = isPvp ? PvPFilter : PvEFilter;

		if (!filter.CanDraw)
		{
			if (filter.AllJobs.Length == 0)
			{
				return;
			}

			DrawUnavailable(filter, isPvp);
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

	private void DrawUnavailable(JobFilter filter, bool isPvp)
	{
		var row = M3SettingRow.Begin(Name, null, Vector2.Zero,
			leadingIcon: FontAwesomeIcon.Ban, disabled: true, strikeThrough: true);

		if (row.Hovered)
		{
			var desc = isPvp
				? (_pvpFilterDescription ??= filter.Description)
				: (_pveFilterDescription ??= filter.Description);
			ImguiTooltips.ShowTooltip(desc);
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
		ImGuiHelper.ReactPopupAt(row.Hovered, PopupKey, false);
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

		ImGuiHelper.ReactPopup(PopupKey, showHand);
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

	public float Step { get; } = property.GetCustomAttribute<RangeAttribute>()?.Step ?? 0f;

	public override string Description
	{
		get
		{
			var baseDesc = base.Description;
			return string.IsNullOrEmpty(baseDesc) ? Unit.ToString() : $"{baseDesc}\n{Unit}";
		}
	}

	protected float SliderScale => Unit == ConfigUnitType.Percent ? 100f : 1f;

	protected string SliderFormat => (Unit == ConfigUnitType.Percent ? "%.1f" : "%.2f") + Unit.ToSymbol().Replace("%", "%%");

	protected string Format(float value)
	{
		return Unit == ConfigUnitType.Percent
			? $"{(value * 100f).ToString(Step >= 0.01f ? "F0" : "F1")}{Unit.ToSymbol()}"
			: $"{value:F2}{Unit.ToSymbol()}";
	}
}
