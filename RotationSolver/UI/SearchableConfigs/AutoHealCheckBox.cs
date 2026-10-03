using Dalamud.Interface.Utility.Raii;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.UI.Material;

namespace RotationSolver.UI.SearchableConfigs;

internal class AutoHealCheckBox(PropertyInfo property, params ISearchable[] otherChildren)
	: CheckBoxCondition(property, ConcatChildren(otherChildren))
{
	private readonly ISearchable[] _otherChildren = otherChildren;

	private static readonly DragFloatSearch
		_healthAreaAbility = CreateDragFloatSearch(nameof(Configs.HealthAreaAbility)),
		_healthAreaAbilityHot = CreateDragFloatSearch(nameof(Configs.HealthAreaAbilityHot)),
		_healthAreaSpell = CreateDragFloatSearch(nameof(Configs.HealthAreaSpell)),
		_healthAreaSpellHot = CreateDragFloatSearch(nameof(Configs.HealthAreaSpellHot)),
		_healthSingleAbility = CreateDragFloatSearch(nameof(Configs.HealthSingleAbility)),
		_healthSingleAbilityHot = CreateDragFloatSearch(nameof(Configs.HealthSingleAbilityHot)),
		_healthSingleSpell = CreateDragFloatSearch(nameof(Configs.HealthSingleSpell)),
		_healthSingleSpellHot = CreateDragFloatSearch(nameof(Configs.HealthSingleSpellHot));

	private static ISearchable[] ConcatChildren(ISearchable[] otherChildren)
	{
		ISearchable[] healthChildren =
		[
			_healthAreaAbility,
			_healthAreaAbilityHot,
			_healthAreaSpell,
			_healthAreaSpellHot,
			_healthSingleAbility,
			_healthSingleAbilityHot,
			_healthSingleSpell,
			_healthSingleSpellHot,
		];

		var result = new ISearchable[otherChildren.Length + healthChildren.Length];
		otherChildren.CopyTo(result, 0);
		healthChildren.CopyTo(result, otherChildren.Length);
		return result;
	}

	private static DragFloatSearch CreateDragFloatSearch(string propertyName)
	{
		var property = typeof(Configs).GetRuntimeProperty(propertyName);
		return property == null
			? throw new ArgumentException($"Property '{propertyName}' not found in Configs.")
			: new DragFloatSearch(property);
	}

	protected override void DrawChildren()
	{
		foreach (var child in _otherChildren)
		{
			child.Draw();
		}

		M3Widgets.SectionLabel(UiString.ConfigWindow_HealingThresholds.GetDescription());

		using var table = ImRaii.Table("Healing things", 3, ImGuiTableFlags.BordersInnerH
			| ImGuiTableFlags.Resizable
			| ImGuiTableFlags.SizingStretchProp);
		if (!table)
		{
			return;
		}

		ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

		_ = ImGui.TableNextColumn();
		ImGui.TableHeader(string.Empty);

		_ = ImGui.TableNextColumn();
		ImGui.TableHeader(UiString.NormalTargets.GetDescription());

		_ = ImGui.TableNextColumn();
		ImGui.TableHeader(UiString.HotTargets.GetDescription());

		DrawHealthRow(UiString.HpAoe0Gcd.GetDescription(), _healthAreaAbility, _healthAreaAbilityHot);
		DrawHealthRow(UiString.HpAoeGcd.GetDescription(), _healthAreaSpell, _healthAreaSpellHot);
		DrawHealthRow(UiString.HpSingle0Gcd.GetDescription(), _healthSingleAbility, _healthSingleAbilityHot);
		DrawHealthRow(UiString.HpSingleGcd.GetDescription(), _healthSingleSpell, _healthSingleSpellHot);
	}

	private static void DrawHealthRow(string description, DragFloatSearch normalTarget, DragFloatSearch hotTarget)
	{
		ImGui.TableNextRow();
		_ = ImGui.TableNextColumn();
		ImGui.AlignTextToFramePadding();
		ImGui.TextWrapped(description);

		_ = ImGui.TableNextColumn();
		normalTarget.DrawCompact(ImGui.GetContentRegionAvail().X);

		_ = ImGui.TableNextColumn();
		hotTarget.DrawCompact(ImGui.GetContentRegionAvail().X);
	}
}
