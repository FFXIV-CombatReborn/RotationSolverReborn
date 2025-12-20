using ECommons.Logging;
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
            if (_highLight == null)
            {
                return;
            }

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
        Enable = (Service.Config.TeachingMode || Service.Config.ReddenDisabledHotbarActions) && DataCenter.IsActivated() && MajorUpdater.IsValid;
        HighlightColor = Service.Config.TeachingModeColor;
    }

    public static void Dispose()
    {
        foreach (DrawingHighlightHotbarBase item in new List<DrawingHighlightHotbarBase>(RotationSolverPlugin._drawingElements))
        {
            item.Dispose();
#if DEBUG
            PluginLog.Debug($"Item: {item} from '_drawingElements' was disposed");
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
            foreach (var item in RotationSolverPlugin._drawingElements)
            {
                // Let each element update its per-frame state before drawing
                item.UpdateOnFrameMain();

                drawing2Ds.Add(Task.Run(() =>
                {
                    return item.To2DMain();
                }));
            }
        }

        _ = await Task.WhenAll(drawing2Ds);

        List<IDrawing2D> result = [];
        foreach (var task in drawing2Ds)
        {
            if (task.Result != null)
            {
                foreach (var drawing in task.Result)
                {
                    result.Add(drawing);
                }
            }
        }
        return [.. result];
    }
}
