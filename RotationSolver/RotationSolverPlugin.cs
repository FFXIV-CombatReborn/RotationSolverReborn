using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.IPC;
using RotationSolver.UI;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using RotationSolver.ActionTimeline;
using WelcomeWindow = RotationSolver.UI.WelcomeWindow;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver;

public sealed class RotationSolverPlugin : IDalamudPlugin, IDisposable
{
    private readonly WindowSystem windowSystem;

    private static RotationConfigWindow? _rotationConfigWindow;
    private static ControlWindow? _controlWindow;
    private static NextActionWindow? _nextActionWindow;
    private static CooldownWindow? _cooldownWindow;
    private static ActionTimelineWindow? _actionTimelineWindow;
    private static WelcomeWindow? _changelogWindow;
    private static OverlayWindow? _overlayWindow;
    private static EasterEggWindow? _easterEggWindow;

    private static readonly List<IDisposable> _dis = [];
    public static string Name => "Rotation Solver Reborn";
    internal static readonly List<DrawingHighlightHotbarBase> _drawingElements = [];

    public static DalamudLinkPayload OpenLinkPayload { get; private set; } = null!;
    public static DalamudLinkPayload? HideWarningLinkPayload { get; private set; }
    private static readonly Random _random = new();

    internal IPCProvider IPCProvider;
    public RotationSolverPlugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
        _ = Svc.Framework.RunOnTick(() =>
        {
            _ = ThreadLoadImageHandler.TryGetIconTextureWrap(0, true, out _);
        });
        IconSet.Init();

