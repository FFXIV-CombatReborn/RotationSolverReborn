using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System.Collections.Concurrent;

namespace RotationSolver.Engine.Helpers;

public static partial class ObjectHelper
{
	private static readonly Dictionary<ulong, Vector3> LastPositions = [];
	internal static bool IsTargetMoving(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Svc.Condition[ConditionFlag.BetweenAreas] || LastPositions.Count > 4096)
		{
			LastPositions.Clear();
		}

		var id = battleChara.GameObjectId;
		var currentPos = battleChara.Position;
		if (LastPositions.TryGetValue(id, out var lastPos))
		{
			var isMoving = Vector3.Distance(currentPos, lastPos) > 0.01f;
			LastPositions[id] = currentPos;
			return isMoving;
		}
		else
		{
			LastPositions[id] = currentPos;
			return false;
		}
	}

	internal static float GetTTK(this IBattleChara battleChara, bool wholeTime = false)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		if (battleChara.IsDummy())
		{
			return 999.99f;
		}

		var rate = TimeToKillTracker.GetRate(battleChara.GameObjectId);
		if (float.IsNaN(rate))
		{
			return float.NaN;
		}

		return wholeTime ? 1f / rate : battleChara.GetHealthRatio() / rate;
	}

	/// <summary>
	/// Seconds until the next DoT tick on the target, or <see cref="float.NaN"/> until one of its ticks has been seen.
	/// </summary>
	/// <param name="battleChara">The target.</param>
	public static float TimeToNextDotTick(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		var now = Environment.TickCount64;
		return DotTickTracker.TryGetNextTick(battleChara.GameObjectId, now, out var next) ? (next - now) / 1000f : float.NaN;
	}

	/// <summary>
	/// DoT ticks on the target between two times in seconds from now. Exact once one of its ticks has been seen, otherwise one per 3 seconds on average.
	/// </summary>
	/// <param name="battleChara">The target.</param>
	/// <param name="fromSeconds">Start of the window; a tick exactly at this time is not counted.</param>
	/// <param name="toSeconds">End of the window; a tick exactly at this time is counted.</param>
	public static float DotTicksBetween(this IBattleChara battleChara, float fromSeconds, float toSeconds)
	{
		if (battleChara == null || !float.IsFinite(fromSeconds) || !float.IsFinite(toSeconds))
		{
			return float.NaN;
		}

		var now = Environment.TickCount64;
		return DotTickTracker.CountTicks(battleChara.GameObjectId, now + (long)(fromSeconds * 1000), now + (long)(toSeconds * 1000));
	}

	private static readonly ConcurrentDictionary<ulong, DateTime> _aliveStartTimes = [];
	private static readonly ConcurrentDictionary<ulong, DateTime> _deadStartTimes = [];

	internal static float TimeAlive(this IBattleChara? battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		if (battleChara.IsDead || Svc.Condition[ConditionFlag.BetweenAreas])
		{
			_ = _aliveStartTimes.TryRemove(battleChara.GameObjectId, out _);
			return 0;
		}

		return SecondsSinceFirstSeen(_aliveStartTimes, battleChara.GameObjectId);
	}

	internal static float TimeDead(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.NaN;
		}

		if (!battleChara.IsDead)
		{
			_ = _deadStartTimes.TryRemove(battleChara.GameObjectId, out _);
			return 0;
		}

		return SecondsSinceFirstSeen(_deadStartTimes, battleChara.GameObjectId);
	}

	private static float SecondsSinceFirstSeen(ConcurrentDictionary<ulong, DateTime> startTimes, ulong id)
	{
		var start = startTimes.GetOrAdd(id, static _ => DateTime.Now);
		return MathF.Min(30, (float)(DateTime.Now - start).TotalSeconds);
	}

	internal static bool IsEngagedByPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		var now = DateTime.Now;
		foreach ((var id, var time) in DataCenter.AttackedTargets)
		{
			if (id == battleChara.GameObjectId)
			{
				return now - time >= TimeSpan.FromSeconds(1);
			}
		}
		return false;
	}

	private struct LosCacheEntry
	{
		public Vector3 PlayerPos;
		public Vector3 TargetPos;
		public long ExpiresAt;
		public bool Visible;
	}
	private static readonly ConcurrentDictionary<ulong, LosCacheEntry> _losCache = [];
	private const float LosPosEpsilonSq = 0.04f;
	private const long LosTtlMs = 33;

	private static void MaybeResetLosCache()
	{
		if (Svc.Condition[ConditionFlag.BetweenAreas] || _losCache.Count > 4096)
		{
			_losCache.Clear();
		}
	}

	internal static unsafe bool CanSeeFrom(this IBattleChara battleChara, Vector3 playerEyePos, float targetYOffset = 2.0f)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var targetStruct = battleChara.Struct();
		if (targetStruct == null)
		{
			return false;
		}

		MaybeResetLosCache();

		var targetPos = battleChara.Position;
		targetPos.Y += targetYOffset;
		var id = battleChara.GameObjectId;
		var now = Environment.TickCount64;

		if (_losCache.TryGetValue(id, out var entry))
		{
			if (now <= entry.ExpiresAt &&
				Vector3.DistanceSquared(playerEyePos, entry.PlayerPos) <= LosPosEpsilonSq &&
				Vector3.DistanceSquared(targetPos, entry.TargetPos) <= LosPosEpsilonSq)
			{
				return entry.Visible;
			}
		}

		var offset = targetPos - playerEyePos;
		var maxDist = offset.Length();
		if (maxDist < 0.01f)
		{
			return true;
		}

		var direction = offset / maxDist;

		RaycastHit hit;
		var materialFilter = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
		var module = Framework.Instance()->BGCollisionModule;
		var blocked = module->RaycastMaterialFilter(&hit, &playerEyePos, &direction, maxDist, 1, materialFilter);

		var visible = !blocked;
		_losCache[id] = new LosCacheEntry
		{
			PlayerPos = playerEyePos,
			TargetPos = targetPos,
			ExpiresAt = now + LosTtlMs,
			Visible = visible
		};
		return visible;
	}

	internal static bool CanSee(this IBattleChara battleChara, float playerYOffset = 2.0f, float targetYOffset = 2.0f)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var playerPos = Player.Object.Position;
		playerPos.Y += playerYOffset;
		return CanSeeFrom(battleChara, playerPos, targetYOffset);
	}

	/// <summary>
	/// Get the Player's current MP percentage.
	/// </summary>
	/// <returns></returns>
	public static float GetPlayerMPRatio()
	{
		if (Player.Object == null)
		{
			return 0;
		}

		if (Player.Object.MaxMp == 0)
		{
			return 0;
		}

		return (float)Player.Object.CurrentMp / Player.Object.MaxMp;
	}

	/// <summary>
	/// Get the Player's current HP percentage.
	/// </summary>
	/// <returns></returns>
	public static float GetPlayerHealthRatio()
	{
		if (Player.Object == null)
		{
			return 0;
		}

		if (DataCenter.RefinedHP.TryGetValue(Player.Object.GameObjectId, out var hp))
		{
			return hp;
		}

		if (Player.Object.MaxHp == 0)
		{
			return 0;
		}

		return (float)Player.Object.CurrentHp / Player.Object.MaxHp;
	}

	/// <summary>
	/// Get the <paramref name="battleChara"/>'s current HP percentage.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float GetHealthRatio(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return 0;
		}

		if (!battleChara.IsValid())
		{
			return 0;
		}

		var id = battleChara.GameObjectId;
		var now = Environment.TickCount64;
		if (_healthRatioCache.TryGetValue(id, out var cached) && now - cached.Tick < HealthRatioTtlMs)
		{
			return cached.Ratio;
		}

		if (_healthRatioCache.Count > 512)
		{
			_healthRatioCache.Clear();
		}

		var ratio = ComputeHealthRatio(battleChara);
		_healthRatioCache[id] = (now, ratio);
		return ratio;
	}

	private const long HealthRatioTtlMs = 15;
	private static readonly Dictionary<ulong, (long Tick, float Ratio)> _healthRatioCache = [];

	private static float ComputeHealthRatio(IBattleChara battleChara)
	{
		if (battleChara.IsParty() && battleChara.DoomNeedHealing())
		{
			return 0.01f;
		}

		if (DataCenter.RefinedHP.TryGetValue(battleChara.GameObjectId, out var hp))
		{
			return hp;
		}

		try
		{
			return battleChara.MaxHp == 0 ? 0 : (float)battleChara.CurrentHp / battleChara.MaxHp;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	/// <summary>
	/// Determines the positional relationship of the player relative to the enemy.
	/// </summary>
	/// <param name="enemy">The enemy game object.</param>
	/// <returns>
	/// An <see cref="EnemyPositional"/> value indicating whether the player is in front, at the rear, or on the flank of the enemy.
	/// </returns>
	public static EnemyPositional FindEnemyPositional(this IBattleChara enemy)
	{
		if (enemy == null || !enemy.IsValid() || enemy.Address == nint.Zero || Player.Object == null)
		{
			return EnemyPositional.None;
		}

		var faceVec = enemy.GetFaceVector();

		var offset = Player.Object.Position - enemy.Position;
		var dir = new Vector2(offset.X, offset.Z);
		if (dir.LengthSquared() < 0.0001f)
		{
			return EnemyPositional.None;
		}

		var dotProduct = Vector2.Dot(Vector2.Normalize(dir), new Vector2(faceVec.X, faceVec.Z));

		const float sectorCos = 0.70710677f;

		if (dotProduct > sectorCos)
		{
			return EnemyPositional.Front;
		}
		else if (dotProduct < -sectorCos)
		{
			return EnemyPositional.Rear;
		}

		return EnemyPositional.Flank;
	}

	internal static Vector3 GetFaceVector(this IBattleChara battleChara)
	{
		if (battleChara == null || !battleChara.IsValid() || battleChara.Address == nint.Zero)
		{
			return Vector3.Zero;
		}

		var rotation = battleChara.Rotation;
		return new Vector3((float)Math.Sin(rotation), 0, (float)Math.Cos(rotation));
	}

	internal static double AngleTo(this Vector3 vec1, Vector3 vec2)
	{
		double lengthProduct = vec1.Length() * vec2.Length();
		if (lengthProduct == 0)
		{
			return 0;
		}

		double dotProduct = Vector3.Dot(vec1, vec2);
		return Math.Acos(dotProduct / lengthProduct);
	}

	/// <summary>
	/// The distance from <paramref name="battleChara"/> to the player
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float DistanceToPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null || Player.Object == null)
		{
			return float.MaxValue;
		}

		var playerPos = Player.Object.Position;
		var targetPos = battleChara.Position;

		var dy = MathF.Abs(targetPos.Y - playerPos.Y);
		if (dy > 30f)
		{
			return dy;
		}

		var dx = targetPos.X - playerPos.X;
		var dz = targetPos.Z - playerPos.Z;
		var distance = MathF.Sqrt(dx * dx + dz * dz) - (Player.Object.HitboxRadius + battleChara.HitboxRadius);
		return MathF.Max(0f, distance);
	}

	/// <summary>
	/// The distance from <paramref name="battleChara"/> to the player's pet
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static float DistanceToPet(IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return float.MaxValue;
		}
		var pet = DataCenter.GetPet();
		if (pet == null)
		{
			return float.MaxValue;
		}

		var pdx = battleChara.Position.X - pet.Position.X;
		var pdz = battleChara.Position.Z - pet.Position.Z;
		return MathF.Max(0f, MathF.Sqrt(pdx * pdx + pdz * pdz) - battleChara.HitboxRadius);
	}

}
