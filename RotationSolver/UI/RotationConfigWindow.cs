using Dalamud.Common;
using Dalamud.Common.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Rotations.Duties;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.IPC;
using RotationSolver.UI.SearchableConfigs;
using RotationSolver.Updaters;
using System.Diagnostics;
using System.Text;
using GAction = Lumina.Excel.Sheets.Action;
using Status = Lumina.Excel.Sheets.Status;

namespace RotationSolver.UI;

public partial class RotationConfigWindow : Window
{
    private static float Scale => ImGuiHelpers.GlobalScale;

    private RotationConfigWindowTab _activeTab;

    private const float MIN_COLUMN_WIDTH = 24;
    private const float JOB_ICON_WIDTH = 50;

    private List<IncompatiblePlugin> _crashPlugins = [];
    private List<IncompatiblePlugin> _enabledIncompatiblePlugins = [];
    private DiagInfo? _cachedDiagInfo;
    private RotationAttribute _curRotationAttribute = new("Unknown", CombatType.PvE);
    private ICustomRotation? _currentRotation;
    private Dictionary<RotationConfigWindowTab, (bool, uint)> _configWindowTabProperties = [];
    private bool _showResetPopup = false;

    // Cache for remote logo texture to avoid per-frame retrieval
    private IDalamudTextureWrap? _logoTexture;
    private DateTime _lastLogoFetchAttempt = DateTime.MinValue;

    // Easter egg: press-and-hold on the RSR icon opens Tic-tac-toe
    private double _rsrIconPressStart = -1;
    private bool _rsrIconTriggered = false;
    private const double RsrIconHoldSeconds = 1.2;

    // Hints system fields
    private static readonly string[] _usageHints =
    [
        "Right-click any action, setting, or toggle to view/copy its macro chat command.",
        "Use /rsr as a shorter alias for /rotation.",
        "Use /rotation Auto, /rotation Manual, or /rotation Off to change modes quickly.",
        "Use the search box (top-left) to jump directly to settings.",
        "Click the external-link icon in search results to jump to that menu.",
        "Right-click a setting label to copy a ready-to-use /rotation Settings command.",
        "Actions tab: click an action icon to configure, enable/disable, or set hotkeys.",
        "Actions: toggle 'Show on CD Window' to include an action in the cooldown overlay.",
        "Actions: enable 'Intercepted' to let RSR fire an action you queue (PvE only).",
        "UI > Information: enable DTR status, toasts, original cooldowns, and these hints.",
        "UI > Windows: enable Next Action, Control, Cooldown, and Timeline windows.",
        "Next Action: 'No Inputs' and 'No Move' options change overlay behavior.",
        "Only show windows in duty or with enemies: UI > Windows > Only show with hostile or in duty.",
        "List tab: manage dispels, priority statuses, knockbacks, invincibility, and no-casting lists.",
        "List tab: use 'Reset and Update' to restore curated lists quickly.",
        "Status lists: press '+' to search by name or ID; fuzzy search is supported.",
        "Status lists: right-click an icon to remove; Delete key works in the popup too.",
        "Target tab: tweak target selection, vision cone, engage behavior, and dummy/boss handling.",
        "Target tab: set /rotation Cycle behaviour and targeting delays.",
        "Manage TargetingTypes via chat: /rotation Settings TargetingTypes add|remove <Type>.",
        "Auto > Action Usage: allow/deny oGCDs, set AoE style, tinctures, interrupts, and True North.",
        "Auto > Healing: adjust thresholds and non-healer healing behavior.",
        "Healer: customize Raise/Swiftcast and prioritization in Auto > Healing.",
        "Ground AoEs: Auto > Healing has options to place beneficial ground actions smartly.",
        "Basic > Timer: tune Action Ahead and Min Updating Time to balance performance vs weaving.",
        "Basic > Auto Switch: auto on/off for countdowns, deaths, area transitions, and more.",
        "Teaching Mode highlights targets; color is in UI > Information.",
        "Job tab: edit DNC partner, SGE Kardia tank, and AST card priorities when on those jobs.",
        "About > Macros lists available chat/macro commands and helpful syntax.",
        "About > Links: open config folder, GitHub, Ko-fi, and Discord.",
        "Extra > Internal: Backup/Restore configs safely.",
        "Extra: optional tweaks like removing animation/cooldown delay.",
        "Click the cube icon at the bottom-left of the sidebar to copy diagnostic info to clipboard.",
        "Timeline window can visualize recent actions (UI > Windows).",
        "Do damage, don't die",
        "Healing Tip: the only HP that matters is the last one",
        "Be kind",
        "The icons for Combat Reborn were made by a player named Altan",
        "Tip: you can remove some self-buffs with “/statusoff <Name>” (e.g., Peloton) when needed.",
        "Tip: RSR works best with Legacy Type movement settings."
    ];
    private int _hintIndex = 0;
    private float _lastHintSwitch = 0f;
    private static readonly Random _hintRng = new();

