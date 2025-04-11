using RotationSolver.Basic.Configuration;
using RotationSolver.UI.SearchableSettings;

namespace RotationSolver.UI.SearchableConfigs;

internal class AutoHealCheckBox(PropertyInfo property, params ISearchable[] otherChildren)
    : CheckBoxSearchCondition(property, otherChildren.Union(new ISearchable[]
        {
            _healthAreaAbility,
            _healthAreaAbilityHot,
            _healthAreaSpell,
            _healthAreaSpellHot,
            _healthSingleAbility,
            _healthSingleAbilityHot,
            _healthSingleSpell,
            _healthSingleSpellHot,
        }).ToArray())
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

    // Method to create DragFloatSearch instances with null checks
    private static DragFloatSearch CreateDragFloatSearch(string propertyName)
    {
        var property = typeof(Configs).GetRuntimeProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found in Configs.");
        }
        return new DragFloatSearch(property);
    }

    protected override void DrawChildren()
    {
        // Draw other children
        foreach (var child in _otherChildren)
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

            ImGui.TableNextColumn();
            ImGui.TableHeader("");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Normal Targets");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Targets with Heal-over-Time");

            DrawHealthRow("HP threshold for AoE healing oGCDs", _healthAreaAbility, _healthAreaAbilityHot);
            DrawHealthRow("HP threshold for AoE healing GCDs", _healthAreaSpell, _healthAreaSpellHot);
            DrawHealthRow("HP threshold for single-target healing oGCDs", _healthSingleAbility, _healthSingleAbilityHot);
            DrawHealthRow("HP threshold for single-target healing GCDs", _healthSingleSpell, _healthSingleSpellHot);

            ImGui.EndTable();
        }
    }

    // Helper method to draw a row in the health table
    private void DrawHealthRow(string description, DragFloatSearch normalTarget, DragFloatSearch hotTarget)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(description);

        ImGui.TableNextColumn();
        normalTarget?.Draw();

        ImGui.TableNextColumn();
        hotTarget?.Draw();
    }
}
