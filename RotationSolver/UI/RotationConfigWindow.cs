using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ExCSS;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.UI.SearchableConfigs;
using RotationSolver.UI.SearchableSettings;
using RotationSolver.Updaters;
using System.Diagnostics;
using GAction = Lumina.Excel.Sheets.Action;
using Status = Lumina.Excel.Sheets.Status;
using Task = System.Threading.Tasks.Task;

namespace RotationSolver.UI;

public partial class RotationConfigWindow : Window
{
    private static float Scale => ImGuiHelpers.GlobalScale;

    private RotationConfigWindowTab _activeTab;

    private const float MIN_COLUMN_WIDTH = 24;
    private const float JOB_ICON_WIDTH = 50;

    public RotationConfigWindow()
    : base("", ImGuiWindowFlags.NoScrollbar, false)
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(740f, 490f);
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 300),
            MaximumSize = new Vector2(5000, 5000),
        };
        RespectCloseHotkey = true;

        _showText = !Service.Config.HasShownMainMenuMessage; // Show the message if it hasn't been shown before
    }

    public override void OnClose()
    {
        Service.Config.Save();
        ActionSequencerUpdater.SaveFiles();
        base.OnClose();
    }

    public override void Draw()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
        try
        {
            using var table = ImRaii.Table("Rotation Config Table", 2, ImGuiTableFlags.Resizable);
            if (table)
            {
                ImGui.TableSetupColumn("Rotation Config Side Bar", ImGuiTableColumnFlags.WidthFixed, 100 * Scale);
                ImGui.TableNextColumn();

                try
                {
                    DrawSideBar();
                }

                catch (Exception ex)
                {
                    Svc.Log.Warning(ex, "Something wrong with sideBar");
                }

                ImGui.TableNextColumn();

                try
                {
                    DrawBody();
                }

                catch (Exception ex)
                {
                    Svc.Log.Warning(ex, "Something wrong with body");
                }

            }

        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Something wrong with config window.");
        }
    }

    private static void DrawDutyRotation()
    {
        var dutyRotation = DataCenter.CurrentDutyRotation;
        if (dutyRotation == null) return;

        var rot = dutyRotation.GetType().GetCustomAttribute<RotationAttribute>();
        if (rot == null) return;

        if (!RotationUpdater.DutyRotations.TryGetValue(Svc.ClientState.TerritoryType, out var rotations)) return;

        if (rotations == null) return;

        const string popUpId = "Right Duty Rotation Popup";
        if (ImGui.Selectable(rot.Name, false, ImGuiSelectableFlags.None, new Vector2(0, 20)))
        {
            ImGui.OpenPopup(popUpId);
        }
        ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_DutyRotationDesc.GetDescription());

        using var popup = ImRaii.Popup(popUpId);
        if (popup)
        {
            foreach (var type in rotations)
            {
                var r = type.GetCustomAttribute<RotationAttribute>();
                if (r == null) continue;

                if (ImGui.Selectable("None"))
                {
                    Service.Config.DutyRotationChoice[Svc.ClientState.TerritoryType] = string.Empty;
                }

                if (ImGui.Selectable(r.Name) && !string.IsNullOrEmpty(type.FullName))
                {
                    Service.Config.DutyRotationChoice[Svc.ClientState.TerritoryType] = type.FullName;
                }
            }
        }
    }

    private void DrawErrorZone()
    {
        var incompatiblePlugins = DownloadHelper.IncompatiblePlugins ?? Array.Empty<IncompatiblePlugin>();

        bool hasIncompatiblePlugin = incompatiblePlugins.Any(p => p.IsInstalled);

        if (hasIncompatiblePlugin)
        {
            string message = "Disable incompatible plugin";
            var installedPlugin = incompatiblePlugins.FirstOrDefault(p => p.IsInstalled);
            if ((int)installedPlugin.Type == 5)
            {
                message = $"Disable {installedPlugin.Name}, causes targetting issues.";
            }

            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically
            using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange)))
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically
                ImGui.Text(message);
                ImGui.PopTextWrapPos(); // Reset text wrapping position
            }
        }

        if (DataCenter.SystemWarnings != null && DataCenter.SystemWarnings.Any())
        {
            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically
            var warningsToRemove = new List<string>();

            foreach (var warning in DataCenter.SystemWarnings.Keys)
            {
                using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange)))
                {
                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically

                    // Calculate the required height for the button
                    var textSize = ImGui.CalcTextSize(warning, availableWidth);
                    float lineHeight = ImGui.GetTextLineHeight();
                    int lineCount = (int)Math.Ceiling(textSize.X / availableWidth);
                    float buttonHeight = lineHeight * lineCount + ImGui.GetStyle().FramePadding.Y * 2;

                    if (ImGui.Button(warning, new Vector2(availableWidth, buttonHeight)))
                    {
                        warningsToRemove.Add(warning);
                    }

                    ImGui.PopTextWrapPos(); // Reset text wrapping position
                }
            }

            // Remove warnings that were cleared
            foreach (var warning in warningsToRemove)
            {
                DataCenter.SystemWarnings.Remove(warning);
            }
        }

        if (Player.Object != null && (Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
        Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
        Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN))
        {
            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange); // Set text color to DalamudOrange
            ImGui.Text($"You are on an unsupported class: {Player.Job}");
            ImGui.PopStyleColor(); // Reset text color
            ImGui.PopTextWrapPos(); // Reset text wrapping position
        }

        else
        {
            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically
            ImGui.Text("No Errors");
            ImGui.PopTextWrapPos(); // Reset text wrapping position
        }
    }

    private void DrawSideBar()
    {
        using var child = ImRaii.Child("Rotation Solver Side bar", -Vector2.One, false, ImGuiWindowFlags.NoScrollbar);
        if (child)
        {
            var wholeWidth = ImGui.GetWindowSize().X;

            DrawHeader(wholeWidth);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var iconSize = Math.Max(Scale * MIN_COLUMN_WIDTH, Math.Min(wholeWidth, Scale * JOB_ICON_WIDTH)) * 0.6f;

            if (wholeWidth > JOB_ICON_WIDTH * Scale)
            {
                DrawDutyRotation();
                DrawErrorZone();

                ImGui.Separator();
                ImGui.Spacing();

                ImGui.SetNextItemWidth(wholeWidth);
                SearchingBox();

                ImGui.Spacing();
            }
            else
            {
                if (IconSet.GetTexture(46, out var icon))
                {
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        using var popup = ImRaii.Popup("Searching Popup");
                        if (popup)
                        {
                            ImGui.SetNextItemWidth(200 * Scale);
                            SearchingBox();
                            if (ImGui.IsKeyDown(ImGuiKey.Enter))
                            {
                                ImGui.CloseCurrentPopup();
                            }
                        }

                        var cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * iconSize))
                        {
                            ImGui.OpenPopup("Searching Popup");
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, -1);
                        ImguiTooltips.HoveredTooltip("Search");

                    }, Math.Max(Scale * MIN_COLUMN_WIDTH, wholeWidth), iconSize);
                }
            }

            foreach (var item in Enum.GetValues<RotationConfigWindowTab>())
            {
                var incompatiblePlugins = DownloadHelper.IncompatiblePlugins ?? Array.Empty<IncompatiblePlugin>();

                // Skip the tab if it has the TabSkipAttribute
                if (item.GetAttribute<TabSkipAttribute>() != null) continue;

                // Check if the "AutoDuty" plugin is installed
                bool isAutoDutyInstalled = false;
                foreach (var plugin in incompatiblePlugins)
                {
                    if (plugin.IsInstalled && plugin.Name == "AutoDuty")
                    {
                        isAutoDutyInstalled = true;
                        break;
                    }
                }

                // Skip the "AutoDuty" tab if the plugin is not installed
                if (item == RotationConfigWindowTab.AutoDuty && !isAutoDutyInstalled) continue;

                if (IconSet.GetTexture(item.GetAttribute<TabIconAttribute>()?.Icon ?? 0, out var icon) && wholeWidth <= JOB_ICON_WIDTH * Scale)
                {
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        var cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * iconSize, item.ToString()))
                        {
                            _activeTab = item;
                            _searchResults = [];
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, _activeTab == item ? 1 : 0);
                    }, Math.Max(Scale * MIN_COLUMN_WIDTH, wholeWidth), iconSize);

                    var desc = item.ToString();
                    var addition = item.GetDescription();
                    if (!string.IsNullOrEmpty(addition)) desc += "\n \n" + addition;
                    ImguiTooltips.HoveredTooltip(desc);
                }
                else
                {
                    if (ImGui.Selectable(item.ToString(), _activeTab == item, ImGuiSelectableFlags.None, new Vector2(0, 20)))
                    {
                        _activeTab = item;
                        _searchResults = [];
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        var desc = item.GetDescription();
                        if (!string.IsNullOrEmpty(desc)) ImguiTooltips.ShowTooltip(desc);
                    }
                }

                // Add a separator after the "Debug" tab
                if (item == RotationConfigWindowTab.Debug)
                {
                    ImGui.Separator();
                }
            }
        }
    }

    private bool _showText;

    private void DrawHeader(float wholeWidth)
    {
        var size = MathF.Max(MathF.Min(wholeWidth, Scale * 128), Scale * MIN_COLUMN_WIDTH);

        if (IconSet.GetTexture((uint)0, out var overlay))
        {
            if (_showText) // Conditionally render the text
            {
                ImGui.TextWrapped("Click RSR icon for main menu.");
                ImGui.Spacing();
            }

            ImGuiHelper.DrawItemMiddle(() =>
            {
                var cursor = ImGui.GetCursorPos();

                if (ImGuiHelper.SilenceImageButton(overlay.ImGuiHandle, Vector2.One * size,
                    _activeTab == RotationConfigWindowTab.About, "About Icon"))
                {
                    _activeTab = RotationConfigWindowTab.About;
                    _searchResults = [];
                    _showText = false; // Update the flag when the icon is clicked

                    // Save the configuration to indicate that the message has been shown
                    Service.Config.HasShownMainMenuMessage = true;
                    Service.Config.Save();
                }
                ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_About_Punchline.GetDescription());

                var logoUrl = $"https://raw.githubusercontent.com/{Service.USERNAME}/{Service.REPO}/main/Images/Logo.png";
                if (ThreadLoadImageHandler.TryGetTextureWrap(logoUrl, out var logo))
                {
                    ImGui.SetCursorPos(cursor);
                    ImGui.Image(logo.ImGuiHandle, Vector2.One * size);
                }
            }, wholeWidth, size);

            ImGui.Spacing();
        }

        var rotation = DataCenter.CurrentRotation;

        if (rotation == null && !(Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
        Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
        Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);

            var text = UiString.ConfigWindow_NoRotation.GetDescription();
            if (text == null)
            {
                Svc.Log.Error("UiString.ConfigWindow_NoRotation.GetDescription() returned null.");
                return;
            }

            var textWidth = ImGuiHelpers.GetButtonSize(text).X;
            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.TextWrapped(text);
            }, wholeWidth, textWidth);
            ImGui.PopStyleColor();
            ImguiTooltips.HoveredTooltip("Please update your rotations!");
            return;
        }

        if (rotation == null && (Player.Job == Job.CRP || Player.Job == Job.BSM || Player.Job == Job.ARM || Player.Job == Job.GSM ||
            Player.Job == Job.LTW || Player.Job == Job.WVR || Player.Job == Job.ALC || Player.Job == Job.CUL ||
            Player.Job == Job.MIN || Player.Job == Job.FSH || Player.Job == Job.BTN))
        {
            float availableWidth = ImGui.GetContentRegionAvail().X; // Get the available width dynamically
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + availableWidth); // Set text wrapping position dynamically
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange); // Set text color to DalamudOrange
            ImGui.Text(":(");
            ImGui.PopStyleColor(); // Reset text color
            ImGui.PopTextWrapPos(); // Reset text wrapping position
            return;
        }

        Type[] rotations = Array.Empty<Type>();
        foreach (var customRotation in RotationUpdater.CustomRotations)
        {
            if (customRotation.ClassJobIds.Contains((Job)(Player.Object?.ClassJob.RowId ?? 0)))
            {
                rotations = customRotation.Rotations;
                break;
            }
        }

        if (rotation != null)
        {
            var rot = rotation.GetType().GetCustomAttribute<RotationAttribute>();

            if (rot == null) return;

            if (DataCenter.IsPvP)
            {
                rotations = rotations.Where(r => r.GetCustomAttribute<RotationAttribute>()?.Type.HasFlag(CombatType.PvP) ?? false).ToArray();
            }
            else
            {
                rotations = rotations.Where(r => r.GetCustomAttribute<RotationAttribute>()?.Type.HasFlag(CombatType.PvE) ?? false).ToArray();
            }

            var iconSize = Math.Max(Scale * MIN_COLUMN_WIDTH, Math.Min(wholeWidth, Scale * JOB_ICON_WIDTH));
            var comboSize = ImGui.CalcTextSize(rot.Name).X;

            const string slash = " - ";
            var gameVersionSize = ImGui.CalcTextSize(slash + rot.GameVersion).X + ImGui.GetStyle().ItemSpacing.X;
            var gameVersion = UiString.ConfigWindow_Helper_GameVersion.GetDescription() + ": ";
            var drawCenter = ImGui.CalcTextSize(slash + gameVersion + rot.GameVersion).X + iconSize + ImGui.GetStyle().ItemSpacing.X * 3 < wholeWidth;
            if (drawCenter) gameVersionSize += ImGui.CalcTextSize(gameVersion).X + ImGui.GetStyle().ItemSpacing.X;

            var horizonalWholeWidth = Math.Max(comboSize, gameVersionSize) + iconSize + ImGui.GetStyle().ItemSpacing.X;

            if (horizonalWholeWidth > wholeWidth)
            {
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    DrawRotationIcon(rotation, iconSize);
                }, wholeWidth, iconSize);

                if (Scale * JOB_ICON_WIDTH < wholeWidth)
                {
                    DrawRotationCombo(comboSize, rotations, rotation, gameVersion);
                }
            }
            else
            {
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    DrawRotationIcon(rotation, iconSize);

                    ImGui.SameLine();

                    using var group = ImRaii.Group();

                    DrawRotationCombo(comboSize, rotations, rotation, gameVersion);
                    ImGui.TextDisabled(slash);
                    ImGui.SameLine();

                    if (drawCenter)
                    {
                        ImGui.TextDisabled(gameVersion);
                        ImGui.SameLine();
                    }
                    ImGui.Text(rot.GameVersion);

                }, wholeWidth, horizonalWholeWidth);
            }
        }
    }

    private void DrawRotationIcon(ICustomRotation rotation, float iconSize)
    {
        var cursor = ImGui.GetCursorPos();

        // Check if the rotation texture is available
        if (rotation.GetTexture(out var jobIcon) && ImGuiHelper.SilenceImageButton(jobIcon.ImGuiHandle, Vector2.One * iconSize, _activeTab == RotationConfigWindowTab.Rotation))
        {
            _activeTab = RotationConfigWindowTab.Rotation;
            _searchResults = Array.Empty<ISearchable>(); // Corrected type
        }

        // Show tooltip if the item is hovered
        if (ImGui.IsItemHovered())
        {
            ImguiTooltips.ShowTooltip(() =>
            {
                var rotationType = rotation.GetType();
                var rotationAttribute = rotationType.GetCustomAttribute<RotationAttribute>();

                if (rotationAttribute != null)
                {
                    ImGui.Text($"{rotation.Name} ({rotationAttribute.Name})");
                    rotationAttribute.Type.Draw();

                    if (!string.IsNullOrEmpty(rotation.Description))
                    {
                        ImGui.Text(rotation.Description);
                    }
                }
            });
        }

        // Check if the icon texture is available
        var rotationTypeAttribute = rotation.GetType().GetCustomAttribute<RotationAttribute>();
        if (rotationTypeAttribute != null && IconSet.GetTexture(rotationTypeAttribute.Type.GetIcon(), out var texture))
        {
            ImGui.SetCursorPos(cursor + Vector2.One * iconSize / 2);
            ImGui.Image(texture.ImGuiHandle, Vector2.One * iconSize / 2);
        }
    }

    private static void DrawRotationCombo(float comboSize, Type[] rotations, ICustomRotation rotation, string gameVersion)
    {
        ImGui.SetNextItemWidth(comboSize);
        const string popUp = "Rotation Solver Select Rotation";

        var rot = rotation.GetType().GetCustomAttribute<RotationAttribute>();
        if (rot == null)
        {
            // Handle the case where the attribute is not found
            ImGui.Text("Rotation attribute not found.");
            return;
        }

        using (var color = ImRaii.PushColor(ImGuiCol.Text, rotation.GetColor()))
        {
            if (ImGui.Selectable(rot.Name + "##RotationName:" + rotation.Name))
            {
                if (!ImGui.IsPopupOpen(popUp)) ImGui.OpenPopup(popUp);
            }
        }

        using (var popup = ImRaii.Popup(popUp))
        {
            if (popup)
            {
                foreach (var r in rotations)
                {
                    var rAttr = r.GetCustomAttribute<RotationAttribute>();
                    if (rAttr == null) continue;

                    if (IconSet.GetTexture(rAttr.Type.GetIcon(), out var texture))
                    {
                        ImGui.Image(texture.ImGuiHandle, Vector2.One * 20 * Scale);
                        if (ImGui.IsItemHovered())
                        {
                            ImguiTooltips.ShowTooltip(() =>
                            {
                                rotation.GetType().GetCustomAttribute<RotationAttribute>()?.Type.Draw();
                            });
                        }
                    }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, r.GetCustomAttribute<BetaRotationAttribute>() == null
                        ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudOrange);
                    if (ImGui.Selectable(rAttr.Name))
                    {
                        if (DataCenter.IsPvP)
                        {
                            Service.Config.PvPRotationChoice = r.FullName;
                        }
                        else
                        {
                            Service.Config.RotationChoice = r.FullName;
                        }
                        Service.Config.Save();
                    }
                    ImguiTooltips.HoveredTooltip(rAttr.Description);
                    ImGui.PopStyleColor();
                }
            }
        }

        var warning = gameVersion + rot.GameVersion;
        if (!rotation.IsValid) warning += "\n" + string.Format(UiString.ConfigWindow_Rotation_InvalidRotation.GetDescription(),
                rotation.GetType().Assembly.GetInfo().Author);

        if (rotation.IsBeta()) warning += "\n" + UiString.ConfigWindow_Rotation_BetaRotation.ToString();

        warning += "\n \n" + UiString.ConfigWindow_Helper_SwitchRotation.GetDescription();
        ImguiTooltips.HoveredTooltip(warning);
    }

    private void DrawBody()
    {
        // Adjust cursor position
        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 8 * Scale);

        // Create a child window for the body content
        using var child = ImRaii.Child("Rotation Solver Body", -Vector2.One);
        if (child)
        {
            // Check if there are search results to display
            if (_searchResults != null && _searchResults.Length != 0)
            {
                // Display search results header
                using (var font = ImRaii.PushFont(FontManager.GetFont(18)))
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
                    ImGui.TextWrapped(UiString.ConfigWindow_Search_Result.GetDescription());
                }

                ImGui.Spacing();

                // Display each search result
                foreach (var searchable in _searchResults)
                {
                    searchable?.Draw();
                }
            }
            else
            {
                // Display content based on the active tab
                switch (_activeTab)
                {

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

                    case RotationConfigWindowTab.Rotations:
                        DrawRotations();
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

    #region About
    private static readonly SortedList<uint, string> CountStringPair = new()
{
    { 100_000, UiString.ConfigWindow_About_Clicking100k.GetDescription() },
    { 500_000, UiString.ConfigWindow_About_Clicking500k.GetDescription() },
};

    private static void DrawAbout()
    {
        // Draw the punchline with a specific font and color
        using (var font = ImRaii.PushFont(FontManager.GetFont(18)))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
            ImGui.TextWrapped(UiString.ConfigWindow_About_Punchline.GetDescription());
        }

        ImGui.Spacing();

        // Draw the description
        ImGui.TextWrapped(UiString.ConfigWindow_About_Description.GetDescription());

        ImGui.Spacing();

        // Draw the warning with a specific color
        using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange)))
        {
            ImGui.TextWrapped(UiString.ConfigWindow_About_Warning.GetDescription());
        }

        var width = ImGui.GetWindowWidth();

        // Draw the Discord link button
        if (IconSet.GetTexture("https://discordapp.com/api/guilds/1064448004498653245/embed.png?style=banner2", out var icon) && ImGuiHelper.TextureButton(icon, width, width))
        {
            Util.OpenLink("https://discord.gg/p54TZMPnC9");
        }

        var clickingCount = OtherConfiguration.RotationSolverRecord.ClickingCount;
        if (clickingCount > 0)
        {
            // Draw the clicking count with a specific color
            using var color = ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.2f, 0.6f, 0.95f, 1));
            var countStr = UiString.ConfigWindow_About_ClickingCount.GetDescription();
            if (countStr != null)
            {
                countStr = string.Format(countStr, clickingCount);
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    ImGui.TextWrapped(countStr);
                }, width, ImGui.CalcTextSize(countStr).X);

                // Draw the appropriate message based on the clicking count
                foreach (var pair in CountStringPair.Reverse())
                {
                    if (clickingCount >= pair.Key && pair.Value != null)
                    {
                        countStr = pair.Value;
                        ImGuiHelper.DrawItemMiddle(() =>
                        {
                            ImGui.TextWrapped(countStr);
                        }, width, ImGui.CalcTextSize(countStr).X);
                        break;
                    }
                }
            }
        }

        // Draw the about headers
        _aboutHeaders.Draw();
    }

    private static void DrawAutoStatusOrderConfig()
    {
        ImGui.Text(UiString.ConfigWindow_Auto_PrioritiesOrganizer.GetDescription());
        ImGui.Spacing();

        if (ImGui.Button("Reset to Default"))
        {
            OtherConfiguration.ResetAutoStatusOrder();
        }
        ImGui.Spacing();

        var autoStatusOrder = OtherConfiguration.AutoStatusOrder.ToList(); // Convert HashSet to List
        bool orderChanged = false;

        // Begin a child window to contain the list
        ImGui.BeginChild("AutoStatusOrderList", new Vector2(0, 200 * Scale), true);

        int itemCount = autoStatusOrder.Count;

        for (int i = 0; i < itemCount; i++)
        {
            var item = autoStatusOrder[i];
            var itemName = Enum.GetName(typeof(AutoStatus), item) ?? item.ToString(); // Retrieve the status name by its enum value

            // Draw up button
            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##Up{i}") && i > 0)
            {
                // Swap with the previous item
                var temp = autoStatusOrder[i - 1];
                autoStatusOrder[i - 1] = autoStatusOrder[i];
                autoStatusOrder[i] = temp;
                orderChanged = true;
            }

            ImGui.SameLine();

            // Draw down button
            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##Down{i}") && i < itemCount - 1)
            {
                // Swap with the next item
                var temp = autoStatusOrder[i + 1];
                autoStatusOrder[i + 1] = autoStatusOrder[i];
                autoStatusOrder[i] = temp;
                orderChanged = true;
            }

            ImGui.SameLine();

            // Draw the item
            ImGui.Text(itemName);

            // Optionally, add tooltips or additional information
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Priority: {i + 1}");
                ImGui.EndTooltip();
            }
        }

        ImGui.EndChild();

        if (orderChanged)
        {
            OtherConfiguration.AutoStatusOrder = new HashSet<uint>(autoStatusOrder); // Convert List back to HashSet
            OtherConfiguration.SaveAutoStatusOrder();
        }
    }

    private static readonly CollapsingHeaderGroup _aboutHeaders = new(new()
    {
        { UiString.ConfigWindow_About_Macros.GetDescription, DrawAboutMacros },
        { UiString.ConfigWindow_About_Compatibility.GetDescription, DrawAboutCompatibility },
        { UiString.ConfigWindow_About_Links.GetDescription, DrawAboutLinks },
    });

    private static void DrawAboutMacros()
    {
        // Adjust item spacing for better layout
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));

        // Display command help for different state commands
        DisplayCommandHelp(StateCommandType.Auto);
        DisplayCommandHelp(StateCommandType.Manual);
        DisplayCommandHelp(StateCommandType.Off);

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
        DisplayCommandHelp(SpecialCommandType.LimitBreak);
        DisplayCommandHelp(SpecialCommandType.NoCasting);
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

        var iconSize = 40 * Scale;

        // Create a table to display incompatible plugins
        using var table = ImRaii.Table("Incompatible plugin", 5, ImGuiTableFlags.BordersInner
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            // Set up table headers
            ImGui.TableNextColumn();
            ImGui.TableHeader("Name");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Icon/Link");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Features");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Type");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Installed");

            // Ensure that IncompatiblePlugins is not null
            var incompatiblePlugins = DownloadHelper.IncompatiblePlugins ?? Array.Empty<IncompatiblePlugin>();

            // Iterate over each incompatible plugin and display its details
            foreach (var item in incompatiblePlugins)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text(item.Name);

                ImGui.TableNextColumn();

                var icon = string.IsNullOrEmpty(item.Icon)
                    ? "https://raw.githubusercontent.com/goatcorp/DalamudAssets/master/UIRes/defaultIcon.png"
                    : item.Icon;

                if (IconSet.GetTexture(icon, out var texture))
                {
                    if (ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, Vector2.One * iconSize))
                    {
                        Util.OpenLink(item.Url);
                    }
                }

                ImGui.TableNextColumn();
                ImGui.TextWrapped(item.Features);

                ImGui.TableNextColumn();
                DisplayPluginType(item.Type);

                ImGui.TableNextColumn();
                ImGui.Text(item.IsInstalled ? "Yes" : "No");
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
        var width = ImGui.GetWindowWidth();

        // Display GitHub link button
        if (IconSet.GetTexture("https://GitHub-readme-stats.vercel.app/api/pin/?username=FFXIV-CombatReborn&repo=RotationSolverReborn&theme=dark", out var icon))
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

        ImGui.Spacing();

        // Display button to open the configuration folder
        var text = UiString.ConfigWindow_About_OpenConfigFolder.GetDescription();
        var textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            if (ImGui.Button(text))
            {
                try
                {
                    Process.Start("explorer.exe", Svc.PluginInterface.ConfigDirectory.FullName);
                }
                catch (Exception ex)
                {
                    // Handle the exception (e.g., log it or display an error message)
                    ImGui.TextColored(ImGuiColors.DalamudRed, $"Failed to open config folder: {ex.Message}");
                }
            }
        }, width, textWidth);
    }
    #endregion

    #region Autoduty

    private void DrawAutoduty()
    {
        ImGui.TextWrapped("While the RSR Team has made effort to make RSRcompatible with Autoduty, please keep in mind that RSR is not a botting tool.");
        ImGui.Spacing();
        ImGui.TextWrapped("This menu is mostly for troubleshooting purposes and is a good first step to share to get assistance.");
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
        // Display the Auto Load Rotations status
        ImGui.TextWrapped($"Auto Load Rotations: {Service.Config.LoadRotationsAtStartup}");
        if (ImGui.Button("Enable Auto Loading Rotations"))
        {
            Service.Config.LoadRotationsAtStartup.Value = true;
        }
        ImGui.Spacing();
        // Display the Download Custom Rotations status
        ImGui.TextWrapped($"Download Custom Rotations: {Service.Config.DownloadCustomRotations}");
        if (ImGui.Button("Enable Downloading Custom Rotations"))
        {
            Service.Config.LoadRotationsAtStartup.Value = true;
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
        var pluginsToCheck = new List<IncompatiblePlugin>
    {
        new IncompatiblePlugin { Name = "AutoDuty", Url = "https://puni.sh/api/repository/herc" },
        new IncompatiblePlugin { Name = "vnavmesh", Url = "https://puni.sh/api/repository/veyn" },
        new IncompatiblePlugin { Name = "BossMod Reborn", Url = "https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json" },
        new IncompatiblePlugin { Name = "Boss Mod", Url = "" },
        new IncompatiblePlugin { Name = "Avarice", Url = "https://love.puni.sh/ment.json" },
        new IncompatiblePlugin { Name = "Deliveroo", Url = "https://plugins.carvel.li/" },
        new IncompatiblePlugin { Name = "AutoRetainer", Url = "https://love.puni.sh/ment.json" },
        new IncompatiblePlugin { Name = "SkipCutscene", Url = "https://raw.githubusercontent.com/KangasZ/DalamudPluginRepository/main/plugin_repository.json" },
        new IncompatiblePlugin { Name = "AntiAfkKick", Url = "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json" },
        // Add more plugins as needed
    };

        // Check if "Boss Mod" and "BossMod Reborn" are installed
        bool isBossModInstalled = pluginsToCheck.Any(plugin => plugin.Name == "Boss Mod" && plugin.IsInstalled);
        bool isBossModRebornInstalled = pluginsToCheck.Any(plugin => plugin.Name == "BossMod Reborn" && plugin.IsInstalled);

        // Iterate through the list and check if each plugin is installed
        foreach (var plugin in pluginsToCheck)
        {
            // Only display information about "Boss Mod" if it is installed
            if (plugin.Name == "Boss Mod" && !isBossModInstalled)
            {
                continue;
            }

            bool isInstalled = plugin.IsInstalled;

            // Add a button to copy the URL to the clipboard if the plugin is not installed
            if (!isInstalled)
            {
                if (ImGui.Button($"Copy Repo URL##{plugin.Name}"))
                {
                    ImGui.SetClipboardText(plugin.Url);
                }
                ImGui.SameLine();
            }

            // Determine the color and text for "Boss Mod"
            Vector4 color;
            string text;
            if (plugin.Name == "Boss Mod" && isBossModInstalled && isBossModRebornInstalled)
            {
                color = ImGuiColors.DalamudYellow; // Display "Boss Mod" in yellow if both are installed
                text = $"{plugin.Name} is {(isInstalled ? "installed and enabled" : "not enabled")}. Both Boss Mods cannot be installed and enabled at the same time. Please disable Boss Mod.";
            }
            else
            {
                color = isInstalled ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                text = $"{plugin.Name} is {(isInstalled ? "installed and enabled" : "not enabled")}";
            }

            // Display the result using ImGui with text wrapping
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
        Svc.Log.Information($"Targeting type changed to: {type}");
    }

    #endregion

    #region Rotation
    private static void DrawRotation()
    {
        var rotation = DataCenter.CurrentRotation;
        if (rotation == null) return;

        var desc = rotation.Description;
        if (!string.IsNullOrEmpty(desc))
        {
            using var font = ImRaii.PushFont(FontManager.GetFont(15));
            ImGuiEx.TextWrappedCopy(desc);
        }

        var wholeWidth = ImGui.GetWindowWidth();
        var type = rotation.GetType();
        var info = type.Assembly.GetInfo();

        if (!string.IsNullOrEmpty(rotation.WhyNotValid))
        {
            var author = info.Author;
            if (string.IsNullOrEmpty(author)) author = "Author";

            // Display relevant debugging information
            var frameworkStyle = Service.Config.FrameworkStyle;
            ImGui.Text($"Current FrameworkStyle: {frameworkStyle}");

            // Add a button to copy the WhyNotValid string to the clipboard
            if (ImGui.Button("Copy Error Message"))
            {
                ImGui.SetClipboardText(rotation.WhyNotValid);
            }

            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DPSRed);
            ImGui.TextWrapped(string.Format(rotation.WhyNotValid, author));
        }

        _rotationHeader.Draw();
    }

    private static uint ChangeAlpha(uint color)
    {
        var c = ImGui.ColorConvertU32ToFloat4(color);
        c.W = 0.55f;
        return ImGui.ColorConvertFloat4ToU32(c);
    }

    private static readonly CollapsingHeaderGroup _rotationHeader = new(new()
    {
        { UiString.ConfigWindow_Rotation_Description.GetDescription, DrawRotationDescription },

        { GetRotationStatusHead,  DrawRotationStatus },

        { UiString.ConfigWindow_Rotation_Configuration.GetDescription, DrawRotationConfiguration },

        { UiString.ConfigWindow_Rotation_Information.GetDescription, DrawRotationInformation },
    });

    private const float DESC_SIZE = 24;
    private static void DrawRotationDescription()
    {
        var rotation = DataCenter.CurrentRotation;
        if (rotation == null) return;

        var wholeWidth = ImGui.GetWindowWidth();
        var type = rotation.GetType();

        var attrs = new List<RotationDescAttribute?> { RotationDescAttribute.MergeToOne(type.GetCustomAttributes<RotationDescAttribute>()) };

        foreach (var m in type.GetAllMethodInfo())
        {
            attrs.Add(RotationDescAttribute.MergeToOne(m.GetCustomAttributes<RotationDescAttribute>()));
        }

        using (var table = ImRaii.Table("Rotation Description", 2, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp))
        {
            if (table)
            {
                foreach (var a in RotationDescAttribute.Merge(attrs))
                {
                    var attr = RotationDescAttribute.MergeToOne(a);
                    if (attr == null) continue;

                    var allActions = new List<IBaseAction>();
                    foreach (var actionId in attr.Actions)
                    {
                        var action = rotation.AllBaseActions.FirstOrDefault(a => a.ID == (uint)actionId);
                        if (action != null)
                        {
                            allActions.Add(action);
                        }
                    }


                    bool hasDesc = !string.IsNullOrEmpty(attr.Description);

                    if (!hasDesc && !allActions.Any()) continue;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    if (IconSet.GetTexture(attr.IconID, out var image)) ImGui.Image(image.ImGuiHandle, Vector2.One * DESC_SIZE * Scale);

                    ImGui.SameLine();
                    var isOnCommand = attr.IsOnCommand;
                    if (isOnCommand) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                    ImGui.Text(" " + attr.Type.GetDescription());
                    if (isOnCommand) ImGui.PopStyleColor();

                    ImGui.TableNextColumn();

                    if (hasDesc)
                    {
                        ImGui.Text(attr.Description);
                    }

                    bool notStart = false;
                    var size = DESC_SIZE * Scale;
                    var y = ImGui.GetCursorPosY() + size * 4 / 82;
                    foreach (var item in allActions)
                    {
                        if (item == null) continue;

                        if (notStart)
                        {
                            ImGui.SameLine();
                        }

                        if (item.GetTexture(out var texture))
                        {
                            ImGui.SetCursorPosY(y);
                            var cursor = ImGui.GetCursorPos();
                            ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, Vector2.One * size);
                            ImGuiHelper.DrawActionOverlay(cursor, size, 1);
                            ImguiTooltips.HoveredTooltip(item.Name);
                        }
                        notStart = true;
                    }
                }
            }
        }

        var links = type.GetCustomAttributes<LinkDescriptionAttribute>();

        foreach (var link in links)
        {
            DrawLinkDescription(link.LinkDescription, wholeWidth, true);
        }
    }

    internal static void DrawLinkDescription(LinkDescription link, float wholeWidth, bool drawQuestion)
    {
        var hasTexture = IconSet.GetTexture(link.Url, out var texture);

        if (hasTexture && ImGuiHelper.TextureButton(texture, wholeWidth, wholeWidth))
        {
            Util.OpenLink(link.Url);
        }

        ImGui.TextWrapped(link.Description);

        if (drawQuestion && !hasTexture && !string.IsNullOrEmpty(link.Url))
        {
            if (ImGuiEx.IconButton(FontAwesomeIcon.Question, link.Description))
            {
                Util.OpenLink(link.Url);
            }
        }
    }

    private static string GetRotationStatusHead()
    {
        var rotation = DataCenter.CurrentRotation;
        var status = UiString.ConfigWindow_Rotation_Status.GetDescription();
        if (Service.Config.InDebug)
        {
            return status;
        }
        if (rotation == null || !rotation.ShowStatus) return string.Empty;
        return status;
    }

    private static void DrawRotationStatus()
    {
        DataCenter.CurrentRotation?.DisplayStatus();
    }

    private static string ToCommandStr(OtherCommandType type, string str, string extra = "")
    {
        var result = Service.COMMAND + " " + type.ToString() + " " + str;
        if (!string.IsNullOrEmpty(extra)) result += " " + extra;
        return result;
    }
    private static void DrawRotationConfiguration()
    {
        var rotation = DataCenter.CurrentRotation;
        if (rotation == null) return;

        var enable = rotation.IsEnabled;
        if (ImGui.Checkbox(rotation.Name, ref enable))
        {
            rotation.IsEnabled = enable;
        }
        if (!enable) return;

        var set = rotation.Configs;

        if (set.Any()) ImGui.Separator();

        foreach (var config in set.Configs)
        {
            if (DataCenter.IsPvP)
            {
                if (!config.Type.HasFlag(CombatType.PvP)) continue;
            }
            else
            {
                if (!config.Type.HasFlag(CombatType.PvE)) continue;
            }

            var key = rotation.GetType().FullName ?? rotation.GetType().Name + "." + config.Name;
            var name = $"##{config.GetHashCode()}_{key}.Name";
            string command = ToCommandStr(OtherCommandType.Rotations, config.Name, config.DefaultValue);
            void Reset() => config.Value = config.DefaultValue;

            ImGuiHelper.PrepareGroup(key, command, Reset);

            if (config is RotationConfigCombo c)
            {
                var names = c.DisplayValues;
                var selectedValue = c.Value;
                var index = names.IndexOf(n => n == selectedValue);

                ImGui.SetNextItemWidth(ImGui.CalcTextSize(c.DisplayValues.OrderByDescending(v => v.Length).First()).X + 50 * Scale);
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
            else continue;

            ImGui.SameLine();
            ImGui.TextWrapped($"{config.DisplayName}");
            ImGuiHelper.ReactPopup(key, command, Reset, false);
        }
    }

    private static void DrawRotationInformation()
    {
        var rotation = DataCenter.CurrentRotation;
        if (rotation == null) return;

        var youtubeLink = rotation.GetType().GetCustomAttribute<YoutubeLinkAttribute>()?.ID;

        var wholeWidth = ImGui.GetWindowWidth();
        if (!string.IsNullOrEmpty(youtubeLink))
        {
            ImGui.NewLine();
            if (IconSet.GetTexture("https://www.gstatic.com/youtube/img/branding/youtubelogo/svg/youtubelogo.svg", out var icon) && ImGuiHelper.TextureButton(icon, wholeWidth, 250 * Scale, "Youtube Link"))
            {
                Util.OpenLink("https://www.youtube.com/watch?v=" + youtubeLink);
            }
        }

        var assembly = rotation.GetType().Assembly;
        var info = assembly.GetInfo();

        if (info != null)
        {
            ImGui.NewLine();

            var link = rotation.GetType().GetCustomAttribute<SourceCodeAttribute>();
            if (link != null)
            {
                var userName = info.GitHubUserName;
                var repository = info.GitHubRepository;

                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(repository) && !string.IsNullOrEmpty(link.Path))
                {
                    DrawGitHubBadge(userName, repository, link.Path, $"https://github.com/{userName}/{repository}/blob/{link.Path}", center: true);
                }
            }
            ImGui.NewLine();

            ImGuiHelper.DrawItemMiddle(() =>
            {
                using var group = ImRaii.Group();
                if (group)
                {
                    if (ImGui.Button(info.Name))
                    {
                        Process.Start("explorer.exe", "/select, \"" + info.FilePath + "\"");
                    }

                    var version = assembly.GetName().Version;
                    if (version != null)
                    {
                        ImGui.Text(" v " + version.ToString());
                    }
                    ImGui.Text(" - " + info.Author);
                }

            }, wholeWidth, _groupWidth);

            _groupWidth = ImGui.GetItemRectSize().X;
        }
    }

    private static float _groupWidth = 100;
    #endregion

    #region Actions
    private static unsafe void DrawActions()
    {
        ImGui.TextWrapped(UiString.ConfigWindow_Actions_Description.GetDescription());

        using var table = ImRaii.Table("Rotation Solver Actions", 2, ImGuiTableFlags.Resizable);

        if (table)
        {
            ImGui.TableSetupColumn("Action Column", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
            ImGui.TableNextColumn();

            if (_actionsList != null)
            {
                _actionsList.ClearCollapsingHeader();

                if (DataCenter.CurrentRotation != null && RotationUpdater.AllGroupedActions != null)
                {
                    var size = 30 * Scale;
                    var count = Math.Max(1, (int)MathF.Floor(ImGui.GetColumnWidth() / (size * 1.1f + ImGui.GetStyle().ItemSpacing.X)));
                    foreach (var pair in RotationUpdater.AllGroupedActions)
                    {
                        _actionsList.AddCollapsingHeader(() => pair.Key, () =>
                        {
                            var index = 0;
                            foreach (var item in pair.OrderBy(t => t.ID))
                            {
                                if (!item.GetTexture(out var icon)) continue;

                                if (index++ % count != 0)
                                {
                                    ImGui.SameLine();
                                }

                                ImGui.BeginGroup();
                                var cursor = ImGui.GetCursorPos();
                                if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * size, item.Name + item.ID))
                                {
                                    _activeAction = item;
                                }
                                ImGuiHelper.DrawActionOverlay(cursor, size, _activeAction == item ? 1 : 0);

                                if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out var texture))
                                {
                                    var offset = new Vector2(1 / 12f, 1 / 6f);
                                    ImGui.SetCursorPos(cursor + new Vector2(0.6f, 0.7f) * size);
                                    ImGui.Image(texture.ImGuiHandle, Vector2.One * size * 0.5f,
                                        new Vector2(item.IsEnabled ? 0 : 0.5f, 0) + offset,
                                        new Vector2(item.IsEnabled ? 0.5f : 1, 1) - offset);
                                }
                                ImGui.EndGroup();

                                string key = $"Action Macro Usage {item.Name} {item.ID}";
                                var cmd = ToCommandStr(OtherCommandType.DoActions, $"{item}-{5}");
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

            ImGui.TextWrapped(UiString.ConfigWindow_Actions_ConditionDescription.GetDescription());
            _sequencerList?.Draw();
        }

        static void DrawConfigsOfAction()
        {
            if (_activeAction == null) return;

            var enable = _activeAction.IsEnabled;
            if (ImGui.Checkbox($"{_activeAction.Name}##{_activeAction.Name} Enabled", ref enable))
            {
                _activeAction.IsEnabled = enable;
            }

            const string key = "Action Enable Popup";
            var cmd = ToCommandStr(OtherCommandType.ToggleActions, _activeAction.ToString()!);
            ImGuiHelper.DrawHotKeysPopup(key, cmd);
            ImGuiHelper.ExecuteHotKeysPopup(key, cmd, string.Empty, false);

            enable = _activeAction.IsInCooldown;
            if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_ShowOnCDWindow.GetDescription()}##{_activeAction.Name}InCooldown", ref enable))
            {
                _activeAction.IsInCooldown = enable;
            }

            if (_activeAction is IBaseAction a)
            {
                DrawConfigsOfBaseAction(a);
            }

            ImGui.Separator();

            static void DrawConfigsOfBaseAction(IBaseAction a)
            {
                var config = a.Config;

                ImGui.Separator();

                var ttk = config.TimeToKill;
                ImGui.SetNextItemWidth(Scale * 150);
                if (ImGui.DragFloat($"{UiString.ConfigWindow_Actions_TTK.GetDescription()}##{a}",
                    ref ttk, 0.1f, 0, 120, $"{ttk:F2}{ConfigUnitType.Seconds.ToSymbol()}"))
                {
                    config.TimeToKill = ttk;
                }
                ImguiTooltips.HoveredTooltip(ConfigUnitType.Seconds.GetDescription());

                if (a.Setting.StatusProvide != null || a.Setting.TargetStatusProvide != null)
                {
                    var shouldStatus = config.ShouldCheckStatus;
                    if (ImGui.Checkbox($"{UiString.ConfigWindow_Actions_CheckStatus.GetDescription()}##{a}", ref shouldStatus))
                    {
                        config.ShouldCheckStatus = shouldStatus;
                    }

                    if (shouldStatus)
                    {
                        var statusGcdCount = (int)config.StatusGcdCount;
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
                    var aoeCount = (int)config.AoeCount;
                    ImGui.SetNextItemWidth(Scale * 150);
                    if (ImGui.DragInt($"{UiString.ConfigWindow_Actions_AoeCount.GetDescription()}##{a}",
                        ref aoeCount, 0.05f, 1, 10))
                    {
                        config.AoeCount = (byte)aoeCount;
                    }
                }

                var ratio = config.AutoHealRatio;
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
            if (!Service.Config.InDebug) return;

            if (_activeAction is IBaseAction action)
            {
                try
                {
                    ImGui.Text("ID: " + action.Info.ID);
                    ImGui.Text("AdjustedID: " + Service.GetAdjustedActionId(action.Info.ID));
                    ImGui.Text($"Can Use: {action.CanUse(out _)} ");
                    ImGui.Text("AoeCount: " + action.Config.AoeCount);
                    ImGui.Text("ShouldCheckStatus: " + action.Config.ShouldCheckStatus);
#if DEBUG
                    ImGui.Text("Is Real GCD: " + action.Info.IsRealGCD);
                    
                    // Ensure ActionManager.Instance() is not null
                    if (ActionManager.Instance() != null)
                    {
                        ImGui.Text("Resources: " + ActionManager.Instance()->CheckActionResources(ActionType.Action, action.AdjustedID));
                        ImGui.Text("Status: " + ActionManager.Instance()->GetActionStatus(ActionType.Action, action.AdjustedID));
                    }
            
                    ImGui.Text("Cast Time: " + action.Info.CastTime);
                    ImGui.Text("MP: " + action.Info.MPNeed);
#endif
                    ImGui.Text("AttackType: " + action.Info.AttackType);
                    ImGui.Text("Level: " + action.Info.Level);
                    ImGui.Text("Range: " + action.Info.Range);
                    ImGui.Text("EffectRange: " + action.Info.EffectRange);
                    ImGui.Text("Aspect: " + action.Info.Aspect);
                    ImGui.Text("Has One:" + action.Cooldown.HasOneCharge);
                    ImGui.Text("Recast One: " + action.Cooldown.RecastTimeOneChargeRaw);
                    ImGui.Text("Recast Elapsed: " + action.Cooldown.RecastTimeElapsedRaw);
                    ImGui.Text($"Charges: {action.Cooldown.CurrentCharges} / {action.Cooldown.MaxCharges}");

                    ImGui.Text("IgnoreCastCheck:" + action.CanUse(out _, skipCastingCheck: true));
                    ImGui.Text("Target Name: " + action.Target.Target?.Name ?? string.Empty);
                    ImGui.Text($"SpellUnlocked: {action.Info.SpellUnlocked} ({action.Action.UnlockLink.RowId})");
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
                        var remain = ActionManager.Instance()->GetRecastTime(ActionType.Item, item.ID) - ActionManager.Instance()->GetRecastTimeElapsed(ActionType.Item, item.ID);
                        ImGui.Text("remain: " + remain.ToString());
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

    private static readonly CollapsingHeaderGroup _actionsList = new CollapsingHeaderGroup(new Dictionary<Func<string>, System.Action>())
    {
        HeaderSize = 18,
    };

    private static readonly CollapsingHeaderGroup _sequencerList = new(new()
    {
        { UiString.ConfigWindow_Actions_ForcedConditionSet.GetDescription, () =>
        {
            ImGui.TextWrapped(UiString.ConfigWindow_Actions_ForcedConditionSet_Description.GetDescription());

            var rotation = DataCenter.CurrentRotation;
            var set = DataCenter.RightSet;

            if (set == null || _activeAction == null || rotation == null) return;
            set.GetCondition(_activeAction.ID)?.DrawMain(rotation);
        } },

        { UiString.ConfigWindow_Actions_DisabledConditionSet.GetDescription, () =>
        {
            ImGui.TextWrapped(UiString.ConfigWindow_Actions_DisabledConditionSet_Description.GetDescription());

            var rotation = DataCenter.CurrentRotation;
            var set = DataCenter.RightSet;

            if (set == null || _activeAction == null || rotation == null) return;
            set.GetDisabledCondition(_activeAction.ID)?.DrawMain(rotation);
        } },
    })
    {
        HeaderSize = 18,
    };
    #endregion

    #region Rotations
    private static void DrawRotations()
    {
        var width = ImGui.GetWindowWidth();

        ImGui.PushFont(FontManager.GetFont(ImGui.GetFontSize() + 5));
        var text = UiString.ConfigWindow_Rotations_Warning.GetDescription();
        var textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, text);
        }, width, textWidth);
        text = UiString.ConfigWindow_Rotations_Warning2.GetDescription();
        textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, text);
        }, width, textWidth);
        ImGui.PopFont();

        ImGui.Separator();
        DrawRotationsSettings();

        ImGui.Separator();
        text = UiString.ConfigWindow_Rotations_Download.GetDescription();
        textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            if (ImGui.Button(text))
            {
                Task.Run(async () =>
                {
                    await RotationUpdater.GetAllCustomRotationsAsync(DownloadOption.MustDownload | DownloadOption.ShowList);
                });
            }
        }, width, textWidth);
        text = UiString.ConfigWindow_Rotations_Reset.GetDescription();
        textWidth = ImGuiHelpers.GetButtonSize(text).X;
        ImGuiHelper.DrawItemMiddle(() =>
        {
            if (ImGui.Button(text))
            {
                Task.Run(async () =>
                {
                    await RotationUpdater.ResetToDefaults();
                    await RotationUpdater.GetAllCustomRotationsAsync(DownloadOption.MustDownload | DownloadOption.ShowList);
                });
            }
        }, width, textWidth);
        ImGui.PushFont(FontManager.GetFont(ImGui.GetFontSize() + 3));
        ImGui.Text(UiString.ConfigWindow_Rotations_Sources.GetDescription());
        ImGui.PopFont();
        DrawRotationsLibraries();


        _rotationsHeader?.Draw();
    }
    private static readonly CollapsingHeaderGroup _rotationsHeader = new(new()
    {
        { UiString.ConfigWindow_Rotations_Loaded.GetDescription, DrawRotationsLoaded},
    });

    private static void DrawRotationsSettings()
    {
        _allSearchable.DrawItems(Configs.Rotations);
    }

    private static void DrawRotationsLoaded()
    {
        var assemblyGrps = RotationUpdater.CustomRotationsDict
            .SelectMany(d => d.Value)
            .SelectMany(g => g.Rotations)
            .GroupBy(r => r.Assembly);

        using var table = ImRaii.Table("Rotation Solver AssemblyTable", 3, ImGuiTableFlags.BordersInner
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp);

        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            ImGui.TableHeader("Information");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Rotations");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Links");

            foreach (var grp in assemblyGrps)
            {
                ImGui.TableNextRow();

                var assembly = grp.Key;

                var info = assembly.GetInfo();
                ImGui.TableNextColumn();

                if (ImGui.Button(info.Name))
                {
                    Process.Start("explorer.exe", "/select, \"" + info.FilePath + "\"");
                }

                var version = assembly.GetName().Version;
                if (version != null)
                {
                    ImGui.Text(" v " + version.ToString());
                }

                ImGui.Text(" - " + info.Author);

                ImGui.TableNextColumn();

                var lastRole = JobRole.None;
                foreach (var jobs in grp.GroupBy(r => r.GetCustomAttribute<JobsAttribute>()!.Jobs[0]).OrderBy(g => Svc.Data.GetExcelSheet<ClassJob>()!.GetRow((uint)g.Key)!.GetJobRole()))
                {
                    var role = Svc.Data.GetExcelSheet<ClassJob>()!.GetRow((uint)jobs.Key)!.GetJobRole();
                    if (lastRole == role && lastRole != JobRole.None) ImGui.SameLine();
                    lastRole = role;

                    if (IconSet.GetTexture(IconSet.GetJobIcon(jobs.Key, IconType.Framed), out var texture, 62574))
                        ImGui.Image(texture.ImGuiHandle, Vector2.One * 30 * Scale);

                    ImguiTooltips.HoveredTooltip(string.Join('\n', jobs.Select(t => t.GetCustomAttribute<UIAttribute>()?.Name ?? t.Name)) +
                                                 Environment.NewLine +
                                                 string.Join('\n', jobs.Select(t => t.GetCustomAttribute<RotationAttribute>()?.Type ?? CombatType.None)));
                }

                ImGui.TableNextColumn();

                if (!string.IsNullOrEmpty(info.GitHubUserName) && !string.IsNullOrEmpty(info.GitHubRepository) && !string.IsNullOrEmpty(info.FilePath))
                {
                    DrawGitHubBadge(info.GitHubUserName, info.GitHubRepository, info.FilePath);
                }

                if (!string.IsNullOrEmpty(info.DonateLink)
                    && IconSet.GetTexture("https://storage.ko-fi.com/cdn/brandasset/kofi_button_red.png", out var icon)
                    && ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, new Vector2(1, (float)icon.Height / icon.Width) * MathF.Min(250, icon.Width) * Scale, info.FilePath ?? string.Empty))
                {
                    Util.OpenLink(info.DonateLink);
                }
            }
        }
    }

    private static void DrawGitHubBadge(string userName, string repository, string id = "", string link = "", bool center = false)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(repository)) return;

        var wholeWidth = ImGui.GetWindowWidth();

        link = string.IsNullOrEmpty(link) ? $"https://GitHub.com/{userName}/{repository}" : link;

        if (IconSet.GetTexture($"https://github-readme-stats.vercel.app/api/pin/?username={userName}&repo={repository}&theme=dark", out var icon)
            && (center ? ImGuiHelper.TextureButton(icon, wholeWidth, icon.Width, id)
            : ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, new Vector2(icon.Width, icon.Height), id)))
        {
            Util.OpenLink(link);
        }

        var hasDate = IconSet.GetTexture($"https://img.shields.io/github/release-date/{userName}/{repository}?style=for-the-badge", out var releaseDate);

        var hasCount = IconSet.GetTexture($"https://img.shields.io/github/downloads/{userName}/{repository}/latest/total?style=for-the-badge&label=", out var downloadCount);

        var style = ImGui.GetStyle();
        var spacing = style.ItemSpacing;
        style.ItemSpacing = Vector2.Zero;
        if (center)
        {
            float width = 0;
            if (hasDate) width += releaseDate.Width;
            if (hasCount) width += downloadCount.Width;
            var ratio = MathF.Min(1, wholeWidth / width);
            ImGuiHelper.DrawItemMiddle(() =>
            {
                if (hasDate && ImGuiHelper.NoPaddingNoColorImageButton(releaseDate.ImGuiHandle, new Vector2(releaseDate.Width, releaseDate.Height) * ratio, id))
                {
                    Util.OpenLink(link);
                }
                if (hasDate && hasCount) ImGui.SameLine();
                if (hasCount && ImGuiHelper.NoPaddingNoColorImageButton(downloadCount.ImGuiHandle, new Vector2(downloadCount.Width, downloadCount.Height) * ratio, id))
                {
                    Util.OpenLink(link);
                }
            }, wholeWidth, width * ratio);
        }
        else
        {
            if (hasDate && ImGuiHelper.NoPaddingNoColorImageButton(releaseDate.ImGuiHandle, new Vector2(releaseDate.Width, releaseDate.Height), id))
            {
                Util.OpenLink(link);
            }
            if (hasDate && hasCount) ImGui.SameLine();
            if (hasCount && ImGuiHelper.NoPaddingNoColorImageButton(downloadCount.ImGuiHandle, new Vector2(downloadCount.Width, downloadCount.Height), id))
            {
                Util.OpenLink(link);
            }
        }
        style.ItemSpacing = spacing;
    }

    private static void DrawRotationsLibraries()
    {
        if (!Service.Config.RotationLibs.Any(string.IsNullOrEmpty))
        {
            Service.Config.RotationLibs = [.. Service.Config.RotationLibs, string.Empty];
        }

        ImGui.Spacing();

        var width = ImGui.GetWindowWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X - ImGui.GetStyle().ItemSpacing.X - 10 * Scale;

        int removeIndex = -1;
        for (int i = 0; i < Service.Config.RotationLibs.Length; i++)
        {
            ImGui.SetNextItemWidth(width);
            ImGui.InputTextWithHint($"##Rotation Solver OtherLib{i}", UiString.ConfigWindow_Rotations_Library.GetDescription(), ref Service.Config.RotationLibs[i], 1024);
            ImGui.SameLine();

            if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Remove Rotation Library{i}"))
            {
                removeIndex = i;
            }
        }
        if (removeIndex > -1)
        {
            var list = Service.Config.RotationLibs.ToList();
            list.RemoveAt(removeIndex);
            Service.Config.RotationLibs = [.. list];
        }
    }
    #endregion 

    #region List
    private static readonly Lazy<Status[]> _allDispelStatus = new(() =>
    Service.GetSheet<Status>()
        .Where(s => s.CanDispel)
        .ToArray());

    internal static Status[] AllDispelStatus => _allDispelStatus.Value;

    private static readonly Lazy<Status[]> _allStatus = new(() =>
        Service.GetSheet<Status>()
            .Where(s => !s.CanDispel && !s.LockMovement && !s.IsGaze && !s.IsFcBuff
                && !string.IsNullOrEmpty(s.Name.ToString()) && s.Icon != 0)
            .ToArray());

    internal static Status[] AllStatus => _allStatus.Value;

    private static readonly Lazy<GAction[]> _allActions = new(() =>
        Service.GetSheet<GAction>()
        .Where(a => !string.IsNullOrEmpty(a.ToString()) && !a.IsPvP && !a.IsPlayerAction
            && a.ClassJob.RowId == 0 && a.Cast100ms > 0)
        .ToArray());

    internal static GAction[] AllActions => _allActions.Value;

    private const int BadStatusCategory = 2;
    private static readonly Lazy<Status[]> _badStatus = new(() =>
        Service.GetSheet<Status>()
            .Where(s => s.StatusCategory == BadStatusCategory && s.Icon != 0)
            .ToArray());

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
        ImGui.InputTextWithHint("##Searching the action", UiString.ConfigWindow_List_StatusNameOrId.GetDescription(), ref _statusSearching, 128);

        using var table = ImRaii.Table("Rotation Solver List Statuses", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Invuln Status List"))
            {
                OtherConfiguration.ResetInvincibleStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_Invincibility.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Priority Status List"))
            {
                OtherConfiguration.ResetPriorityStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_Priority.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Dispell Debuff List"))
            {
                OtherConfiguration.ResetDangerousStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_DangerousStatus.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update No Casting Status List"))
            {
                OtherConfiguration.ResetNoCastingStatus();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_NoCastingStatus.GetDescription());

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_InvincibilityDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.InvincibleStatus), OtherConfiguration.InvincibleStatus, AllStatus);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_PriorityDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.PriorityStatus), OtherConfiguration.PriorityStatus, AllStatus);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_DangerousStatusDesc.GetDescription());
            DrawStatusList(nameof(OtherConfiguration.DangerousStatus), OtherConfiguration.DangerousStatus, AllDispelStatus);

            ImGui.TableNextColumn();
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
                Svc.Log.Warning(ex, CopyErrorMessage);
            }
        }

        ImGui.SameLine();

        if (ImGui.Button(UiString.ActionSequencer_FromClipboard.GetDescription()))
        {
            try
            {
                var clipboardText = ImGui.GetClipboardText();
                if (clipboardText != null)
                {
                    foreach (var aId in JsonConvert.DeserializeObject<uint[]>(clipboardText) ?? Array.Empty<uint>())
                    {
                        items.Add(aId);
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Warning(ex, PasteErrorMessage);
            }
            finally
            {
                OtherConfiguration.Save();
                ImGui.CloseCurrentPopup();
            }
        }
    }

    static string _statusSearching = string.Empty;
    private static void DrawStatusList(string name, HashSet<uint> statuses, Status[] allStatus)
    {
        const float IconWidth = 24f;
        const float IconHeight = 32f;
        const uint DefaultNotLoadId = 10100;

        ImGui.PushID(name);
        FromClipBoardButton(statuses);

        uint removeId = 0;
        uint notLoadId = DefaultNotLoadId;

        var popupId = $"Rotation Solver Popup{name}";

        StatusPopUp(popupId, allStatus, ref _statusSearching, status =>
        {
            statuses.Add(status.RowId);
            OtherConfiguration.Save();
        }, notLoadId);

        var count = Math.Max(1, (int)MathF.Floor(ImGui.GetColumnWidth() / (IconWidth * Scale + ImGui.GetStyle().ItemSpacing.X)));
        var index = 0;

        if (IconSet.GetTexture(16220, out var text))
        {
            if (index++ % count != 0)
            {
                ImGui.SameLine();
            }
            if (ImGuiHelper.NoPaddingNoColorImageButton(text.ImGuiHandle, new Vector2(IconWidth, IconHeight) * Scale, name))
            {
                if (!ImGui.IsPopupOpen(popupId)) ImGui.OpenPopup(popupId);
            }
            ImguiTooltips.HoveredTooltip(UiString.ConfigWindow_List_AddStatus.GetDescription());
        }

        foreach (var statusId in statuses)
        {
            var status = Service.GetSheet<Status>().GetRow(statusId);
            if (status.RowId == 0) continue;

            void Delete() => removeId = status.RowId;

            var key = $"Status{status.RowId}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty, (UiString.ConfigWindow_List_Remove.GetDescription(), Delete, new[] { "Delete" }));

            if (IconSet.GetTexture(status.Icon, out var texture, notLoadId))
            {
                if (index++ % count != 0)
                {
                    ImGui.SameLine();
                }
                ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, new Vector2(IconWidth, IconHeight) * Scale, $"Status{status.RowId}");

                ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, $"{status.Name} ({status.RowId})", false,
                    (Delete, new[] { VirtualKey.DELETE }));
            }
        }

        if (removeId != 0)
        {
            statuses.Remove(removeId);
            OtherConfiguration.Save();
        }
        ImGui.PopID();
    }

    internal static void StatusPopUp(string popupId, Status[] allStatus, ref string searching, Action<Status> clicked, uint notLoadId = 10100, float size = 32)
    {
        const float InputWidth = 200f;
        const float ChildHeight = 400f;
        const int InputTextLength = 128;

        using var popup = ImRaii.Popup(popupId);
        if (popup)
        {
            ImGui.SetNextItemWidth(InputWidth * Scale);
            ImGui.InputTextWithHint("##Searching the status", UiString.ConfigWindow_List_StatusNameOrId.GetDescription(), ref searching, InputTextLength);

            ImGui.Spacing();

            using var child = ImRaii.Child("Rotation Solver Reborn Add Status", new Vector2(-1, ChildHeight * Scale));
            if (child)
            {
                var count = Math.Max(1, (int)MathF.Floor(ImGui.GetWindowWidth() / (size * 3 / 4 * Scale + ImGui.GetStyle().ItemSpacing.X)));
                var index = 0;

                var searchingKey = searching;
                foreach (var status in allStatus.OrderByDescending(s => SearchableCollection.Similarity($"{s.Name} {s.RowId}", searchingKey)))
                {
                    if (IconSet.GetTexture(status.Icon, out var texture, notLoadId))
                    {
                        if (index++ % count != 0)
                        {
                            ImGui.SameLine();
                        }
                        if (ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, new Vector2(size * 3 / 4, size) * Scale, $"Adding{status.RowId}"))
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
        ImGui.InputTextWithHint("##Searching the action", UiString.ConfigWindow_List_ActionNameOrId.GetDescription(), ref _actionSearching, 128);

        using var table = ImRaii.Table("Rotation Solver List Actions", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Tankbuster List"))
            {
                OtherConfiguration.ResetHostileCastingTank();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingTank.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update AOE List"))
            {
                OtherConfiguration.ResetHostileCastingArea();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingArea.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Update Knockback List"))
            {
                OtherConfiguration.ResetHostileCastingKnockback();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingKnockback.GetDescription());

            ImGui.TableNextColumn();
            if (ImGui.Button("Reset and Stop Casting List"))
            {
                OtherConfiguration.ResetHostileCastingStop();
            }
            ImGui.TableHeader(UiString.ConfigWindow_List_HostileCastingStop.GetDescription());

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingTankDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingTank), OtherConfiguration.HostileCastingTank);

            ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingAreaDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingArea), OtherConfiguration.HostileCastingArea);

            ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List2);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingKnockbackDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingKnockback), OtherConfiguration.HostileCastingKnockback);

            ImGui.TableNextColumn();
            _allSearchable.DrawItems(Configs.List3);
            ImGui.TextWrapped(UiString.ConfigWindow_List_HostileCastingStopDesc.GetDescription());
            DrawActionsList(nameof(OtherConfiguration.HostileCastingStop), OtherConfiguration.HostileCastingStop);
        }
    }

    private static string _actionSearching = string.Empty;

    private static void DrawActionsList(string name, HashSet<uint> actions)
    {
        // Initialize actions to an empty HashSet if it is null
        actions ??= new HashSet<uint>();

        const float InputWidth = 200f;
        const float ChildHeight = 400f;

        ImGui.PushID(name);
        uint removeId = 0;

        var popupId = $"Rotation Solver Reborn Action Popup{name}";

        if (ImGui.Button($"{UiString.ConfigWindow_List_AddAction.GetDescription()}##{name}"))
        {
            if (!ImGui.IsPopupOpen(popupId)) ImGui.OpenPopup(popupId);
        }

        ImGui.SameLine();
        FromClipBoardButton(actions);

        ImGui.Spacing();

        foreach (var action in actions.Select(a => Service.GetSheet<GAction>().GetRow(a))
            .Where(a => a.RowId != 0)
            .OrderByDescending(s => SearchableCollection.Similarity($"{s!.Name} {s.RowId}", _actionSearching)))
        {
            void Reset() => removeId = action.RowId;

            var key = $"Action{action!.RowId}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty, (UiString.ConfigWindow_List_Remove.GetDescription(), Reset, new[] { "Delete" }));

            ImGui.Selectable($"{action.Name} ({action.RowId})");

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, false, (Reset, new[] { VirtualKey.DELETE }));
        }

        if (removeId != 0)
        {
            actions.Remove(removeId);
            OtherConfiguration.Save();
        }

        using var popup = ImRaii.Popup(popupId);
        if (popup)
        {
            ImGui.SetNextItemWidth(InputWidth * Scale);
            ImGui.InputTextWithHint("##Searching the action pop up", UiString.ConfigWindow_List_ActionNameOrId.GetDescription(), ref _actionSearching, 128);

            ImGui.Spacing();

            using var child = ImRaii.Child("Rotation Solver Add action", new Vector2(-1, ChildHeight * Scale));
            if (child)
            {
                foreach (var action in AllActions.OrderByDescending(s => SearchableCollection.Similarity($"{s.Name} {s.RowId}", _actionSearching)))
                {
                    var selected = ImGui.Selectable($"{action.Name} ({action.RowId})");
                    if (ImGui.IsItemHovered())
                    {
                        ImguiTooltips.ShowTooltip($"{action.Name} ({action.RowId})");
                        if (selected)
                        {
                            actions.Add(action.RowId);
                            OtherConfiguration.Save();
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }
        }
        ImGui.PopID();
    }

    public static Vector3 HoveredPosition { get; private set; } = Vector3.Zero;
    private static void DrawListTerritories()
    {
        if (Svc.ClientState == null) return;

        var territoryId = Svc.ClientState.TerritoryType;

        using (var font = ImRaii.PushFont(FontManager.GetFont(21)))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);

            const int iconSize = 32;
            var contentFinder = DataCenter.Territory?.ContentType;
            var territoryName = DataCenter.Territory?.Name;

            if (contentFinder.HasValue && !string.IsNullOrEmpty(DataCenter.Territory?.ContentFinderName))
            {
                territoryName += $" ({DataCenter.Territory?.ContentFinderName})";
            }
            var icon = DataCenter.Territory?.ContentFinderIcon ?? 23;
            if (icon == 0) icon = 23;
            var getIcon = IconSet.GetTexture(icon, out var texture);
            ImGuiHelper.DrawItemMiddle(() =>
            {
                if (getIcon)
                {
                    ImGui.Image(texture.ImGuiHandle, Vector2.One * 28 * Scale);
                    ImGui.SameLine();
                }
                ImGui.Text(territoryName);
            }, ImGui.GetWindowWidth(), ImGui.CalcTextSize(territoryName).X + ImGui.GetStyle().ItemSpacing.X + iconSize);
        }

        DrawContentFinder(DataCenter.Territory?.ContentFinderIcon ?? 23);

        using var table = ImRaii.Table("Rotation Solver List Territories", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchSame);
        if (table)
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_NoHostile.GetDescription());

            ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_NoProvoke.GetDescription());

            ImGui.TableNextColumn();
            ImGui.TableHeader(UiString.ConfigWindow_List_BeneficialPositions.GetDescription());

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_NoHostileDesc.GetDescription());

            var width = ImGui.GetColumnWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X - ImGui.GetStyle().ItemSpacing.X - 10 * Scale;

            if (!OtherConfiguration.NoHostileNames.TryGetValue(territoryId, out var libs))
            {
                OtherConfiguration.NoHostileNames[territoryId] = libs = [];
            }

            // Add one.
            if (!libs.Any(string.IsNullOrEmpty))
            {
                OtherConfiguration.NoHostileNames[territoryId] = [.. libs, string.Empty];
            }

            int removeIndex = -1;
            for (int i = 0; i < libs.Length; i++)
            {
                ImGui.SetNextItemWidth(width);
                if (ImGui.InputTextWithHint($"##Rotation Solver Territory Target Name {i}", UiString.ConfigWindow_List_NoHostilesName.GetDescription(), ref libs[i], 1024))
                {
                    OtherConfiguration.NoHostileNames[territoryId] = libs;
                    OtherConfiguration.SaveNoHostileNames();
                }
                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Remove Territory Target Name {i}"))
                {
                    removeIndex = i;
                }
            }
            if (removeIndex > -1)
            {
                var list = libs.ToList();
                list.RemoveAt(removeIndex);
                OtherConfiguration.NoHostileNames[territoryId] = [.. list];
                OtherConfiguration.SaveNoHostileNames();
            }

            ImGui.TableNextColumn();
            ImGui.TextWrapped(UiString.ConfigWindow_List_NoProvokeDesc.GetDescription());

            width = ImGui.GetColumnWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X - ImGui.GetStyle().ItemSpacing.X - 10 * Scale;

            if (!OtherConfiguration.NoProvokeNames.TryGetValue(territoryId, out libs))
            {
                OtherConfiguration.NoProvokeNames[territoryId] = libs = [];
            }

            // Add one.
            if (!libs.Any(string.IsNullOrEmpty))
            {
                OtherConfiguration.NoProvokeNames[territoryId] = [.. libs, string.Empty];
            }

            removeIndex = -1;
            for (int i = 0; i < libs.Length; i++)
            {
                ImGui.SetNextItemWidth(width);
                if (ImGui.InputTextWithHint($"##Rotation Solver Reborn Territory Provoke Name {i}", UiString.ConfigWindow_List_NoProvokeName.GetDescription(), ref libs[i], 1024))
                {
                    OtherConfiguration.NoProvokeNames[territoryId] = libs;
                    OtherConfiguration.SaveNoProvokeNames();
                }
                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Reborn Remove Territory Provoke Name {i}"))
                {
                    removeIndex = i;
                }
            }
            if (removeIndex > -1)
            {
                var list = libs.ToList();
                list.RemoveAt(removeIndex);
                OtherConfiguration.NoProvokeNames[territoryId] = [.. list];
                OtherConfiguration.SaveNoProvokeNames();
            }

            ImGui.TableNextColumn();

            if (!OtherConfiguration.BeneficialPositions.TryGetValue(territoryId, out var pts))
            {
                OtherConfiguration.BeneficialPositions[territoryId] = pts = [];
            }

            if (ImGui.Button(UiString.ConfigWindow_List_AddPosition.GetDescription()) && Player.Available) unsafe
                {
                    var point = Player.Object.Position;
                    var pointMathed = point + Vector3.UnitY * 5;
                    var direction = Vector3.UnitY;
                    Vector3* directionPtr = &direction;
                    Vector3* pointPtr = &pointMathed;
                    int* unknown = stackalloc int[] { 0x4000, 0, 0x4000, 0 };

                    RaycastHit hit = default;

                    OtherConfiguration.BeneficialPositions[territoryId] =
                    [
                        .. pts,
                    Framework.Instance()->BGCollisionModule
                        ->RaycastMaterialFilter(&hit, pointPtr, directionPtr, 20, 1, unknown) ? hit.Point : point,
                ];
                    OtherConfiguration.SaveBeneficialPositions();
                }

            HoveredPosition = Vector3.Zero;
            removeIndex = -1;
            for (int i = 0; i < pts.Length; i++)
            {
                void Reset() => removeIndex = i;

                var key = "Beneficial Positions" + i.ToString();

                ImGuiHelper.DrawHotKeysPopup(key, string.Empty, (UiString.ConfigWindow_List_Remove.GetDescription(), Reset, ["Delete"]));

                ImGui.Selectable(pts[i].ToString());

                if (ImGui.IsItemHovered())
                {
                    HoveredPosition = pts[i];
                }

                ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, false, (Reset, [VirtualKey.DELETE]));
            }
            if (removeIndex > -1)
            {
                var list = pts.ToList();
                list.RemoveAt(removeIndex);
                OtherConfiguration.BeneficialPositions[territoryId] = [.. list];
                OtherConfiguration.SaveBeneficialPositions();
            }
        }
    }

    internal static void DrawContentFinder(uint imageId)
    {
        const float MaxWidth = 480f;
        var badge = imageId;
        if (badge != 0
            && IconSet.GetTexture(badge, out var badgeTexture))
        {
            var wholeWidth = ImGui.GetWindowWidth();
            var size = new Vector2(badgeTexture.Width, badgeTexture.Height) * MathF.Min(1, MathF.Min(MaxWidth, wholeWidth) / badgeTexture.Width);

            ImGuiHelper.DrawItemMiddle(() =>
            {
                ImGui.Image(badgeTexture.ImGuiHandle, size);
            }, wholeWidth, size.X);
        }
    }

    #endregion

    #region Debug
    private static void DrawDebug()
    {
        _allSearchable.DrawItems(Configs.Debug);

        if (!Player.Available || !Service.Config.InDebug) return;

        _debugHeader?.Draw();

        if (ImGui.Button("Reset Action Configs"))
        {
            DataCenter.ResetActionConfigs = DataCenter.ResetActionConfigs != true;
        }
        ImGui.Text($"Reset Action Configs: {DataCenter.ResetActionConfigs}");
        if (ImGui.Button("Add Test Warning"))
        {
#pragma warning disable CS0436
            WarningHelper.AddSystemWarning("This is a test warning.");
        }
    }

    private static readonly CollapsingHeaderGroup _debugHeader = new(new()
    {
        {() => DataCenter.CurrentRotation != null ? "Rotation" : string.Empty, DrawDebugRotationStatus},
        {() => "Player Status", DrawStatus },
        {() => "Party", DrawParty },
        {() => "Target Data", DrawTargetData },
        {() => "Next Action", DrawNextAction },
        {() => "Last Action", DrawLastAction },
        {() => "Others", DrawOthers },
        {() => "GCD Cooldown Visualization", DrawGCDCooldownStuff },
        {() => "Effect", () =>
            {
                ImGui.Text(Watcher.ShowStrSelf);
                ImGui.Separator();
                ImGui.Text(DataCenter.Role.ToString());
            } },
    });

    private static void DrawDebugRotationStatus()
    {
        DataCenter.CurrentRotation?.DisplayStatus();
    }

    private static unsafe void DrawStatus()
    {
        ImGui.Text($"Merged Status: {DataCenter.MergedStatus}");
        if ((IntPtr)FateManager.Instance() != IntPtr.Zero)
        {
            ImGui.Text($"Fate: {DataCenter.FateId}");
        }
        ImGui.Text($"Height: {Player.Character->ModelContainer.CalculateHeight()}");
        var conditions = Svc.Condition.AsReadOnlySet().ToArray();
        ImGui.Text("InternalCondition:");
        foreach (var condition in conditions)
        {
            ImGui.Text($"    {condition}");
        }
        ImGui.Text($"OnlineStatus: {Player.OnlineStatus}");
        ImGui.Text($"IsDead: {Player.Object.IsDead}");
        ImGui.Text($"Dead Time: {DataCenter.DeadTimeRaw}");
        ImGui.Text($"Alive Time: {DataCenter.AliveTimeRaw}");
        ImGui.Text($"Moving: {DataCenter.IsMoving}");
        ImGui.Text($"Moving Time: {DataCenter.MovingRaw}");
        ImGui.Text($"Stop Moving: {DataCenter.StopMovingRaw}");
        ImGui.Text($"CountDownTime: {Service.CountDownTime}");
        ImGui.Text($"Combo Time: {DataCenter.ComboTime}");
        ImGui.Text($"TargetingType: {DataCenter.TargetingType}");
        ImGui.Text($"DeathTarget: {DataCenter.DeathTarget}");
        foreach (var item in DataCenter.AttackedTargets)
        {
            ImGui.Text(item.id.ToString());
        }

        // VFX info
        //ImGui.Text("VFX Data:");
        //foreach (var item in DataCenter.VfxDataQueue)
        //{
        //    ImGui.Text(item.ToString());
        //}

        // Check and display VFX casting status
        ImGui.Text($"Is Casting Tank VFX: {DataCenter.IsCastingTankVfx()}");
        ImGui.Text($"Is Casting Area VFX: {DataCenter.IsCastingAreaVfx()}");
        ImGui.Text($"Is Hostile Casting Stop: {DataCenter.IsHostileCastingStop}");
        ImGui.Text($"VfxDataQueue: {DataCenter.VfxDataQueue.Count}");

        // Check and display VFX casting status
        ImGui.Text("Casting Vfx:");
        var filteredVfx = DataCenter.VfxDataQueue
            .Where(s => s.Path.StartsWith("vfx/lockon/eff/") && s.TimeDuration.TotalSeconds is > 0 and < 6);
        foreach (var vfx in filteredVfx)
        {
            ImGui.Text($"Path: {vfx.Path}");
        }

        // Display dead party members
        var deadPartyMembers = DataCenter.PartyMembers.GetDeath();
        if (deadPartyMembers.Any())
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

        // Display all party members
        var partyMembers = DataCenter.PartyMembers;
        if (partyMembers.Count != 0)
        {
            ImGui.Text("Party Members:");
            foreach (var member in partyMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Party Members: None");
        }

        // Display all party members
        var friendlyNPCMembers = DataCenter.FriendlyNPCMembers;
        if (friendlyNPCMembers.Count != 0)
        {
            ImGui.Text("Friendly NPC Members:");
            foreach (var member in friendlyNPCMembers)
            {
                ImGui.Text($"- {member.Name}");
            }
        }
        else
        {
            ImGui.Text("Friendly NPC Members: None");
        }

        // Display dispel target
        var dispelTarget = DataCenter.DispelTarget;
        if (dispelTarget != null)
        {
            ImGui.Text("Dispel Target:");
            ImGui.Text($"- {dispelTarget.Name}");
        }
        else
        {
            ImGui.Text("Dispel Target: None");
        }

        ImGui.Text($"TerritoryType: {DataCenter.Territory?.ContentType}");
        ImGui.Text($"DPSTaken: {DataCenter.DPSTaken}");
        ImGui.Text($"IsHostileCastingToTank: {DataCenter.IsHostileCastingToTank}");
        ImGui.Text($"CurrentRotation: {DataCenter.CurrentRotation}");
        ImGui.Text($"Job: {DataCenter.Job}");
        ImGui.Text($"JobRange: {DataCenter.JobRange}");
        ImGui.Text($"Job Role: {DataCenter.Role}");
        ImGui.Text($"Have pet: {DataCenter.HasPet}");
        ImGui.Text($"Hostile Near Count: {DataCenter.NumberOfHostilesInRange}");
        ImGui.Text($"Hostile Near Count Max Range: {DataCenter.NumberOfHostilesInMaxRange}");
        ImGui.Text($"Have Companion: {DataCenter.HasCompanion}");
        ImGui.Text($"MP: {DataCenter.CurrentMp}");
        ImGui.Text($"Count Down: {Service.CountDownTime}");

        foreach (var status in Player.Object.StatusList)
        {
            var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
            ImGui.Text($"{status.GameData.Value.Name}: {status.StatusId} From: {source}");
        }
    }

    private static unsafe void DrawParty()
    {
        ImGui.Text($"Your combat state: {DataCenter.InCombat}");
        ImGui.Text($"Number of Party Members: {DataCenter.PartyMembers.Count}");
        ImGui.Text($"Is in Alliance Raid: {DataCenter.IsInAllianceRaid}");
        ImGui.Text($"Number of Alliance Members: {DataCenter.AllianceMembers.Count}");
        ImGui.Text($"Average Party HP Percent: {DataCenter.PartyMembersAverHP * 100}");
        foreach (var p in Svc.Party)
        {
            if (p.GameObject is not IBattleChara b) continue;

            var text = $"Name: {b.Name}, In Combat: {b.InCombat()}";
            if (b.TimeAlive() > 0) text += $", Time Alive: {b.TimeAlive()}";
            if (b.TimeDead() > 0) text += $", Time Dead: {b.TimeDead()}";
            ImGui.Text(text);
        }
    }

    private static unsafe void DrawTargetData()
    {
        var target = Svc.Targets.Target;
        if (target == null) return;

        ImGui.Text($"Height: {target.Struct()->Height}");
        ImGui.Text($"Kind: {target.GetObjectKind()}");
        ImGui.Text($"SubKind: {target.GetBattleNPCSubKind()}");

        var owner = Svc.Objects.SearchById(target.OwnerId);
        if (owner != null)
        {
            ImGui.Text($"Owner: {owner.Name}");
        }

        if (target is IBattleChara battleChara)
        {
            ImGui.Text($"HP: {battleChara.CurrentHp} / {battleChara.MaxHp}");
            ImGui.Text($"Is Current Focus Target: {battleChara.IsFocusTarget()}");
            ImGui.Text($"TTK: {battleChara.GetTimeToKill()}");
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
            ImGui.Text($"Is Alliance: {battleChara.IsAllianceMember()}");
            ImGui.Text($"Is Enemy: {battleChara.IsEnemy()}");
            ImGui.Text($"Distance To Player: {battleChara.DistanceToPlayer()}");
            ImGui.Text($"Is In EnemiesList: {battleChara.IsInEnemiesList()}");
            ImGui.Text($"Is Attackable: {battleChara.IsAttackable()}");
            ImGui.Text($"Is Jeuno Boss Immune: {battleChara.IsJeunoBossImmune()}");
            ImGui.Text($"CanProvoke: {battleChara.CanProvoke()}");
            ImGui.Text($"EventType: {battleChara.GetEventType()}");
            ImGui.Text($"NamePlate: {battleChara.GetNamePlateIcon()}");
            ImGui.Text($"StatusFlags: {battleChara.StatusFlags}");
            ImGui.Text($"InView: {Svc.GameGui.WorldToScreen(battleChara.Position, out _)}");
            ImGui.Text($"Name Id: {battleChara.NameId}");
            ImGui.Text($"Data Id: {battleChara.DataId}");
            ImGui.Text($"Enemy Positional: {battleChara.FindEnemyPositional()}");
            ImGui.Text($"NameplateKind: {battleChara.GetNameplateKind()}");
            ImGui.Text($"BattleNPCSubKind: {battleChara.GetBattleNPCSubKind()}");
            ImGui.Text($"Is Top Priority Hostile: {battleChara.IsTopPriorityHostile()}");
            ImGui.Text($"Is Others Players: {battleChara.IsOthersPlayers()}");
            ImGui.Text($"Targetable: {battleChara.Struct()->Character.GameObject.TargetableStatus}");

            foreach (var status in battleChara.StatusList)
            {
                var source = status.SourceId == Player.Object.GameObjectId ? "You" : Svc.Objects.SearchById(status.SourceId) == null ? "None" : "Others";
                ImGui.Text($"{status.GameData.Value.Name}: {status.StatusId} From: {source}");
            }
        }

        ImGui.Text($"All: {DataCenter.AllTargets.Count()}");
        ImGui.Text($"Hostile: {DataCenter.AllHostileTargets.Count()}");
        foreach (var item in DataCenter.AllHostileTargets)
        {
            ImGui.Text(item.Name.ToString());
        }
    }

    private static void DrawNextAction()
    {
        ImGui.Text(DataCenter.CurrentRotation?.GetType().GetCustomAttribute<RotationAttribute>()!.Name);
        ImGui.Text(DataCenter.SpecialType.ToString());

        ImGui.Text(ActionUpdater.NextAction?.Name ?? "null");
        ImGui.Text($"GCD Total: {DataCenter.DefaultGCDTotal}");
        ImGui.Text($"GCD Remain: {DataCenter.DefaultGCDRemain}");
        ImGui.Text($"GCD Elapsed: {DataCenter.DefaultGCDElapsed}");
        ImGui.Text($"Calculated Action Ahead: {DataCenter.CalculatedActionAhead}");
        ImGui.Text($"Actual Action Ahead: {DataCenter.ActionAhead}");
        ImGui.Text($"Animation Lock Delay: {ActionManagerHelper.GetCurrentAnimationLock()}");
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

    private static void DrawOthers()
    {
        ImGui.Text($"Combat Time: {DataCenter.CombatTimeRaw}");
        ImGui.Text($"Limit Break: {CustomRotation.LimitBreakLevel}");
    }

    private static float _maxAnimationLockTime = 0;

    private static void DrawGCDCooldownStuff()
    {
        ImGui.Text("GCD Cooldown Visualization");

        ImGui.Text($"GCD Elapsed: {DataCenter.DefaultGCDElapsed}");
        ImGui.Text($"GCD Remain: {DataCenter.DefaultGCDRemain}");
        ImGui.Text($"GCD Total: {DataCenter.DefaultGCDTotal}");

        // Visualize the GCD and oGCD slots
        float gcdTotal = DataCenter.DefaultGCDElapsed + DataCenter.DefaultGCDRemain;
        float gcdProgress = DataCenter.DefaultGCDElapsed / gcdTotal;

        // Draw the progress bar
        ImGui.ProgressBar(gcdProgress, new Vector2(-1, 0), $"{DataCenter.DefaultGCDElapsed:F2}s / {gcdTotal:F2}s");

        // Update the maximum Animation Lock Time if the current value is larger
        float currentAnimationLockTime = ActionManagerHelper.GetCurrentAnimationLock(); // Assuming you have this value
        if (currentAnimationLockTime > _maxAnimationLockTime)
        {
            _maxAnimationLockTime = currentAnimationLockTime;
        }

        // Calculate the position for the Animation Lock Delay marker
        float markerPosition = _maxAnimationLockTime / gcdTotal;

        // Draw the marker on the progress bar
        Vector2 cursorPos = ImGui.GetCursorPos();
        float progressBarWidth = ImGui.GetContentRegionAvail().X;
        float markerXPos = cursorPos.X + (progressBarWidth * markerPosition);

        // Draw the marker on the same line as the progress bar
        ImGui.SetCursorPos(new Vector2(markerXPos, cursorPos.Y - ImGui.GetTextLineHeight() / 2));
        ImGui.Text("|");

        // Check if the marker is hovered and display a tooltip
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Most recent recorded animation lock delay");
        }

        // Reset cursor position
        ImGui.SetCursorPos(cursorPos);

        // Add space below the progress bar
        ImGui.Dummy(new Vector2(0, 20));

        // Add any additional visualization for oGCD slots if needed
    }

    private static void DrawAction(ActionID id, string type)
    {
        ImGui.Text($"{type}: {id}");
    }

    private static bool BeginChild(string str_id, Vector2 size)
    {
        if (IsFailed()) return false;
        return ImGui.BeginChild(str_id, size);
    }

    private static bool BeginChild(string str_id, Vector2 size, bool border, ImGuiWindowFlags flags)
    {
        if (IsFailed()) return false;
        return ImGui.BeginChild(str_id, size, border, flags);
    }

    private static bool IsFailed()
    {
        var style = ImGui.GetStyle();
        var min = style.WindowPadding.X + style.WindowBorderSize;
        var columnWidth = ImGui.GetColumnWidth();
        var windowSize = ImGui.GetWindowSize();
        var cursor = ImGui.GetCursorPos();

        return columnWidth > 0 && columnWidth <= min
            || windowSize.Y - cursor.Y <= min
            || windowSize.X - cursor.X <= min;
    }
    #endregion
}
