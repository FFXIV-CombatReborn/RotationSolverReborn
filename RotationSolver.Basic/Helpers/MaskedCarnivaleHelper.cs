using Lumina.Excel.Sheets;
using System.Collections.Frozen;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Represents the resistance level of a Masked Carnivale mob to a particular damage type.
/// </summary>
public enum MobResistance : byte
{
	/// <summary>
	/// The mob has no special interaction with this damage type.
	/// </summary>
	Normal = 0,

	/// <summary>
	/// The mob is weak to this damage type; actions of this type deal increased damage or have
	/// a higher chance of applying their effect.
	/// </summary>
	Weak = 20,

	/// <summary>
	/// The mob is immune to this damage type; actions of this type deal no damage or have no effect.
	/// </summary>
	Immune = 21,
}

/// <summary>
/// Provides per-mob elemental/physical resistance and status-vulnerability data sourced from the
/// <c>AOZContentBriefingBNpc</c> game sheet, used in the Masked Carnivale (AOZ) content.
/// </summary>
public static class MaskedCarnivaleHelper
{
	private sealed record MobData(
		byte Fire, byte Ice, byte Wind, byte Earth, byte Thunder, byte Water,
		byte Slashing, byte Piercing, byte Blunt, byte Magic,
		bool SlowVuln, bool PetrificationVuln, bool ParalysisVuln,
		bool InterruptionVuln, bool BlindVuln, bool StunVuln,
		bool SleepVuln, bool BindVuln, bool HeavyVuln, bool FlatOrDeathVuln);

	private static FrozenDictionary<uint, MobData>? _mobDataByNameId;

	private static FrozenDictionary<uint, MobData> MobDataByNameId
	{
		get
		{
			_mobDataByNameId ??= BuildMobDataDictionary();
			return _mobDataByNameId;
		}
	}

	private static FrozenDictionary<uint, MobData> BuildMobDataDictionary()
	{
		var sheet = Service.GetSheet<AOZContentBriefingBNpc>();
		var dict = new Dictionary<uint, MobData>();

		foreach (var row in sheet)
		{
			var nameId = row.BNpcName.RowId;
			if (nameId == 0)
				continue;

			dict[nameId] = new MobData(
				row.Fire, row.Ice, row.Wind, row.Earth, row.Thunder, row.Water,
				row.Slashing, row.Piercing, row.Blunt, row.Magic,
				row.SlowVuln, row.PetrificationVuln, row.ParalysisVuln,
				row.InterruptionVuln, row.BlindVuln, row.StunVuln,
				row.SleepVuln, row.BindVuln, row.HeavyVuln, row.FlatOrDeathVuln);
		}

		return dict.ToFrozenDictionary();
	}

	/// <summary>
	/// Returns the <see cref="MobResistance"/> of the specified <paramref name="target"/> against the given
	/// <paramref name="aspect"/>, using data from the <c>AOZContentBriefingBNpc</c> sheet.
	/// Returns <see cref="MobResistance.Normal"/> when the target is not found in the database.
	/// </summary>
	public static MobResistance GetAspectResistance(IBattleChara target, Aspect aspect)
	{
		if (target == null)
			return MobResistance.Normal;
		return GetAspectResistance(target.NameId, aspect);
	}

	/// <summary>
	/// Returns the <see cref="MobResistance"/> of the mob with the given <paramref name="nameId"/> against the
	/// specified <paramref name="aspect"/>.
	/// </summary>
	public static MobResistance GetAspectResistance(uint nameId, Aspect aspect)
	{
		if (!MobDataByNameId.TryGetValue(nameId, out var data))
			return MobResistance.Normal;

		byte raw = aspect switch
		{
			Aspect.Fire => data.Fire,
			Aspect.Ice => data.Ice,
			Aspect.Wind => data.Wind,
			Aspect.Earth => data.Earth,
			Aspect.Lightning => data.Thunder,
			Aspect.Water => data.Water,
			Aspect.Slashing => data.Slashing,
			Aspect.Piercing => data.Piercing,
			Aspect.Blunt => data.Blunt,
			_ => 0,
		};

		return (MobResistance)raw;
	}

	/// <summary>
	/// Returns the <see cref="MobResistance"/> of the specified <paramref name="target"/> against the given
	/// <paramref name="attackType"/>.
	/// </summary>
	public static MobResistance GetAttackTypeResistance(IBattleChara target, Data.AttackType attackType)
	{
		if (target == null)
			return MobResistance.Normal;
		return GetAttackTypeResistance(target.NameId, attackType);
	}

