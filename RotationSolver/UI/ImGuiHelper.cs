using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;

namespace RotationSolver.UI;

internal static class ImGuiHelper
{
	private const float INDENT_WIDTH = 180;

	internal static readonly string[] DeleteHint = ["Delete"];
	internal static readonly string[] MoveUpHint = ["↑"];
	internal static readonly string[] MoveDownHint = ["↓"];

	internal static ImRaii.StyleDisposable PushOverlayStyle()
	{
		const float rounding = 11f;
		return ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f))
			.Push(ImGuiStyleVar.FramePadding, new Vector2(4, 3))
			.Push(ImGuiStyleVar.WindowPadding, new Vector2(12, 12))
			.Push(ImGuiStyleVar.CellPadding, new Vector2(4, 2))
			.Push(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4))
			.Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4))
			.Push(ImGuiStyleVar.IndentSpacing, 21f)
			.Push(ImGuiStyleVar.ScrollbarSize, 16f)
			.Push(ImGuiStyleVar.GrabMinSize, 13f)
			.Push(ImGuiStyleVar.WindowRounding, rounding)
			.Push(ImGuiStyleVar.ChildRounding, rounding)
			.Push(ImGuiStyleVar.FrameRounding, rounding)
			.Push(ImGuiStyleVar.PopupRounding, rounding)
			.Push(ImGuiStyleVar.ScrollbarRounding, rounding)
			.Push(ImGuiStyleVar.GrabRounding, rounding)
			.Push(ImGuiStyleVar.TabRounding, rounding);
	}

	internal static void DisplayCommandHelp(this Enum command, string extraCommand = "", Func<Enum, string>? getHelp = null, bool sameLine = true)
	{
		var cmdStr = command.GetCommandStr(extraCommand);

		if (ImGui.Button(cmdStr))
		{
			_ = Svc.Commands.ProcessCommand(cmdStr);
		}
		if (ImGui.IsItemHovered())
		{
			ImguiTooltips.ShowTooltip($"{UiString.ConfigWindow_Helper_RunCommand.GetDescription()}: {cmdStr}\n{UiString.ConfigWindow_Helper_CopyCommand.GetDescription()}: {cmdStr}");

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
		ImGui.SetNextItemWidth(50);

		if (ImGui.DragInt($"{UiString.ConfigWindow_Events_MacroIndex.GetDescription()}##MacroIndex{info.GetHashCode()}", ref info.MacroIndex, 1, -1, 99))
		{
			Service.Config.Save();
		}

		ImGui.SameLine();
		if (ImGui.Checkbox($"{UiString.ConfigWindow_Events_ShareMacro.GetDescription()}##ShareMacro{info.GetHashCode()}", ref info.IsShared))
		{
			Service.Config.Save();
		}
	}

	public static void DisplayEvent(this ActionEventInfo info)
	{
		var name = info.Name;
		if (ImGui.InputText($"{UiString.ConfigWindow_Events_ActionName.GetDescription()}##ActionName{info.GetHashCode()}", ref name, 100))
		{
			info.Name = name;
			Service.Config.Save();
		}

		info.DisplayMacro();
	}

	public static bool SelectableCombo(string popUp, string[] items, ref int index, ImFontPtr? font = null, Vector4? color = null)
	{
		var count = items.Length;
		if (count == 0)
		{
			return false;
		}

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
				if (!ImGui.IsPopupOpen(popUp))
				{
					ImGui.OpenPopup(popUp);
				}
			}
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		ImGui.SetNextWindowSizeConstraints(Vector2.Zero, Vector2.One * 500);
		if (ImGui.BeginPopup(popUp))
		{
			for (var i = 0; i < count; i++)
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
		// Called for many buttons every frame, so push/pop directly instead of allocating disposables.
		if (font != null)
		{
			ImGui.PushFont(font.Value);
		}

		var colorCount = 3;
		if (color != null)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
			colorCount++;
		}

		ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
		ImGui.PushStyleColor(ImGuiCol.Button, 0);
		var result = ImGui.Button(name);
		ImGui.PopStyleColor(colorCount);

		if (font != null)
		{
			ImGui.PopFont();
		}

		return result;
	}

	#region Image
	internal static unsafe bool NoPaddingNoColorImageButton(IDalamudTextureWrap handle, Vector2 size, string id = "")
	{
		if (handle == null)
		{
			return false;
		}

		return NoPaddingNoColorImageButton(handle, size, Vector2.Zero, Vector2.One, id);
	}

	internal static bool NoPaddingNoColorImageButton(IDalamudTextureWrap handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
	{
		if (handle == null)
		{
			return false;
		}

		const int StyleColorCount = 3;

		ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
		ImGui.PushStyleColor(ImGuiCol.Button, 0);
		var buttonClicked = NoPaddingImageButton(handle, size, uv0, uv1, id);
		ImGui.PopStyleColor(StyleColorCount);

		return buttonClicked;
	}

	internal static bool NoPaddingImageButton(IDalamudTextureWrap handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
	{
		if (handle == null || id == null)
		{
			return false;
		}

		var style = ImGui.GetStyle();
		var originalPadding = style.FramePadding;
		style.FramePadding = Vector2.Zero;

		//https://xkcd.com/2347/
		ImGui.PushID(id + "literally anything");
		//https://xkcd.com/2347/

		var buttonClicked = false;
		var drawn = false;
		try
		{
			if (!handle.Handle.IsNull)
			{
				buttonClicked = ImGui.ImageButton(handle.Handle, size, uv0, uv1);
				drawn = true;
			}
		}
		catch
		{
			// The texture can be disposed between the null check and the draw.
			buttonClicked = false;
			drawn = false;
		}
		finally
		{
			ImGui.PopID();
			style.FramePadding = originalPadding;
		}

		if (drawn && ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return buttonClicked;
	}

	internal static readonly uint ProgressCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 0.7f));
	internal static readonly uint Black = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1));
	internal static readonly uint White = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));

	internal static void TextShade(Vector2 pos, string text, float width = 1.5f)
	{
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddText(pos + new Vector2(0, -width), Black, text);
		drawList.AddText(pos + new Vector2(0, width), Black, text);
		drawList.AddText(pos + new Vector2(-width, 0), Black, text);
		drawList.AddText(pos + new Vector2(width, 0), Black, text);
		drawList.AddText(pos, White, text);
	}

	// Resolve overlay cover textures per draw to avoid using disposed wraps.
	// Do not cache IDalamudTextureWrap instances long-term; the provider may dispose them.

	internal static void DrawActionOverlay(Vector2 cursor, float width, float percent)
	{
		var pixPerUnit = width / 82f;

		_ = IconSet.GetTexture("ui/uld/icona_frame_hr1.tex", out var coverFrame);
		_ = IconSet.GetTexture("ui/uld/icona_recast_hr1.tex", out var coverRecast);
		_ = IconSet.GetTexture("ui/uld/icona_recast2_hr1.tex", out var coverRecast2);

		try
		{
			if (percent < 0f)
			{
				if (coverFrame?.Handle != null)
				{
					ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));
					Vector2 start = new(4f / coverFrame.Width, 96f * 2 / coverFrame.Height);
					ImGui.Image(coverFrame.Handle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
						start, start + new Vector2(88f / coverFrame.Width, 94f / coverFrame.Height));
				}
				return;
			}

			if (percent < 1f)
			{
				if (coverRecast?.Handle != null)
				{
					ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, 0));
					var P = (int)(percent * 81f);
					Vector2 step = new(88f / coverRecast.Width, 96f / coverRecast.Height);
					Vector2 start = new((P % 9) * step.X, (P / 9) * step.Y);
					ImGui.Image(coverRecast.Handle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
						start, start + new Vector2(88f / coverRecast.Width, 94f / coverRecast.Height));
				}
			}
			else
			{
				if (coverFrame?.Handle != null)
				{
					ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));
					ImGui.Image(coverFrame.Handle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
						new Vector2(4f / coverFrame.Width, 0f / coverFrame.Height),
						new Vector2(92f / coverFrame.Width, 94f / coverFrame.Height));
				}
			}

			if (percent > 1f && coverRecast2?.Handle != null)
			{
				ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, 0));
				var P = (int)(percent % 1f * 81f);
				Vector2 step = new(88f / coverRecast2.Width, 96f / coverRecast2.Height);
				Vector2 start = new(((P % 9) + 9) * step.X, (P / 9) * step.Y);
				ImGui.Image(coverRecast2.Handle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
					start, start + new Vector2(88f / coverRecast2.Width, 94f / coverRecast2.Height));
			}
		}
		catch
		{
			// The texture can be disposed between fetch and draw; skip this frame.
		}
	}
	#endregion

	#region PopUp
	public static void DrawHotKeysPopup(string key, string command, params (string name, Action action, string[] keys)[] pairs)
	{
		using var popup = ImRaii.Popup(key);
		if (popup.Success)
		{
			var showKeys = false;
			if (pairs != null)
			{
				foreach ((_, var action, var keys) in pairs)
				{
					if (action != null && keys is { Length: > 0 })
					{
						showKeys = true;
						break;
					}
				}
			}

			if (ImGui.BeginTable(key, showKeys ? 2 : 1, ImGuiTableFlags.BordersOuter))
			{
				if (pairs != null)
				{
					foreach ((var name, var action, var keys) in pairs)
					{
						if (action == null)
						{
							continue;
						}

						DrawHotKeys(name, action, showKeys, keys);
					}
				}
				if (!string.IsNullOrEmpty(command))
				{
					DrawHotKeys($"Execute \"{command}\"", () => ExecuteCommand(command), showKeys);
					DrawHotKeys($"Copy \"{command}\"", () => CopyCommand(command), showKeys);
				}
				ImGui.EndTable();
			}
		}
	}

	public static void PrepareGroup(string key, string command, Action reset)
	{
		ArgumentNullException.ThrowIfNull(reset);

		DrawHotKeysPopup(key, command, ("Reset to Default Value.", reset, []));
	}

	public static void ReactPopup(string key, bool showHand = true)
	{
		ExecuteHotKeysPopup(key, string.Empty, showHand);
	}

	// For custom-drawn rows where the hovered area isn't the last ImGui item.
	public static void ReactPopupAt(bool hovered, string key, bool showHand = true)
	{
		ExecuteHotKeysPopupAt(hovered, key, string.Empty, showHand);
	}

	public static void ExecuteHotKeysPopup(string key, string tooltip, bool showHand, params (Action action, VirtualKey[] keys)[] pairs)
	{
		ExecuteHotKeysPopupAt(ImGui.IsItemHovered(), key, tooltip, showHand, pairs);
	}

	public static void ExecuteHotKeysPopupAt(bool hovered, string key, string tooltip, bool showHand, params (Action action, VirtualKey[] keys)[] pairs)
	{
		if (!hovered)
		{
			return;
		}

		if (!string.IsNullOrEmpty(tooltip))
		{
			ImguiTooltips.ShowTooltip(tooltip);
		}

		if (showHand)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			if (!ImGui.IsPopupOpen(key))
			{
				ImGui.OpenPopup(key);
			}
		}

		if (pairs != null)
		{
			foreach ((var action, var keys) in pairs)
			{
				if (action == null)
				{
					continue;
				}

				ExecuteHotKeys(action, keys);
			}
		}
	}

	private static void ExecuteCommand(string command)
	{
		_ = Svc.Commands.ProcessCommand(command);
	}

	private static void CopyCommand(string command)
	{
		ImGui.SetClipboardText(command);
		Notify.Success($"\"{command}\" copied to clipboard.");
	}

	private static readonly SortedList<string, bool> _lastChecked = [];

	private static void ExecuteHotKeys(Action action, params VirtualKey[] keys)
	{
		if (action == null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(keys);

		var name = string.Join(' ', keys);

		if (!_lastChecked.TryGetValue(name, out var last))
		{
			last = false;
		}

		var now = true;
		foreach (var k in keys)
		{
			if (!Svc.KeyState[k])
			{
				now = false;
				break;
			}
		}
		_lastChecked[name] = now;

		if (!last && now)
		{
			action();
		}
	}

	private static void DrawHotKeys(string name, Action action, bool showKeys, params string[] keys)
	{
		if (action == null)
		{
			return;
		}

		ArgumentNullException.ThrowIfNull(keys);

		ImGui.TableNextRow();
		_ = ImGui.TableNextColumn();
		if (ImGui.Selectable(name))
		{
			action();
			ImGui.CloseCurrentPopup();
		}

		if (showKeys)
		{
			_ = ImGui.TableNextColumn();
			ImGui.TextDisabled(string.Join(' ', keys));
		}
	}

	#endregion

	public static string ToSymbol(this ConfigUnitType unit)
	{
		return unit switch
		{
			ConfigUnitType.Seconds => " s",
			ConfigUnitType.Degree => " °",
			ConfigUnitType.Pixels => " p",
			ConfigUnitType.Yalms => " y",
			ConfigUnitType.Percent => " %",
			_ => string.Empty,
		};
	}

	public static void Draw(this CombatType type)
	{
		if (type == CombatType.None)
		{
			ImGui.TextColored(ImGuiColors.DalamudRed, " None of PvE or PvP!");
			return;
		}

		var first = true;
		if (type.HasFlag(CombatType.PvE))
		{
			ImGui.TextColored(ImGuiColors.DalamudYellow, " PvE");
			first = false;
		}

		if (type.HasFlag(CombatType.PvP))
		{
			if (!first)
			{
				ImGui.SameLine();
			}

			ImGui.TextColored(ImGuiColors.TankBlue, " PvP");
		}
	}
}