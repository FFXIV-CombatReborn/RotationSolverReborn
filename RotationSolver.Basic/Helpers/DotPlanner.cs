namespace RotationSolver.Basic.Helpers;

// Decides when an action with a DotEffect should be applied, from the target's tick clock and time to kill.
internal static class DotPlanner
{
	// From the action going off to its status landing on the target.
	private const float ApplyDelay = 0.6f;

	// Allowance for GCD drift and tick jitter when weighing a refresh one GCD later.
	private const float DeferMargin = 0.25f;

	// Horizon used while the target's time to kill is unknown.
	private const float UnknownTtk = 600;

	private const float BaseGcd = 2.5f;

	internal static bool ShouldApply(IBaseAction action, IBattleChara target)
	{
		var setting = action.Setting;
		var dot = setting.Dot;
		var statuses = setting.TargetStatusProvide;
		if (dot == null || statuses == null || target.HasApplyStatus(statuses))
		{
			return false;
		}

		var expiry = target.StatusTime(setting.StatusFromSelf, statuses);
		if (expiry == float.MaxValue)
		{
			return false;
		}

		var gcd = DataCenter.DefaultGCDTotal > 0 ? DataCenter.DefaultGCDTotal : BaseGcd;
		var extraGcds = dot.ExtraGcds?.Invoke() ?? 0;
		var ttk = target.GetTTK();
		var plan = new Plan(target.GameObjectId, dot, expiry, float.IsNaN(ttk) ? UnknownTtk : ttk, gcd);
		var land = DataCenter.DefaultGCDRemain + (extraGcds * gcd) + action.Info.CastTime + ApplyDelay;

		// Once the current copy won't cover the landing, waiting only leaves more ticks uncovered.
		if (expiry > land && plan.Loss(land) >= plan.Loss(land + gcd + DeferMargin))
		{
			return false;
		}

		if (dot.FillerPotency == null)
		{
			return true;
		}

		var gcdCost = extraGcds + (action.Action.Recast100ms / 25f);
		return plan.Gain(land) > plan.Filler * gcdCost;
	}

	// Times are seconds from now; _expiry is when the current copy runs out (0 if none) and _death when the target dies.
	private readonly struct Plan
	{
		private readonly ulong _id;
		private readonly long _now;
		private readonly float _expiry;
		private readonly float _death;
		private readonly float _duration;
		private readonly float _maxDuration;
		private readonly float _hit;
		private readonly float _tick;
		private readonly float _bonusPerSecond;

		internal float Filler { get; }

		internal Plan(ulong id, DotEffect dot, float expiry, float death, float gcd)
		{
			_id = id;
			_now = Environment.TickCount64;
			_expiry = expiry;
			_death = death;
			_duration = dot.Duration;
			_maxDuration = Math.Max(dot.MaxDuration, dot.Duration);
			_hit = dot.HitPotency?.Invoke() ?? 0;
			Filler = dot.FillerPotency?.Invoke() ?? 0;

			// Without potency data, timing weighs every tick the same.
			_tick = dot.TickPotency?.Invoke() ?? (dot.DamageTakenBonus > 0 ? 0 : 1);
			_bonusPerSecond = dot.DamageTakenBonus * (Filler > 0 ? Filler / gcd : 1);
		}

		// Potency lost by landing then: uncovered ticks before it lands, plus duration cut short by an early refresh or the cap.
		internal float Loss(float land)
		{
			var idealExpiry = land + Math.Max(_expiry - land, 0) + _duration;
			return Value(_expiry, Math.Min(land, _death))
				+ Value(Math.Min(NewExpiry(land), _death), Math.Min(idealExpiry, _death));
		}

		// Potency the application adds beyond the current copy before the target dies.
		internal float Gain(float land)
		{
			return _hit + Value(Math.Max(land, _expiry), Math.Min(NewExpiry(land), _death));
		}

		private float NewExpiry(float land)
		{
			return land + Math.Min(Math.Max(_expiry - land, 0) + _duration, _maxDuration);
		}

		private float Value(float from, float to)
		{
			if (to <= from)
			{
				return 0;
			}

			var ticks = DotTickTracker.CountTicks(_id, _now + (long)(from * 1000), _now + (long)(to * 1000));
			return (_tick * ticks) + (_bonusPerSecond * (to - from));
		}
	}
}
