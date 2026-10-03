using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.ActionTimeline;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.IPC;
//using KamiToolKit;
using RotationSolver.UI;
using RotationSolver.UI.ExtraWindows;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.UI.Material;
using RotationSolver.Updaters;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver;

public sealed class RotationSolverPlugin : IAsyncDalamudPlugin
{
	private readonly WindowSystem windowSystem;

	private static MainWindow? _mainWindow;
	private static ControlWindow? _controlWindow;
	private static NextActionWindow? _nextActionWindow;
	private static InterceptedActionWindow? _interceptedActionWindow;
	private static ActionTimelineWindow? _actionTimelineWindow;
	private static OverlayWindow? _overlayWindow;
	//private static NativeControlWindow? _nativeControlWindow;
	private static StateControlWindow? _stateControlWindow;
	private static EasterEggWindow? _easterEggWindow;
	private static FirstStartTutorialWindow? _firstStartTutorialWindow;
	private static UpdateNotesWindow? _updateNotesWindow;

	private static readonly List<IDisposable> _dis = [];
	public static string Name => "Rotation Solver Reborn";
	internal static readonly List<DrawingHighlightHotbarBase> _drawingElements = [];

	public static DalamudLinkPayload OpenLinkPayload { get; private set; } = null!;
	public static DalamudLinkPayload? HideWarningLinkPayload { get; private set; }
	private static readonly Random _random = new();

	/// <summary>
	/// The registered IPC provider. Only one instance may exist, since creating one registers the IPC endpoints.
	/// </summary>
	internal static IPCProvider IPCProvider { get; private set; } = null!;

