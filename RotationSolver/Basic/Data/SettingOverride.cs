namespace RotationSolver.Basic.Data;

/// <summary>
/// How an IPC caller wants a boolean setting handled while the operating mode it requested is active.
/// </summary>
public enum SettingOverride : byte
{
	/// <summary>
	/// Keep the user's own setting.
	/// </summary>
	UseSetting,

	/// <summary>
	/// Force the setting off.
	/// </summary>
	ForceOff,

	/// <summary>
	/// Force the setting on.
	/// </summary>
	ForceOn,
}
