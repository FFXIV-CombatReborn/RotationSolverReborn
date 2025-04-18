using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace RotationSolver.Basic.Rotations;
partial class CustomRotation
{
    #region Player
    /// <summary>
    /// This is the player.
    /// </summary>
    protected static IPlayerCharacter Player => ECommons.GameHelpers.Player.Object;

    /// <summary>
    /// Does player have swift cast, dual cast or triple cast.
    /// </summary>
    [Description("Has Swift")]
    public static bool HasSwift => Player?.HasStatus(true, StatusHelper.SwiftcastStatus) ?? false;

    /// <summary>
    /// 
    /// </summary>
    [Description("Has tank stance")]
    public static bool HasTankStance => Player?.HasStatus(true, StatusHelper.TankStanceStatus) ?? false;

    /// <summary>
    /// Check the player is moving, such as running, walking or jumping.
    /// </summary>
    [Description("Is Moving or Jumping")]
    public static bool IsMoving => DataCenter.IsMoving;

    /// <summary>
    /// Check if the player is dead.
    /// </summary>
    [Description("Is Dead, or inversely, is Alive")]
    public static bool IsDead => Player.IsDead;

    /// <summary>
    /// Is in combat.
    /// </summary>
    [Description("In Combat")]
    public static bool InCombat => DataCenter.InCombat;

    /// <summary>
    /// Is out of combat.
    /// </summary>
    [Description("Not In Combat Delay")]
    public static bool NotInCombatDelay => DataCenter.NotInCombatDelay;

    /// <summary>
    /// Player's MP.
    /// </summary>
    [Description("Player's MP")]
    public static uint CurrentMp => DataCenter.CurrentMp;

    /// <summary>
    /// Does someone in party have a damage buff active.
    /// </summary>
    [Description("Party Burst Status Active")]
    public static bool PartyBurst => PartyMembers.Any(m => m.HasStatus(true, StatusID.Brotherhood, StatusID.SearingLight, StatusID.BattleLitany, StatusID.ArcaneCircle, StatusID.MagesBallad, StatusID.TechnicalFinish, StatusID.Embolden));

    /// <summary>
    /// Determines if the current combat time is within an even minute.
    /// WARNING: Do not use as a main function of your rotation, hardcoding timers is begging for everything to fuck up.
    /// </summary>
    /// <returns>True if the current combat time is within an even minute; otherwise, false.</returns>
    public static bool IsEvenMinute()
    {
        if (CombatTime <= 0)
            return false;

        int minutes = (int)Math.Floor(CombatTime / 60f);
        return minutes % 2 == 0;
    }

    /// <summary>
    /// Determines if the current combat time is within the first 15 seconds of an even minute.
    /// WARNING: Do not use as a main function of your rotation, hardcoding timers is begging for everything to fuck up.
    /// </summary>
    /// <returns>True if the current combat time is within the first 15 seconds of an even minute; otherwise, false.</returns>
    public static bool IsWithinFirst15SecondsOfEvenMinute()
    {
        if (CombatTime <= 0)
            return false;

        int minutes = (int)Math.Floor(CombatTime / 60f);
        int secondsInCurrentMinute = (int)Math.Floor(CombatTime % 60f);

        return minutes % 2 == 0 && secondsInCurrentMinute < 15;
    }

    /// <summary>
    /// Condition.
    /// </summary>
    protected static ICondition Condition => Svc.Condition;

    #endregion

    #region Friends
    /// <summary>
    /// Has the comapnion now.
    /// </summary>
    [Description("Has companion")]
    public static bool HasCompanion => DataCenter.HasCompanion;

    /// <summary>
    /// Party member.
    /// </summary>
    protected static IEnumerable<IBattleChara> PartyMembers => DataCenter.PartyMembers;

    /// <summary>
    /// Alliance members.
    /// </summary>
    protected static IEnumerable<IBattleChara> AllianceMembers => DataCenter.AllianceMembers;

    /// <summary>
    /// Whether the number of party members is 8.
    /// </summary>
    [Description("Is Full Party")]
    public static bool IsFullParty => PartyMembers.Count() is 8 or 9;

    /// <summary>
    /// party members HP.
    /// </summary>
    protected static IEnumerable<float> PartyMembersHP => DataCenter.PartyMembersHP;

