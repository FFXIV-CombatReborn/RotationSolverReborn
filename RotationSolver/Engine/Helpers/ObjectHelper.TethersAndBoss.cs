using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using Lumina.Excel.Sheets;
using System.Collections.Concurrent;

namespace RotationSolver.Engine.Helpers;

public static partial class ObjectHelper
{
	private static MethodInfo[]? _tetherProviderCandidates;

	/// <summary>
	/// Attempts to retrieve all tethers currently present using the ECommons TetherInfo API via reflection.
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetAllTethers()
	{
		try
		{
			if (_tetherProviderCandidates == null)
			{
				var methods = typeof(TetherInfo).GetMethods(BindingFlags.Public | BindingFlags.Static);
				List<MethodInfo> matching = [];
				foreach (var m in methods)
				{
					if (m.ReturnType == typeof(TetherInfo[]) || typeof(System.Collections.IEnumerable).IsAssignableFrom(m.ReturnType))
					{
						matching.Add(m);
					}
				}
				_tetherProviderCandidates = [.. matching];
			}
			var candidates = _tetherProviderCandidates;

			foreach (var m in candidates)
			{
				var res = m.Invoke(null, null);
				if (res == null)
				{
					continue;
				}

				if (res is TetherInfo[] arr)
				{
					return arr;
				}

				if (res is System.Collections.IEnumerable ie)
				{
					var list = new List<TetherInfo>();
					foreach (var o in ie)
					{
						if (o is TetherInfo ti)
						{
							list.Add(ti);
						}
					}
					return list;
				}
			}
		}
		catch
		{

		}

		return [];
	}

