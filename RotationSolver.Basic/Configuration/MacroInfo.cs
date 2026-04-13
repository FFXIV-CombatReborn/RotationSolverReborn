using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace RotationSolver.Basic.Configuration;
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
			RaptureMacroModule.Macro* macro = RaptureMacroModule.Instance()->GetMacro(IsShared ? 1u : 0u, (uint)MacroIndex);

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