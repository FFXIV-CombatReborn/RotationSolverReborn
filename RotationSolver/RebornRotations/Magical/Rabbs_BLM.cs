using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Utility.Signatures;
using ECommons;
using ExCSS;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using RotationSolver.Basic.Actions;
using RotationSolver.Basic.Data;
using RotationSolver.Basic.Rotations.Basic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Serialization;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.DataCenterHelper;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using LuminaAction = Lumina.Excel.Sheets.Action;
using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Collections.Concurrent;
using ActionSetting = RotationSolver.Basic.Actions.ActionSetting;




namespace RabbsRotationsNET8.Magical;
[Rotation("Rabbs Black Mage", CombatType.PvE, GameVersion = "7.3")]
[SourceCode(Path = "main/RebornRotations/Magical/Rabbs_BLM.cs")]
[ExtraRotation]

public sealed class BLM_Gamma : BlackMageRotation
{
    #region Config Options


    [RotationConfig(CombatType.PvE, Name = "Use Countdown Ability (Fire 3)")]
    public bool Usecountdown { get; set; } = false;

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


    [RotationConfig(CombatType.PvE, Name = "How to use pots)")]
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

    private static void ModifyAltFlareOpenerPvE(ref RotationSolver.Basic.Actions.ActionSetting setting)
    {
        setting.RotationCheck = () => InAstralFire;
        setting.UnlockedByQuestID = 66614u;

    }

    private readonly Lazy<IBaseAction> _AltFlareOpenerPvE = new Lazy<IBaseAction>(delegate
    {
        IBaseAction action460 = new BaseAction(ActionID.FlarePvE);
        RotationSolver.Basic.Actions.ActionSetting setting460 = action460.Setting;
        ModifyAltFlareOpenerPvE(ref setting460);
        action460.Setting = setting460;
        return action460;
    });


    private static readonly HashSet<uint> burstStatusIds = new HashSet<uint>
{
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

};

    public bool isPartyBurst => PartyMembers?.Any(member =>
        member?.StatusList?.Any(status => burstStatusIds.Contains(status.StatusId)) == true
    ) == true;

    public bool isPartyMedicated => PartyMembers?.Any(member =>
    member?.StatusList?.Any(status => status.StatusId == (uint)StatusID.Medicated) == true
) == true;

    



    public bool isAnyBossinRange => AllHostileTargets is not null && AllHostileTargets.Any(hostile => hostile.IsBossFromIcon() || hostile.IsBossFromTTK());

    public bool isCurrentTargetBoss => CurrentTarget is not null && (CurrentTarget.IsBossFromIcon() || CurrentTarget.IsBossFromTTK());

    public bool isOpenerChosen => (When2Open == OpenWhen.BossIsTarget && isCurrentTargetBoss) || (When2Open == OpenWhen.BossInRoom && isAnyBossinRange) || When2Open == OpenWhen.Allday;

    public bool isInOpener => isOpenerChosen && CombatTime > 0 && CombatTime < 60 && InCombat && !Player.HasStatus(true, StatusID.BrinkOfDeath, StatusID.Weakness);

    public bool isPotReady => (Poterchoice == Potchoice.WithOthers && isPartyMedicated) || (Poterchoice == Potchoice.Q2M && IsWithinFirst15SecondsOfEvenMinute()) || (Poterchoice == Potchoice.Allday);

    public bool isBurstReady => (When2Burst == BurstWhen.WithOthers && isPartyBurst) || (When2Burst == BurstWhen.Q2M && IsWithinFirst15SecondsOfEvenMinute()) || (When2Burst == BurstWhen.Allday);


    #endregion

    #region underhood stuff
    public bool TargetHasThunderDebuff => Target is not null && Target.HasStatus(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder);
    public bool ThunderBuffAboutToFallOff => Target is not null && TargetHasThunderDebuff && Target.StatusTime(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder) < 3;
    public bool ThunderBuffMoreThan10 => Target is not null && TargetHasThunderDebuff && Target.StatusTime(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder) > 10;
    /*
    public float ThunderTime => (Target is not null && TargetHasThunderDebuff) ? Target.StatusTime(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder):0;
    public int ThunderTicks
    {
        get
        {
            if (ThunderTime > 0 && ThunderTime <= 3.0f)
            {
                return 1;
            }
            return (int)(ThunderTime / 3.0f);
        }
    }
    */
    //public bool ShouldThunder => Player.HasStatus(true, StatusID.Thunderhead) && (!TargetHasThunderDebuff || ThunderBuffAboutToFallOff);
    public bool ShouldThunder
    {
        get
        {
            if (isInOpener)
            {
                int currentGcdEstimate = (int)(CombatTime / 2.5);

                return Player.HasStatus(true, StatusID.Thunderhead) &&
                       currentGcdEstimate >= 9 && currentGcdEstimate <= 12;
            }
            else
            {
                return Player.HasStatus(true, StatusID.Thunderhead) &&
                       (!TargetHasThunderDebuff || ThunderBuffAboutToFallOff);
            }
        }
    }
    private Stopwatch? noHostilesTimer = null;