	public RotationSolverPlugin(IDalamudPluginInterface pluginInterface)
	{
		ECommonsMain.Init(pluginInterface, this, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
		//KamiToolKitLibrary.Initialize(pluginInterface);
		IconSet.Init();

		_dis.Add(new Service());

		ActionTracer.Init();

		IPCProvider = new();

		_mainWindow = new();
		_controlWindow = new();
		_nextActionWindow = new();
		_interceptedActionWindow = new();
		_actionTimelineWindow = new();
		_overlayWindow = new();
		//_nativeControlWindow = new();
		_stateControlWindow = new();
		_easterEggWindow = new();
		_firstStartTutorialWindow = new();
		_updateNotesWindow = new();

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
		windowSystem.AddWindow(_mainWindow);
		windowSystem.AddWindow(_controlWindow);
		windowSystem.AddWindow(_nextActionWindow);
		windowSystem.AddWindow(_interceptedActionWindow);
		windowSystem.AddWindow(_actionTimelineWindow);
		windowSystem.AddWindow(_overlayWindow);
		windowSystem.AddWindow(_stateControlWindow);
		windowSystem.AddWindow(_easterEggWindow);
		windowSystem.AddWindow(_firstStartTutorialWindow);
		windowSystem.AddWindow(_updateNotesWindow);

		//Notify.Success("Overlay Window was added!");

		Svc.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.Draw += OnDraw;
	}

	public async Task LoadAsync(CancellationToken cancellationToken)
	{
		// Warm up texture cache on framework thread. This is not critical to plugin
		// functionality, so it should not be allowed to block/consume the load-timeout
		// budget that Dalamud provides for LoadAsync via cancellationToken.
		var textureWarmupTask = Svc.Framework.Run(() =>
		{
			_ = ThreadLoadImageHandler.TryGetIconTextureWrap(0, true, out _);
		}, cancellationToken);

		// Load main config asynchronously (off main thread)
		try
		{
			if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
			{
				var json = await File.ReadAllTextAsync(Svc.PluginInterface.ConfigFile.FullName, cancellationToken);
				var oldConfigs = JsonConvert.DeserializeObject<Configs>(json) ?? new Configs();

				var newConfigs = Configs.Migrate(oldConfigs);
				if (newConfigs.Version != Configs.CurrentVersion)
				{
					newConfigs = new Configs();
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

		// Load OtherConfiguration files
		await OtherConfiguration.InitAsync(cancellationToken);

		try
		{
			await textureWarmupTask;
		}
		catch (OperationCanceledException)
		{
			PluginLog.Warning("Texture warmup was canceled; continuing plugin load.");
		}

		// The following registers hooks/events required for the plugin to function and
		// must complete even if Dalamud's load-timeout token fires (e.g. due to slow
		// disk I/O or the framework being busy during a loading screen), otherwise the
		// plugin ends up in a partially-initialized state with hooks leaking.
		await Svc.Framework.Run(() =>
		{
			//HotbarHighlightDrawerManager.Init();

			MajorUpdater.Enable();
			AutoAttackUpdater.Enable();
			Watcher.Enable();
			ActionQueueManager.Enable();
			BMRPlanUpdater.Enable();
			ActionContextMenu.Init();
			HotbarHighlightManager.Init();

			Svc.DutyState.DutyStarted += DutyState_DutyStarted;
			Svc.DutyState.DutyWiped += DutyState_DutyWiped;
			Svc.DutyState.DutyCompleted += DutyState_DutyCompleted;
			Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
			ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);

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
		}, CancellationToken.None);
	}

	private static void DutyState_DutyCompleted(IDutyStateEventArgs e)
	{
		var delay = TimeSpan.FromSeconds(_random.Next(4, 6));
		_ = Svc.Framework.RunOnTick(() =>
		{
			_ = Service.Config.DutyEnd.AddMacro();

			if (Service.Config.AutoOffWhenDutyCompleted)
			{
				RSCommands.CancelState();
			}
		}, delay);
	}

	private static void ClientState_TerritoryChanged(uint id)
	{
		DataCenter.ResetAllRecords();

		if (id == 0)
		{
			PluginLog.Information("Invalid territory id: 0");
			return;
		}

		var territory = Service.GetSheet<TerritoryType>().GetRow(id);
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

	private static void DutyState_DutyStarted(IDutyStateEventArgs e)
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
			var warning = string.Format(UiString.HighEndWarning.GetDescription(), DataCenter.Territory.ContentFinderName);
			BasicWarningHelper.AddSystemWarning(warning);
		}
	}

	private static void DutyState_DutyWiped(IDutyStateEventArgs e)
	{
		if (!Player.Available)
		{
			return;
		}

		DataCenter.ResetAllRecords();
	}

	private void OnDraw()
	{
		if (Svc.GameGui.GameUiHidden)
		{
			return;
		}

		M3.BeginFrame();
		windowSystem.Draw();
	}

	internal static void ChangeUITranslation()
	{
		_mainWindow!.WindowName = UiString.ConfigWindowHeader.GetDescription()
			+ (typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?") + "###rsrConfigWindow";

		RSCommands.Disable();
		RSCommands.Enable();
	}

	private void OnOpenConfigUi()
	{
		OpenConfigWindow();
	}

	internal static void OpenConfigWindow()
	{
		if (_mainWindow is { IsOpen: true, IsMinimized: true })
		{
			_mainWindow.Restore();
			return;
		}

		_mainWindow?.Toggle();
	}

	internal static void ToggleStateControlWindow()
	{
		_stateControlWindow?.Toggle();
	}

	internal static void OpenStateControlWindow()
	{
		_stateControlWindow?.IsOpen = true;
	}

	internal static void OpenTicTacToe()
	{
		_easterEggWindow?.IsOpen = true;
	}

	internal static void ShowConfigWindow(MainWindowTab? tab = null)
	{
		if (_mainWindow == null)
		{
			return;
		}

		_mainWindow.IsOpen = true;
		_mainWindow.Restore();
		if (tab.HasValue)
		{
			_mainWindow.SetActiveTab(tab.Value);
		}
	}

	internal static void OpenFirstStartTutorial()
	{
		if (_firstStartTutorialWindow?.IsOpen == true)
		{
			return;
		}

		_firstStartTutorialWindow?.Toggle();
	}

	internal static void ShowFirstStartTutorialIfNeeded()
	{
		_firstStartTutorialWindow?.OpenIfFirstStart();
	}

	internal static void OpenChangelog()
	{
		_updateNotesWindow?.IsOpen = true;
	}

	internal static void ShowChangelogIfUpdated()
	{
		_updateNotesWindow?.OpenIfUpdated();
	}

	internal static void UpdateDisplayWindow()
	{
		var isValid = MajorUpdater.IsValid && DataCenter.CurrentRotation != null;

		isValid &= !Service.Config.OnlyShowWithHostileOrInDuty
				|| Svc.Condition[ConditionFlag.BoundByDuty]
				|| AnyHostileTargetWithinDistance(25);

		_controlWindow!.IsOpen = isValid && Service.Config.ShowControlWindow;
		//if (isValid && Service.Config.ShowControlWindow)
		//{
		//	if (!(_nativeControlWindow?.IsOpen ?? false))
		//		_nativeControlWindow?.Open();
		//}
		//else
		//{
		//	_nativeControlWindow?.Close();
		//}
		_nextActionWindow!.IsOpen = isValid && Service.Config.ShowNextActionWindow;
		_interceptedActionWindow!.IsOpen = isValid && Service.Config.ShowInterceptedActionWindow;
		UpdateActionTimeline(isValid);
		_overlayWindow!.IsOpen = isValid && Service.Config.TeachingMode;
	}

	private static void UpdateActionTimeline(bool isValid)
	{
		var config = Service.Config;
		if (!config.ShowActionTimelineWindow)
		{
			_actionTimelineWindow!.IsOpen = false;
			ActionTimelineManager.DisposeInstance();
			return;
		}

		ActionTimelineManager.Instance.Update();

		_actionTimelineWindow!.IsOpen = isValid
			&& (!config.ActionTimelineOnlyWhenActive || DataCenter.IsActivated())
			&& (!config.ActionTimelineOnlyInCombat || DataCenter.InCombat);
	}

	private static bool AnyHostileTargetWithinDistance(float distance)
	{
		foreach (var target in DataCenter.AllHostileTargets)
		{
			if (target.DistanceToPlayer() < distance)
			{
				return true;
			}
		}
		return false;
	}

	public async ValueTask DisposeAsync()
	{
		ActionTracer.Shutdown();

		Service.Config.Save();
		await OtherConfiguration.Save();

		AutoAttackUpdater.Disable();
		RSCommands.Disable();
		Watcher.Disable();
		ActionQueueManager.Disable();
		BMRPlanUpdater.Disable();
		ActionContextMenu.Dispose();
		Svc.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.OpenMainUi -= OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.Draw -= OnDraw;

		Svc.DutyState.DutyStarted -= DutyState_DutyStarted;
		Svc.DutyState.DutyWiped -= DutyState_DutyWiped;
		Svc.DutyState.DutyCompleted -= DutyState_DutyCompleted;
		Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;

		Svc.Chat.RemoveChatLinkHandler();
		OpenLinkPayload = null!;
		HideWarningLinkPayload = null;

		foreach (var item in _dis)
		{
			item.Dispose();
		}
		_dis.Clear();

		//_nativeControlWindow?.Close();
		//KamiToolKitLibrary.Dispose();
		MajorUpdater.Dispose();
		HotbarDisabledColor.ResetOnUnload();
		HotbarHighlightManager.Dispose();
		ActionTimelineManager.DisposeInstance();
		FontManager.DisposeAll();

		BMRInfo_IPCSubscriber.Dispose();
		BMRTimeline_IPCSubscriber.Dispose();
		BMRPlan_IPCSubscriber.Dispose();
		Wrath_IPCSubscriber.Dispose();

		ECommonsMain.Dispose();
	}
}