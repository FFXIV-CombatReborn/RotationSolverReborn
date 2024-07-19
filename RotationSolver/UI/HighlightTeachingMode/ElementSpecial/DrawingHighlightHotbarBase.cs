namespace RotationSolver.UI.HighlightTeachingMode.ElementSpecial;

/// <summary> 
/// Drawing element 
/// </summary>
public abstract class DrawingHighlightHotbarBase : IDisposable
{
    /// <summary> 
    /// If it is enabled. 
    /// </summary>
    public virtual bool Enable { get; set; } = true;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RotationSolverPlugin._drawingElements.Remove(this);
        GC.SuppressFinalize(this);
    }

    internal IEnumerable<IDrawing2D> To2DMain()
    {
        if (!Enable) return new List<IDrawing2D>();
        return To2D();
    }

    internal void UpdateOnFrameMain()
    {
        if (!Enable) return;
        UpdateOnFrame();
    }

    protected DrawingHighlightHotbarBase()
    {
        RotationSolverPlugin._drawingElements.Add(this);
    }

    private protected abstract IEnumerable<IDrawing2D> To2D();

    /// <summary> 
    /// The things that it should update on every frame. 
    /// </summary>
    protected abstract void UpdateOnFrame();

    private bool _disposed;
}