    /// <summary>
    /// Min HP in party members.
    /// </summary>
    [Description("Min HP in party members.")]
    public static float PartyMembersMinHP => DataCenter.PartyMembersMinHP;

    /// <summary>
    /// Average HP in party members.
    /// </summary>
    [Description("Average HP in party members.")]
    public static float PartyMembersAverHP => DataCenter.PartyMembersAverHP;
    #endregion

    #region Target
    /// <summary>
    /// The player's target.
    /// <br> WARNING: Do not use if there is more than one target, this is not the actions target, it is the players current hard target. Try to use <see cref="IBaseAction.Target"/> or <seealso cref="HostileTarget"/> instead after using this.</br>
    /// </summary>
    protected static IBattleChara Target => Svc.Targets.Target is IBattleChara b ? b : Player;

    /// <summary>
    /// The player's target, or null if no valid target. (null clears the target)
    /// </summary>
    protected static IBattleChara? CurrentTarget => Svc.Targets.Target is IBattleChara b ? b : null;

    /// <summary>
    /// The last attacked hostile target.
    /// </summary>
    protected static IBattleChara? HostileTarget => DataCenter.HostileTarget;

    /// <summary>
    /// Is player in position to hit the positional?
    /// </summary>
    /// <param name="positional"> Which Positional? "Flank" or "Rear"?</param>
    /// <param name="enemy"></param>
    /// <returns></returns>
    public static bool CanHitPositional(EnemyPositional positional, IBattleChara enemy)
    {
        if (enemy == null) return false;

        if (!enemy.HasPositional()) return true;

        EnemyPositional enemy_positional = enemy.FindEnemyPositional();

        if (enemy_positional == positional)
            return true;
        return false;
    }

    /// <summary>
    /// Is there any hostile target in range? 25 for ranged jobs and healer, 3 for melee and tank.
    /// </summary>
    [Description("Has hostiles in Range")]
    public static bool HasHostilesInRange => DataCenter.HasHostilesInRange;

    /// <summary>
    /// Is there any hostile target in 25 yalms?
    /// </summary>
    [Description("Has hostiles in 25 yalms")]
    public static bool HasHostilesInMaxRange => DataCenter.HasHostilesInMaxRange;

    /// <summary>
    /// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank.
    /// </summary>
    [Description("The number of hostiles in Range")]
    public static int NumberOfHostilesInRange => DataCenter.NumberOfHostilesInRange;

    /// <summary>
    /// How many hostile targets in max range (25 yalms) regardless of job
    /// </summary>
    [Description("The number of hostiles in max Range")]
    public static int NumberOfHostilesInMaxRange => DataCenter.NumberOfHostilesInMaxRange;

    /// <summary>
    /// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank. This is all can attack.
    /// </summary>
    [Description("The number of all hostiles in Range")]
    public static int NumberOfAllHostilesInRange => DataCenter.NumberOfAllHostilesInRange;

    /// <summary>
    /// How many hostile targets in max range (25 yalms) regardless of job. This is all can attack.
    /// </summary>
    [Description("The number of all hostiles in max Range")]
    public static int NumberOfAllHostilesInMaxRange => DataCenter.NumberOfAllHostilesInMaxRange;

    /// <summary>
    /// All hostile Targets. This is all attackable targets.
    /// </summary>
    protected static IEnumerable<IBattleChara> AllHostileTargets => DataCenter.AllHostileTargets;

    /// <summary>
    /// All targets. This includes both hostile and friendly targets.
    /// </summary>
    protected static IEnumerable<IBattleChara> AllTargets => DataCenter.AllTargets;

    /// <summary>
    /// Average time to kill for all targets.
    /// </summary>
    [Description("Average time to kill")]
    public static float AverageTTK => DataCenter.AverageTTK;

    /// <summary>
    /// The level of the LB.
    /// </summary>
    [Description("Limit Break Level")]
    public unsafe static byte LimitBreakLevel
    {
        get
        {
            var controller = UIState.Instance()->LimitBreakController;
            var barValue = *(ushort*)&controller.BarCount;
            if (barValue == 0) return 0;
            return (byte)(controller.BarCount / barValue);
        }
    }

