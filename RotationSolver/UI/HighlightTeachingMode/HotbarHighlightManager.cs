using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;

namespace RotationSolver.UI.HighlightTeachingMode;

internal static class HotbarHighlightManager
{
    public static bool Enable { get; set; } = false;
    public static bool UseTaskToAccelerate { get; set; } = false;
    private static DrawingHighlightHotbar? _highLight;
    public static HashSet<HotbarID> HotbarIDs => _highLight?.HotbarIDs ?? [];
    internal static IDrawing2D[] _drawingElements2D = [];

    public static Vector4 HighlightColor
    {
        get => _highLight?.Color ?? Vector4.One;
        set
        {
            if (_highLight == null) return;
            _highLight.Color = value;
        }
    }

    public static void Init()
    {
        _highLight = new DrawingHighlightHotbar(Service.Config.TeachingModeColor);
        UpdateSettings();
    }

    public static void UpdateSettings()
    {
        //UseTaskToAccelerate = Service.Config.UseTasksForOverlay;
        Enable = !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] && Service.Config.TeachingMode && MajorUpdater.IsValid;
        HighlightColor = Service.Config.TeachingModeColor;
    }

    public static void Dispose()
    {
        foreach (var item in new List<DrawingHighlightHotbarBase>(RotationSolverPlugin._drawingElements))
        {
            item.Dispose();
#if DEBUG
            Svc.Log.Debug($"Item: {item} from '_drawingElements' was disposed");
#endif
        }
        _highLight?.Dispose();
        _highLight = null;
    }

    internal static async Task<IDrawing2D[]> To2DAsync()
    {
        List<Task<IEnumerable<IDrawing2D>>> drawing2Ds = [];

        if (RotationSolverPlugin._drawingElements != null)
        {
            drawing2Ds.AddRange(RotationSolverPlugin._drawingElements.Select(item => System.Threading.Tasks.Task.Run(() =>
            {
                return item.To2DMain();
            })));
        }

        await System.Threading.Tasks.Task.WhenAll([.. drawing2Ds]);
        return drawing2Ds.SelectMany(i => i.Result).ToArray();
    }
}
