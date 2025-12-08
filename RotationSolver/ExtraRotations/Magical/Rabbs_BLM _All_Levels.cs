using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using System.ComponentModel;
using System.Diagnostics;

namespace RotationSolver.ExtraRotations.Magical;
[Rotation("Rabbs Blackest Mage", CombatType.PvE, GameVersion = "7.3")]
[SourceCode(Path = "main/ExtraRotations/Magical/Rabbs_BLM_All_Levels.cs")]
[ExtraRotation]

public sealed class Rabbs_BLM : BlackMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Countdown Ability (Fire 3)")]
    public bool Usecountdown { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "When to use Opener")]
    [Range(1, 4, ConfigUnitType.None, 1)]
    public OpenWhen When2Open { get; set; } = OpenWhen.Never;

    [RotationConfig(CombatType.PvE, Name = "Which Opener to use")]
    [Range(1, 2, ConfigUnitType.None, 1)]
    public Openchoice Openerchoice { get; set; } = Openchoice.Standard;

    [RotationConfig(CombatType.PvE, Name = "When to use Burst")]
    [Range(1, 5, ConfigUnitType.None, 1)]
    public BurstWhen When2Burst { get; set; } = BurstWhen.Never;

    [RotationConfig(CombatType.PvE, Name = "Which Abilities for burst to manage")]
    [Range(1, 3, ConfigUnitType.None, 1)]
    public Burstchoice ChoiceBurst { get; set; } = Burstchoice.Leylines;

    [RotationConfig(CombatType.PvE, Name = "How to use pots")]
    [Range(1, 3, ConfigUnitType.None, 1)]
    public Potchoice Poterchoice { get; set; } = Potchoice.Never;

    public enum Openchoice : byte
    {
        [Description("Standard 5+7 Opener")] Standard,
        [Description("Alternative Flare Opener")] AltFlare
    }

    public enum OpenWhen : byte
    {
        [Description("Never")] Never,
        [Description("When boss is Range")] BossInRoom,
        [Description("When boss is Targeted")] BossIsTarget,
        [Description("All day everyday")] Allday,
    }

    public enum BurstWhen : byte
    {
        [Description("Never (Self Managed)")] Never,
        [Description("Only to prevent Cap")] PreventCap,
        [Description("With others (checks if other people have party buffs")] WithOthers,
        [Description("Every Two Minutes (uses combat time so expect some error)")] Q2M,
        [Description("All day everyday")] Allday,
    }

    public enum Burstchoice : byte
    {
        [Description("Leylines")] Leylines,
        [Description("Xenoglossy")] XenoOnly,
        [Description("Both Leylines and Xenoglossy")] Both,
    }

    public enum Potchoice : byte
    {
        [Description("Never")] Never,
        [Description("With others (checks if other people have medicated status")] WithOthers,
        [Description("Every Two Minutes (uses combat time so expect some error)")] Q2M,
        [Description("All day everyday")] Allday,
    }
    #endregion

    #region Config Under Hood Stuff
    // temp flare fix for alt flare opener only
    public IBaseAction AltFlareOpenerPvE => _AltFlareOpenerPvE.Value;

    private static void ModifyAltFlareOpenerPvE(ref Basic.Actions.ActionSetting setting)
    {
        setting.RotationCheck = () => InAstralFire;
        setting.UnlockedByQuestID = 66614u;
    }

    private readonly Lazy<IBaseAction> _AltFlareOpenerPvE = new(static delegate
    {
        Basic.Actions.ActionSetting setting460 = new BaseAction(ActionID.FlarePvE).Setting;
        ModifyAltFlareOpenerPvE(ref setting460);
        new BaseAction(ActionID.FlarePvE).Setting = setting460;
        return new BaseAction(ActionID.FlarePvE);
    });

    private static readonly HashSet<uint> burstStatusIds =
    [
        (uint)StatusID.Divination,
        (uint)StatusID.Brotherhood,
        (uint)StatusID.BattleLitany,
        (uint)StatusID.ArcaneCircle,
        (uint)StatusID.StarryMuse,
        (uint)StatusID.Embolden,
        (uint)StatusID.SearingLight,
        (uint)StatusID.BattleVoice,
        (uint)StatusID.TechnicalFinish,
        (uint)StatusID.RadiantFinale

    ];

    public static bool IsPartyBurst => PartyMembers?.Any(member =>
        member?.StatusList?.Any(status => burstStatusIds.Contains(status.StatusId)) == true
    ) == true;

    public static bool IsPartyMedicated => PartyMembers?.Any(member =>
    member?.StatusList?.Any(status => status.StatusId == (uint)StatusID.Medicated) == true
    ) == true;

    public static bool IsAnyBossinRange => AllHostileTargets is not null && AllHostileTargets.Any(hostile => hostile.IsBossFromIcon() || hostile.IsBossFromTTK());

    public static bool IsCurrentTargetBoss => CurrentTarget is not null && (CurrentTarget.IsBossFromIcon() || CurrentTarget.IsBossFromTTK());

    public bool IsOpenerChosen => (When2Open == OpenWhen.BossIsTarget && IsCurrentTargetBoss) || (When2Open == OpenWhen.BossInRoom && IsAnyBossinRange) || When2Open == OpenWhen.Allday;

    public bool IsInOpener => IsOpenerChosen && CombatTime > 0 && CombatTime < 60 && InCombat && !Player.HasStatus(true, StatusID.BrinkOfDeath, StatusID.Weakness);

    public bool IsPotReady => (Poterchoice == Potchoice.WithOthers && IsPartyMedicated) || (Poterchoice == Potchoice.Q2M && IsWithinFirst15SecondsOfEvenMinute()) || (Poterchoice == Potchoice.Allday);

    public bool IsBurstReady => (When2Burst == BurstWhen.WithOthers && IsPartyBurst) || (When2Burst == BurstWhen.Q2M && IsWithinFirst15SecondsOfEvenMinute()) || (When2Burst == BurstWhen.Allday);
    #endregion

    #region underhood stuff
    // Centralize Thunder DoT IDs in one place so we don’t repeat them.
    private static readonly StatusID[] ThunderStatusIds =
    [
    StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv,
    StatusID.HighThunder_3872, StatusID.HighThunder
    ];

    /// <summary>
    /// Time remaining on Thunder DoT for the current target. 
    /// Returns null if no target or no Thunder present.
    /// </summary>
    public static double? ThunderDuration =>
        Target?.StatusTime(true, ThunderStatusIds) > 0
            ? Target.StatusTime(true, ThunderStatusIds)
            : null;

    public static bool TargetHasThunderDebuff => ThunderDuration.HasValue;

    public static bool ThunderBuffAboutToFallOff => ThunderDuration is < 3.0;

    public static bool ThunderBuffMoreThan10 => ThunderDuration is > 10.0;

    /// <summary>
    /// Checks if the Thunder DoT is missing on at least 3 enemies within AoE range.
    /// Used in AoE rotation to decide if Thunder II should be applied.
    /// </summary>
    /// <summary>
    /// True if there exists an AoE center such that within Thunder II range:
    /// 3 or more enemies are either missing Thunder now or will be missing it within 3s.
    /// </summary>
    public bool MissingThunderAoE
    {
        get
        {
            const int minTargets = 3;
            const double soonThreshold = 3.0;
            float aoeRange = ThunderIiPvE.Info.EffectRange;

            if (AllHostileTargets == null || !AllHostileTargets.Any())
                return false;

            foreach (var centerTarget in AllHostileTargets)
            {
                var inAoE = AllHostileTargets.Where(t =>
                    Vector3.Distance(centerTarget.Position, t.Position) <
                    (aoeRange + centerTarget.HitboxRadius));

                int needThunder = 0;

                foreach (var t in inAoE)
                {
                    if (!t.HasStatus(true, ThunderStatusIds) ||
                        t.StatusTime(true, ThunderStatusIds) <= soonThreshold)
                    {
                        needThunder++;
                    }

                    if (needThunder >= minTargets)
                        return true;
                }
            }

            return false;
        }
    }

    public bool ShouldThunder
    {
        get
        {
            if (IsInOpener)
            {
                const int openerMinGcd = 9;
                const int openerMaxGcd = 12;
                double gcdDuration = 2.5; // TODO: Replace with player’s actual GCD

                int currentGcdEstimate = (int)(CombatTime / gcdDuration);

                return Player.HasStatus(true, StatusID.Thunderhead) &&
                       currentGcdEstimate >= openerMinGcd &&
                       currentGcdEstimate <= openerMaxGcd;
            }

            if (GetAoeCount(ThunderIiPvE) >= 3)
            {
                // AoE: cast Thunder if we have Thunderhead and enough enemies need DoT
                return Player.HasStatus(true, StatusID.Thunderhead) && MissingThunderAoE;
            }

            // Single target: cast Thunder if we have Thunderhead and DoT is missing or about to fall off

            return Player.HasStatus(true, StatusID.Thunderhead) &&
                   (!TargetHasThunderDebuff || ThunderBuffAboutToFallOff);
        }
    }


    private Stopwatch? noHostilesTimer = null;

    public double GetTimeSinceNoHostilesInCombat()
    {
        if (InCombat && NumberOfAllHostilesInMaxRange == 0)
        {
            noHostilesTimer ??= Stopwatch.StartNew();
            return noHostilesTimer.Elapsed.TotalSeconds;
        }
        else
        {
            noHostilesTimer?.Stop(); // Stop the timer if it's running
            noHostilesTimer = null;
            return 0.0;
        }
    }
    //public double GetGCDRecastTime => ActionManager.GetAdjustedRecastTime(ActionType.Action, 162) / 1000.00;

    public static bool WillHave2PolyglotWithin6GCDs => (PolyglotStacks == 1 && EnochianTime < 6 * 2.5) || PolyglotStacks >= 2;

    public static bool WillHave2PolyglotWithin2GCDs => (PolyglotStacks == 1 && EnochianTime < 2 * 2.5) || PolyglotStacks >= 2;

    public bool WillBeAbleToFlareStarST
    {
        get
        {
            const int baseFireFourCost = 1600;
            const int fireFourCostWithHeart = 800;
            int soulDeficit = 6 - AstralSoulStacks;
            int discountedCasts = Math.Min(soulDeficit, UmbralHearts);
            int normalCasts = soulDeficit - discountedCasts;

            // If Manafont is available, we can likely cast enough spells to get 6 stacks
            // before running out of MP (given its significant MP recovery).
            if (!ManafontPvE.Cooldown.IsCoolingDown)
            {
                return true;
            }

            // If we already have 6 stacks, we can cast Flare Star (0 MP cost)
            if (AstralSoulStacks == 6)
            {
                return true;
            }
            // calculate the mp needed to get to 6 stacks
            int howMuchManaINeed = (discountedCasts * fireFourCostWithHeart) + (normalCasts * baseFireFourCost);

            if (CurrentMp >= howMuchManaINeed)
            {
                return true;
            }

            return false;
        }
    }


    public bool WillBeAbleToFlareStarMT
    {
        get
        {
            int soulDeficit = 6 - AstralSoulStacks;
            int flaresNeeded = (int)Math.Ceiling((double)soulDeficit / 3); // Flare grants 3 stacks

            // Manafont check still applies
            if (!ManafontPvE.Cooldown.IsCoolingDown || AstralSoulStacks == 6)
            {
                return true;
            }

            if (flaresNeeded > 2)
            { return false; }

            if (flaresNeeded == 2)
            {
                if (UmbralHearts > 0 && CurrentMp >= 2400)
                {
                    return true;
                }
            }

            if (flaresNeeded == 1)
                if (CurrentMp >= 800)
                {
                    return true;
                }

            return false; // If we can afford all the needed Flare casts
        }
    }

    public static int GetAoeCount(IBaseAction action)
    {
        int maxAoeCount = 0;

        if (!IsManual)
        {
            if (AllHostileTargets != null)
            {
                foreach (var centerTarget in AllHostileTargets.Where(t => t.DistanceToPlayer() < action.Info.Range && t.CanSee()))
                {
                    int currentAoeCount = 0;
                    foreach (var otherTarget in AllHostileTargets)
                    {
                        if (Vector3.Distance(centerTarget.Position, otherTarget.Position) < (action.Info.EffectRange + centerTarget.HitboxRadius))
                        {
                            currentAoeCount++;
                        }
                    }

                    maxAoeCount = Math.Max(maxAoeCount, currentAoeCount);
                }
            }
        }
        else if (AllHostileTargets != null && CurrentTarget != null) // Use action.Target.Target
        {
            int count = 0;
            foreach (var otherTarget in AllHostileTargets)
            {
                if (Vector3.Distance(CurrentTarget.Position, otherTarget.Position) < (action.Info.EffectRange + otherTarget.HitboxRadius))
                {
                    count++;
                }
            }
            maxAoeCount = count;
        }

        return maxAoeCount;
    }

    public bool ShouldTranspose
    {
        get
        {
            if (GetTimeSinceNoHostilesInCombat() > 5f) //we are in combat but nothing to attack for 5 seconds
            {
                if (!IsParadoxActive)
                { return true; }
                if (!HasThunder)
                { return true; }
            }
            //var recentActions = RecordActions.Take(2);
            //var lastAction = RecordActions.FirstOrDefault(); // Get the first (most recent) action, or null if the list is empty
            if (GetAoeCount(FlarePvE) >= 2)
            {
                if (InUmbralIce)
                {
                    //if (GetAoeCount(FlarePvE) >= 3 && lastAction != null && (lastAction.Action.RowId == FoulPvE.ID || lastAction.Action.RowId == ThunderIiiPvE.ID || lastAction.Action.RowId == ParadoxPvE.ID))
                    if (GetAoeCount(FlarePvE) >= 3 && (IsLastAction(true, FoulPvE) || IsLastAction(true, ThunderIiiPvE) || IsLastAction(true, ParadoxPvE)))
                    {
                        return true;
                    }
                }
                if (InAstralFire)
                {
                    //if (GetAoeCount(FlarePvE) >= 3 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && lastAction != null && (lastAction.Action.RowId == FoulPvE.ID || lastAction.Action.RowId == ThunderIiiPvE.ID || lastAction.Action.RowId == ParadoxPvE.ID))
                    if (GetAoeCount(FlarePvE) >= 2 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && (IsLastAction(true, FoulPvE) || IsLastAction(true, ThunderIiiPvE) || IsLastAction(true, ParadoxPvE)))
                    {
                        return true;
                    }
                }
            }
            if (GetAoeCount(FlarePvE) == 1)
            {
                if (InAstralFire)
                {
                    if (CurrentMp < 800 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && !WillBeAbleToFlareStarST && (!IsParadoxActive || CurrentMp < 1600) && ManafontPvE.Cooldown.IsCoolingDown)
                    {

                        if (NextGCDisInstant)
                        {
                            return true;
                        }
                    }
                }
                if (InUmbralIce)
                {
                    if (UmbralHearts == 3 && UmbralIceStacks == 3 && !IsParadoxActive && CurrentMp == 10000)
                    { return true; }
                }
            }

            return false;
        }
    }
    public bool ShouldTransposeLowLevel
    {
        get
        {
            if (!TransposePvE.Cooldown.IsCoolingDown)
            {
                if (DataCenter.PlayerSyncedLevel() < 99)
                {

                    if (GetAoeCount(FlarePvE) >= 3)
                    {
                        if (DataCenter.PlayerSyncedLevel() >= 26 && DataCenter.PlayerSyncedLevel() <= 49)//If Thunder II needs to be refreshed, use the following to generate a fresh Thunderhead proc:Transpose->Fire II x3 > Transpose
                        {

                            if (MissingThunderAoE && !Player.HasStatus(true, StatusID.Thunderhead) && (InUmbralIce || InAstralFire))
                            { return true; }

                        }
                        if (DataCenter.PlayerSyncedLevel() < 35)// can only get 2 astral fire or umbral ice stacks at these levels
                        {
                            if (InAstralFire)
                            {
                                if (DataCenter.PlayerSyncedLevel() < 18)
                                {
                                    return true;
                                }
                                if (CurrentMp < FireIiPvE.Info.MPNeed)
                                {
                                    return true;
                                }
                            }
                            if (InUmbralIce)
                            {
                                if (DataCenter.PlayerSyncedLevel() < 18)
                                { return false; }
                                // The MP condition for all levels is checked here.
                                if (CurrentMp >= 10000 || (DataCenter.PlayerSyncedLevel() >= 35 ? CurrentMp > 5000 && Player.IsCasting && UmbralIceStacks == 3 : DataCenter.PlayerSyncedLevel() >= 20 ? CurrentMp > 7500 && Player.IsCasting && UmbralIceStacks == 2 : CurrentMp > 7500 && Player.IsCasting && UmbralIceStacks == 1))
                                {
                                    return true;
                                }
                            }
                        }

                        if (DataCenter.PlayerSyncedLevel() >= 50 && DataCenter.PlayerSyncedLevel() <= 57)//(From Umbral Ice) Freeze x2 > Transpose > Fire II > Flare > Transpose > repeat
                        {
                            if (InAstralFire)
                            {
                                if (CurrentMp < FlarePvE.Info.MPNeed)
                                {
                                    return true;
                                }
                            }
                            if (InUmbralIce)
                            {
                                if (CurrentMp >= 2300 && UmbralHearts > 0)
                                {
                                    return true;
                                }
                            }
                        }
                        if (DataCenter.PlayerSyncedLevel() > 57)
                        {
                            if (InAstralFire)
                            {
                                if (CurrentMp < FlarePvE.Info.MPNeed)
                                {
                                    return true;
                                }
                            }
                            if (InUmbralIce)
                            {
                                if (CurrentMp >= 2300 && UmbralHearts > 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    //single target situation
                    if (GetAoeCount(FlarePvE) < 3)
                    {
                        if (DataCenter.PlayerSyncedLevel() < 35)
                        {
                            if (InAstralFire)
                            {
                                if (CurrentMp < FirePvE.Info.MPNeed)
                                {
                                    return true;
                                }
                            }
                            if (InUmbralIce)
                            {
                                // The MP condition for all levels is checked here.
                                if (CurrentMp >= 10000 || (DataCenter.PlayerSyncedLevel() >= 35 ? CurrentMp > 5000 && Player.IsCasting && UmbralIceStacks == 3 : DataCenter.PlayerSyncedLevel() >= 20 ? CurrentMp > 7500 && Player.IsCasting && UmbralIceStacks == 2 : CurrentMp > 7500 && Player.IsCasting && UmbralIceStacks == 1))
                                {
                                    return true;
                                }
                            }
                        }
                        if (DataCenter.PlayerSyncedLevel() >= 90)
                        {
                            if (InUmbralIce)
                            {
                                if (HasFire)
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    // General combat downtime check
                    if (GetTimeSinceNoHostilesInCombat() > 5f)
                    {
                        if (InAstralFire && CurrentMp < 10000)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    public bool ShouldXeno
    {
        get
        {
            if (PolyglotStacks == 3)
            { return true; }

            if (PolyglotStacks > 0 && IsBurstReady && CombatTime > 60)
            {
                if (ChoiceBurst == Burstchoice.Both || ChoiceBurst == Burstchoice.XenoOnly)
                { return true; }
            }
            if (PolyglotStacks > 0 && IsInOpener && CombatTime < 30 && Openerchoice == Openchoice.Standard && CurrentMp < FireIvPvE.Info.MPNeed)
                return true;
            if (PolyglotStacks > 0 && IsInOpener && CombatTime < 30 && Openerchoice == Openchoice.AltFlare && AstralSoulStacks == 2)
                return true;

            return false;
        }
    }

    public bool ShouldLeyLine
    {
        get
        {
            if (LeyLinesPvE.Cooldown.CurrentCharges == LeyLinesPvE.Cooldown.MaxCharges && !Player.HasStatus(true, StatusID.LeyLines) && (ChoiceBurst == Burstchoice.Leylines || ChoiceBurst == Burstchoice.Both) && When2Burst == BurstWhen.PreventCap)
            { return true; }

            if (LeyLinesPvE.Cooldown.CurrentCharges > 0 && IsBurstReady && !Player.HasStatus(true, StatusID.LeyLines))
            {
                if (ChoiceBurst == Burstchoice.Both || ChoiceBurst == Burstchoice.Leylines)
                { return true; }
            }

            return false;
        }
    }

    #endregion

    #region Countdown
    protected override IAction? CountDownAction(float remainTime)
    {
        if (Usecountdown)
        {
            IAction act;
            if (remainTime < FireIiiPvE.Info.CastTime + CountDownAhead && remainTime > 1)
            {
                if (FireIiiPvE.CanUse(out act)) return act;
            }
            if (remainTime < FireIiiPvE.Info.CastTime - CountDownAhead && IsMoving)
            {
                if (!NextGCDisInstant)
                {
                    if (CanMakeInstant)
                    {
                        if (SwiftcastPvE.CanUse(out act)) return act;
                    }
                }
            }
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Additional oGCD Logic
    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.PlayerSyncedLevel() == 100)
        {
            if (ShouldTranspose)
            {
                if (TransposePvE.CanUse(out act, skipCastingCheck: true, skipComboCheck: true, skipAoeCheck: true, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, skipTTKCheck: true, usedUp: true)) return true;
            }

            if (nextGCD.IsTheSameTo(true, BlizzardIiiPvE))
            {
                if (!NextGCDisInstant && CanMakeInstant && InCombat)
                {
                    if (IsInOpener)
                    {
                        if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                    }
                    if (SwiftcastPvE.CanUse(out act)) return true;
                }
            }

            if (!NextGCDisInstant && CanMakeInstant && InCombat && InAstralFire)
            {
                if (CurrentMp < 800 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && !WillBeAbleToFlareStarST && (!IsParadoxActive || CurrentMp < 1600) && ManafontPvE.Cooldown.IsCoolingDown)
                {
                    if (SwiftcastPvE.CanUse(out act)) return true;
                }
            }

            #region Opener
            if (IsInOpener)
            {
                if (AmplifierPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.LeyLines))
                {
                    if (LeyLinesPvE.CanUse(out act)) return true;
                }
            }

            if (InAstralFire)
            {

                if (IsInOpener)
                {
                    if (!NextGCDisInstant && CombatTime < 30)
                    {
                        if (SwiftcastPvE.CanUse(out act)) return true;
                    }
                    if (!IsPolyglotStacksMaxed)
                    {
                        if (AmplifierPvE.CanUse(out act)) return true;
                    }
                    if (IsPotReady && UseBurstMedicine(out act)) return true;
                }



            }
            #endregion
            if (nextGCD.IsTheSameTo(true, FlarePvE) && IsInOpener && Openerchoice == Openchoice.AltFlare && InCombat)
            {
                if (!NextGCDisInstant && CanMakeInstant)
                {
                    if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;

                }

            }

            //for aoe check if we need to use triple cast to save resources for umbral ice
            if (GetAoeCount(FlarePvE) >= 3)
            {
                if (InAstralFire)
                {
                    if (UmbralHearts > 0)
                    {
                        if (nextGCD.IsTheSameTo(true, FlarePvE) && ThunderBuffMoreThan10 && !WillHave2PolyglotWithin6GCDs) //checking if we won't need to refresh thunder AND we wont have foul after freeze (6gcd's)
                        {
                            if (!NextGCDisInstant && TriplecastPvE.Cooldown.CurrentCharges > 0 && InCombat)
                            {
                                if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                            }

                        }
                    }
                }
            }

            if (!ManafontPvE.Cooldown.IsCoolingDown && CurrentMp < 800 && AstralSoulStacks < 6 && InAstralFire)
            {
                if (ManafontPvE.CanUse(out act, skipCastingCheck: true, skipComboCheck: true, skipAoeCheck: true, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, skipTTKCheck: true, usedUp: true)) return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AetherialManipulationPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (AetherialManipulationPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BetweenTheLinesPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (BetweenTheLinesPvE.CanUse(out act)) return true;
        return base.MoveBackAbility(nextGCD, out act);
    }



    [RotationDesc(ActionID.ManawardPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE, ActionID.AddlePvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act)) return true;
        if (AddlePvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }


    #endregion

    #region oGCD Logic

    [RotationDesc(ActionID.TransposePvE, ActionID.LeyLinesPvE, ActionID.RetracePvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (ShouldLeyLine && InCombat && HasHostilesInRange && LeyLinesPvE.CanUse(out act, usedUp: ShouldLeyLine)) return true;
        //if (!IsLastAbility(ActionID.LeyLinesPvE) && UseRetrace && InCombat && HasHostilesInRange && !Player.HasStatus(true, StatusID.CircleOfPower) && RetracePvE.CanUse(out act)) return true;

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RetracePvE, ActionID.SwiftcastPvE, ActionID.TriplecastPvE, ActionID.AmplifierPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (DataCenter.PlayerSyncedLevel() == 100)
        {
            if (InCombat && HasHostilesInRange)
            {
                if (InUmbralIce)
                {
                    if (IsLastAction(ActionID.TransposePvE))
                    {
                        if (SwiftcastPvE.CanUse(out act)) return true;
                    }
                }

                if (CombatTime > 65 || When2Open == OpenWhen.Never)
                {
                    if (!IsPolyglotStacksMaxed)
                    {
                        if (AmplifierPvE.CanUse(out act)) return true;
                    }
                    if (IsPotReady && UseBurstMedicine(out act)) return true;
                }

            }
        }
        if (DataCenter.PlayerSyncedLevel() < 100)
        {
            if (!IsPolyglotStacksMaxed)
            {
                if (AmplifierPvE.CanUse(out act)) return true;
            }
            if (!ManafontPvE.Cooldown.IsCoolingDown && CurrentMp < 800 && InAstralFire)
            {
                if (ManafontPvE.CanUse(out act, skipCastingCheck: true, skipComboCheck: true, skipAoeCheck: true, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, skipTTKCheck: true, usedUp: true)) return true;
            }
            if (IsLastAction(true,DespairPvE) && ManafontPvE.Cooldown.IsCoolingDown && !NextGCDisInstant)
            {
                if (SwiftcastPvE.CanUse(out act)) return true;
            }
            if (InAstralFire && ManafontPvE.Cooldown.IsCoolingDown && CurrentMp < 5000)
            {
                if (LucidDreamingPvE.CanUse(out act)) return true;
            }

        }

            return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        //var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        //var isTargetDying = CurrentTarget?.IsDying() ?? false;
        //var recentActions = RecordActions.Take(4);
        //var lastAction = RecordActions.FirstOrDefault(); // Get the first (most recent) action, or null if the list is empty

        if (DataCenter.PlayerSyncedLevel() < 100)
        {
            if ((InCombat && GetTimeSinceNoHostilesInCombat() > 5f) || (!InCombat && TimeSinceLastAction.TotalSeconds > 4.5))
            {
                if (InUmbralIce && DataCenter.PlayerSyncedLevel() >= 58)
                {
                    if (UmbralIceStacks < 3 || UmbralHearts < 3)
                    {
                        if (UmbralSoulPvE.CanUse(out act)) return true;
                    }
                }
                if (InUmbralIce && DataCenter.PlayerSyncedLevel() >= 35)
                {
                    if (CurrentMp < Player.MaxMp)
                    {
                        if (UmbralSoulPvE.CanUse(out act)) return true;
                    }
                }
                if (InAstralFire)
                {
                    if ((InCombat && GetTimeSinceNoHostilesInCombat() > 5f) || (!InCombat && TimeSinceLastAction.TotalSeconds > 4.5))
                    {
                        if (TransposePvE.CanUse(out act)) return true;
                    }
                }
            }
            if (ShouldThunder)
            {
                if (ThunderIiPvE.CanUse(out act)) return true;
                if (ThunderPvE.CanUse(out act)) return true;
            }
            if (!InUmbralIce && !InAstralFire) // assume just starting dungeon or death
            {

                if (GetAoeCount(FlarePvE) >= 3)
                {
                    if (CurrentMp == 10000)
                    {
                        if (FireIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        if (FirePvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }
                    if (CurrentMp < 10000)
                    {
                        if (BlizzardIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        if (BlizzardPvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }
                }
                if (GetAoeCount(FlarePvE) < 3)
                {
                    if (CurrentMp == 10000)
                    {
                        if (FireIiiPvE.CanUse(out act)) return true;
                        if (FirePvE.CanUse(out act)) return true;
                    }
                    if (CurrentMp < 10000)
                    {
                        if (BlizzardIiiPvE.CanUse(out act)) return true;
                        if (BlizzardPvE.CanUse(out act)) return true;
                    }
                }
            }

            if (InAstralFire)
            {
                if (ShouldTransposeLowLevel)
                {
                    if (TransposePvE.CanUse(out act)) return true;
                    else return false;
                }
                if (GetAoeCount(FlarePvE) >= 3)
                {

                    if (DataCenter.PlayerSyncedLevel() < 58)
                    {
                        if (FoulPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        if (CurrentMp < FireIiPvE.Info.MPNeed)
                        {
                            if (FlarePvE.CanUse(out act, skipAoeCheck: true)) return true;
                        }
                    }
                    if (DataCenter.PlayerSyncedLevel() >= 58)
                    {
                        if (FlarePvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }

                    if (FireIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                    if (FirePvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
                if (GetAoeCount(FlarePvE) < 3)
                {

                    if (ParadoxPvE.CanUse(out act) && ParadoxPvEReady) return true;
                    if (CurrentMp < FireIvPvE.Info.MPNeed)
                    {
                        if (DespairPvE.CanUse(out act)) return true;
                    }
                    if (DataCenter.PlayerSyncedLevel() >= 35 && DataCenter.PlayerSyncedLevel() < 60)
                    {
                        if (CurrentMp < FireIiiPvE.Info.MPNeed)
                        {
                            if (BlizzardIiiPvE.CanUse(out act)) return true;
                        }
                    }
                    if (DataCenter.PlayerSyncedLevel() >= 60)
                    {
                        if (CurrentMp < FireIvPvE.Info.MPNeed)
                        {
                            if (BlizzardIiiPvE.CanUse(out act)) return true;
                        }
                    }
                    if (XenoglossyPvE.CanUse(out act)) return true;
                    if (FoulPvE.CanUse(out act)) return true;
                    if (FireIvPvE.CanUse(out act) && IsSoulStacksMaxed) return true;
                    if (FireIiiPvE.CanUse(out act)) return true;
                    if (FirePvE.CanUse(out act)) return true;
                }
            }

            if (InUmbralIce)
            {

                if (ShouldTransposeLowLevel)
                {
                    act = TransposePvE;
                    return true;
                }
                if ((!TransposePvE.Cooldown.IsCoolingDown || CurrentMp < 5000))
                { 
                if (GetAoeCount(FlarePvE) >= 3 && !ShouldTransposeLowLevel)
                {
                    
                    if (FreezePvE.CanUse(out act, skipAoeCheck: true)) return true;
                    if (BlizzardIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
                    if (GetAoeCount(FlarePvE) < 3 && !ShouldTransposeLowLevel)
                    {
                        if (BlizzardIvPvE.CanUse(out act) && IsSoulStacksMaxed) return true;
                        if (DataCenter.PlayerSyncedLevel() < 35)
                        {
                            if (CurrentMp < 10000)
                            {
                                if (BlizzardIiiPvE.CanUse(out act)) return true;
                                if (BlizzardPvE.CanUse(out act)) return true;
                            }

                        }
                        if (DataCenter.PlayerSyncedLevel() >= 35 && DataCenter.PlayerSyncedLevel() < 99)
                        {
                            if (IsSoulStacksMaxed && CurrentMp == 10000 && (DataCenter.PlayerSyncedLevel() < 58 || UmbralHearts > 0))
                            {
                                if (FireIiiPvE.CanUse(out act)) return true;
                            }
                            if (BlizzardIiiPvE.CanUse(out act)) return true;
                            if (BlizzardPvE.CanUse(out act)) return true;
                        }
                    }
                }
            }

        }

        if (DataCenter.PlayerSyncedLevel() == 100)
        {
            if ((InCombat && GetTimeSinceNoHostilesInCombat() > 5f) || (!InCombat && TimeSinceLastAction.TotalSeconds > 4.5))
            {
                if (InUmbralIce && DataCenter.PlayerSyncedLevel() >= 58)
                {
                    if (UmbralIceStacks < 3 || UmbralHearts < 3)
                    {
                        if (UmbralSoulPvE.CanUse(out act)) return true;
                    }
                }
                if (InUmbralIce && DataCenter.PlayerSyncedLevel() >= 35)
                {
                    if (CurrentMp < Player.MaxMp)
                    {
                        if (UmbralSoulPvE.CanUse(out act)) return true;
                    }
                }
                if (InAstralFire)
                {
                    if ((InCombat && GetTimeSinceNoHostilesInCombat() > 5f) || (!InCombat && TimeSinceLastAction.TotalSeconds > 4.5))
                    {
                        if (TransposePvE.CanUse(out act)) return true;
                    }
                }
            }
            if (GetAoeCount(FlarePvE) >= 3)

            {
                //astral is flare flare flarestar, umbral is freeze, always use foul or high thunder before transpose
                // https://www.thebalanceffxiv.com/img/jobs/blm/black-mage-aoe-rotation.png

                // we need to check if we are running low on filler spells before we start fire phase, polyglot should be 2 or more and if thunder is > 10 seconds that wont refresh either.


                if (InAstralFire)
                {

                    if (FlareStarPvE.CanUse(out act)) return true;
                    if (FlarePvE.CanUse(out act, skipAoeCheck: true)) return true;
                    if (AstralSoulStacks == 0)
                    {
                        if (ThunderIiPvE.CanUse(out act, skipAoeCheck: true) && ShouldThunder) return true;
                        if (WillHave2PolyglotWithin2GCDs)
                        {
                            if (FoulPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        }

                    }
                    //failsafe to use transpose per balance recommendation /Clipping Transpose after Flare Star is only a small clip and allows for conserving filler spells for Umbral Ice, which is needed to wait out the Transpose cooldown/
                    if (AstralSoulStacks != 6 && CurrentMp < 800)
                    {
                        if (TransposePvE.CanUse(out act, skipCastingCheck: true)) return true;
                    }
                }

                if (InUmbralIce)
                {
                    if (UmbralHearts > 0)
                    {
                        if (TransposePvE.CanUse(out act, skipCastingCheck: true)) return true;
                    }
                    //if (lastAction != null && lastAction.Action.RowId == FreezePvE.ID)
                    if (IsLastAction(true, FreezePvE))
                    {
                        if (ThunderIiPvE.CanUse(out act, skipAoeCheck: true) && ShouldThunder) return true;
                        if (FoulPvE.CanUse(out act, skipAoeCheck: true, usedUp: true)) return true;
                        if (ParadoxPvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }
                    if (FreezePvE.CanUse(out act, skipAoeCheck: true) && UmbralHearts == 0) return true;
                }
                //assumes neither, either start of combat in dungeon or death recovery, use high blizard II as there are no other options
                if (!InUmbralIce && !InAstralFire)
                {
                    if (HighBlizzardIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }

            }
            if (GetAoeCount(FlarePvE) >= 2)

            {
                //astral is flare flare flarestar, umbral is freeze, always use foul or high thunder before transpose
                // https://www.thebalanceffxiv.com/img/jobs/blm/black-mage-aoe-rotation.png

                // we need to check if we are running low on filler spells before we start fire phase, polyglot should be 2 or more and if thunder is > 10 seconds that wont refresh either.


                if (InAstralFire)
                {
                    //before we consume umbral hearts lets look if we need to use a triplecast to save resources for umbral ice filler, its in emergency ability section
                    //if (lastAction != null && lastAction.Action.RowId == FlareStarPvE.ID)
                    if (IsLastAction(true, FlareStarPvE))
                    {

                        if (ThunderIiPvE.CanUse(out act, skipAoeCheck: true) && ShouldThunder) return true;
                        if (WillHave2PolyglotWithin2GCDs)
                        {
                            if (FoulPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        }
                        //failsafe to use transpose per balance recommendation /Clipping Transpose after Flare Star is only a small clip and allows for conserving filler spells for Umbral Ice, which is needed to wait out the Transpose cooldown/
                        if (TransposePvE.CanUse(out act, skipCastingCheck: true)) return true;
                    }
                    if (FlareStarPvE.CanUse(out act)) return true;
                    if (FlarePvE.CanUse(out act, skipAoeCheck: true)) return true;
                    if (AstralSoulStacks != 6 && CurrentMp < 800)
                    {
                        if (TransposePvE.CanUse(out act, skipCastingCheck: true)) return true;
                    }

                }

                if (InUmbralIce)
                {
                    if (UmbralHearts > 0)
                    {
                        if (TransposePvE.CanUse(out act, skipCastingCheck: true)) return true;
                    }
                    //if (lastAction != null && lastAction.Action.RowId == FreezePvE.ID)
                    if (IsLastAction(true, FreezePvE))
                    {
                        if (ThunderIiPvE.CanUse(out act, skipAoeCheck: true) && ShouldThunder) return true;
                        if (FoulPvE.CanUse(out act, skipAoeCheck: true, usedUp: true)) return true;
                        if (ParadoxPvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }
                    if (BlizzardIvPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
                //assumes neither, either start of combat in dungeon or death recovery, use blizzard 4

                if (!InUmbralIce && !InAstralFire)
                {
                    if (HighBlizzardIiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
            }
            if (GetAoeCount(FlarePvE) < 2)
            {
                //single target section starts here
                if (ThunderPvE.CanUse(out act) && ShouldThunder) return true;
                if (ShouldXeno)
                {
                    if (XenoglossyPvE.CanUse(out act, usedUp: ShouldXeno)) return true;
                }
                if (InAstralFire)
                {
                    if (Openerchoice == Openchoice.AltFlare && IsInOpener)
                    {
                        if (AstralSoulStacks == 4 && CombatTime < 30 && ManafontPvE.Cooldown.HasOneCharge)
                        {
                            if (DespairPvE.CanUse(out act)) return true;
                        }

                        if (ManafontPvE.Cooldown.IsCoolingDown && ManafontPvE.Cooldown.RecastTimeElapsed > 6 && AstralSoulStacks == 4)
                        {
                            if (ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                            if (AltFlareOpenerPvE.CanUse(out act, skipAoeCheck: true)) return true;
                        }

                    }
                    if (AstralFireStacks < 3)
                    {
                        if (HasFire)
                        {
                            if (FireIiiPvE.CanUse(out act)) return true;
                        }
                        if (ParadoxPvE.CanUse(out act)) return true;
                    }
                    if (WillBeAbleToFlareStarMT && !WillBeAbleToFlareStarST)
                    {
                        if (AltFlareOpenerPvE.CanUse(out act, skipAoeCheck: true)) return true;
                    }

                    if (CurrentMp < FireIvPvE.Info.MPNeed && (!IsParadoxActive || CurrentMp < ParadoxPvE.Info.MPNeed) && AstralSoulStacks < 6)
                    {
                        if (DespairPvE.CanUse(out act)) return true;
                    }
                    if (FlareStarPvE.CanUse(out act)) return true;
                    if (FireIvPvE.CanUse(out act) && CurrentMp >= FireIvPvE.Info.MPNeed + (IsParadoxActive && AstralSoulStacks != 5 ? 1600 : 0)) return true;
                    if (ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                    if (!NextGCDisInstant)
                    {
                        if (SwiftcastPvE.Cooldown.IsCoolingDown)
                        {
                            if (BlizzardIiiPvE.CanUse(out act)) return true; // skip transpose if we can't make b3 instant per balance stuff
                        }
                    }


                    if (InCombat && IsMoving && !NextGCDisInstant && HasHostilesInRange && NextAbilityToNextGCD < 0.5)
                    {
                        if (PolyglotStacks > 0)
                        {
                            if (XenoglossyPvE.CanUse(out act, usedUp: true)) return true;
                        }
                        if (CanMakeInstant)
                        {

                            if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;

                            if (SwiftcastPvE.CanUse(out act)) return true;
                        }
                    }
                }

                if (InUmbralIce)
                {
                    if (UmbralIceStacks < 3)
                    {
                        if (BlizzardIiiPvE.CanUse(out act)) return true;
                    }

                    if (UmbralHearts < 3)
                    {
                        if (BlizzardIvPvE.CanUse(out act)) return true;
                    }
                    if (IsParadoxActive)
                    {
                        if (ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true)) return true;
                    }

                    if (BlizzardIiiPvE.CanUse(out act) && CurrentMp < 10000) return true;

                }
                if (!InUmbralIce && !InAstralFire)
                {
                    if (BlizzardIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;
                }
            }
        }
        return base.GeneralGCD(out act);
    }

    #endregion

    #region Black Magic


    #endregion

    public unsafe override void DisplayRotationStatus()
    {
        //motif
        ImGui.Text("GCDTime " + GCDTime());
        ImGui.Text($"Last Action: {RecordActions?.FirstOrDefault()?.Action.RowId}");
        ImGui.Text(" currentmp " + CurrentMp);
        ImGui.Text(" Player.CurrentMP " + Player.CurrentMp);
        ImGui.Text("iscasting " + Player.IsCasting);
        ImGui.Text("FlareAoeNumber " + GetAoeCount(FlarePvE));
        ImGui.Text("falre aoe range " + FlarePvE.Info.EffectRange);
        ImGui.Text("Player.BaseCastTime " + Player.BaseCastTime);
        ImGui.Text("Player.CurrentCastTime) " + Player.CurrentCastTime);
        ImGui.Text("Player.TotalCastTime " + Player.TotalCastTime);

        base.DisplayRotationStatus();
    }
}