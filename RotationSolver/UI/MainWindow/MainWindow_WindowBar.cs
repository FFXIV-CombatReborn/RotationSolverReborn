using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RotationSolver.UI.Material;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse;

	private static readonly Vector2 DefaultSize = new(960f, 640f);

	private static readonly WindowSizeConstraints DefaultSizeConstraints = new()
	{
		MinimumSize = new Vector2(360, 340),
		MaximumSize = new Vector2(5000, 5000),
	};

	private const string DiscordUrl = "https://discord.gg/r9V4RHYt6v";
	private const string KofiUrl = "https://ko-fi.com/ltscombatreborn";

	private static readonly M3WindowAction[] _windowActions =
	[
		new("##window_discord", FontAwesomeIcon.Comments, "Join the Discord"),
		new("##window_kofi", FontAwesomeIcon.MugHot, "Support the developer on Ko-fi"),
		new("##window_reset", FontAwesomeIcon.Skull, "Reset all plugin settings"),
	];

	private int _shownActions = _windowActions.Length;
	private float _barTop;

	private bool _minimized;
	private float _minimizeTime;

	private bool _foldLayout;
	private bool _foldSettled;

	private Vector2 _restoreSize;
	private Vector2 _anchorOpen;
	private Vector2 _anchorFolded;

	private Vector2 _windowPos;
	private Vector2 _windowSize;
	private Vector2 _openPadding;
	private float _windowRounding;
	private bool _foldStylePushed;

	internal bool IsMinimized => _minimized;

	private float Folded
	{
		get
		{
			var t = _minimizeTime;
			return t < 0.5f ? 4f * t * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 3f) * 0.5f);
		}
	}

	private M3WindowBrand Brand => new(GetLogoTexture(), "RSR");

	private Vector2 AnchorInset => new(_openPadding.X, _openPadding.Y + _barTop);

	internal void Restore()
	{
		if (!_minimized)
		{
			return;
		}

		if (_minimizeTime >= 1f)
		{
			_anchorOpen = OpenAnchorNear(_anchorFolded);
		}

		_minimized = false;
	}

	private void ToggleMinimized(Vector2 anchor)
	{
		if (_minimized)
		{
			Restore();
			return;
		}

		if (!_foldLayout)
		{
			_restoreSize = _windowSize;
			_anchorOpen = anchor;
			_anchorFolded = anchor;
			_foldLayout = true;
		}

		_minimized = true;
	}

	private void RestoreOnClose()
	{
		if (!_foldLayout)
		{
			return;
		}

		Restore();
		_minimizeTime = 0f;
	}

	private void PrepareFold()
	{
		var style = ImGui.GetStyle();
		_openPadding = style.WindowPadding;
		_windowRounding = style.WindowRounding;

		var resting = _minimized && _minimizeTime >= 1f;

		var target = _minimized ? 1f : 0f;
		if (_minimizeTime != target)
		{
			var step = ImGui.GetIO().DeltaTime / M3Motion.EmphasisedDuration;
			_minimizeTime = _minimized ? MathF.Min(1f, _minimizeTime + step) : MathF.Max(0f, _minimizeTime - step);
		}

		if (!_foldLayout)
		{
			return;
		}

		if (_foldSettled && !_minimized && _minimizeTime <= 0f)
		{
			_foldLayout = false;
			_foldSettled = false;
			Position = null;
			Size = DefaultSize;
			SizeCondition = ImGuiCond.FirstUseEver;
			SizeConstraints = DefaultSizeConstraints;
			Flags = BaseFlags;
			return;
		}

		var folded = Folded;
		var barSize = M3Widgets.WindowActionsSize(_shownActions, Brand, folded);
		var anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		var inset = AnchorInset * (1f - folded);
		var size = Vector2.Lerp(_restoreSize, barSize, folded);

		Position = resting ? null : new Vector2(anchor.X + inset.X - size.X, anchor.Y - inset.Y);
		PositionCondition = ImGuiCond.Always;
		Size = size / ImGuiHelpers.GlobalScale;
		SizeCondition = ImGuiCond.Always;
		SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = Vector2.One,
			MaximumSize = DefaultSizeConstraints.MaximumSize,
		};

		// Don't save settings while folded, so the window reopens at its real size.
		Flags = BaseFlags | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings
			| (resting ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoMove);
		_foldSettled = !_minimized && _minimizeTime <= 0f;

		_windowRounding = float.Lerp(_windowRounding, barSize.Y * 0.5f, folded);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, _windowRounding);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Lerp(_openPadding, Vector2.Zero, folded));
		_foldStylePushed = true;
	}

	private void PopFoldStyle()
	{
		if (_foldStylePushed)
		{
			ImGui.PopStyleVar(2);
			_foldStylePushed = false;
		}
	}

	private (Vector2 Pos, Vector2 Size) OpenRect()
	{
		if (!_foldLayout)
		{
			return (_windowPos, _windowSize);
		}

		var inset = AnchorInset;
		return (new Vector2(_anchorOpen.X + inset.X - _restoreSize.X, _anchorOpen.Y - inset.Y), _restoreSize);
	}

	private Vector2 OpenAnchorNear(Vector2 folded)
	{
		var inset = AnchorInset;
		var pos = new Vector2(folded.X + inset.X - _restoreSize.X, folded.Y - inset.Y);

		var viewport = ImGui.GetMainViewport();
		var min = viewport.WorkPos;
		var max = viewport.WorkPos + viewport.WorkSize;
		if (folded.X >= min.X && folded.X <= max.X && folded.Y >= min.Y && folded.Y <= max.Y)
		{
			pos.X = Math.Clamp(pos.X, min.X, MathF.Max(min.X, max.X - _restoreSize.X));
			pos.Y = Math.Clamp(pos.Y, min.Y, MathF.Max(min.Y, max.Y - _restoreSize.Y));
		}

		return new Vector2(pos.X + _restoreSize.X - inset.X, pos.Y + inset.Y);
	}

	private void DrawWindowBar()
	{
		var folded = Folded;
		var brand = Brand;

		Vector2 anchor;
		if (!_foldLayout)
		{
			var inset = AnchorInset;
			anchor = new Vector2(_windowPos.X + _windowSize.X - inset.X, _windowPos.Y + inset.Y);
		}
		else
		{
			if (_minimized && _minimizeTime >= 1f)
			{
				_anchorFolded = new Vector2(_windowPos.X + _windowSize.X, _windowPos.Y);
			}

			anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		}

		var barSize = M3Widgets.WindowActionsSize(_shownActions, brand, folded);
		ImGui.SetCursorScreenPos(new Vector2(anchor.X - barSize.X, anchor.Y));
		using var bar = ImRaii.Child("##rsr_window_bar", barSize, false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
		if (!bar)
		{
			return;
		}

		var first = _windowActions.Length - _shownActions;
		var pressed = M3Widgets.WindowActions("##rsr_window_actions", anchor, _windowActions.AsSpan(first), brand, folded,
			out var toggled, out var closed, M3.Scheme.SurfaceContainerHigh);

		// Indices follow _windowActions.
		switch (pressed < 0 ? -1 : first + pressed)
		{
			case 0:
				OpenLinkSafely(DiscordUrl);
				break;

			case 1:
				OpenLinkSafely(KofiUrl);
				break;

			case 2:
				_showResetPopup = true;
				break;
		}

		if (toggled)
		{
			ToggleMinimized(anchor);
		}

		if (closed)
		{
			IsOpen = false;
		}
	}
}