    /// <summary>
    /// Is the <see cref="AverageTTK"/> larger than <paramref name="time"/>.
    /// </summary>
    /// <param name="time">Time</param>
    /// <returns>Is Longer.</returns>
    public static bool IsLongerThan(float time)
    {
        if (IsInHighEndDuty) return true;
        return AverageTTK > time;
    }

    /// <summary>
    /// How long each mob has been in combat.
    /// </summary>
    [Description("Mobs Time")]
    public static bool MobsTime => DataCenter.MobsTime;
    #endregion

    /// <summary>
    /// Whether or not the player can use AOE heal oGCDs.
    /// </summary>
    [Description("Can heal area ability")]
    public virtual bool CanHealAreaAbility => true;

    /// <summary>
    /// Whether or not the player can use AOE heal GCDs.
    /// </summary>
    [Description("Can heal area spell")]
    public virtual bool CanHealAreaSpell => true;

    /// <summary>
    /// Whether or not the player can use ST heal oGCDs.
    /// </summary>
    [Description("Can heal single ability")]
    public virtual bool CanHealSingleAbility => true;

    /// <summary>
    /// Whether or not the player can use ST heal GCDs.
    /// </summary>
    [Description("Can heal single spell")]
    public virtual bool CanHealSingleSpell => true;

    /// <summary>
    /// Is RSR enabled.
    /// </summary>
    [Description("The state of auto. True for on.")]
    public static bool AutoState => DataCenter.State;

    /// <summary>
    /// Is RSR in manual mode.
    /// </summary>
    [Description("The state of manual. True for manual.")]
    public static bool IsManual => DataCenter.IsManual;

    #region GCD

    /// <summary>
    /// 
    /// </summary>
    protected static float WeaponRemain => DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    protected static float WeaponTotal => DataCenter.DefaultGCDTotal;

    /// <summary>
    /// 
    /// </summary>
    protected static float WeaponElapsed => DataCenter.DefaultGCDElapsed;
    #endregion

    /// <summary>
    /// Client Language.
    /// </summary>
    protected static ClientLanguage Language => Svc.ClientState.ClientLanguage;

    /// <summary>
    /// Type of the content player is in.
    /// </summary>
    protected static TerritoryContentType TerritoryContentType => DataCenter.Territory?.ContentType ?? TerritoryContentType.None;

    /// <summary>
    /// Is player in high-end duty, savage, extrene or ultimate.
    /// </summary>
    [Description("Is in the high-end duty")]
    public static bool IsInHighEndDuty => DataCenter.Territory?.IsHighEndDuty ?? false;

    /// <summary>
    /// Is player in a normal or chaotic Alliance Raid.
    /// </summary>
    [Description("Is in an Alliance Raid (including Chaotic)")]
    public static bool IsInAllianceRaid => DataCenter.IsInAllianceRaid;

    /// <summary>
    /// Is player in UCoB duty.
    /// </summary>
    [Description("Is in UCoB duty")]
    public static bool IsInUCoB => DataCenter.IsInUCoB;

    /// <summary>
    /// Is player in UwU duty.
    /// </summary>
    [Description("Is in UwU duty")]
    public static bool IsInUwU => DataCenter.IsInUwU;

    /// <summary>
    /// Is player in TEA duty.
    /// </summary>
    [Description("Is in TEA duty")]
    public static bool IsInTEA => DataCenter.IsInTEA;

    /// <summary>
    /// Is player in DSR duty.
    /// </summary>
    [Description("Is in DSR duty")]
    public static bool IsInDSR => DataCenter.IsInDSR;

    /// <summary>
    /// Is player in TOP duty.
    /// </summary>
    [Description("Is in TOP duty")]
    public static bool IsInTOP => DataCenter.IsInTOP;

    ///<summary>
    /// Is player in FRU duty.
    ///</summary>
    [Description("Is in FRU duty")]
    public static bool IsInFRU => DataCenter.IsInFRU;

    ///<summary>
    /// Is player in COD duty.
    ///</summary>
    [Description("Is in FRU duty")]
    public static bool IsInCOD => DataCenter.IsInCOD;

    /// <summary>
    /// Is player in any instanced duty.
    /// </summary>
    [Description("Is player in duty")]
    public static bool IsInDuty => DataCenter.IsInDuty;

    /// <summary>
    /// Your ping.
    /// </summary>
    [Description("Your ping")]
    [Obsolete("No longer tracked or calculated")]
    public static float Ping => 0f;

    /// <summary>
    /// Time from next ability to next GCD
    /// </summary>
    [Description("Time from next ability to next GCD")]
    public static float NextAbilityToNextGCD => DataCenter.DefaultGCDRemain - Math.Max(ActionManagerHelper.GetCurrentAnimationLock(), DataCenter.MinAnimationLock);

    /// <summary>
    /// Treats one action as another.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static uint AdjustId(uint id) => Service.GetAdjustedActionId(id);

    /// <summary>
    /// Treats one action as another.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static ActionID AdjustId(ActionID id) => Service.GetAdjustedActionId(id);

    /// <summary>
    /// Average amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
    /// </summary>
    public double AverageCountOfLastUsing { get; internal set; } = 0;

    /// <summary>
    /// Max amount of times a rotation calls IsLastGCD, IsLastAbility, or IsLastAction.
    /// </summary>
    public int MaxCountOfLastUsing { get; internal set; } = 0;

    /// <summary>
    /// The average count of not recommend members using.
    /// </summary>
    public double AverageCountOfCombatTimeUsing { get; internal set; } = 0;

    /// <summary>
    /// The max count of not recommend members using.
    /// </summary>
    public int MaxCountOfCombatTimeUsing { get; internal set; } = 0;
    internal long CountOfTracking { get; set; } = 0;

    internal static int CountingOfLastUsing { get; set; } = 0;
    internal static int CountingOfCombatTimeUsing { get; set; } = 0;


    /// <summary>
    ///  The actions that were used by player successfully. The first one is the latest successfully used one.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    protected static ActionRec[] RecordActions
    {
        get
        {
            CountingOfLastUsing++;
            return DataCenter.RecordActions;
        }
    }

    /// <summary>
    /// How much time has passed since the last action was used.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    protected static TimeSpan TimeSinceLastAction
    {
        get
        {
            CountingOfLastUsing++;
            return DataCenter.TimeSinceLastAction;
        }
    }

    /// <summary>
    /// Last used GCD.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used GCD")]
    public static bool IsLastGCD(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastGCD(isAdjust, actions);
    }

