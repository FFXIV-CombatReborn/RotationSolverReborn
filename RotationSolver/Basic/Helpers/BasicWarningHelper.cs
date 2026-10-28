using ECommons.Logging;

namespace RotationSolver.Basic.Helpers
{
	internal static class BasicWarningHelper
	{
		internal static bool AddSystemWarning(string warning)
		{
			var systemWarnings = DataCenter.SystemWarnings;
			if (systemWarnings == null)
			{
				PluginLog.Error("SystemWarnings dictionary is not initialized.");
				throw new InvalidOperationException("SystemWarnings dictionary is not initialized.");
			}

			lock (systemWarnings)
			{
				try
				{
					if (systemWarnings.TryAdd(warning, DateTime.Now))
					{
						return true;
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error($"Failed to add system warning: {ex.Message}");
				}
			}
			return false;
		}
	}
}