using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.Localization;
using RotationSolver.UI.HighlightTeachingMode;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.Updaters;

internal static class MajorUpdater
{
    public static bool IsValid => Svc.Condition.Any()
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.BetweenAreas51]
        && !Svc.Condition[ConditionFlag.LoggingOut]
        && Player.Available;

    static bool _showedWarning, _work;
    static Exception? _threadException;
    static DateTime _lastUpdatedWork = DateTime.Now;
    static DateTime _warningsLastDisplayed = DateTime.MinValue;

    private unsafe static void FrameworkUpdate(IFramework framework)
    {
        //HotbarHighlightDrawerManager.HotbarIDs.Clear();
        HotbarHighlightManager.HotbarIDs.Clear();
        RotationSolverPlugin.UpdateDisplayWindow();
        if (!IsValid)
        {
            ActionUpdater.ClearNextAction();
            CustomRotation.MoveTarget = null;
            return;
        }

        if (DataCenter.SystemWarnings.Any())
        {
            foreach (var warning in DataCenter.SystemWarnings)
            {
                if ((warning.Value + new TimeSpan(0, 10, 0)) < DateTime.Now)
                {
                    DataCenter.SystemWarnings.Remove(warning.Key);
                    continue;
                }
            }
            if (_warningsLastDisplayed + new TimeSpan(0, 10, 0) < DateTime.Now)
            {
                _warningsLastDisplayed = DateTime.Now;
                WarningHelper.ShowWarning("System warnings are present.");
            }
        }

        try
        {
            SocialUpdater.UpdateSocial();
            PreviewUpdater.UpdatePreview();
            UpdateHighlight();

            // Just sets a local variable, I don't think this is used or needed for anything
            //if (Service.Config.TeachingMode && ActionUpdater.NextAction != null)
            //{
            //    //Sprint action id is 3 however the id in hot bar is 4.
            //    var id = ActionUpdater.NextAction.AdjustedID;
            //}

            ActionUpdater.UpdateActionInfo();

            var canDoAction = ActionUpdater.CanDoAction();
            MovingUpdater.UpdateCanMove(canDoAction);
            if (canDoAction)
            {
                RSCommands.DoAction();
            }

            MacroUpdater.UpdateMacro();

            CloseWindow();
            OpenChest();
        }
        catch (Exception ex)
        {
            if (_threadException != ex)
            {
                _threadException = ex;
                Svc.Log.Error(ex, "Main Thread Exception");
            }
        }

        try
        {
            if (_work) return;
            if (DateTime.Now - _lastUpdatedWork < TimeSpan.FromSeconds(Service.Config.MinUpdatingTime))
                return;

            _work = true;
            _lastUpdatedWork = DateTime.Now;

            if (Service.Config.UseWorkTask)
            {
                Task.Run(UpdateWork);
            }
            else
            {
                UpdateWork();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Worker Exception");
        }
    }

    private static XivChatEntry BuildWarningChatEntry()
    {
        DalamudLinkPayload linkPayload = Svc.PluginInterface.AddChatLinkHandler(3, OpenWarningChatHandler);
        XivChatEntry entry = new()
        {
            Message = new SeString(new TextPayload("RotationSolver Reborn: System warnings are present. Click here to view."), linkPayload),
            Type = XivChatType.ErrorMessage,
        };
        return entry;
    }

    private static void OpenWarningChatHandler(uint arg1, SeString @string)
    {
        if (arg1 == 3)
        {
            RotationSolverPlugin.OpenConfigWindow();
        }
    }

    /// <summary>
    /// <br>Updates next to-be highlighted action/item on the hotbar</br>
    /// Returns early if <seealso cref="ActionUpdater.NextAction"/> is not an <seealso cref="IAction"/> OR if the config 'TeachingMode' is off.
    /// </summary>
    private static void UpdateHighlight()
    {
        if (!Service.Config.TeachingMode || ActionUpdater.NextAction is not IAction nextAction) return;
        HotbarID? hotbar = null;

        if (nextAction is IBaseItem item)
        {
            hotbar = new(HotbarSlotType.Item, item.ID); // TODO: Test items, might need to be changed to 'AdjustedID'.  Update: Items don't seem to work for me :(
        }
        else if (nextAction is IBaseAction baseAction)
        {
            if (baseAction.Action.ActionCategory.Row is 10 or 11)//System Action.
            {
                var gAct = Svc.Data.GetExcelSheet<GeneralAction>()?.FirstOrDefault(g => g.Action.Row == baseAction.ID);

                if (gAct != null)
                {
                    hotbar = new(HotbarSlotType.GeneralAction, gAct.RowId);
                }
            }
            else
            {
                hotbar = new HotbarID(HotbarSlotType.Action, baseAction.AdjustedID);
            }
        }
        if (hotbar.HasValue)
        {
            //HotbarHighlightDrawerManager.HotbarIDs.Add(hotbar.Value);
            HotbarHighlightManager.HotbarIDs.Add(hotbar.Value);
        }
    }

    private static void ShowWarning()
    {
        if ((int)Svc.ClientState.ClientLanguage == 4)
        {
            var warning = "Rotation Solver 未进行国服适配并不提供相关支持! 建议使用国服的插件，如：";
            Svc.Toasts.ShowError(warning + "AE Assist 2.0！");

            var seString = new SeString(new TextPayload(warning),
                Svc.PluginInterface.AddChatLinkHandler(2, (id, str) =>
                {
                    if (id == 2)
                    {
                        Util.OpenLink("https://discord.gg/w2DPwRRZuT");
                    }
                }),
                new UIForegroundPayload(31),
                new TextPayload("AE Assist 2.0"),
                UIForegroundPayload.UIForegroundOff,
                RawPayload.LinkTerminator,
                new TextPayload("！"));

            Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
            {
                Message = seString,
                Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
            });
        }

        if (!Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "Avarice"))
        {
            UiString.AvariceWarning.Local().ShowWarning(0);
        }
        if (!Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "TextToTalk"))
        {
            UiString.TextToTalkWarning.Local().ShowWarning(0);
        }
    }

    public static void Enable()
    {
        ActionSequencerUpdater.Enable(Svc.PluginInterface.ConfigDirectory.FullName + "\\Conditions");
        SocialUpdater.Enable();
        RaidTimeUpdater.EnableAsync();

        Svc.Framework.Update += FrameworkUpdate;
    }

    static Exception? _innerException;
    private static void UpdateWork()
    {
        var waitingTime = (DateTime.Now - _lastUpdatedWork).TotalMilliseconds;
        if (waitingTime > 100)
        {
            Svc.Log.Warning($"The time for completing a running cycle for RS is {waitingTime:F2} ms, try disabling the option \"{"UseWorkTask"}\" to get better performance or check your other running plugins for one of them using too many resources and try disabling that.");
        }

        if (!IsValid)
        {
            ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
            return;
        }

        try
        {
            StateUpdater.UpdateState();

            if (Service.Config.AutoReloadRotations)
            {
                RotationUpdater.LocalRotationWatcher();
            }

            RotationUpdater.UpdateRotation();
            if (DataCenter.IsActivated())
            {
                TargetUpdater.UpdateTarget();
                RaidTimeUpdater.UpdateTimeline();
                ActionSequencerUpdater.UpdateActionSequencerAction();
                ActionUpdater.UpdateNextAction();
            }

            RSCommands.UpdateRotationState();
            //HotbarHighlightDrawerManager.UpdateSettings();
            HotbarHighlightManager.UpdateSettings();
        }
        catch (Exception ex)
        {
            if (_innerException != ex)
            {
                _innerException = ex;
                Svc.Log.Error(ex, "Inner Worker Exception");
            }
        }

        _work = false;
    }

    static DateTime _closeWindowTime = DateTime.Now;
    private unsafe static void CloseWindow()
    {
        if (_closeWindowTime < DateTime.Now) return;

        var needGreedWindow = Svc.GameGui.GetAddonByName("NeedGreed", 1);
        if (needGreedWindow == IntPtr.Zero) return;

        var notification = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_Notification", 1);
        if (notification == null) return;

        var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
        atkValues[0].Type = atkValues[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
        atkValues[0].Int = 0;
        atkValues[1].Int = 2;
        try
        {
            notification->FireCallback(2, atkValues);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Failed to close the window!");
        }
        finally
        {
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    static DateTime _nextOpenTime = DateTime.Now;
    static ulong _lastChest = 0;
    private unsafe static void OpenChest()
    {
        if (!Service.Config.AutoOpenChest) return;
        var player = Player.Object;

        var treasure = Svc.Objects.FirstOrDefault(o =>
        {
            if (o == null) return false;
            var dis = Vector3.Distance(player.Position, o.Position) - player.HitboxRadius - o.HitboxRadius;
            if (dis > 0.5f) return false;

            var address = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)o.Address;
            if ((ObjectKind)address->ObjectKind != ObjectKind.Treasure) return false;

            //Opened!
            foreach (var item in Loot.Instance()->Items)
            {
                if (item.ChestObjectId == o.GameObjectId) return false;
            }

            return true;
        });

        if (treasure == null) return;
        if (DateTime.Now < _nextOpenTime) return;
        if (treasure.GameObjectId == _lastChest && DateTime.Now - _nextOpenTime < TimeSpan.FromSeconds(10)) return;

        _nextOpenTime = DateTime.Now.AddSeconds(new Random().NextDouble() + 0.2);
        _lastChest = treasure.GameObjectId;

        try
        {
            Svc.Targets.Target = treasure;

            TargetSystem.Instance()->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)treasure.Address);

            Notify.Plain($"Try to open the chest {treasure.Name}");
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to open the chest!");
        }

        if (!Service.Config.AutoCloseChestWindow) return;
        _closeWindowTime = DateTime.Now.AddSeconds(0.5);
    }

    public static void Dispose()
    {
        Svc.Framework.Update -= FrameworkUpdate;
        PreviewUpdater.Dispose();
        ActionSequencerUpdater.SaveFiles();
        SocialUpdater.Disable();
        ActionUpdater.ClearNextAction();
        RaidTimeUpdater.Disable();
    }
}
