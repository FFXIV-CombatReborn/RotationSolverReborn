using Dalamud.Game.ClientState.Statuses;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Logging;
using Lumina.Excel;
using GameStatus = FFXIVClientStructs.FFXIV.Client.Game.Status;
using StatusRow = Lumina.Excel.Sheets.Status;

namespace RotationSolver.Engine.Helpers;

/// <summary>
/// The helper for the status.
/// </summary>
public static partial class StatusHelper
{
	/// <summary>
	/// Determines if the specified battle character has reached the maximum number of status effects.
	/// </summary>
	/// <param name="battleChara">The battle character to check.</param>
	/// <returns>
	/// <c>true</c> if the character's status list is at the cap (30 for players, 60 for NPCs); otherwise, <c>false</c>.
	/// </returns>
	public unsafe static bool IsStatusCapped(IBattleChara battleChara)
	{
		if (battleChara == null || !battleChara.IsValid() || battleChara.Struct() == null)
		{
			return false;
		}

		var statuses = battleChara.GetStatuses();
		if (statuses.IsEmpty)
		{
			return false;
		}

		foreach (ref readonly var status in statuses)
		{
			if (status.StatusId == 0)
			{
				return false;
			}
		}

		return true;
	}

	internal static unsafe ReadOnlySpan<GameStatus> GetStatuses(this IBattleChara? battleChara)
	{
		if (battleChara == null)
		{
			return default;
		}

		var chara = battleChara.Struct();
		if (chara == null)
		{
			return default;
		}

		var statuses = chara->StatusManager.Status;
		int count = chara->StatusManager.NumValidStatuses;
		if (count <= 0)
		{
			return default;
		}

		return count < statuses.Length ? statuses[..count] : statuses;
	}

