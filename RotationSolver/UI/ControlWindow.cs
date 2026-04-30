using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;

using RotationSolver.Updaters;

namespace RotationSolver.UI;

internal class ControlWindow : CtrlWindow
{
	public static DateTime DidTime { get; set; }
	private static bool _isOpen = false;
	public static bool Opened { get => _isOpen; }

	public ControlWindow()
		: base(nameof(ControlWindow))
	{
		Size = new Vector2(570f, 300f);
		SizeCondition = ImGuiCond.FirstUseEver;
	}

	public override void OnOpen()
	{
		_isOpen = true;
		DataCenter.DrawingActions = true;
		base.OnOpen();
	}

	public override void OnClose()
	{
		_isOpen = false;
		DataCenter.DrawingActions = false;
		base.OnClose();
	}

	public override unsafe void Draw()
	{
		using var selectableAlign = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
		using var framePad = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 3));
		using var childWinPad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12, 12));
		using var frameCellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4, 2));
		using var frameItemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
		using var frameItemInnerSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));
		using var frameIndentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 21f);
		using var frameScrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 16f);
		using var frameGrabMinSize = ImRaii.PushStyle(ImGuiStyleVar.GrabMinSize, 13f);
		using var frameWindowRounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 11f);
		using var frameChildRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 11f);
		using var frameFrameRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 11f);
		using var framePopupRounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 11f);
		using var frameScrollbarRounding = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarRounding, 11f);
		using var frameGrabRounding = ImRaii.PushStyle(ImGuiStyleVar.GrabRounding, 11f);
		using var frameTabRounding = ImRaii.PushStyle(ImGuiStyleVar.TabRounding, 11f);

		ImGui.Columns(3, "Control Bolder", false);
		float gcd = Service.Config.ControlWindowGCDSize
			* Service.Config.ControlWindowNextSizeRatio;
		float ability = Service.Config.ControlWindow0GCDSize
			* Service.Config.ControlWindowNextSizeRatio;
		float width = gcd + ability + ImGui.GetStyle().ItemSpacing.X;

		ImGui.SetColumnWidth(1, 8);

		DrawNextAction(gcd, ability, width);

		ImGui.SameLine();
		float columnWidth = ImGui.GetCursorPosX();
		ImGui.NewLine();

		ImGui.Spacing();
		DrawCommandAction(61822, StateCommandType.Auto, ImGuiColors.DPSRed);
		ImGui.SameLine();
		DrawCommandAction(61751, StateCommandType.Manual, ImGuiColors.DPSRed);
		columnWidth = Math.Max(columnWidth, ImGui.GetCursorPosX());
		ImGui.SameLine();
		DrawCommandAction(61764, StateCommandType.Off, ImGuiColors.DalamudWhite2);
		ImGui.Spacing();
		columnWidth = Math.Max(columnWidth, ImGui.GetCursorPosX());

		TargetingType autoMode = DataCenter.TargetingType;
		ImGui.Text(" Targeting: " + autoMode.ToString());

		ConfigTypes.AoEType aoeType = Service.Config.AoEType;
		if (ImGuiHelper.SelectableButton("AoE: " + aoeType.ToString()))
		{
			aoeType = (ConfigTypes.AoEType)(((int)aoeType + 1) % 3);
			Service.Config.AoEType = aoeType;
		}
		// Track whether the style color was pushed
		bool pushedStyleColor = false;

		ConditionBoolean isBurst = Service.Config.AutoBurst;
		// Track whether the style color was pushed
		pushedStyleColor = false;
		Vector4 color = *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);

		if (!isBurst)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, color);
			pushedStyleColor = true; // Indicate that a style color has been pushed
		}

		if (ImGuiHelper.SelectableButton("Burst"))
		{
			Service.Config.AutoBurst.Value = !isBurst;
		}

		// Ensure PopStyleColor is called only if PushStyleColor was called
		if (pushedStyleColor)
		{
			ImGui.PopStyleColor();
		}
		ImGui.SameLine();

		int value = Service.Config.IsControlWindowLock ? 0 : 1;
		if (ImGuiHelper.SelectableCombo("Rotation Solver Reborn Lock the Control Window",
		[
			UiString.InfoWindowNoMove.GetDescription(),
			UiString.InfoWindowMove.GetDescription(),
		], ref value))
		{
			Service.Config.IsControlWindowLock.Value = value == 0;
		}
		columnWidth = Math.Max(columnWidth, ImGui.GetCursorPosX());
		ImGui.SetColumnWidth(0, columnWidth + 10);

		ImGui.NextColumn();
		ImGui.NextColumn();

		DrawSpecials();

		ImGui.Columns(1);
	}

	private static void DrawSpecials()
	{
		ICustomRotation? rotation = DataCenter.CurrentRotation;

		DrawCommandAction(rotation?.ActionHealAreaGCD, rotation?.ActionHealAreaAbility,
			SpecialCommandType.HealArea, ImGuiColors.HealerGreen);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionHealSingleGCD, rotation?.ActionHealSingleAbility,
			SpecialCommandType.HealSingle, ImGuiColors.HealerGreen);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionDefenseAreaGCD, rotation?.ActionDefenseAreaAbility,
			SpecialCommandType.DefenseArea, ImGuiColors.TankBlue);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionDefenseSingleGCD, rotation?.ActionDefenseSingleAbility,
			SpecialCommandType.DefenseSingle, ImGuiColors.TankBlue);

		ImGui.Spacing();

		DrawCommandAction(rotation?.ActionMoveForwardGCD, rotation?.ActionMoveForwardAbility,
			SpecialCommandType.MoveForward, ImGuiColors.DalamudOrange);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionMoveBackAbility,
			SpecialCommandType.MoveBack, ImGuiColors.DalamudOrange);

		ImGui.SameLine();

		DrawCommandAction(61397, SpecialCommandType.NoCasting, ImGuiColors.DalamudWhite2);

		ImGui.SameLine();

		DrawCommandAction(61804, SpecialCommandType.Burst, ImGuiColors.DalamudWhite2);

		ImGui.SameLine();

		DrawCommandAction(61753, SpecialCommandType.EndSpecial, ImGuiColors.DalamudWhite2);

		ImGui.Spacing();

		DrawCommandAction(rotation?.ActionDispelStancePositionalGCD, rotation?.ActionDispelStancePositionalAbility,
			SpecialCommandType.DispelStancePositional, ImGuiColors.ParsedGold);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionRaiseShirkGCD, rotation?.ActionRaiseShirkAbility,
			SpecialCommandType.RaiseShirk, ImGuiColors.ParsedBlue);

		ImGui.SameLine();


		DrawCommandAction(rotation?.ActionAntiKnockbackAbility,
			SpecialCommandType.AntiKnockback, ImGuiColors.DalamudWhite2);

		ImGui.SameLine();

		DrawCommandAction(rotation?.ActionSpeedAbility,
			SpecialCommandType.Speed, ImGuiColors.DalamudWhite2);

		ImGui.Spacing();

		ImGui.Text("CMD:");
		ImGui.SameLine();

		_ = DrawIAction(DataCenter.CommandNextAction, Service.Config.ControlWindow0GCDSize, 1);

		ImGui.SameLine();

		using var group = ImRaii.Group();
		ImGui.Text(DataCenter.CurrentTargetToHostileType.GetDescription());
		ImGui.Text("Auto: " + DataCenter.AutoStatus.ToString());
	}

	private static void DrawCommandAction(IAction? gcd, IAction? ability, SpecialCommandType command, Vector4 color)
	{
		float gcdW = Service.Config.ControlWindowGCDSize;
		float abilityW = Service.Config.ControlWindow0GCDSize;
		float width = gcdW + abilityW + ImGui.GetStyle().ItemSpacing.X;
		string str = command.ToString();
		float strWidth = ImGui.CalcTextSize(str).X;

		Vector2 pos = ImGui.GetCursorPos();

		using var group = ImRaii.Group();

		using (var subGroup = ImRaii.Group())
		{
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(2, (width / 2) - (strWidth / 2)));
			ImGui.TextColored(color, str);

			string help = command.GetDescription();
			if (ability != null)
			{
				help = help + "\n" + $"({ability.Name})";
			}
			string baseId = "ImgButton" + command.ToString();

			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (strWidth / 2) - (width / 2)));

			if (IconSet.GetTexture(gcd, out IDalamudTextureWrap? texture))
			{
				float y = ImGui.GetCursorPosY();

				string gcdHelp = help;
				if (gcd != null)
				{
					gcdHelp += "\n" + gcd.ToString();
				}
				if (texture?.Handle != null)
				{
					DrawIAction(texture, baseId + nameof(gcd), gcdW, command, gcdHelp);
				}
				if (IconSet.GetTexture(ability, out texture))
				{
					ImGui.SameLine();

					ImGui.SetCursorPosY(y);

					string abilityHelp = help;
					if (ability != null)
					{
						abilityHelp += "\n" + ability.ToString();
					}
					if (texture?.Handle != null)
					{
						DrawIAction(texture, baseId + nameof(ability), abilityW, command, abilityHelp);
					}
				}
			}
		}

		if (DataCenter.SpecialType == command)
		{
			Vector2 size = ImGui.GetItemRectSize();
			Vector2 winPos = ImGui.GetWindowPos();

			HighLight(winPos + pos, size);

			if (DataCenter.SpecialTimeLeft > 0)
			{
				string time = DataCenter.SpecialTimeLeft.ToString("F2") + "s";
				Vector2 strSize = ImGui.CalcTextSize(time);
				ImGuiHelper.TextShade(winPos + pos + size - strSize, time);
			}
		}
	}

	public static void HighLight(Vector2 pt, Vector2 size, float thickness = 2f)
	{
		Vector2 offset = ImGui.GetStyle().ItemSpacing / 2;
		ImGui.GetWindowDrawList().AddRect(pt - offset, pt + size + offset,
			ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey), 5, ImDrawFlags.RoundCornersAll, thickness);
	}

	private static void DrawCommandAction(IAction? ability, SpecialCommandType command, Vector4 color)
	{
		if (ability.GetTexture(out IDalamudTextureWrap? texture))
		{
			DrawCommandAction(texture, command, color, ability?.ToString() ?? "");
		}
	}

	private static void DrawCommandAction(uint iconId, SpecialCommandType command, Vector4 color)
	{
		if (IconSet.GetTexture(iconId, out IDalamudTextureWrap? texture))
		{
			DrawCommandAction(texture, command, color);
		}
	}

	private static void DrawCommandAction(IDalamudTextureWrap texture, SpecialCommandType command, Vector4 color, string helpAddition = "")
	{
		float abilityW = Service.Config.ControlWindow0GCDSize;
		float width = abilityW + (ImGui.GetStyle().ItemInnerSpacing.X * 2);
		string str = command.ToString();
		float strWidth = ImGui.CalcTextSize(str).X;

		Vector2 pos = ImGui.GetCursorPos();

		using var group = ImRaii.Group();

		using (var subGroup = ImRaii.Group())
		{
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (width / 2) - (strWidth / 2)));
			ImGui.TextColored(color, str);

			string help = command.GetDescription();
			if (!string.IsNullOrEmpty(helpAddition))
			{
				help += "\n" + helpAddition;
			}
			string baseId = "ImgButton" + command.ToString();

			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (strWidth / 2) - (width / 2)));
			if (texture?.Handle != null)
			{
				DrawIAction(texture, baseId, abilityW, command, help);
			}
		}

		if (DataCenter.SpecialType == command)
		{
			Vector2 size = ImGui.GetItemRectSize();
			Vector2 winPos = ImGui.GetWindowPos();

			HighLight(winPos + pos, size);

			if (DataCenter.SpecialTimeLeft > 0)
			{
				string time = DataCenter.SpecialTimeLeft.ToString("F2") + "s";
				Vector2 strSize = ImGui.CalcTextSize(time);
				ImGuiHelper.TextShade(winPos + pos + size - strSize, time);
			}
		}
	}

	private static void DrawCommandAction(uint iconId, StateCommandType command, Vector4 color)
	{
		float abilityW = Service.Config.ControlWindow0GCDSize;
		float width = abilityW + (ImGui.GetStyle().ItemInnerSpacing.X * 2);
		string str = command.ToString();
		float strWidth = ImGui.CalcTextSize(str).X;

		Vector2 pos = ImGui.GetCursorPos();

		using (var group = ImRaii.Group())
		{
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (width / 2) - (strWidth / 2)) - 3.5f);
			ImGui.TextColored(color, str);

			string help = command.GetDescription();
			string baseId = "ImgButton" + command.ToString();

			if (IconSet.GetTexture(iconId, out IDalamudTextureWrap? texture) && texture?.Handle != null)
			{
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (strWidth / 2) - (width / 2)));
				DrawIAction(texture, baseId, abilityW, command, help);
			}
		}

		bool isMatch = false;
		switch (command)
		{
			case StateCommandType.Auto when DataCenter.State && !DataCenter.IsManual:
			case StateCommandType.Manual when DataCenter.State && DataCenter.IsManual:
			case StateCommandType.Off when !DataCenter.State:
				isMatch = true;
				break;
		}

		if (isMatch)
		{
			Vector2 size = ImGui.GetItemRectSize();
			Vector2 winPos = ImGui.GetWindowPos();

			HighLight(winPos + pos, size);
		}
	}

	private static void DrawIAction(IDalamudTextureWrap handle, string id, float width, SpecialCommandType command, string help)
	{
		Vector2 cursor = ImGui.GetCursorPos();
		if (ImGuiHelper.NoPaddingNoColorImageButton(handle, Vector2.One * width, id))
		{
			_ = Svc.Commands.ProcessCommand(command.GetCommandStr());
		}
		ImGuiHelper.DrawActionOverlay(cursor, width, IconSet.GetTexture(0u, out IDalamudTextureWrap? text) && text?.Handle != null && text.Handle.Handle == handle.Handle ? -1 : 1);
		ImguiTooltips.HoveredTooltip(help);
	}

	private static void DrawIAction(IDalamudTextureWrap handle, string id, float width, StateCommandType command, string help)
	{
		Vector2 cursor = ImGui.GetCursorPos();
		if (ImGuiHelper.NoPaddingNoColorImageButton(handle, Vector2.One * width, id))
		{
			_ = Svc.Commands.ProcessCommand(command.GetCommandStr());
		}
		ImGuiHelper.DrawActionOverlay(cursor, width, 1);
		ImguiTooltips.HoveredTooltip(help);
	}

	internal static (Vector2, Vector2) DrawIAction(IAction? action, float width, float percent, bool isAdjust = true)
	{
		if (!action.GetTexture(out IDalamudTextureWrap? texture, isAdjust))
		{
			return (default, default);
		}

		Vector2 cursor = ImGui.GetCursorPos();

		string desc = action?.Name ?? string.Empty;
		if (texture?.Handle != null && ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * width, desc))
		{
			if (!DataCenter.State)
			{
				bool canDoIt = false;
				if (action is IBaseAction act)
				{
					IBaseAction.ForceEnable = true;
					canDoIt = act.CanUse(out _, usedUp: true, skipAoeCheck: true);
					IBaseAction.ForceEnable = false;
				}
				else if (action is IBaseItem item)
				{
					canDoIt = item.CanUse(out _, true);
				}
				if (canDoIt)
				{
					_ = (action?.Use());
				}
			}
			else if (action != null)
			{
				DataCenter.AddCommandAction(action, 5);
			}
		}
		Vector2 size = ImGui.GetItemRectSize();
		Vector2 pos = cursor;

		if (action == null || !Service.Config.ShowCooldownsAlways)
		{
			ImGuiHelper.DrawActionOverlay(pos, width, -1);
			ImguiTooltips.HoveredTooltip(desc);

			return (pos, size);
		}
		else
		{
			float recast = action.Cooldown.RecastTimeOneChargeRaw;
			float elapsed = action.Cooldown.RecastTimeElapsedRaw;
			Vector2 winPos = ImGui.GetWindowPos();
			float r = -1f;
			if (Service.Config.UseOriginalCooldown)
			{
				r = !action.EnoughLevel ? 0 : recast == 0 || !action.Cooldown.IsCoolingDown ? 1 : elapsed / recast;
			}
			ImGuiHelper.DrawActionOverlay(cursor, width, r);
			ImguiTooltips.HoveredTooltip(desc);

			if (!action.EnoughLevel)
			{
				if (!Service.Config.UseOriginalCooldown)
				{
					ImGui.GetWindowDrawList().AddRectFilled(new Vector2(pos.X, pos.Y) + winPos,
						new Vector2(pos.X + size.X, pos.Y + size.Y) + winPos, ImGuiHelper.ProgressCol);
				}
			}
			else if (action.Cooldown.IsCoolingDown)
			{
				if (!Service.Config.UseOriginalCooldown)
				{
					float ratio = recast == 0 || !action.EnoughLevel ? 0 : elapsed % recast / recast;
					Vector2 startPos = new Vector2(pos.X + (size.X * ratio), pos.Y) + winPos;
					ImGui.GetWindowDrawList().AddRectFilled(startPos,
						new Vector2(pos.X + size.X, pos.Y + size.Y) + winPos, ImGuiHelper.ProgressCol);

					ImGui.GetWindowDrawList().AddLine(startPos, startPos + new Vector2(0, size.Y), ImGuiHelper.Black);
				}

				using var font = ImRaii.PushFont(ImGui.GetFont());
				string time = recast == 0 ? "0" : ((int)(recast - (elapsed % recast)) + 1).ToString();
				Vector2 strSize = ImGui.CalcTextSize(time);
				Vector2 fontPos = new Vector2(pos.X + (size.X / 2) - (strSize.X / 2), pos.Y + (size.Y / 2) - (strSize.Y / 2)) + winPos;

				ImGuiHelper.TextShade(fontPos, time);
			}

			if (action.EnoughLevel && action is IBaseAction bAct && bAct.Cooldown.MaxCharges > 1)
			{
				for (int i = 0; i < bAct.Cooldown.CurrentCharges; i++)
				{
					ImGui.GetWindowDrawList().AddCircleFilled(winPos + pos + ((i + 0.5f) * new Vector2(width / 5, 0)), width / 12, ImGuiHelper.White);
				}
			}

			return (pos, size);
		}
	}

	private static unsafe void DrawNextAction(float gcd, float ability, float width)
	{
		using var group = ImRaii.Group();

		string str = "Next Action";
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width / 2) - (ImGui.CalcTextSize(str).X / 2));
		ImGui.TextColored(ImGuiColors.DalamudYellow, str);

		NextActionWindow.DrawGcdCooldown(width, true);

		float y = ImGui.GetCursorPosY();

		_ = DrawIAction(ActionUpdater.NextGCDAction, gcd, 1);

		IAction? next = ActionUpdater.NextGCDAction != ActionUpdater.NextAction ? ActionUpdater.NextAction : null;

		ImGui.SameLine();

		ImGui.SetCursorPosY(y);

		_ = DrawIAction(next, ability, 1);
	}
}
