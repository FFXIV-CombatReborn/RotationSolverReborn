using Dalamud.Game.ClientState.Statuses;
using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.GameFunctions;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// The helper for the status.
/// </summary>
public static class StatusHelper
{
    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] RangePhysicalDefense { get; } =
    [
        StatusID.Troubadour,
        StatusID.Tactician_1951,
        StatusID.Tactician_2177,
        StatusID.ShieldSamba,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] PhysicalResistance { get; } =
    [
        StatusID.IceSpikes_1720,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] PhysicalRangedResistance { get; } =
    [
        StatusID.RangedResistance,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] MagicResistance { get; } =
    [
        StatusID.MagicResistance,
        StatusID.RepellingSpray_556,
        StatusID.MagitekField_2166,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] AreaHots { get; } =
    [
        StatusID.AspectedHelios,
        StatusID.HeliosConjunction,
        StatusID.MedicaIi,
        StatusID.TrueMedicaIi,
        StatusID.PhysisIi,
        StatusID.Physis,
        StatusID.SacredSoil_1944,
        StatusID.WhisperingDawn,
        StatusID.AngelsWhisper,
        StatusID.Seraphism_3885,
        StatusID.Asylum_1911,
        StatusID.DivineAura,
        StatusID.MedicaIii_3986,
        StatusID.MedicaIii
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] SingleHots { get; } =
    [
        StatusID.AspectedBenefic,
        StatusID.Regen,
        StatusID.Regen_897,
        StatusID.Regen_1330,
        StatusID.TheEwer_3891,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] TankStanceStatus { get; } =
    [
        StatusID.Grit,
        StatusID.RoyalGuard_1833,
        StatusID.IronWill,
        StatusID.Defiance,
        StatusID.Defiance_3124,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] NoNeedHealingStatus { get; } =
    [
        StatusID.Holmgang_409,
        StatusID.LivingDead,
        //StatusID.WalkingDead,
        StatusID.Superbolide,
        StatusID.Invulnerability,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] SwiftcastStatus { get; } =
    [
        StatusID.Swiftcast,
        StatusID.Triplecast,
        StatusID.Dualcast,
        StatusID.OccultQuick
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] AstCardStatus { get; } =
    [
        StatusID.TheBalance_3887,
        StatusID.TheSpear_3889,
        StatusID.Weakness,
        StatusID.BrinkOfDeath,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] RampartStatus { get; } =
    [
        StatusID.Superbolide,
        StatusID.HallowedGround,
        StatusID.Rampart,
        StatusID.Bulwark,
        StatusID.Bloodwhetting,
        StatusID.Vengeance,
        StatusID.Sentinel,
        StatusID.ShadowWall,
        StatusID.Nebula,
        StatusID.GreatNebula,
        StatusID.Holmgang_409,
        StatusID.LivingDead,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] NoPositionalStatus { get; } =
    [
        StatusID.TrueNorth,
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] DoomHealStatus { get; } =
    [
        StatusID.Doom_1769,
    ];

    /// <summary>
    /// Statuses for the Phantom Oracle spell PredictPvE.
    /// </summary>
    public static StatusID[] OracleStatuses { get; } =
    [
        StatusID.PredictionOfCleansing,
        StatusID.PredictionOfStarfall,
        StatusID.PredictionOfJudgment,
        StatusID.PredictionOfBlessing
    ];

    /// <summary>
    /// Statuses that can be dispelled by Occult Dispel.
    /// </summary>
    public static StatusID[] PhantomDispellable { get; } =
    [
        StatusID.DamageUp_1161,
        StatusID.DamageUp,
        StatusID.DarkDefenses,
        StatusID.MagicDamageUp_2556,
        StatusID.EvasionUp_1706
        //StatusID.Invincibility_4539 maybe this, need verification, seems to be on guardian knight in Rooms & Lockwards part of forked tower
    ];

    /// <summary>
    /// 
    /// </summary>
    public static StatusID[] PurifyPvPStatuses { get; } =
    [
        StatusID.Stun_1343,
        StatusID.Heavy_1344,
        StatusID.Bind_1345,
        StatusID.Silence_1347,
        StatusID.DeepFreeze_3219,
        StatusID.MiracleOfNature,
    ];

    /// <summary>
    /// Determines if the specified battle character has reached the maximum number of status effects.
    /// </summary>
    /// <param name="battleChara">The battle character to check.</param>
    /// <returns>
    /// <c>true</c> if the character's status list is at the cap (30 for players, 60 for NPCs); otherwise, <c>false</c>.
    /// </returns>
    public unsafe static bool IsStatusCapped(IBattleChara battleChara)
    {
        if (battleChara == null)
            return false;

        try
        {
            if (battleChara.StatusList == null)
                return false;
        }
        catch
        {
            return false;
        }

        if (battleChara.IsValid())
        {
            int count = 0;
            foreach (var x in battleChara.StatusList)
            {
                if (x.StatusId != 0)
                {
                    count++;
                }
            }
            if (count == battleChara.Struct()->StatusManager.NumValidStatuses)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check whether the target needs to be healing.
    /// </summary>
    /// <param name="Invulnp"></param>
    /// <returns></returns>
    public static bool NoNeedHealingInvuln(this IBattleChara Invulnp)
    {
        return Invulnp.WillStatusEndGCD(2, 0, false, NoNeedHealingStatus);
    }

    /// <summary>
    /// Check if the target needs to be healed because of Doomed To Heal status.
    /// </summary>
    /// <param name="Doomp"></param>
    /// <returns></returns>
    public static bool DoomNeedHealing(this IBattleChara Doomp)
    {
        return Doomp.HasStatus(false, DoomHealStatus);
    }

    /// <summary>
    /// Will any of <paramref name="statusIDs"/> end after <paramref name="gcdCount"/> GCDs plus <paramref name="offset"/> seconds?
    /// </summary>
    /// <param name="battleChara"></param>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool WillStatusEndGCD(this IBattleChara battleChara, uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params StatusID[] statusIDs)
    {
        return WillStatusEnd(battleChara, DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);
    }

    /// <summary>
    /// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
    /// </summary>
    /// <param name="battleChara"></param>
    /// <param name="time"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool WillStatusEnd(this IBattleChara battleChara, float time, bool isFromSelf = true, params StatusID[] statusIDs)
    {
        if (HasApplyStatus(battleChara, statusIDs))
        {
            return false;
        }

        float statusTime = battleChara.StatusTime(isFromSelf, statusIDs);
        return (statusTime >= 0f || !battleChara.HasStatus(isFromSelf, statusIDs)) && statusTime <= time;
    }

    /// <summary>
    /// Get the remaining time of the status (raw remaining time of the earliest matching status). Returns 0 if none.
    /// NOTE: Previously this subtracted DefaultGCDRemain which caused premature refresh decisions.
    /// </summary>
    public static float StatusTime(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        try
        {
            if (HasApplyStatus(battleChara, statusIDs))
            {
                return float.MaxValue;
            }

            IEnumerable<float> times = battleChara.StatusTimes(isFromSelf, statusIDs);
            float min = float.MaxValue;
            bool found = false;
            foreach (float t in times)
            {
                if (t < min)
                {
                    min = t;
                }
                found = true;
            }
            // Return 0 when not found (legacy behaviour expected by callers), otherwise raw remaining time.
            return !found ? 0f : min;
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to get status time: {ex.Message}");
            return 0f;
        }
    }

    internal static IEnumerable<float> StatusTimes(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        foreach (Status status in battleChara.GetStatus(isFromSelf, statusIDs))
        {
            yield return status.RemainingTime == 0f ? float.MaxValue : status.RemainingTime;
        }
    }

    /// <summary>
    /// Get the stack count of the status.
    /// </summary>
    /// <param name="battleChara"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static byte StatusStack(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (HasApplyStatus(battleChara, statusIDs))
        {
            return byte.MaxValue;
        }

        IEnumerable<byte> stacks = battleChara.StatusStacks(isFromSelf, statusIDs);
        byte min = byte.MaxValue;
        bool found = false;
        foreach (byte s in stacks)
        {
            if (s < min)
            {
                min = s;
            }

            found = true;
        }
        return found ? min : (byte)0;
    }

    private static IEnumerable<byte> StatusStacks(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        foreach (Status status in battleChara.GetStatus(isFromSelf, statusIDs))
        {
            yield return (byte)(status.Param == 0 ? byte.MaxValue : status.Param);
        }
    }

    /// <summary>
    /// Check if the object has any of the specified statuses.
    /// </summary>
    /// <param name="battleChara"></param>
    /// <param name="isFromSelf"></param>
    /// <param name="statusIDs"></param>
    /// <returns></returns>
    public static bool HasStatus(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        try
        {
            if (battleChara.StatusList == null)
            {
                return false;
            }
        }
        catch
        {
            // StatusList threw, treat as unavailable
            return false;
        }

        if (HasApplyStatus(battleChara, statusIDs))
        {
            return true;
        }

        foreach (Status _ in battleChara.GetStatus(isFromSelf, statusIDs))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified <paramref name="battleChara"/> currently has any of the statuses being applied,
    /// as tracked by <c>DataCenter.ApplyStatus</c> during effect time.
    /// </summary>
    /// <param name="battleChara">The battle character to check for applied statuses.</param>
    /// <param name="statusIDs">An array of status IDs to check against the applied status.</param>
    /// <returns>
    /// <c>true</c> if any of the specified statuses are currently being applied to the character; otherwise, <c>false</c>.
    /// </returns>
    public static bool HasApplyStatus(this IBattleChara battleChara, StatusID[] statusIDs)
    {
        try
        {
            if (battleChara.StatusList == null)
            {
                return false;
            }
        }
        catch
        {
            // StatusList threw, treat as unavailable
            return false;
        }

        if (DataCenter.InEffectTime && DataCenter.ApplyStatus.TryGetValue(battleChara.GameObjectId, out uint statusId))
        {
            foreach (StatusID s in statusIDs)
            {
                if ((uint)s == statusId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Remove the specified status.
    /// </summary>
    /// <param name="status"></param>
    public static void StatusOff(StatusID status)
    {
        if (!DataCenter.IsActivated())
        {
            return;
        }

        if (!Player.Object.HasStatus(false, status))
        {
            return;
        }
        
        try
        {
            Chat.SendMessage($"/statusoff {GetStatusName(status)}");
            PluginLog.Information($"Status {GetStatusName(status)} removed successfully.");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to remove status {GetStatusName(status)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the name of the specified status.
    /// </summary>
    /// <param name="id">The status ID.</param>
    /// <returns>The name of the status.</returns>
    internal static string GetStatusName(StatusID id)
    {
        Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Status> sheet = Service.GetSheet<Lumina.Excel.Sheets.Status>();
        if (sheet == null)
        {
            return string.Empty;
        }

        Lumina.Excel.Sheets.Status statusRow = sheet.GetRow((uint)id);
        return statusRow.RowId == 0 ? string.Empty : statusRow.Name.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Get the statuses of the specified object.
    /// </summary>
    /// <param name="battleChara">The object to get the statuses from.</param>
    /// <param name="isFromSelf">Whether the statuses are from self.</param>
    /// <param name="statusIDs">The status IDs to look for.</param>
    /// <returns>An enumerable of statuses.</returns>
private static IEnumerable<Status> GetStatus(this IBattleChara battleChara, bool isFromSelf, params StatusID[] statusIDs)
    {
        if (battleChara == null)
        {
            yield break;
        }

        StatusList statusList;
        try
        {
            statusList = battleChara.StatusList;
            if (statusList == null)
            {
                yield break;
            }
        }
        catch
        {
            // StatusList threw, treat as unavailable
            yield break;
        }

        // Linear membership check to avoid HashSet allocation (statusIDs is small in practice)
        static bool ContainsId(uint id, StatusID[] ids)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if ((uint)ids[i] == id) return true;
            }
            return false;
        }

        ulong playerId = Player.Object.GameObjectId;

        for (int i = 0; i < statusList.Length; i++)
        {
            Status? status = statusList[i];
            if (status == null || status.StatusId == 0)
            {
                continue;
            }

            if (isFromSelf)
            {
                if (status.SourceId != playerId && status.SourceObject?.OwnerId != playerId)
                {
                    continue;
                }
            }

            if (ContainsId(status.StatusId, statusIDs))
            {
                yield return status;
            }
        }
    }


    /// <summary>
    /// Check if the status is invincible.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status is invincible, otherwise false.</returns>
    public static bool IsInvincible(this Status status)
    {
        if (status == null)
        {
            return false;
        }

        if (status.GameData.Value.Icon == 15024)
        {
            return true;
        }

        if (OtherConfiguration.InvincibleStatus == null)
        {
            return false;
        }

        foreach (uint id in OtherConfiguration.InvincibleStatus)
        {
            if (id == status.StatusId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if the status is a priority.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status is a priority, otherwise false.</returns>
    public static bool IsPriority(this Status status)
    {
        if (status == null)
        {
            return false;
        }

        if (OtherConfiguration.PriorityStatus == null)
        {
            return false;
        }

        foreach (uint id in OtherConfiguration.PriorityStatus)
        {
            if (id == status.StatusId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if the status needs to be dispelled immediately.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status needs to be dispelled, otherwise false.</returns>
    public static bool IsDangerous(this Status status)
    {
        if (status == null)
        {
            return false;
        }

        if (!status.CanDispel())
        {
            return false;
        }

        if (status.Param > 2)
        {
            return true;
        }

        if (status.RemainingTime > 20)
        {
            return true;
        }

        if (OtherConfiguration.DangerousStatus == null)
        {
            return false;
        }

        foreach (uint id in OtherConfiguration.DangerousStatus)
        {
            if (id == status.StatusId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if the status can be dispelled.
    /// </summary>
    /// <param name="status">The status to check.</param>
    /// <returns>True if the status can be dispelled, otherwise false.</returns>
    public static bool CanDispel(this Status status)
    {
        return status != null && status.GameData.Value.CanDispel == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool CanStatusOff(this Status status)
    {
        return status != null && status.GameData.Value.CanStatusOff == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool LockActions(this Status status)
    {
        return status != null && status.GameData.Value.LockActions == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool LockMovement(this Status status)
    {
        return status != null && status.GameData.Value.LockMovement == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool LockControl(this Status status)
    {
        return status != null && status.GameData.Value.LockControl == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }

    /// <summary>
    /// Unknown3 is used to determine if the status indicates a tether.
    /// </summary>
    public static bool IsTether(this Status status)
    {
        return status != null && status.GameData.Value.Unknown3 == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
    }
}
