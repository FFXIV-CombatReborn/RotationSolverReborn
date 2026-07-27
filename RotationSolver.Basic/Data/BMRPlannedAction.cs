namespace RotationSolver.Basic.Data;

/// <summary>
/// Mirrors BossMod's record for JSON deserialization over IPC
/// </summary>
public readonly record struct BMRPlannedAction(
	string ModuleType,
	string TrackName,
	string OptionName,
	uint ActionType,
	uint ActionId,
	float ActivationIn,
	float WindowEndIn,
	int Target,
	int TargetParam);
