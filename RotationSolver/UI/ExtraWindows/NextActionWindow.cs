using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;

namespace RotationSolver.UI.ExtraWindows;

internal class NextActionWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = FullControlWindow.BaseFlags
	| ImGuiWindowFlags.AlwaysAutoResize
	| ImGuiWindowFlags.NoCollapse
	| ImGuiWindowFlags.NoTitleBar
	| ImGuiWindowFlags.NoResize;

	private readonly record struct TargetHint(IBattleChara Target, string Name, bool IsSelf, bool IsCurrent);

	private const float IconSize = 60f;

	private M3.WindowScaleScope _scale;
	private M3Style.Scope _theme;

	public NextActionWindow()
		: base(nameof(NextActionWindow), BaseFlags)
	{
	}

	public override void PreDraw()
	{
		_scale = M3.PushWindowScale(Service.Config.NextActionWindowScale);
		_theme = M3Style.Push(compact: true);

		ImGui.PushStyleColor(ImGuiCol.WindowBg, Service.Config.InfoWindowBg);

		Flags = BaseFlags;
		if (Service.Config.IsInfoWindowNoInputs)
		{
			Flags |= ImGuiWindowFlags.NoInputs;
		}
		if (Service.Config.IsInfoWindowNoMove)
		{
			Flags |= ImGuiWindowFlags.NoMove;
		}
		base.PreDraw();
	}

	public override void PostDraw()
	{
		ImGui.PopStyleColor();
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
		_scale.Dispose();
		_scale = default;
	}

	public override void Draw()
	{
		var config = Service.Config;
		var windowScale = config.NextActionWindowScale;
		var size = IconSize * windowScale;
		var action = ActionUpdater.NextAction;

		var keybind = config.ShowNextActionKeybind ? HotbarKeybindHelper.GetKeybind(action) : string.Empty;
		var hint = config.TeachingMode && config.TeachingModeShowTargetHint ? GetTargetHint(action) : null;

		// Measure the widest element first. Centering on the window's last width would stop it from ever shrinking.
		var width = size;
		if (keybind.Length > 0)
		{
			width = MathF.Max(width, KeyCapSize(keybind).X);
		}
		if (hint is { } measured)
		{
			width = MathF.Max(width, M3Widgets.PillSize(measured.Name, FontAwesomeIcon.Crosshairs).X);
		}

		var left = ImGui.GetCursorPosX();

		ImGui.SetCursorPosX(left + ((width - size) * 0.5f));
		DrawGcdProgress(size, showTime: false, windowScale);

		ImGui.SetCursorPosX(left + ((width - size) * 0.5f));
		if (M3ActionIcon.Draw("##next_action", action, size, config.ShowCooldownsAlways))
		{
			FullControlWindow.UseOrQueue(action);
		}

		if (keybind.Length > 0)
		{
			ImGui.SetCursorPosX(left + ((width - KeyCapSize(keybind).X) * 0.5f));
			DrawKeyCap(keybind);
		}

		if (hint is { } target)
		{
			ImGui.SetCursorPosX(left + ((width - M3Widgets.PillSize(target.Name, FontAwesomeIcon.Crosshairs).X) * 0.5f));
			DrawTargetHint(target);
		}
	}

	internal static void DrawGcdProgress(float width, bool showTime, float heightScale = 1f)
	{
		var s = M3.Scheme;
		var remain = DataCenter.DefaultGCDRemain;
		var total = DataCenter.DefaultGCDTotal;
		var elapsed = DataCenter.DefaultGCDElapsed;

		float? marker = null;
		if (remain > 0 && total > 0)
		{
			var value = total - DataCenter.CalculatedActionAhead;
			var playerObject = Player.Object;
			if (playerObject != null && value > playerObject.TotalCastTime)
			{
				marker = value / total;
			}
		}

		var fraction = total > 0 ? elapsed / total : 0f;
		M3Widgets.LinearProgress(new Vector2(width, MathF.Max(2f, Service.Config.ControlProgressHeight) * heightScale), fraction, marker);

		if (!showTime)
		{
			return;
		}

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var time = $"{remain:F2}s / {total:F2}s";
		var timeSize = ImGui.CalcTextSize(time);
		ImGui.Dummy(new Vector2(width, timeSize.Y));

		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddText(min, M3.U32(s.OnSurfaceVariant, 0.85f), "GCD");
		drawList.AddText(new Vector2(min.X + width - timeSize.X, min.Y), M3.U32(s.OnSurfaceVariant, 0.85f), time);
	}

	private static Vector2 KeyCapSize(string keybind)
	{
		return ImGui.CalcTextSize(keybind) + (new Vector2(8f, 3f) * M3.Scale * 2f);
	}

	private static void DrawKeyCap(string keybind)
	{
		var s = M3.Scheme;
		var size = KeyCapSize(keybind);

		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		M3Draw.Container(drawList, min, max, s.SurfaceContainerHighest, M3.ShapeExtraSmall, M3.Alpha(s.Outline, 0.7f));

		var textSize = ImGui.CalcTextSize(keybind);
		drawList.AddText(min + ((size - textSize) * 0.5f), M3.U32(s.OnSurface), keybind);
	}

	private static TargetHint? GetTargetHint(IAction? action)
	{
		if (action is not BaseAction baseAction || baseAction.Target.Target is not { } target)
		{
			return null;
		}

		try
		{
			var name = target.Name.TextValue;
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			var targetId = target.GameObjectId;
			return new TargetHint(target, name,
				IsSelf: targetId == (Player.Object?.GameObjectId ?? 0),
				IsCurrent: Svc.Targets.Target?.GameObjectId == targetId);
		}
		catch
		{
			return null;
		}
	}

	private static void DrawTargetHint(in TargetHint hint)
	{
		var s = M3.Scheme;
		var accent = hint.IsSelf ? s.OnSurfaceVariant : hint.IsCurrent ? s.Success : s.Warning;
		var tooltip = hint.IsSelf ? null : hint.IsCurrent ? "Already targeted" : "Click to target";

		if (!M3Widgets.Pill("##next_action_target", hint.Name, accent, FontAwesomeIcon.Crosshairs, tooltip, interactive: !hint.IsSelf))
		{
			return;
		}

		try
		{
			Svc.Targets.Target = hint.Target;
		}
		catch
		{
			// The object may be gone by now.
		}
	}
}