	/// <summary>
	/// Returns the <see cref="MobResistance"/> of the mob with the given <paramref name="nameId"/> against the
	/// specified <paramref name="attackType"/>.
	/// </summary>
	public static MobResistance GetAttackTypeResistance(uint nameId, Data.AttackType attackType)
	{
		if (!MobDataByNameId.TryGetValue(nameId, out var data))
			return MobResistance.Normal;

		byte raw = attackType switch
		{
			Data.AttackType.Slashing => data.Slashing,
			Data.AttackType.Piercing => data.Piercing,
			Data.AttackType.Blunt => data.Blunt,
			Data.AttackType.Magic => data.Magic,
			_ => 0,
		};

		return (MobResistance)raw;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the target mob has no aspect or attack-type resistances at all,
	/// meaning every elemental and physical resistance value is <see cref="MobResistance.Normal"/>.
	/// Also returns <see langword="true"/> when the target is not found in the database.
	/// </summary>
	public static bool HasNoResistancesOrImmunities(IBattleChara target)
	{
		if (target == null)
			return true;
		if (!MobDataByNameId.TryGetValue(target.NameId, out var data))
			return true;

		return data.Fire == 0 && data.Ice == 0 && data.Wind == 0 &&
			   data.Earth == 0 && data.Thunder == 0 && data.Water == 0 &&
			   data.Slashing == 0 && data.Piercing == 0 && data.Blunt == 0 &&
			   data.Magic == 0;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is weak to the specified <paramref name="aspect"/>,
	/// meaning actions of that aspect will deal increased damage or be more effective.
	/// </summary>
	public static bool IsWeakToAspect(IBattleChara target, Aspect aspect)
		=> GetAspectResistance(target, aspect) == MobResistance.Weak;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is immune to the specified <paramref name="aspect"/>,
	/// meaning actions of that aspect will deal no damage and should be avoided.
	/// </summary>
	public static bool IsImmuneToAspect(IBattleChara target, Aspect aspect)
		=> GetAspectResistance(target, aspect) == MobResistance.Immune;

	/// <summary>
	/// Returns <see langword="true"/> if <b>all</b> of the action's aspects are permitted against the target,
	/// i.e. none of the aspects are blocked by the mob's immunity.
	/// </summary>
	/// <remarks>
	/// Use this as an <see cref="Actions.ActionSetting.ActionCheck"/> guard when inside the Masked Carnivale
	/// to avoid wasting a blocked action on an immune mob.
	/// </remarks>
	public static bool IsActionAspectAllowed(IBattleChara target, IBaseAction action)
	{
		if (target == null || action == null)
			return true;

		foreach (var aspect in action.Info.Aspects)
		{
			if (IsImmuneToAspect(target, aspect))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Returns <see langword="true"/> if <b>at least one</b> of the action's aspects is a weakness of the
	/// target mob, meaning the action will be especially effective.
	/// </summary>
	public static bool IsActionAspectWeak(IBattleChara target, IBaseAction action)
	{
		if (target == null || action == null)
			return false;

		foreach (var aspect in action.Info.Aspects)
		{
			if (IsWeakToAspect(target, aspect))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Slow</b> in the Masked Carnivale.
	/// </summary>
	public static bool IsVulnerableToSlow(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.SlowVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Petrification</b>.
	/// </summary>
	public static bool IsVulnerableToPetrification(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.PetrificationVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Paralysis</b>.
	/// </summary>
	public static bool IsVulnerableToParalysis(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.ParalysisVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob can be <b>Interrupted</b>.
	/// </summary>
	public static bool IsVulnerableToInterruption(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.InterruptionVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Blind</b>.
	/// </summary>
	public static bool IsVulnerableToBlind(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.BlindVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Stun</b>.
	/// </summary>
	public static bool IsVulnerableToStun(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.StunVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Sleep</b>.
	/// </summary>
	public static bool IsVulnerableToSleep(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.SleepVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Bind</b>.
	/// </summary>
	public static bool IsVulnerableToBind(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.BindVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to <b>Heavy</b>.
	/// </summary>
	public static bool IsVulnerableToHeavy(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.HeavyVuln;

	/// <summary>
	/// Returns <see langword="true"/> if the target mob is vulnerable to flat-damage or instant-death
	/// effects (e.g. <see cref="ActionID.TailScrewPvE"/>, <see cref="ActionID.MissilePvE"/>,
	/// <see cref="ActionID.Level5DeathPvE"/>).
	/// </summary>
	public static bool IsVulnerableToFlatOrDeath(IBattleChara target)
		=> target != null && MobDataByNameId.TryGetValue(target.NameId, out var d) && d.FlatOrDeathVuln;
}
