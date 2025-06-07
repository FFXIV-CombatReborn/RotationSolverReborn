using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace RotationSolver.UI;

internal static class ActionContextMenu
{
    private static IContextMenu? contextMenu;
    private static IGameInteropProvider? interop;

    public static void Init()
    {
        contextMenu = Svc.ContextMenu;
        interop = Svc.Hook;
        contextMenu.OnMenuOpened += AddActionMenu;
    }

    //TODO: Cleanup when the enable/disable is available
    //The primary issue is that HoveredActionChanged is not triggered when you are no longer hovering a valid action, unlike HoverItemChanged.
    //This is a Dalamud issue that I will need to fix and PR to them.
    private static void AddActionMenu(IMenuOpenedArgs args)
    {
        Svc.Log.Debug($"{args}");
        var contextAction = new BaseAction((ActionID)Svc.GameGui.HoveredAction.ActionID);
        Svc.GameGui.HoveredActionChanged += (sender, e) => { contextAction = new BaseAction((ActionID)Svc.GameGui.HoveredAction.ActionID); };
        Svc.GameGui.HoveredItemChanged += (sender, e) => { Svc.GameGui.HoveredAction.ActionID = 0; };
        
        if (contextAction == null || Svc.GameGui.HoveredAction.ActionID == 0)
        {
            return;
        }
        
        Svc.Log.Debug(
            $"Menu attempted spawned from {contextAction.Name}/{Svc.GameGui.HoveredAction.ActionID},{Svc.GameGui.HoveredItem}, {args.AddonName}, {args.MenuType}, {args.Target}");
        
        if (args.AddonName == null || !args.AddonName.Contains("Action") || contextAction == null)
        {
            return;
        }
         
        #region Enable/Disable Action
        if (contextAction.IsEnabled)
        {
            var enabledEntry = new MenuItem
            {
                Name = $"Disable {contextAction.Name}",
                PrefixChar = 'R',
                PrefixColor = 545
            };
            
            enabledEntry.OnClicked += clickedEntry => { contextAction.IsEnabled = false; }; 
            args.AddMenuItem(enabledEntry);
        }
        else
        {
            var enabledEntry = new MenuItem
            {
                Name = $"Enable {contextAction.Name}",
                PrefixChar = 'R',
                PrefixColor = 545
            };

            enabledEntry.OnClicked += clickedEntry => { contextAction.IsEnabled = true; };
            args.AddMenuItem(enabledEntry);
        }
        #endregion
        
        var subMenuEntry = new MenuItem
        {
            Name = "Extra Functions",
            IsSubmenu = true,
            PrefixChar = 'R',
            PrefixColor = 545
        };

        subMenuEntry.OnClicked += args => BuildSubMenu(args, contextAction);
        
        //TODO: Add more functions here 
        // args.AddMenuItem(subMenuEntry);
    }

    private static void BuildSubMenu(IMenuItemClickedArgs args, BaseAction contextAction)
    {
        var entries = new List<MenuItem>();

        if (entries.Count > 0)
        {
            args.OpenSubmenu(entries);
        }
    }
}