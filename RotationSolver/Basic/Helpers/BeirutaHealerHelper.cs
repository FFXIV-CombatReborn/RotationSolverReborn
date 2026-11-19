namespace RotationSolver.Basic.Helpers;

#region Heal Model

internal static class BeirutaHealModel
{
	public const float HpPerPotency = 84f;
	public const float AlwaysInjuredHp = 0.5f;
	public const float HotCreditTicks = 3f;
	public const float WhisperingDawnTickPotency = 72f;
	public const float PhysisIiBoostedTickPotency = 143f;
	public const float KeracholeTickPotency = 100f;

	private const float ReferenceMaxHp = 226813f;
	private const float HotTickSeconds = 3f;
	private const float EudaimoniaPotency = 180f;

	private static readonly (StatusID[] Status, float TickPotency)[] PartyHots =
	[
		([StatusID.PhysisIi], PhysisIiBoostedTickPotency),
		([StatusID.Physis], 100f),
		([StatusID.Kerakeia], KeracholeTickPotency),
		([StatusID.WhisperingDawn, StatusID.AngelsWhisper], WhisperingDawnTickPotency),
		([StatusID.SacredSoil_1944], 100f),
		([StatusID.MedicaIii, StatusID.MedicaIii_3986], 175f),
		([StatusID.MedicaIi], 150f),
		([StatusID.Asylum, StatusID.Asylum_1911], 100f),
		([StatusID.Regen, StatusID.Regen_897, StatusID.Regen_1330], 250f),
		([StatusID.AspectedBenefic], 250f),
		([StatusID.HeliosConjunction], 175f),
		([StatusID.AspectedHelios], 150f),
		([StatusID.Opposition], 100f),
		([StatusID.WheelOfFortune], 100f),
	];

	public static float EffectiveHpRatio(IBattleChara member, bool wardMode, float landingPotency = 0f)
	{
		if (member.MaxHp == 0)
		{
			return 1f;
		}

		var hp = (float)member.CurrentHp;
		if (!wardMode)
		{
			hp += (HotCreditPotency(member) + landingPotency) * HpPerPotency;
		}

		return MathF.Min(1f, hp / member.MaxHp);
	}

	public static float FullUseHp(float potency, float healBoost = 1f) =>
		1f - PotencyToRatio(potency * healBoost);

	public static float RoundToFive(float ratio) => MathF.Round(ratio * 20f) / 20f;

	public static float Threshold(
		float potency,
		float healBoost,
		bool wardMode,
		bool useCustom,
		float customWardOff,
		IEnumerable<IBattleChara>? party,
		float wardOffCap = 1f)
	{
		var fullUse = FullUseHp(potency, healBoost);
		if (wardMode)
		{
			return RoundToFive(fullUse);
		}

		return useCustom
			? customWardOff
			: MathF.Min(wardOffCap, RoundToFive(fullUse - PotencyToRatio(PartnerGcdPotency(party))));
	}

	private static float HotCreditPotency(IBattleChara member)
	{
		var potency = 0f;

		foreach (var (status, tickPotency) in PartyHots)
		{
			var remain = member.StatusTime(false, status);
			if (remain > 0f)
			{
				potency += MathF.Min(HotCreditTicks, MathF.Floor(remain / HotTickSeconds)) * tickPotency;
			}
		}

		if (member.HasStatus(false, StatusID.Eudaimonia))
		{
			potency += EudaimoniaPotency * HotCreditTicks;
		}

		return potency;
	}

	private static float PotencyToRatio(float potency) => potency * HpPerPotency / ReferenceMaxHp;

	private static float PartnerGcdPotency(IEnumerable<IBattleChara>? party)
	{
		if (party == null)
		{
			return 0f;
		}

		var selfId = ECommons.GameHelpers.Player.Object?.GameObjectId;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.GameObjectId == selfId ||
				!member.IsJobCategory(JobRole.Healer))
			{
				continue;
			}

			if (member.IsJobs(ECommons.ExcelServices.Job.WHM, ECommons.ExcelServices.Job.AST))
			{
				return 250f;
			}