    public double GetTimeSinceNoHostilesInCombat()
    {
        if (InCombat && NumberOfAllHostilesInMaxRange == 0)
        {
            if (noHostilesTimer == null)
            {
                noHostilesTimer = Stopwatch.StartNew();
            }
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

    public bool willHave2PolyglotWithin6GCDs => (PolyglotStacks == 1 && EnochianTime < 6 * 2.5) || PolyglotStacks >= 2;

    public bool willHave2PolyglotWithin2GCDs => (PolyglotStacks == 1 && EnochianTime < 2 * 2.5) || PolyglotStacks >= 2;

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

            if (CurrentMp > howMuchManaINeed)
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
                if (CurrentMp > 800)
                {
                    return true;
                }


            return false; // If we can afford all the needed Flare casts
        }
    }







    public int GetAoeCount(IBaseAction action)
    {
        int maxAoeCount = 0;
        if (!CustomRotation.IsManual)
        {
            if (AllHostileTargets != null && action.Target.Target != null) // Use action.Target.Target
            {
                maxAoeCount = AllHostileTargets.Count(otherTarget =>
                    Vector3.Distance(action.Target.Target.Position, otherTarget.Position) < (action.TargetInfo.EffectRange + otherTarget.HitboxRadius));
            }
        }
        else if (AllHostileTargets != null && CurrentTarget != null) // Use action.Target.Target
        {
            maxAoeCount = AllHostileTargets.Count(otherTarget =>
                Vector3.Distance(CurrentTarget.Position, otherTarget.Position) < (action.TargetInfo.EffectRange + otherTarget.HitboxRadius));
        }

        return maxAoeCount;
    }

    public bool shouldTranspose
    {
        get
        {
            //var recentActions = RecordActions.Take(2);
            //var lastAction = RecordActions.FirstOrDefault(); // Get the first (most recent) action, or null if the list is empty
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
                if (GetAoeCount(FlarePvE) >= 3 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && (IsLastAction(true, FoulPvE) || IsLastAction(true, ThunderIiiPvE) || IsLastAction(true, ParadoxPvE)))
                {
                    return true;
                }
            }
            if (GetTimeSinceNoHostilesInCombat() > 5f) //we are in combat but nothing to attack for 5 seconds
            {
                if (!IsParadoxActive)
                { return true; }
                if (!HasThunder)
                { return true; }
            }
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
                if (UmbralHearts == 3 && UmbralIceStacks == 3 && !IsParadoxActive)
                { return true; }
            }

