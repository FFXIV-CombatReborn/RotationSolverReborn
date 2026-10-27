using Dalamud.Common;
using Dalamud.Common.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI;

public partial class MainWindow : Window
{
	private static float Scale => M3.Scale;

	private MainWindowTab _activeTab;

	private List<IncompatiblePlugin> _crashPlugins = [];
	private List<IncompatiblePlugin> _enabledIncompatiblePlugins = [];
	private static DiagInfo? _cachedDiagInfo;
	private bool _showResetPopup = false;
	private M3Style.Scope _theme;

	// Keep this in this file: the page files read it while setting up, and this file is set up first.
	internal static SearchableCollection _allSearchable = new();

	private static bool? _cnLanguageClient;

	public static bool CNLanguageClient => _cnLanguageClient ??= GetClientLanguage() is "Chinese" or "ChineseSimplified";

	private static string GetClientLanguage()
	{
		return DalamudReflector.TryGetDalamudStartInfo(out var startInfo, Svc.PluginInterface)
			? startInfo.Language.ToString()
			: Svc.ClientState.ClientLanguage.ToString();
	}

	internal sealed class DiagInfo(DalamudStartInfo startInfo)
	{
		public string RSRVersion { get; } = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?";
		public GameVersion? GameVersion { get; } = startInfo.GameVersion;
		public string Platform { get; } = startInfo.Platform.ToString();
		public ClientLanguage Language { get; } = startInfo.Language;
	}

	public MainWindow()
	: base("###rsrConfigWindow", BaseFlags, false)
	{
		SizeCondition = ImGuiCond.FirstUseEver;
		Size = DefaultSize;
		SizeConstraints = DefaultSizeConstraints;
		RespectCloseHotkey = true;

		AllowPinning = false;
		AllowClickthrough = false;
	}

	public override void OnOpen()
	{
		// Clear any pin or click-through left over from when the window had a title bar.
		IsPinned = false;
		IsClickthrough = false;

		_enabledIncompatiblePlugins = [];
		_crashPlugins = [];

		foreach (var p in PluginCompatibility.IncompatiblePlugins)
		{
			if (p.IsInstalled && p.IsEnabled)
			{
				_enabledIncompatiblePlugins.Add(p);
			}
		}

		if (DalamudReflector.TryGetDalamudStartInfo(out var startinfo, Svc.PluginInterface))
		{
			_cachedDiagInfo = new DiagInfo(startinfo);
		}
		else
		{
			PluginLog.Error("Failed to get Dalamud start info.");
		}

		base.OnOpen();
	}

	public override void OnClose()
	{
		Service.Config.Save();
		_cachedDiagInfo = null;

		M3Motion.Reset();
		M3CardHost.Reset();
		M3Snackbar.Clear();
		_fold.Reset();

		base.OnClose();
	}

	internal void SetActiveTab(MainWindowTab tab)
	{
		_activeTab = tab;
		_searchResults = [];
	}

	// Push the theme before Begin, since Begin draws the window's background, padding and corners.
	public override void PreDraw()
	{
		_theme = M3Style.Push(M3Density.Tight);
		PrepareFold();
		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();

		_fold.PopStyle();
		_theme.Dispose();
		_theme = default;
	}

	public override void Draw()
	{
		_fold.BeginDraw();
		_windowPos = ImGui.GetWindowPos();
		_windowSize = ImGui.GetWindowSize();

		if (_fold.Amount < 1f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, _fold.ContentAlpha);
			M3Draw.WindowBackdrop(_fold.Rounding);
			DrawWindowContent();
		}

