namespace RotationSolver.Engine.Actions;

internal ref struct ForceEnableScope
{
	public ForceEnableScope()
	{
		IBaseAction.ForceEnable = true;
	}

	public void Dispose()
	{
		IBaseAction.ForceEnable = false;
	}
}
