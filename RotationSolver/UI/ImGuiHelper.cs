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
using RotationSolver.Data;

namespace RotationSolver.UI;

internal static class ImGuiHelper
{
	internal static void SetNextWidthWithName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}

		ImGui.SetNextItemWidth(Math.Max(80 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(name).X + (30 * ImGuiHelpers.GlobalScale)));
	}

	private const float INDENT_WIDTH = 180;

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
		// Set the width for the next item
		ImGui.SetNextItemWidth(50);

		// Display a draggable integer input for the macro index
		if (ImGui.DragInt($"{UiString.ConfigWindow_Events_MacroIndex.GetDescription()}##MacroIndex{info.GetHashCode()}", ref info.MacroIndex, 1, -1, 99))
		{
			Service.Config.Save();
		}

		// Display a checkbox for the shared macro option
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

	public static void SearchCombo<T>(string popId, string name, ref string searchTxt, T[] items, Func<T, string> getSearchName, Action<T> selectAction, string searchingHint, ImFontPtr? font = null, Vector4? color = null)
	{
		if (SelectableButton(name + "##" + popId, font, color))
		{
			if (!ImGui.IsPopupOpen(popId))
			{
				ImGui.OpenPopup(popId);
			}
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		using var popUp = ImRaii.Popup(popId);
		if (!popUp.Success)
		{
			return;
		}

		if (items == null || items.Length == 0)
		{
			ImGui.TextColored(ImGuiColors.DalamudRed, "ConfigWindow_Condition_NoItemsWarning".Loc("There are no items!"));
			return;
		}

		var searchingKey = searchTxt;

		// Score each item once; scoring inside the comparison re-split both strings on every compare.
		List<(T Item, string Name, float Score)> scored = new(items.Length);
		foreach (var item in items)
		{
			var searchName = getSearchName(item);
			scored.Add((item, searchName, SearchableCollection.Similarity(searchName, searchingKey)));
		}

		scored.Sort((x, y) => y.Score.CompareTo(x.Score));

		List<(T, string)> members = new(scored.Count);
		foreach (var (Item, Name, Score) in scored)
		{
			members.Add((Item, Name));
		}

		ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, GetMaxButtonSize(members)));
		_ = ImGui.InputTextWithHint("##Searching the member", searchingHint, ref searchTxt, 128);

		ImGui.Spacing();

		if (members.Count >= 15)
		{
			ImGui.SetNextWindowSizeConstraints(new Vector2(0, 300), new Vector2(500, 300));
			using var child = ImRaii.Child(popId);
			if (!child.Success)
			{
				return;
			}

			foreach (var member in members)
			{
				if (ImGui.Selectable(member.Item2))
				{
					selectAction?.Invoke(member.Item1);
					ImGui.CloseCurrentPopup();
				}
			}
		}
		else
		{
			foreach (var member in members)
			{
				if (ImGui.Selectable(member.Item2))
				{
					selectAction?.Invoke(member.Item1);
					ImGui.CloseCurrentPopup();
				}
			}
		}
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

	internal static void DrawItemMiddle(Action drawAction, float wholeWidth, float width, bool leftAlign = true)
	{
		if (drawAction == null)
		{
			return;
		}

		var distance = (wholeWidth - width) / 2;
		if (leftAlign)
		{
			distance = MathF.Max(distance, 0);
		}

		ImGui.SetCursorPosX(distance);
		drawAction();
	}

	#region Image
	internal static unsafe bool SilenceImageButton(IDalamudTextureWrap handle, Vector2 size, bool selected, string id = "")
	{
		if (handle == null)
		{
			return false;
		}

		return SilenceImageButton(handle, size, Vector2.Zero, Vector2.One, selected, id);
	}

	internal static unsafe bool SilenceImageButton(IDalamudTextureWrap handle, Vector2 size, Vector2 uv0, Vector2 uv1, bool selected, string id = "")
	{
		if (handle == null)
		{
			return false;
		}

		var buttonColor = selected ? ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.Header)) : 0;
		return SilenceImageButton(handle, size, uv0, uv1, buttonColor, id);
	}

	internal static unsafe bool SilenceImageButton(IDalamudTextureWrap handle, Vector2 size, Vector2 uv0, Vector2 uv1, uint buttonColor, string id = "")
	{
		if (handle == null)
		{
			return false;
		}

		const int StyleColorCount = 3;

		ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
		ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

		var buttonClicked = NoPaddingImageButton(handle, size, uv0, uv1, id);
		ImGui.PopStyleColor(StyleColorCount);

		return buttonClicked;
	}

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
		catch (ObjectDisposedException)
		{
			buttonClicked = false;
			drawn = false;
		}
		catch
		{
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

	internal static bool TextureButton(IDalamudTextureWrap texture, float wholeWidth, float maxWidth, string id = "")
	{
		if (texture == null)
		{
			return false;
		}

		var size = new Vector2(texture.Width, texture.Height) * MathF.Min(1, MathF.Min(maxWidth, wholeWidth) / texture.Width);

		var buttonClicked = false;
		DrawItemMiddle(() =>
		{
			if (texture?.Handle != null)
			{
				buttonClicked = NoPaddingNoColorImageButton(texture, size, id);
			}
		}, wholeWidth, size.X);
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
		catch (ObjectDisposedException)
		{
			// A texture was disposed between fetch and draw; skip this frame to avoid propagating.
		}
		catch
		{
			// Defensive: avoid bubbling up draw exceptions from overlay.
		}
	}
	#endregion

	#region PopUp
	public static void DrawHotKeysPopup(string key, string command, params (string name, Action action, string[] keys)[] pairs)
	{
		using var popup = ImRaii.Popup(key);
		if (popup.Success)
		{
			if (ImGui.BeginTable(key, 2, ImGuiTableFlags.BordersOuter))
			{
				if (pairs != null)
				{
					foreach ((var name, var action, var keys) in pairs)
					{
						if (action == null)
						{
							continue;
						}

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
		ArgumentNullException.ThrowIfNull(reset);

		DrawHotKeysPopup(key, command, ("Reset to Default Value.", reset, stringArray));
	}

	public static void ReactPopup(string key, string command, Action reset, bool showHand = true)
	{
		ArgumentNullException.ThrowIfNull(reset);

		ExecuteHotKeysPopup(key, command, string.Empty, showHand, (reset, new VirtualKey[] { VirtualKey.BACK }));
	}

	public static void ExecuteHotKeysPopup(string key, string command, string tooltip, bool showHand, params (Action action, VirtualKey[] keys)[] pairs)
	{
		if (!ImGui.IsItemHovered())
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
		if (!string.IsNullOrEmpty(command))
		{
			ExecuteHotKeys(() => ExecuteCommand(command), VirtualKey.MENU);
			ExecuteHotKeys(() => CopyCommand(command), VirtualKey.CONTROL);
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
	internal static readonly string[] stringArray = ["Backspace"];

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

	private static void DrawHotKeys(string name, Action action, params string[] keys)
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

		_ = ImGui.TableNextColumn();
		ImGui.TextDisabled(string.Join(' ', keys));
	}

	#endregion

	public static bool IsInRect(Vector2 leftTop, Vector2 size)
	{
		var pos = ImGui.GetMousePos() - leftTop;
		return pos.X > 0 && pos.Y > 0 && pos.X < size.X && pos.Y < size.Y;
	}

	public static string ToSymbol(this ConfigUnitType unit)
	{
		return unit switch
		{
			ConfigUnitType.Seconds => " s",
			ConfigUnitType.Degree => " °",
			ConfigUnitType.Pixels => " p",
			ConfigUnitType.Yalms => " y",
			ConfigUnitType.Percent => " %%",
			_ => string.Empty,
		};
	}

	public static void Draw(this CombatType type)
	{
		var first = true;
		if (type.HasFlag(CombatType.PvE))
		{
			if (!first)
			{
				ImGui.SameLine();
			}

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
			first = false;
		}
		if (type == CombatType.None)
		{
			if (!first)
			{
				ImGui.SameLine();
			}

			ImGui.TextColored(ImGuiColors.DalamudRed, " None of PvE or PvP!");
		}
	}
}