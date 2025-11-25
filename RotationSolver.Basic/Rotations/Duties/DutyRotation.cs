using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;

namespace RotationSolver.Basic.Rotations.Duties;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Represents the base class for duty rotations.
/// </summary>
public partial class DutyRotation : IDisposable
{
    #region GCD
    public virtual bool EmergencyGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool MyInterruptGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool GeneralGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool RaiseGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DispelGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool MoveForwardGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool HealSingleGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool HealAreaGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DefenseSingleGCD(out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DefenseAreaGCD(out IAction? act)
    {
        act = null; return false;
    }
    #endregion

    #region Ability

    public virtual bool InterruptAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DispelAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool ProvokeAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    public virtual bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null; return false;
    }

    #endregion

    public virtual void DisplayDutyStatus()
    {
        if (DataCenter.IsInMonsterHunterDuty)
        {
            ImGui.Spacing();
            ImGui.Text($"MegaPotionPvE Slotted: {MegaPotionPvE.Info.IsOnSlot}");
            ImGui.Text($"MegaPotionPvE Charges: {MegaPotionPvE.Cooldown.CurrentCharges}");
            ImGui.Spacing();
            ImGui.Text($"Rathalos Normal: {RathalosNormal}");
            ImGui.Text($"Rathalos EX: {RathalosEX}");
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Text($"MegaPotionPvE_44247 Slotted: {MegaPotionPvE_44247.Info.IsOnSlot}");
            ImGui.Text($"MegaPotionPvE_44247 Charges: {MegaPotionPvE_44247.Cooldown.CurrentCharges}");
            ImGui.Spacing();
            ImGui.Text($"Arkveld Normal: {ArkveldNormal}");
            ImGui.Text($"Arkveld EX: {ArkveldEX}");
            ImGui.Spacing();
        }

        if (DataCenter.IsInOccultCrescentOp)
        {
            ImGui.Text($"ActivePhantomJob: {ActivePhantomJob ?? "N/A"}");
            ImGui.Spacing();

            if (string.Equals(ActivePhantomJob, "Oracle", StringComparison.OrdinalIgnoreCase))
            {
                ImGui.Text($"HasCleansing: {HasCleansing}");
                ImGui.Text($"HasStarfall: {HasStarfall}");
                ImGui.Text($"HasPhantomJudgment: {HasPhantomJudgment}");
                ImGui.Text($"HasBlessing: {HasBlessing}");
            }

            if (string.Equals(ActivePhantomJob, "Samurai", StringComparison.OrdinalIgnoreCase))
            {
                ImGui.Text($"Has item for Zeninage: {ZeninageItem.HasIt}");
            }

            if (string.Equals(ActivePhantomJob, "Chemist", StringComparison.OrdinalIgnoreCase))
            {
                ImGui.Text($"Has item for Occult Potion: {OccultPotionItem.HasIt}");
                ImGui.Text($"Has item for Occult Ether: {OccultPotionItem.HasIt}");
                ImGui.Text($"Has item for Occult Elixir: {OccultElixirItem.HasIt}");
            }
        }

        if (InVariantDungeon)
        {
            ImGui.Text($"VariantUltimatumPvE Status: {Player.HasStatus(true, StatusID.VariantUltimatumSet)}");
            ImGui.Spacing();
            ImGui.Text($"VariantSpiritDartPvE_33863  Status: {Player.HasStatus(true, StatusID.VariantSpiritDartSet)}");
            ImGui.Text($"VariantSpiritDartPvE_33863 Slotted: {VariantSpiritDartPvE_33863.Info.IsOnSlot}");
            ImGui.Spacing();
            ImGui.Text($"VariantSpiritDartPvE  Status: {Player.HasStatus(true, StatusID.VariantSpiritDartSet)}");
            ImGui.Text($"VariantSpiritDartPvE Slotted: {VariantSpiritDartPvE.Info.IsOnSlot}");
            ImGui.Spacing();
            ImGui.Text($"VariantRampartPvE_33864 Status: {Player.HasStatus(true, StatusID.VariantRampartSet)}");
            ImGui.Spacing();
            ImGui.Text($"VariantRampartPvE Status: {Player.HasStatus(true, StatusID.VariantRampartSet)}");
            ImGui.Spacing();
            ImGui.Text($"VariantCurePvE_33862  Status: {Player.HasStatus(true, StatusID.VariantCureSet)}");
            ImGui.Text($"VariantCurePvE_33862 Slotted: {VariantCurePvE_33862.Info.IsOnSlot}");
            ImGui.Spacing();
            ImGui.Text($"VariantCurePvE  Status: {Player.HasStatus(true, StatusID.VariantCureSet)}");
            ImGui.Text($"VariantCurePvE Slotted: {VariantCurePvE.Info.IsOnSlot}");
            ImGui.Spacing();
            ImGui.Text($"VariantRaisePvE Status: {Player.HasStatus(true, StatusID.VariantRampartSet)}");
            ImGui.Spacing();
            ImGui.Text($"VariantRaiseIiPvE Status: {Player.HasStatus(true, StatusID.VariantRaiseSet)}");
            ImGui.Spacing();
            ImGui.Text($"VariantRampartPvE Status: {Player.HasStatus(true, StatusID.VariantRaiseSet)}");
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Text($"Sildihn Subterrane: {SildihnSubterrane}");
            ImGui.Text($"Mount Rokkon: {MountRokkon}");
            ImGui.Text($"Aloalo Island: {AloaloIsland}");
            ImGui.Spacing();
        }
    }