			return member.IsJobs(ECommons.ExcelServices.Job.SCH) ? 200f : 100f;
		}

		return 0f;
	}

	public const float NonTankTopUpHp = 0.9f;

	public static IBattleChara? NonTankTopUpTarget(IEnumerable<IBattleChara>? party, float range, Func<IBattleChara, float> hpRatio, Func<IBattleChara, bool> blocked, int aoeCount)
	{
		if (party == null)
		{
			return null;
		}

		IBattleChara? best = null;
		var bestHp = 1f;
		var count = 0;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.IsJobCategory(JobRole.Tank) ||
				member.DistanceToPlayer() > range ||
				(DataCenter.IsInDMU && member.HasStatus(false, StatusID.Accretion)) ||
				blocked(member))
			{
				continue;
			}

			var hp = hpRatio(member);
			if (hp >= NonTankTopUpHp)
			{
				continue;
			}

			count++;
			if (best == null || hp < bestHp)
			{
				best = member;
				bestHp = hp;
			}
		}

		return count < aoeCount ? best : null;
	}
}

#endregion

#region Heal Window

internal static class BeirutaHealWindow
{
	public const float OgcdSeconds = 5f;
	public const double GcdSeconds = 3d;

	public static bool OgcdJustUsed(params ReadOnlySpan<IBaseAction> actions)
	{
		foreach (var action in actions)
		{
			if (!action.EnoughLevel || !action.Cooldown.IsCoolingDown)
			{
				continue;
			}

			var oneCharge = action.Cooldown.RecastTimeOneChargeRaw;
			var elapsed = action.Cooldown.RecastTimeElapsedRaw;
			if (oneCharge > 0f)
			{
				elapsed %= oneCharge;
			}

			if (elapsed < OgcdSeconds)
			{
				return true;
			}
		}

		return false;
	}

	public static bool GcdJustUsed(params ReadOnlySpan<ActionID> ids) =>
		IsCasting(ids) || UsedWithin(GcdSeconds, ids);

	public static bool UsedSince(DateTime since, params ReadOnlySpan<ActionID> ids)
	{
		if (IsCasting(ids))
		{
			return true;
		}

		foreach (var rec in DataCenter.RecordActions)
		{
			if (rec.UsedTime < since)
			{
				break;
			}

			if (Contains(ids, rec.Action.RowId))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsCasting(ReadOnlySpan<ActionID> ids) =>
		ECommons.GameHelpers.Player.Object is { IsCasting: true } player && Contains(ids, player.CastActionId);

	private static bool UsedWithin(double seconds, ReadOnlySpan<ActionID> ids)
	{
		var now = DateTime.Now;
		foreach (var rec in DataCenter.RecordActions)
		{
			if ((now - rec.UsedTime).TotalSeconds >= seconds)
			{
				break;
			}

			if (Contains(ids, rec.Action.RowId))
			{
				return true;
			}
		}

		return false;
	}

	private static bool Contains(ReadOnlySpan<ActionID> ids, uint id)
	{
		foreach (var item in ids)
		{
			if ((uint)item == id)
			{
				return true;
			}
		}

		return false;
	}
}

#endregion

#region Ward Mode

internal static class BeirutaWard
{
	public const int UnshieldedPartyCount = 4;

	private const string WardModeConfigName = "WardMode";

	public static void Track(ICustomRotation rotation, bool wardMode, ref bool lastWardMode)
	{
		if (wardMode && ECommons.GameHelpers.Player.Object is { IsDead: true })
		{
			SetWardMode(rotation, false);
			wardMode = false;
		}

		if (wardMode == lastWardMode)
		{
			return;
		}

		lastWardMode = wardMode;
		ECommons.DalamudServices.Svc.Toasts.ShowQuest($"Ward Mode {(wardMode ? "ON" : "OFF")}");
	}

	public static void SetWardMode(ICustomRotation rotation, bool value)
	{
		foreach (var config in rotation.Configs.Configs)
		{
			if (config.Name == WardModeConfigName)
			{
				config.Value = value.ToString();
				return;
			}
		}
	}

	public static int CountMissing(IEnumerable<IBattleChara>? party, float range, Func<IBattleChara, bool> hasEffect)
	{
		if (party == null)
		{
			return 0;
		}

		var count = 0;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.DistanceToPlayer() > range ||
				hasEffect(member))
			{
				continue;
			}

			count++;
		}

		return count;
	}

	public static IBattleChara? StanceTank(IEnumerable<IBattleChara>? party, float range)
	{
		if (party == null)
		{
			return null;
		}

		IBattleChara? fallback = null;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				!member.IsJobCategory(JobRole.Tank) ||
				member.DistanceToPlayer() > range)
			{
				continue;
			}

			if (member.HasStatus(false, StatusHelper.TankStanceStatus))
			{
				return member;
			}

			fallback ??= member;
		}

		return fallback;
	}
}

