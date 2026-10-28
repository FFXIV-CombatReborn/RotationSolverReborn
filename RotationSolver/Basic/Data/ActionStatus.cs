namespace RotationSolver.Basic.Data;

/// <summary>
/// Values returned by ActionManager.GetActionStatus
/// </summary>
public enum ActionStatus : uint
{
	/// <summary>
	/// The action can be used.
	/// </summary>
	None = 0,

	/// <summary>
	/// Not enough HP.
	/// </summary>
	NotEnoughHp = 567,

	/// <summary>
	/// Not enough MP.
	/// </summary>
	NotEnoughMp = 568,

	/// <summary>
	/// Not enough GP.
	/// </summary>
	NotEnoughGp = 570,

	/// <summary>
	/// Not enough CP.
	/// </summary>
	NotEnoughCp = 571,

	/// <summary>
	/// "Cannot use yet." A gauge, stack, proc or combo requirement isn't met.
	/// </summary>
	RequirementNotMet = 572,

	/// <summary>
	/// "Action not yet learned." The unlock quest isn't done or the level is too low.
	/// </summary>
	NotLearned = 573,

	/// <summary>
	/// "Cannot use as current class."
	/// </summary>
	WrongClass = 574,

	/// <summary>
	/// "Cannot use during battle."
	/// </summary>
	InCombat = 575,

	/// <summary>
	/// "Cannot execute at this time." The generic failure, e.g. loading, a cutscene, being occupied or a duty restriction.
	/// </summary>
	CannotExecute = 579,

	/// <summary>
	/// "Cannot use while casting." Only returned when checkCastingActive is set.
	/// </summary>
	Casting = 580,

	/// <summary>
	/// "Cannot use while under status affliction." A status such as Silence or Pacification blocks it.
	/// </summary>
	StatusAffliction = 581,

	/// <summary>
	/// "Not yet ready." The recast is running. Only returned when checkRecastActive is set, and it hides every check after it.
	/// </summary>
	OnCooldown = 582,

	/// <summary>
	/// "You do not have that item."
	/// </summary>
	NoItem = 583,

	/// <summary>
	/// "Only available while gathering."
	/// </summary>
	OnlyWhileGathering = 584,

	/// <summary>
	/// "Cannot use while gathering."
	/// </summary>
	NotWhileGathering = 585,

	/// <summary>
	/// "Only available while crafting."
	/// </summary>
	OnlyWhileCrafting = 586,

	/// <summary>
	/// "Cannot use while crafting."
	/// </summary>
	NotWhileCrafting = 587,

	/// <summary>
	/// "Cannot use while interacting."
	/// </summary>
	Interacting = 592,

	/// <summary>
	/// "Cannot be used here."
	/// </summary>
	NotUsableHere = 593,

	/// <summary>
	/// "Unable to use in PvP areas."
	/// </summary>
	NotInPvP = 639,

	/// <summary>
	/// "Unable to use action outside of PvP."
	/// </summary>
	OnlyInPvP = 656,

	/// <summary>
	/// "Unable to use outside duels or Frontline."
	/// </summary>
	OnlyInDuelsOrFrontline = 663,

	/// <summary>
	/// "Unable to use. You are currently using another item."
	/// </summary>
	UsingAnotherItem = 1262,

	/// <summary>
	/// "Unable to use while jumping."
	/// </summary>
	Jumping = 1263,

	/// <summary>
	/// "Your inventory is full."
	/// </summary>
	InventoryFull = 1872,

	/// <summary>
	/// "Unable to use that item in this area."
	/// </summary>
	ItemNotUsableHere = 2651,

	/// <summary>
	/// "Unable to execute action. Insufficient EP."
	/// </summary>
	NotEnoughEp = 5205,

	/// <summary>
	/// "Unable to use that event action anymore." The duty action is out of charges.
	/// </summary>
	NoDutyActionCharges = 5209,

	/// <summary>
	/// "Unable to execute. No PvP additional actions are set."
	/// </summary>
	NoPvPAdditionalActions = 5298,

	/// <summary>
	/// "Unable to execute action. Action not set to active." The Blue Mage spell isn't in the active set.
	/// </summary>
	NotInActiveSet = 9301,
}