using System.ComponentModel;
using RotationSolver.Updaters;

namespace RotationSolver.ExtraRotations.Melee;

[Rotation("Churin MNK", CombatType.PvE, GameVersion = "7.4", Description = "An eye for an eye. A tooth for a tooth. An eye and a tooth for a loaf of bread. Eyes and teeth are the new currency.")]
[SourceCode(Path = "main/ExtraRotations/Melee/ChurinMNK.cs")]
public sealed class ChurinMNK : MonkRotation
{
    #region Properties

    #region Enums
    private enum OpenerType : byte
    {
        [Description("Double Lunar")] DoubleLunar,
        [Description("Solar Lunar")] SolarLunar,
        [Description("Triple Lunar")] TripleLunar
    }

    private enum OpenerVariation : byte
    {
        [Description("Dragon Kick - 5s")] DragonKick5,
        [Description("Dragon Kick - 7s")] DragonKick7,
        [Description("Demolish - 7s")] Demolish7
    }

    private enum Nadi : byte
{
    [Description("None")] None,
    [Description("Lunar")] Lunar,
    [Description("Solar")] Solar,
}

    private enum Blitz : byte
    {
        [Description("None")] None,
        [Description("Elixir Burst")] ElixirBurst,         // Grants Lunar
        [Description("Rising Phoenix")] RisingPhoenix,     // Grants Solar
        [Description("Phantom Rush")] PhantomRush,         // Consumes Both
    }

    #endregion

    #region States

    private Nadi NextNadiGoal { get; set; } = Nadi.Lunar;
    private Blitz NextBlitz { get; set; }
    private bool PhantomRushed { get; set; }
    private bool LunarOddWindow => NextNadiGoal == Nadi.Lunar
                                   && BrotherhoodPvE.Cooldown.RecastTimeElapsedRaw is >30f and < 90f;
    private bool SolarOddWindow => NextNadiGoal == Nadi.Solar
                                   && BrotherhoodPvE.Cooldown.RecastTimeElapsedRaw is >30f and < 90f;
    private static bool HasBlitzReady => !BeastChakrasContains(BeastChakra.None);
    private static bool IsReadySoon(IBaseAction action, int maxGCD)
    {
        var gcdTotal = WeaponTotal;
        const float Buffer = 0.6f;

        for (var i = 0; i <= maxGCD; i++)
        {
            var deadLine = gcdTotal * i + (gcdTotal - Math.Abs(WeaponRemain - Buffer));
            if (action.Cooldown.WillHaveOneCharge(deadLine)) return true;
        }
        return false;
    }
    private static int IsReadyIndex(IBaseAction action, int maxGCDs)
    {
        var gcdTotal = WeaponTotal;
        const float Buffer = 0.6f;

        for (var i = 0; i <= maxGCDs; i++)
        {
            var deadline = gcdTotal * i + (gcdTotal - Buffer + WeaponRemain);
            if (action.Cooldown.RecastTimeRemain <= deadline) return i;
        }
        return -1;
    }
    private bool IsCooldownAligned(int range)
    {
        var bhIndex = IsReadyIndex(BrotherhoodPvE, range);
        var rofIndex = IsReadyIndex(RiddleOfFirePvE, range);

        if (bhIndex == -1 || rofIndex == -1) return false;

        var difference = Math.Abs(rofIndex - bhIndex);
        return difference <= 0.6f;
    }
    private bool CanBurst => MergedStatus.HasFlag(AutoStatus.Burst) && BrotherhoodPvE.IsEnabled;
    private static bool InBurst => HasBrotherhood && HasRiddleOfFire;
    private bool MustUseOpo => HasFormlessFist || IsLastGCD(true, FiresReplyPvE, MasterfulBlitzPvE, ElixirBurstPvE,
        RisingPhoenixPvE, PhantomRushPvE);
    private static bool IsNextGCDOpo => ActionUpdater.NextGCDAction != null &&
                                        ActionUpdater.NextGCDAction.IsTheSameTo(true, ActionID.DragonKickPvE,
                                            ActionID.LeapingOpoPvE, ActionID.BootshinePvE,
                                            ActionID.ShadowOfTheDestroyerPvE, ActionID.ArmOfTheDestroyerPvE);
    private bool IsLastGCDMasterfulBlitz => IsLastGCD(true, ElixirBurstPvE, PhantomRushPvE, RisingPhoenixPvE);
    private bool IsLastGCDOpo => IsLastGCD( true, DragonKickPvE, LeapingOpoPvE, BootshinePvE, ShadowOfTheDestroyerPvE, ArmOfTheDestroyerPvE);
    private static bool PerfectBalanceStacks(int stacks) => StatusHelper.PlayerStatusStack(true, StatusID.PerfectBalance) == stacks;
    private static bool CanLateWeave => WeaponRemain <= LateWeaveWindow;
    private static bool CanEarlyWeave => WeaponRemain >= LateWeaveWindow;
    private const float LateWeaveWindow = 1.15f;
    private static bool EnoughWeaveTime => WeaponRemain > 0.75f;
    private static bool IsOpenerStart => InCombat && CombatTime < 5.0f;
    private static bool HasBothNadi => HasLunar && HasSolar;
    private static bool HasNoNadi => !HasLunar && !HasSolar;
    private int BlitzCount { get; set; }
    private bool _canIncrement;
    private const float BossHealthThreshold = 0.1f;

