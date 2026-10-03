using Dalamud.Game.Gui;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;

namespace RotationSolver.UI;

internal static class ActionContextMenu
{
	private static IContextMenu? contextMenu;
	private static BaseAction? currentContextAction;
	private static uint currentHoveredActionId;
	private static bool _initialized;

	public static void Init()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;

		contextMenu = Svc.ContextMenu;
		contextMenu?.OnMenuOpened += AddActionMenu;

		// Subscribe to hover events once
		Svc.GameGui.HoveredActionChanged += OnHoveredActionChanged;
		Svc.GameGui.HoveredItemChanged += OnHoveredItemChanged;
	}

	public static void Dispose()
	{
		if (!_initialized)
		{
			return;
		}

		_initialized = false;

		contextMenu?.OnMenuOpened -= AddActionMenu;

		// Unsubscribe from events
		Svc.GameGui.HoveredActionChanged -= OnHoveredActionChanged;
		Svc.GameGui.HoveredItemChanged -= OnHoveredItemChanged;

		currentContextAction = null;
		contextMenu = null;
	}

	private static void OnHoveredActionChanged(object? sender, HoveredAction hoveredAction)
	{
		currentHoveredActionId = hoveredAction.ActionId;
		if (!Service.Config.ShowContext)
		{
			currentContextAction = null;
			return;
		}

		//Svc.Log.Verbose($"HoveredAction changed: {hoveredAction.DetailKind}");

		if (!Player.Available)
		{
			currentContextAction = null;
			return;
		}
		if (hoveredAction.DetailKind != DetailKind.Action)
		{
			currentContextAction = null;
			return;
		}
		if (hoveredAction.ActionId != 0)
		{
			try
			{
				currentContextAction = new BaseAction((ActionID)hoveredAction.ActionId);
			}
			catch
			{
				currentContextAction = null;
			}
		}
		else
		{
			currentContextAction = null;
		}
	}

	private static void OnHoveredItemChanged(object? sender, ulong itemId)
	{
		currentHoveredActionId = 0;
		currentContextAction = null;
	}

	//TODO: Cleanup when the enable/disable is available
	//The primary issue is that HoveredActionChanged is not triggered when you are no longer hovering a valid action, unlike HoverItemChanged.
	//This is a Dalamud issue that I will need to fix and PR to them.
	private static void AddActionMenu(IMenuOpenedArgs args)
	{
		if (!Service.Config.ShowContext)
		{
			return;
		}

		if (DataCenter.Role == JobRole.DiscipleOfTheLand || DataCenter.Role == JobRole.DiscipleOfTheHand)
		{
			return;
		}

		// Use cached action instead of creating new ones
		var contextAction = currentContextAction;

		if (contextAction == null || currentHoveredActionId == 0)
		{
			return;
		}

		if (!contextAction.Info.IsAbility && !contextAction.Info.IsRealGCD && !contextAction.Info.IsGeneralGCD && !contextAction.Info.IsDutyAction)
		{
			return;
		}

		Svc.Log.Debug(
			$"Menu attempted spawned from {contextAction.Name}/{currentHoveredActionId},{Svc.GameGui.HoveredItem}, {args.AddonName}, {args.MenuType}, {args.Target}, ");

		if (string.IsNullOrEmpty(args.AddonName) || !args.AddonName.Contains("Action"))
		{
			return;
		}

		var enabled = contextAction.IsEnabled;
		var toggleEntry = new MenuItem
		{
			Name = $"{(enabled ? "Disable" : "Enable")} {contextAction.Name}",
			PrefixChar = 'R',
			PrefixColor = 545
		};

		toggleEntry.OnClicked += _ => contextAction.IsEnabled = !enabled;
		args.AddMenuItem(toggleEntry);
	}
}