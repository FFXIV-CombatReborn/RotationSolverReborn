using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace RotationSolver.Basic.Data;

/// <summary>
/// The class to handle the event of macro.
/// </summary>
/// <remarks>
/// Constructer
/// </remarks>
/// <param name="target"></param>
/// <param name="macro"></param>
public unsafe class MacroItem(IGameObject? target, RaptureMacroModule.Macro* macro)
{
	private ulong _lastTargetId;
	private readonly ulong _targetId = target?.GameObjectId ?? 0;
	private readonly RaptureMacroModule.Macro* _macro = macro;

	/// <summary>
	/// The target of this macro.
	/// </summary>
	public IGameObject? Target => _targetId == 0 ? null : Svc.Objects.SearchById(_targetId);

	/// <summary>
	/// Is macro running.
	/// </summary>
	public bool IsRunning { get; private set; }

	/// <summary>
	/// Starts running the macro.
	/// </summary>
	/// <returns>True if the macro started successfully; otherwise, false.</returns>
	public bool StartUseMacro()
	{
		if (RaptureShellModule.Instance()->MacroCurrentLine > -1)
		{
			return false;
		}

		_lastTargetId = Svc.Targets.Target?.GameObjectId ?? 0;
		Svc.Targets.Target = Target;
		RaptureShellModule.Instance()->ExecuteMacro(_macro);

		IsRunning = true;
		return true;
	}

	/// <summary>
	/// Ends the macro.
	/// </summary>
	/// <returns>True if the macro ended successfully; otherwise, false.</returns>
	public bool EndUseMacro()
	{
		if (RaptureShellModule.Instance()->MacroCurrentLine > -1)
		{
			return false;
		}

		Svc.Targets.Target = _lastTargetId == 0 ? null : Svc.Objects.SearchById(_lastTargetId);

		IsRunning = false;
		return true;
	}
}
