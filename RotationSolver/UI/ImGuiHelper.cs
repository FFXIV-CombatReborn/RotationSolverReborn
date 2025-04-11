using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.LanguageHelpers;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;

namespace RotationSolver.UI;

internal static class ImGuiHelper
{
    internal static void SetNextWidthWithName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        ImGui.SetNextItemWidth(Math.Max(80 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(name).X + 30 * ImGuiHelpers.GlobalScale));
    }

    const float INDENT_WIDTH = 180;

    internal static void DisplayCommandHelp(this Enum command, string extraCommand = "", Func<Enum, string>? getHelp = null, bool sameLine = true)
    {
        var cmdStr = command.GetCommandStr(extraCommand);

        if (ImGui.Button(cmdStr))
        {
            Svc.Commands.ProcessCommand(cmdStr);
        }
        if (ImGui.IsItemHovered())
        {
            ImguiTooltips.ShowTooltip($"{"Click to execute the command"}: {cmdStr}\n{"Right-click to copy the command"}: {cmdStr}");

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(cmdStr);
            }
        }

        var help = getHelp?.Invoke(command);

        if (!string.IsNullOrEmpty(help))
        {
            if (sameLine)
            {
                ImGui.SameLine();
                ImGui.Indent(INDENT_WIDTH);
            }
            ImGui.Text(" → ");
            ImGui.SameLine();
            ImGui.TextWrapped(help);
            if (sameLine)
            {
                ImGui.Unindent(INDENT_WIDTH);
            }
        }
    }

    public static void DisplayMacro(this MacroInfo info)
    {
        // Set the width for the next item
        ImGui.SetNextItemWidth(50);

        // Display a draggable integer input for the macro index
        if (ImGui.DragInt($"{"Macro No."}##MacroIndex{info.GetHashCode()}", ref info.MacroIndex, 1, -1, 99))
        {
            Service.Config.Save();
        }

        // Display a checkbox for the shared macro option
        ImGui.SameLine();
        if (ImGui.Checkbox($"{"Is Shared"}##ShareMacro{info.GetHashCode()}", ref info.IsShared))
        {
            Service.Config.Save();
        }
    }

    public static void DisplayEvent(this ActionEventInfo info)
    {
        var name = info.Name;
        if (ImGui.InputText($"{"Action Name"}##ActionName{info.GetHashCode()}", ref name, 100))
        {
            info.Name = name;
            Service.Config.Save();
        }

        info.DisplayMacro();
    }

    public static void SearchCombo<T>(string popId, string name, ref string searchTxt, T[] items, Func<T, string> getSearchName, Action<T> selectAction, string searchingHint, ImFontPtr? font = null, Vector4? color = null)
    {
        if (SelectableButton(name + "##" + popId, font, color))
        {
            if (!ImGui.IsPopupOpen(popId)) ImGui.OpenPopup(popId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        using var popUp = ImRaii.Popup(popId);
        if (!popUp.Success) return;

        if (items == null || items.Length == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "ConfigWindow_Condition_NoItemsWarning".Loc("There are no items!"));
            return;
        }

        var searchingKey = searchTxt;

        var members = new List<(T, string)>();
        foreach (var item in items)
        {
            members.Add((item, getSearchName(item)));
        }

        members.Sort((x, y) => SearchableCollection.Similarity(y.Item2, searchingKey).CompareTo(SearchableCollection.Similarity(x.Item2, searchingKey)));

        ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, GetMaxButtonSize(members)));
        ImGui.InputTextWithHint("##Searching the member", searchingHint, ref searchTxt, 128);

        ImGui.Spacing();

        ImRaii.IEndObject? child = null;
        if (members.Count >= 15)
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(0, 300), new Vector2(500, 300));
            child = ImRaii.Child(popId);
            if (!child) return;
        }

        foreach (var member in members)
        {
            if (ImGui.Selectable(member.Item2))
            {
                selectAction?.Invoke(member.Item1);
                ImGui.CloseCurrentPopup();
            }
        }
        child?.Dispose();
    }

    private static float GetMaxButtonSize<T>(List<(T, string)> members)
    {
        float maxSize = 0;
        foreach (var member in members)
        {
            var size = ImGuiHelpers.GetButtonSize(member.Item2).X;
            if (size > maxSize)
            {
                maxSize = size;
            }
        }
        return maxSize;
    }

    public static unsafe bool SelectableCombo(string popUp, string[] items, ref int index, ImFontPtr? font = null, Vector4? color = null)
    {
        var count = items.Length;
        var originIndex = index;
        index = Math.Max(0, index) % count;
        var name = items[index] + "##" + popUp;

        var result = originIndex != index;

        if (SelectableButton(name, font, color))
        {
            if (count < 3)
            {
                index = (index + 1) % count;
                result = true;
            }
            else
            {
                if (!ImGui.IsPopupOpen(popUp)) ImGui.OpenPopup(popUp);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, Vector2.One * 500);
        if (ImGui.BeginPopup(popUp))
        {
            for (int i = 0; i < count; i++)
            {
                if (ImGui.Selectable(items[i]))
                {
                    index = i;
                    result = true;
                }
            }
            ImGui.EndPopup();
        }

        return result;
    }

    public static unsafe bool SelectableButton(string name, ImFontPtr? font = null, Vector4? color = null)
    {
        List<IDisposable> disposables = new(2);
        if (font != null)
        {
            disposables.Add(ImRaii.PushFont(font.Value));
        }
        if (color != null)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Text, color.Value));
        }
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        var result = ImGui.Button(name);
        ImGui.PopStyleColor(3);
        foreach (var item in disposables)
        {
            item.Dispose();
        }

        return result;
    }

    internal static void DrawItemMiddle(Action drawAction, float wholeWidth, float width, bool leftAlign = true)
    {
        if (drawAction == null) return;
        var distance = (wholeWidth - width) / 2;
        if (leftAlign) distance = MathF.Max(distance, 0);
        ImGui.SetCursorPosX(distance);
        drawAction();
    }

    #region Image
    internal unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, bool selected, string id = "")
    => SilenceImageButton(handle, size, Vector2.Zero, Vector2.One, selected, id);

    internal unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, bool selected, string id = "")
    {
        uint buttonColor = selected ? ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.Header)) : 0;
        return SilenceImageButton(handle, size, uv0, uv1, buttonColor, id);
    }

    internal unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, uint buttonColor, string id = "")
    {
        const int StyleColorCount = 3;

        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

        var buttonClicked = NoPaddingImageButton(handle, size, uv0, uv1, id);
        ImGui.PopStyleColor(StyleColorCount);

        return buttonClicked;
    }

    internal unsafe static bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, string id = "")
        => NoPaddingNoColorImageButton(handle, size, Vector2.Zero, Vector2.One, id);

    internal unsafe static bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
    {
        const int StyleColorCount = 3;

        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        var buttonClicked = NoPaddingImageButton(handle, size, uv0, uv1, id);
        ImGui.PopStyleColor(StyleColorCount);

        return buttonClicked;
    }

    internal static bool NoPaddingImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
    {
        var style = ImGui.GetStyle();
        var originalPadding = style.FramePadding;
        style.FramePadding = Vector2.Zero;

        ImGui.PushID(id);
        var buttonClicked = ImGui.ImageButton(handle, size, uv0, uv1);
        ImGui.PopID();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        style.FramePadding = originalPadding;
        return buttonClicked;
    }

    internal static bool TextureButton(IDalamudTextureWrap texture, float wholeWidth, float maxWidth, string id = "")
    {
        if (texture == null) return false;

        var size = new Vector2(texture.Width, texture.Height) * MathF.Min(1, MathF.Min(maxWidth, wholeWidth) / texture.Width);

        var buttonClicked = false;
        DrawItemMiddle(() =>
        {
            buttonClicked = NoPaddingNoColorImageButton(texture.ImGuiHandle, size, id);
        }, wholeWidth, size.X);
        return buttonClicked;
    }

    internal static readonly uint ProgressCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 0.7f));
    internal static readonly uint Black = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1));
    internal static readonly uint White = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));

    internal static void TextShade(Vector2 pos, string text, float width = 1.5f)
    {
        var offsets = new Vector2[]
        {
            new Vector2(0, -width),
            new Vector2(0, width),
            new Vector2(-width, 0),
            new Vector2(width, 0)
        };

        var drawList = ImGui.GetWindowDrawList();
        foreach (var offset in offsets)
        {
            drawList.AddText(pos + offset, Black, text);
        }
        drawList.AddText(pos, White, text);
    }

    internal static void DrawActionOverlay(Vector2 cursor, float width, float percent)
    {
        var pixPerUnit = width / 82;

        if (percent < 0)
        {
            if (IconSet.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));

                var start = new Vector2((96f * 0 + 4f) / cover.Width, (96f * 2) / cover.Height);

                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else if (percent < 1)
        {
            if (IconSet.GetTexture("ui/uld/icona_recast_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)(percent * 81);

                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2(P % 9 * step.X, P / 9 * step.Y);

                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else
        {
            if (IconSet.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));

                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    new Vector2(4f / cover.Width, 0f / cover.Height),
                    new Vector2(92f / cover.Width, 94f / cover.Height));
            }
        }

        if (percent > 1)
        {
            if (IconSet.GetTexture("ui/uld/icona_recast2_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)(percent % 1 * 81);

                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2((P % 9 + 9) * step.X, P / 9 * step.Y);

                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
    }
    #endregion

    #region PopUp
    public static void DrawHotKeysPopup(string key, string command, params (string name, Action action, string[] keys)[] pairs)
    {
        using var popup = ImRaii.Popup(key);
        if (popup)
        {
            if (ImGui.BeginTable(key, 2, ImGuiTableFlags.BordersOuter))
            {
                if (pairs != null)
                {
                    foreach (var (name, action, keys) in pairs)
                    {
                        if (action == null) continue;
                        DrawHotKeys(name, action, keys);
                    }
                }
                if (!string.IsNullOrEmpty(command))
                {
                    DrawHotKeys($"Execute \"{command}\"", () => ExecuteCommand(command), "Alt");
                    DrawHotKeys($"Copy \"{command}\"", () => CopyCommand(command), "Ctrl");
                }
                ImGui.EndTable();
            }
        }
    }

    public static void PrepareGroup(string key, string command, Action reset)
    {
        if (reset == null) throw new ArgumentNullException(nameof(reset));
        DrawHotKeysPopup(key, command, ("Reset to Default Value.", reset, new string[] { "Backspace" }));
    }

    public static void ReactPopup(string key, string command, Action reset, bool showHand = true)
    {
        if (reset == null) throw new ArgumentNullException(nameof(reset));
        ExecuteHotKeysPopup(key, command, string.Empty, showHand, (reset, new VirtualKey[] { VirtualKey.BACK }));
    }

    public static void ExecuteHotKeysPopup(string key, string command, string tooltip, bool showHand, params (Action action, VirtualKey[] keys)[] pairs)
    {
        if (!ImGui.IsItemHovered()) return;
        if (!string.IsNullOrEmpty(tooltip)) ImguiTooltips.ShowTooltip(tooltip);

        if (showHand) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            if (!ImGui.IsPopupOpen(key))
            {
                ImGui.OpenPopup(key);
            }
        }

        if (pairs != null)
        {
            foreach (var (action, keys) in pairs)
            {
                if (action == null) continue;
                ExecuteHotKeys(action, keys);
            }
        }
        if (!string.IsNullOrEmpty(command))
        {
            ExecuteHotKeys(() => ExecuteCommand(command), VirtualKey.MENU);
            ExecuteHotKeys(() => CopyCommand(command), VirtualKey.CONTROL);
        }
    }

    private static void ExecuteCommand(string command)
    {
        Svc.Commands.ProcessCommand(command);
    }

    private static void CopyCommand(string command)
    {
        ImGui.SetClipboardText(command);
        Notify.Success($"\"{command}\" copied to clipboard.");
    }

    private static readonly SortedList<string, bool> _lastChecked = [];
    private static void ExecuteHotKeys(Action action, params VirtualKey[] keys)
    {
        if (action == null) return;
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        var name = string.Join(' ', keys);

        if (!_lastChecked.TryGetValue(name, out var last)) last = false;
        var now = keys.All(k => Svc.KeyState[k]);
        _lastChecked[name] = now;

        if (!last && now) action();
    }

    private static void DrawHotKeys(string name, Action action, params string[] keys)
    {
        if (action == null) return;
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (ImGui.Selectable(name))
        {
            action();
            ImGui.CloseCurrentPopup();
        }

        ImGui.TableNextColumn();
        ImGui.TextDisabled(string.Join(' ', keys));
    }

    #endregion

    public static bool IsInRect(Vector2 leftTop, Vector2 size)
    {
        var pos = ImGui.GetMousePos() - leftTop;
        return pos.X > 0 && pos.Y > 0 && pos.X < size.X && pos.Y < size.Y;
    }

    public static string ToSymbol(this ConfigUnitType unit) => unit switch
    {
        ConfigUnitType.Seconds => " s",
        ConfigUnitType.Degree => " °",
        ConfigUnitType.Pixels => " p",
        ConfigUnitType.Yalms => " y",
        ConfigUnitType.Percent => " %%",
        _ => string.Empty,
    };

    public static void Draw(this CombatType type)
    {
        bool first = true;
        if (type.HasFlag(CombatType.PvE))
        {
            if (!first) ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudYellow, " PvE");
            first = false;
        }
        if (type.HasFlag(CombatType.PvP))
        {
            if (!first) ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.TankBlue, " PvP");
            first = false;
        }
        if (type == CombatType.None)
        {
            if (!first) ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudRed, " None of PvE or PvP!");
        }
    }
}
