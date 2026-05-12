namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// One row in the PvP LB database. <see cref="ActionId"/> is the value of
/// <see cref="Dalamud.Game.ClientState.Objects.SubKinds.IBattleNpc.CastActionId"/>
/// observed during a cast (also exposed on <c>IBattleChara</c>).
/// </summary>
public readonly record struct LBEntry(
    uint ActionId,
    LBCategory Category,
    string Description);
