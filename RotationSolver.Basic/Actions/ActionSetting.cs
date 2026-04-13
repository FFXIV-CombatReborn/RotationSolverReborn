namespace RotationSolver.Basic.Actions;

/// <summary>
/// Specific action type for the action.
/// </summary>
public enum SpecialActionType : byte
{
	/// <summary>
	/// 
	/// </summary>
	None,

	/// <summary>
	/// 
	/// </summary>
	MeleeRange,

	/// <summary>
	/// 
	/// </summary>
	MovingBackward,

	/// <summary>
	/// 
	/// </summary>
	HostileMovingForward,

	/// <summary>
	/// 
	/// </summary>
	FriendlyMovingForward,

	/// <summary>
	/// 
	/// </summary>
	HostileFriendlyMovingForward,
}

/// <summary>
/// Setting from the developer.
/// </summary>
public class ActionSetting
{
	/// <summary>
	/// The Ninjutsu action of this action.
	/// </summary>
	public IBaseAction[]? Ninjutsu { get; set; } = null;

	/// <summary>
	/// The override of the <see cref="ActionBasicInfo.MPNeed"/>.
	/// </summary>
	public Func<uint?>? MPOverride { get; set; } = null;

	/// <summary>
	/// Is this action in the melee range.
	/// </summary>
	internal SpecialActionType SpecialType { get; set; }

	/// <summary>
	/// Is this status only ever added by the caster/player. 
	/// By default true, if false, it can be added by other sources and prevents the action from being used in case of overlapping statuses.
	/// </summary>
	public bool StatusFromSelf { get; set; } = true;

	/// <summary>
	/// Is this action a Mudra. 
	/// </summary>
	public bool IsMudra { get; set; } = false;

	/// <summary>
	/// The status that is provided to the target of the ability.
	/// </summary>
	public StatusID[]? TargetStatusProvide { get; set; } = null;

	/// <summary>
	/// The status that it needs on the target.
	/// </summary>
	public StatusID[]? TargetStatusNeed { get; set; } = null;

	/// <summary>
	/// Can the target be targeted.
	/// </summary>
	public Func<IBattleChara, bool> CanTarget { get; set; } = t => true;

	/// <summary>
	/// The additional not combo ids.
	/// </summary>
	public ActionID[]? ComboIdsNot { get; set; }

	/// <summary>
	/// The additional combo ids.
	/// </summary>
	public ActionID[]? ComboIds { get; set; }

	/// <summary>
	/// Status that this action provides.
	/// </summary>
	public StatusID[]? StatusProvide { get; set; } = null;

	/// <summary>
	/// Status that this action needs.
	/// </summary>
	public StatusID[]? StatusNeed { get; set; } = null;

	/// <summary>
	/// Your custom rotation check for your rotation.
	/// </summary>
	public Func<bool>? RotationCheck { get; set; } = null;

	internal Func<bool>? ActionCheck { get; set; } = null;

	internal Func<ActionConfig>? CreateConfig { get; set; } = null;

	/// <summary>
	/// Is this action friendly.
	/// </summary>
	public bool IsFriendly { get; set; }

	/// <summary>
	/// Is this action a Single Target Healing GCD.
	/// </summary>
	public bool GCDSingleHeal { get; set; }

	private TargetType _type = TargetType.Big;

	/// <summary>
	/// The strategy to target the target.
	/// </summary>
	public TargetType TargetType
	{
		get
		{
			TargetType type = IBaseAction.TargetOverride ?? _type;
			if (IsFriendly)
			{

			}
			else
			{
				switch (type)
				{
					case TargetType.BeAttacked:
						return _type;
				}
			}

			return type;
		}
		set => _type = value;
	}

	/// <summary>
	/// The enemy positional for this action.
	/// </summary>
	public EnemyPositional EnemyPositional { get; set; } = EnemyPositional.None;

	/// <summary>
	/// Should end the special.
	/// </summary>
	public bool EndSpecial { get; set; }

	/// <summary>
	/// The quest ID that unlocks this action.
	/// 0 means no quest.
	/// </summary>
	public uint UnlockedByQuestID { get; set; } = 0;
}