#endregion

#region Raidwide Tracking

internal sealed class BeirutaRaidwide
{
	private const float Range = 30f;
	private const long WindowMs = 10_000;

	private static readonly StatusID[] PrepStatus =
	[
		StatusID.EukrasianPrognosis,
		StatusID.Kerachole,
		StatusID.Holos,
		StatusID.Holosakos,
		StatusID.Panhaima,
		StatusID.Panhaimatinon,
		StatusID.Galvanize,
		StatusID.SeraphicVeil,
		StatusID.SacredSoil,
		StatusID.SacredSoil_299,
		StatusID.FeyIllumination,
		StatusID.CollectiveUnconscious_849,
		StatusID.NeutralSect_1921,
		StatusID.SunSign,
	];

	private long _sinceMs;
	private bool _wasActive;

	public bool Coming =>
		DataCenter.MergedStatus.HasFlag(AutoStatus.DefenseArea) ||
		(_sinceMs != 0 && Environment.TickCount64 - _sinceMs <= WindowMs);

	public void Track(bool inCombat, IEnumerable<IBattleChara>? party)
	{
		var active = inCombat && PrepStatusesUp(party);
		if (active && !_wasActive)
		{
			_sinceMs = Environment.TickCount64;
		}
		else if (!active)
		{
			_sinceMs = 0;
		}

		_wasActive = active;
	}

	private static bool PrepStatusesUp(IEnumerable<IBattleChara>? party)
	{
		if (party == null)
		{
			return false;
		}

		var nonTanks = 0;
		var prepared = 0;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				member.IsJobCategory(JobRole.Tank) ||
				member.DistanceToPlayer() > Range)
			{
				continue;
			}

			nonTanks++;
			if (member.HasStatus(false, PrepStatus))
			{
				prepared++;
			}
		}

		return nonTanks > 0 && prepared >= Math.Max(2, (nonTanks + 1) / 2);
	}
}

#endregion

#region DMU Accretion

internal static class BeirutaAccretion
{
	private const float HealRange = 30f;
	private const float TopUpSafeHp = 0.3f;
	private const int TopUpSafeCount = 3;

	private static bool PartySafeForTopUp(IEnumerable<IBattleChara> party, IBattleChara target)
	{
		var count = 0;
		foreach (var member in party)
		{
			if (member != null &&
				member.GameObjectId != target.GameObjectId &&
				!member.IsDead &&
				!member.IsJobCategory(JobRole.Tank) &&
				member.GetHealthRatio() > TopUpSafeHp)
			{
				count++;
			}
		}

		return count >= TopUpSafeCount;
	}

	public static IBattleChara? FindTarget(IEnumerable<IBattleChara>? party, bool requireSafeParty = true)
	{
		if (party == null || !DataCenter.IsInDMU || ReadState(party).Settling)
		{
			return null;
		}

		var target = FindLowest(party, static m => m.IsJobCategory(JobRole.Healer));
		if (target == null)
		{
			if (AnyMember(party, static m => m.IsJobCategory(JobRole.Healer) && m.HasStatus(false, StatusID.Accretion)) ||
				AnyMember(party, static m => m.HasStatus(false, StatusID.EarthResistanceDownIi_3372)))
			{
				return null;
			}

			target = FindLowest(party, static m => !m.IsJobCategory(JobRole.Healer) && !m.IsJobCategory(JobRole.Tank));
		}

		return target != null && (!requireSafeParty || PartySafeForTopUp(party, target)) ? target : null;
	}

	private static IBattleChara? FindLowest(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> role)
	{
		IBattleChara? best = null;
		foreach (var member in party)
		{
			if (member == null ||
				member.IsDead ||
				!role(member) ||
				!member.HasStatus(false, StatusID.Accretion) ||
				member.HasStatus(false, StatusID.EarthResistanceDownIi_3372) ||
				member.GetHealthRatio() >= 1f ||
				member.DistanceToPlayer() > HealRange)
			{
				continue;
			}

			if (best == null || member.GetHealthRatio() < best.GetHealthRatio())
			{
				best = member;
			}
		}

		return best;
	}

	private static bool HasAccretionToFill(IBattleChara m) =>
		!m.IsDead &&
		!m.IsJobCategory(JobRole.Tank) &&
		m.HasStatus(false, StatusID.Accretion) &&
		!m.HasStatus(false, StatusID.EarthResistanceDownIi_3372);

