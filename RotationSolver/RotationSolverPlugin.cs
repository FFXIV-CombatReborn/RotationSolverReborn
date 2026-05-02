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
using RotationSolver.Helpers;
using RotationSolver.IPC;
//using KamiToolKit;
using RotationSolver.UI;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver;

public sealed class RotationSolverPlugin : IAsyncDalamudPlugin
{
	private readonly WindowSystem windowSystem;

	private static RotationConfigWindow? _rotationConfigWindow;
	private static ControlWindow? _controlWindow;
	private static NextActionWindow? _nextActionWindow;
	private static InterceptedActionWindow? _interceptedActionWindow;
	private static CooldownWindow? _cooldownWindow;
	private static ActionTimelineWindow? _actionTimelineWindow;
	private static OverlayWindow? _overlayWindow;
	//private static NativeControlWindow? _nativeControlWindow;
	private static EasterEggWindow? _easterEggWindow;
	private static FirstStartTutorialWindow? _firstStartTutorialWindow;

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
		//KamiToolKitLibrary.Initialize(pluginInterface);
		IconSet.Init();

		_dis.Add(new Service());

		IPCProvider = new();

		_rotationConfigWindow = new();
		_controlWindow = new();
		_nextActionWindow = new();
		_interceptedActionWindow = new();
		_cooldownWindow = new();
		_actionTimelineWindow = new();
		_overlayWindow = new();
		//_nativeControlWindow = new();
		_easterEggWindow = new();
		_firstStartTutorialWindow = new();

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
		windowSystem.AddWindow(_interceptedActionWindow);
		windowSystem.AddWindow(_cooldownWindow);
		windowSystem.AddWindow(_actionTimelineWindow);
		windowSystem.AddWindow(_overlayWindow);
		windowSystem.AddWindow(_easterEggWindow);
		windowSystem.AddWindow(_firstStartTutorialWindow);

		//Notify.Success("Overlay Window was added!");

		Svc.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.Draw += OnDraw;
	}

	public async Task LoadAsync(CancellationToken cancellationToken)
	{
		// Warm up texture cache on framework thread
		await Svc.Framework.Run(() =>
		{
			_ = ThreadLoadImageHandler.TryGetIconTextureWrap(0, true, out _);
		});

		// Load main config asynchronously (off main thread)
		try
		{
			if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
			{
				string json = await File.ReadAllTextAsync(Svc.PluginInterface.ConfigFile.FullName, cancellationToken);
				Configs oldConfigs = JsonConvert.DeserializeObject<Configs>(json) ?? new Configs();

				Configs newConfigs = Configs.Migrate(oldConfigs);
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

		// Load OtherConfiguration files and download incompatible plugin list concurrently
		await Task.WhenAll(
			OtherConfiguration.InitAsync(cancellationToken),
			DownloadHelper.DownloadAsync(cancellationToken)
		);

		// The following must run on the main/framework thread
		await Svc.Framework.Run(() =>
		{
			//HotbarHighlightDrawerManager.Init();

			MajorUpdater.Enable();
			Watcher.Enable();
			ActionQueueManager.Enable();
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
		});
	}

	private static void DutyState_DutyCompleted(IDutyStateEventArgs e)
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

	private static void ClientState_TerritoryChanged(uint id)
	{
		DataCenter.ResetAllRecords();

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
			string warning = string.Format(UiString.HighEndWarning.GetDescription(), DataCenter.Territory.ContentFinderName);
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

	internal static void ShowConfigWindow(RotationConfigWindowTab? tab = null)
	{
		if (_rotationConfigWindow == null)
		{
			return;
		}

		_rotationConfigWindow.IsOpen = true;
		if (tab.HasValue)
		{
			_rotationConfigWindow.SetActiveTab(tab.Value);
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

	internal static void UpdateDisplayWindow()
	{
		bool isValid = MajorUpdater.IsValid && DataCenter.CurrentRotation != null;

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
		_cooldownWindow!.IsOpen = isValid && Service.Config.ShowCooldownWindow;
		_nextActionWindow!.IsOpen = isValid && Service.Config.ShowNextActionWindow;
		_interceptedActionWindow!.IsOpen = isValid && Service.Config.ShowInterceptedActionWindow;

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

	public async ValueTask DisposeAsync()
	{
		Service.Config.Save();
		await OtherConfiguration.Save();

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

		//_nativeControlWindow?.Close();
		//KamiToolKitLibrary.Dispose();
		MajorUpdater.Dispose();
		MiscUpdater.Dispose();
		HotbarHighlightManager.Dispose();
		ActionTimelineManager.Instance.Dispose();

		ECommonsMain.Dispose();
	}
}