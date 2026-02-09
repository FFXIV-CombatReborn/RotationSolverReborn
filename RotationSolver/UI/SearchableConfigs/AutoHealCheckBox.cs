using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI.SearchableConfigs;

internal class AutoHealCheckBox(PropertyInfo property, params ISearchable[] otherChildren)
    : CheckBoxCondition(property, ConcatChildren(otherChildren))
{
    private readonly ISearchable[] _otherChildren = otherChildren;


    // Static fields for health-related properties
    private static readonly DragFloatSearch
        _healthAreaAbility = CreateDragFloatSearch(nameof(Configs.HealthAreaAbility)),
        _healthAreaAbilityHot = CreateDragFloatSearch(nameof(Configs.HealthAreaAbilityHot)),
        _healthAreaSpell = CreateDragFloatSearch(nameof(Configs.HealthAreaSpell)),
        _healthAreaSpellHot = CreateDragFloatSearch(nameof(Configs.HealthAreaSpellHot)),
        _healthSingleAbility = CreateDragFloatSearch(nameof(Configs.HealthSingleAbility)),
        _healthSingleAbilityHot = CreateDragFloatSearch(nameof(Configs.HealthSingleAbilityHot)),
        _healthSingleSpell = CreateDragFloatSearch(nameof(Configs.HealthSingleSpell)),
        _healthSingleSpellHot = CreateDragFloatSearch(nameof(Configs.HealthSingleSpellHot));

    // Helper to concatenate arrays without LINQ
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

        ISearchable[] result = new ISearchable[otherChildren.Length + healthChildren.Length];
        otherChildren.CopyTo(result, 0);
        healthChildren.CopyTo(result, otherChildren.Length);
        return result;
    }

    // Method to create DragFloatSearch instances with null checks
    private static DragFloatSearch CreateDragFloatSearch(string propertyName)
    {
        PropertyInfo? property = typeof(Configs).GetRuntimeProperty(propertyName);
        return property == null
            ? throw new ArgumentException($"Property '{propertyName}' not found in Configs.")
            : new DragFloatSearch(property);
    }

    protected override void DrawChildren()
    {
        // Draw other children
        foreach (ISearchable child in _otherChildren)
        {
            child.Draw();
        }

        // Draw health-related properties in a table
        if (ImGui.BeginTable("Healing things", 3, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("");

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.NormalTargets.GetDescription());

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.HotTargets.GetDescription());

            DrawHealthRow(UiString.HpAoe0Gcd.GetDescription(), _healthAreaAbility, _healthAreaAbilityHot);
            DrawHealthRow(UiString.HpAoeGcd.GetDescription(), _healthAreaSpell, _healthAreaSpellHot);
            DrawHealthRow(UiString.HpSingle0Gcd.GetDescription(), _healthSingleAbility, _healthSingleAbilityHot);
            DrawHealthRow(UiString.HpSingleGcd.GetDescription(), _healthSingleSpell, _healthSingleSpellHot);

            ImGui.EndTable();
        }
    }

    // Helper method to draw a row in the health table
    private static void DrawHealthRow(string description, DragFloatSearch normalTarget, DragFloatSearch hotTarget)
    {
        ImGui.TableNextRow();
        _ = ImGui.TableNextColumn();
        ImGui.Text(description);

        _ = ImGui.TableNextColumn();
        normalTarget?.Draw();

        _ = ImGui.TableNextColumn();
        hotTarget?.Draw();
    }
}