    #endregion

    #region Updaters

    private void RotationUpdater()
    {
        if (!InCombat && HasNoNadi && OpoOpoFury == 0 && RaptorFury == 0 && CoeurlFury == 0)
        {
            ResetState();
            return;
        }

        if (IsLastGCD(true, PhantomRushPvE))
        {
            BlitzCount = 0; // Reset loop
            PhantomRushed = true;
        }

        var incrementTrigger = IsLastGCD(true, ElixirBurstPvE, RisingPhoenixPvE);

        if (!_canIncrement && incrementTrigger)
        {
                        BlitzCount++;
        }

        _canIncrement = incrementTrigger;


        // Determine the next Blitz based on the Opener or Standard Rotation
        NextBlitz = GetNextBlitz();

        // Determine the Nadi Goal based on the Blitz we want to execute
        NextNadiGoal = NextBlitz switch
        {
            Blitz.ElixirBurst => Nadi.Lunar,
            Blitz.RisingPhoenix => Nadi.Solar,
            Blitz.PhantomRush => Nadi.Lunar,
            _ => Nadi.None
        };
    }
    private Blitz GetNextBlitz()
    {
    // 1. Handle Openers (Fixed Sequences)
        if (!PhantomRushed)
        {
            return (ChosenOpener, BlitzCount) switch
            {
                // Solar Lunar: Solar -> Lunar -> Phantom Rush
                (OpenerType.SolarLunar, 0) => Blitz.RisingPhoenix,
                (OpenerType.SolarLunar, 1) => Blitz.ElixirBurst,
                (OpenerType.SolarLunar, 2) => Blitz.PhantomRush,

                // Double Lunar: Lunar -> Lunar -> Solar -> Phantom Rush
                (OpenerType.DoubleLunar, 0) => Blitz.ElixirBurst,
                (OpenerType.DoubleLunar, 1) => Blitz.ElixirBurst,
                (OpenerType.DoubleLunar, 2) => Blitz.RisingPhoenix,
                (OpenerType.DoubleLunar, 3) => Blitz.PhantomRush,

                // Triple Lunar (Theoretical/Niche): Lunar x3 -> Solar -> PR
                (OpenerType.TripleLunar, 0) => Blitz.ElixirBurst,
                (OpenerType.TripleLunar, 1) => Blitz.ElixirBurst,
                (OpenerType.TripleLunar, 2) => Blitz.ElixirBurst,
                (OpenerType.TripleLunar, 3) => Blitz.RisingPhoenix,
                (OpenerType.TripleLunar, 4) => Blitz.PhantomRush,

                // Default fallback if counts go out of bounds before PR
                _ => Blitz.None
        };
    }

        // 2. Handle Standard Loop (Post-Opener)
        // Logic: Fill missing Nadi -> Phantom Rush -> Repeat
        return (HasLunar, HasSolar) switch
        {
            (true, true) => Blitz.PhantomRush,      // Have both? Rush.
            (false, false) => Blitz.ElixirBurst,    // Have neither? Default to Lunar (Standard Loop start).
            (true, false) => Blitz.RisingPhoenix,   // Have Lunar? Get Solar.
            (false, true) => Blitz.ElixirBurst,     // Have Solar? Get Lunar.
        };
    }
    private void ResetState()
{
    BlitzCount = 0;
    PhantomRushed = false;
    _canIncrement = false;

    // Pre-combat prep: Set initial Blitz based on opener preference
    NextBlitz = ChosenOpener switch
    {
        OpenerType.SolarLunar => Blitz.RisingPhoenix,
        _ => Blitz.ElixirBurst // Double and Triple Lunar start with Elixir Burst
    };

    NextNadiGoal = NextBlitz == Blitz.RisingPhoenix ? Nadi.Solar : Nadi.Lunar;
}
    private static string CheckFuryGaugeState()
    {

        if (OpoOpoFury > 0)
        {
            return "Coeurl, Raptor, Opo";
        }

        if (RaptorFury > 0 || (OpoOpoFury == 0 && CoeurlFury == 0 && RaptorFury == 0))
        {
            return "Opo, Coeurl, Raptor";
        }

        return CoeurlFury > 0 ? "Opo, Raptor, Coeurl" : "Coeurl, Raptor, Opo";
    }
    #endregion