	public static bool GuardActive(IEnumerable<IBattleChara>? party) =>
		party != null &&
		DataCenter.IsInDMU &&
		AnyMember(party, HasAccretionToFill);

	private const long AccretionSettleMs = 3_000;
	private static int _lastHolders;
	private static long _holdersChangedAtMs;

	private static (int Holders, bool EarthDown, bool Settling) ReadState(IEnumerable<IBattleChara> party)
	{
		var holders = 0;
		var earthDown = false;
		foreach (var member in party)
		{
			if (member == null || member.IsDead)
			{
				continue;
			}

			if (member.HasStatus(false, StatusID.Accretion))
			{
				holders++;
			}

			if (member.HasStatus(false, StatusID.EarthResistanceDownIi_3372))
			{
				earthDown = true;
			}
		}

		var now = Environment.TickCount64;
		if (holders != _lastHolders)
		{
			_holdersChangedAtMs = now;
		}

		_lastHolders = holders;

		if (earthDown)
		{
			_holdersChangedAtMs = 0;
		}

		return (holders, earthDown, _holdersChangedAtMs != 0 && now - _holdersChangedAtMs < AccretionSettleMs);
	}

	public static bool MicrocosmosSafe(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			_lastHolders = 0;
			_holdersChangedAtMs = 0;
			return true;
		}

		var state = ReadState(party);
		return !state.Settling && (state.Holders == 0 || (state.Holders == 1 && !state.EarthDown));
	}

	public const float GroupHealSafeHp = 0.5f;

	public static bool GroupHealBlocked(IEnumerable<IBattleChara>? party)
	{
		if (party == null || !DataCenter.IsInDMU)
		{
			return false;
		}

		foreach (var member in party)
		{
			if (member != null && HasAccretionToFill(member) && member.GetHealthRatio() > GroupHealSafeHp)
			{
				return true;
			}
		}

		return false;
	}

	public static IBattleChara? NextToFill(IEnumerable<IBattleChara> party)
	{
		var healer = LowestMatching(party, static m => HasAccretionToFill(m) && m.IsJobCategory(JobRole.Healer));
		if (healer != null)
		{
			return healer;
		}

		if (AnyMember(party, static m => m.HasStatus(false, StatusID.EarthResistanceDownIi_3372)))
		{
			return null;
		}

		return LowestMatching(party, static m => HasAccretionToFill(m) && !m.IsJobCategory(JobRole.Healer));
	}

	public static bool WouldFillOutOfOrder(IEnumerable<IBattleChara>? party, IBattleChara? member, float healHp)
	{
		if (party == null || member == null || !GuardActive(party) || !HasAccretionToFill(member))
		{
			return false;
		}

		var next = NextToFill(party);
		if (next != null && next.GameObjectId == member.GameObjectId)
		{
			return false;
		}

		return (float)member.MaxHp - member.CurrentHp <= healHp;
	}

	public static bool AreaSafe(IEnumerable<IBattleChara>? party, System.Numerics.Vector3 center, float radius, float healHp)
	{
		if (party == null || !GuardActive(party))
		{
			return true;
		}

		foreach (var member in party)
		{
			if (member != null &&
				System.Numerics.Vector3.Distance(center, member.Position) <= radius &&
				WouldFillOutOfOrder(party, member, healHp))
			{
				return false;
			}
		}

		return true;
	}

	private static IBattleChara? LowestMatching(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> match)
	{
		IBattleChara? best = null;
		foreach (var member in party)
		{
			if (member == null || !match(member))
			{
				continue;
			}

			if (best == null || member.GetHealthRatio() < best.GetHealthRatio())
			{
				best = member;
			}
		}

		return best;
	}

	private static bool AnyMember(IEnumerable<IBattleChara> party, Func<IBattleChara, bool> match)
	{
		foreach (var member in party)
		{
			if (member != null && !member.IsDead && match(member))
			{
				return true;
			}
		}

		return false;
	}

	public static bool TryUseOn(IBaseAction action, IBattleChara target, out IAction? act, bool usedUp = false)
	{
		act = null;

		if (!action.EnoughLevel || !action.IsEnabled)
		{
			return false;
		}

		if (!action.CanUse(out act,
				skipStatusProvideCheck: true,
				skipTargetStatusNeedCheck: true,
				usedUp: usedUp,
				skipAoeCheck: true,
				targetOverride: TargetType.Self))
		{
			act = null;
			return false;
		}

		action.Target = new TargetResult(target, [target], target.Position);
		return true;
	}
}

#endregion
