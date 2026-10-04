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

	private readonly M3WindowFold _fold = new();

	private int _shownActions = _windowActions.Length;

	internal bool IsMinimized => _fold.IsMinimized;

	private M3WindowBrand Brand => new(GetLogoTexture(), "RSR Settings");

	internal void Restore()
	{
		_fold.Restore();
	}

	private void PrepareFold()
	{
		Flags = BaseFlags;
		if (_fold.Prepare(this, _shownActions, Brand))
		{
			Position = null;
			Size = DefaultSize;
			SizeCondition = ImGuiCond.FirstUseEver;
			SizeConstraints = DefaultSizeConstraints;
		}
	}

	private void DrawWindowBar()
	{
		var first = _windowActions.Length - _shownActions;
		var pressed = _fold.DrawBar("##rsr_window_actions", _windowActions.AsSpan(first), Brand, out var closed,
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
		}

		if (closed)
		{
			IsOpen = false;
		}
	}
}