    #endregion

    #region Tracking Properties
    /// <summary>
    /// Displays the current rotation status in the UI.
    /// </summary>
    // csharp
    public override void DisplayRotationStatus()
    {
        // Colors
        var green = new Vector4(0.22f, 0.85f, 0.32f, 1f);
        var red = new Vector4(0.95f, 0.28f, 0.28f, 1f);
        var yellow = new Vector4(0.98f, 0.78f, 0.18f, 1f);
        var blue = new Vector4(0.33f, 0.66f, 0.95f, 1f);
        var gray = new Vector4(0.75f, 0.75f, 0.75f, 1f);

        if (!ImGui.BeginTable("Rotation Status", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
        {
            ImGui.EndTable();
            return;
        }

        ImGui.TableSetupColumn("Property");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();

        // Header / meta
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("Rotation Snapshot");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextDisabled($"Updated: {DateTime.Now:T}");

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("— Performance");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(string.Empty);

        AddTableRowColored("Weapon Total", $"{WeaponTotal:F2}s", blue);
        AddTableRowColored("Animation Lock", $"{AnimationLock:F2}s", gray);
        AddTableRowColored("Late Weave Window", $"{LateWeaveWindow:F2}s", gray);

        // Weave hints
        AddTableRowColored("Can Early Weave", CanEarlyWeave ? "Yes" : "No", CanEarlyWeave ? green : red);
        AddTableRowColored("Enough Weave Time", EnoughWeaveTime ? "Yes" : "No", EnoughWeaveTime ? green : red);

        // Nadi / Blitz
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("— Nadi & Blitz");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(string.Empty);

        var oddWindow = LunarOddWindow ? "Lunar Odd Window" : SolarOddWindow ? "Solar Odd Window" : "Normal Window";
        var oddWindowColor = LunarOddWindow ? blue : SolarOddWindow ? yellow : red;
        AddTableRowColored("Odd Window", oddWindow, oddWindowColor);

        var nadiLabel = HasNoNadi ? "No Nadi" : HasBothNadi ? "Both" : HasLunar ? "Lunar" : "Solar";
        var nadiColor = HasNoNadi ? gray : HasBothNadi ? yellow : HasLunar ? blue : new Vector4(1f, 0.55f, 0.12f, 1f);
        AddTableRowColored("Nadi State", nadiLabel, nadiColor);

        var blitzReadyLabel = ElixirBurstPvEReady ? "Elixir Burst" : RisingPhoenixPvEReady ? "Rising Phoenix" : PhantomRushPvEReady ? "Phantom Rush" : "None";
        var blitzReadyColor = ElixirBurstPvEReady || RisingPhoenixPvEReady || PhantomRushPvEReady ? green : gray;
        AddTableRowColored("Next Blitz", $"{NextBlitz} (Count: {BlitzCount})", blitzReadyColor);
        AddTableRowColored("Blitz Ready", blitzReadyLabel, blitzReadyColor);

        AddTableRowColored("Next Nadi Goal", $"{NextNadiGoal}", nadiColor);
        AddTableRowColored("Has Used Phantom Rush?", PhantomRushed ? "Yes ✓" : "No ✗", PhantomRushed ? gray : green);

        // Forms & Fury
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("— Forms & Fury");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(string.Empty);

        var formLabel = InOpoopoForm ? "Opo-Opo" : InRaptorForm ? "Raptor" : InCoeurlForm ? "Coeurl" : "None";
        AddTableRowColored("Current Form", formLabel, InOpoopoForm || InRaptorForm || InCoeurlForm ? yellow : gray);

        AddTableRowColored("Beast Chakras", $"{BeastChakras[0]}, {BeastChakras[1]}, {BeastChakras[2]}", gray);
        AddTableRowColored("Fury Gauge State", CheckFuryGaugeState(), gray);

        // GCD / Opo checks
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("— GCD / Opener Hints");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(string.Empty);

        AddTableRowColored("Is Last GCD Opo?", IsLastGCDOpo ? "Yes ✓" : "No ✗", IsLastGCDOpo ? yellow : gray);
        AddTableRowColored("Is Next GCD Opo", IsNextGCDOpo ? "Yes ✓" : "No ✗", IsNextGCDOpo ? yellow : gray);

        // Small helper badge row for important cooldowns
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted("Key Cooldowns");
        ImGui.TableSetColumnIndex(1);
        ImGui.BeginGroup();
        AddBadge("Brotherhood", BrotherhoodPvE.IsEnabled && BrotherhoodPvE.Cooldown.HasOneCharge, green, gray);
        ImGui.SameLine();
        AddBadge("Riddle of Fire", RiddleOfFirePvE.IsEnabled && RiddleOfFirePvE.Cooldown.HasOneCharge, green, gray);
        ImGui.SameLine();
        // Ensure we pass both active and inactive colors (fourth param) to match AddBadge signature
        AddBadge("Perfect Balance", PerfectBalancePvE.IsEnabled && PerfectBalancePvE.Cooldown.HasOneCharge, BrotherhoodPvE.IsEnabled ? yellow : gray, gray);
        ImGui.EndGroup();

        ImGui.EndTable();
    }

    // Helper: colored table row
    private static void AddTableRowColored(string label, string value, Vector4 color)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(value);
        ImGui.PopStyleColor();
    }

    // Helper: small badge (simple rectangular label)
    private static void AddBadge(string text, bool active, Vector4 activeColor, Vector4 inactiveColor)
    {
        var color = active ? activeColor : inactiveColor;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(active ? $"[{text} ✓]" : $"[{text} ✗]");
        ImGui.PopStyleColor();
    }

    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "Choose Opener.")]
    private OpenerType ChosenOpener { get; set; } = OpenerType.DoubleLunar;

    [RotationConfig(CombatType.PvE, Name = "Choose Opener Variation")]
    private OpenerVariation ChosenVariation { get; set; } = OpenerVariation.DragonKick5;


    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if  (remainTime <= 0.8f && ThunderclapPvE.CanUse(out var act)
            || remainTime <= 2 && TrueNorthPvE.CanUse(out act)
            || remainTime <= 5 && Chakra < 5 && TryUseMeditations(out act))
        {
            return act;
        }

        return remainTime < 15 && FormShiftPvE.CanUse(out act) ? act : base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return TryUseBuffs(out act)
               || TryUsePerfectBalance(out act)
               || base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ThunderclapPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return ThunderclapPvE.CanUse(out act) || base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeintPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!EnoughWeaveTime) return false;
        return FeintPvE.CanUse(out act) || base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.MantraPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!EnoughWeaveTime) return false;
        if (EarthsReplyPvE.CanUse(out act))
        {
            return true;
        }

        return MantraPvE.CanUse(out act) || base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RiddleOfEarthPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (!EnoughWeaveTime) return false;
        return RiddleOfEarthPvE.CanUse(out act, usedUp: true) || base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return TryUseRiddleOfWind(out act)
               || TryUseForbiddenChakra(out act)
               || base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        RotationUpdater();

        if (MustUseOpo)
        {
            return CombatElapsedLessGCD(1) ? TryUseOpenerVariation(out act) : TryUseOpoOpo(out act);
        }

        if (TryUseWindsReply(out act)) return true;
        if (TryUseFiresReply(out act)) return true;

        return  TryGenerateNadi(out act)
            || TryUseMasterfulBlitz(out act)
            || TryUseFiller(out act)
            || TryUseMeditations(out act)
            || base.GeneralGCD(out act);
    }

    #endregion

    #region  Extra Methods

    #region Form Execution
    private bool TryUseMeditations(out IAction? act)
    {
        act = null;
        if (InCombat && HasHostilesInRange) return false;

        if ((!HasHostilesInRange || !InCombat) && Chakra < 5)
        {
            return EnlightenedMeditationPvE.CanUse(out act)
                   || ForbiddenMeditationPvE.CanUse(out act)
                   || InspiritedMeditationPvE.CanUse(out act)
                   || !ForbiddenMeditationPvE.Info.EnoughLevelAndQuest() && SteeledMeditationPvE.CanUse(out act);
        }

        return false;
    }
    private bool TryUseOpoOpo(out IAction? act)
    {
        act = null;
        if (InOpoopoForm || HasFormlessFist || HasPerfectBalance)
        {
            if (ArmOfTheDestroyerPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }

            switch (OpoOpoFury)
            {
                case > 0 when LeapingOpoPvE.EnoughLevel:
                    return LeapingOpoPvE.CanUse(out act, skipComboCheck: true);
                case > 0:
                    return BootshinePvE.CanUse(out act, skipComboCheck: true);
                case 0:
                    return DragonKickPvE.CanUse(out act, skipComboCheck: true);
            }
        }
        return false;
    }
    private bool TryUseRaptor(out IAction? act)
    {
        act = null;
        if (InRaptorForm || HasFormlessFist ||HasPerfectBalance)
        {
            if (FourpointFuryPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }

            switch (RaptorFury)
            {
                case > 0 when RisingRaptorPvE.EnoughLevel:
                    return RisingRaptorPvE.CanUse(out act, skipComboCheck: true);
                case > 0:
                    return TrueStrikePvE.CanUse(out act, skipComboCheck: true);
                case 0:
                    return TwinSnakesPvE.CanUse(out act, skipComboCheck: true);

            }
        }
        return false;
    }
    private bool TryUseCoeurl(out IAction? act)
    {
        act = null;
        if (InCoeurlForm || HasFormlessFist ||HasPerfectBalance)
        {
            if (RockbreakerPvE.CanUse(out act, skipComboCheck: true))
            {
                return true;
            }

            switch (CoeurlFury)
            {
                case > 0 when PouncingCoeurlPvE.EnoughLevel:
                    return PouncingCoeurlPvE.CanUse(out act, skipComboCheck: true);
                case > 0:
                    return SnapPunchPvE.CanUse(out act, skipComboCheck: true);
                case 0:
                    return DemolishPvE.CanUse(out act, skipComboCheck: true);
            }

        }

        return false;
    }
    private bool TryUseFiller(out IAction? act)
    {

        return TryUseOpoOpo(out act)
               || TryUseRaptor(out act)
               || TryUseCoeurl(out act)
               || TryUseFormShift(out act);

    }
    private bool TryUseOpenerVariation(out IAction? act)
    {
        act = null;
        if (!CombatElapsedLessGCD(1)) return false;

        return ChosenVariation switch
        {
            OpenerVariation.Demolish7 => TryUseCoeurl(out act),
            OpenerVariation.DragonKick5 => TryUseOpoOpo(out act),
            OpenerVariation.DragonKick7 => TryUseOpoOpo(out act),
            _ => false
        };
    }

    #endregion

    #region Perfect Balance / Nadi Generation

    private bool TryGenerateNadi(out IAction? act)
    {
        act = null;
                if (!HasPerfectBalance || !BeastChakrasContains(BeastChakra.None)) return false;

        return NextNadiGoal switch
        {
            Nadi.None => false,
            Nadi.Lunar when BeastChakrasContains(BeastChakra.None) => TryGenerateLunarNadi(out act),
            Nadi.Solar when BeastChakrasContains(BeastChakra.None) => TryGenerateSolarNadi(out act),
            _ => false
        };
    }

    private bool TryGenerateLunarNadi(out IAction? act)
    {
        act = null;
        if (!BeastChakrasContains(BeastChakra.None) || NextNadiGoal != Nadi.Lunar) return false;

        return TryUseOpoOpo(out act);
    }

    private bool TryGenerateSolarNadi(out IAction? act)
    {
        act = null;
        if (!BeastChakrasContains(BeastChakra.None) || NextNadiGoal != Nadi.Solar) return false;

        var furyState = CheckFuryGaugeState();

        return furyState switch
        {
            "Coeurl, Raptor, Opo" => !BeastChakrasContains(BeastChakra.Coeurl) && TryUseCoeurl(out act)
                                || !BeastChakrasContains(BeastChakra.Raptor) && TryUseRaptor(out act)
                                || !BeastChakrasContains(BeastChakra.OpoOpo) && TryUseOpoOpo(out act),

            "Opo, Coeurl, Raptor" => !BeastChakrasContains(BeastChakra.OpoOpo) && TryUseOpoOpo(out act)
                                || !BeastChakrasContains(BeastChakra.Coeurl) && TryUseCoeurl(out act)
                                || !BeastChakrasContains(BeastChakra.Raptor) && TryUseRaptor(out act),

            "Opo, Raptor, Coeurl" => !BeastChakrasContains(BeastChakra.OpoOpo) && TryUseOpoOpo(out act)
                                || !BeastChakrasContains(BeastChakra.Raptor) && TryUseRaptor(out act)
                                || !BeastChakrasContains(BeastChakra.Coeurl) && TryUseCoeurl(out act),

            _ => false,
        };
    }

    #endregion

    #region Masterful Blitz Execution

    private bool TryUseMasterfulBlitz(out IAction? act)
    {
        act = null;
        if (BeastChakrasContains(BeastChakra.None)) return false;

        if (HasBothNadi && PhantomRushPvEReady)
        {
            return PhantomRushPvE.CanUse(out act);
        }

        if (BeastChakrasAllSame() && ElixirBurstPvEReady)
        {
            return ElixirBurstPvE.CanUse(out act);
        }

        if (BeastChakrasAllDifferent() && RisingPhoenixPvEReady)
        {
            return RisingPhoenixPvE.CanUse(out act);
        }

        return false;
    }

    #endregion

    #region  Other GCDs

    private bool TryUseFiresReply(out IAction? act)
    {
        act = null;
        if (!HasRiddleOfFire && !HasFiresRumination || HasPerfectBalance) return false;

        if (IsBurst && !HasBlitzReady)
        {
            if (FiresReplyPvE.CanUse(out act))
            {
                if (IsLastGCD(true, WindsReplyPvE))
                {
                    return true;
                }

                if (IsLastGCDOpo)
                {
                    return true;
                }
            }
        }

        if (!IsBurst && !HasBlitzReady)
        {
            if (FiresReplyPvE.CanUse(out act))
            {
                if (LunarOddWindow && ((PhantomRushed && BlitzCount == 2) || !PhantomRushed && BlitzCount == 3))
                {
                    return IsLastGCDOpo;
                }

                if (SolarOddWindow && IsLastGCDOpo && !HasBlitzReady)
                {
                    return true;
                }
            }
        }

        return false;
    }
    private bool TryUseWindsReply(out IAction? act)
    {
        act = null;
        if (!HasWindsRumination || HasPerfectBalance) return false;

        if (WindsReplyPvE.CanUse(out act))
        {
            if (IsBurst && !HasBlitzReady && IsLastGCDOpo)
            {
                return true;
            }

            if (!IsBurst && HasRiddleOfFire && IsLastGCDOpo)
            {
                return true;
            }

            if (!IsBurst && !HasRiddleOfFire && IsLastGCDOpo)
            {
                return true;
            }
        }

        return false;
    }
    private bool TryUseSixSidedStar(out IAction? act)
    {
        act = null;
        if (!IsInHighEndDuty || (CurrentTarget != null && CurrentTarget.GetHealthRatio() > BossHealthThreshold)) return false;

        if (CurrentTarget != null && (CurrentTarget.IsBossFromIcon() || CurrentTarget.IsBossFromTTK()))
        {
            if (CurrentTarget.GetHealthRatio() <= BossHealthThreshold && (!HasPerfectBalance && !HasFormlessFist))
            {
                return SixsidedStarPvE.CanUse(out act);
            }
        }

        return false;
    }
    private bool TryUseFormShift(out IAction? act)
    {
        act = null;
        if (HasFormlessFist || InOpoopoForm || InCoeurlForm || InRaptorForm ||
            HasPerfectBalance || InCombat && HasHostilesInRange) return false;

        return FormShiftPvE.CanUse(out act);
    }

    #endregion

    #region oGCD Methods

    private bool TryUseBuffs(out IAction? act)
    {
        return TryUseBrotherhood(out act)
                   || TryUseRiddleOfFire(out act);
    }
    private bool TryUsePerfectBalance(out IAction? act)
    {
        act = null;
        if (HasPerfectBalance) return false;

        if (CombatElapsedLessGCD(1))
        {
            if (BrotherhoodPvE.Cooldown.HasOneCharge &&
                RiddleOfFirePvE.Cooldown.HasOneCharge)
            {
                return PerfectBalancePvE.CanUse(out act, usedUp: false);
            }
        }

        if (InBurst && !HasFiresRumination)
        {
            return IsLastGCDOpo && PerfectBalancePvE.CanUse(out act, usedUp: true);
        }

        if (SolarOddWindow && IsLastGCDOpo && IsReadySoon(RiddleOfFirePvE, 2))
        {
            return PerfectBalancePvE.CanUse(out act, usedUp: true);
        }

        if (LunarOddWindow && (HasRiddleOfFire || IsReadySoon(RiddleOfFirePvE, 0)) && !HasBothNadi)
        {
            return  IsLastGCDOpo && PerfectBalancePvE.Cooldown.WillHaveOneCharge(10) && PerfectBalancePvE.CanUse(out act, usedUp: true);
        }

        if (IsReadySoon(BrotherhoodPvE, 2) && IsReadySoon(RiddleOfFirePvE,2))
        {
                return IsLastGCDOpo && PerfectBalancePvE.CanUse(out act, usedUp: true);
        }


        return false;
    }
    private bool TryUseBrotherhood(out IAction? act)
    {
        act = null;
        if (!CanBurst || !CanEarlyWeave || !RiddleOfFirePvE.Cooldown.WillHaveOneCharge(0.5f)) return false;

        var timeRequirement = ChosenVariation switch
        {
            OpenerVariation.DragonKick5 => PerfectBalanceStacks(1) && CombatElapsedLessGCD(10),
            OpenerVariation.DragonKick7 => HasBlitzReady && CombatElapsedLessGCD(10),
            OpenerVariation.Demolish7 => HasBlitzReady && CombatElapsedLessGCD(10),
            _ => PerfectBalanceStacks(1) && CombatElapsedLessGCD(10)
        };

        if (timeRequirement) return BrotherhoodPvE.CanUse(out act);

        if (!CombatElapsedLessGCD(10))
        {
            if (IsCooldownAligned(3))
            {
                return BrotherhoodPvE.CanUse(out act);
            }
        }
        return false;
    }
    private bool TryUseRiddleOfFire(out IAction? act)
    {
        act = null;
        if (!RiddleOfFirePvE.IsEnabled || CanEarlyWeave || !CanLateWeave) return false;

        if (RiddleOfFirePvE.CanUse(out act))
        {
            if (IsLastAbility(ActionID.BrotherhoodPvE) || HasBrotherhood)
            {
                return true;
            }



            if (LunarOddWindow)
            {
                if (IsLastAbility(ActionID.PerfectBalancePvE) && IsLastGCDOpo || !IsLastGCDOpo)
                {
                    return true;
                }
            }

            if (SolarOddWindow)
            {
                if (IsLastGCDOpo || IsLastAbility(ActionID.PerfectBalancePvE) || IsLastGCDMasterfulBlitz ||
                    HasBlitzReady)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool TryUseRiddleOfWind(out IAction? act)
    {
        act = null;
        if (!RiddleOfWindPvE.IsEnabled || !EnoughWeaveTime || !RiddleOfWindPvE.Cooldown.WillHaveOneCharge(WeaponRemain) && WeaponRemain <= 1.2f ) return false;

        if (RiddleOfWindPvE.CanUse(out act))
        {
            if (InBurst)
            {
                if (IsLastGCDOpo || IsLastGCDMasterfulBlitz)
                {
                    return true;
                }
            }

            if (HasRiddleOfFire)
            {
                if (IsLastGCDOpo || IsLastGCDMasterfulBlitz)
                {
                    return true;
                }
            }

            if (IsLastGCDOpo && BrotherhoodPvE.Cooldown.IsCoolingDown && RiddleOfFirePvE.Cooldown.IsCoolingDown)
            {
                return true;
            }
        }

        return false;
    }
    private bool TryUseForbiddenChakra(out IAction? act)
    {
        act = null;

                if (Chakra < 5 || !EnoughWeaveTime || ((IsReadySoon(BrotherhoodPvE, 1) || IsReadySoon(RiddleOfFirePvE, 1)) && !IsOpenerStart)) return false;

        // AoE Check
        if (EnlightenmentPvE.CanUse(out act)) return true;

        if (IsOpenerStart && BlitzCount == 0)
        {
            if (ChosenVariation == OpenerVariation.Demolish7 && !InBurst) return false;
            return Chakra >= 5 && TheForbiddenChakraPvE.CanUse(out act);
        }

        if (Chakra >= 5 && TheForbiddenChakraPvE.CanUse(out act)) return true;

        // Low level fallback
        return !TheForbiddenChakraPvE.EnoughLevel && Chakra >= 5 && SteelPeakPvE.CanUse(out act);
    }

    #endregion

    #endregion
}