	/// <summary>
	/// Returns tethers where the object is either source or target.
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetTethersFor(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == id || tgt == id))
			{
				result.Add(t);
			}
		}
		return result;
	}

	/// <summary>
	/// Returns outgoing tethers (where object is source).
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetOutgoingTethers(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out _) && src == id)
			{
				result.Add(t);
			}
		}
		return result;
	}

	/// <summary>
	/// Returns incoming tethers (where object is target).
	/// </summary>
	public static IReadOnlyList<TetherInfo> GetIncomingTethers(this IGameObject obj)
	{
		if (obj == null)
		{
			return [];
		}

		var all = GetAllTethers();
		var id = obj.GameObjectId;
		var result = new List<TetherInfo>();
		foreach (var t in all)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out _, out var tgt) && tgt == id)
			{
				result.Add(t);
			}
		}
		return result;
	}

	private static readonly string[] TetherIdNames = ["TetherId", "Id", "Tether"];
	private static readonly string[] TetherSourceNames = ["SourceObjectId", "SourceId", "Source", "SourceActorId"];
	private static readonly string[] TetherTargetNames = ["TargetObjectId", "TargetId", "Target", "TargetActorId"];
	private static MemberInfo[]? _tetherIdMembers;
	private static MemberInfo[]? _tetherSourceMembers;
	private static MemberInfo[]? _tetherTargetMembers;

	private static bool ExtractTetherId(TetherInfo t, out uint tetherId, out ulong sourceObjectId, out ulong targetObjectId)
	{
		tetherId = 0;
		sourceObjectId = 0;
		targetObjectId = 0;
		try
		{
			var tType = typeof(TetherInfo);
			_tetherIdMembers ??= ResolveTetherMembers(tType, TetherIdNames);
			_tetherSourceMembers ??= ResolveTetherMembers(tType, TetherSourceNames);
			_tetherTargetMembers ??= ResolveTetherMembers(tType, TetherTargetNames);

			object? val;

			val = TryGetMemberValue(t, _tetherIdMembers);
			if (val != null)
			{
				if (val is uint ui)
				{
					tetherId = ui;
				}
				else if (val is int i)
				{
					tetherId = (uint)i;
				}
				else if (uint.TryParse(val.ToString(), out var parsed))
				{
					tetherId = parsed;
				}
			}

			val = TryGetMemberValue(t, _tetherSourceMembers);
			if (val != null)
			{
				if (val is ulong ul)
				{
					sourceObjectId = ul;
				}
				else if (val is uint u)
				{
					sourceObjectId = u;
				}
				else if (ulong.TryParse(val.ToString(), out var pul))
				{
					sourceObjectId = pul;
				}
			}

			val = TryGetMemberValue(t, _tetherTargetMembers);
			if (val != null)
			{
				if (val is ulong ul2)
				{
					targetObjectId = ul2;
				}
				else if (val is uint u2)
				{
					targetObjectId = u2;
				}
				else if (ulong.TryParse(val.ToString(), out var pul2))
				{
					targetObjectId = pul2;
				}
			}

			return sourceObjectId != 0 || targetObjectId != 0 || tetherId != 0;
		}
		catch
		{
			return false;
		}
	}

	private static MemberInfo[] ResolveTetherMembers(Type tType, string[] names)
	{
		List<MemberInfo> members = [];
		foreach (var n in names)
		{
			var f = tType.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (f != null)
			{
				members.Add(f);
				continue;
			}
			var p = tType.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (p != null)
			{
				members.Add(p);
			}
		}
		return [.. members];
	}

	private static object? TryGetMemberValue(object instance, MemberInfo[] members)
	{
		foreach (var m in members)
		{
			var v = m is FieldInfo f ? f.GetValue(instance) : ((PropertyInfo)m).GetValue(instance);
			if (v != null)
			{
				return v;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns true if the specified battleChara is tethered to the player.
	/// </summary>
	public static bool IsTetheredToPlayer(this IBattleChara battleChara)
	{
		if (battleChara == null || Player.Object == null)
		{
			return false;
		}

		var playerId = Player.Object.GameObjectId;
		var tethers = GetTethersFor(battleChara);
		foreach (var t in tethers)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == playerId || tgt == playerId))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns true if the specified checkedtarget is tethered to checkedtarget.
	/// </summary>
	public static bool IsTetheredToSpecificTarget(this IBattleChara battleChara, IBattleChara checkedtarget)
	{
		if (battleChara == null || checkedtarget == null)
		{
			return false;
		}

		var memberId = checkedtarget.GameObjectId;
		var tethers = GetTethersFor(battleChara);
		foreach (var t in tethers)
		{
			if (t == null)
			{
				continue;
			}

			if (ExtractTetherId(t, out _, out var src, out var tgt) && (src == memberId || tgt == memberId))
			{
				return true;
			}
		}
		return false;
	}

	private static readonly ConcurrentDictionary<uint, bool> _effectRangeCheck = [];

	internal static bool CanInterrupt(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		unsafe
		{
			if (battleChara.Struct() == null)
			{
				return false;
			}
		}

		var baseCheck = battleChara.IsCasting && battleChara.IsCastInterruptible && battleChara.TotalCastTime >= 2;
		if (!baseCheck)
		{
			return false;
		}

		if (!Service.Config.InterruptibleMoreCheck)
		{
			return false;
		}

		if (_effectRangeCheck.TryGetValue(battleChara.CastActionId, out var check))
		{
			return check;
		}

		var act = Service.GetSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(battleChara.CastActionId) ?? default;
		return act.RowId == 0
			? (_effectRangeCheck[battleChara.CastActionId] = false)
			: (CastType)act.CastType == CastType.Cone || (CastType)act.CastType == CastType.Donut || (act.EffectRange > 0 && act.EffectRange < 8)
			? (_effectRangeCheck[battleChara.CastActionId] = false)
			: (_effectRangeCheck[battleChara.CastActionId] = true);
	}

	internal static bool IsDummy(this IBattleChara battleChara)
	{
		return battleChara?.NameId == 541;
	}

	/// <summary>
	/// Checks if the target is immune due to any special mob/boss.
	/// </summary>
	/// <param name="battleChara">The object to check.</param>
	/// <returns>True if the target is immune due to any special mechanic; otherwise, false.</returns>
	public static bool IsSpecialImmune(this IBattleChara battleChara)
	{
		return EncounterImmunity.IsImmune(battleChara);
	}

	/// <summary>
	/// Is target a boss depends on the ttk.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsBossFromTTK(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Service.Config.DummyBoss && battleChara.IsDummy())
		{
			return true;
		}

		return battleChara.GetTTK(true) >= Service.Config.BossTimeToKill;
	}

	/// <summary>
	/// Is target a boss depends on the icon.
	/// </summary>
	/// <param name="battleChara">the object.</param>
	/// <returns></returns>
	public static bool IsBossFromIcon(this IBattleChara battleChara)
	{
		if (battleChara == null)
		{
			return false;
		}

		if (Service.Config.DummyBoss && battleChara.IsDummy())
		{
			return true;
		}

		return Svc.Data.GetExcelSheet<BNpcBase>().TryGetRow(battleChara.BaseId, out var dataRow) && dataRow.Rank is 2 or 6;
	}

	/// <summary>
	/// Returns object's calculated shield value.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static uint GetObjectShield(this IBattleChara battleChara)
	{
		return battleChara is ICharacter character && character.MaxHp > 0 && character.ShieldPercentage > 0
			? (uint)((ulong)character.MaxHp * character.ShieldPercentage / 100)
			: 0;
	}

	/// <summary>
	/// Returns object's calculated effective HP.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static uint GetEffectiveHp(this IBattleChara battleChara)
	{
		return battleChara is ICharacter
			? battleChara.CurrentHp + GetObjectShield(battleChara)
			: 0;
	}

	/// <summary>
	/// Returns object's calculated effective HP as a percentage of Max HP, rounded down to the nearest whole number.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns>Effective HP percentage (0-100). Returns 0 if MaxHp is 0 or not an ICharacter.</returns>
	public static int GetEffectiveHpPercent(this IBattleChara battleChara)
	{
		if (battleChara is not ICharacter character || character.MaxHp == 0)
		{
			return 0;
		}

		var effectiveHp = character.CurrentHp + GetObjectShield(battleChara);
		return (int)Math.Floor((float)effectiveHp / character.MaxHp * 100f);
	}

	/// <summary>
	/// Is object dying.
	/// </summary>
	/// <param name="battleChara"></param>
	/// <returns></returns>
	public static bool IsDying(this IBattleChara battleChara)
	{
		return battleChara != null && !battleChara.IsDummy() && (battleChara.GetTTK() <= Service.Config.DyingTimeToKill || battleChara.GetHealthRatio() < Service.Config.IsDyingConfig);
	}

	internal static unsafe bool InCombat(this IBattleChara battleChara)
	{
		return battleChara != null && battleChara.Struct() != null && battleChara.Struct()->Character.InCombat;
	}
}
