using ECommons.Logging;

namespace RotationSolver.Basic.Helpers
{
	internal static class BasicWarningHelper
	{
		internal static bool AddSystemWarning(string warning)
		{
			Dictionary<string, DateTime> systemWarnings = DataCenter.SystemWarnings;
			if (systemWarnings == null)
			{
				// Log the error before throwing the exception
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
					// Log the exception
					PluginLog.Error($"Failed to add system warning: {ex.Message}");
				}
			}
			return false;
		}
	}
}