		DrawResetDialog();
		DrawWindowBar();
	}

	private void DrawWindowContent()
	{
		using var content = _fold.BeginContent("##rsr_window_content");
		if (!content)
		{
			return;
		}

		try
		{
			// Not a table: ImGui saves table column widths and they can't be clamped later. Capped at half the window.
			var available = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
			var spacing = ImGui.GetStyle().ItemSpacing.X;
			var sidebarWidth = MathF.Min(NavigationColumnMinWidth(), available * 0.5f);

			var origin = ImGui.GetCursorScreenPos();
			var paneHeight = MathF.Max(1f, ImGui.GetContentRegionAvail().Y);
			var dividerX = origin.X + sidebarWidth + (spacing * 0.5f);
			ImGui.GetWindowDrawList().AddLine(
				new Vector2(dividerX, origin.Y),
				new Vector2(dividerX, origin.Y + paneHeight),
				M3.U32(M3.Scheme.OutlineVariant, 0.45f), 1f * Scale);

			try
			{
				DrawNavigationColumn(sidebarWidth);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Something wrong with sideBar: {ex.Message}");
			}

			ImGui.SameLine(0f, spacing);

			try
			{
				DrawBody();
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Something wrong with body: {ex.Message}");
			}

			M3Snackbar.Draw(new Vector2(origin.X + sidebarWidth + spacing, origin.Y),
				new Vector2(origin.X + available, origin.Y + paneHeight));
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Something wrong with config window: {ex.Message}");
		}
	}

	private void DrawResetDialog()
	{
		const string title = "Reset RSR Plugin Settings";

		if (_showResetPopup)
		{
			ImGui.OpenPopup(title);
			_showResetPopup = false;
		}

		ImGui.SetNextWindowSizeConstraints(new Vector2(380, 0) * Scale, new Vector2(520, 400) * Scale);

		using var popup = ImRaii.PopupModal(title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
		if (!popup)
		{
			return;
		}

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			ImGui.TextUnformatted(CNLanguageClient ? "重置所有插件设置？" : "Reset all plugin settings?");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant))
		{
			ImGui.TextWrapped(CNLanguageClient
				? "如果你在使用旧版默认配置的 RSR 时遇到问题，通常推荐执行此操作。此操作无法撤销。"
				: "This is often recommended for users having issues while using an installation of RSR with an outdated default configuration. This cannot be undone.");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));

		var cancelLabel = CNLanguageClient ? "取消" : "Cancel";
		var resetLabel = CNLanguageClient ? "重置" : "Reset everything";
		var resetWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.TrashAlt, resetLabel);
		var cancelWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.None, cancelLabel);

		// Right-aligned against the dialog's own padding, whatever the theme sets it to.
		ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(),
			ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - resetWidth - M3.Space2 - cancelWidth));

		if (M3Widgets.Button("##reset_cancel", cancelLabel, M3ButtonStyle.Text))
		{
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##reset_confirm", resetLabel, M3ButtonStyle.Danger, FontAwesomeIcon.TrashAlt))
		{
			Service.Config = new Configs();
			Service.Config.Save();
			ImGui.CloseCurrentPopup();
		}
	}

	private void DrawTopAppBar()
	{
		var searching = _searchResults is { Length: > 0 };
		var title = searching
			? UiString.ConfigWindow_Search_Result.GetDescription()
			: GetActiveTabTitle();
		var subtitle = searching
			? string.Format("{0} matching settings", _searchResults!.Length)
			: _activeTab.GetDescription();
		var clearWidth = searching ? M3Widgets.IconButtonSize + (4f * Scale) : 0f;

		// A locked window can't be resized, so it never drops the lock, its last action.
		var kept = Service.Config.IsMainWindowLock ? 1 : 0;
		using var bar = M3TopAppBar.Begin(title, subtitle, _windowActions.Length, Brand, clearWidth, M3.Space1, kept);
		_shownActions = bar.ShownActions;
		_fold.BarTop = bar.BarTop;

		if (searching)
		{
			ImGui.SetCursorScreenPos(bar.TrailingPosition(M3Widgets.IconButtonSize));
			if (M3Widgets.IconButton("##appbar_clear_search", FontAwesomeIcon.Eraser, "Clear search results"))
			{
				_searchText = string.Empty;
				_searchResults = [];
			}
		}
	}

	private string GetActiveTabTitle()
	{
		if (_activeTab == MainWindowTab.Job && Player.Object != null)
		{
			return CNLanguageClient
				? Player.ClassJob.ValueNullable?.Name.ExtractText() ?? Player.Job.ToString()
				: Player.Job.ToString();
		}

		if (_activeTab == MainWindowTab.DutyRotation)
		{
			return GetDutyRotationTabName();
		}

		return CNLanguageClient ? _activeTab.CNString() : _activeTab.ToString();
	}

	private void DrawBody()
	{
		using var child = ImRaii.Child("Rotation Solver Body", -Vector2.One, false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!child)
		{
			return;
		}

		DrawTopAppBar();

		using var page = ImRaii.Child("Rotation Solver Page", -Vector2.One);
		if (!page)
		{
			return;
		}

		if (_searchResults is { Length: > 0 })
		{
			DrawSearchResults();
			return;
		}

		_ = DrawNotices();

		switch (_activeTab)
		{
			case MainWindowTab.Main:
			case MainWindowTab.About:
				DrawAbout();
				break;

			case MainWindowTab.Job:
			case MainWindowTab.Rotation:
				DrawRotation();
				break;

			case MainWindowTab.DutyRotation:
				DrawDutyRotationBody();
				break;

			case MainWindowTab.AutoDuty:
				DrawAutoduty();
				break;

			case MainWindowTab.Actions:
				DrawActions();
				break;

			case MainWindowTab.List:
				DrawList();
				break;

			case MainWindowTab.Basic:
				DrawBasic();
				break;

			case MainWindowTab.UI:
				DrawUI();
				break;

			case MainWindowTab.Auto:
				DrawAuto();
				break;

			case MainWindowTab.Target:
				DrawTarget();
				break;

			case MainWindowTab.Duty:
				DrawDutySpecific();
				break;

			case MainWindowTab.Extra:
				DrawExtra();
				break;

			case MainWindowTab.Debug:
				DrawDebug();
				break;

			default:
				ImGui.TextUnformatted("Unknown tab selected.");
				break;
		}
	}

	private static void OpenLinkSafely(string url)
	{
		try
		{
			Util.OpenLink(url);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to open {url}: {ex.Message}");
		}
	}

	private static void DrawPageIntro(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.92f)))
		{
			ImGui.TextWrapped(text);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
	}

	private static CollapsingHeaderGroup BuildHeaderGroup(
		Dictionary<Func<string>, Action> headers,
		params (UiString Title, FontAwesomeIcon Icon)[] icons)
	{
		var group = new CollapsingHeaderGroup(headers);
		foreach (var (title, icon) in icons)
		{
			group.SetHeaderIcon(title.GetDescription(), icon);
		}

		return group;
	}
}
