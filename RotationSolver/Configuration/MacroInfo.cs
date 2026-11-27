using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace RotationSolver.Configuration;
#pragma warning disable CS1591 // Missing XML comment for publicly visible
public class MacroInfo
{
	public int MacroIndex;
	public bool IsShared;

	public MacroInfo()
	{
		MacroIndex = -1;
		IsShared = false;
	}

	public unsafe bool AddMacro(IGameObject? tar = null)
	{
		if (MacroIndex is < 0 or > 99)
		{
			return false;
		}

		try
		{
			var macroModule = RaptureMacroModule.Instance();
			if (macroModule == null)
			{
				return false;
			}

			var macro = macroModule->GetMacro(IsShared ? 1u : 0u, (uint)MacroIndex);
			if (macro == null)
			{
				return false;
			}

			DataCenter.Macros.Enqueue(new MacroItem(tar, macro));
			return true;
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to add macro: {ex.Message}");
			return false;
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible