using static RotationSolver.Configuration.ConfigTypes;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver.Engine.Actions;

public partial struct ActionTargetInfo
{
	#region Get Most Target

	private readonly IEnumerable<IBattleChara> GetMostCanTargetObjects(IEnumerable<IBattleChara> canTargets, IEnumerable<IBattleChara> canAffects, int aoeCount)
	{
		if (canTargets == null || canAffects == null)
		{
			yield break;
		}

		if (IsSingleTarget || EffectRange <= 0)
		{
			foreach (var target in canTargets)
			{
				yield return target;
			}

			yield break;
		}

		if (!action.Setting.IsFriendly && Service.Config.AoEType == AoEType.Off)
		{
			yield break;
		}

		if (!action.Setting.IsFriendly && aoeCount > 1 && (Service.Config.AoEType == AoEType.Cleave || (DataCenter.IsInM9S && Service.Config.M9SCleaveOnly)))
		{
			yield break;
		}

		var targets = canTargets as List<IBattleChara> ?? [.. canTargets];

		List<IBattleChara> best = [];
		var bestCount = Math.Max(0, aoeCount);

		foreach (var t in targets)
		{
			var count = GetCanTargetCount(t, canAffects);
			if (count < bestCount)
			{
				continue;
			}

			if (count > bestCount)
			{
				bestCount = count;
				best.Clear();
			}
			best.Add(t);
		}

		for (var i = 0; i < best.Count; i++)
		{
			yield return best[i];
		}
	}

	private readonly int GetCanTargetCount(IBattleChara target, IEnumerable<IBattleChara> canAffects)
	{
		if (target == null || canAffects == null)
		{
			return 0;
		}

		var count = 0;
		foreach (var t in canAffects)
		{
			if (target != t && !GetCanTarget(target, t))
			{
				continue;
			}

			if (Service.Config.NoNewHostiles && t.TargetObject == null && !t.IsDummy())
			{
				return 0;
			}
			count++;
		}

		return count;
	}

	private const double _alpha = Math.PI / 3;
	private static readonly float SinAlpha = (float)Math.Sin(_alpha);
	private static readonly double CosAlpha = Math.Cos(_alpha);

	private readonly bool GetCanTarget(IBattleChara target, IBattleChara subTarget)
	{
		var player = Player.Object;
		if (target == null || subTarget == null || player == null)
		{
			return false;
		}

		var pPos = player.Position;
		var targetPos = target.Position;
		var subPos = subTarget.Position;
		var subRadius = subTarget.HitboxRadius;
		var effectRange = EffectRange;
		var castType = (CastType)action.Action.CastType;
		var dir = targetPos - pPos;
		var tdir = subPos - pPos;

		var dirLen = dir.Length();
		var tdirLen = tdir.Length();

		if (dirLen <= 0.001f)
		{
			switch (castType)
			{
				case CastType.Circle:
					return Vector3.Distance(targetPos, subPos) - subRadius <= effectRange;

				case CastType.Cone:
					{
						if (tdirLen - subRadius > effectRange)
						{
							return false;
						}

						var face = player.GetFaceVector();
						var faceLen = face.Length();
						if (faceLen <= 0.001f)
						{
							return true;
						}

						var faceN = face / faceLen;
						var tdirAdj = tdir + faceN * (subRadius / SinAlpha);
						var tlen = tdirAdj.Length();
						if (tlen <= 0.001f)
						{
							return true;
						}

						var cos = Vector3.Dot(faceN, tdirAdj) / tlen;
						return cos >= CosAlpha;
					}

				case CastType.StraightLine:
					{
						if (tdirLen - subRadius > effectRange)
						{
							return false;
						}

						var face = player.GetFaceVector();
						var lineDir2 = Vector3.Normalize(new Vector3(face.X, 0, face.Z));
						Vector3 tdirXZ2 = new(tdir.X, 0, tdir.Z);
						var perp2 = Vector3.Cross(lineDir2, tdirXZ2).Length();
						var halfWidth2 = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier / 2f : 2f;
						return perp2 <= halfWidth2 + subRadius && Vector3.Dot(lineDir2, tdirXZ2) >= 0;
					}

				case CastType.Donut:
					{
						var centerDist = Vector3.Distance(targetPos, subPos);
						var innerRadius = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier : 8f;
						return centerDist - subRadius <= effectRange && centerDist + subRadius >= innerRadius;
					}

				default:
					return false;
			}
		}

		switch (castType)
		{
			case CastType.Circle:
				return Vector3.Distance(targetPos, subPos) - subRadius <= effectRange;

			case CastType.Cone:
				{
					if (tdirLen - subRadius > effectRange)
					{
						return false;
					}

					var dirN = dir / dirLen;

					var tdirAdj = tdir + dirN * (subRadius / SinAlpha);

					var tlen = tdirAdj.Length();
					if (tlen <= 0.001f)
					{
						return true;
					}

					var cos = Vector3.Dot(dir, tdirAdj) / (dirLen * tlen);
					return cos >= CosAlpha;
				}

			case CastType.StraightLine:
				{
					if (tdirLen - subRadius > effectRange)
					{
						return false;
					}

					var flatDir = new Vector3(dir.X, 0, dir.Z);
					Vector3 lineDir;
					if (flatDir.LengthSquared() > 1e-6f)
					{
						lineDir = Vector3.Normalize(flatDir);
					}
					else
					{
						var face = player.GetFaceVector();
						lineDir = Vector3.Normalize(new Vector3(face.X, 0, face.Z));
					}

					Vector3 tdirXZ = new(tdir.X, 0, tdir.Z);
					var perp = Vector3.Cross(lineDir, tdirXZ).Length();

					var halfWidth = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier / 2f : 2f;
					var width = halfWidth + subRadius;

					return perp <= width && Vector3.Dot(lineDir, tdirXZ) >= 0;
				}

			case CastType.Donut:
				{
					var centerDist = Vector3.Distance(targetPos, subPos);
					var innerRadius = action.Action.XAxisModifier > 0 ? action.Action.XAxisModifier : 8f;
					return centerDist - subRadius <= effectRange && centerDist + subRadius >= innerRadius;
				}

			default:
				return false;
		}
	}

	#endregion
}