    public RotationConfigWindow()
    : base("###rsrConfigWindow", ImGuiWindowFlags.NoScrollbar, false)
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(740f, 490f);
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 300),
            MaximumSize = new Vector2(5000, 5000),
        };
        RespectCloseHotkey = true;

        TitleBarButtons.Add(new TitleBarButton()
        {
            Icon = FontAwesomeIcon.Skull,
            ShowTooltip = () =>
            {
                ImGui.BeginTooltip();
                ImGui.Text("Click to reset plugin configs");
                ImGui.EndTooltip();
            },
            Priority = 3,
            Click = _ =>
            {
                _showResetPopup = true;
            },
            AvailableClickthrough = true
        });

        TitleBarButtons.Add(new TitleBarButton()
        {
            Icon = FontAwesomeIcon.MugHot,
            ShowTooltip = () =>
            {
                ImGui.BeginTooltip();
                ImGui.Text("Support the developer on Ko-fi");
                ImGui.EndTooltip();
            },
            Priority = 2,
            Click = _ =>
            {
                try
                {
                    Util.OpenLink("https://ko-fi.com/ltscombatreborn");
                }
                catch
                {
                    // ignored
                }
            },
            AvailableClickthrough = true
        });
    }

    public override void OnOpen()
    {
        _enabledIncompatiblePlugins = [];
        _crashPlugins = [];

        foreach (var p in DownloadHelper.IncompatiblePlugins ?? [])
        {
            if (p.IsInstalled && p.IsEnabled)
            {
                _enabledIncompatiblePlugins.Add(p);
            }
        }

        if (DalamudReflector.TryGetDalamudStartInfo(out DalamudStartInfo? startinfo, Svc.PluginInterface))
        {
            _cachedDiagInfo = new DiagInfo(startinfo);
        }
        else
        {
            PluginLog.Error("Failed to get Dalamud start info.");
        }

        if (_configWindowTabProperties.Count == 0)
        {
            foreach (RotationConfigWindowTab tab in Enum.GetValues<RotationConfigWindowTab>())
            {
                bool shouldSkip = false;
                if (tab.GetAttribute<TabSkipAttribute>() != null)
                {
                    shouldSkip = true;
                }

                _configWindowTabProperties[tab] = (shouldSkip, tab.GetAttribute<TabIconAttribute>()?.Icon ?? 0);
            }
        }

        // Preload logo texture once
        try
        {
            string logoUrl = $"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Images/Logo.png";
            if (ThreadLoadImageHandler.TryGetTextureWrap(logoUrl, out IDalamudTextureWrap? logo) && logo != null)
            {
                _logoTexture = logo;
            }
        }
        catch
        {
            // ignore
        }

        base.OnOpen();
    }

    public override void OnClose()
    {
        Service.Config.Save();
        ActionSequencerUpdater.SaveFiles();
        _cachedDiagInfo = null;
        base.OnClose();
    }

    public override void Draw()
    {
        if (_showResetPopup)
        {
            ImGui.OpenPopup("Reset RSR Plugin Settings");
            _showResetPopup = false;
        }

        // Custom padding for this popup only (affects this modal window)
        using var popupWinPad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12, 12) * Scale);
        using var popupFramePad = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 3) * Scale);
        using var popupCellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4, 2) * Scale);
        using var popupItemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4) * Scale);
        using var popupItemInnerSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4) * Scale);
        using var popupIndentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 21f * Scale);
        using var popupScrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 16f * Scale);
        using var popupGrabMinSize = ImRaii.PushStyle(ImGuiStyleVar.GrabMinSize, 13f * Scale);
        using var popupWindowBorderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 0f * Scale);
        using var popupChildRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 11f * Scale);
        using var popupFrameRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 11f * Scale);
        using var popupPopupRounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 11f * Scale);
        using var popupScrollbarRounding = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarRounding, 11f * Scale);
        using var popupGrabRounding = ImRaii.PushStyle(ImGuiStyleVar.GrabRounding, 11f * Scale);
        using var popupTabRounding = ImRaii.PushStyle(ImGuiStyleVar.TabRounding, 11f * Scale);
        if (ImGui.BeginPopupModal("Reset RSR Plugin Settings"))
        {
            ImGui.Text("Are you sure you want to reset all plugin settings?");
            ImGui.Spacing();
            ImGui.Text("This is often recommended for users having issues while using an installation of RSR using an outdated default configuration.");
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(120, 0)))
            {
                Service.Config = new Configs();
                Service.Config.Save();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        // This affects framed widgets and child windows you create below
        using ImRaii.Style selectableAlign = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
        using var framePad = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 3) * Scale);
        using var childWinPad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12, 12) * Scale);
        using var frameCellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4, 2) * Scale);
        using var frameItemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4) * Scale);
        using var frameItemInnerSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4) * Scale);
        using var frameIndentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 21f * Scale);
        using var frameScrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 16f * Scale);
        using var frameGrabMinSize = ImRaii.PushStyle(ImGuiStyleVar.GrabMinSize, 13f * Scale);
        using var frameWindowRounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 11f * Scale);
        using var frameChildRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 11f * Scale);
        using var frameFrameRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 11f * Scale);
        using var framePopupRounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 11f * Scale);
        using var frameScrollbarRounding = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarRounding, 11f * Scale);
        using var frameGrabRounding = ImRaii.PushStyle(ImGuiStyleVar.GrabRounding, 11f * Scale);
        using var frameTabRounding = ImRaii.PushStyle(ImGuiStyleVar.TabRounding, 11f * Scale);
        try
        {
            using ImRaii.IEndObject table = ImRaii.Table("Rotation Config Table", 2, ImGuiTableFlags.Resizable);
            if (table)
            {
                ImGui.TableSetupColumn("Rotation Config Side Bar", ImGuiTableColumnFlags.WidthFixed, 100 * Scale);
                _ = ImGui.TableNextColumn();
                try
                {
                    DrawSideBar();
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Something wrong with sideBar: {ex.Message}");
                }
                _ = ImGui.TableNextColumn();
                try
                {
                    DrawBody();
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Something wrong with body: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Something wrong with config window: {ex.Message}");
        }
    }

    private bool CheckErrors()
    {
        if (_crashPlugins.Count != 0)
        {
            return true;
        }

        if (DataCenter.SystemWarnings != null && DataCenter.SystemWarnings.Count > 0)
        {
            return true;
        }

        if (Watcher.DalamudBranch() != "release")
        {
            return true;
        }

        return Player.AvailableThreadSafe && (Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
        Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
        Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN);
    }

    internal sealed class DiagInfo(DalamudStartInfo startInfo)
    {
        public string RSRVersion { get; } = typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?";
        public GameVersion? GameVersion { get; } = startInfo.GameVersion;
        public string Platform { get; } = startInfo.Platform.ToString();
        public string DalamudBranch { get; } = Watcher.DalamudBranch();
        public ClientLanguage Language { get; } = startInfo.Language;
    }

    private void DrawDiagnosticInfoCube()
    {
        StringBuilder diagInfo = new();

        if (_cachedDiagInfo == null && DalamudReflector.TryGetDalamudStartInfo(out Dalamud.Common.DalamudStartInfo? startinfo, Svc.PluginInterface))
        {
            _cachedDiagInfo = new DiagInfo(startinfo);
        }

        if (_cachedDiagInfo == null)
        {
            _ = diagInfo.AppendLine($"Rotation Solver Reborn v{typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?"}");
            _ = diagInfo.AppendLine("Failed to get Dalamud start info.");
        }
        else
        {
            _ = diagInfo.AppendLine($"Rotation Solver Reborn v{_cachedDiagInfo.RSRVersion}");
            _ = diagInfo.AppendLine($"FFXIV Version: {_cachedDiagInfo.GameVersion}");
            _ = diagInfo.AppendLine($"OS Type: {_cachedDiagInfo.Platform}");
            _ = diagInfo.AppendLine($"Dalamud Branch: {_cachedDiagInfo.DalamudBranch}");
            _ = diagInfo.AppendLine($"Game Language: {_cachedDiagInfo.Language}");
            _ = diagInfo.AppendLine($"Update Frequency: {Service.Config.MinUpdatingTime}");
            _ = diagInfo.AppendLine($"Intercept: {Service.Config.InterceptAction2}");
            _ = diagInfo.AppendLine($"Player Level: {DataCenter.PlayerSyncedLevel()}");
            _ = diagInfo.AppendLine($"Player Job: {Player.Job}");
            _ = diagInfo.AppendLine($"AutoFaceTargetOnActionSetting: {DataCenter.AutoFaceTargetOnActionSetting()}");
            var moveModeValue = DataCenter.MoveModeSetting();
            string moveModeText = moveModeValue switch
            {
                0 => "Standard",
                1 => "Legacy",
                _ => moveModeValue.ToString()
            };
            _ = diagInfo.AppendLine($"MoveModeSetting: {moveModeText}");
        }

        // Ensure that IncompatiblePlugins is not null
        IncompatiblePlugin[] incompatiblePlugins = DownloadHelper.IncompatiblePlugins ?? [];

        bool anyCrash = false;
        _ = diagInfo.AppendLine("\nPlugins:");
        foreach (IncompatiblePlugin item in incompatiblePlugins)
        {
            if (item.IsEnabled)
            {
                string name = item.Name ?? "Unnamed Incompatible Plugin";

                // Flag that at least one crash-prone plugin is enabled so the info marker pulses red
                if (item.Type.HasFlag(CompatibleType.Crash))
                {
                    anyCrash = true;
                }

                if (!string.IsNullOrEmpty(item.Name) && item.Name.Contains("Combo"))
                {
                    BasicWarningHelper.AddSystemWarning($"Disable {item.Name}");
                }

                // List all enabled incompatible plugins
                _ = diagInfo.AppendLine($"{name}");
            }
        }

        // Pulse red if any crash-flagged plugin is enabled, otherwise yellow for general incompatibles
        Vector4 diagColor;
        if (anyCrash)
        {
            // Alpha pulses between ~0.25 and ~0.70 at a comfortable speed
            float t = (float)ImGui.GetTime();
            float pulse = (MathF.Sin(t * 4f) + 1f) * 0.5f; // 0..1
            float alpha = 0.25f + (0.45f * pulse);
            diagColor = new Vector4(1f, 0f, 0f, alpha);
        }
        else
        {
            diagColor = new Vector4(1f, 1f, .4f, .3f);
        }

        ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - 20);
        ImGui.SetCursorPosX(0);

        // Create an invisible button over the area where the InfoMarker will be drawn
        Vector2 markerSize = ImGui.CalcTextSize(FontAwesomeIcon.Cube.ToIconString());
        markerSize.Y = Math.Max(markerSize.Y, ImGui.GetTextLineHeight()); // Ensure height is at least one line

        ImGui.InvisibleButton("##DiagInfoMarkerBtn", new Vector2(ImGui.GetWindowWidth(), markerSize.Y));
        bool clicked = ImGui.IsItemClicked();

        ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - 20);
        ImGui.SetCursorPosX(0);
        ImGuiEx.InfoMarker(diagInfo.ToString(), diagColor, FontAwesomeIcon.Cube.ToIconString(), false);

        // Gold star if Tic-tac-toe win achieved
        if (OtherConfiguration.RotationSolverRecord.TicTacToeWinStar == true)
        {
            ImGui.SameLine();
            using (var starCol = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold))
            {
                ImGuiEx.Icon(FontAwesomeIcon.Star);
            }
            ImguiTooltips.HoveredTooltip("Tic-tac-toe winner!");
        }

        if (clicked)
        {
            ImGui.SetClipboardText(diagInfo.ToString());
        }
    }

    private void DrawSideBar()
    {
        using ImRaii.IEndObject child = ImRaii.Child("Rotation Solver Side bar", -Vector2.One, false, ImGuiWindowFlags.NoScrollbar);
        if (child)
        {
            float wholeWidth = ImGui.GetWindowSize().X;
            DrawHeader(wholeWidth);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            float iconSize = Math.Max(Scale * MIN_COLUMN_WIDTH, Math.Min(wholeWidth, Scale * JOB_ICON_WIDTH)) * 0.6f;
            if (wholeWidth > JOB_ICON_WIDTH * Scale)
            {
                ImGui.SetNextItemWidth(wholeWidth);
                SearchingBox();
                ImGui.Spacing();
            }
            foreach (RotationConfigWindowTab item in Enum.GetValues<RotationConfigWindowTab>())
            {
                // Skip the tab if it has the TabSkipAttribute
                if (_configWindowTabProperties[item].Item1)
                {
                    continue;
                }

                string displayName = item.ToString();
                if (item == RotationConfigWindowTab.Job && Player.Object != null)
                {
                    displayName = Player.Job.ToString(); // Use the current player's job name
                }
                else if (item == RotationConfigWindowTab.Duty && Player.Object != null)
                {
                    if (!DataCenter.IsInDuty || DataCenter.CurrentDutyRotation == null)
                    {
                        continue;
                    }

                    displayName = true switch
                    {
                        var _ when DataCenter.IsInOccultCrescentOp => $"Duty - {DutyRotation.ActivePhantomJob}",
                        var _ when DataCenter.InVariantDungeon => "Duty - Variant",
                        var _ when DataCenter.IsInBozja => "Duty - Bozja",
                        var _ when DataCenter.IsInMonsterHunterDuty => "Duty - Monster Hunter",
                        _ => "Duty",
                    };
                }

                // Reverse the order of these to do the non-interop check first
                if (wholeWidth <= JOB_ICON_WIDTH * Scale && IconSet.GetTexture(_configWindowTabProperties[item].Item2, out IDalamudTextureWrap? icon))
                {
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        Vector2 cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * iconSize, displayName))
                        {
                            _activeTab = item;
                            _searchResults = [];
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, _activeTab == item ? 1 : 0);
                    }, Math.Max(Scale * MIN_COLUMN_WIDTH, wholeWidth), iconSize);

                    string desc = displayName;
                    string addition = item.GetDescription();
                    if (!string.IsNullOrEmpty(addition))
                    {
                        desc += "\n \n" + addition;
                    }

                    ImguiTooltips.HoveredTooltip(desc);
                }
                else
                {
                    if (ImGui.Selectable(displayName, _activeTab == item, ImGuiSelectableFlags.None, new Vector2(0, 20)))
                    {
                        _activeTab = item;
                        _searchResults = [];
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        string desc = item.GetDescription();
                        if (!string.IsNullOrEmpty(desc))
                        {
                            ImguiTooltips.ShowTooltip(desc);
                        }
                    }
                }

                // Add a separator after the "Debug" tab
                if (item == RotationConfigWindowTab.Debug)
                {
                    ImGui.Separator();
                }

                // Add a separator after the "Duty" tab
                if (item == RotationConfigWindowTab.Duty)
                {
                    ImGui.Separator();
                }

                // Add a separator after the "Main" tab
                if (item == RotationConfigWindowTab.Main)
                {
                    ImGui.Separator();
                }
            }
            DrawDiagnosticInfoCube();
            ImGui.Spacing();
        }
    }

    private void DrawHeader(float wholeWidth)
    {
        float size = MathF.Max(MathF.Min(wholeWidth, Scale * 128), Scale * MIN_COLUMN_WIDTH);
        if (IconSet.GetTexture((uint)0, out IDalamudTextureWrap? overlay) && overlay?.Handle != null)
        {
            ImGuiHelper.DrawItemMiddle(() =>
            {
                Vector2 cursor = ImGui.GetCursorPos();
                if (ImGuiHelper.SilenceImageButton(overlay, Vector2.One * size,
                    _activeTab == RotationConfigWindowTab.About, "About Icon"))
                {
                    _activeTab = RotationConfigWindowTab.About;
                    _searchResults = [];
                }

                // Detect long-press on the icon to open the Easter egg window.
                if (ImGui.IsItemActive())
                {
                    double now = ImGui.GetTime();
                    if (_rsrIconPressStart < 0)
                    {
                        _rsrIconPressStart = now;
                    }
                    else if (!_rsrIconTriggered && (now - _rsrIconPressStart) >= RsrIconHoldSeconds)
                    {
                        RotationSolverPlugin.OpenTicTacToe();
                        _rsrIconTriggered = true;
                    }
                }
                else
                {
                    _rsrIconPressStart = -1;
                    _rsrIconTriggered = false;
                }

                ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Punchline.GetDescription());
                if (_logoTexture?.Handle != null)
                {
                    ImGui.SetCursorPos(cursor);
                    ImGui.Image(_logoTexture.Handle, Vector2.One * size);
                }
                else
                {
                    // Retry loading the logo texture in draw (throttled) if not ready at OnOpen
                    if ((DateTime.UtcNow - _lastLogoFetchAttempt).TotalSeconds > 1)
                    {
                        _lastLogoFetchAttempt = DateTime.UtcNow;
                        string logoUrl = $"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Images/Logo.png";
                        if (ThreadLoadImageHandler.TryGetTextureWrap(logoUrl, out IDalamudTextureWrap? logo) && logo?.Handle != null)
                        {
                            _logoTexture = logo;
                            ImGui.SetCursorPos(cursor);
                            ImGui.Image(_logoTexture.Handle, Vector2.One * size);
                        }
                    }
                }
            }, wholeWidth, size);
            ImGui.Spacing();
        }
        ICustomRotation? rotation = DataCenter.CurrentRotation;

        if (rotation == null)
        {
            if (!(Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
                Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
                Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);

                string? text = UiString.ConfigWindow_NoRotation.GetDescription();
                if (string.IsNullOrEmpty(text))
                {
                    PluginLog.Error("UiString.ConfigWindow_NoRotation.GetDescription() returned null or empty.");
                    ImGui.PopStyleColor();
                    return;
                }

                float textWidth = ImGuiHelpers.GetButtonSize(text).X;
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    ImGui.TextWrapped(text);
                }, wholeWidth, textWidth);
                ImGui.PopStyleColor();
                ImguiTooltips.HoveredTooltip("Please update your rotations!");
                return;
            }
            float availableWidth = ImGui.GetContentRegionAvail().X;
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
            ImGui.Text(":(");
            ImGui.PopStyleColor();
            ImGui.PopTextWrapPos();
            return;
        }

        var playerJob = Player.Job;
        ICustomRotation[] rotations = RotationUpdater.GetRotations(playerJob, DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE);

        if (_currentRotation != rotation)
        {
            RotationAttribute? rot = rotation.GetAttributes();
            if (rot == null)
            {
                // Defensive: don't update fields if attributes are missing
                return;
            }
            _currentRotation = rotation;
            _curRotationAttribute = rot;
        }

        // Defensive: ensure _curRotationAttribute is not null
        var curAttr = _curRotationAttribute ?? new RotationAttribute("Unknown", CombatType.PvE);

        float iconSize = Math.Max(Scale * MIN_COLUMN_WIDTH, Math.Min(wholeWidth, Scale * JOB_ICON_WIDTH));
        float comboSize = ImGui.CalcTextSize(curAttr.Name ?? string.Empty).X;

        ImGuiHelper.DrawItemMiddle(() =>
        {
            DrawRotationIcon(rotation, iconSize);
        }, wholeWidth, iconSize);

        if (Scale * JOB_ICON_WIDTH < wholeWidth)
        {
            DrawRotationCombo(comboSize, rotations, rotation);
        }
    }
    private static readonly string[] pairsArray = ["Delete"];
    private static readonly string[] pairs = ["Delete"];

    private void DrawRotationIcon(ICustomRotation? rotation, float iconSize)
    {
        if (rotation == null)
            return;

        Vector2 cursor = ImGui.GetCursorPos();

        if (!rotation.GetTexture(out IDalamudTextureWrap? jobIcon) || jobIcon?.Handle == null)
            return;

        if (ImGuiHelper.SilenceImageButton(jobIcon, Vector2.One * iconSize, _activeTab == RotationConfigWindowTab.Rotation))
        {
            _activeTab = RotationConfigWindowTab.Rotation;
            _searchResults = [];
        }

        if (ImGui.IsItemHovered())
        {
            ImguiTooltips.ShowTooltip(() =>
            {
                ImGui.TextColored(rotation.GetColor(), $"{rotation.Name ?? string.Empty} ({_curRotationAttribute?.Name ?? string.Empty})");
                _curRotationAttribute?.Type.Draw();

                if (!string.IsNullOrEmpty(rotation.Description))
                {
                    ImGui.Text(rotation.Description);
                }
            });
        }

        IDalamudTextureWrap? overlayTexture = null;
        if (!DataCenter.IsInOccultCrescentOp || DutyRotation.GetPhantomJob() == DutyRotation.PhantomJob.None)
        {
            var curCombatType = DataCenter.IsPvP ? CombatType.PvP : CombatType.PvE;
            IconSet.GetTexture(curCombatType.GetIcon(), out overlayTexture);
        }
        else
        {
            overlayTexture = IconSet.GetOccultIcon();
        }

        if (overlayTexture?.Handle != null)
        {
            ImGui.SetCursorPos(cursor + (Vector2.One * iconSize / 2));
            ImGui.Image(overlayTexture.Handle, Vector2.One * iconSize / 2);
        }
    }

    private void DrawRotationCombo(float comboSize, ICustomRotation[] rotations, ICustomRotation rotation)
    {
        ImGui.SetNextItemWidth(comboSize);
        const string popUp = "Rotation Solver Select Rotation";
        var rotationColor = rotation.GetColor();
        using (ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, rotation.IsExtra() ? ImGuiColors.DalamudViolet : ImGuiColors.DalamudWhite))
        {
            if (ImGui.Selectable(_curRotationAttribute.Name + "##RotationName:" + rotation.Name))
            {
                if (!ImGui.IsPopupOpen(popUp))
                {
                    ImGui.OpenPopup(popUp);
                }
            }
        }
        using (ImRaii.IEndObject popup = ImRaii.Popup(popUp))
        {
            if (popup)
            {
                foreach (ICustomRotation r in rotations)
                {
                    RotationAttribute? rAttr = r.GetAttributes();
                    if (rAttr == null)
                    {
                        continue;
                    }

                    if (IconSet.GetTexture(rAttr.Type.GetIcon(), out IDalamudTextureWrap? texture))
                    {
                        if (texture?.Handle != null)
                        {
                            ImGui.Image(texture.Handle, Vector2.One * 20 * Scale);
                            if (ImGui.IsItemHovered())
                            {
                                ImguiTooltips.ShowTooltip(() =>
                                {
                                    rAttr.Type.Draw();
                                });
                            }
                        }
                    }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, r.IsExtra() ? ImGuiColors.DalamudViolet : ImGuiColors.DalamudWhite);

                    if (ImGui.Selectable(rAttr.Name))
                    {
                        if (DataCenter.IsPvP)
                        {
                            Service.Config.PvPRotationChoice = r.GetType().FullName;
                        }
                        else
                        {
                            Service.Config.RotationChoice = r.GetType().FullName;
                        }
                        Service.Config.Save();
                        RotationUpdater.ChangeRotation(r);
                    }
                    ImguiTooltips.HoveredTooltip(rAttr.Description);
                    ImGui.PopStyleColor();
                }
            }
        }

        string warning = "Game version: " + _curRotationAttribute.GameVersion;
        warning += "\n \n" + UiString.ConfigWindow_Helper_SwitchRotation.GetDescription();
        ImguiTooltips.HoveredTooltip(warning);
    }

    // Hint bar at the top of the body
    private void DrawHintsBar()
    {
        if (CheckErrors())
        {
            string errorText = string.Empty;
            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically

            if (Watcher.DalamudBranch() != "release")
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth);
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
                ImGui.TextWrapped($"Warning: You are running the '{Watcher.DalamudBranch()}' branch of Dalamud. For best compatibility, use /xlbranch and switch back to 'release' branch if available for your current version of FFXIV.");
                ImGui.PopStyleColor();
                ImGui.PopTextWrapPos();
                ImGui.Spacing();
            }

            if (_crashPlugins.Count > 0 && _crashPlugins[0].Name != null)
            {
                errorText = $"Disable {_crashPlugins[0].Name}, can cause conflicts/crashes.";
            }
            else if (Player.AvailableThreadSafe && (Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
                    Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
                    Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN))
            {
                errorText = $"You are on an unsupported class: {Player.Job}";
            }

            if (DataCenter.SystemWarnings != null && DataCenter.SystemWarnings.Count != 0)
            {
                List<string> warningsToRemove = [];

                foreach (string warning in DataCenter.SystemWarnings.Keys)
                {
                    using ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange));
                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically

                    // Calculate the required height for the button
                    Vector2 textSize = ImGui.CalcTextSize(warning, false, availableWidth);
                    float buttonHeight = textSize.Y + (ImGui.GetStyle().FramePadding.Y * 2);
                    float lineHeight = ImGui.GetTextLineHeight();
                    int lineCount = (int)Math.Ceiling(textSize.X / availableWidth);

                    if (ImGui.Button(warning, new Vector2(availableWidth, buttonHeight)))
                    {
                        warningsToRemove.Add(warning);
                    }

                    ImGui.PopTextWrapPos(); // Reset text wrapping position
                }

                // Remove warnings that were cleared
                foreach (string warning in warningsToRemove)
                {
                    _ = DataCenter.SystemWarnings.Remove(warning);
                }
            }

            if (errorText != string.Empty)
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed); // Set text color to DalamudOrange
                ImGui.Text(errorText);
                ImGui.PopStyleColor(); // Reset text color
                ImGui.PopTextWrapPos(); // Reset text wrapping position
            }

            ImGui.Separator();
            ImGui.Spacing();
        }

        if (!Service.Config.ShowHints)
        {
            return;
        }

        if (_usageHints == null || _usageHints.Length == 0)
        {
            return;
        }

        float now = (float)ImGui.GetTime();
        if (now - _lastHintSwitch >= 7f)
        {
            _lastHintSwitch = now;
            int next;
            do
            {
                next = _hintRng.Next(_usageHints.Length);
            } while (next == _hintIndex && _usageHints.Length > 1);
            _hintIndex = next;
        }

        using (ImRaii.Font _ = ImRaii.PushFont(FontManager.GetFont(12)))
        using (ImRaii.Color __ = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow)))
        {
            float avail = ImGui.GetContentRegionAvail().X;
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + avail);
            ImGui.TextWrapped($"Tip: {_usageHints[_hintIndex]}");
            ImGui.PopTextWrapPos();
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawBody()
    {
        // Adjust cursor position
        ImGui.SetCursorPos(ImGui.GetCursorPos() + (Vector2.One * 8 * Scale));

        // Create a child window for the body content
        using ImRaii.IEndObject child = ImRaii.Child("Rotation Solver Body", -Vector2.One);
        if (child)
        {
            // Hints bar at the top of the body (hide when search is active)
            if (_searchResults == null || _searchResults.Length == 0)
            {
                DrawHintsBar();
            }

            // Check if there are search results to display
            if (_searchResults != null && _searchResults.Length != 0)
            {
                // Display search results header
                using (ImRaii.Font font = ImRaii.PushFont(FontManager.GetFont(18)))
                {
                    using ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
                    ImGui.TextWrapped(UiString.ConfigWindow_Search_Result.GetDescription());
                }

                ImGui.Spacing();

                // Display each search result
                foreach (ISearchable searchable in _searchResults)
                {
                    if (searchable == null)
                    {
                        continue;
                    }

                    searchable.Draw();

                    // Offer a way to jump to the menu where this item resides
                    if (searchable is Searchable s)
                    {
                        string filter = s.Filter;
                        if (!string.IsNullOrEmpty(filter))
                        {
                            ImGui.SameLine();
                            string btnId = $"##JumpToMenu_{s.ID}_{s.GetHashCode()}";
                            if (ImGuiEx.IconButton(FontAwesomeIcon.ExternalLinkAlt, btnId))
                            {
                                NavigateToFilter(filter);
                                _searchResults = [];
                            }

                            string path = GetFilterMenuPath(filter);
                            if (!string.IsNullOrEmpty(path))
                            {
                                ImguiTooltips.HoveredTooltip($"Open: {path}");
                            }
                        }
                    }
                }
            }
            else
            {
                // Display content based on the active tab
                switch (_activeTab)
                {

                    case RotationConfigWindowTab.Main:
                        DrawAbout();
                        break;

                    case RotationConfigWindowTab.Duty:
                        DrawDutyRotationBody();
                        break;

                    case RotationConfigWindowTab.Job:
                        DrawRotation();
                        break;

                    case RotationConfigWindowTab.AutoDuty:
                        DrawAutoduty();
                        break;

                    case RotationConfigWindowTab.About:
                        DrawAbout();
                        break;

                    case RotationConfigWindowTab.Rotation:
                        DrawRotation();
                        break;

                    case RotationConfigWindowTab.Actions:
                        DrawActions();
                        break;

                    case RotationConfigWindowTab.List:
                        DrawList();
                        break;

                    case RotationConfigWindowTab.Basic:
                        DrawBasic();
                        break;

                    case RotationConfigWindowTab.UI:
                        DrawUI();
                        break;

                    case RotationConfigWindowTab.Auto:
                        DrawAuto();
                        break;

                    case RotationConfigWindowTab.Target:
                        DrawTarget();
                        break;

                    case RotationConfigWindowTab.Extra:
                        DrawExtra();
                        break;

                    case RotationConfigWindowTab.Debug:
                        DrawDebug();
                        break;

                    default:
                        // Handle unexpected tab values
                        ImGui.Text("Unknown tab selected.");
                        break;
                }
            }
        }
    }

    private static string GetFilterMenuPath(string filter)
    {
        // Build a human-friendly path like "Auto > Action usage"
        return filter switch
        {
            Configs.BasicTimer => $"Basic > {UiString.ConfigWindow_Basic_Timer.GetDescription()}",
            Configs.BasicParams => $"Basic > {UiString.ConfigWindow_Basic_Others.GetDescription()}",

            Configs.UiInformation => $"UI > {UiString.ConfigWindow_UI_Information.GetDescription()}",
            Configs.UiWindows => $"UI > {UiString.ConfigWindow_UI_Windows.GetDescription()}",

            Configs.BasicAutoSwitch => $"Auto > {UiString.ConfigWindow_Basic_AutoSwitch.GetDescription()}",
            Configs.AutoActionUsage => $"Auto > {UiString.ConfigWindow_Auto_ActionUsage.GetDescription()}",
            Configs.HealingActionCondition => $"Auto > {UiString.ConfigWindow_Auto_HealingCondition.GetDescription()}",
            Configs.PvPSpecificControls => $"Auto > {UiString.ConfigWindow_Auto_PvPSpecific.GetDescription()}",

            Configs.TargetConfig => $"Target > {UiString.ConfigWindow_Target_Config.GetDescription()}",

            Configs.Extra => $"Extra > {UiString.ConfigWindow_Extra_Others.GetDescription()}",

            Configs.List => $"List > {UiString.ConfigWindow_List_Actions.GetDescription()}",
            Configs.List2 => $"List > {UiString.ConfigWindow_List_Actions.GetDescription()}",
            Configs.List3 => $"List > {UiString.ConfigWindow_List_Actions.GetDescription()}",

            Configs.Debug => $"Debug",

            _ => string.Empty,
        };
    }

    private void NavigateToFilter(string filter)
    {
        switch (filter)
        {
            case Configs.BasicTimer:
                _activeTab = RotationConfigWindowTab.Basic;
                _baseHeader.OpenHeaderByTitle(UiString.ConfigWindow_Basic_Timer.GetDescription());
                break;
            case Configs.BasicParams:
                _activeTab = RotationConfigWindowTab.Basic;
                _baseHeader.OpenHeaderByTitle(UiString.ConfigWindow_Basic_Others.GetDescription());
                break;

            case Configs.UiInformation:
                _activeTab = RotationConfigWindowTab.UI;
                _UIHeader.OpenHeaderByTitle(UiString.ConfigWindow_UI_Information.GetDescription());
                break;
            case Configs.UiWindows:
                _activeTab = RotationConfigWindowTab.UI;
                _UIHeader.OpenHeaderByTitle(UiString.ConfigWindow_UI_Windows.GetDescription());
                break;

            case Configs.BasicAutoSwitch:
                _activeTab = RotationConfigWindowTab.Auto;
                _autoHeader.OpenHeaderByTitle(UiString.ConfigWindow_Basic_AutoSwitch.GetDescription());
                break;
            case Configs.AutoActionUsage:
                _activeTab = RotationConfigWindowTab.Auto;
                _autoHeader.OpenHeaderByTitle(UiString.ConfigWindow_Auto_ActionUsage.GetDescription());
                break;
            case Configs.HealingActionCondition:
                _activeTab = RotationConfigWindowTab.Auto;
                _autoHeader.OpenHeaderByTitle(UiString.ConfigWindow_Auto_HealingCondition.GetDescription());
                break;
            case Configs.PvPSpecificControls:
                _activeTab = RotationConfigWindowTab.Auto;
                _autoHeader.OpenHeaderByTitle(UiString.ConfigWindow_Auto_PvPSpecific.GetDescription());
                break;

            case Configs.TargetConfig:
                _activeTab = RotationConfigWindowTab.Target;
                _targetHeader.OpenHeaderByTitle(UiString.ConfigWindow_Target_Config.GetDescription());
                break;

            case Configs.Extra:
                _activeTab = RotationConfigWindowTab.Extra;
                _extraHeader.OpenHeaderByTitle(UiString.ConfigWindow_Extra_Others.GetDescription());
                break;

            case Configs.List:
            case Configs.List2:
            case Configs.List3:
                _activeTab = RotationConfigWindowTab.List;
                _idsHeader.OpenHeaderByTitle(UiString.ConfigWindow_List_Actions.GetDescription());
                break;

            case Configs.Debug:
                _activeTab = RotationConfigWindowTab.Debug;
                break;
        }
    }

    #region DutyRotation
    private static void DrawDutyRotationBody()
    {
        DutyRotation? rotation = DataCenter.CurrentDutyRotation;
        if (rotation == null)
            return;

        _dutyRotationHeader.Draw();
    }

    private static readonly CollapsingHeaderGroup _dutyRotationHeader = new(new()
    {
        { GetDutyRotationStatusHead,  DrawDutyRotationStatus },

        { UiString.ConfigWindow_DutyRotation_Configuration.GetDescription, DrawDutyRotationConfiguration }
    });

    private static string GetDutyRotationStatusHead()
    {
        DutyRotation? rotation = DataCenter.CurrentDutyRotation;
        string status = UiString.ConfigWindow_DutyRotation_Status.GetDescription();
        return rotation == null ? string.Empty : status;
    }

    private static void DrawDutyRotationStatus()
    {
        if (DataCenter.CurrentDutyRotation == null)
        {
            return;
        }
        DataCenter.CurrentDutyRotation?.DisplayDutyStatus();
    }

    private static void DrawDutyRotationConfiguration()
    {
        DutyRotation? rotation = DataCenter.CurrentDutyRotation;
        if (rotation == null) return;
        if (!Player.AvailableThreadSafe) return;

        IRotationConfigSet set = rotation.Configs;

        bool hasAny = false;
        foreach (var _ in set.Configs) { hasAny = true; break; }
        if (hasAny) ImGui.Separator();

        foreach (IRotationConfig config in set.Configs)
        {
            if (!config.Type.HasFlag(CombatType.PvE)) continue;
            if (!ShouldShowRotationConfig(config, set)) continue;

            string key = rotation.GetType().FullName ?? rotation.GetType().Name + "." + config.Name;
            string name = $"##{config.GetHashCode()}_{key}.Name";
            string command = ToCommandStr(OtherCommandType.DutyRotations, config.Name, config.DefaultValue);
            void Reset() => config.Value = config.DefaultValue;

            ImGuiHelper.PrepareGroup(key, command, Reset);

            DutyRotation.PhantomJob phantomJob = DutyRotation.GetPhantomJob();

            if (config is RotationConfigCombo c)
            {
                if (c.PhantomJob != DutyRotation.PhantomJob.None && c.PhantomJob != phantomJob) continue;
                string[] names = c.DisplayValues;
                string selectedValue = c.Value;
                int index = -1;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].Equals(selectedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1) index = 0;
                string longest = "";
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].Length > longest.Length) longest = names[i];
                }
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(longest).X + (50 * Scale));
                if (ImGui.Combo(name, ref index, names, names.Length))
                {
                    c.Value = names[index];
                }
            }
            else if (config is RotationConfigBoolean b)
            {
                if (b.PhantomJob != DutyRotation.PhantomJob.None && b.PhantomJob != phantomJob) continue;
                if (bool.TryParse(config.Value, out bool val))
                {
                    if (ImGui.Checkbox(name, ref val))
                    {
                        config.Value = val.ToString();
                    }
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else if (config is RotationConfigFloat f)
            {
                if (f.PhantomJob != DutyRotation.PhantomJob.None && f.PhantomJob != phantomJob) continue;
                if (float.TryParse(config.Value, out float val))
                {
                    ImGui.SetNextItemWidth(Scale * Searchable.DRAG_WIDTH);
                    if (f.UnitType == ConfigUnitType.Percent)
                    {
                        float displayValue = val * 100;
                        if (ImGui.SliderFloat(name, ref displayValue, f.Min * 100, f.Max * 100, $"{displayValue:F1}{f.UnitType.ToSymbol()}"))
                        {
                            config.Value = (displayValue / 100).ToString();
                        }
                    }
                    else
                    {
                        if (ImGui.DragFloat(name, ref val, f.Speed, f.Min, f.Max, $"{val:F2}{f.UnitType.ToSymbol()}"))
                        {
                            config.Value = val.ToString();
                        }
                    }
                    ImguiTooltips.HoveredTooltip(f.UnitType.GetDescription());
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else if (config is RotationConfigString s)
            {
                if (s.PhantomJob != DutyRotation.PhantomJob.None && s.PhantomJob != phantomJob) continue;
                string val = config.Value;
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth());
                if (ImGui.InputTextWithHint(name, config.DisplayName, ref val, 128))
                {
                    config.Value = val;
                }
                ImGuiHelper.ReactPopup(key, command, Reset);
                continue;
            }
            else if (config is RotationConfigInt i)
            {
                if (i.PhantomJob != DutyRotation.PhantomJob.None && i.PhantomJob != phantomJob) continue;
                if (int.TryParse(config.Value, out int val))
                {
                    ImGui.SetNextItemWidth(Scale * Searchable.DRAG_WIDTH);
                    if (ImGui.DragInt(name, ref val, i.Speed, i.Min, i.Max))
                    {
                        config.Value = val.ToString();
                    }
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else
            {
                continue;
            }

            ImGui.SameLine();
            ImGui.TextWrapped($"{config.DisplayName}");
            ImGuiHelper.ReactPopup(key, command, Reset, false);
        }
    }
    #endregion

    #region About
    private static void DrawAbout()
    {
        // Draw the punchline with a specific font and color
        using (ImRaii.Font font = ImRaii.PushFont(FontManager.GetFont(18)))
        {
            using ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
            ImGui.TextWrapped(UiString.ConfigWindow_About_Punchline.GetDescription());
        }

        ImGui.Spacing();

        // Draw the description
        ImGui.TextWrapped(UiString.ConfigWindow_About_Description.GetDescription());

        ImGui.Spacing();

        // Draw the warning with a specific color
        using (ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange)))
        {
            ImGui.TextWrapped(UiString.ConfigWindow_About_Warning.GetDescription());
        }

        ImGui.Spacing();
        float width2 = ImGui.GetWindowWidth();
        if (IconSet.GetTexture("https://storage.ko-fi.com/cdn/brandasset/kofi_button_red.png", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon2) && ImGuiHelper.TextureButton(icon2, width2, 250 * Scale, "Ko-fi link"))
        {
            Util.OpenLink("https://ko-fi.com/ltscombatreborn");
        }

        float width = ImGui.GetWindowWidth();

        // Draw the Discord link button
        if (IconSet.GetTexture("https://discordapp.com/api/guilds/1064448004498653245/embed.png?style=banner2", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) && ImGuiHelper.TextureButton(icon, width, 250 * Scale, "Discord link"))
        {
            Util.OpenLink("https://discord.gg/p54TZMPnC9");
        }

        uint clickingCount = OtherConfiguration.RotationSolverRecord.ClickingCount;
        if (clickingCount > 0)
        {
            // Draw the clicking count with a specific color
            using ImRaii.Color color = ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.2f, 0.6f, 0.95f, 1));
            string countStr = UiString.ConfigWindow_About_ClickingCount.GetDescription();
            if (countStr != null)
            {
                countStr = string.Format(countStr, clickingCount);
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    ImGui.TextWrapped(countStr);
                }, width, ImGui.CalcTextSize(countStr).X);
            }
        }

        // Draw the about headers
        _aboutHeaders.Draw();
    }

    private static readonly CollapsingHeaderGroup _aboutHeaders = new(new()
    {
        { UiString.ConfigWindow_About_Macros.GetDescription, DrawAboutMacros },
        { UiString.ConfigWindow_About_SettingMacros.GetDescription, DrawAboutSettingsCommands },
        { UiString.ConfigWindow_About_Compatibility.GetDescription, DrawAboutCompatibility },
        { UiString.ConfigWindow_About_Links.GetDescription, DrawAboutLinks },
    });

    private static void DrawAboutMacros()
    {
        // Adjust item spacing for better layout
        using ImRaii.Style style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));

        // Display command help for different state commands
        DisplayCommandHelp(StateCommandType.Auto);
        DisplayCommandHelp(StateCommandType.Manual);
        DisplayCommandHelp(StateCommandType.Off);
        DisplayCommandHelp(OtherCommandType.Cycle);
        DisplayCommandHelp(StateCommandType.TargetOnly);
        ImGui.NewLine();

        // Display command help for other commands
        DisplayCommandHelp(OtherCommandType.NextAction);

        ImGui.NewLine();

        // Display command help for special commands
        DisplayCommandHelp(SpecialCommandType.EndSpecial);
        DisplayCommandHelp(SpecialCommandType.HealArea);
        DisplayCommandHelp(SpecialCommandType.HealSingle);
        DisplayCommandHelp(SpecialCommandType.DefenseArea);
        DisplayCommandHelp(SpecialCommandType.DefenseSingle);
        DisplayCommandHelp(SpecialCommandType.MoveForward);
        DisplayCommandHelp(SpecialCommandType.MoveBack);
        DisplayCommandHelp(SpecialCommandType.Speed);
        DisplayCommandHelp(SpecialCommandType.DispelStancePositional);
        DisplayCommandHelp(SpecialCommandType.RaiseShirk);
        DisplayCommandHelp(SpecialCommandType.AntiKnockback);
        DisplayCommandHelp(SpecialCommandType.Burst);
        DisplayCommandHelp(SpecialCommandType.NoCasting);
    }

    private static void DrawAboutSettingsCommands()
    {
        // Adjust item spacing for better layout
        using ImRaii.Style style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));
        ImGui.NewLine();
        ImGui.TextWrapped("These commands can be used to open or change plugin settings directly from chat or macros.");
        ImGui.NewLine();
        ImGui.TextWrapped("Simply right clicking any action, setting, or toggle will pop up the macro associated with it.");
    }

    // Helper method to display command help
    private static void DisplayCommandHelp<T>(T commandType) where T : Enum
    {
        commandType.DisplayCommandHelp(getHelp: Data.EnumExtensions.GetDescription);
    }

    private static void DrawAboutCompatibility()
    {
        // Display the compatibility description
        ImGui.TextWrapped(UiString.ConfigWindow_About_Compatibility_Description.GetDescription());

        ImGui.Spacing();

        float iconSize = 40 * Scale;

        // Create a table to display incompatible plugins
        using ImRaii.IEndObject table = ImRaii.Table("Incompatible plugin", 5, ImGuiTableFlags.BordersInner
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            // Set up table headers
            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("Name");

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("Icon/Link");

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("Features");

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("Type");

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader("Enabled");

            // Ensure that IncompatiblePlugins is not null
            IncompatiblePlugin[] incompatiblePlugins = DownloadHelper.IncompatiblePlugins ?? [];

            // Iterate over each incompatible plugin and display its details
            foreach (IncompatiblePlugin item in incompatiblePlugins)
            {
                ImGui.TableNextRow();
                _ = ImGui.TableNextColumn();

                ImGui.Text(item.Name);

                _ = ImGui.TableNextColumn();

                string icon = string.IsNullOrEmpty(item.Icon)
                    ? "https://raw.githubusercontent.com/goatcorp/DalamudAssets/master/UIRes/defaultIcon.png"
                    : item.Icon;

                if (IconSet.GetTexture(icon, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture))
                {
                    if (ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * iconSize))
                    {
                        Util.OpenLink(item.Url);
                    }
                }

                _ = ImGui.TableNextColumn();
                ImGui.TextWrapped(item.Features);

                _ = ImGui.TableNextColumn();
                DisplayPluginType(item.Type);

                _ = ImGui.TableNextColumn();
                ImGui.Text(item.IsEnabled ? "Yes" : "No");
            }
        }
    }

    // Helper method to display plugin type with appropriate colors and tooltips
    private static void DisplayPluginType(CompatibleType type)
    {
        if (type.HasFlag(CompatibleType.Skill_Usage))
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, CompatibleType.Skill_Usage.GetDescription().Replace('_', ' '));
            ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Compatibility_Mistake.GetDescription());
        }
        if (type.HasFlag(CompatibleType.Skill_Selection))
        {
            ImGui.TextColored(ImGuiColors.DalamudOrange, CompatibleType.Skill_Selection.GetDescription().Replace('_', ' '));
            ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Compatibility_Mislead.GetDescription());
        }
        if (type.HasFlag(CompatibleType.Crash))
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, CompatibleType.Crash.GetDescription().Replace('_', ' '));
            ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Compatibility_Crash.GetDescription());
        }
        if (type.HasFlag(CompatibleType.Broken))
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, CompatibleType.Broken.GetDescription().Replace('_', ' '));
            ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Compatibility_Crash.GetDescription());
        }
    }

    private static void DrawAboutLinks()
    {
        float width = ImGui.GetWindowWidth();

        ImGui.Spacing();

        // Display button to open the configuration folder
        string text = UiString.ConfigWindow_About_OpenConfigFolder.GetDescription();
        float textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            if (ImGui.Button(text))
            {
                try
                {
                    _ = Process.Start("explorer.exe", Svc.PluginInterface.ConfigDirectory.FullName);
                }
                catch (Exception ex)
                {
                    // Handle the exception (e.g., log it or display an error message)
                    ImGui.TextColored(ImGuiColors.DalamudRed, $"Failed to open config folder: {ex.Message}");
                }
            }
        }, width, textWidth);

        ImGui.Spacing();
        // Display GitHub link button
        if (IconSet.GetTexture("https://GitHub-readme-stats.vercel.app/api/pin/?username=FFXIV-CombatReborn&repo=RotationSolverReborn&show_icons=true&theme=dark", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon))
        {
            if (ImGuiHelper.TextureButton(icon, width, width))
            {
                Util.OpenLink($"https://GitHub.com/{Service.USERNAME}/{Service.REPO}");
            }
        }
        else
        {
            // Handle the case where the texture is not found
            ImGui.Text("Failed to load GitHub icon.");
        }
    }
    #endregion

    #region Autoduty

    private void DrawAutoduty()
    {
        ImGui.TextWrapped("While the RSR Team has made effort to make RSR compatible with Autoduty, please keep in mind that RSR is not designed with botting in mind.");
        ImGui.Spacing();
        ImGui.TextWrapped("This menu is for troubleshooting and initial setup purposes and is a good first step to share to get assistance.");
        ImGui.Spacing();
        ImGui.TextWrapped("Below are relevant settings and their current states for RSR to work well with AutoDuty mode.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        // Display the current HostileType
        ImGui.TextWrapped($"Current Targeting Mode: {GetHostileTypeDescription(DataCenter.CurrentTargetToHostileType)}");

        // Add a button to change the targeting to AllTargetsCanAttack (type 0) aka Autoduty Mode
        if (ImGui.Button("Change Targeting to Autoduty Mode"))
        {
            SetTargetingType(TargetHostileType.AllTargetsCanAttack);
        }

        // Display the current NPC Heal/Raise Support status
        ImGui.TextWrapped($"NPC Heal/Raise Support Enabled: {Service.Config.FriendlyPartyNpcHealRaise3}");
        if (ImGui.Button("Enable NPC Heal/Raise Support"))
        {
            Service.Config.FriendlyPartyNpcHealRaise3.Value = true;
        }
        ImGui.Spacing();
        // Display the Auto Off Between Area status
        ImGui.TextWrapped($"Auto Off Between Areas: {Service.Config.AutoOffBetweenArea}");
        if (ImGui.Button("Disable Auto Off Between Areas"))
        {
            Service.Config.AutoOffBetweenArea.Value = false;
        }
        ImGui.Spacing();
        // Display the Auto Off Cut Scene status
        ImGui.TextWrapped($"Auto Off During Cutscenes: {Service.Config.AutoOffCutScene}");
        if (ImGui.Button("Disable Auto Off During Cutscenes"))
        {
            Service.Config.AutoOffCutScene.Value = false;
        }
        ImGui.Spacing();
        // Display the Auto Off After Combat Time status
        ImGui.TextWrapped($"Auto Off After Combat: {Service.Config.AutoOffAfterCombat}");
        if (ImGui.Button("Disable Auto Off After Combat"))
        {
            Service.Config.AutoOffAfterCombat.Value = false;
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextWrapped($"Below are plugins used by Autoduty and their current states");
        ImGui.Spacing();

        // Create a new list of AutoDutyPlugin objects
        List<AutoDutyPlugin> pluginsToCheck =
        [
            new AutoDutyPlugin { Name = "AutoDuty", Url = "https://puni.sh/api/repository/erdelf" },
            new AutoDutyPlugin { Name = "vnavmesh", Url = "https://puni.sh/api/repository/veyn" },
            new AutoDutyPlugin { Name = "BossModReborn", Url = "https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json" },
            new AutoDutyPlugin { Name = "Boss Mod", Url = "https://puni.sh/api/repository/veyn" },
            new AutoDutyPlugin { Name = "Avarice", Url = "https://love.puni.sh/ment.json" },
            new AutoDutyPlugin { Name = "AutoRetainer", Url = "https://love.puni.sh/ment.json" },
            new AutoDutyPlugin { Name = "SkipCutscene", Url = "https://raw.githubusercontent.com/KangasZ/DalamudPluginRepository/main/plugin_repository.json" },
            new AutoDutyPlugin { Name = "AntiAfkKick", Url = "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json" },
            new AutoDutyPlugin { Name = "Gearsetter", Url = "https://puni.sh/api/repository/vera" },
        ];

        // Check if "Boss Mod" and "BossMod Reborn" are enabled
        bool isBossModEnabled = pluginsToCheck.Any(plugin => plugin.Name == "Boss Mod" && plugin.IsEnabled);
        bool isBossModRebornEnabled = pluginsToCheck.Any(plugin => plugin.Name == "BossModReborn" && plugin.IsEnabled);

        // Iterate through the list and check if each plugin is installed and enabled
        foreach (AutoDutyPlugin plugin in pluginsToCheck)
        {
            // Only display information about "Boss Mod" if it is installed and enabled
            if (plugin.Name == "Boss Mod" && !isBossModEnabled)
            {
                continue;
            }

            bool isEnabled = plugin.IsEnabled;
            bool isInstalled = plugin.IsInstalled;

            // Add a button to copy the URL to the clipboard if the plugin is not installed
            if (!isEnabled)
            {
                if (DalamudReflector.HasRepo(plugin.Url) && !isInstalled)
                {
                    if (ImGui.Button($"Add Plugin##{plugin.Name}"))
                    {
                        PluginLog.Information($"Attempting to add plugin: {plugin.Name} from URL: {plugin.Url}");
                        _ = DalamudReflector.AddPlugin(plugin.Url, plugin.Name).ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully && t.Result)
                            {
                                PluginLog.Information($"Successfully added plugin: {plugin.Name} from URL: {plugin.Url}");
                            }
                            else
                            {
                                PluginLog.Error($"Failed to add plugin: {plugin.Name} from URL: {plugin.Url}");
                            }
                            // Refresh plugin masters after install
                            DalamudReflector.ReloadPluginMasters();
                        });
                    }
                    ImGui.SameLine();
                }
                else if (!DalamudReflector.HasRepo(plugin.Url))
                {
                    if (ImGui.Button($"Add Repo##{plugin.Name}"))
                    {
                        PluginLog.Information($"Attempting to add repository: {plugin.Url}");
                        DalamudReflector.AddRepo(plugin.Url, true);
                        DalamudReflector.ReloadPluginMasters();
                        PluginLog.Information($"Successfully added repository: {plugin.Url}");
                    }
                    ImGui.SameLine();
                }
            }

            // Determine the color and text for "Boss Mod"
            Vector4 color;
            string text;
            if (plugin.Name == "Boss Mod" && isBossModEnabled && isBossModRebornEnabled)
            {
                color = ImGuiColors.DalamudYellow;
                text = $"{plugin.Name} is {(isEnabled ? "installed and enabled" : "not enabled")}. Both Boss Mods cannot be installed and enabled at the same time. Please disable Boss Mod.";
            }
            else if (plugin.Name == "Boss Mod" && isBossModEnabled && !isBossModRebornEnabled)
            {
                color = isEnabled ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudRed;
                text = $"{plugin.Name} is {(isEnabled ? "installed and enabled" : "not enabled")}. Please use BossModReborn instead, BMR has specific integration with RSR that improves RSRs ability to react to combat i.e. Gaze effects.";
            }
            else if (plugin.Name == "BossModReborn" && isBossModRebornEnabled && !isBossModEnabled)
            {
                color = isEnabled ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                text = $"{plugin.Name} is {(isEnabled ? "installed and enabled" : "not enabled")}.";
            }
            else
            {
                color = isEnabled ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                text = $"{plugin.Name} is {(isEnabled ? "installed and enabled" : "not enabled")}";
            }

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextWrapped(text);
            ImGui.PopStyleColor();

            ImGui.Spacing();
        }
    }

    private string GetHostileTypeDescription(TargetHostileType type)
    {
        return type switch
        {
            TargetHostileType.AllTargetsCanAttack => "All Targets Can Attack aka Tank/Autoduty Mode",
            TargetHostileType.TargetsHaveTarget => "Targets Have A Target",
            TargetHostileType.AllTargetsWhenSoloInDuty => "All Targets When Solo In Duty",
            TargetHostileType.AllTargetsWhenSolo => "All Targets When Solo",
            _ => "Unknown Target Type"
        };
    }

    // Method to set the targeting type
    private void SetTargetingType(TargetHostileType type)
    {
        Service.Config.HostileType = type;
        // Add any additional logic needed when changing the targeting type
        PluginLog.Information($"Targeting type changed to: {type}");
    }

    #endregion

    #region Rotation
    private static void DrawRotation()
    {
        ICustomRotation? rotation = DataCenter.CurrentRotation;
        if (rotation == null)
        {
            return;
        }

        string desc = rotation.Description;
        if (!string.IsNullOrEmpty(desc))
        {
            using ImRaii.Font font = ImRaii.PushFont(FontManager.GetFont(15));
            ImGuiEx.TextWrappedCopy(desc);
        }

        _ = ImGui.GetWindowWidth();
        _ = rotation.GetType();

        _rotationHeader.Draw();
    }

    private static uint ChangeAlpha(uint color)
    {
        Vector4 c = ImGui.ColorConvertU32ToFloat4(color);
        c.W = 0.55f;
        return ImGui.ColorConvertFloat4ToU32(c);
    }

    private static readonly CollapsingHeaderGroup _rotationHeader = new(new()
    {
        { UiString.ConfigWindow_Rotation_Description.GetDescription, DrawRotationDescription },

        { GetRotationStatusHead,  DrawRotationStatus },

        { UiString.ConfigWindow_Rotation_Configuration.GetDescription, DrawRotationConfiguration },
    });

    private const float DESC_SIZE = 24;
    private static void DrawRotationDescription()
    {
        ICustomRotation? rotation = DataCenter.CurrentRotation;
        if (rotation == null)
        {
            return;
        }

        _ = ImGui.GetWindowWidth();
        Type type = rotation.GetType();

        List<RotationDescAttribute?> attrs = [RotationDescAttribute.MergeToOne(type.GetCustomAttributes<RotationDescAttribute>())];

        foreach (MethodInfo m in type.GetAllMethodInfo())
        {
            attrs.Add(RotationDescAttribute.MergeToOne(m.GetCustomAttributes<RotationDescAttribute>()));
        }

        using ImRaii.IEndObject table = ImRaii.Table("Rotation Description", 2, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp);
        if (table)
        {
            foreach (RotationDescAttribute[] a in RotationDescAttribute.Merge(attrs))
            {
                RotationDescAttribute? attr = RotationDescAttribute.MergeToOne(a);
                if (attr == null)
                {
                    continue;
                }

                List<IBaseAction> allActions = [];
                foreach (ActionID actionId in attr.Actions)
                {
                    IBaseAction? action = null;
                    foreach (IBaseAction baseAction in rotation.AllBaseActions)
                    {
                        if (baseAction.ID == (uint)actionId)
                        {
                            action = baseAction;
                            break;
                        }
                    }
                    if (action != null)
                    {
                        allActions.Add(action);
                    }
                }

                bool hasDesc = !string.IsNullOrEmpty(attr.Description);

                if (!hasDesc && allActions.Count == 0)
                {
                    continue;
                }

                ImGui.TableNextRow();
                _ = ImGui.TableNextColumn();

                if (IconSet.GetTexture(attr.IconID, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? image) && image?.Handle != null)
                {
                    ImGui.Image(image.Handle, Vector2.One * DESC_SIZE * Scale);
                }

                ImGui.SameLine();
                bool isOnCommand = attr.IsOnCommand;
                if (isOnCommand)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                }

                ImGui.Text(" " + attr.Type.GetDescription());
                if (isOnCommand)
                {
                    ImGui.PopStyleColor();
                }

                _ = ImGui.TableNextColumn();

                if (hasDesc)
                {
                    ImGui.Text(attr.Description);
                }

                bool notStart = false;
                float size = DESC_SIZE * Scale;
                float y = ImGui.GetCursorPosY() + (size * 4 / 82);
                foreach (IBaseAction item in allActions)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    if (notStart)
                    {
                        ImGui.SameLine();
                    }

                    if (item.GetTexture(out IDalamudTextureWrap? texture))
                    {
                        ImGui.SetCursorPosY(y);
                        Vector2 cursor = ImGui.GetCursorPos();
                        _ = ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * size);
                        ImGuiHelper.DrawActionOverlay(cursor, size, 1);
                        ImguiTooltips.HoveredTooltip(item.Name);
                    }
                    notStart = true;
                }
            }
        }
    }

    private static string GetRotationStatusHead()
    {
        ICustomRotation? rotation = DataCenter.CurrentRotation;
        string status = UiString.ConfigWindow_Rotation_Status.GetDescription();
        return rotation == null ? string.Empty : status;
    }

    private static void DrawRotationStatus()
    {
        DataCenter.CurrentRotation?.DisplayRotationStatus();
    }

    private static string ToCommandStr(OtherCommandType type, string str, string extra = "")
    {
        string result = Service.COMMAND + " " + type.ToString() + " " + str;
        if (!string.IsNullOrEmpty(extra))
        {
            result += " " + extra;
        }

        return result;
    }

    /// <summary>
    /// Checks if a rotation config should be visible based on parent-child relationships.
    /// </summary>
    /// <param name="config">The configuration to check.</param>
    /// <param name="configSet">The set of all configurations.</param>
    /// <returns>True if the config should be shown, false otherwise.</returns>
    private static bool ShouldShowRotationConfig(IRotationConfig config, IRotationConfigSet configSet)
    {
        if (string.IsNullOrEmpty(config.Parent))
        {
            return true;
        }

        IRotationConfig? parentConfig = null;
        foreach (var c in configSet.Configs)
        {
            if (c.Name == config.Parent)
            {
                parentConfig = c;
                break;
            }
        }

        if (parentConfig == null)
        {
            return true;
        }

        if (parentConfig is RotationConfigBoolean parentBool)
        {
            if (!bool.TryParse(parentBool.Value, out var isEnabled) || !isEnabled)
            {
                return false;
            }
        }
        else
        {
            var parentValueProperty = config.GetType().GetProperty("ParentValue");
            if (parentValueProperty != null)
            {
                var parentValue = parentValueProperty.GetValue(config);
                if (parentValue != null)
                {
                    var parentValueStr = parentValue.ToString();
                    if (parentValue.GetType().IsEnum && parentValueStr != null && parentValueStr.Contains('.'))
                    {
                        parentValueStr = parentValueStr.Split('.').Last();
                    }

                    if (parentConfig.Value == null ||
                        !string.Equals(parentConfig.Value.Trim(), parentValueStr?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static void DrawRotationConfiguration()
    {
        ICustomRotation? rotation = DataCenter.CurrentRotation;
        if (rotation == null) return;
        if (!Player.AvailableThreadSafe) return;

        bool enable = rotation.IsEnabled;
        if (ImGui.Checkbox(rotation.Name, ref enable))
        {
            rotation.IsEnabled = enable;
        }
        if (!enable) return;

        IRotationConfigSet set = rotation.Configs;

        bool hasAny = false;
        foreach (var _ in set.Configs)
        {
            hasAny = true;
            break;
        }
        if (hasAny) ImGui.Separator();

        foreach (IRotationConfig config in set.Configs)
        {
            if (DataCenter.IsPvP)
            {
                if (!config.Type.HasFlag(CombatType.PvP)) continue;
            }
            else
            {
                if (!config.Type.HasFlag(CombatType.PvE)) continue;
            }

            if (!ShouldShowRotationConfig(config, set)) continue;

            string key = rotation.GetType().FullName ?? rotation.GetType().Name + "." + config.Name;
            string name = $"##{config.GetHashCode()}_{key}.Name";
            string command = ToCommandStr(OtherCommandType.Rotations, config.Name, config.DefaultValue);
            void Reset() => config.Value = config.DefaultValue;

            ImGuiHelper.PrepareGroup(key, command, Reset);

            if (config is RotationConfigCombo c)
            {
                string[] names = c.DisplayValues;
                string selectedValue = c.Value;
                int index = -1;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].Equals(selectedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1) index = 0;

                string longest = "";
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].Length > longest.Length) longest = names[i];
                }
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(longest).X + (50 * Scale));
                if (ImGui.Combo(name, ref index, names, names.Length))
                {
                    c.Value = names[index];
                }
            }
            else if (config is RotationConfigBoolean b)
            {
                if (bool.TryParse(config.Value, out bool val))
                {
                    if (ImGui.Checkbox(name, ref val))
                    {
                        config.Value = val.ToString();
                    }
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else if (config is RotationConfigFloat f)
            {
                if (float.TryParse(config.Value, out float val))
                {
                    ImGui.SetNextItemWidth(Scale * Searchable.DRAG_WIDTH);
                    if (f.UnitType == ConfigUnitType.Percent)
                    {
                        float displayValue = val * 100;
                        if (ImGui.SliderFloat(name, ref displayValue, f.Min * 100, f.Max * 100, $"{displayValue:F1}{f.UnitType.ToSymbol()}"))
                        {
                            config.Value = (displayValue / 100).ToString();
                        }
                    }
                    else
                    {
                        if (ImGui.DragFloat(name, ref val, f.Speed, f.Min, f.Max, $"{val:F2}{f.UnitType.ToSymbol()}"))
                        {
                            config.Value = val.ToString();
                        }
                    }
                    ImguiTooltips.HoveredTooltip(f.UnitType.GetDescription());
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else if (config is RotationConfigString s)
            {
                string val = config.Value;
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth());
                if (ImGui.InputTextWithHint(name, config.DisplayName, ref val, 128))
                {
                    config.Value = val;
                }
                ImGuiHelper.ReactPopup(key, command, Reset);
                continue;
            }
            else if (config is RotationConfigInt i)
            {
                if (int.TryParse(config.Value, out int val))
                {
                    ImGui.SetNextItemWidth(Scale * Searchable.DRAG_WIDTH);
                    if (ImGui.DragInt(name, ref val, i.Speed, i.Min, i.Max))
                    {
                        config.Value = val.ToString();
                    }
                    ImGuiHelper.ReactPopup(key, command, Reset);
                }
            }
            else
            {
                continue;
            }

            ImGui.SameLine();
            ImGui.TextWrapped($"{config.DisplayName}");
            ImGuiHelper.ReactPopup(key, command, Reset, false);
        }

        if (Player.AvailableThreadSafe && DataCenter.PartyMembers != null && Player.Object.IsJobs(Job.DNC))
        {
            ImGui.Spacing();
            ImGui.Text("Dance Partner Priority");
            ImGui.Spacing();
            //var currentDancePartnerPriority = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.DancePartner, 0, SpecialActionType.None);
            //ImGui.Text($"Current Target: {currentDancePartnerPriority?.Name ?? "None"}");
            //ImGui.Spacing();

            if (ImGui.Button("Reset to Default"))
            {
                OtherConfiguration.ResetDancePartnerPriority();
            }
            ImGui.Spacing();

            List<Job> workingCopy = [.. OtherConfiguration.DancePartnerPriority];
            bool orderChanged = false;

            _ = ImGui.BeginChild("DancePartnerPriorityList", new Vector2(0, 200 * Scale), true);

            for (int i = 0; i < workingCopy.Count; i++)
            {
                Job job = workingCopy[i];
                string jobName = job.ToString();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##Up{i}") && i > 0)
                {
                    (workingCopy[i - 1], workingCopy[i]) = (workingCopy[i], workingCopy[i - 1]);
                    orderChanged = true;
                }

                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##Down{i}") && i < workingCopy.Count - 1)
                {
                    (workingCopy[i + 1], workingCopy[i]) = (workingCopy[i], workingCopy[i + 1]);
                    orderChanged = true;
                }

                ImGui.SameLine();
                ImGui.Text(jobName);
            }

            ImGui.EndChild();

            if (orderChanged)
            {
                OtherConfiguration.DancePartnerPriority = workingCopy;
                _ = OtherConfiguration.SaveDancePartnerPriority();
            }
        }

        if (Player.AvailableThreadSafe && DataCenter.PartyMembers != null && Player.Object.IsJobs(Job.SGE))
        {
            ImGui.Spacing();
            ImGui.Text("Kardia Tank Priority");
            ImGui.Spacing();
            //var currentKardiaTankPriority = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.Kardia, 0, SpecialActionType.None);
            //ImGui.Text($"Current Target: {currentKardiaTankPriority?.Name ?? "None"}");
            //ImGui.Spacing();

            if (ImGui.Button("Reset to Default"))
            {
                OtherConfiguration.ResetKardiaTankPriority();
            }
            ImGui.Spacing();

            List<Job> kardiaTankPriority = [.. OtherConfiguration.KardiaTankPriority];
            bool orderChanged = false;

            _ = ImGui.BeginChild("KardiaTankPriorityList", new Vector2(0, 200 * Scale), true);

            for (int i = 0; i < kardiaTankPriority.Count; i++)
            {
                Job job = kardiaTankPriority[i];
                string jobName = job.ToString();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##Up{i}") && i > 0)
                {
                    (kardiaTankPriority[i], kardiaTankPriority[i - 1]) = (kardiaTankPriority[i - 1], kardiaTankPriority[i]);
                    orderChanged = true;
                }

                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##Down{i}") && i < kardiaTankPriority.Count - 1)
                {
                    (kardiaTankPriority[i], kardiaTankPriority[i + 1]) = (kardiaTankPriority[i + 1], kardiaTankPriority[i]);
                    orderChanged = true;
                }

                ImGui.SameLine();
                ImGui.Text(jobName);
            }

            ImGui.EndChild();

            if (orderChanged)
            {
                OtherConfiguration.KardiaTankPriority = kardiaTankPriority;
                _ = OtherConfiguration.SaveKardiaTankPriority();
            }
        }

        if (Player.AvailableThreadSafe && DataCenter.PartyMembers != null && Player.Object.IsJobs(Job.AST))
        {
            using ImRaii.IEndObject table = ImRaii.Table("AstCardPriorityTable", 2, ImGuiTableFlags.SizingStretchProp);
            if (!table)
                return;

            // Column 1: Spear Card Priority
            ImGui.TableNextColumn();
            ImGui.Spacing();
            ImGui.Text("Spear Card Priority");
            ImGui.Spacing();
            //var currentTheSpearPriority = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheSpear, 0, SpecialActionType.None);
            //ImGui.Text($"Current Target: {currentTheSpearPriority?.Name ?? "None"}");
            //ImGui.Spacing();

            if (ImGui.Button("Reset to Default##Spear"))
            {
                OtherConfiguration.ResetTheSpearPriority();
            }
            ImGui.Spacing();

            List<Job> spearPriority = [.. OtherConfiguration.TheSpearPriority];
            bool spearOrderChanged = false;

            _ = ImGui.BeginChild("TheSpearPriorityList", new Vector2(0, 200 * Scale), true);

            for (int i = 0; i < spearPriority.Count; i++)
            {
                Job job = spearPriority[i];
                string jobName = job.ToString();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##UpSpear{i}") && i > 0)
                {
                    (spearPriority[i], spearPriority[i - 1]) = (spearPriority[i - 1], spearPriority[i]);
                    spearOrderChanged = true;
                }

                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##DownSpear{i}") && i < spearPriority.Count - 1)
                {
                    (spearPriority[i], spearPriority[i + 1]) = (spearPriority[i + 1], spearPriority[i]);
                    spearOrderChanged = true;
                }

                ImGui.SameLine();
                ImGui.Text(jobName);
            }

            ImGui.EndChild();

            if (spearOrderChanged)
            {
                OtherConfiguration.TheSpearPriority = spearPriority;
                _ = OtherConfiguration.SaveTheSpearPriority();
            }

            // Column 2: Balance Card Priority
            ImGui.TableNextColumn();
            ImGui.Spacing();
            ImGui.Text("Balance Card Priority");
            ImGui.Spacing();
            //var currentTheBalancePriority = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheBalance, 0, SpecialActionType.None);
            //ImGui.Text($"Current Target: {currentTheBalancePriority?.Name ?? "None"}");
            //ImGui.Spacing();

            if (ImGui.Button("Reset to Default##Balance"))
            {
                OtherConfiguration.ResetTheBalancePriority();
            }
            ImGui.Spacing();

            List<Job> balancePriority = [.. OtherConfiguration.TheBalancePriority];
            bool balanceOrderChanged = false;

            _ = ImGui.BeginChild("TheBalancePriorityList", new Vector2(0, 200 * Scale), true);

            for (int i = 0; i < balancePriority.Count; i++)
            {
                Job job = balancePriority[i];
                string jobName = job.ToString();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##UpBalance{i}") && i > 0)
                {
                    (balancePriority[i], balancePriority[i - 1]) = (balancePriority[i - 1], balancePriority[i]);
                    balanceOrderChanged = true;
                }

                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##DownBalance{i}") && i < balancePriority.Count - 1)
                {
                    (balancePriority[i], balancePriority[i + 1]) = (balancePriority[i + 1], balancePriority[i]);
                    balanceOrderChanged = true;
                }

                ImGui.SameLine();
                ImGui.Text(jobName);
            }

            ImGui.EndChild();

            if (balanceOrderChanged)
            {
                OtherConfiguration.TheBalancePriority = balancePriority;
                _ = OtherConfiguration.SaveTheBalancePriority();
            }
        }
    }
    #endregion

    #region Actions
    private static unsafe void DrawActions()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Actions_Description.GetDescription());

        using ImRaii.IEndObject table = ImRaii.Table("Rotation Solver Actions", 2, ImGuiTableFlags.Resizable);

        if (table)
        {
            ImGui.TableSetupColumn("Action Column", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
            ImGui.TableNextColumn();

            if (_actionsList != null)
            {
                _actionsList.ClearCollapsingHeader();

                if (DataCenter.CurrentRotation != null && RotationUpdater.AllGroupedActions != null)
                {
                    float size = 30 * Scale;
                    int count = Math.Max(1, (int)MathF.Floor(ImGui.GetColumnWidth() / ((size * 1.1f) + ImGui.GetStyle().ItemSpacing.X)));
                    foreach (var pair in RotationUpdater.AllGroupedActions)
                    {
                        _actionsList.AddCollapsingHeader(() => pair.Key, () =>
                        {
                            int index = 0;

                            // Build list from the current group
                            List<IAction> source = [.. pair];

                            // Group by AdjustedID, sort groups by their lowest Level,
                            // then flatten back while keeping Adjusted IDs contiguous.
                            var byAdjusted = new Dictionary<uint, List<IAction>>();
                            for (int i = 0; i < source.Count; i++)
                            {
                                IAction a = source[i];
                                if (!byAdjusted.TryGetValue(a.AdjustedID, out var list))
                                {
                                    list = [];
                                    byAdjusted[a.AdjustedID] = list;
                                }
                                list.Add(a);
                            }

                            var groups = new List<(uint AdjustedID, byte MinLevel, List<IAction> Items)>(byAdjusted.Count);
                            foreach (var kv in byAdjusted)
                            {
                                List<IAction> items = kv.Value;
                                // Sort items inside a group by Level then by ID for stability
                                items.Sort((x, y) =>
                                {
                                    int cmp = x.Level.CompareTo(y.Level);
                                    return cmp != 0 ? cmp : x.ID.CompareTo(y.ID);
                                });
                                byte minLvl = items.Count > 0 ? items[0].Level : (byte)0;
                                groups.Add((kv.Key, minLvl, items));
                            }

                            // Sort groups by the lowest required level, then by AdjustedID
                            groups.Sort((g1, g2) =>
                            {
                                int cmp = g1.MinLevel.CompareTo(g2.MinLevel);
                                return cmp != 0 ? cmp : g1.AdjustedID.CompareTo(g2.AdjustedID);
                            });

                            // Flatten into the final sorted list
                            List<IAction> sorted = new(source.Count);
                            foreach (var (AdjustedID, MinLevel, Items) in groups)
                                sorted.AddRange(Items);

                            foreach (IAction? item in sorted)
                            {
                                if (!IconSet.GetTexture(item.IconID, out IDalamudTextureWrap? icon))
                                {
                                    continue;
                                }

                                if (index++ % count != 0)
                                {
                                    ImGui.SameLine();
                                }

                                ImGui.BeginGroup();
                                Vector2 cursor = ImGui.GetCursorPos();
                                if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * size, item.Name + item.ID))
                                {
                                    _activeAction = item;
                                }
                                ImGuiHelper.DrawActionOverlay(cursor, size, _activeAction == item ? 1 : 0);

                                if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out IDalamudTextureWrap? texture))
                                {
                                    Vector2 offset = new(1 / 12f, 1 / 6f);
                                    ImGui.SetCursorPos(cursor + (new Vector2(0.6f, 0.7f) * size));
                                    ImGui.Image(texture.Handle, Vector2.One * size * 0.5f,
                                        new Vector2(item.IsEnabled ? 0 : 0.5f, 0) + offset,
                                        new Vector2(item.IsEnabled ? 0.5f : 1, 1) - offset);
                                }
                                ImGui.EndGroup();

                                string key = $"Action Macro Usage {item.Name} {item.ID}";
                                string cmd = ToCommandStr(OtherCommandType.DoActions, $"{item}-{5}");
                                ImGuiHelper.DrawHotKeysPopup(key, cmd);
                                ImGuiHelper.ExecuteHotKeysPopup(key, cmd, item.Name, false);
                            }
                        });
                    }
                }

                _actionsList.Draw();
            }

            ImGui.TableNextColumn();

            DrawConfigsOfAction();
            DrawActionDebug();
        }

        static void DrawConfigsOfAction()
        {
            if (_activeAction == null)
            {
                return;
            }

            bool isEnabled = _activeAction.IsEnabled;
            if (ImGui.Checkbox($"{_activeAction.Name}##{_activeAction.Name} Enabled", ref isEnabled))
            {
                _activeAction.IsEnabled = isEnabled;
            }

            const string key = "Action Enable Popup";
            string cmd = ToCommandStr(OtherCommandType.ToggleActions, _activeAction.ToString()!);
            ImGuiHelper.DrawHotKeysPopup(key, cmd);
            ImGuiHelper.ExecuteHotKeysPopup(key, cmd, string.Empty, false);

            bool isIntercepted = _activeAction.IsIntercepted;
            if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_IsIntercepted.GetDescription()}##{_activeAction.Name}", ref isIntercepted))
            {
                _activeAction.IsIntercepted = isIntercepted;
            }

            bool minHPFeatureSet = _activeAction.MinHPFeature;
            if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_MinHPFeature.GetDescription()}##{_activeAction.Name}", ref minHPFeatureSet))
            {
                _activeAction.MinHPFeature = minHPFeatureSet;
            }

            bool isRestrictedDOT = _activeAction.IsRestrictedDOT;
            if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_IsRestrictedDOT.GetDescription()}##{_activeAction.Name}", ref isRestrictedDOT))
            {
                _activeAction.IsRestrictedDOT = isRestrictedDOT;
            }

            float minHPPercentSet = _activeAction.MinHPPercent;
            if (_activeAction.MinHPFeature == true)
            {
                float minHPPercentUi = Math.Clamp(_activeAction.MinHPPercent * 100f, 0f, 100f);
                ImGui.SetNextItemWidth(Scale * 150);
                if (ImGui.DragFloat($"{UiString.ConfigWindow_Actions_MinHPPercent.GetDescription()}##{_activeAction.Name}",
                    ref minHPPercentUi, 0.1f, 0f, 100f, $"{minHPPercentUi:F1}{ConfigUnitType.Percent.ToSymbol()}"))
                {
                    _activeAction.MinHPPercent = Math.Clamp(minHPPercentUi / 100f, 0f, 1f);
                }
            }

            bool showOnCdWindow = _activeAction.IsOnCooldownWindow;
            if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_ShowOnCDWindow.GetDescription()}##{_activeAction.Name}InCooldown", ref showOnCdWindow))
            {
                _activeAction.IsOnCooldownWindow = showOnCdWindow;
            }

            if (_activeAction is IBaseAction a)
            {
                DrawConfigsOfBaseAction(a);
            }

            ImGui.Separator();

            static void DrawConfigsOfBaseAction(IBaseAction a)
            {
                ActionConfig config = a.Config;

                ImGui.Separator();

                int ttk = config.TimeToKill;
                ImGui.SetNextItemWidth(Scale * 150);
                if (ImGui.DragInt($"{UiString.ConfigWindow_Actions_TTK.GetDescription()}##{a}",
                    ref ttk, 0.1f, 0, 120, $"{ttk:F2}{ConfigUnitType.Seconds.ToSymbol()}"))
                {
                    config.TimeToKill = ttk;
                }
                ImguiTooltips.HoveredTooltip(ConfigUnitType.Seconds.GetDescription());

                if (a.Setting.StatusProvide != null || a.Setting.TargetStatusProvide != null)
                {
                    bool shouldStatus = config.ShouldCheckStatus;
                    if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_CheckStatus.GetDescription()}##{a}", ref shouldStatus))
                    {
                        config.ShouldCheckStatus = shouldStatus;
                    }

                    if (shouldStatus)
                    {
                        int statusGcdCount = config.StatusGcdCount;
                        ImGui.SetNextItemWidth(Scale * 150);
                        if (ImGui.DragInt($"{UiString.ConfigWindow_Actions_GcdCount.GetDescription()}##{a}",
                            ref statusGcdCount, 0.05f, 1, 10))
                        {
                            config.StatusGcdCount = (byte)statusGcdCount;
                        }
                    }
                }

                if (!a.TargetInfo.IsSingleTarget)
                {
                    int aoeCount = config.AoeCount;
                    ImGui.SetNextItemWidth(Scale * 150);
                    if (ImGui.DragInt($"{UiString.ConfigWindow_Actions_AoeCount.GetDescription()}##{a}",
                        ref aoeCount, 0.05f, 1, 10))
                    {
                        config.AoeCount = (byte)aoeCount;
                    }
                }

                float ratio = config.AutoHealRatio;
                ImGui.SetNextItemWidth(Scale * 150);
                if (ImGui.DragFloat($"{UiString.ConfigWindow_Actions_HealRatio.GetDescription()}##{a}",
                    ref ratio, 0.002f, 0, 1, $"{ratio * 100:F1}{ConfigUnitType.Percent.ToSymbol()}"))
                {
                    config.AutoHealRatio = ratio;
                }
                ImguiTooltips.HoveredTooltip(ConfigUnitType.Percent.GetDescription());

            }
        }

        static void DrawActionDebug()
        {
            if (!Player.AvailableThreadSafe || !Service.Config.InDebug)
            {
                return;
            }

            if (_activeAction is IBaseAction action)
            {
                try
                {
                    IBattleChara target = action.Target.Target;
                    if (target is not IBattleChara battleChara)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Target is not a valid BattleChara.");
                        return;
                    }
                    ImGui.Text("Can Use: " + action.CanUse(out _));
                    ImGui.Spacing();
                    ImGui.Text("ID: " + action.Info.ID);
                    ImGui.Text("GCDSingleHeal: " + action.Config.GCDSingleHeal);
                    ImGui.Text("MinHPPercent: " + action.MinHPPercent);
                    ImGui.Text("AdjustedID: " + Service.GetAdjustedActionId(action.Info.ID));
                    ImGui.Text($"IsQuestUnlocked: {action.Info.IsQuestUnlocked()} ({action.Action.UnlockLink.RowId})");
                    ImGui.Text("EnoughLevel: " + action.EnoughLevel);
                    if (!action.TargetInfo.IsSingleTarget)
                    {
                        ImGui.Text("AoeCount: " + action.Config.AoeCount);
                    }
                    ImGui.Text("ShouldCheckStatus: " + action.Config.ShouldCheckStatus);
                    ImGui.Text("ShouldCheckTargetStatus: " + action.Config.ShouldCheckTargetStatus);
                    ImGui.Text("Is Real GCD: " + action.Info.IsRealGCD);
                    ImGui.Text("Is PvP Action: " + action.Info.IsPvP);
                    ImGui.Text("Cast Type: " + action.Info.CastType);

                    // Ensure ActionManager.Instance() is not null and action.AdjustedID is valid
                    if (ActionManager.Instance() != null && action.AdjustedID != 0)
                    {
                        ImGui.Text("Resources: " + ActionManager.Instance()->CheckActionResources(ActionType.Action, action.AdjustedID));
                        ImGui.Text("Status: " + ActionManager.Instance()->GetActionStatus(ActionType.Action, action.AdjustedID));
                    }
                    ImGui.Text("Cast Time: " + action.Info.CastTime);
                    ImGui.Text("MP: " + action.Info.MPNeed);
                    ImGui.Text("HasEnoughMP: " + action.Info.HasEnoughMP());
                    ImGui.Text("AttackType: " + action.Info.AttackType);
                    ImGui.Text("Level: " + action.Info.Level);
                    ImGui.Text("Range: " + action.Info.Range);
                    ImGui.Text("EffectRange: " + action.Info.EffectRange);
                    ImGui.Text("Aspect: " + action.Info.Aspect);
                    ImGui.Text("Has One:" + action.Cooldown.HasOneCharge);
                    ImGui.Text("Recast One: " + action.Cooldown.RecastTimeOneChargeRaw);
                    ImGui.Text("Recast Elapsed: " + action.Cooldown.RecastTimeElapsed);
                    ImGui.Text("Recast Time Elapsed One Charge: " + action.Cooldown.RecastTimeElapsedOneCharge);
                    ImGui.Text("Recast Time Remain One Charge: " + action.Cooldown.RecastTimeRemainOneCharge);
                    ImGui.Text($"Charges: {action.Cooldown.CurrentCharges} / {action.Cooldown.MaxCharges}");

                    ImGui.Text("IgnoreCastCheck:" + action.CanUse(out _, skipCastingCheck: true));
                    ImGui.Text("Target Name: " + action.Target.Target?.Name ?? string.Empty);
                }
                catch (Exception ex)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Error: " + ex.Message);
                }
            }
            else if (_activeAction is IBaseItem item)
            {
                try
                {
                    // Ensure ActionManager.Instance() is not null
                    if (ActionManager.Instance() != null)
                    {
                        ImGui.Text("Status: " + ActionManager.Instance()->GetActionStatus(ActionType.Item, item.ID).ToString());
                        ImGui.Text("Status HQ: " + ActionManager.Instance()->GetActionStatus(ActionType.Item, item.ID + 1000000).ToString());
                        float remain = ActionManager.Instance()->GetRecastTime(ActionType.Item, item.ID) - ActionManager.Instance()->GetRecastTimeElapsed(ActionType.Item, item.ID);
                        ImGui.Text("remain: " + remain.ToString());
                        ImGui.Text("ID: " + item.ID.ToString());
                        ImGui.Text("A4: " + item.A4.ToString());
                        ImGui.Text("AdjustedID: " + item.AdjustedID.ToString());
                    }

                    ImGui.Text("CanUse: " + item.CanUse(out _, true).ToString());

                    if (item is HpPotionItem healPotionItem)
                    {
                        ImGui.Text("MaxHP:" + healPotionItem.MaxHp.ToString());
                    }
                }
                catch (Exception ex)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Error: " + ex.Message);
                }
            }
        }
    }

    private static IAction? _activeAction;

    private static readonly CollapsingHeaderGroup _actionsList = new([])
    {
        HeaderSize = 18,
    };
    #endregion

    #region List
    private static readonly Lazy<Status[]> _allDispelStatus = new(() =>
    {
        var sheet = Service.GetSheet<Status>();
        var list = new List<Status>();
        foreach (var s in sheet)
        {
            if (s.CanDispel)
            {
                list.Add(s);
            }
        }
        return [.. list];
    });

    internal static Status[] AllDispelStatus => _allDispelStatus.Value;

    private static readonly Lazy<Status[]> _allStatus = new(() =>
    {
        var sheet = Service.GetSheet<Status>();
        if (sheet == null)
            return [];
        var list = new List<Status>();
        foreach (var s in sheet)
        {
            if (!string.IsNullOrEmpty(s.Name.ToString()) && s.Icon != 0)
            {
                list.Add(s);
            }
        }
        return [.. list];
    });

    internal static Status[] AllStatus => _allStatus.Value;

    private static readonly Lazy<GAction[]> _allActions = new(() =>
    {
        var sheet = Service.GetSheet<GAction>();
        var list = new List<GAction>();
        foreach (var a in sheet)
        {
            if (!string.IsNullOrEmpty(a.ToString()) && !a.IsPvP && !a.IsPlayerAction
                && a.Cast100ms > 0)
            {
                list.Add(a);
            }
        }
        GAction[] result = new GAction[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            result[i] = list[i];
        }
        return result;
    });

    internal static GAction[] AllActions => _allActions.Value;

    private const int BadStatusCategory = 2;
    private static readonly Lazy<Status[]> _badStatus = new(() =>
    {
        var sheet = Service.GetSheet<Status>();
        var list = new List<Status>();
        foreach (var s in sheet)
        {
            if (s.StatusCategory == BadStatusCategory && s.Icon != 0)
            {
                list.Add(s);
            }
        }
        return [.. list];
    });

    internal static Status[] BadStatus => _badStatus.Value;

    private static void DrawList()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_List_Description.GetDescription());
        _idsHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _idsHeader = new(new()
    {
        { UiString.ConfigWindow_List_Statuses.GetDescription, DrawListStatuses},
        { () => Service.Config.UseDefenseAbility ? UiString.ConfigWindow_List_Actions.GetDescription() : string.Empty, DrawListActions},
        { UiString.ConfigWindow_List_Territories.GetDescription, DrawListTerritories},
    });

    private static void DrawListStatuses()
    {
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth());
        _ = ImGui.InputTextWithHint("##Searching the action", UiString.ConfigWindow_List_StatusNameOrId.GetDescription(), ref _statusSearching, 50);

        using ImRaii.IEndObject table = ImRaii.Table("Rotation Solver List Statuses", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Invuln Status List"))
            {
                OtherConfiguration.ResetInvincibleStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_Invincibility.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Priority Status List"))
            {
                OtherConfiguration.ResetPriorityStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_Priority.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Dispell Debuff List"))
            {
                OtherConfiguration.ResetDangerousStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_DangerousStatus.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update No Casting Status List"))
            {
                OtherConfiguration.ResetNoCastingStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_NoCastingStatus.GetDescription());

            ImGui.TableNextRow();

            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_InvincibilityDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.InvincibleStatus), OtherConfiguration.InvincibleStatus, AllStatus);

            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_PriorityDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.PriorityStatus), OtherConfiguration.PriorityStatus, AllStatus);

            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_DangerousStatusDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.DangerousStatus), OtherConfiguration.DangerousStatus, AllDispelStatus);

            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_NoCastingStatusDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.NoCastingStatus), OtherConfiguration.NoCastingStatus, BadStatus);
        }
    }

    private static void FromClipBoardButton(HashSet<uint> items)
    {
        const string CopyErrorMessage = "Failed to copy the values to the clipboard.";
        const string PasteErrorMessage = "Failed to copy the values from the clipboard.";

        if (ImGui.Button(UiString.ConfigWindow_Actions_Copy.GetDescription()))
        {
            try
            {
                ImGui.SetClipboardText(JsonConvert.SerializeObject(items));
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"{CopyErrorMessage}: {ex.Message}");
            }
        }

        ImGui.SameLine();

        if (ImGui.Button(UiString.ActionSequencer_FromClipboard.GetDescription()))
        {
            try
            {
                string clipboardText = ImGui.GetClipboardText();
                if (clipboardText != null)
                {
                    foreach (uint aId in JsonConvert.DeserializeObject<uint[]>(clipboardText) ?? [])
                    {
                        _ = items.Add(aId);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"{PasteErrorMessage}: {ex.Message}");
            }
            finally
            {
                _ = OtherConfiguration.Save();
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static string _statusSearching = string.Empty;
    private static void DrawStatusList(string name, HashSet<uint> statuses, Status[] allStatus)
    {
        const float IconWidth = 24f;
        const float IconHeight = 32f;
        const uint DefaultNotLoadId = 0;

        ImGui.PushID(name);
        FromClipBoardButton(statuses);

        uint removeStatusId = 0; // Renamed variable to avoid conflict
        uint notLoadId = DefaultNotLoadId;

        string popupId = $"Rotation Solver Popup{name}";

        StatusPopUp(popupId, allStatus, ref _statusSearching, status =>
        {
            _ = statuses.Add(status.RowId);
            _ = OtherConfiguration.Save();
        }, notLoadId);

        int count = Math.Max(1, (int)MathF.Floor(ImGui.GetColumnWidth() / ((IconWidth * Scale) + ImGui.GetStyle().ItemSpacing.X)));
        int index = 0;

        if (index++ % count != 0)
        {
            ImGui.SameLine();
        }
        if (ImGui.Button("+", new Vector2(IconWidth, IconHeight) * Scale))
        {
            if (!ImGui.IsPopupOpen(popupId))
            {
                ImGui.OpenPopup(popupId);
            }
        }
        ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_List_AddStatus.GetDescription());

        foreach (uint statusId in statuses)
        {
            Status status = Service.GetSheet<Status>().GetRow(statusId);
            if (status.RowId == 0)
            {
                continue;
            }

            void Delete()
            {
                removeStatusId = status.RowId; // Updated variable name
            }

            string key = $"Status{status.RowId}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty, (UiString.ConfigWindow_List_Remove.GetDescription(), Delete, pairsArray));

            if (IconSet.GetTexture(status.Icon, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, notLoadId) && texture?.Handle != null)
            {
                if (index++ % count != 0)
                {
                    ImGui.SameLine();
                }
                _ = ImGuiHelper.NoPaddingNoColorImageButton(texture, new Vector2(IconWidth, IconHeight) * Scale, $"Status{status.RowId}");

                ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, $"{status.Name} ({status.RowId})", false,
                    (Delete, new[] { VirtualKey.DELETE }));
            }
        }

        if (removeStatusId != 0) // Updated variable name
        {
            _ = statuses.Remove(removeStatusId); // Updated variable name
            _ = OtherConfiguration.Save();
        }
        ImGui.PopID();
    }

    internal static void StatusPopUp(string popupId, Status[] allStatus, ref string searching, Action<Status> clicked, uint notLoadId = 0, float size = 32)
    {
        const float InputWidth = 200f;
        const float ChildHeight = 400f;
        const int InputTextLength = 128;

        using ImRaii.IEndObject popup = ImRaii.Popup(popupId);
        if (popup)
        {
            ImGui.SetNextItemWidth(InputWidth * Scale);
            _ = ImGui.InputTextWithHint("##Searching the status", "Enter status name/number", ref searching, InputTextLength);

            ImGui.Spacing();

            using ImRaii.IEndObject child = ImRaii.Child("Rotation Solver Reborn Add Status", new Vector2(-1, ChildHeight * Scale));
            if (child)
            {
                int count = Math.Max(1, (int)MathF.Floor(ImGui.GetWindowWidth() / ((size * 3 / 4 * Scale) + ImGui.GetStyle().ItemSpacing.X)));
                int index = 0;

                if (string.IsNullOrWhiteSpace(searching))
                {
                    return;
                }

                string searchingKey = searching;

                // Manual filtering and sorting instead of LINQ
                List<(Status status, double score)> filtered = [];
                for (int i = 0; i < allStatus.Length; i++)
                {
                    Status s = allStatus[i];
                    double sim = SearchableCollection.Similarity($"{s.Name} {s.RowId}", searchingKey);
                    if (sim > 0)
                    {
                        filtered.Add((s, sim));
                    }
                }

                // Sort descending by similarity
                for (int i = 0; i < filtered.Count - 1; i++)
                {
                    for (int j = i + 1; j < filtered.Count; j++)
                    {
                        if (filtered[j].score > filtered[i].score)
                        {
                            (filtered[j], filtered[i]) = (filtered[i], filtered[j]);
                        }
                    }
                }

                if (filtered.Count == 0)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "No matching statuses found.");
                    return;
                }

                foreach (var tuple in filtered)
                {
                    Status status = tuple.status;
                    if (status.Icon != 215049 && IconSet.GetTexture(status.Icon, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, notLoadId) && texture?.Handle != null)
                    {
                        if (index++ % count != 0)
                        {
                            ImGui.SameLine();
                        }
                        if (ImGuiHelper.NoPaddingNoColorImageButton(texture, new Vector2(size * 3 / 4, size) * Scale, $"Adding{status.RowId}"))
                        {
                            clicked?.Invoke(status);
                            ImGui.CloseCurrentPopup();
                        }
                        ImguiTooltips.HoveredTooltip($"{status.Name} ({status.RowId})");
                    }
                }
            }
        }
    }

    private static void DrawListActions()
    {
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth());
        _ = ImGui.InputTextWithHint("##Searching the action", UiString.ConfigWindow_List_ActionNameOrId.GetDescription(), ref _actionSearching, 50);

        using ImRaii.IEndObject table = ImRaii.Table("Rotation Solver List Actions", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Tankbuster List"))
            {
                OtherConfiguration.ResetHostileCastingTank();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingTank.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update AOE List"))
            {
                OtherConfiguration.ResetHostileCastingArea();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingArea.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Knockback List"))
            {
                OtherConfiguration.ResetHostileCastingKnockback();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingKnockback.GetDescription());

            _ = ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Stop Casting List"))
            {
                OtherConfiguration.ResetHostileCastingStop();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingStop.GetDescription());

            ImGui.TableNextRow();

            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingTankDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingTank), OtherConfiguration.HostileCastingTank);

            _ = ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingAreaDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingArea), OtherConfiguration.HostileCastingArea);

            _ = ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List2);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingKnockbackDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingKnockback), OtherConfiguration.HostileCastingKnockback);

            _ = ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List3);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingStopDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingStop), OtherConfiguration.HostileCastingStop);
        }
    }

    private static string _actionSearching = string.Empty;
    private static string _actionPopupSearching = string.Empty;

    private static void DrawActionsList(string name, HashSet<uint> actions)
    {
        actions ??= [];
        if (name == null)
        {
            return;
        }
        ImGui.PushID(name);
        uint removeId = 0;
        string popupId = $"Rotation Solver Reborn Action Popup{name}";

        if (ImGui.Button($"{UiString.ConfigWindow_List_AddAction.GetDescription()}##{name}"))
        {
            if (!ImGui.IsPopupOpen(popupId))
            {
                ImGui.OpenPopup(popupId);
            }
        }

        ImGui.SameLine();
        FromClipBoardButton(actions);

        ImGui.Spacing();

        // Build a list of GAction objects from the action IDs
        List<GAction> actionList = [];
        foreach (uint a in actions)
        {
            GAction? act = Service.GetSheet<GAction>().GetRow(a);
            if (act != null)
            {
                actionList.Add(act.Value);
            }
        }

        // Efficient search and sort
        if (!string.IsNullOrEmpty(_actionSearching))
        {
            // Precompute similarity scores
            var scored = new List<(GAction action, float score)>(actionList.Count);
            foreach (var action in actionList)
            {
                float sim = SearchableCollection.Similarity($"{action.Name} {action.RowId}", _actionSearching);
                scored.Add((action, sim));
            }
            // Sort descending by score
            scored.Sort((a, b) => b.score.CompareTo(a.score));
            // Overwrite actionList with sorted results
            actionList.Clear();
            foreach (var (action, score) in scored)
            {
                actionList.Add(action);
            }
        }

        for (int idx = 0; idx < actionList.Count; idx++)
        {
            GAction action = actionList[idx];
            void Reset() => removeId = action.RowId;
            string key = $"Action{action.RowId}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty, (UiString.ConfigWindow_List_Remove.GetDescription(), Reset, pairs));

            _ = ImGui.Selectable($"{action.Name} ({action.RowId})");

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, false, (Reset, new[] { VirtualKey.DELETE }));
        }

        if (removeId != 0)
        {
            _ = actions.Remove(removeId);
            _ = OtherConfiguration.Save();
        }

        ActionPopup(popupId, actions);

        ImGui.PopID();
    }

    private static void ActionPopup(string popupId, HashSet<uint> actions)
    {
        const float InputWidth = 200f;
        const float ChildHeight = 400f;
        const int MaxDisplayCount = 50;

        using ImRaii.IEndObject popup = ImRaii.Popup(popupId);
        if (popup)
        {
            ImGui.SetNextItemWidth(InputWidth * Scale);
            _ = ImGui.InputTextWithHint("##Searching the action pop up", UiString.ConfigWindow_List_ActionNameOrId.GetDescription(), ref _actionPopupSearching, 50);

            ImGui.Spacing();

            using ImRaii.IEndObject child = ImRaii.Child("Rotation Solver Add action", new Vector2(-1, ChildHeight * Scale));
            if (child)
            {
                if (string.IsNullOrWhiteSpace(_actionPopupSearching))
                {
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "Enter a search term to filter actions.");
                }
                else
                {
                    // Manual filtering and sorting (no LINQ)
                    var filtered = new List<(GAction action, float sim)>();
                    string searchLower = _actionPopupSearching.Trim().ToLowerInvariant();

                    for (int i = 0; i < AllActions.Length; i++)
                    {
                        GAction a = AllActions[i];

                        // Skip actions already in the list
                        bool found = false;
                        foreach (var id in actions)
                        {
                            if (id == a.RowId)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            continue;

                        string nameLower = a.Name.ToString().ToLowerInvariant();
                        string idStr = a.RowId.ToString();

                        // Direct substring or ID match gets highest score
                        if (nameLower.Contains(searchLower) || idStr == searchLower)
                        {
                            filtered.Add((a, 1000f)); // Arbitrary high score for direct match
                        }
                        else
                        {
                            float sim = SearchableCollection.Similarity($"{a.Name} {a.RowId}", _actionPopupSearching);
                            if (sim > 0)
                                filtered.Add((a, sim));
                        }
                    }

                    // Sort descending by score (manual, no LINQ)
                    int n = filtered.Count;
                    for (int i = 0; i < n - 1; i++)
                    {
                        for (int j = i + 1; j < n; j++)
                        {
                            if (filtered[j].sim > filtered[i].sim)
                            {
                                (filtered[j], filtered[i]) = (filtered[i], filtered[j]);
                            }
                        }
                    }

                    int shown = 0;
                    for (int i = 0; i < filtered.Count && shown < MaxDisplayCount; i++)
                    {
                        GAction action = filtered[i].action;
                        bool selected = ImGui.Selectable($"{action.Name} ({action.RowId})");
                        if (ImGui.IsItemHovered())
                        {
                            ImguiTooltips.ShowTooltip($"{action.Name} ({action.RowId})");
                            if (selected)
                            {
                                _ = actions.Add(action.RowId);
                                _ = OtherConfiguration.Save();
                                ImGui.CloseCurrentPopup();
                            }
                        }
                        shown++;
                    }

                    if (shown == 0)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "No matching actions found.");
                    }
                }
            }
        }
    }

    public static Vector3 HoveredPosition { get; private set; } = Vector3.Zero;
    private static void DrawListTerritories()
    {
        if (Svc.ClientState == null) return;
        ushort territoryId = Svc.ClientState.TerritoryType;

        using ImRaii.IEndObject table = ImRaii.Table("Rotation Solver List Territories", 4,
            ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            _ = ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_NoHostile.GetDescription());
            _ = ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_NoProvoke.GetDescription());
            _ = ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_BeneficialPositions.GetDescription());

            ImGui.TableNextRow();

            // NoHostile
            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_NoHostileDesc.GetDescription());
            float width = ImGui.GetColumnWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X - ImGui.GetStyle().ItemSpacing.X - (10 * Scale);

            if (!OtherConfiguration.NoHostileNames.TryGetValue(territoryId, out string[]? libs))
            {
                OtherConfiguration.NoHostileNames[territoryId] = libs = [];
            }

            bool hasEmpty = false;
            for (int i = 0; i < libs.Length; i++)
            {
                if (string.IsNullOrEmpty(libs[i]))
                {
                    hasEmpty = true;
                    break;
                }
            }
            if (!hasEmpty)
            {
                var newArr = new string[libs.Length + 1];
                for (int i = 0; i < libs.Length; i++) newArr[i] = libs[i];
                newArr[^1] = string.Empty;
                OtherConfiguration.NoHostileNames[territoryId] = libs = newArr;
            }

            int removeIndex = -1;
            for (int i = 0; i < libs.Length; i++)
            {
                ImGui.SetNextItemWidth(width);
                if (ImGui.InputTextWithHint($"##Rotation Solver Territory Target Name {i}",
                    UiString.ConfigWindow_List_NoHostilesName.GetDescription(), ref libs[i], 1024))
                {
                    OtherConfiguration.NoHostileNames[territoryId] = libs;
                    _ = OtherConfiguration.SaveNoHostileNames();
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Remove Territory Target Name {i}"))
                {
                    removeIndex = i;
                }
            }
            if (removeIndex > -1)
            {
                var list = new List<string>(libs.Length - 1);
                for (int i = 0; i < libs.Length; i++)
                {
                    if (i == removeIndex) continue;
                    list.Add(libs[i]);
                }
                OtherConfiguration.NoHostileNames[territoryId] = [.. list];
                _ = OtherConfiguration.SaveNoHostileNames();
            }

            // NoProvoke
            _ = ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_NoProvokeDesc.GetDescription());

            width = ImGui.GetColumnWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X
                - ImGui.GetStyle().ItemSpacing.X - (10 * Scale);

            if (!OtherConfiguration.NoProvokeNames.TryGetValue(territoryId, out libs))
            {
                OtherConfiguration.NoProvokeNames[territoryId] = libs = [];
            }

            hasEmpty = false;
            for (int i = 0; i < libs.Length; i++)
            {
                if (string.IsNullOrEmpty(libs[i])) { hasEmpty = true; break; }
            }
            if (!hasEmpty)
            {
                var newArr = new string[libs.Length + 1];
                for (int i = 0; i < libs.Length; i++) newArr[i] = libs[i];
                newArr[^1] = string.Empty;
                OtherConfiguration.NoProvokeNames[territoryId] = libs = newArr;
            }

            removeIndex = -1;
            for (int i = 0; i < libs.Length; i++)
            {
                ImGui.SetNextItemWidth(width);
                if (ImGui.InputTextWithHint($"##Rotation Solver Reborn Territory Provoke Name {i}",
                    UiString.ConfigWindow_List_NoProvokeName.GetDescription(), ref libs[i], 1024))
                {
                    OtherConfiguration.NoProvokeNames[territoryId] = libs;
                    _ = OtherConfiguration.SaveNoProvokeNames();
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Reborn Remove Territory Provoke Name {i}"))
                {
                    removeIndex = i;
                }
            }
            if (removeIndex > -1)
            {
                var list = new List<string>(libs.Length - 1);
                for (int i = 0; i < libs.Length; i++)
                {
                    if (i == removeIndex) continue;
                    list.Add(libs[i]);
                }
                OtherConfiguration.NoProvokeNames[territoryId] = [.. list];
                _ = OtherConfiguration.SaveNoProvokeNames();
            }

            _ = ImGui.TableNextColumn();
            if (!OtherConfiguration.BeneficialPositions.TryGetValue(territoryId, out Vector3[]? pts))
            {
                OtherConfiguration.BeneficialPositions[territoryId] = pts = [];
            }

            if (ImGui.Button(UiString.ConfigWindow_List_AddPosition.GetDescription()) && Player.AvailableThreadSafe)
            {
                unsafe
                {
                    Vector3 point = Player.Object.Position;
                    Vector3 pointMathed = point + (Vector3.UnitY * 5);
                    Vector3 direction = Vector3.UnitY;
                    Vector3* directionPtr = &direction;
                    Vector3* pointPtr = &pointMathed;
                    int* unknown = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
                    RaycastHit hit = default;

                    var newPts = new Vector3[pts.Length + 1];
                    for (int i = 0; i < pts.Length; i++) newPts[i] = pts[i];
                    if (Framework.Instance()->BGCollisionModule
                        ->RaycastMaterialFilter(&hit, pointPtr, directionPtr, 20, 1, unknown))
                    {
                        newPts[^1] = hit.Point;
                    }
                    else
                    {
                        newPts[^1] = point;
                    }
                    OtherConfiguration.BeneficialPositions[territoryId] = newPts;
                    _ = OtherConfiguration.SaveBeneficialPositions();
                }
            }

            HoveredPosition = Vector3.Zero;
            int removePosIndex = -1;
            for (int i = 0; i < pts.Length; i++)
            {
                void Reset() => removePosIndex = i;
                string key = "Beneficial Positions" + i.ToString();
                ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                    (UiString.ConfigWindow_List_Remove.GetDescription(), Reset, ["Delete"]));
                _ = ImGui.Selectable(pts[i].ToString());
                if (ImGui.IsItemHovered()) HoveredPosition = pts[i];
                ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, false,
                    (Reset, [VirtualKey.DELETE]));
            }
            if (removePosIndex > -1)
            {
                var list = new List<Vector3>(pts.Length - 1);
                for (int i = 0; i < pts.Length; i++)
                {
                    if (i == removePosIndex) continue;
                    list.Add(pts[i]);
                }
                OtherConfiguration.BeneficialPositions[territoryId] = [.. list];
                _ = OtherConfiguration.SaveBeneficialPositions();
            }
        }
    }

    internal static void DrawContentFinder(uint imageId)
    {
        const float MaxWidth = 480f;
        uint badge = imageId;
        if (badge != 0
            && IconSet.GetTexture(badge, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? badgeTexture) && badgeTexture?.Handle != null)
        {
            float wholeWidth = ImGui.GetWindowWidth();
            Vector2 size = new Vector2(badgeTexture.Width, badgeTexture.Height) * MathF.Min(1, MathF.Min(MaxWidth, wholeWidth) / badgeTexture.Width);

            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.Image(badgeTexture.Handle, size);
            }, wholeWidth, size.X);
        }
    }

    #endregion

    #region Debug
    private static void DrawDebug()
    {
        _allSearchable.DrawItems(Configs.Debug);

        if (!Player.AvailableThreadSafe || !Service.Config.InDebug)
        {
            return;
        }

        _debugHeader?.Draw();

        if (ImGui.Button("Reset Action Configs"))
        {
            DataCenter.ResetActionConfigs = DataCenter.ResetActionConfigs != true;
        }
        ImGui.Text($"Reset Action Configs: {DataCenter.ResetActionConfigs}");
        if (ImGui.Button("Add Test Warning"))
        {
            BasicWarningHelper.AddSystemWarning("This is a test warning.");
        }
    }

    private static readonly CollapsingHeaderGroup _debugHeader = new(new()
    {
        {() => DataCenter.CurrentRotation != null ? "Loaded Rotation Info" : string.Empty, DrawDebugRotationStatus},
        {() => DataCenter.CurrentRotation != null ? "Base Rotation Info" : string.Empty, DrawDebugBaseStatus},
        {() => "Player Status", DrawStatus },
        {() => "Raise Info", DrawRaiseInfo },
        {() => "Duty Info", DrawDutyInfo },
        {() => "Party", DrawParty },
        {() => "Target Data", DrawTargetData },
        {() => "Next Action", DrawNextAction },
        {() => "Last Action", DrawLastAction },
        {() => "IPC Testing", DrawIPC },
        {() => "Effect", () =>
            {
                ImGui.Text(Watcher.ShowStrSelf);
                ImGui.Separator();
                ImGui.Text(DataCenter.Role.ToString());
            } },
    });

    private static void DrawDebugRotationStatus()
    {
        DataCenter.CurrentRotation?.DisplayRotationStatus();
    }

    private static void DrawDebugBaseStatus()
    {
        DataCenter.CurrentRotation?.DisplayBaseStatus();
    }

    private static unsafe void DrawStatus()
    {
        ImGui.Text($"PlayerSyncedLevel: {DataCenter.PlayerSyncedLevel()}");
        ImGui.Text($"PlayerUnsyncedLevel: {DataCenter.PlayerUnsyncedLevel}");
        ImGui.Text($"Merged Status: {DataCenter.MergedStatus}");
        ImGui.Text($"PlayerHasLockActions: {ActionUpdater.PlayerHasLockActions()}");
        ImGui.Text($"Height: {Player.Character->ModelContainer.CalculateHeight()}");
        ImGui.Text($"AutoFaceTargetOnActionSetting: {DataCenter.AutoFaceTargetOnActionSetting()}");
        ImGui.Text($"MoveModeSetting: {DataCenter.MoveModeSetting()}");
        Dalamud.Game.ClientState.Conditions.ConditionFlag[] conditions = [.. Svc.Condition.AsReadOnlySet()];
        ImGui.Text("InternalCondition:");
        foreach (Dalamud.Game.ClientState.Conditions.ConditionFlag condition in conditions)
        {
            ImGui.Text($"    {condition}");
        }
        ImGui.Text($"OnlineStatus: {Player.OnlineStatus}");
        ImGui.Text($"CanBeRaised: {Player.Object.CanBeRaised()}");
        ImGui.Text($"Current Hp: {Player.Object.CurrentHp}");
        ImGui.Text($"Effective Hp: {ObjectHelper.GetEffectiveHp(Player.Object)}");
        ImGui.Text($"Effective Hp Percent: {ObjectHelper.GetEffectiveHpPercent(Player.Object)}");
        ImGui.Text($"IsDead: {Player.Object.IsDead}");
        ImGui.Text($"DoomNeedHealing: {Player.Object.DoomNeedHealing()}");
        ImGui.Text($"Dead Time: {DataCenter.DeadTimeRaw}");
        ImGui.Text($"Alive Time: {DataCenter.AliveTimeRaw}");
        ImGui.Text($"Moving: {DataCenter.IsMoving}");
        ImGui.Text($"Moving Time: {DataCenter.MovingRaw}");
        ImGui.Text($"Stop Moving: {DataCenter.StopMovingRaw}");
        ImGui.Text($"CountDownTime: {Service.CountDownTime}");
        ImGui.Text($"Combo Time: {DataCenter.ComboTime}");
        ImGui.Text($"TargetingType: {DataCenter.TargetingType}");
        ImGui.Spacing();
        ImGui.Text($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}");
        ImGui.Text($"AttackedTargets: {DataCenter.AttackedTargets?.Count ?? 0}");
        if (DataCenter.AttackedTargets != null)
        {
            foreach ((ulong id, DateTime time) in DataCenter.AttackedTargets)
            {
                ImGui.Text(id.ToString() ?? "Unknown ID");
            }
        }

        // VFX info
        //ImGui.Text("VFX Data:");
        //foreach (var item in DataCenter.VfxDataQueue)
        //{
        //    ImGui.Text(item.ToString());
        //}

        // Check and display VFX casting status
        //ImGui.Text($"Is Casting Tank VFX: {DataCenter.IsCastingTankVfx()}");
        //ImGui.Text($"Is Casting Area VFX: {DataCenter.IsCastingAreaVfx()}");
        //ImGui.Text($"Is Hostile Casting Stop: {DataCenter.IsHostileCastingStop}");
        //ImGui.Text($"VfxDataQueue: {DataCenter.VfxDataQueue.Count}");

        // Check and display VFX casting status
        ImGui.Text("Casting Vfx:");
        List<VfxNewData> filteredVfx = [];
        foreach (VfxNewData s in DataCenter.VfxDataQueue)
        {
            if (s.Path.StartsWith("vfx/lockon/eff/") && s.TimeDuration.TotalSeconds > 0 && s.TimeDuration.TotalSeconds < 6)
            {
                filteredVfx.Add(s);
            }
        }
        foreach (VfxNewData vfx in filteredVfx)
        {
            ImGui.Text($"Path: {vfx.Path}");
        }

        // Display all party members
        List<IBattleChara> partyMembers = DataCenter.PartyMembers;
        if (partyMembers.Count != 0)
        {
            ImGui.Text("Party Members:");
            foreach (IBattleChara member in partyMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Party Members: None");
        }

        List<IBattleChara> tankPartyMembers = [];
        foreach (var member in DataCenter.PartyMembers)
        {
            if (member.IsJobCategory(JobRole.Tank))
                tankPartyMembers.Add(member);
        }
        if (tankPartyMembers.Count != 0)
        {
            ImGui.Text("Tank Party Members:");
            foreach (IBattleChara? member in tankPartyMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Tank Party Members: None");
        }

        // Display dispel target
        IBattleChara? dispelTarget = DataCenter.DispelTarget;
        if (dispelTarget != null)
        {
            ImGui.Text("Dispel Target:");
            ImGui.Text($"- {dispelTarget.Name}");
        }
        else
        {
            ImGui.Text("Dispel Target: None");
        }

        ImGui.Text($"DPSTaken: {DataCenter.DPSTaken}");
        ImGui.Text($"CurrentRotation: {DataCenter.CurrentRotation}");
        ImGui.Text($"Job: {DataCenter.Job}");
        ImGui.Text($"JobRange: {DataCenter.JobRange}");
        ImGui.Text($"Job Role: {DataCenter.Role}");
        ImGui.Text($"Have pet: {DataCenter.HasPet()}");
        ImGui.Text($"Hostile Near Count: {DataCenter.NumberOfHostilesInRange}");
        ImGui.Text($"Hostile Near Count Max Range: {DataCenter.NumberOfHostilesInMaxRange}");
        ImGui.Text($"Have Companion: {DataCenter.HasCompanion}");
        ImGui.Text($"MP: {DataCenter.CurrentMp}");
        ImGui.Text($"Count Down: {Service.CountDownTime}");

        ImGui.Spacing();
        ImGui.Text($"Statuses:");
        foreach (Dalamud.Game.ClientState.Statuses.Status status in Player.Object.StatusList)
        {
            string source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
            byte stacks = Player.Object.StatusStack(true, (StatusID)status.StatusId);
            string stackDisplay = stacks == byte.MaxValue ? "N/A" : stacks.ToString(); // Convert 255 to "N/A"
            ImGui.Text($"{status.GameData.Value.Name}: {status.StatusId} From: {source} Stacks: {stackDisplay}");
        }
    }

    private static void DrawRaiseInfo()
    {
        ImGui.Text($"Can Raise: {DataCenter.CanRaise()}");
        ImGui.Text($"Death Target: {DataCenter.DeathTarget}");

        IEnumerable<IBattleChara> deadPartyMembers = DataCenter.PartyMembers.GetDeath();
        bool hasDeadParty = false;
        using (var enumerator = deadPartyMembers.GetEnumerator())
        {
            if (enumerator.MoveNext()) hasDeadParty = true;
        }
        if (hasDeadParty)
        {
            ImGui.Text("Dead Party Members:");
            foreach (var member in deadPartyMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Dead Party Members: None");
        }

        IEnumerable<IBattleChara> deadAllianceMembers = DataCenter.AllianceMembers.GetDeath();
        bool hasDeadAlliance = false;
        using (var enumerator = deadAllianceMembers.GetEnumerator())
        {
            if (enumerator.MoveNext()) hasDeadAlliance = true;
        }
        if (hasDeadAlliance)
        {
            ImGui.Text("Dead Alliance Members:");
            foreach (var member in deadAllianceMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Dead Alliance Members: None");
        }
    }

    private static unsafe void DrawDutyInfo()
    {
        ImGui.Spacing();
        ImGui.Text($"DC State: {DataCenter.State}");
        ImGui.Text($"Your combat state: {DataCenter.InCombat}");
        ImGui.Text($"Combat Time: {DataCenter.CombatTimeRaw}");
        ImGui.Text($"TerritoryID: {DataCenter.TerritoryID}");
        ImGui.Text($"TerritoryType: {DataCenter.Territory?.ContentType}");
        ImGui.Text($"Is in Alliance Raid: {DataCenter.IsInAllianceRaid}");
        ImGui.Spacing();
        ImGui.Text($"IsPvP: {DataCenter.IsPvP}");
        ImGui.Text($"IsInFate: {DataCenter.IsInFate}");
        if ((IntPtr)FateManager.Instance() != IntPtr.Zero)
        {
            ImGui.Text($"Fate ID: {DataCenter.PlayerFateId}");
        }
        ImGui.Spacing();
        ImGui.Text($"IsInBozjanFieldOp: {DataCenter.IsInBozjanFieldOp}");
        ImGui.Text($"IsInBozjanFieldOpCE: {DataCenter.IsInBozjanFieldOpCE}");
        ImGui.Text($"IsInDelubrumNormal: {DataCenter.IsInDelubrumNormal}");
        ImGui.Text($"IsInDelubrumSavage: {DataCenter.IsInDelubrumSavage}");
        ImGui.Text($"IsInBozja: {DataCenter.IsInBozja}");
        ImGui.Spacing();
        ImGui.Text($"In Occult Crescent: {DataCenter.IsInOccultCrescentOp}");
        ImGui.Text($"Is In ForkedTower: {DataCenter.IsInForkedTower}");
        ImGui.Text($"FreelancerLevel: {DutyRotation.FreelancerLevel}");
        ImGui.Text($"KnightLevel: {DutyRotation.KnightLevel}");
        ImGui.Text($"MonkLevel: {DutyRotation.MonkLevel}");
        ImGui.Text($"BardLevel: {DutyRotation.BardLevel}");
        ImGui.Text($"ChemistLevel: {DutyRotation.ChemistLevel}");
        ImGui.Text($"TimeMageLevel: {DutyRotation.TimeMageLevel}");
        ImGui.Text($"CannoneerLevel: {DutyRotation.CannoneerLevel}");
        ImGui.Text($"OracleLevel: {DutyRotation.OracleLevel}");
        ImGui.Text($"BerserkerLevel: {DutyRotation.BerserkerLevel}");
        ImGui.Text($"RangerLevel: {DutyRotation.RangerLevel}");
        ImGui.Text($"ThiefLevel: {DutyRotation.ThiefLevel}");
        ImGui.Text($"SamuraiLevel: {DutyRotation.SamuraiLevel}");
        ImGui.Text($"GeomancerLevel: {DutyRotation.GeomancerLevel}");
        ImGui.Spacing();
        ImGui.Text($"InVariantDungeon: {DataCenter.InVariantDungeon}");
        ImGui.Text($"AloaloIsland: {DataCenter.AloaloIsland}");
        ImGui.Text($"MountRokkon: {DataCenter.MountRokkon}");
        ImGui.Text($"SildihnSubterrane: {DataCenter.SildihnSubterrane}");
        ImGui.Spacing();
        ImGui.Text($"IsCastingMultiHit: {DataCenter.IsCastingMultiHit()}");
        ImGui.Text($"IsCastingTankVfx: {DataCenter.IsCastingTankVfx()}");
        ImGui.Text($"IsCastingAreaVfx: {DataCenter.IsCastingAreaVfx()}");
        ImGui.Text($"AreHostilesCastingKnockback: {DataCenter.AreHostilesCastingKnockback}");
        ImGui.Text($"IsHostileCastingAOE: {DataCenter.IsHostileCastingAOE}");
        ImGui.Text($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}");
        ImGui.Text($"IsHostileCastingStop: {DataCenter.IsHostileCastingStop}");
    }

    private static unsafe void DrawParty()
    {
        ImGui.Text($"Number of Party Members: {DataCenter.PartyMembers.Count}");
        ImGui.Text($"Number of Alliance Members: {DataCenter.AllianceMembers.Count}");
        ImGui.Text($"Average Party HP Percent: {DataCenter.PartyMembersAverHP * 100}");
        ImGui.Text($"Average Lowest Party HP Percent: {DataCenter.LowestPartyMembersAverHP * 100}");
        int doomedCount = 0;
        foreach (var member in DataCenter.PartyMembers)
        {
            if (member.DoomNeedHealing()) doomedCount++;
        }
        ImGui.Text($"Number of Party Members with Doomed To Heal status: {doomedCount}");


        // AST-only card target preview
        if (Player.Object.IsJobs(Job.AST))
        {
            IBattleChara? spear = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheSpear, 0, SpecialActionType.None);
            IBattleChara? balance = ActionTargetInfo.FindTargetByType(DataCenter.PartyMembers, TargetType.TheBalance, 0, SpecialActionType.None);
            ImGui.Spacing();
            ImGui.Text("AST Card Targets (Preview):");
            ImGui.Text($"- The Spear: {spear?.Name ?? "None"}");
            ImGui.Text($"- The Balance: {balance?.Name ?? "None"}");
            ImGui.Spacing();
        }

        foreach (Dalamud.Game.ClientState.Party.IPartyMember p in Svc.Party)
        {
            if (p.GameObject is not IBattleChara b)
            {
                continue;
            }

            string text = $"Name: {b.Name}, In Combat: {b.InCombat()}";
            if (b.TimeAlive() > 0)
            {
                text += $", Time Alive: {b.TimeAlive()}";
            }

            if (b.TimeDead() > 0)
            {
                text += $", Time Dead: {b.TimeDead()}";
            }

            ImGui.Text(text);
        }
        ImGui.Spacing();
        ImGui.Text($"Limit Break: {CustomRotation.LimitBreakLevel}");
        ImGui.Spacing();
        ImGui.Text($"Object Data");
        ImGui.Text($"NumberOfPartyMembersInRangeOf 5m: {DataCenter.NumberOfPartyMembersInRangeOf(5)}");
        ImGui.Text($"AllTargets Count: {DataCenter.AllTargets.Count}");
        ImGui.Text($"AllHostileTargets Count: {DataCenter.AllHostileTargets.Count}");
        foreach (IBattleChara item in DataCenter.AllHostileTargets)
        {
            ImGui.Text(item.Name.ToString());
        }
        ImGui.Spacing();
        ImGui.Text($"Party Composition:");
        var party = CustomRotation.PartyComposition;
        if (party.Count == 0)
        {
            ImGui.Text("No party members.");
        }
        else
        {
            for (int i = 0; i < party.Count; i++)
            {
                // Assuming RowRef<ClassJob> has a .Value property with a .Name or .Abbreviation
                var classJob = party[i].Value;
                string jobName = classJob.Abbreviation.ToString() ?? classJob.Name.ToString() ?? $"Job #{i}";
                ImGui.Text($"{i + 1}: {jobName}");
            }
        }
        ImGui.Spacing();
        float mitigationFraction = CustomRotation.GetCurrentMitigationPercent(); // 0.0–0.95
        ImGui.Text($"Current Mitigation Percent: {mitigationFraction * 100f:F1}%");
        ImGui.Text($"Current Mitigation Percent RAW: {mitigationFraction}");

        ImGui.Text($"Is Magical Damage Incoming: {CustomRotation.IsMagicalDamageIncoming()}");
    }

    private static unsafe void DrawTargetData()
    {
        if (Svc.Targets.Target is not IBattleChara target)
        {
            return;
        }

        ImGui.Text($"Height: {target.Struct()->Height}");
        ImGui.Text($"Kind: {target.GetObjectKind()}");
        ImGui.Text($"SubKind: {target.GetBattleNPCSubKind()}");

        IGameObject? owner = Svc.Objects.SearchById(target.OwnerId);
        if (owner != null)
        {
            ImGui.Text($"Owner: {owner.Name}");
        }

        if (target is IBattleChara battleChara)
        {
            ImGui.Text($"Is Status Capped: {StatusHelper.IsStatusCapped(battleChara)}");
            ImGui.Text($"CanSee: {battleChara.CanSee()}");
            ImGui.Text($"CanBeRaised: {battleChara.CanBeRaised()}");
            ImGui.Text($"HP: {battleChara.CurrentHp} / {battleChara.MaxHp}");
            ImGui.Text($"HealthRatio: {battleChara.GetHealthRatio()}");
            ImGui.Spacing();
            ImGui.Text($"NamePlate Icon ID: {battleChara.GetNamePlateIcon()}");
            ImGui.Text($"Event Type: {battleChara.GetEventType()}");
            ImGui.Text($"TargetCharaCondition: {battleChara.TargetCharaCondition()}");
            //ImGui.Text($"GetMarkerNumber: {MarkingHelper.GetMarkerNumber((long)battleChara.GameObjectId)}");
            ImGui.Text($"Name Id: {battleChara.NameId}");
            ImGui.Text($"Data Id: {battleChara.BaseId}");
            ImGui.Spacing();
            ImGui.Text($"Is Attackable: {battleChara.IsAttackable()}");
            ImGui.Text($"Is Others Players Mob: {battleChara.IsOthersPlayersMob()}");
            ImGui.Text($"Is Alliance: {battleChara.IsAllianceMember()}");
            ImGui.Text($"Is Enemy Action Check: {battleChara.IsEnemy()}");
            ImGui.Text($"IsSpecialExecptionImmune: {battleChara.IsSpecialExceptionImmune()}");
            ImGui.Text($"IsSpecialImmune: {battleChara.IsSpecialImmune()}");
            ImGui.Text($"IsTopPriorityNamedHostile: {battleChara.IsTopPriorityNamedHostile()}");
            ImGui.Text($"IsTopPriorityHostile: {battleChara.IsTopPriorityHostile()}");
            ImGui.Spacing();
            ImGui.Text($"FateID: {battleChara.FateId().ToString() ?? string.Empty}");
            ImGui.Text($"EventType: {battleChara.GetEventType().ToString() ?? string.Empty}");
            ImGui.Text($"IsBozjanCEFateMob: {battleChara.IsBozjanCEMob()}");
            ImGui.Spacing();
            ImGui.Text($"IsOccultCEMob: {battleChara.IsOccultCEMob()}");
            ImGui.Text($"IsOccultFateMob: {battleChara.IsOccultFateMob()}");
            ImGui.Text($"IsOCUndeadTarget: {battleChara.IsOCUndeadTarget()}");
            ImGui.Text($"IsOCSlowgaImmuneTarget: {battleChara.IsOCSlowgaImmuneTarget()}");
            ImGui.Text($"IsOCDoomImmuneTarget: {battleChara.IsOCDoomImmuneTarget()}");
            ImGui.Text($"IsOCStunImmuneTarget: {battleChara.IsOCStunImmuneTarget()}");
            ImGui.Text($"IsOCFreezeImmuneTarget: {battleChara.IsOCFreezeImmuneTarget()}");
            ImGui.Text($"IsOCBlindImmuneTarget: {battleChara.IsOCBlindImmuneTarget()}");
            ImGui.Text($"IsOCParalysisImmuneTarget: {battleChara.IsOCParalysisImmuneTarget()}");
            ImGui.Spacing();
            ImGui.Text($"Is Current Focus Target: {battleChara.IsFocusTarget()}");
            ImGui.Text($"TTK: {battleChara.GetTTK()}");
            ImGui.Text($"Is Boss TTK: {battleChara.IsBossFromTTK()}");
            ImGui.Text($"Is Boss Icon: {battleChara.IsBossFromIcon()}");
            ImGui.Text($"Rank: {battleChara.GetObjectNPC()?.Rank.ToString() ?? string.Empty}");
            ImGui.Text($"Has Positional: {battleChara.HasPositional()}");
            ImGui.Text($"IsNpcPartyMember: {battleChara.IsNpcPartyMember()}");
            ImGui.Text($"IsPlayerCharacterChocobo: {battleChara.IsPlayerCharacterChocobo()}");
            ImGui.Text($"IsFriendlyBattleNPC: {battleChara.IsFriendlyBattleNPC()}");
            ImGui.Text($"Is Dying: {battleChara.IsDying()}");
            ImGui.Text($"Is Alive: {battleChara.IsAlive()}");
            ImGui.Text($"Is Party: {battleChara.IsParty()}");
            ImGui.Text($"Is Healer: {battleChara.IsJobCategory(JobRole.Healer)}");
            ImGui.Text($"Is DPS: {battleChara.IsJobCategory(JobRole.AllDPS)}");
            ImGui.Text($"Is Tank: {battleChara.IsJobCategory(JobRole.Tank)}");
            ImGui.Text($"Is Alliance: {battleChara.IsAllianceMember()}");
            ImGui.Text($"Distance To Player: {battleChara.DistanceToPlayer()}");
            ImGui.Text($"CanProvoke: {battleChara.CanProvoke()}");
            ImGui.Text($"StatusFlags: {battleChara.StatusFlags}");
            ImGui.Text($"InView: {Svc.GameGui.WorldToScreen(battleChara.Position, out _)}");
            ImGui.Text($"Enemy Positional: {battleChara.FindEnemyPositional()}");
            ImGui.Text($"NameplateKind: {battleChara.GetNameplateKind()}");
            ImGui.Text($"BattleNPCSubKind: {battleChara.GetBattleNPCSubKind()}");
            ImGui.Text($"Is Top Priority Hostile: {battleChara.IsTopPriorityHostile()}");
            ImGui.Text($"Targetable: {battleChara.Struct()->Character.GameObject.TargetableStatus}");
            ImGui.Spacing();
            ImGui.Text($"Statuses:");
            foreach (Dalamud.Game.ClientState.Statuses.Status status in battleChara.StatusList)
            {
                string source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
                ImGui.Text($"{status.GameData.Value.Name}: {status.StatusId} From: {source}");
            }
        }
    }

    private static void DrawNextAction()
    {
        ImGui.Text(DataCenter.CurrentRotation?.GetAttributes()?.Name);
        ImGui.Text(DataCenter.SpecialType.ToString());

        ImGui.Text(ActionUpdater.NextAction?.Name ?? "null");
        ImGui.Text($"GCD Total: {DataCenter.DefaultGCDTotal}");
        ImGui.Text($"GCD Remain: {DataCenter.DefaultGCDRemain}");
        ImGui.Text($"GCD Elapsed: {DataCenter.DefaultGCDElapsed}");
        ImGui.Text($"Calculated Action Ahead: {DataCenter.CalculatedActionAhead}");
        ImGui.Text($"Animation Lock Delay: {DataCenter.AnimationLock}");
    }

    private static void DrawLastAction()
    {
        DrawAction(DataCenter.LastAction, nameof(DataCenter.LastAction));
        DrawAction(DataCenter.LastAbility, nameof(DataCenter.LastAbility));
        DrawAction(DataCenter.LastGCD, nameof(DataCenter.LastGCD));
        DrawAction(DataCenter.LastComboAction, nameof(DataCenter.LastComboAction));
        ImGui.Text($"IsLastActionAbility: {IActionHelper.IsLastActionAbility()}");
        ImGui.Text($"IsLastActionGCD: {IActionHelper.IsLastActionGCD()}");
    }

    private static string _ipcTestText = "Sent data";

    private static void DrawIPC()
    {
        ImGui.SetNextItemWidth(200 * Scale);
        ImGui.InputText("##IPCTextBox", ref _ipcTestText, 128);
        ImGui.SameLine();
        if (ImGui.Button("Test Function"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.Test(_ipcTestText);
        }

        if (ImGui.Button("Test ChangeOperatingMode to Manual IPC"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.ChangeOperatingMode(StateCommandType.Manual);
        }

        if (ImGui.Button("Test ChangeOperatingMode to Off IPC"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.ChangeOperatingMode(StateCommandType.Off);
        }

        if (ImGui.Button("Test TriggerSpecialState DefenseArea IPC"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.TriggerSpecialState(SpecialCommandType.DefenseArea);
        }

        if (ImGui.Button("Test TriggerSpecialState AntiKnockback IPC"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.TriggerSpecialState(SpecialCommandType.AntiKnockback);
        }

        if (ImGui.Button("Test Setting IPC (Changing engage setting to All Target)"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.OtherCommand(OtherCommandType.Settings, "HostileType AllTargetsCanAttack");
        }

        if (ImGui.Button("Test OtherCommand DoAction IPC (Magick Barrier on RDM)"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.OtherCommand(OtherCommandType.DoActions, "Magick Barrier-5");
        }

        if (ImGui.Button("Test ToggleAction IPC (Magick Barrier on RDM)"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.OtherCommand(OtherCommandType.ToggleActions, "Magick Barrier");
        }

        if (ImGui.Button("Test ActionCommand IPC (Magick Barrier on RDM)"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.ActionCommand("Magick Barrier", 7);
        }
        if (ImGui.Button("Test AutodutyChangeOperatingMode IPC (AutoDuty, HighHPPercent)"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.AutodutyChangeOperatingMode(StateCommandType.AutoDuty, TargetingType.HighHPPercent);
        }
        if (ImGui.Button("Test Henchman IPC support"))
        {
            IPCProvider ipcProvider = new();
            ipcProvider.ChangeOperatingMode(StateCommandType.Henched);
        }
    }

    private static void DrawAction(ActionID id, string type)
    {
        ImGui.Text($"{type}: {id}");
    }

    private static bool BeginChild(string str_id, Vector2 size)
    {
        return !IsFailed() && ImGui.BeginChild(str_id, size);
    }

    private static bool BeginChild(string str_id, Vector2 size, bool border, ImGuiWindowFlags flags)
    {
        return !IsFailed() && ImGui.BeginChild(str_id, size, border, flags);
    }

    private static bool IsFailed()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        float min = style.WindowPadding.X + style.WindowBorderSize;
        float columnWidth = ImGui.GetColumnWidth();
        Vector2 windowSize = ImGui.GetWindowSize();
        Vector2 cursor = ImGui.GetCursorPos();

        return columnWidth > 0 && columnWidth <= min
            || windowSize.Y - cursor.Y <= min
            || windowSize.X - cursor.X <= min;
    }
    #endregion
}