    /// <summary>
    /// Last used GCD.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastGCD(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastGCD(ids);
    }

    /// <summary>
    /// Last used Ability.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Ability")]
    public static bool IsLastAbility(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAbility(isAdjust, actions);
    }

    /// <summary>
    /// Last used Ability.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastAbility(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAbility(ids);
    }

    /// <summary>
    /// Last used Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Action")]
    public static bool IsLastAction(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAction(isAdjust, actions);
    }

    /// <summary>
    /// Last used Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastAction(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastAction(ids);
    }

    /// <summary>
    /// Last used Combo Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="isAdjust">Check for adjust id not raw id.</param>
    /// <param name="actions">True if any of this is matched.</param>
    /// <returns></returns>
    [Description("Just used Combo Action")]
    public static bool IsLastComboAction(bool isAdjust, params IAction[] actions)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastComboAction(isAdjust, actions);
    }

    /// <summary>
    /// Last used Combo Action.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="ids">True if any of this is matched.</param>
    /// <returns></returns>
    public static bool IsLastComboAction(params ActionID[] ids)
    {
        CountingOfLastUsing++;
        return IActionHelper.IsLastComboAction(ids);
    }

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="GCD"></param>
    /// <returns></returns>
    protected static bool CombatElapsedLessGCD(int GCD)
    {
        CountingOfCombatTimeUsing++;
        return CombatElapsedLess(GCD * DataCenter.DefaultGCDTotal);
    }

    /// <summary>
    /// Whether the battle lasted less than <paramref name="time"/> seconds
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="time">time in second.</param>
    /// <returns></returns>
    protected static bool CombatElapsedLess(float time)
    {
        CountingOfCombatTimeUsing++;
        return CombatTime <= time;
    }

    /// <summary>
    /// How long combat has been going.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Combat time")]
    public static float CombatTime
    {
        get
        {
            CountingOfCombatTimeUsing++;
            return InCombat ? DataCenter.CombatTimeRaw + DataCenter.DefaultGCDRemain : 0;
        }
    }

    /// <summary>
    /// How long is remaining on the Combo Timer.
    /// <br>WARNING: Do not make this method the main logic of your rotation.</br>
    /// </summary>
    [Description("Combo time")]
    public static float LiveComboTime
    {
        get
        {
            try
            {
                return DataCenter.ComboTime;
            }
            catch (Exception)
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="GCD"></param>
    /// <returns></returns>
    protected static bool StopMovingElapsedLessGCD(int GCD) => StopMovingElapsedLess(GCD * DataCenter.DefaultGCDTotal);

    /// <summary>
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    /// <param name="time">time in second.</param>
    /// <returns></returns>
    protected static bool StopMovingElapsedLess(float time) => StopMovingTime <= time;

    /// <summary>
    /// How long the player has been standing still.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Stop moving time")]
    public static float StopMovingTime => IsMoving ? 0 : DataCenter.StopMovingRaw + DataCenter.DefaultGCDRemain;


    /// <summary>
    /// How long the player has been moving.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("Moving time")]
    public static float MovingTime => IsMoving ? DataCenter.MovingRaw + DataCenter.DefaultGCDRemain : 0;
    /// <summary>
    /// How long the player has been alive.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("How long the player has been alive.")]
    public static float AliveTime => Player.IsAlive() ? DataCenter.AliveTimeRaw + DataCenter.DefaultGCDRemain : 0;

    /// <summary>
    /// How long the player has been dead.
    /// <br>WARNING: Do Not make this method the main of your rotation.</br>
    /// </summary>
    [Description("How long the player has been dead.")]
    public static float DeadTime => Player.IsAlive() ? 0 : DataCenter.DeadTimeRaw + DataCenter.DefaultGCDRemain;

    /// <summary>
    /// Time from GCD.
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static float GCDTime(uint gcdCount = 0, float offset = 0)
        => DataCenter.GCDTime(gcdCount, offset);

    #region Service

    /// <summary>
    /// The count down ahead.
    /// </summary>
    [Description("Count Down ahead")]
    public static float CountDownAhead => Service.Config.CountDownAhead;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of Area Ability")]
    public static float HealthAreaAbility => Service.Config.HealthAreaAbility;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of Area spell")]
    public static float HealthAreaSpell => Service.Config.HealthAreaSpell;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of Area Ability Hot")]
    public static float HealthAreaAbilityHot => Service.Config.HealthAreaAbilityHot;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of Area spell Hot")]
    public static float HealthAreaSpellHot => Service.Config.HealthAreaSpellHot;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of single ability")]
    public static float HealthSingleAbility => Service.Config.HealthSingleAbility;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of single spell")]
    public static float HealthSingleSpell => Service.Config.HealthSingleSpell;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of single ability Hot")]
    public static float HealthSingleAbilityHot => Service.Config.HealthSingleAbilityHot;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of single spell Hot")]
    public static float HealthSingleSpellHot => Service.Config.HealthSingleSpellHot;

    /// <summary>
    /// 
    /// </summary>
    [Description("Health of dying tank")]
    public static float HealthForDyingTanks => Service.Config.HealthForDyingTanks;

    /// <summary>
    /// 
    /// </summary>
    [Description("Whether or not Invincibility should be ignored for a PvP action.")]
    public static bool IgnorePvPInvincibility => Service.Config.IgnorePvPInvincibility;
    #endregion

    /// <summary>
    /// In the burst status.
    /// </summary>
    [Description("Is burst")]
    public static bool IsBurst => MergedStatus.HasFlag(AutoStatus.Burst);

    /// <summary>
    /// The merged status, which contains <see cref="AutoState"/> and <see cref="CommandStatus"/>.
    /// </summary>
    public static AutoStatus MergedStatus => DataCenter.MergedStatus;

    /// <summary>
    /// The automatic status, which is checked from RS.
    /// </summary>
    public static AutoStatus AutoStatus => DataCenter.AutoStatus;

    /// <summary>
    /// The CMD status, which is checked from the player.
    /// </summary>
    public static AutoStatus CommandStatus => DataCenter.CommandStatus;
}