    public DutyRotation()
    {
        Configs = new RotationConfigSet(this);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="DutyRotation"/> class.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Rotation config set for display
    /// </summary>
    internal IRotationConfigSet Configs { get; }

    /// <summary>
    /// Gets the hostile target.
    /// </summary>
    public static IBattleChara? HostileTarget => DataCenter.HostileTarget;

    /// <summary>
    /// Check if in combat.
    /// </summary>
    public static bool InCombat => DataCenter.InCombat;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsMoving => DataCenter.IsMoving;

    /// <summary>
    /// 
    /// </summary>
    public static bool RathalosEX => DataCenter.RathalosEX;

    /// <summary>
    /// 
    /// </summary>
    public static bool RathalosNormal => DataCenter.RathalosNormal;

    /// <summary>
    /// 
    /// </summary>
    public static bool ArkveldNormal => DataCenter.ArkveldNormal;

    /// <summary>
    /// 
    /// </summary>
    public static bool ArkveldEX => DataCenter.ArkveldEX;

    /// <summary>
    /// 
    /// </summary>
    public static bool SildihnSubterrane => DataCenter.SildihnSubterrane;

    /// <summary>
    /// 
    /// </summary>
    public static bool MountRokkon => DataCenter.MountRokkon;

    /// <summary>
    /// 
    /// </summary>
    public static bool AloaloIsland => DataCenter.AloaloIsland;

    /// <summary>
    /// 
    /// </summary>
    public static bool InVariantDungeon => DataCenter.InVariantDungeon;

    /// <summary>
    /// This is the player.
    /// </summary>
    protected static IPlayerCharacter Player => ECommons.GameHelpers.Player.Object;

    public static bool IsRDM => DataCenter.Job == Job.RDM;
    public static bool IsPLD => DataCenter.Job == Job.PLD;
    public static bool IsBLM => DataCenter.Job == Job.BLM;

    public static float PartyMembersAverHP => DataCenter.PartyMembersAverHP;

    public static bool InBurstWindow()
    {
        return DataCenter.CurrentRotation?.IsInBurstWindow == true;
    }

    public enum PhantomJob : byte
    {
        Freelancer,
        Knight,
        Berserker,
        Monk,
        Ranger,
        Samurai,
        Bard,
        Geomancer,
        TimeMage,
        Cannoneer,
        Chemist,
        Oracle,
        Thief,
        None
        //MysticKnight,
        //Dancer,
        //Gladiator
    }

    public static PhantomJob GetPhantomJob()
    {
        if (FreelancerLevel > 0) return PhantomJob.Freelancer;
        if (KnightLevel > 0) return PhantomJob.Knight;
        if (MonkLevel > 0) return PhantomJob.Monk;
        if (BardLevel > 0) return PhantomJob.Bard;
        if (ChemistLevel > 0) return PhantomJob.Chemist;
        if (TimeMageLevel > 0) return PhantomJob.TimeMage;
        if (CannoneerLevel > 0) return PhantomJob.Cannoneer;
        if (OracleLevel > 0) return PhantomJob.Oracle;
        if (BerserkerLevel > 0) return PhantomJob.Berserker;
        if (RangerLevel > 0) return PhantomJob.Ranger;
        if (ThiefLevel > 0) return PhantomJob.Thief;
        if (SamuraiLevel > 0) return PhantomJob.Samurai;
        if (GeomancerLevel > 0) return PhantomJob.Geomancer;

        //if (MysticKnightLevel > 0) return PhantomJob.MysticKnight;
        //if (DancerLevel > 0) return PhantomJob.Dancer;
        //if (GladiatorLevel > 0) return PhantomJob.Gladiator;
        return PhantomJob.None;
    }

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
    /// 
    /// </summary>
    [Description("Has tank stance")]
    public static bool HasTankInvuln => Player?.HasStatus(true, StatusHelper.NoNeedHealingStatus) ?? false;

    /// <summary>
    /// In the burst status.
    /// </summary>
    [Description("Is burst")]
    public static bool IsBurst => MergedStatus.HasFlag(AutoStatus.Burst);

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

    #region Phantom Levels

    /// <summary>
    /// Gets the name of the current active Phantom Job, or None if none are active.
    /// </summary>
    public static string? ActivePhantomJob => GetPhantomJob().ToString();

    public static byte FreelancerLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomFreelancer);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte KnightLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomKnight);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte MonkLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomMonk);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte BardLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomBard);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte ChemistLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomChemist);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte TimeMageLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomTimeMage);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte CannoneerLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomCannoneer);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte OracleLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomOracle);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte BerserkerLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomBerserker);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte RangerLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomRanger);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte ThiefLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomThief);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte SamuraiLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomSamurai);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    public static byte GeomancerLevel
    {
        get
        {
            byte stacks = Player.StatusStack(true, StatusID.PhantomGeomancer);
            return stacks == byte.MaxValue ? (byte)0 : stacks;
        }
    }

    //public static byte MysticKnightLevel
    //{
    //    get
    //    {
    //        byte stacks = Player.StatusStack(true, StatusID.PhantomMysticKnight);
    //        return stacks == byte.MaxValue ? (byte)0 : stacks;
    //    }
    //}

    //public static byte DancerLevel
    //{
    //    get
    //    {
    //        byte stacks = Player.StatusStack(true, StatusID.PhantomDancer);
    //        return stacks == byte.MaxValue ? (byte)0 : stacks;
    //    }
    //}

    //public static byte GladiatorLevel
    //{
    //    get
    //    {
    //        byte stacks = Player.StatusStack(true, StatusID.PhantomGladiator);
    //        return stacks == byte.MaxValue ? (byte)0 : stacks;
    //    }
    //}

    #endregion
    /// <summary>
    /// Gets all actions available in the duty rotation.
    /// </summary>
    internal IAction[] AllActions
    {
        get
        {
            var runtimeProperties = GetType().GetRuntimeProperties();
            var propertiesList = new List<PropertyInfo>();

            foreach (var p in runtimeProperties)
            {
                var attr = p.GetCustomAttribute<IDAttribute>();
                uint id = attr != null ? attr.ID : uint.MaxValue;
                if (DataCenter.DutyActions.Contains(id))
                {
                    propertiesList.Add(p);
                }
            }

            var actionsList = new List<IAction>();
            foreach (var prop in propertiesList)
            {
                var value = prop.GetValue(this);
                if (value is IAction action)
                {
                    actionsList.Add(action);
                }
            }

            return [.. actionsList];
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member