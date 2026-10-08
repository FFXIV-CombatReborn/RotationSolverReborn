using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.UI.SearchableConfigs;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawRotation()
	{
		var rotation = DataCenter.CurrentRotation;
		if (rotation == null)
		{
			return;
		}

		var desc = rotation.Description;
		if (!string.IsNullOrEmpty(desc))
		{
			using var card = M3Card.Begin("rotation_summary", rotation.Name, FontAwesomeIcon.Sync,
				rotation.IsExtra() ? M3.Scheme.Tertiary : M3.Scheme.Primary, M3CardStyle.Elevated);
			using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.95f));
			ImGuiEx.TextWrappedCopy(desc);
		}

		_rotationHeader.Draw();
	}

	private static readonly CollapsingHeaderGroup _rotationHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_Rotation_Description.GetDescription(), DrawRotationDescription },
			{ GetRotationStatusHead, DrawRotationStatus },
			{ () => UiString.ConfigWindow_Rotation_Configuration.GetDescription(), DrawRotationConfiguration },
		},
		(UiString.ConfigWindow_Rotation_Description, FontAwesomeIcon.BookOpen),
		(UiString.ConfigWindow_Rotation_Status, FontAwesomeIcon.HeartBroken),
		(UiString.ConfigWindow_Rotation_Configuration, FontAwesomeIcon.SlidersH));

	private const float DESC_SIZE = 24;
	private static void DrawRotationDescription()
	{
		var rotation = DataCenter.CurrentRotation;
		if (rotation == null)
		{
			return;
		}

		var descriptions = GetRotationDescriptions(rotation.GetType());

		using var table = ImRaii.Table("Rotation Description", 2, ImGuiTableFlags.Borders
			| ImGuiTableFlags.Resizable
			| ImGuiTableFlags.SizingStretchProp);
		if (table)
		{
			foreach (var attr in descriptions)
			{
				List<IBaseAction> allActions = [];
				foreach (var actionId in attr.Actions)
				{
					IBaseAction? action = null;
					foreach (var baseAction in rotation.AllBaseActions)
					{
						if (baseAction.ID == (uint)actionId)
						{
							action = baseAction;
							break;
						}
					}
					if (action != null)
					{
						allActions.Add(action);
					}
				}

				var hasDesc = !string.IsNullOrEmpty(attr.Description);

				if (!hasDesc && allActions.Count == 0)
				{
					continue;
				}

				ImGui.TableNextRow();
				_ = ImGui.TableNextColumn();

				if (IconSet.GetTexture(attr.IconID, out var image) && image?.Handle != null)
				{
					ImGui.Image(image.Handle, Vector2.One * DESC_SIZE * Scale);
				}

				ImGui.SameLine();
				var isOnCommand = attr.IsOnCommand;
				if (isOnCommand)
				{
					ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
				}

				ImGui.Text(" " + attr.Type.GetDescription());
				if (isOnCommand)
				{
					ImGui.PopStyleColor();
				}

				_ = ImGui.TableNextColumn();

				if (hasDesc)
				{
					ImGui.Text(attr.Description);
				}

				var notStart = false;
				var size = DESC_SIZE * Scale;
				var y = ImGui.GetCursorPosY() + (size * 4 / 82);
				foreach (var item in allActions)
				{
					if (item == null)
					{
						continue;
					}

					if (notStart)
					{
						ImGui.SameLine();
					}

					if (item.GetTexture(out var texture))
					{
						ImGui.SetCursorPosY(y);
						var cursor = ImGui.GetCursorPos();
						_ = ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * size);
						ImGuiHelper.DrawActionOverlay(cursor, size, 1);
						ImguiTooltips.HoveredTooltip(item.Name);
					}
					notStart = true;
				}
			}
		}
	}

	private static readonly Dictionary<Type, List<RotationDescAttribute>> _rotationDescriptions = [];

	private static List<RotationDescAttribute> GetRotationDescriptions(Type type)
	{
		if (_rotationDescriptions.TryGetValue(type, out var descriptions))
		{
			return descriptions;
		}

		List<RotationDescAttribute?> attrs = [RotationDescAttribute.MergeToOne(type.GetCustomAttributes<RotationDescAttribute>())];
		foreach (var m in type.GetAllMethodInfo())
		{
			attrs.Add(RotationDescAttribute.MergeToOne(m.GetCustomAttributes<RotationDescAttribute>()));
		}

		descriptions = [];
		foreach (var group in RotationDescAttribute.Merge(attrs))
		{
			var attr = RotationDescAttribute.MergeToOne(group);
			if (attr != null)
			{
				descriptions.Add(attr);
			}
		}

		_rotationDescriptions[type] = descriptions;
		return descriptions;
	}

	private static string GetRotationStatusHead()
	{
		var rotation = DataCenter.CurrentRotation;
		var status = UiString.ConfigWindow_Rotation_Status.GetDescription();
		return rotation == null ? string.Empty : status;
	}

	private static void DrawRotationStatus()
	{
		DataCenter.CurrentRotation?.DisplayRotationStatus();
	}

	private static string ToCommandStr(OtherCommandType type, string str, string extra = "")
	{
		var result = Service.COMMAND + " " + type.ToString() + " " + str;
		if (!string.IsNullOrEmpty(extra))
		{
			result += " " + extra;
		}

		return result;
	}

	private static bool ShouldShowRotationConfig(IRotationConfig config, IRotationConfigSet configSet)
	{
		if (string.IsNullOrEmpty(config.Parent))
		{
			return true;
		}

		_visitingConfigs.Clear();
		return ShouldShowRotationConfigInternal(config, configSet, _visitingConfigs);
	}

	private static readonly Dictionary<(Type RotationType, string ConfigName), string?> _attributeTooltips = [];
	private static readonly Dictionary<Type, PropertyInfo?> _tooltipProperties = [];

	private static string? GetRotationConfigTooltip(ICustomRotation rotation, IRotationConfig config)
	{
		var attributeKey = (rotation.GetType(), config.Name);
		if (!_attributeTooltips.TryGetValue(attributeKey, out var tooltip))
		{
			tooltip = attributeKey.Item1.GetProperty(config.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?.GetCustomAttribute<RotationConfigAttribute>()?.Tooltip;
			_attributeTooltips[attributeKey] = tooltip;
		}

		if (!string.IsNullOrEmpty(tooltip))
		{
			return tooltip;
		}

		var configType = config.GetType();
		if (!_tooltipProperties.TryGetValue(configType, out var tooltipProperty))
		{
			tooltipProperty = configType.GetProperty("Tooltip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_tooltipProperties[configType] = tooltipProperty;
		}

		return tooltipProperty?.GetValue(config) as string;
	}

	private static readonly HashSet<string> _visitingConfigs = new(StringComparer.Ordinal);
	private static readonly Dictionary<Type, PropertyInfo?> _parentValueProperties = [];

	private static bool ShouldShowRotationConfigInternal(
		IRotationConfig config,
		IRotationConfigSet configSet,
		HashSet<string> visiting)
	{
		if (string.IsNullOrEmpty(config.Parent))
		{
			return true;
		}

		if (!visiting.Add(config.Name))
		{
			return false;
		}

		IRotationConfig? parentConfig = null;
		foreach (var c in configSet.Configs)
		{
			if (c.Name == config.Parent)
			{
				parentConfig = c;
				break;
			}
		}

		if (parentConfig == null)
		{
			visiting.Remove(config.Name);
			return true;
		}

		if (!ShouldShowRotationConfigInternal(parentConfig, configSet, visiting))
		{
			visiting.Remove(config.Name);
			return false;
		}

		if (parentConfig is RotationConfigBoolean parentBool)
		{
			if (!bool.TryParse(parentBool.Value, out var isEnabled) || !isEnabled)
			{
				visiting.Remove(config.Name);
				return false;
			}
		}
		else
		{
			var configType = config.GetType();
			if (!_parentValueProperties.TryGetValue(configType, out var parentValueProperty))
			{
				parentValueProperty = configType.GetProperty("ParentValue");
				_parentValueProperties[configType] = parentValueProperty;
			}

			if (parentValueProperty != null)
			{
				var parentValue = parentValueProperty.GetValue(config);
				if (parentValue != null)
				{
					var parentValueStr = parentValue.ToString();
					if (parentValue.GetType().IsEnum && parentValueStr != null && parentValueStr.Contains('.'))
					{
						var dotIndex = parentValueStr.LastIndexOf('.');
						parentValueStr = dotIndex >= 0 ? parentValueStr[(dotIndex + 1)..] : parentValueStr;
					}

					if (parentConfig.Value == null ||
						!string.Equals(parentConfig.Value.Trim(), parentValueStr?.Trim(),
							StringComparison.OrdinalIgnoreCase))
					{
						visiting.Remove(config.Name);
						return false;
					}
				}
			}
		}

		visiting.Remove(config.Name);
		return true;
	}

	private static Vector2 ResolveRotationConfigControlSize(IRotationConfig config)
	{
		return config switch
		{
			RotationConfigBoolean => M3Widgets.SwitchSize(),
			RotationConfigCombo combo => new Vector2(ComboWidth(combo), M3Widgets.ComboHeight),
			RotationConfigString => new Vector2(240f * Scale, M3Widgets.ComboHeight),
			_ => new Vector2((Searchable.DRAG_WIDTH * Scale) + M3Widgets.SliderValueGutter("000.00%"), M3Widgets.ButtonHeight),
		};

		static float ComboWidth(RotationConfigCombo combo)
		{
			var names = combo.DisplayValues;
			var index = SelectedComboIndex(combo);
			var label = index < names.Length ? names[index] : string.Empty;
			return MathF.Min(MathF.Max(M3Widgets.ComboWidthFor(label), Searchable.DRAG_WIDTH * Scale),
				M3SettingRow.MaxControlWidth());
		}
	}

	private static int SelectedComboIndex(RotationConfigCombo combo)
	{
		return Math.Max(0, Array.FindIndex(combo.DisplayValues, n => n.Equals(combo.Value, StringComparison.OrdinalIgnoreCase)));
	}

	private static void DrawRotationConfigControl(IRotationConfig config, string id, float controlWidth)
	{
		switch (config)
		{
			case RotationConfigCombo combo:
				{
					var names = combo.DisplayValues;
					var index = SelectedComboIndex(combo);
					if (M3Widgets.Combo($"{id}_combo", ref index, names, controlWidth) && index < names.Length)
					{
						combo.Value = names[index];
					}

					break;
				}

			case RotationConfigBoolean:
				{
					if (bool.TryParse(config.Value, out var flag) && M3Widgets.Switch($"{id}_switch", ref flag))
					{
						config.Value = flag.ToString();
					}

					break;
				}

			case RotationConfigFloat f:
				{
					if (!float.TryParse(config.Value, out var value))
					{
						break;
					}

					var trackWidth = Searchable.DRAG_WIDTH * Scale;
					if (f.UnitType == ConfigUnitType.Percent)
					{
						var display = value * 100f;
						if (M3Widgets.Slider($"{id}_slider", ref display, f.Min * 100f, f.Max * 100f,
							$"{display:F1}{f.UnitType.ToSymbol()}", trackWidth))
						{
							config.Value = (display / 100f).ToString();
						}
					}
					else if (M3Widgets.Slider($"{id}_slider", ref value, f.Min, f.Max,
						$"{value:F2}{f.UnitType.ToSymbol()}", trackWidth))
					{
						config.Value = value.ToString();
					}

					break;
				}

			case RotationConfigInt i:
				{
					if (int.TryParse(config.Value, out var value)
						&& M3Widgets.SliderInt($"{id}_slider", ref value, i.Min, i.Max, value.ToString(), Searchable.DRAG_WIDTH * Scale))
					{
						config.Value = value.ToString();
					}

					break;
				}

			case RotationConfigString:
				{
					var value = config.Value;
					ImGui.SetNextItemWidth(controlWidth);
					if (ImGui.InputTextWithHint($"{id}_text", config.DisplayName, ref value, 128))
					{
						config.Value = value;
					}

					break;
				}
		}
	}

	private static void DrawRotationConfiguration()
	{
		var rotation = DataCenter.CurrentRotation;
		if (rotation == null || !Player.Available)
		{
			return;
		}

		var enable = rotation.IsEnabled;
		var enableRow = M3SettingRow.Begin(rotation.Name ?? "Rotation",
			"Turn this rotation off to stop Rotation Solver from driving it.", M3Widgets.SwitchSize());
		ImGui.SetCursorScreenPos(enableRow.ControlPosition);
		if (M3Widgets.Switch("##rotation_enabled_switch", ref enable))
		{
			rotation.IsEnabled = enable;
		}

		M3SettingRow.End(enableRow);

		if (!enable)
		{
			return;
		}

		var set = rotation.Configs;
		var drewSeparator = false;

		foreach (var config in set.Configs)
		{
			var requiredType = DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE;
			if (!config.Type.HasFlag(requiredType) || !ShouldShowRotationConfig(config, set))
			{
				continue;
			}

			if (!drewSeparator)
			{
				M3Widgets.Divider(6f);
				drewSeparator = true;
			}

			var tooltip = GetRotationConfigTooltip(rotation, config);
			var supporting = Service.Config.UiInlineDescriptions ? tooltip : null;

			DrawRotationConfigRow(config, rotation, OtherCommandType.Rotations, supporting, tooltip);
		}

		DrawJobTargetPriorities();
	}

	private static void DrawJobTargetPriorities()
	{
		if (!Player.Available || DataCenter.PartyMembers == null || Player.Object == null)
		{
			return;
		}

		if (Player.Object.IsJobs(Job.DNC))
		{
			DrawJobPriorityList("Dance Partner Priority", "DancePartner",
				OtherConfiguration.DancePartnerPriority,
				OtherConfiguration.ResetDancePartnerPriority,
				list =>
				{
					OtherConfiguration.DancePartnerPriority = list;
					_ = OtherConfiguration.SaveDancePartnerPriority();
				});
		}

		if (Player.Object.IsJobs(Job.SGE))
		{
			DrawJobPriorityList("Kardia Tank Priority", "KardiaTank",
				OtherConfiguration.KardiaTankPriority,
				OtherConfiguration.ResetKardiaTankPriority,
				list =>
				{
					OtherConfiguration.KardiaTankPriority = list;
					_ = OtherConfiguration.SaveKardiaTankPriority();
				});
		}

		if (!Player.Object.IsJobs(Job.AST))
		{
			return;
		}

		using var table = ImRaii.Table("AstCardPriorityTable", 2, ImGuiTableFlags.SizingStretchProp);
		if (!table)
		{
			return;
		}

		ImGui.TableNextColumn();
		DrawJobPriorityList("Spear Card Priority", "Spear",
			OtherConfiguration.TheSpearPriority,
			OtherConfiguration.ResetTheSpearPriority,
			list =>
			{
				OtherConfiguration.TheSpearPriority = list;
				_ = OtherConfiguration.SaveTheSpearPriority();
			});

		ImGui.TableNextColumn();
		DrawJobPriorityList("Balance Card Priority", "Balance",
			OtherConfiguration.TheBalancePriority,
			OtherConfiguration.ResetTheBalancePriority,
			list =>
			{
				OtherConfiguration.TheBalancePriority = list;
				_ = OtherConfiguration.SaveTheBalancePriority();
			});
	}

	private static void DrawJobPriorityList(string title, string id, IEnumerable<Job> priority, Action reset, Action<List<Job>> save)
	{
		ImGui.Spacing();
		ImGui.Text(title);
		ImGui.Spacing();

		if (ImGui.Button($"Reset to Default##{id}"))
		{
			reset();
		}

		ImGui.Spacing();

		List<Job> working = [.. priority];
		var orderChanged = false;

		// EndChild must run whatever BeginChild returns.
		if (ImGui.BeginChild($"{id}PriorityList", new Vector2(0, 200 * Scale), true))
		{
			for (var i = 0; i < working.Count; i++)
			{
				if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##Up{id}{i}") && i > 0)
				{
					(working[i - 1], working[i]) = (working[i], working[i - 1]);
					orderChanged = true;
				}

				ImGui.SameLine();

				if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##Down{id}{i}") && i < working.Count - 1)
				{
					(working[i + 1], working[i]) = (working[i], working[i + 1]);
					orderChanged = true;
				}

				ImGui.SameLine();
				ImGui.Text(working[i].ToString());
			}
		}

		ImGui.EndChild();

		if (orderChanged)
		{
			save(working);
		}
	}
}
