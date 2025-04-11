using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration.Conditions;
using RotationSolver.Data;
using RotationSolver.Updaters;
using Action = System.Action;
using TargetType = RotationSolver.Basic.Configuration.Conditions.TargetType;

namespace RotationSolver.UI;

internal static class ConditionRenderer
{
    internal static void DrawMain(this ConditionSet? conditionSet, ICustomRotation? rotation)
    {
        if (conditionSet == null) return;
        if (rotation == null) return;

        DrawCondition(conditionSet.IsTrue(rotation), conditionSet.GetHashCode().ToString(), ref conditionSet.Not);
        ImGui.SameLine();
        conditionSet.Draw(rotation);
    }

    internal static void DrawCondition(bool? tag, string id, ref bool isNot)
    {
        float size = IconSize * (1 + 8 / 82);
        if (!tag.HasValue)
        {
            if (IconSet.GetTexture("ui/uld/image2.tex", out var texture, true) || IconSet.GetTexture(0u, out texture))
            {
                if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * size, false, id))
                {
                    isNot = !isNot;
                }

                ImguiTooltips.HoveredTooltip(string.Format("Click to make it reverse.\nIs reversed: {0}", isNot));
            }
        }
        else
        {
            if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out var texture, true))
            {
                if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * size,
                    new Vector2(tag.Value ? 0 : 0.5f, 0),
                    new Vector2(tag.Value ? 0.5f : 1, 1), isNot ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.8f, 0.5f, 0.2f)) : 0, id))
                {
                    isNot = !isNot;
                }
                ImguiTooltips.HoveredTooltip(string.Format("Click to make it reverse.\nIs reversed: {0}", isNot));
            }
        }
    }

    internal static void DrawCondition(bool? tag)
    {
        float size = IconSize * (1 + 8 / 82);

        if (!tag.HasValue)
        {
            if (IconSet.GetTexture("ui/uld/image2.tex", out var texture, true) || IconSet.GetTexture(0u, out texture))
            {
                ImGui.Image(texture.ImGuiHandle, Vector2.One * size);
            }
        }
        else
        {
            if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out var texture, true))
            {
                ImGui.Image(texture.ImGuiHandle, Vector2.One * size,
                    new Vector2(tag.Value ? 0 : 0.5f, 0),
                    new Vector2(tag.Value ? 0.5f : 1, 1));
            }
        }
    }

    private static IEnumerable<MemberInfo> GetAllMethods(this Type? type, Func<Type, IEnumerable<MemberInfo>> getFunc)
    {
        if (type == null || getFunc == null) return [];

        var methods = getFunc(type);
        return methods.Union(type.BaseType.GetAllMethods(getFunc));
    }

    public static bool DrawByteEnum<T>(string name, ref T value) where T : struct, Enum
    {
        var values = Enum.GetValues<T>().Where(i => i.GetAttribute<ObsoleteAttribute>() == null).ToHashSet().ToArray();
        var index = Array.IndexOf(values, value);
        var names = values.Select(v => v.GetDescription()).ToArray();

        if (ImGuiHelper.SelectableCombo(name, names, ref index))
        {
            value = values[index];
            return true;
        }
        return false;
    }
    public static bool DrawDragFloat2(ConfigUnitType type, string name, ref Vector2 value, string id, string name1, string name2)
    {
        ImGui.Text(name);
        id = "##" + id;
        var result = DrawDragFloat(type, name1 + id, ref value.X);
        result |= DrawDragFloat(type, name2 + id, ref value.Y);
        return result;
    }

    public static bool DrawDragFloat3(ConfigUnitType type, string name, ref Vector3 value, string id, string name1, string name2, string name3, Func<Vector3>? func = null)
    {
        var result = false;
        if (func == null)
        {
            ImGui.Text(name);
        }
        else
        {
            if (ImGui.Button(name + "##" + id))
            {
                value = func();
                result = true;
            }
        }

        id = "##" + id;
        result |= DrawDragFloat(type, name1 + id, ref value.X);
        result |= DrawDragFloat(type, name2 + id, ref value.Y);
        result |= DrawDragFloat(type, name3 + id, ref value.Z);
        return result;
    }

    public static bool DrawDragFloat(ConfigUnitType type, string name, ref float value, string tooltip = "")
    {
        // Display the value with appropriate formatting based on the unit type
        ImGui.SameLine();
        var show = type == ConfigUnitType.Percent ? $"{value * 100:F1}{type.ToSymbol()}" : $"{value:F2}{type.ToSymbol()}";

        // Set the width for the next item
        ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(show).X));

        // Draw the appropriate control based on the unit type
        var result = type == ConfigUnitType.Percent ? ImGui.SliderFloat(name, ref value, 0, 1, show)
            : ImGui.DragFloat(name, ref value, 0.1f, 0, 0, show);

        // Append the type description to the tooltip if it is not null or empty
        if (!string.IsNullOrEmpty(tooltip))
        {
            tooltip += "\n";
        }
        ImguiTooltips.HoveredTooltip(tooltip + type.GetDescription());

        return result;
    }

    public static bool DrawDragInt(string name, ref int value)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(value.ToString()).X));
        return ImGui.DragInt(name, ref value);
    }

    public static bool DrawCondition(ICondition condition, ref int index)
    {
        ImGui.SameLine();

        return ImGuiHelper.SelectableCombo($"##Comparation{condition.GetHashCode()}", [">", "<", "="], ref index);
    }

    internal static void SearchItemsReflection(string popId, string name, ref string searchTxt, MemberInfo[] actions, Action<MemberInfo> selectAction)
    {
        // Provide the missing 'searchingHint' parameter
        ImGuiHelper.SearchCombo(
            popId,
            name,
            ref searchTxt,
            actions,
            action => action.Name, // Assuming the function to get the name of the action
            selectAction,
            "Member Name",
            null, // Optional parameter, can be null if not needed
            null  // Optional parameter, can be null if not needed
        );
    }

    public static float IconSizeRaw => ImGuiHelpers.GetButtonSize("H").Y;
    public static float IconSize => IconSizeRaw * ImGuiHelpers.GlobalScale;
    private const int count = 8;
    public static void ActionSelectorPopUp(string popUpId, CollapsingHeaderGroup group, ICustomRotation rotation, Action<IAction> action, Action? others = null)
    {
        if (group == null) return;

        using var popUp = ImRaii.Popup(popUpId);

        if (!popUp.Success) return;

        others?.Invoke();

        group.ClearCollapsingHeader();

        foreach (var pair in RotationUpdater.GroupActions(rotation.AllBaseActions.Where(i => i.Action.IsInJob()))!)
        {
            group.AddCollapsingHeader(() => pair.Key, () =>
            {
                var index = 0;
                foreach (var item in pair.OrderBy(t => t.ID))
                {
                    if (!item.GetTexture(out var icon)) continue;

                    if (index++ % count != 0)
                    {
                        ImGui.SameLine();
                    }

                    using (var group = ImRaii.Group())
                    {
                        var cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * IconSize, group.GetHashCode().ToString()))
                        {
                            action?.Invoke(item);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
                    }

                    var name = item.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        ImguiTooltips.HoveredTooltip(name);
                    }
                }
            });
        }
        group.Draw();
    }

    #region Draw
    public static void Draw(this ICondition condition, ICustomRotation rotation)
    {
        if (rotation == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Rotation is null. Please log in or switch jobs!");
            return;
        }

        condition.CheckBefore(rotation);

        condition.DrawBefore();

        if (condition is DelayCondition delay) delay.DrawDelay();

        ImGui.SameLine();

        condition.DrawAfter(rotation);
    }

    private static void DrawDelay(this DelayCondition condition)
    {
        const float MIN = 0, MAX = 600;

        ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
        if (ImGui.DragFloatRange2($"##Random Delay {condition.GetHashCode()}", ref condition.DelayMin, ref condition.DelayMax, 0.1f, MIN, MAX,
            $"{condition.DelayMin:F1}{ConfigUnitType.Seconds.ToSymbol()}", $"{condition.DelayMax:F1}{ConfigUnitType.Seconds.ToSymbol()}"))
        {
            condition.DelayMin = Math.Max(Math.Min(condition.DelayMin, condition.DelayMax), MIN);
            condition.DelayMax = Math.Min(Math.Max(condition.DelayMin, condition.DelayMax), MAX);
        }
        ImguiTooltips.HoveredTooltip("Delay its transition to true" +
            "\n" + ConfigUnitType.Seconds.GetDescription());

        ImGui.SameLine();

        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        ImGui.DragFloat($"##Offset Delay {condition.GetHashCode()}", ref condition.DelayOffset, 0.1f, MIN, MAX,
            $"{condition.DelayOffset:F1}{ConfigUnitType.Seconds.ToSymbol()}");

        ImguiTooltips.HoveredTooltip("Delay its transition" +
    "\n" + ConfigUnitType.Seconds.GetDescription());
    }

    private static void DrawBefore(this ICondition condition)
    {
        if (condition is ConditionSet)
        {
            ImGui.BeginGroup();
        }
    }

    private static void DrawAfter(this ICondition condition, ICustomRotation rotation)
    {
        switch (condition)
        {
            case TraitCondition traitCondition:
                traitCondition.DrawAfter(rotation);
                break;

            case ActionCondition actionCondition:
                actionCondition.DrawAfter(rotation);
                break;

            case ConditionSet conditionSet:
                conditionSet.DrawAfter(rotation);
                break;

            case RotationCondition rotationCondition:
                rotationCondition.DrawAfter(rotation);
                break;

            case TargetCondition targetCondition:
                targetCondition.DrawAfter(rotation);
                break;

            case NamedCondition namedCondition:
                namedCondition.DrawAfter(rotation);
                break;

            case TerritoryCondition territoryCondition:
                territoryCondition.DrawAfter(rotation);
                break;
        }
    }

    private static void DrawAfter(this NamedCondition namedCondition, ICustomRotation _)
    {
        ImGuiHelper.SearchCombo($"##Comparation{namedCondition.GetHashCode()}", namedCondition.ConditionName, ref searchTxt,
            DataCenter.CurrentConditionValue.NamedConditions.Select(p => p.Name).ToArray(), i => i.ToString(), i =>
            {
                namedCondition.ConditionName = i;
            }, "Condition Name");

        ImGui.SameLine();
    }

    private static void DrawAfter(this TraitCondition traitCondition, ICustomRotation rotation)
    {
        var name = traitCondition._trait?.Name ?? string.Empty;
        var popUpKey = "Trait Condition Pop Up" + traitCondition.GetHashCode().ToString();

        using (var popUp = ImRaii.Popup(popUpKey))
        {
            if (popUp.Success)
            {
                var index = 0;
                foreach (var trait in rotation.AllTraits)
                {
                    if (!trait.GetTexture(out var traitIcon)) continue;

                    if (index++ % count != 0)
                    {
                        ImGui.SameLine();
                    }

                    using (var group = ImRaii.Group())
                    {
                        if (group.Success)
                        {
                            var cursor = ImGui.GetCursorPos();
                            if (ImGuiHelper.NoPaddingNoColorImageButton(traitIcon.ImGuiHandle, Vector2.One * IconSize, trait.GetHashCode().ToString()))
                            {
                                traitCondition.TraitID = trait.ID;
                                ImGui.CloseCurrentPopup();
                            }
                            ImGuiHelper.DrawActionOverlay(cursor, IconSize, -1);
                        }
                    }

                    var tooltip = trait.Name;
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        ImguiTooltips.HoveredTooltip(tooltip);
                    }
                }
            }
        }

        if (traitCondition._trait?.GetTexture(out var icon) ?? false || IconSet.GetTexture(4, out icon))
        {
            var cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * IconSize, traitCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey)) ImGui.OpenPopup(popUpKey);
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, -1);
            ImguiTooltips.HoveredTooltip(name);
        }

        ImGui.SameLine();
        var i = 0;
        ImGuiHelper.SelectableCombo($"##Category{traitCondition.GetHashCode()}",
        [
            "Sufficient Level"
        ], ref i);
        ImGui.SameLine();
    }

    private static readonly CollapsingHeaderGroup _actionsList = new CollapsingHeaderGroup(new Dictionary<Func<string>, Action>())
    {
        HeaderSize = 12,
    };

    static string searchTxt = string.Empty;

    private static void DrawAfter(this ActionCondition actionCondition, ICustomRotation rotation)
    {
        var name = actionCondition._action?.Name ?? string.Empty;
        var popUpKey = "Action Condition Pop Up" + actionCondition.GetHashCode().ToString();

        ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => actionCondition.ID = (ActionID)item.ID);

        if ((actionCondition._action?.GetTexture(out var icon) ?? false) || IconSet.GetTexture(4, out icon))
        {
            var cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * IconSize, actionCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey)) ImGui.OpenPopup(popUpKey);
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
            ImguiTooltips.HoveredTooltip(name);
        }

        ImGui.SameLine();

        DrawByteEnum($"##Category{actionCondition.GetHashCode()}", ref actionCondition.ActionConditionType);

        switch (actionCondition.ActionConditionType)
        {
            case ActionConditionType.Elapsed:
            case ActionConditionType.Remain:
                DrawDragFloat(ConfigUnitType.Seconds, $"##Seconds{actionCondition.GetHashCode()}", ref actionCondition.Time);
                break;

            case ActionConditionType.ElapsedGCD:
            case ActionConditionType.RemainGCD:
                if (DrawDragInt($"GCD##GCD{actionCondition.GetHashCode()}", ref actionCondition.Param1))
                {
                    actionCondition.Param1 = Math.Max(0, actionCondition.Param1);
                }
                if (DrawDragInt($"{"Time Offset"}##Ability{actionCondition.GetHashCode()}", ref actionCondition.Param2))
                {
                    actionCondition.Param2 = Math.Max(0, actionCondition.Param2);
                }
                break;

            case ActionConditionType.CanUse:
                var popUpId = "Can Use Id" + actionCondition.GetHashCode().ToString();
                var option = (CanUseOption)actionCondition.Param1;

                if (ImGui.Selectable($"{option}##CanUse{actionCondition.GetHashCode()}"))
                {
                    if (!ImGui.IsPopupOpen(popUpId)) ImGui.OpenPopup(popUpId);
                }

                using (var popUp = ImRaii.Popup(popUpId))
                {
                    if (popUp.Success)
                    {
                        var showedValues = Enum.GetValues<CanUseOption>().Where(i => i.GetAttribute<JsonIgnoreAttribute>() == null);

                        foreach (var value in showedValues)
                        {
                            var b = option.HasFlag(value);
                            if (ImGui.Checkbox(value.GetDescription(), ref b))
                            {
                                option ^= value;
                                actionCondition.Param1 = (int)option;
                            }
                        }
                    }
                }
                break;

            case ActionConditionType.CurrentCharges:
            case ActionConditionType.MaxCharges:
                DrawCondition(actionCondition, ref actionCondition.Param2);

                if (DrawDragInt($"{"Charges"}##Charges{actionCondition.GetHashCode()}", ref actionCondition.Param1))
                {
                    actionCondition.Param1 = Math.Max(0, actionCondition.Param1);
                }
                break;
        }
    }

    private static void DrawAfter(this ConditionSet conditionSet, ICustomRotation rotation)
    {
        AddButton();

        ImGui.SameLine();

        DrawByteEnum($"##Rule{conditionSet.GetHashCode()}", ref conditionSet.Type);

        ImGui.Spacing();

        for (int i = 0; i < conditionSet.Conditions.Count; i++)
        {
            var condition = conditionSet.Conditions[i];

            void Delete()
            {
                conditionSet.Conditions.RemoveAt(i);
            };

            void Up()
            {
                conditionSet.Conditions.RemoveAt(i);
                conditionSet.Conditions.Insert(Math.Max(0, i - 1), condition);
            };

            void Down()
            {
                conditionSet.Conditions.RemoveAt(i);
                conditionSet.Conditions.Insert(Math.Min(conditionSet.Conditions.Count, i + 1), condition);
            }

            void Copy()
            {
                var str = JsonConvert.SerializeObject(conditionSet.Conditions[i], Formatting.Indented);
                ImGui.SetClipboardText(str);
            }

            var key = $"Condition Pop Up: {condition.GetHashCode()}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                ("Remove", Delete, ["Delete"]),
                ("Move Up", Up, ["?"]),
                ("Move Down", Down, ["?"]),
                ("Copy", Copy, ["Ctrl"]));

            if (condition is DelayCondition delay)
            {
                DrawCondition(delay.IsTrue(rotation), delay.GetHashCode().ToString(), ref delay.Not);
            }
            else
            {
                DrawCondition(condition.IsTrue(rotation));
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
                (Delete, [VirtualKey.DELETE]),
                (Up, [VirtualKey.UP]),
                (Down, [VirtualKey.DOWN]),
                (Copy, [VirtualKey.CONTROL]));

            ImGui.SameLine();

            condition.Draw(rotation);
        }

        ImGui.EndGroup();

        void AddButton()
        {
            if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "AddButton" + conditionSet.GetHashCode().ToString()))
            {
                ImGui.OpenPopup("Popup" + conditionSet.GetHashCode().ToString());
            }

            using var popUp = ImRaii.Popup("Popup" + conditionSet.GetHashCode().ToString());
            if (popUp)
            {
                AddOneCondition<ConditionSet>("Condition Value");
                AddOneCondition<ActionCondition>("Action Condition");
                AddOneCondition<TraitCondition>("Trait Condition");
                AddOneCondition<TargetCondition>("Target Condition");
                AddOneCondition<RotationCondition>("Rotation Condition");
                AddOneCondition<NamedCondition>("Named Condition");
                AddOneCondition<TerritoryCondition>("Territory Condition");
                if (ImGui.Selectable("From Clipboard"))
                {
                    var str = ImGui.GetClipboardText();
                    try
                    {
                        var set = JsonConvert.DeserializeObject<ICondition>(str, new IConditionConverter())!;
                        conditionSet.Conditions.Add(set);
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Warning(ex, "Failed to load the condition.");
                    }
                    ImGui.CloseCurrentPopup();
                }
            }

            void AddOneCondition<T>(string description) where T : ICondition
            {
                if (ImGui.Selectable(description))
                {
                    conditionSet.Conditions.Add(Activator.CreateInstance<T>());
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static void DrawAfter(this RotationCondition rotationCondition, ICustomRotation rotation)
    {
        DrawByteEnum($"##Category{rotationCondition.GetHashCode()}", ref rotationCondition.ComboConditionType);

        switch (rotationCondition.ComboConditionType)
        {
            case ComboConditionType.Bool:
                ImGui.SameLine();
                SearchItemsReflection($"##Comparation{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllBools, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                break;

            case ComboConditionType.Integer:
                ImGui.SameLine();
                SearchItemsReflection($"##ByteChoice{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllBytesOrInt, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                DrawCondition(rotationCondition, ref rotationCondition.Condition);

                DrawDragInt($"##Value{rotationCondition.GetHashCode()}", ref rotationCondition.Param1);

                break;

            case ComboConditionType.Float:
                ImGui.SameLine();
                SearchItemsReflection($"##FloatChoice{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllFloats, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                DrawCondition(rotationCondition, ref rotationCondition.Condition);

                DrawDragFloat(ConfigUnitType.None, $"##Value{rotationCondition.GetHashCode()}", ref rotationCondition.Param2);
                break;

            case ComboConditionType.Last:
                ImGui.SameLine();

                var names = new string[]
                    {
                        nameof(CustomRotation.IsLastGCD),
                        nameof(CustomRotation.IsLastAction),
                        nameof(CustomRotation.IsLastAbility),
                    };
                var index = Math.Max(0, Array.IndexOf(names, rotationCondition.MethodName));
                if (ImGuiHelper.SelectableCombo($"##Last{rotationCondition.GetHashCode()}", names, ref index))
                {
                    rotationCondition.MethodName = names[index];
                }

                ImGui.SameLine();

                var name = rotationCondition._action?.Name ?? string.Empty;

                var popUpKey = "Rotation Condition Pop Up" + rotationCondition.GetHashCode().ToString();

                ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => rotationCondition.ID = (ActionID)item.ID);

                if (rotationCondition._action?.GetTexture(out var icon) ?? false || IconSet.GetTexture(4, out icon))
                {
                    var cursor = ImGui.GetCursorPos();
                    if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * IconSize, rotationCondition.GetHashCode().ToString()))
                    {
                        if (!ImGui.IsPopupOpen(popUpKey)) ImGui.OpenPopup(popUpKey);
                    }
                    ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
                }

                ImGui.SameLine();
                ImGuiHelper.SelectableCombo($"##Adjust{rotationCondition.GetHashCode()}",
                [
                    "Original",
                    "Adjusted",
                ], ref rotationCondition.Param1);
                break;
        }
    }

    private static Status[]? _allStatus = null;
    private static Status[] AllStatus => _allStatus ??= Enum.GetValues<StatusID>()
        .Select(id => Service.GetSheet<Status>().GetRow((uint)id)).ToArray()!;

    private static void DrawAfter(this TargetCondition targetCondition, ICustomRotation rotation)
    {
        DelayCondition.CheckBaseAction(rotation, targetCondition.ID, ref targetCondition._action);

        if (targetCondition.StatusId != StatusID.None &&
            (targetCondition.Status == null || targetCondition.Status.Value.RowId != (uint)targetCondition.StatusId))
        {
            targetCondition.Status = AllStatus.FirstOrDefault(a => a.RowId == (uint)targetCondition.StatusId);
        }

        var popUpKey = "Target Condition Pop Up" + targetCondition.GetHashCode().ToString();

        ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => targetCondition.ID = (ActionID)item.ID, () =>
        {
            if (ImGui.Selectable(TargetType.HostileTarget.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.HostileTarget;
            }

            if (ImGui.Selectable(TargetType.Target.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.Target;
            }

            if (ImGui.Selectable(TargetType.Player.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.Player;
            }
        });

        if (targetCondition._action != null ? targetCondition._action.GetTexture(out var icon) || IconSet.GetTexture(4, out icon)
            : IconSet.GetTexture(targetCondition.TargetType switch
            {
                TargetType.Target => 16u,
                TargetType.HostileTarget => 15u,
                TargetType.Player => 18u,
                _ => 0,
            }, out icon))
        {
            var cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * IconSize, targetCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey)) ImGui.OpenPopup(popUpKey);
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);

            var description = targetCondition._action != null ? string.Format("{0}'s target", targetCondition._action.Name)
                : targetCondition.TargetType.GetDescription();
            ImguiTooltips.HoveredTooltip(description);
        }

        ImGui.SameLine();
        DrawByteEnum($"##Category{targetCondition.GetHashCode()}", ref targetCondition.TargetConditionType);

        var popupId = "Status Finding Popup" + targetCondition.GetHashCode().ToString();

        RotationConfigWindow.StatusPopUp(popupId, AllStatus, ref searchTxt, status =>
        {
            targetCondition.Status = status;
            targetCondition.StatusId = (StatusID)targetCondition.Status.Value.RowId;
        }, size: IconSizeRaw);

        void DrawStatusIcon()
        {
            if (IconSet.GetTexture(targetCondition.Status?.Icon ?? 16220, out var icon)
                || IconSet.GetTexture(16220, out icon))
            {
                if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, new Vector2(IconSize * 3 / 4, IconSize) * ImGuiHelpers.GlobalScale, targetCondition.GetHashCode().ToString()))
                {
                    if (!ImGui.IsPopupOpen(popupId)) ImGui.OpenPopup(popupId);
                }
                ImguiTooltips.HoveredTooltip(targetCondition.Status?.Name.ExtractText() ?? string.Empty);
            }
        }

        switch (targetCondition.TargetConditionType)
        {
            case TargetConditionType.HasStatus:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                var check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    "From All",
                    "From Self",
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }
                break;

            case TargetConditionType.StatusEnd:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    "From All",
                    "From Self",
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }

                DrawCondition(targetCondition, ref targetCondition.Param2);

                DrawDragFloat(ConfigUnitType.Seconds, $"s##Seconds{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;


            case TargetConditionType.StatusEndGCD:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    "From All",
                    "From Self",
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }

                DrawDragInt($"GCD##GCD{targetCondition.GetHashCode()}", ref targetCondition.GCD);
                DrawDragFloat(ConfigUnitType.Seconds, $"{"Time Offset"}##Ability{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.Distance:
                DrawCondition(targetCondition, ref targetCondition.Param2);

                if (DrawDragFloat(ConfigUnitType.Yalms, $"##yalm{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime))
                {
                    targetCondition.DistanceOrTime = Math.Max(0, targetCondition.DistanceOrTime);
                }
                break;

            case TargetConditionType.CastingAction:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CastingActionName);
                ImGui.InputText($"Ability Name##CastingActionName{targetCondition.GetHashCode()}", ref targetCondition.CastingActionName, 128);
                break;

            case TargetConditionType.CastingActionTime:
                DrawCondition(targetCondition, ref targetCondition.Param2);
                DrawDragFloat(ConfigUnitType.Seconds, $"##CastingActionTimeUntil{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.HPRatio:
                DrawCondition(targetCondition, ref targetCondition.Param2);

                DrawDragFloat(ConfigUnitType.Percent, $"##HPRatio{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.MP:
            case TargetConditionType.HP:
                DrawCondition(targetCondition, ref targetCondition.Param2);

                DrawDragInt($"##HPorMP{targetCondition.GetHashCode()}", ref targetCondition.GCD);
                break;

            case TargetConditionType.TimeToKill:
                DrawCondition(targetCondition, ref targetCondition.Param2);

                DrawDragFloat(ConfigUnitType.Seconds, $"##TimeToKill{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.TargetName:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CastingActionName);
                ImGui.InputText($"Name##TargetName{targetCondition.GetHashCode()}", ref targetCondition.CastingActionName, 128);
                break;
            case TargetConditionType.TargetRole:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CombatRole.ToString());
                ImGui.InputText($"Name##TargetRole{targetCondition.GetHashCode()}", ref targetCondition.CombatRole, 128);
                break;
        }

        if (targetCondition._action == null && targetCondition.TargetType == TargetType.Target)
        {
            using var style = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped("You should not use this, as this target isn't the action's target. Try selecting it from the action instead.");
        }
    }

    private static string[]? _territoryNames = null;
    public static string[] TerritoryNames => _territoryNames ??= Service.GetSheet<TerritoryType>()?
        .Select(t => t.PlaceName.Value.Name.ExtractText() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToArray()!;

    private static string[]? _dutyNames = null;

    public static string[] DutyNames => _dutyNames ??= new HashSet<string>(Service.GetSheet<ContentFinderCondition>()?
        .Select(t => t.Name.ExtractText() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Reverse()!).ToArray();

    private static void DrawAfter(this TerritoryCondition territoryCondition, ICustomRotation _)
    {
        DrawByteEnum($"##Category{territoryCondition.GetHashCode()}", ref territoryCondition.TerritoryConditionType);

        switch (territoryCondition.TerritoryConditionType)
        {
            case TerritoryConditionType.TerritoryContentType:
                ImGui.SameLine();

                var type = (TerritoryContentType)territoryCondition.TerritoryId;
                DrawByteEnum($"##TerritoryContentType{territoryCondition.GetHashCode()}", ref type);
                territoryCondition.TerritoryId = (int)type;
                break;

            case TerritoryConditionType.TerritoryName:
                ImGui.SameLine();

                ImGuiHelper.SearchCombo($"##TerritoryName{territoryCondition.GetHashCode()}", territoryCondition.Name, ref searchTxt,
                TerritoryNames, i => i.ToString(), i =>
                {
                    territoryCondition.Name = i;
                }, "Territory Name");
                break;

            case TerritoryConditionType.DutyName:
                ImGui.SameLine();

                ImGuiHelper.SearchCombo($"##DutyName{territoryCondition.GetHashCode()}", territoryCondition.Name, ref searchTxt,
                DutyNames, i => i.ToString(), i =>
                {
                    territoryCondition.Name = i;
                }, "Duty Name");
                break;
        }
    }
    #endregion
}

