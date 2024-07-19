namespace RotationSolver.UI.HighlightTeachingMode.ElementSpecial;

/// <summary>
/// Drawing element
/// </summary>
public abstract class DrawingHighlightHotbarBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// If it is enabled.
    /// </summary>
    public virtual bool Enable { get; set; } = true;

    private protected DrawingHighlightHotbarBase()
    {
        RotationSolverPlugin._drawingElements.Add(this);
    }

    internal void UpdateOnFrameMain()
    {
        if (!Enable) return;
        UpdateOnFrame();
    }

    /// <summary>
    /// The things that it should update on every frame.
    /// </summary>
    protected abstract void UpdateOnFrame();

    internal IEnumerable<IDrawing2D> To2DMain()
    {
        if (!Enable) return new List<IDrawing2D>();
        return To2D();
    }

    private protected abstract IEnumerable<IDrawing2D> To2D();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RotationSolverPlugin._drawingElements.Remove(this);
        GC.SuppressFinalize(this);
    }
}