	/// <summary>
	/// True when the player has no <see cref="NoNeedHealingStatus"/> (invuln, healing block) lasting beyond 2 GCDs.
	/// </summary>
	public static bool PlayerCanBeHealed()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerWillStatusEndGCD(2, 0, false, NoNeedHealingStatus);
	}

	/// <summary>
	/// True when the target has no <see cref="NoNeedHealingStatus"/> (invuln, healing block) lasting beyond 2 GCDs.
	/// </summary>
	public static bool CanBeHealed(this IBattleChara battleChara)
	{
		return battleChara.WillStatusEndGCD(2, 0, false, NoNeedHealingStatus);
	}

	/// <summary>
	/// Check if the Player needs to be healed because of Doomed To Heal status.
	/// </summary>
	/// <returns></returns>
	public static bool PlayerDoomNeedHealing()
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerHasStatus(false, DoomHealStatus);
	}

	/// <summary>
	/// Check if the target needs to be healed because of Doomed To Heal status.
	/// </summary>
	/// <param name="Doomp"></param>
	/// <returns></returns>
	public static bool DoomNeedHealing(this IBattleChara Doomp)
	{
		if (Doomp == null)
		{
			return false;
		}

		if (!Doomp.IsValid())
		{
			return false;
		}

		if (Doomp.Address == nint.Zero || Doomp.EntityId == 0)
		{
			return false;
		}

		try
		{
			if (HasStatus(Doomp, false, DoomHealStatus))
			{
				return true;
			}
		}
		catch (Exception)
		{
			return false;
		}

		return false;
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="gcdCount"/> GCDs plus <paramref name="offset"/> seconds?
	/// </summary>
	/// <param name="gcdCount"></param>
	/// <param name="offset"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerWillStatusEndGCD(uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return false;
		}

		return PlayerWillStatusEnd(DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);
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
	public static bool WillStatusEndGCD(this IBattleChara battleChara, uint gcdCount = 0, float offset = 0, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		return WillStatusEnd(battleChara, DataCenter.GCDTime(gcdCount, offset), isFromSelf, statusIDs);
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
	/// </summary>
	/// <param name="time"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerWillStatusEnd(float time, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (PlayerHasApplyStatus(statusIDs))
		{
			return false;
		}

		var statusTime = PlayerStatusTime(isFromSelf, statusIDs);
		return (statusTime >= 0f || !PlayerHasStatus(isFromSelf, statusIDs)) && statusTime <= time;
	}

	/// <summary>
	/// Will any of <paramref name="statusIDs"/> end after <paramref name="time"/> seconds?
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="time"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool WillStatusEnd(this IBattleChara battleChara, float time, bool isFromSelf = true, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (HasApplyStatus(battleChara, statusIDs))
		{
			return false;
		}

		var statusTime = battleChara.StatusTime(isFromSelf, statusIDs);
		return (statusTime >= 0f || !battleChara.HasStatus(isFromSelf, statusIDs)) && statusTime <= time;
	}

	/// <summary>
	/// Get the remaining time of the status (raw remaining time of the earliest matching status). Returns 0 if none.
	/// NOTE: Previously this subtracted DefaultGCDRemain which caused premature refresh decisions.
	/// </summary>
	public static float PlayerStatusTime(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return 0f;
		}

		try
		{
			if (PlayerHasApplyStatus(statusIDs))
			{
				return float.MaxValue;
			}

			var min = MinStatusRemainingTime(Player.Object, isFromSelf, statusIDs, out var found);
			return !found ? 0f : min;
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to get status time: {ex.Message}");
			return 0f;
		}
	}

	/// <summary>
	/// Get the remaining time of the status (raw remaining time of the earliest matching status). Returns 0 if none.
	/// </summary>
	public static float StatusTime(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		try
		{
			if (HasApplyStatus(battleChara, statusIDs))
			{
				return float.MaxValue;
			}

			var min = MinStatusRemainingTime(battleChara, isFromSelf, statusIDs, out var found);
			return !found ? 0f : min;
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to get status time: {ex.Message}");
			return 0f;
		}
	}

	/// <summary>
	/// Get the stack count of the status.
	/// </summary>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static byte PlayerStatusStack(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (Player.Object == null)
		{
			return 0;
		}

		if (PlayerHasApplyStatus(statusIDs))
		{
			return byte.MaxValue;
		}

		var min = MinStatusStack(Player.Object, isFromSelf, statusIDs, out var found);
		return found ? min : (byte)0;
	}

	/// <summary>
	/// Get the stack count of the status.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static byte StatusStack(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		if (HasApplyStatus(battleChara, statusIDs))
		{
			return byte.MaxValue;
		}

		var min = MinStatusStack(battleChara, isFromSelf, statusIDs, out var found);
		return found ? min : (byte)0;
	}

	/// <summary>
	/// Check if the player object has any of the specified statuses.
	/// </summary>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool PlayerHasStatus(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		var player = Player.Object;
		if (player == null)
		{
			return false;
		}

		return PlayerHasApplyStatus(statusIDs) || AnyStatusMatches(player, isFromSelf, statusIDs);
	}

	/// <summary>
	/// Check if the object has any of the specified statuses.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <param name="isFromSelf"></param>
	/// <param name="statusIDs"></param>
	/// <returns></returns>
	public static bool HasStatus(this IBattleChara battleChara, bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		try
		{
			if (battleChara == null || !DataCenter.PlayerAvailable() || !battleChara.IsValid())
			{
				return false;
			}

			return HasApplyStatus(battleChara, statusIDs) || AnyStatusMatches(battleChara, isFromSelf, statusIDs);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Checks if the player currently has any of the statuses being applied,
	/// as tracked by <c>DataCenter.ApplyStatus</c> during effect time.
	/// </summary>
	/// <param name="statusIDs">An array of status IDs to check against the applied status.</param>
	/// <returns>
	/// <c>true</c> if any of the specified statuses are currently being applied to the character; otherwise, <c>false</c>.
	/// </returns>
	public static bool PlayerHasApplyStatus(ReadOnlySpan<StatusID> statusIDs)
	{
		if (!DataCenter.InEffectTime)
		{
			return false;
		}

		var player = Player.Object;
		return player != null && IsApplyingStatus(player.GameObjectId, statusIDs);
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
	public static bool HasApplyStatus(this IBattleChara battleChara, ReadOnlySpan<StatusID> statusIDs)
	{
		return battleChara != null && battleChara.Address != nint.Zero && DataCenter.InEffectTime
			&& IsApplyingStatus(battleChara.GameObjectId, statusIDs);
	}

	private static bool IsApplyingStatus(ulong objectId, ReadOnlySpan<StatusID> statusIDs)
	{
		return DataCenter.ApplyStatus.TryGetValue(objectId, out var statusId) && ContainsId(statusId, statusIDs);
	}

	public static bool AnyPartyMemberHasStatus(bool isFromSelf, params ReadOnlySpan<StatusID> statusIDs)
	{
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.HasStatus(isFromSelf, statusIDs))
			{
				return true;
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

		if (!PlayerHasStatus(false, status))
		{
			return;
		}

		try
		{
			Chat.SendMessage($"/statusoff \"{GetStatusName(status)}\"");
			PluginLog.Information($"Status {GetStatusName(status)} removed successfully.");
		}
		catch (Exception ex)
		{
			PluginLog.Error($"Failed to remove status {GetStatusName(status)}: {ex.Message}");
		}
	}

	internal static string GetStatusName(StatusID id)
	{
		var sheet = Service.GetSheet<Lumina.Excel.Sheets.Status>();
		if (sheet == null)
		{
			return string.Empty;
		}

		return sheet.TryGetRow((uint)id, out var statusRow) && statusRow.RowId != 0
			? statusRow.Name.ToString() ?? string.Empty
			: string.Empty;
	}

	private static bool ContainsId(uint id, ReadOnlySpan<StatusID> ids)
	{
		for (var i = 0; i < ids.Length; i++)
		{
			if ((uint)ids[i] == id)
			{
				return true;
			}
		}
		return false;
	}

	private static bool AnyStatusMatches(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs)
	{
		var statuses = battleChara.GetStatuses();
		if (statuses.IsEmpty)
		{
			return false;
		}

		var playerId = isFromSelf ? Player.Object?.GameObjectId ?? 0 : 0;
		if (isFromSelf && playerId == 0)
		{
			return false;
		}

		foreach (ref readonly var status in statuses)
		{
			if (status.StatusId != 0 && ContainsId(status.StatusId, statusIDs) && (!isFromSelf || IsFromPlayer(status, playerId)))
			{
				return true;
			}
		}
		return false;
	}

	private static long _ownedIdsTick = long.MinValue;
	private static ulong _ownedIdsPlayerId;
	private static readonly HashSet<uint> _ownedIds = [];

	private static bool IsFromPlayer(in GameStatus status, ulong playerId)
	{
		if (playerId == 0)
		{
			return false;
		}

		var sourceId = status.SourceObject.ObjectId;
		if (sourceId == playerId)
		{
			return true;
		}

		if (sourceId is 0 or 0xE0000000)
		{
			return false;
		}

		var now = Environment.TickCount64;
		if (_ownedIdsPlayerId != playerId || _ownedIdsTick == long.MinValue || now - _ownedIdsTick >= 15)
		{
			_ownedIds.Clear();
			_ownedIdsPlayerId = playerId;
			_ownedIdsTick = now;
			foreach (var obj in Svc.Objects)
			{
				if (obj != null && obj.OwnerId == playerId)
				{
					_ = _ownedIds.Add(obj.EntityId);
				}
			}
		}

		return _ownedIds.Contains(sourceId);
	}

	internal static float MinStatusRemainingTime(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs, out bool found)
	{
		found = false;
		var min = float.MaxValue;
		var statuses = battleChara.GetStatuses();
		if (statuses.IsEmpty)
		{
			return min;
		}

		var playerId = isFromSelf ? Player.Object?.GameObjectId ?? 0 : 0;
		if (isFromSelf && playerId == 0)
		{
			return min;
		}
		foreach (ref readonly var status in statuses)
		{
			if (status.StatusId != 0 && ContainsId(status.StatusId, statusIDs) && (!isFromSelf || IsFromPlayer(status, playerId)))
			{
				var t = status.RemainingTime == 0f ? float.MaxValue : status.RemainingTime;
				if (t < min)
				{
					min = t;
				}
				found = true;
			}
		}
		return min;
	}

	private static byte MinStatusStack(IBattleChara? battleChara, bool isFromSelf, ReadOnlySpan<StatusID> statusIDs, out bool found)
	{
		found = false;
		var min = byte.MaxValue;
		var statuses = battleChara.GetStatuses();
		if (statuses.IsEmpty)
		{
			return min;
		}

		var playerId = isFromSelf ? Player.Object?.GameObjectId ?? 0 : 0;
		foreach (ref readonly var status in statuses)
		{
			if (status.StatusId != 0 && ContainsId(status.StatusId, statusIDs) && (!isFromSelf || IsFromPlayer(status, playerId)))
			{
				var s = (byte)(status.Param == 0 ? byte.MaxValue : status.Param);
				if (s < min)
				{
					min = s;
				}
				found = true;
			}
		}
		return min;
	}

	private static ExcelSheet<StatusRow>? _statusSheet;

	private static bool TryGetStatusRow(uint statusId, out StatusRow row)
	{
		return (_statusSheet ??= Service.GetSheet<StatusRow>()).TryGetRow(statusId, out row);
	}

	internal static bool IsInvincibleStatus(uint statusId)
	{
		return (TryGetStatusRow(statusId, out var row) && row.Icon == 15024)
			|| OtherConfiguration.InvincibleStatus?.Contains(statusId) == true;
	}

	internal static bool HasInvincibleStatus(this IBattleChara battleChara)
	{
		foreach (ref readonly var status in battleChara.GetStatuses())
		{
			if (status.StatusId != 0 && IsInvincibleStatus(status.StatusId))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool HasPriorityStatus(this IBattleChara battleChara)
	{
		var priority = OtherConfiguration.PriorityStatus;
		if (priority == null || priority.Count == 0)
		{
			return false;
		}

		foreach (ref readonly var status in battleChara.GetStatuses())
		{
			if (status.StatusId != 0 && priority.Contains(status.StatusId))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool CanDispel(in GameStatus status, float minRemaining)
	{
		return status.StatusId != 0 && status.RemainingTime > minRemaining
			&& TryGetStatusRow(status.StatusId, out var row) && row.CanDispel;
	}

	internal static bool IsDangerous(in GameStatus status, float minRemaining)
	{
		if (!CanDispel(status, minRemaining))
		{
			return false;
		}

		return (status.RemainingTime > 3 && TryGetStatusRow(status.StatusId, out var row) && row.Icon == 215503)
			|| status.Param > 2
			|| status.RemainingTime > 20
			|| OtherConfiguration.DangerousStatus?.Contains(status.StatusId) == true;
	}

	internal static bool LocksActions(in GameStatus status, float minRemaining)
	{
		if (status.StatusId == 0)
		{
			return false;
		}

		var remaining = status.RemainingTime == 0f ? float.MaxValue : status.RemainingTime;
		if (remaining <= minRemaining)
		{
			return false;
		}

		return (StatusID)status.StatusId is StatusID.OutOfTheAction_1113 or StatusID.OutOfTheAction_1284 or StatusID.OutOfTheAction_1462
			|| (TryGetStatusRow(status.StatusId, out var row) && row.LockActions);
	}

	/// <summary>
	/// Check if the status is invincible.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status is invincible, otherwise false.</returns>
	public static bool IsInvincible(this IStatus status)
	{
		return status != null && IsInvincibleStatus(status.StatusId);
	}

	/// <summary>
	/// Check if the status is a priority.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status is a priority, otherwise false.</returns>
	public static bool IsPriority(this IStatus status)
	{
		return status != null && OtherConfiguration.PriorityStatus?.Contains(status.StatusId) == true;
	}

	/// <summary>
	/// Check if the status needs to be dispelled immediately.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status needs to be dispelled, otherwise false.</returns>
	public static bool IsDangerous(this IStatus status)
	{
		if (status == null)
		{
			return false;
		}

		if (!status.CanDispel())
		{
			return false;
		}

		if (status.GameData.IsValid && status.GameData.Value.Icon == 215503 && status.RemainingTime > 3)
		{
			return true;
		}

		if (status.Param > 2)
		{
			return true;
		}

		if (status.RemainingTime > 20)
		{
			return true;
		}

		return OtherConfiguration.DangerousStatus?.Contains(status.StatusId) == true;
	}

	/// <summary>
	/// Check if the status can be dispelled.
	/// </summary>
	/// <param name="status">The status to check.</param>
	/// <returns>True if the status can be dispelled, otherwise false.</returns>
	public static bool CanDispel(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.CanDispel == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool CanStatusOff(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.CanStatusOff == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockActions(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.LockActions == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockMovement(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.LockMovement == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// 
	/// </summary>
	public static bool LockControl(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.LockControl == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}

	/// <summary>
	/// Unknown3 is used to determine if the status indicates a tether.
	/// </summary>
	public static bool IsTether(this IStatus status)
	{
		return status != null && status.GameData.IsValid && status.GameData.Value.Unknown3 == true && status.RemainingTime > 1 + DataCenter.DefaultGCDRemain;
	}
}
