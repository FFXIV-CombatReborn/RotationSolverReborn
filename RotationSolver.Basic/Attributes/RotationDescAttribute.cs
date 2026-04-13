namespace RotationSolver.Basic.Attributes;

/// <summary>
/// The description about the macro. If it tags the rotation class, it means Burst. Others mean the macro that this method belongs to.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class RotationDescAttribute : Attribute
{
	/// <summary>
	/// Description.
	/// </summary>
	public string Description { get; private set; } = string.Empty;

	/// <summary>
	/// Description type.
	/// </summary>
	public DescType Type { get; private set; } = DescType.None;

	/// <summary>
	/// What actions this linked.
	/// </summary>
	public IEnumerable<ActionID> Actions { get; private set; } = [];

	internal uint IconID => Type switch
	{
		DescType.BurstActions => 62583,

		DescType.HealAreaGCD or DescType.HealAreaAbility or
		DescType.HealSingleGCD or DescType.HealSingleAbility => 62582,

		DescType.DefenseAreaGCD or DescType.DefenseAreaAbility or
		DescType.DefenseSingleGCD or DescType.DefenseSingleAbility => 62581,

		DescType.MoveForwardGCD or DescType.MoveForwardAbility or
		DescType.MoveBackAbility => 104,

		DescType.SpeedAbility => 844,

		_ => 62144,
	};

	internal bool IsOnCommand
	{
		get
		{
			SpecialCommandType command = DataCenter.SpecialType;
			return Type switch
			{
				DescType.BurstActions => command == SpecialCommandType.Burst,
				DescType.HealAreaAbility or DescType.HealAreaGCD => command == SpecialCommandType.HealArea,
				DescType.HealSingleAbility or DescType.HealSingleGCD => command == SpecialCommandType.HealSingle,
				DescType.DefenseAreaGCD or DescType.DefenseAreaAbility => command == SpecialCommandType.DefenseArea,
				DescType.DefenseSingleGCD or DescType.DefenseSingleAbility => command == SpecialCommandType.DefenseSingle,
				DescType.MoveForwardGCD or DescType.MoveForwardAbility => command == SpecialCommandType.MoveForward,
				DescType.MoveBackAbility => command == SpecialCommandType.MoveBack,
				DescType.SpeedAbility => command == SpecialCommandType.Speed,
				_ => false,
			};
		}
	}

	internal RotationDescAttribute(DescType descType)
	{
		Type = descType;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="actions"></param>
	public RotationDescAttribute(params ActionID[] actions)
		: this(string.Empty, actions)
	{
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="desc"></param>
	/// <param name="actions"></param>
	public RotationDescAttribute(string desc, params ActionID[] actions)
	{
		Description = desc;
		Actions = actions;
	}

	private RotationDescAttribute()
	{
	}

	internal static IEnumerable<RotationDescAttribute[]> Merge(IEnumerable<RotationDescAttribute?> rotationDescAttributes)
	{
		if (rotationDescAttributes == null)
		{
			yield break;
		}

		var dict = new Dictionary<DescType, List<RotationDescAttribute>>();
		foreach (var r in rotationDescAttributes)
		{
			if (r == null) continue;
			if (!dict.TryGetValue(r.Type, out var list))
			{
				list = new List<RotationDescAttribute>();
				dict[r.Type] = list;
			}
			list.Add(r);
		}

		var keys = new List<DescType>(dict.Keys);
		keys.Sort();
		foreach (var k in keys)
		{
			yield return dict[k].ToArray();
		}
	}

	internal static RotationDescAttribute? MergeToOne(IEnumerable<RotationDescAttribute> rotationDescAttributes)
	{
		RotationDescAttribute result = new();
		var actionSet = new HashSet<ActionID>();
		foreach (RotationDescAttribute attr in rotationDescAttributes)
		{
			if (attr == null)
			{
				continue;
			}

			if (!string.IsNullOrEmpty(attr.Description))
			{
				result.Description = attr.Description;
			}
			if (attr.Type != DescType.None)
			{
				result.Type = attr.Type;
			}
			if (attr.Actions != null)
			{
				foreach (var a in attr.Actions)
				{
					actionSet.Add(a);
				}
			}
		}

		result.Actions = [.. actionSet];
		return result.Type == DescType.None ? null : result;
	}
}
