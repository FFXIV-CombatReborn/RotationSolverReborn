namespace RotationSolver.Updaters;

internal static class MacroUpdater
{
	internal static MacroItem? DoingMacro;

	public static void UpdateMacro()
	{
		if (DoingMacro == null && DataCenter.Macros.TryDequeue(out MacroItem? macro))
		{
			DoingMacro = macro;
		}

		if (DoingMacro == null)
		{
			return;
		}

		if (DoingMacro.IsRunning)
		{
			if (DoingMacro.EndUseMacro())
			{
				DoingMacro = null;
			}
			return;
		}

		_ = DoingMacro.StartUseMacro();
	}
}