using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using RotationSolver.Updaters;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace RotationSolver.UI.HighlightHotbar;

/// <summary>
/// Since paintermanager was used to interact with the submodule XIVDrawer this class shouldnt act is either PainterManager nor XIVDrawerMain,
/// rather it should act as a combination, using PainterManagers properties and fields and XIVDrawerMain's properties and fields required for actually using the 
/// highlight hotbar feature (WITHOUT the initilization code that was used because XIVDrawerMain used to be its own plugin)
/// </summary>
internal static class HotbarHighlightDrawerManager
{
    internal const string _name = "RSR - Overlay (Hotbat Highlight)";
    private static HotbarHighlightDrawing? _highLight;
    public static HashSet<HotbarID> HotbarIDs => _highLight?.HotbarIDs ?? [];
    internal static readonly List<HotbarHighlightDrawing> _drawingElements = [];
    internal static IDrawing2D[] _drawingElements2D = [];


    #region Config
    public static bool Enable { get; set; } = false;
    public static bool UseTaskToAccelerate { get; set; } = false;
    public static Vector4 HighlightColor
    {
        get => _highLight?.Color ?? Vector4.One;
        set
        {
            if (_highLight == null) return;
            _highLight.Color = value;
        }
    }
    #endregion

    private static bool _initiated = false;
    public static void Init()
    {
        if (_initiated)
        {
#if DEBUG
            Svc.Log.Debug("HotbarHighlightDrawerManager Initialization skipped: already initiated.");
#endif
            return;
        }

        _initiated = true;
#if DEBUG
        Svc.Log.Debug("HotbarHighlightDrawerManager Initialization started.");
#endif

        _highLight = new();
#if DEBUG
        Svc.Log.Debug("HotbarHighlightDrawerManager instance created.");
#endif

        UpdateSettings();
#if DEBUG
        Svc.Log.Debug("HotbarHighlightDrawerManager Initialization completed.");
#endif
    }

    public static void Dispose()
    {
        if (!_initiated) return;
        _initiated = false;

        foreach (var item in new List<HotbarHighlightDrawing>(_drawingElements))
        {
            item.Dispose();
#if DEBUG
            Svc.Log.Debug($"Item: {item} from '_drawingElements' was disposed");
#endif
        }
    }

    public static void UpdateSettings()
    {
        UseTaskToAccelerate = Service.Config.UseTasksForOverlay;
        Enable = !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] && Service.Config.TeachingMode && MajorUpdater.IsValid;
        if (_highLight != null)
        {
            _highLight.Color = Service.Config.TeachingModeColor;
        }
        else
        {
            Svc.Log.Warning("_highLight is null!");
        }
    }

    internal static async Task<IDrawing2D[]> To2DAsync()
    {
        List<Task<IEnumerable<IDrawing2D>>> drawing2Ds = [];

        if (_drawingElements != null)
        {
            drawing2Ds.AddRange(_drawingElements.Select(item => System.Threading.Tasks.Task.Run(() =>
            {
                return item.To2DMain();
            })));
        }

        await System.Threading.Tasks.Task.WhenAll([.. drawing2Ds]);
        return drawing2Ds.SelectMany(i => i.Result).ToArray();
    }
}


/// <summary>
/// Belonged in the XIVDrawer namespace same as XIVDrawerMain (not sure if needed yet, therefor we comment it out)
/// </summary>
//public abstract class BasicDrawing : IDisposable
//{
//    private bool _disposed;

//    /// <summary>
//    /// If it is enabled.
//    /// </summary>
//    public virtual bool Enable { get; set; } = true;

//    /// <summary>
//    /// The time that it will disappear.
//    /// </summary>
//    public DateTime DeadTime { get; set; } = DateTime.MinValue;

//    private protected BasicDrawing()
//    {
//        Service.Framework.Update += Framework_Update;
//    }

//    private void Framework_Update(IFramework framework)
//    {
//        if (DeadTime != DateTime.MinValue && DeadTime < DateTime.Now)
//        {
//            Dispose();
//            return;
//        }

//        AdditionalUpdate();
//    }

//    private protected virtual void AdditionalUpdate()
//    {

//    }

//    /// <inheritdoc/>
//    public void Dispose()
//    {
//        if (_disposed) return;
//        _disposed = true;

//        Service.Framework.Update -= Framework_Update;
//        AdditionalDispose();
//        GC.SuppressFinalize(this);
//    }

//    private protected virtual void AdditionalDispose()
//    {

//    }
//}
