using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Configuration.Conditions;
using RotationSolver.Data;

using RotationSolver.UI.SearchableSettings;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

public partial class RotationConfigWindow
{
    private string _searchText = string.Empty;
    private ISearchable[] _searchResults = new ISearchable[0];

    internal static SearchableCollection _allSearchable = new SearchableCollection();

    private void SearchingBox()
    {
        if (ImGui.InputTextWithHint("##Rotation Solver Reborn Search Box", "Setting Search", ref _searchText, 128, ImGuiInputTextFlags.AutoSelectAll))
        {
            _searchResults = _allSearchable.SearchItems(_searchText);
        }
    }

    #region Basic
    private static void DrawBasic()
    {
        _baseHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _baseHeader = new CollapsingHeaderGroup(new Dictionary<Func<string>, Action>
    {
        { () => "Timer", DrawBasicTimer },
        { () => "Named Conditions", DrawBasicNamedConditions },
        { () => "Others", DrawBasicOthers },
    });

    private static readonly uint PING_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGreen);
    private static readonly uint LOCK_TIME_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedBlue);
    private static readonly uint WEAPON_DELAY_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold);
    private static readonly uint IDEAL_CLICK_TIME_COLOR = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0f, 0f, 1f));
    private static readonly uint CLICK_TIME_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedPink);
    private static readonly uint ADVANCE_TIME_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow);
    private static readonly uint ADVANCE_ABILITY_TIME_COLOR = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedOrange);
    const float gcdSize = 50, ogcdSize = 40, pingHeight = 12, spacingHeight = 8;

    private static unsafe void AddPingLockTime(ImDrawListPtr drawList, Vector2 lineStart, float sizePerTime, float ping, float animationLockTime, float advanceTime, uint color, float clickTime)
    {
        if (drawList.NativePtr == null) throw new ArgumentNullException(nameof(drawList));

        const float pingHeight = 12;
        const float spacingHeight = 8;
        const float lineThickness = 1.5f;
        const float clickLineThickness = 2.5f;

        var size = new Vector2(ping * sizePerTime, pingHeight);
        drawList.AddRectFilled(lineStart, lineStart + size, ChangeAlpha(PING_COLOR));
        if (ImGuiHelper.IsInRect(lineStart, size))
        {
            ImguiTooltips.ShowTooltip("The ping time.\nIn RSR, this means the time from sending the action request to receiving the success message from the server.");
        }

        var rectStart = lineStart + new Vector2(ping * sizePerTime, 0);
        size = new Vector2(animationLockTime * sizePerTime, pingHeight);
        drawList.AddRectFilled(rectStart, rectStart + size, ChangeAlpha(LOCK_TIME_COLOR));
        if (ImGuiHelper.IsInRect(rectStart, size))
        {
            ImguiTooltips.ShowTooltip("The animation lock time for individual actions. For example, 0.6s.");
        }

        drawList.AddLine(lineStart - new Vector2(0, spacingHeight), lineStart + new Vector2(0, pingHeight * 2 + spacingHeight / 2), IDEAL_CLICK_TIME_COLOR, lineThickness);

        rectStart = lineStart + new Vector2(-advanceTime * sizePerTime, pingHeight);
        size = new Vector2(advanceTime * sizePerTime, pingHeight);
        drawList.AddRectFilled(rectStart, rectStart + size, ChangeAlpha(color));
        if (ImGuiHelper.IsInRect(rectStart, size))
        {
            ImguiTooltips.ShowTooltip(() =>
            {
                ImGui.TextWrapped("The clicking duration - RSR will try to click at this moment.");

                ImGui.Separator();

                ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(IDEAL_CLICK_TIME_COLOR),
                    "The ideal click time");

                ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(CLICK_TIME_COLOR),
                    "The actual click time");
            });
        }

        float time = 0;
        while (time < advanceTime)
        {
            var start = lineStart + new Vector2((time - advanceTime) * sizePerTime, 0);
            drawList.AddLine(start + new Vector2(0, pingHeight), start + new Vector2(0, pingHeight * 2 + spacingHeight), CLICK_TIME_COLOR, clickLineThickness);

            time += clickTime;
        }
    }

    private static void DrawBasicTimer()
    {
        _allSearchable.DrawItems(Configs.BasicTimer);
    }

    private static readonly CollapsingHeaderGroup _autoSwitch = new CollapsingHeaderGroup(new Dictionary<Func<string>, Action>
    {
        {
            () => "Auto turn-off RSR conditions",
            () => DataCenter.CurrentConditionValue.SwitchCancelConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Auto manual targetting mode conditions",
            () => DataCenter.CurrentConditionValue.SwitchManualConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Auto automatic targetting mode conditions",
            () => DataCenter.CurrentConditionValue.SwitchAutoConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
    })
    {
        HeaderSize = 18,
    };

    private static readonly Dictionary<int, bool> _isOpen = new Dictionary<int, bool>();

    private static void DrawBasicNamedConditions()
    {
        // Ensure there is always an empty named condition at the end
        if (!DataCenter.CurrentConditionValue.NamedConditions.Any(c => string.IsNullOrEmpty(c.Name)))
        {
            DataCenter.CurrentConditionValue.NamedConditions = DataCenter.CurrentConditionValue.NamedConditions.Append((string.Empty, new ConditionSet())).ToArray();
        }

        ImGui.Spacing();

        int removeIndex = -1;
        for (int i = 0; i < DataCenter.CurrentConditionValue.NamedConditions.Length; i++)
        {
            var value = _isOpen.TryGetValue(i, out var open) && open;

            var toggle = value ? FontAwesomeIcon.ArrowUp : FontAwesomeIcon.ArrowDown;
            float ItemSpacing = 20 * Scale; // Changed from const to local variable
            var width = ImGui.GetWindowWidth() - ImGuiEx.CalcIconSize(FontAwesomeIcon.Ban).X
                - ImGuiEx.CalcIconSize(toggle).X - ImGui.GetStyle().ItemSpacing.X * 2 - ItemSpacing;

            ImGui.SetNextItemWidth(width);
            ImGui.InputTextWithHint($"##Rotation Solver Named Condition{i}", "Condition Name",
                ref DataCenter.CurrentConditionValue.NamedConditions[i].Name, 1024);

            ImGui.SameLine();

            if (ImGuiEx.IconButton(toggle, $"##Rotation Solver Toggle Named Condition{i}"))
            {
                _isOpen[i] = value = !value;
            }

            ImGui.SameLine();

            if (ImGuiEx.IconButton(FontAwesomeIcon.Ban, $"##Rotation Solver Remove Named Condition{i}"))
            {
                removeIndex = i;
            }

            if (value && DataCenter.CurrentRotation != null)
            {
                DataCenter.CurrentConditionValue.NamedConditions[i].Condition?.DrawMain(DataCenter.CurrentRotation);
            }
        }

        // Remove the named condition if needed
        if (removeIndex > -1)
        {
            var list = DataCenter.CurrentConditionValue.NamedConditions.ToList();
            list.RemoveAt(removeIndex);
            DataCenter.CurrentConditionValue.NamedConditions = list.ToArray();
        }
    }

    private static void DrawBasicOthers()
    {
        var set = DataCenter.CurrentConditionValue;

        const string popUpId = "Right Set Popup";
        if (ImGui.Selectable(set.Name, false, ImGuiSelectableFlags.None, new Vector2(0, 20)))
        {
            ImGui.OpenPopup(popUpId);
        }
        ImguiTooltips.HoveredTooltip("The condition value you chose. Click to modify.");

        using var popup = ImRaii.Popup(popUpId);
        if (popup)
        {
            var combos = DataCenter.ConditionSets;
            for (int i = 0; i < combos.Length; i++)
            {
                void DeleteFile()
                {
                    ActionSequencerUpdater.Delete(combos[i].Name);
                }

                if (combos[i].Name == set.Name)
                {
                    ImGuiHelper.SetNextWidthWithName(set.Name);
                    ImGui.InputText("##MajorConditionValue", ref set.Name, 100);
                }
                else
                {
                    var key = "Condition Set At " + i.ToString();
                    ImGuiHelper.DrawHotKeysPopup(key, string.Empty, ("Remove", DeleteFile, ["Delete"]));

                    if (ImGui.Selectable(combos[i].Name))
                    {
                        Service.Config.ActionSequencerIndex = i;
                    }

                    ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, false,
                        (DeleteFile, [VirtualKey.DELETE]));
                }
            }

            ImGui.PushFont(UiBuilder.IconFont);

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            if (ImGui.Selectable(FontAwesomeIcon.Plus.ToIconString()))
            {
                ActionSequencerUpdater.AddNew();
            }
            ImGui.PopStyleColor();

            if (ImGui.Selectable(FontAwesomeIcon.FileDownload.ToIconString()))
            {
                ActionSequencerUpdater.LoadFiles();
            }

            ImGui.PopFont();
            ImguiTooltips.HoveredTooltip("Load a condition set from a file.");
        }
        _allSearchable.DrawItems(Configs.BasicParams);
    }
    #endregion

    #region UI
    private static void DrawUI()
    {
        _UIHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _UIHeader = new CollapsingHeaderGroup(new Dictionary<Func<string>, Action>
    {
        {
            () => "Information",
            () => _allSearchable.DrawItems(Configs.UiInformation)
        },
        {
            () => "Windows",
            () => _allSearchable.DrawItems(Configs.UiWindows)
        },
    });

    #endregion

    #region Auto
    private const int HeaderSize = 18;

    /// <summary>
    /// Draws the auto section of the configuration window.
    /// </summary>
    private void DrawAuto()
    {
        ImGui.TextWrapped("Change how RSR automatically uses actions");
        _autoHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _autoHeader = new(new Dictionary<Func<string>, Action>
    {
        { () => "Auto Switch", DrawBasicAutoSwitch },
        { () => "Reorder AutoStatus Priorities", DrawAutoStatusOrderConfig },
        { () => "Action Usage Control", DrawActionUsageControl },
        { () => "Healing Usage and Control", DrawHealingActionCondition },
        { () => "PvP-Specific Controls", DrawPvPSpecificControls },
        { () => "Custom State Condition", () => _autoState?.Draw() },
    })
    {
        HeaderSize = HeaderSize,
    };

    private static void DrawBasicAutoSwitch()
    {
        _allSearchable.DrawItems(Configs.BasicAutoSwitch);
        _autoSwitch?.Draw();
    }

    private static void DrawPvPSpecificControls()
    {
        ImGui.TextWrapped("PvP-Specific Controls");
        ImGui.Separator();
        _allSearchable.DrawItems(Configs.PvPSpecificControls);
    }

    /// <summary>
    /// Draws the Action Usage and Control section.
    /// </summary>
    private static void DrawActionUsageControl()
    {
        ImGui.TextWrapped("Which actions RSR can use");
        ImGui.Separator();
        _allSearchable.DrawItems(Configs.AutoActionUsage);
    }

    /// <summary>
    /// Draws the healing action condition section.
    /// </summary>
    private static void DrawHealingActionCondition()
    {
        ImGui.TextWrapped("How RSR should use healing abilities");
        ImGui.Separator();
        _allSearchable.DrawItems(Configs.HealingActionCondition);
    }

    private static readonly CollapsingHeaderGroup _autoState = new(new Dictionary<Func<string>, Action>
    {
        {
            () => "Force Use AOE Heal Condition",
            () => DataCenter.CurrentConditionValue.HealAreaConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Single Target Heal Condition",
            () => DataCenter.CurrentConditionValue.HealSingleConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use AOE Defense Condition",
            () => DataCenter.CurrentConditionValue.DefenseAreaConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Single Target Defense Condition",
            () => DataCenter.CurrentConditionValue.DefenseSingleConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Dispel/Stance/Positional Condition",
            () => DataCenter.CurrentConditionValue.DispelStancePositionalConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Raise/Shirk Condition",
            () => DataCenter.CurrentConditionValue.RaiseShirkConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Move Forward Action Condition",
            () => DataCenter.CurrentConditionValue.MoveForwardConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Move Back Action Condition",
            () => DataCenter.CurrentConditionValue.MoveBackConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Anti-Knockback Condition",
            () => DataCenter.CurrentConditionValue.AntiKnockbackConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force Use Speed Action Condition",
            () => DataCenter.CurrentConditionValue.SpeedConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
        {
            () => "Force No Casting Actions Condition",
            () => DataCenter.CurrentConditionValue.NoCastingConditionSet?.DrawMain(DataCenter.CurrentRotation)
        },
    })
    {
        HeaderSize = HeaderSize,
    };
    #endregion

    #region Target
    private static void DrawTarget()
    {
        _targetHeader?.Draw();
    }

    /// <summary>
    /// Header group for target-related configurations.
    /// </summary>
    private static readonly CollapsingHeaderGroup _targetHeader = new(new Dictionary<Func<string>, Action>
    {
    { () => "Targeting Configuration", DrawTargetConfig },
    { () => "Targeting Modes Configuration", DrawTargetHostile },
    { () => "Priority Targets Configuration", DrawTargetPriority },
});

    /// <summary>
    /// Draws the target configuration items.
    /// </summary>
    private static void DrawTargetConfig()
    {
        _allSearchable.DrawItems(Configs.TargetConfig);
    }

    private static void DrawTargetHostile()
    {
        if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add Hostile"))
        {
            Service.Config.TargetingTypes.Add(TargetingType.Big);
        }
        ImGui.SameLine();
        ImGui.TextWrapped("Enemy targeting logic. Adding more options cycles them when using /rotation Auto.\nUse /rotation Settings TargetingTypes add <option> to add,\n/rotation Settings TargetingTypes remove <option> to remove,\nand /rotation Settings TargetingTypes removeall to remove all options.");

        for (int i = 0; i < Service.Config.TargetingTypes.Count; i++)
        {
            var targetType = Service.Config.TargetingTypes[i];
            var key = $"TargetingTypePopup_{i}";

            void Delete()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
            }

            void Up()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
                Service.Config.TargetingTypes.Insert(Math.Max(0, i - 1), targetType);
            }

            void Down()
            {
                Service.Config.TargetingTypes.RemoveAt(i);
                Service.Config.TargetingTypes.Insert(Math.Min(Service.Config.TargetingTypes.Count - 1, i + 1), targetType);
            }

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                ("Remove", Delete, ["Delete"]),
                ("Move Up", Up, ["?"]),
                ("Move Down", Down, ["?"]));

            var names = Enum.GetNames(typeof(TargetingType));
            var targetingType = (int)Service.Config.TargetingTypes[i];
            var text = "Hostile target selection condition";
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(text).X + 30 * Scale);
            if (ImGui.Combo(text + "##HostileCondition" + i, ref targetingType, names, names.Length))
            {
                Service.Config.TargetingTypes[i] = (TargetingType)targetingType;
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
                (Delete, new[] { VirtualKey.DELETE }),
                (Up, new[] { VirtualKey.UP }),
                (Down, new[] { VirtualKey.DOWN }));
        }
    }

    private static void DrawTargetPriority()
    {
        // Convert HashSet<uint> to string[] for ImGui input
        var prioIdSet = OtherConfiguration.PrioTargetId;
        string[] prioId = prioIdSet.Select(id => id.ToString()).ToArray();

        // Begin new column for Prioritized Target Names
        ImGui.TableNextColumn();
        ImGui.TextWrapped("Enemies that will be prioritized. This system is under construction and experimental but should be stable.");

        // List all DataIds in the current list
        ImGui.Text("Current Priority DataIds:");
        foreach (var id in prioIdSet)
        {
            ImGui.Text(id.ToString());
        }

        ImGui.TableNextColumn();
        if (ImGui.Button("Reset and Update Target Priority List"))
        {
            OtherConfiguration.ResetPrioTargetId();
        }

        // Render a button to add the DataId of the current target
        if (ImGui.Button("Add Current Target"))
        {
            var currentTarget = Svc.Targets.Target;
            if (currentTarget != null)
            {
                uint dataId = currentTarget.DataId;
                PriorityTargetHelper.AddPriorityTarget(dataId);
                prioIdSet.Add(dataId);
                OtherConfiguration.PrioTargetId = prioIdSet;
                OtherConfiguration.SavePrioTargetId();
            }
        }
    }
    #endregion

    #region Extra
    private static void DrawExtra()
    {
        ImGui.TextWrapped("RSR focuses on the rotation itself. These are side features. Subject to removal at any time.");
        _extraHeader?.Draw();
    }

    private static readonly CollapsingHeaderGroup _extraHeader = new(new Dictionary<Func<string>, Action>
    {
    { () => "Events", DrawEventTab },
    {
        () => "Others",
        () => _allSearchable.DrawItems(Configs.Extra)
    },
});

    private static void DrawEventTab()
    {
        if (ImGui.Button("Add Events"))
        {
            Service.Config.Events.Add(new ActionEventInfo());
        }
        ImGui.SameLine();

        ImGui.TextWrapped("In this window, you can set which macro will be triggered after using an action.");

        ImGui.Text("Duty Start: ");
        ImGui.SameLine();
        Service.Config.DutyStart.DisplayMacro();

        ImGui.Text("Duty End: ");
        ImGui.SameLine();
        Service.Config.DutyEnd.DisplayMacro();

        ImGui.Separator();

        for (int i = 0; i < Service.Config.Events.Count; i++)
        {
            var eve = Service.Config.Events[i];
            eve.DisplayEvent();

            ImGui.SameLine();

            if (ImGui.Button($"{"Delete Event"}##RemoveEvent{eve.GetHashCode()}"))
            {
                Service.Config.Events.RemoveAt(i);
                i--; // Adjust index after removal
            }
            ImGui.Separator();
        }
    }
    #endregion
}

