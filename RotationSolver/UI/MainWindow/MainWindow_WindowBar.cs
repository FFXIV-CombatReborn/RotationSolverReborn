using Dalamud.Interface.Windowing;
using RebornMaterial;

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
		new("##window_lock", FontAwesomeIcon.LockOpen, "Lock the window in place"),
	];

	private static readonly M3WindowAction[] _lockedWindowActions =
	[
		.. _windowActions[..^1],
		new("##window_lock", FontAwesomeIcon.Lock, "Locked in place. Click to allow moving and resizing."),
	];

	private readonly M3WindowFold _fold = new();

	private int _shownActions = _windowActions.Length;

	private Vector2 _windowPos;
	private Vector2 _windowSize;

	internal bool IsMinimized => _fold.IsMinimized;

	private M3WindowBrand Brand => new(GetLogoTexture(), "RSR Settings");

	internal void Restore()
	{
		_fold.Restore();
	}

	private void PrepareFold()
	{
		Flags = BaseFlags;
		var locked = Service.Config.IsMainWindowLock;
		if (locked)
		{
			Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
		}

		if (_fold.Prepare(this, _shownActions, Brand))
		{
			Position = null;
			Size = DefaultSize;
			SizeCondition = ImGuiCond.FirstUseEver;
			SizeConstraints = DefaultSizeConstraints;
		}

		if (!_fold.IsActive)
		{
			Position = locked ? OnScreenPosition() : null;
			PositionCondition = ImGuiCond.Always;
		}
	}

	// A locked window can't be dragged back, so keep it on screen. Null when it already fits.
	private Vector2? OnScreenPosition()
	{
		if (_windowSize == Vector2.Zero)
		{
			return null;
		}

		var viewport = ImGui.GetMainViewport();
		var min = viewport.WorkPos;
		var max = viewport.WorkPos + viewport.WorkSize;

		// The action pill with the lock is top-right, so the top and right edges win on a window bigger than the screen.
		var x = MathF.Min(MathF.Max(_windowPos.X, min.X), max.X - _windowSize.X);
		var y = MathF.Max(MathF.Min(_windowPos.Y, max.Y - _windowSize.Y), min.Y);

		// Whole pixels, since ImGui truncates window positions.
		var pos = new Vector2(MathF.Floor(x), MathF.Floor(y));
		return pos == _windowPos ? null : pos;
	}

	private void DrawWindowBar()
	{
		var locked = Service.Config.IsMainWindowLock;
		var actions = locked ? _lockedWindowActions : _windowActions;
		var first = actions.Length - _shownActions;
		var pressed = _fold.DrawBar("##rsr_window_actions", actions.AsSpan(first), Brand, out var closed,
			M3.Scheme.SurfaceContainerHigh);

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

			case 3:
				Service.Config.IsMainWindowLock = !locked;
				break;
		}

		if (closed)
		{
			IsOpen = false;
		}
	}
}