        _dis.Add(new Service());
        try
        {
            // Check if the config file exists before attempting to read and deserialize it
            if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
            {
                Configs oldConfigs = JsonConvert.DeserializeObject<Configs>(
                    File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName))
                    ?? new Configs();

                // Check version and migrate or reset if necessary
                Configs newConfigs = Configs.Migrate(oldConfigs);
                if (newConfigs.Version != Configs.CurrentVersion)
                {
                    newConfigs = new Configs(); // Reset to default if versions do not match
                }
                Service.Config = newConfigs;
            }
            else
            {
                Service.Config = new Configs();
            }
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to load config: {ex.Message}");
            Service.Config = new Configs();
        }

        IPCProvider = new();

        _rotationConfigWindow = new();
        _controlWindow = new();
        _nextActionWindow = new();
        _cooldownWindow = new();
        _actionTimelineWindow = new();
        _changelogWindow = new();
        _overlayWindow = new();
        _easterEggWindow = new();

        // Start cactbot bridge if enabled
        //try
        //{
        //    if (Service.Config.EnableCactbotTimeline)
        //    {
        //        var cactbotBridge = new Helpers.CactbotTimelineBridge();
        //        _dis.Add(cactbotBridge);
        //    }
        //}
        //catch (Exception ex)
        //{
        //    PluginLog.Warning($"Failed to start CactbotTimelineBridge: {ex.Message}");
        //}

        windowSystem = new WindowSystem(Name);
        windowSystem.AddWindow(_rotationConfigWindow);
        windowSystem.AddWindow(_controlWindow);
        windowSystem.AddWindow(_nextActionWindow);
        windowSystem.AddWindow(_cooldownWindow);
        windowSystem.AddWindow(_actionTimelineWindow);
        windowSystem.AddWindow(_changelogWindow);
        windowSystem.AddWindow(_overlayWindow);
        windowSystem.AddWindow(_easterEggWindow);
        //Notify.Success("Overlay Window was added!");

        Svc.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.Draw += OnDraw;

        //HotbarHighlightDrawerManager.Init();

        MajorUpdater.Enable();
        Watcher.Enable();
        ActionQueueManager.Enable();
        OtherConfiguration.Init();
        ActionContextMenu.Init();
        HotbarHighlightManager.Init();

        Svc.DutyState.DutyStarted += DutyState_DutyStarted;
        Svc.DutyState.DutyWiped += DutyState_DutyWiped;
        Svc.DutyState.DutyCompleted += DutyState_DutyCompleted;
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);

        static void DutyState_DutyCompleted(object? sender, ushort e)
        {
            TimeSpan delay = TimeSpan.FromSeconds(_random.Next(4, 6));
            _ = Svc.Framework.RunOnTick(() =>
            {
                _ = Service.Config.DutyEnd.AddMacro();

                if (Service.Config.AutoOffWhenDutyCompleted)
                {
                    RSCommands.CancelState();
                }
            }, delay);
        }

        static void ClientState_TerritoryChanged(ushort id)
        {
            DataCenter.ResetAllRecords();

            // Check if the id is valid before proceeding
            if (id == 0)
            {
                PluginLog.Information("Invalid territory id: 0");
                return;
            }

            TerritoryType territory = Service.GetSheet<TerritoryType>().GetRow(id);

            DataCenter.Territory = new TerritoryInfo(territory);

            try
            {
                DataCenter.CurrentRotation?.OnTerritoryChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Failed on Territory changed: {ex.Message}");
            }
        }

        static void DutyState_DutyStarted(object? sender, ushort e)
        {
            if (!Player.Available)
            {
                return;
            }

            if (!TargetFilter.PlayerJobCategory(JobRole.Tank) && !TargetFilter.PlayerJobCategory(JobRole.Healer))
            {
                return;
            }

            if (DataCenter.Territory?.IsHighEndDuty ?? false)
            {
                string warning = string.Format(UiString.HighEndWarning.GetDescription(), DataCenter.Territory.ContentFinderName);
                BasicWarningHelper.AddSystemWarning(warning);
            }
        }

        static void DutyState_DutyWiped(object? sender, ushort e)
        {
            if (!Player.Available)
            {
                return;
            }

            DataCenter.ResetAllRecords();
        }

        ChangeUITranslation();

        OpenLinkPayload = Svc.Chat.AddChatLinkHandler(0, (guid, seString) =>
        {
            if (guid == 0)
            {
                OpenConfigWindow();
            }
        });
        HideWarningLinkPayload = Svc.Chat.AddChatLinkHandler(1, (guid, seString) =>
        {
            if (guid == 0)
            {
                Service.Config.HideWarning.Value = true;
                Svc.Chat.Print("Warning has been hidden.");
            }
        });

        // Load rotations on startup
        _ = Task.Run(async () =>
        {
            await DownloadHelper.DownloadAsync();
        });
    }

    private void OnDraw()
    {
        if (Svc.GameGui.GameUiHidden)
        {
            return;
        }

        windowSystem.Draw();
    }

    internal static void ChangeUITranslation()
    {
        _rotationConfigWindow!.WindowName = UiString.ConfigWindowHeader.GetDescription()
            + (typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?") + "###rsrConfigWindow";

        RSCommands.Disable();
        RSCommands.Enable();
    }

    private void OnOpenConfigUi()
    {
        OpenConfigWindow();
    }

    internal static void OpenConfigWindow()
    {
        _rotationConfigWindow?.Toggle();
    }

    internal static void OpenTicTacToe()
    {
        _easterEggWindow?.IsOpen = true;
    }

    internal static void UpdateDisplayWindow()
    {
        bool isValid = MajorUpdater.IsValid && DataCenter.CurrentRotation != null;

        _nextActionWindow!.IsOpen = isValid && Service.Config.ShowNextActionWindow;

        isValid &= !Service.Config.OnlyShowWithHostileOrInDuty
                || Svc.Condition[ConditionFlag.BoundByDuty]
                || AnyHostileTargetWithinDistance(25);

        _controlWindow!.IsOpen = isValid && Service.Config.ShowControlWindow;
        _cooldownWindow!.IsOpen = isValid && Service.Config.ShowCooldownWindow;

        // ActionTimeline window with additional checks
        bool showActionTimeline = isValid && Service.Config.ShowActionTimelineWindow;

        if (Service.Config.ActionTimelineOnlyWhenActive)
        {
            showActionTimeline &= DataCenter.IsActivated();
        }

        if (Service.Config.ActionTimelineOnlyInCombat)
        {
            showActionTimeline &= DataCenter.InCombat;
        }

        _actionTimelineWindow!.IsOpen = showActionTimeline;

        if (showActionTimeline)
        {
            ActionTimelineManager.Instance.UpdateCombatState();
        }

        _overlayWindow!.IsOpen = isValid && Service.Config.TeachingMode;
    }

    private static bool AnyHostileTargetWithinDistance(float distance)
    {
        foreach (IBattleChara target in DataCenter.AllHostileTargets)
        {
            if (target.DistanceToPlayer() < distance)
            {
                return true;
            }
        }
        return false;
    }

    void IDisposable.Dispose()
    {
        Dispose().GetAwaiter().GetResult();
    }

    public async Task Dispose()
    {
        RSCommands.Disable();
        Watcher.Disable();
        ActionQueueManager.Disable();
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.Draw -= OnDraw;

        foreach (IDisposable item in _dis)
        {
            item.Dispose();
        }
        _dis.Clear();

        MajorUpdater.Dispose();
        MiscUpdater.Dispose();
        HotbarHighlightManager.Dispose();
        ActionTimelineManager.Instance.Dispose();
        await OtherConfiguration.Save();

        ECommonsMain.Dispose();

        Service.Config.Save();
    }
}