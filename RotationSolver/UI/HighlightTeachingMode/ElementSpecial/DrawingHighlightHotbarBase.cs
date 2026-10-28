namespace RotationSolver.UI.HighlightTeachingMode.ElementSpecial;

public abstract class DrawingHighlightHotbarBase : IDisposable
{
	public virtual bool Enable { get; set; } = true;

	private bool _disposed;
	private readonly Lock _disposeLock = new();

	protected DrawingHighlightHotbarBase()
	{
		RotationSolverPlugin._drawingElements.Add(this);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		lock (_disposeLock)
		{
			if (_disposed)
			{
				return;
			}

			if (disposing)
			{
				_ = RotationSolverPlugin._drawingElements.Remove(this);
			}

			_disposed = true;
		}
	}

	internal IEnumerable<IDrawing2D> To2DMain()
	{
		return !Enable ? [] : To2D();
	}

	internal void UpdateOnFrameMain()
	{
		if (!Enable)
		{
			return;
		}

		UpdateOnFrame();
	}

	private protected abstract IEnumerable<IDrawing2D> To2D();

	protected abstract void UpdateOnFrame();
}