            return false;
        }
    }

    public bool shouldXeno
    {
        get
        {
            if (PolyglotStacks == 3)
            { return true; }

            if (PolyglotStacks > 0 && isBurstReady && CombatTime > 60)
            {
                if (ChoiceBurst == Burstchoice.Both || ChoiceBurst == Burstchoice.XenoOnly)
                { return true; }
            }
            if (PolyglotStacks > 0 && isInOpener && CombatTime < 30 && Openerchoice == Openchoice.Standard && CurrentMp < FireIvPvE.Info.MPNeed)
                return true;
            if (PolyglotStacks > 0 && isInOpener && CombatTime < 30 && Openerchoice == Openchoice.AltFlare && AstralSoulStacks == 2)
                return true;

            return false;
        }
    }

    public bool shouldLeyLine
    {
        get
        {
            if (LeyLinesPvE.Cooldown.CurrentCharges == LeyLinesPvE.Cooldown.MaxCharges && !Player.HasStatus(true, StatusID.LeyLines) && (ChoiceBurst == Burstchoice.Leylines || ChoiceBurst == Burstchoice.Both) && When2Burst == BurstWhen.PreventCap)
            { return true; }

            if (LeyLinesPvE.Cooldown.CurrentCharges > 0 && isBurstReady && !Player.HasStatus(true, StatusID.LeyLines))
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
                        if (TriplecastPvE.CanUse(out act, usedUp: true)) return act;
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

        if (shouldTranspose)
        {
            if (TransposePvE.CanUse(out act, skipCastingCheck: true, skipComboCheck: true, skipAoeCheck: true, skipStatusProvideCheck: true, skipTargetStatusNeedCheck: true, skipTTKCheck: true, usedUp: true)) return true;
        }
        if (nextGCD.IsTheSameTo(true, BlizzardIiiPvE))
        {
            if (!NextGCDisInstant && CanMakeInstant && InCombat)
            {
                if (isInOpener)
                {
                    if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                }
                if (SwiftcastPvE.CanUse(out act)) return true;
                if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
            }
            


        }
        if (!NextGCDisInstant && CanMakeInstant && InCombat && InAstralFire)
        {
            if (CurrentMp < 800 && AstralSoulStacks < 6 && !WillBeAbleToFlareStarMT && !WillBeAbleToFlareStarST && (!IsParadoxActive || CurrentMp < 1600) && ManafontPvE.Cooldown.IsCoolingDown)
            {

                if (SwiftcastPvE.CanUse(out act)) return true;
                if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
            }
        }
        #region Opener
        if (isInOpener)
        {
            if (AmplifierPvE.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.LeyLines))
            {
                if (LeyLinesPvE.CanUse(out act)) return true;
            }
        }
        if (InAstralFire)
        {

            if (isInOpener)
            {
                if (!NextGCDisInstant && CombatTime < 30)
                {
                    if (SwiftcastPvE.CanUse(out act)) return true;
                }
                if (!IsPolyglotStacksMaxed)
                {
                    if (AmplifierPvE.CanUse(out act)) return true;
                }
                if (isPotReady && UseBurstMedicine(out act)) return true;
            }



        }

        #endregion
        if (nextGCD.IsTheSameTo(true, FlarePvE) && isInOpener && Openerchoice == Openchoice.AltFlare && InCombat)
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
                    if (nextGCD.IsTheSameTo(true, FlarePvE) && ThunderBuffMoreThan10 && !willHave2PolyglotWithin6GCDs) //checking if we won't need to refresh thunder AND we wont have foul after freeze (6gcd's)
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



        if (shouldLeyLine && InCombat && HasHostilesInRange && LeyLinesPvE.CanUse(out act, usedUp: shouldLeyLine)) return true;
        //if (!IsLastAbility(ActionID.LeyLinesPvE) && UseRetrace && InCombat && HasHostilesInRange && !Player.HasStatus(true, StatusID.CircleOfPower) && RetracePvE.CanUse(out act)) return true;

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RetracePvE, ActionID.SwiftcastPvE, ActionID.TriplecastPvE, ActionID.AmplifierPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {

        if (InCombat && HasHostilesInRange)
        {
            if (InUmbralIce)
            {
                if (IsLastAction(ActionID.TransposePvE))
                {
                    if (CanMakeInstant)
                    {
                        if (SwiftcastPvE.CanUse(out act)) return true;
                        if (TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                    }
                }
            }

            if (CombatTime > 65 || When2Open == OpenWhen.Never)
            {
                if (!IsPolyglotStacksMaxed)
                {
                    if (AmplifierPvE.CanUse(out act)) return true;
                }
                if (isPotReady && UseBurstMedicine(out act)) return true;
            }

        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool GeneralGCD(out IAction? act)
    {


        var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        var isTargetDying = CurrentTarget?.IsDying() ?? false;
        //var recentActions = RecordActions.Take(4);
        //var lastAction = RecordActions.FirstOrDefault(); // Get the first (most recent) action, or null if the list is empty

        


        if ((InCombat && GetTimeSinceNoHostilesInCombat() > 5f) || (!InCombat && TimeSinceLastAction.TotalSeconds > 4.5))
        {
            if (InUmbralIce)
            {
                if (UmbralIceStacks < 3 || UmbralHearts < 3)
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
                    if (willHave2PolyglotWithin2GCDs)
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
                if (FreezePvE.CanUse(out act, skipAoeCheck: true)) return true;
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
                    if (willHave2PolyglotWithin2GCDs)
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
            if (shouldXeno)
            {
                if (XenoglossyPvE.CanUse(out act, usedUp: shouldXeno)) return true;
            }
            if (InAstralFire)
            {
                if (Openerchoice == Openchoice.AltFlare && isInOpener)
                {
                    if (AstralSoulStacks == 4 && CombatTime < 30 && ManafontPvE.Cooldown.HasOneCharge)
                    {
                        if (DespairPvE.CanUse(out act)) return true;
                    }

                    if (ManafontPvE.IsInCooldown && ManafontPvE.Cooldown.RecastTimeElapsed > 6 && AstralSoulStacks == 4)
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
                    if (!CanMakeInstant)
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

                if (BlizzardIiiPvE.CanUse(out act)) return true;

            }
            if (!InUmbralIce && !InAstralFire)
            {
                if (BlizzardIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;
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
        ImGui.Text("WeaponElapsed " + WeaponElapsed);
        ImGui.Text("FlareAoeNumber " + GetAoeCount(FlarePvE));


        base.DisplayRotationStatus();
    }
}