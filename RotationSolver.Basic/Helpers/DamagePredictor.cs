using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic.Helpers;

public static class DamagePredictor
{
    /// <summary>
    /// Predicts if a raidwide attack is currently being cast by any hostile target in combat.
    /// This uses heuristics (cast time >= 4s, targeting self or no one) to pre-shield/mitigate globally.
    /// </summary>
    public static bool IsRaidwidePredictable()
    {
        if (Player.Object == null) return false;

        foreach (var obj in Svc.Objects)
        {
            if (obj is IBattleChara hostile && hostile.IsEnemy() && hostile.IsCasting)
            {
                // If the boss is casting a spell lasting >= 4s that targets itself or no specific player
                if (hostile.IsBossFromTTK() && hostile.TotalCastTime >= 4f && hostile.TargetObjectId == hostile.GameObjectId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Predicts if a tankbuster is currently being cast targeting the specified player.
    /// </summary>
    public static bool IsTankbusterPredictable(IBattleChara target)
    {
        if (Player.Object == null || target == null) return false;

        foreach (var obj in Svc.Objects)
        {
            if (obj is IBattleChara hostile && hostile.IsEnemy() && hostile.IsCasting)
            {
                // If the boss is casting a long spell specifically on the target (usually a Tankbuster)
                if (hostile.IsBossFromTTK() && hostile.TotalCastTime >= 3f && hostile.TargetObjectId == target.GameObjectId)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
