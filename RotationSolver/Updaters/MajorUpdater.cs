using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using RotationSolver.Commands;
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
        && Player.AvailableThreadSafe;

    private static Exception? _threadException;
    private static DateTime _lastUpdatedWork = DateTime.Now;
    public static void Enable()
    {
        ActionSequencerUpdater.Enable(Svc.PluginInterface.ConfigDirectory.FullName + "\\Conditions");
        Svc.Framework.Update += RSRUpdate;
    }

    // "Transition locked" means when !MajorUpdater.IsValid
    // TransitionSafeCommands are any function that are qualified safe for RSRUpdate to run, even when other updates should not happen.
    // TransitionSafeCommands *should* be duplicated elsewhere for commands that should also run when otherwise safe to update.
    // TransitionSafeCommands *should not* be used for commands that should *only* run when transition locked.
    public static void TransitionSafeCommands()
    {
        RSCommands.UpdateRotationState();
        ActionUpdater.UpdateLifetime();
    }

    private unsafe static void RSRUpdate(IFramework framework)
    {
        HotbarHighlightManager.HotbarIDs.Clear();
        RotationSolverPlugin.UpdateDisplayWindow();

        if (!IsValid)
        {
            TransitionSafeCommands();
            ActionUpdater.ClearNextAction();
            CustomRotation.MoveTarget = null;
            return;
        }

        HandleSystemWarnings();

        try
        {
            PreviewUpdater.UpdatePreview();
            UpdateHighlight();
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
                if (Service.Config.InDebug)
#pragma warning disable CS0436
                    WarningHelper.AddSystemWarning("Main Thread Exception");
            }
        }

        HandleWorkUpdateAsync().ConfigureAwait(false);
    }

    private static void HandleSystemWarnings()
    {
        if (DataCenter.SystemWarnings.Count > 0)
        {
            var warningsToRemove = new List<string>();

            foreach (var warning in DataCenter.SystemWarnings)
            {
                if ((warning.Value + TimeSpan.FromMinutes(10)) < DateTime.Now)
                {
                    warningsToRemove.Add(warning.Key);
                }
            }

            foreach (var warningKey in warningsToRemove)
            {
                DataCenter.SystemWarnings.Remove(warningKey);
            }
        }
    }

    private static async Task HandleWorkUpdateAsync()
    {
        var now = DateTime.Now;
        if (now - _lastUpdatedWork < TimeSpan.FromSeconds(Service.Config.MinUpdatingTime))
            return;

        _lastUpdatedWork = now;

        try
        {
            await Svc.Framework.RunOnTick(UpdateWork);
        }
        catch (Exception tEx)
        {
            Svc.Log.Error(tEx, "Worker Task Exception");
            if (Service.Config.InDebug)
#pragma warning disable CS0436
                WarningHelper.AddSystemWarning("Inner Worker Exception");
        }
    }

    private static void UpdateWork()
    {
        if (!IsValid)
        {
            ActionUpdater.NextAction = ActionUpdater.NextGCDAction = null;
            return;
        }

        try
        {
            if (Service.Config.AutoReloadRotations)
            {
                RotationUpdater.LocalRotationWatcher();
            }

            RotationUpdater.UpdateRotation();

            if (DataCenter.IsActivated())
            {
                TargetUpdater.UpdateTargets();
                StateUpdater.UpdateState();
                ActionSequencerUpdater.UpdateActionSequencerAction();
                ActionUpdater.UpdateNextAction();
            }

            RSCommands.UpdateRotationState();
            HotbarHighlightManager.UpdateSettings();

            RemoveExpiredVfxData();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Inner Worker Exception");
            if (Service.Config.InDebug)
#pragma warning disable CS0436
                WarningHelper.AddSystemWarning("Inner Worker Exception");
        }
    }

    private static void RemoveExpiredVfxData()
    {
        var expiredVfx = new List<VfxNewData>();
        lock (DataCenter.VfxDataQueue)
        {
            for (int i = 0; i < DataCenter.VfxDataQueue.Count; i++)
            {
                var vfx = DataCenter.VfxDataQueue[i];
                if (vfx.TimeDuration > TimeSpan.FromSeconds(6))
                {
                    expiredVfx.Add(vfx);
                }
            }

            foreach (var vfx in expiredVfx)
            {
                DataCenter.VfxDataQueue.Remove(vfx);
            }
        }
    }

    private static void UpdateHighlight()
    {
        if (!Service.Config.TeachingMode || ActionUpdater.NextAction is not IAction nextAction) return;

        HotbarID? hotbar = nextAction switch
        {
            IBaseItem item => new HotbarID(HotbarSlotType.Item, item.ID),
            IBaseAction baseAction when baseAction.Action.ActionCategory.RowId is 10 or 11 => GetGeneralActionHotbarID(baseAction),
            IBaseAction baseAction => new HotbarID(HotbarSlotType.Action, baseAction.AdjustedID),
            _ => null
        };

        if (hotbar.HasValue)
        {
            HotbarHighlightManager.HotbarIDs.Add(hotbar.Value);
        }
    }

    private static HotbarID? GetGeneralActionHotbarID(IBaseAction baseAction)
    {
        var generalActions = Svc.Data.GetExcelSheet<GeneralAction>();
        if (generalActions == null) return null;

        foreach (var gAct in generalActions)
        {
            if (gAct.Action.RowId == baseAction.ID)
            {
                return new HotbarID(HotbarSlotType.GeneralAction, gAct.RowId);
            }
        }

        return null;
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
        if (DataCenter.InCombat) return;
        var player = Player.Object;

        var treasure = Svc.Objects.FirstOrDefault(o =>
        {
            if (o == null) return false;
            var dis = Vector3.Distance(player.Position, o.Position) - player.HitboxRadius - o.HitboxRadius;
            if (dis > 0.5f) return false;

            var address = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)o.Address;
            if (address->ObjectKind != FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Treasure) return false;

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
        Svc.Framework.Update -= RSRUpdate;
        PreviewUpdater.Dispose();
        ActionSequencerUpdater.SaveFiles();
        ActionUpdater.ClearNextAction();
    }
}

