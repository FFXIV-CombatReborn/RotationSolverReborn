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
	/// Does this action care about the Flat Damage/Death check on masked carnivale mobs.
	/// </summary>
	public bool IsFlatDamageDeath { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Slow</b>? When true, it is skipped against mobs not vulnerable to Slow in the Masked Carnivale.
	/// </summary>
	public bool IsSlowSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Petrification</b>? When true, it is skipped against mobs not vulnerable to Petrification in the Masked Carnivale.
	/// </summary>
	public bool IsPetrificationSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Paralysis</b>? When true, it is skipped against mobs not vulnerable to Paralysis in the Masked Carnivale.
	/// </summary>
	public bool IsParalysisSpell { get; set; } = false;

	/// <summary>
	/// Does this action care about the Interrupt check on masked carnivale mobs.
	/// </summary>
	public bool IsInterruptSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Blind</b>? When true, it is skipped against mobs not vulnerable to Blind in the Masked Carnivale.
	/// </summary>
	public bool IsBlindSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Stun</b>? When true, it is skipped against mobs not vulnerable to Stun in the Masked Carnivale.
	/// </summary>
	public bool IsStunSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Sleep</b>? When true, it is skipped against mobs not vulnerable to Sleep in the Masked Carnivale.
	/// </summary>
	public bool IsSleepSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Bind</b>? When true, it is skipped against mobs not vulnerable to Bind in the Masked Carnivale.
	/// </summary>
	public bool IsBindSpell { get; set; } = false;

	/// <summary>
	/// Does this action primarily apply <b>Heavy</b>? When true, it is skipped against mobs not vulnerable to Heavy in the Masked Carnivale.
	/// </summary>
	public bool IsHeavySpell { get; set; } = false;

	/// <summary>
	/// Overrides the <see cref="ActionBasicInfo.Aspect"/> reported for this action.
	/// When set, this value is returned instead of the game data aspect.
	/// Useful for physical actions (e.g., <see cref="Aspect.Physical"/>) where
	/// the actual damage type (Slashing, Piercing, Blunt) should be exposed.
	/// </summary>
	public Aspect? AspectOverride { get; set; } = null;

	/// <summary>
	/// Additional aspects for this action beyond the primary <see cref="AspectOverride"/>.
	/// Use when a spell deals damage of more than one aspect simultaneously.
	/// Combined with <see cref="AspectOverride"/> (or the game data aspect when that is not set)
	/// and exposed via <see cref="ActionBasicInfo.Aspects"/> and <see cref="ActionBasicInfo.HasAspect"/>.
	/// </summary>
	public Aspect[]? AdditionalAspects { get; set; } = null;

	/// <summary>
	/// Overrides the <see cref="ActionBasicInfo.AttackType"/> reported for this action.
	/// When set, this value is returned instead of the game data attack type.
	/// Useful when the raw game data attack type differs from the intended damage type
	/// (e.g., overriding a generic <see cref="AttackType.Physical"/> to <see cref="AttackType.Slashing"/>,
	/// <see cref="AttackType.Piercing"/>, or <see cref="AttackType.Blunt"/>).
	/// </summary>
	public AttackType? AttackTypeOverride { get; set; } = null;

	/// <summary>
	/// Additional attack types for this action beyond the primary <see cref="AttackTypeOverride"/>.
	/// Use when an action can inflict damage of more than one attack type simultaneously.
	/// Combined with <see cref="AttackTypeOverride"/> (or the game data attack type when that is not set)
	/// and exposed via <see cref="ActionBasicInfo.AttackTypes"/> and <see cref="ActionBasicInfo.HasAttackType"/>.
	/// </summary>
	public AttackType[]? AdditionalAttackTypes { get; set; } = null;

	/// <summary>
	/// The quest ID that unlocks this action.
	/// 0 means no quest.
	/// </summary>
	public uint UnlockedByQuestID { get; set; } = 0;
}
