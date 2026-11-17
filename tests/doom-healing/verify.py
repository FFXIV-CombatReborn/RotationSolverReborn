"""Run Doom/emergency-healing scenarios against extracted production methods.

Requires Python 3 and the .NET 10 SDK. Native game APIs and geometry are stubbed;
this does not replace live encounter validation. No third-party Python packages.
"""
from pathlib import Path
import subprocess
import tempfile


def verify(root, out):
    def source(path):
        return (root/path).read_text(encoding='utf-8-sig')
    def method(path,signature):
        s=source(path); start=s.index(signature); b=s.index('{',start); depth=1; end=b+1
        while depth:
            depth+=(s[end]=='{')-(s[end]=='}'); end+=1
        return s[start:end].replace('private static','public static',1)
    status='RotationSolver/Basic/Helpers/StatusHelper.cs'
    state='RotationSolver/Updaters/StateUpdater.cs'
    obj='RotationSolver/Basic/Helpers/ObjectHelper.cs'
    dc='RotationSolver/Basic/DataCenter.cs'
    target='RotationSolver/Basic/Actions/ActionTargetInfo.cs'
    parts=['using System; using System.Linq; using System.Collections.Generic; using System.Numerics;']
    parts.append(source('RotationSolver/Basic/Data/AutoStatus.cs').replace('namespace RotationSolver.Basic.Data;',''))
    parts.append('''
    public enum StatusID { Doom=210,Doom_910=910,Doom_1769=1769,Doom_5473=5473,Mounted=9991,HpRecoveryDown=9992,Hot=9993,Hell=9994 }
    public enum JobRole { Healer,Tank,Melee }
    public class IBattleChara {
     public uint CurrentHp=1000,MaxHp=1000,EntityId=1; public ulong GameObjectId; public nint Address=1;
     public bool IsDead,Valid=true,Healable=true,Party=true,Visible=true; public float Distance; public HashSet<ulong> Covers=[]; public JobRole Role=JobRole.Melee;
     public Dictionary<StatusID,float> Statuses=[];
     public bool IsValid()=>Valid; public bool IsParty()=>Party; public bool IsJobCategory(JobRole role)=>Role==role;
     public bool CanBeHealed()=>Healable;
     public bool CanSee()=>Visible;
    }
    public static class Player { public static IBattleChara? Object; }
    public class Objects { public Dictionary<ulong,IBattleChara> Items=[]; public object? SearchById(ulong id)=>Items.GetValueOrDefault(id); }
    public static class Svc { public static Objects Objects=new(); }
    public class Config {
     public bool UseHealWhenNotAHealer,AutoHeal=true,HealOutOfCombat,OnlyHealAsNonHealIfNoHealers,OnlyHealSelfWhenNoHealer;
     public float AutoHealTimeToKill=10,HealthSingleAbility=.7f,HealthSingleAbilityHot=.5f,HealthSingleSpell=.7f,HealthSingleSpellHot=.5f;
     public float HealthAreaAbility=.6f,HealthAreaAbilityHot=.4f,HealthAreaSpell=.6f,HealthAreaSpellHot=.4f,HealthDifference=.2f;
     public float HealthSelfRatio=.4f,HealthHealerRatio=.4f,HealthTankRatio=.45f; public AoEType AoEType; public bool M9SCleaveOnly;
    }
    public static class Service { public static Config Config=new(); }
    public static class CustomRotation { public static float TTK=100; public static bool IsLongerThan(float t)=>TTK>t; }
    public static class IBaseAction { public static bool AutoHealCheck=true; }
    public enum AoEType { All, Off, Cleave }
    public enum TargetType { None, Heal, Self, Tank }
    public record struct TargetResult(IBattleChara Target,IBattleChara[] AffectedTargets,Vector3? Position);
    public class ActionSetting { public bool IsFriendly=true; public TargetType TargetType=TargetType.Heal; }
    public class ActionStub { public ActionSetting Setting=new(); }
    public static class TargetHelper { public static List<IBattleChara> GetTargetsByRange(float range,bool friendly)=>DataCenter.PartyMembers.Where(t=>t.Distance<=range).ToList(); }
    public static partial class StatusHelper {
     public static StatusID[] SingleHots=[StatusID.Hot],AreaHots=[StatusID.Hot],HellInACellStatus=[StatusID.Hell],TankStanceStatus=[];
     public static bool HasStatus(this IBattleChara t,bool from,params StatusID[] ids)=>ids.Any(t.Statuses.ContainsKey);
     public static bool PlayerHasStatus(bool from,params StatusID[] ids)=>Player.Object!=null && Player.Object.HasStatus(from,ids);
     public static float StatusTime(this IBattleChara t,bool from,params StatusID[] ids)=>ids.Where(t.Statuses.ContainsKey).Select(id=>t.Statuses[id]).DefaultIfEmpty(0).Min();
     public static float PlayerStatusTime(bool from,params StatusID[] ids)=>Player.Object?.StatusTime(from,ids)??0;
     public static bool PlayerCanBeHealed()=>Player.Object?.Healable==true;
    ''')
    s=source(status); start=s.index('public static StatusID[] DoomHealStatus'); end=s.index('];',start)+2
    parts.append(s[start:end]); parts.extend(method(status,x) for x in ['public static bool DoomNeedHealing(', 'public static bool PlayerDoomNeedHealing()']); parts.append('}')
    parts.append('''public static class DataCenter {
     public static List<IBattleChara> PartyMembers=[]; public static JobRole Role=JobRole.Healer;
     public static bool IsPvP,InCombat=true,IsInM9S,IsInWindurst,InEffectTime,Tyrant;
     public static Dictionary<ulong,uint> HealHP=[],_lastHp=[];
     public static Dictionary<ulong,float> RefinedHP=>PartyMembers.ToDictionary(t=>t.GameObjectId,GetPartyMemberHPRatio);
     public static bool HPNotFull=>PartyMembers.Any(t=>t.CurrentHp>0 && GetPartyMemberHPRatio(t)<1);
     public static float PartyMembersDifferHP=>0; public static float LowestPartyMembersDifferHP=>0;
     public static float PartyMembersAverHP=>PartyMembers.Average(GetPartyMemberHPRatio); public static float LowestPartyMembersAverHP=>PartyMembersAverHP;
     public static bool IsTyrantCastingSpecialIndicator()=>Tyrant;
    ''')
    parts.append(method(dc,'private static float GetPartyMemberHPRatio('));parts.append('}')
    parts.append(source('RotationSolver/Basic/Helpers/HealPrediction.cs').replace('namespace RotationSolver.Basic.Helpers;',''))
    parts.append('\n'.join(line for line in source('RotationSolver/Basic/Helpers/HealingHelper.cs').splitlines() if not line.startswith('using ') and not line.startswith('namespace ')))
    parts.append('public static class ObjectHelper { public static float GetHealthRatio(this IBattleChara t)=>ComputeHealthRatio(t); public static bool IsParty(IBattleChara t)=>t.Party;')
    parts.extend(method(obj,x) for x in ['private static float ComputeHealthRatio(', 'public static float GetPlayerHealthRatio()']);parts.append('}')
    parts.append('public static class StateUpdater {')
    s=source(state); start=s.index('private static bool CanUseHealAction'); end=s.index(';',start)+1
    parts.append(s[start:end].replace('private static','public static',1))
    parts.extend(method(state,x) for x in ['private static bool AnyLivingHealerInParty()', 'private static bool NonHealerHealLogic()', 'private static AutoStatus StatusFromHealing()', 'private static bool ShouldHealArea(', 'private static float SelfHealingOfTimeRatio(', 'private static float GetHealingOfTimeRatio(', 'private static int ShouldHealSingle(', 'private static bool ShouldHealSelf(', 'private static bool ShouldHealSingle(IBattleChara', 'private static float Lerp(']);parts.append('}')

    parts.append('''
    public class AreaSelection {
     public ActionStub action=new(); public TargetResult? CoreResult;
     public bool IsSingleTarget; public float EffectRange=15; public bool IsTargetFriendly=>action.Setting.IsFriendly;
     private TargetResult? FindTargetCore(bool a,bool b,bool c,TargetType t)=>CoreResult;
     private bool GetCanTarget(IBattleChara anchor,IBattleChara other)=>anchor.Covers.Contains(other.GameObjectId);
     private int GetCanTargetCount(IBattleChara anchor,IEnumerable<IBattleChara> targets)=>targets.Count(t=>GetCanTarget(anchor,t));
    ''')
    parts.append(method(target,'internal readonly TargetResult? FindTarget(').replace('internal readonly','public',1))
    parts.append(method(target,'private readonly IEnumerable<IBattleChara> GetMostCanTargetObjects(').replace('private readonly','public',1))
    parts.append('}')
    parts.append('public class Targeting(List<IBattleChara> battleChara) {')
    parts.append(method(target,'IBattleChara? FindHealTarget(').replace('IBattleChara? FindHealTarget(', 'public IBattleChara? FindHealTarget(',1));parts.append('}')
    parts.append('''
    public static class Program {
     static int count,failed; static ulong id;
     static IBattleChara Add(uint hp=1000, StatusID? doom=null,float time=10,JobRole role=JobRole.Melee) {
      var t=new IBattleChara { CurrentHp=hp,GameObjectId=++id,Role=role };
      if(doom!=null)t.Statuses[doom.Value]=time;
      DataCenter.PartyMembers.Add(t);Svc.Objects.Items[t.GameObjectId]=t;return t;
     }
     static void Check(bool b,string message="assertion failed") { if(!b)throw new Exception(message); }
     static void Reset() {
      DataCenter.PartyMembers=[];Svc.Objects=new();Service.Config=new();DataCenter.HealHP=[];DataCenter._lastHp=[];
      DataCenter.Role=JobRole.Healer;DataCenter.IsPvP=DataCenter.IsInM9S=DataCenter.IsInWindurst=DataCenter.InEffectTime=DataCenter.Tyrant=false;
      DataCenter.InCombat=true; CustomRotation.TTK=100; IBaseAction.AutoHealCheck=true;id=0;Player.Object=Add(role:JobRole.Healer);
     }
     static bool Single()=>StateUpdater.StatusFromHealing().HasFlag(AutoStatus.HealSingleSpell);
     static void Test(string name,Action test) { Reset();try { test();count++;Console.WriteLine("PASS "+name); } catch(Exception e) { failed++;Console.WriteLine("FAIL "+name+": "+e.Message); } }
     public static void Main() {
      foreach(var status in StatusHelper.DoomHealStatus)Test("recognized "+status,()=>{var t=Add(999,status);Check(t.DoomNeedHealing());Check(Single());});
      Test("other Doom variants do not request heal-to-full",()=>{foreach(var s in new[]{StatusID.Doom,StatusID.Doom_910,(StatusID)1738,(StatusID)3364})Check(!Add(999,s).DoomNeedHealing());Check(!Single());});
      Test("one HP missing despite regen and very low threshold",()=>{var t=Add(999,StatusID.Doom_1769);t.Statuses[StatusID.Hot]=30;Service.Config.HealthSingleSpell=.001f;Check(Single());Check(StateUpdater.ShouldHealSingle(t,StatusHelper.SingleHots,.001f,.001f));});
      Test("pending heal does not count as actual full HP",()=>{var t=Add(990,StatusID.Doom_1769);DataCenter.InEffectTime=true;DataCenter._lastHp[t.GameObjectId]=990;DataCenter.HealHP[t.GameObjectId]=100;Check(DataCenter.GetPartyMemberHPRatio(t)==.99f);Check(DataCenter.HPNotFull);Check(Single());});
      Test("ordinary healing prediction remains enabled",()=>{var t=Add(990);DataCenter.InEffectTime=true;DataCenter._lastHp[t.GameObjectId]=990;DataCenter.HealHP[t.GameObjectId]=100;Check(DataCenter.GetPartyMemberHPRatio(t)==1);Check(!Single());});
      Test("full HP with lingering Doom does not trigger healing",()=>{var t=Add(1000,StatusID.Doom_1769);Check(!t.DoomNeedHealing());Check(t.GetHealthRatio()==1);Check(!Single());});
      Test("damage after full HP retriggers monitoring",()=>{var t=Add(1000,StatusID.Doom_1769);Check(!Single());t.CurrentHp=999;Check(Single());t.CurrentHp=1000;Check(!Single());});
      Test("Doom removed restores ordinary thresholds",()=>{var t=Add(999,StatusID.Doom_1769);Check(Single());t.Statuses.Clear();Check(!Single());});
      Test("zero HP is not a healing candidate",()=>{var t=Add(0,StatusID.Doom_1769);Check(!t.DoomNeedHealing());Check(t.GetHealthRatio()==0);Check(!Single());});
      Test("dead flag is not a healing candidate",()=>{var t=Add(999,StatusID.Doom_1769);t.IsDead=true;Check(!t.DoomNeedHealing());Check(!Single());});
      Test("zero maximum HP is excluded",()=>{var t=Add(0,StatusID.Doom_1769);t.MaxHp=0;Check(!t.DoomNeedHealing());Check(!Single());});
      Test("invalid object is excluded",()=>{var t=Add(999,StatusID.Doom_1769);t.Valid=false;Check(!t.DoomNeedHealing());t.Valid=true;t.Address=0;Check(!t.DoomNeedHealing());});
      Test("missing object table entry is excluded",()=>{var t=Add(999,StatusID.Doom_1769);Svc.Objects.Items.Remove(t.GameObjectId);Check(!t.DoomNeedHealing());});
      Test("null player is safe",()=>{Player.Object=null;Check(!StatusHelper.PlayerDoomNeedHealing());});
      Test("player uses same actual HP rule",()=>{var t=Player.Object!;t.Statuses[StatusID.Doom_1769]=10;t.CurrentHp=999;Check(StatusHelper.PlayerDoomNeedHealing());Check(StateUpdater.ShouldHealSelf(StatusHelper.SingleHots,.001f,.001f));t.CurrentHp=1000;Check(!StatusHelper.PlayerDoomNeedHealing());});
      Test("out of combat Doom still heals",()=>{Add(999,StatusID.Doom_1769);DataCenter.InCombat=false;Check(Single());});
      Test("short time to kill does not suppress Doom",()=>{Add(999,StatusID.Doom_1769);CustomRotation.TTK=0;Check(Single());});
      Test("auto heal disabled is respected",()=>{Add(999,StatusID.Doom_1769);Service.Config.AutoHeal=false;Check(StateUpdater.StatusFromHealing()==AutoStatus.None);});
      Test("non healer disabled is respected",()=>{Add(999,StatusID.Doom_1769);DataCenter.Role=JobRole.Melee;Check(StateUpdater.StatusFromHealing()==AutoStatus.None);});
      Test("two Doom targets request area and single healing",()=>{Add(999,StatusID.Doom_1769);Add(999,StatusID.Doom_5473);var s=StateUpdater.StatusFromHealing();Check(s.HasFlag(AutoStatus.HealAreaSpell|AutoStatus.HealAreaAbility|AutoStatus.HealSingleSpell|AutoStatus.HealSingleAbility));});
      Test("full Doom target does not inflate area count",()=>{Add(999,StatusID.Doom_1769);Add(1000,StatusID.Doom_5473);Check(Single());Check(!StateUpdater.StatusFromHealing().HasFlag(AutoStatus.HealAreaSpell));});
      Test("unhealable target is not counted",()=>{Add(999,StatusID.Doom_1769).Healable=false;Check(StateUpdater.StatusFromHealing()==AutoStatus.None);});
      Test("M9 spell restriction preserved",()=>{Add(999,StatusID.Doom_1769);DataCenter.IsInM9S=true;Player.Object!.Statuses[StatusID.Hell]=10;var s=StateUpdater.StatusFromHealing();Check(s.HasFlag(AutoStatus.HealSingleAbility));Check(!s.HasFlag(AutoStatus.HealSingleSpell));});
      Test("Doom priority ahead of noncritical self healer and tank",()=>{Player.Object!.CurrentHp=500;Add(500,role:JobRole.Tank);var t=Add(999,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==t);});
      Test("earliest countdown is selected",()=>{Add(600,StatusID.Doom_1769,20);var t=Add(999,StatusID.Doom_1769,3);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==t);});
      Test("countdown tie prefers lower actual HP",()=>{Add(999,StatusID.Doom_1769);var t=Add(500,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==t);});
      Test("action threshold cannot exclude Doom",()=>{var t=Add(999,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(0)==t);});
      Test("full earlier Doom skipped for next target",()=>{Add(1000,StatusID.Doom_1769,1);var t=Add(999,StatusID.Doom_1769,8);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==t);});
      Test("healed first target moves priority to second",()=>{var first=Add(999,StatusID.Doom_1769,1);var second=Add(999,StatusID.Doom_1769,8);var target=new Targeting(DataCenter.PartyMembers);Check(target.FindHealTarget(.8f)==first);first.CurrentHp=1000;Check(target.FindHealTarget(.8f)==second);});
      Test("lowest actual emergency HP precedes ordinary role preference",()=>{Player.Object!.CurrentHp=200;var tank=Add(100,role:JobRole.Tank);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});

      Test("10 percent tank wins over 99 percent Doom DPS",()=>{var tank=Add(100,role:JobRole.Tank);Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("configured tank emergency threshold is respected",()=>{var tank=Add(440,role:JobRole.Tank);Add(990,StatusID.Doom_1769);Check(tank.NeedsEmergencyHealing());Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("tank above emergency threshold yields to Doom",()=>{Add(460,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==doomed);});
      Test("critical healer is rescued before Doom DPS",()=>{var healer=Add(350,role:JobRole.Healer);Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==healer);});
      Test("critical DPS is also protected",()=>{var dps=Add(200);Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==dps);});
      Test("ordinary DPS critical threshold is 30 percent inclusive",()=>{var a=Add(300);var b=Add(301);Check(a.NeedsEmergencyHealing());Check(!b.NeedsEmergencyHealing());});
      Test("custom tank threshold extends emergency protection",()=>{Service.Config.HealthTankRatio=.6f;var tank=Add(550,role:JobRole.Tank);Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("lower configured thresholds retain universal critical floor",()=>{Service.Config.HealthTankRatio=.1f;var tank=Add(200,role:JobRole.Tank);Check(tank.NeedsEmergencyHealing());});
      Test("emergency selection uses actual HP instead of Doom synthetic HP",()=>{var tank=Add(100,role:JobRole.Tank);Add(250,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("critical Doom target is rescued ahead of a healthy short countdown",()=>{var first=Add(200,StatusID.Doom_1769,20);Add(990,StatusID.Doom_5473,1);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==first);});
      Test("rescue hands back to Doom after tank recovers",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var t=new Targeting(DataCenter.PartyMembers);Check(t.FindHealTarget(.8f)==tank);tank.CurrentHp=600;Check(t.FindHealTarget(.8f)==doomed);});
      Test("unlanded prediction cannot hide a critical tank",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);DataCenter.InEffectTime=true;DataCenter._lastHp[tank.GameObjectId]=100;DataCenter.HealHP[tank.GameObjectId]=1000;Check(DataCenter.GetPartyMemberHPRatio(tank)==.1f);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("per action threshold cannot exclude emergency tank",()=>{var tank=Add(100,role:JobRole.Tank);Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.01f)==tank);});
      Test("regen and low thresholds do not delay emergency healing",()=>{var tank=Add(100,role:JobRole.Tank);tank.Statuses[StatusID.Hot]=30;Check(StateUpdater.ShouldHealSingle(tank,StatusHelper.SingleHots,.01f,.01f));});
      Test("dead or blocked emergency target does not take priority",()=>{var tank=Add(100,role:JobRole.Tank);tank.Healable=false;var doomed=Add(990,StatusID.Doom_1769);Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==doomed);tank.Healable=true;tank.IsDead=true;Check(!tank.NeedsEmergencyHealing());});
      Test("full invalid zero HP and zero max HP are not emergencies",()=>{var t=Add(1000);Check(!t.NeedsEmergencyHealing());t.CurrentHp=0;Check(!t.NeedsEmergencyHealing());t.MaxHp=0;Check(!t.NeedsEmergencyHealing());t.MaxHp=1000;t.CurrentHp=100;t.Valid=false;Check(!t.NeedsEmergencyHealing());});
      Test("group covering tank and Doom is permitted",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);Check(HealingHelper.CoversEmergencyHealingTarget([tank,doomed],HealingHelper.FindEmergencyHealingTarget(DataCenter.PartyMembers)));});
      Test("group missing emergency tank is rejected",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);Check(!HealingHelper.CoversEmergencyHealingTarget([doomed],HealingHelper.FindEmergencyHealingTarget(DataCenter.PartyMembers)));});
      Test("no emergency keeps ordinary group selection",()=>{var doomed=Add(990,StatusID.Doom_1769);Check(HealingHelper.CoversEmergencyHealingTarget([doomed],HealingHelper.FindEmergencyHealingTarget(DataCenter.PartyMembers)));});

      Test("area wrapper rejects group missing reachable critical tank",()=>{var tank=Add(100,role:JobRole.Tank);tank.Distance=25;var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed],null) };Check(area.FindTarget(false,false,false,TargetType.None)==null);});
      Test("area wrapper permits group covering critical tank",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed,tank],null) };Check(area.FindTarget(false,false,false,TargetType.None)!=null);});
      Test("unreachable critical tank does not block group healing",()=>{var tank=Add(100,role:JobRole.Tank);tank.Distance=35;var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed],null) };Check(area.FindTarget(false,false,false,TargetType.None)!=null);});
      Test("single target action keeps its normal result",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { IsSingleTarget=true,CoreResult=new TargetResult(doomed,[doomed],null) };Check(area.FindTarget(false,false,false,TargetType.None)!=null);});
      Test("non heal targeting keeps explicit selection",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed],null) };Check(area.FindTarget(false,false,false,TargetType.Tank)!=null);});
      Test("non automatic targeting keeps normal area result",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed],null) };IBaseAction.AutoHealCheck=false;Check(area.FindTarget(false,false,false,TargetType.None)!=null);});
      Test("null area result remains null",()=>{Check(new AreaSelection().FindTarget(false,false,false,TargetType.None)==null);});
      Test("area anchor covering tank wins over larger distant group",()=>{var tank=Add(100,role:JobRole.Tank);var a=Add(990,StatusID.Doom_1769);var b=Add(990,StatusID.Doom_5473);var c=Add(600);var small=Add();var large=Add();small.Covers=[tank.GameObjectId,a.GameObjectId];large.Covers=[a.GameObjectId,b.GameObjectId,c.GameObjectId];var areas=new AreaSelection();Check(areas.GetMostCanTargetObjects([small,large],[tank,a,b,c],1).Single()==small);});
      Test("uncovered area anchor rejected for single heal fallback",()=>{var tank=Add(100,role:JobRole.Tank);var a=Add(990,StatusID.Doom_1769);var b=Add(990,StatusID.Doom_5473);var anchor=Add();anchor.Covers=[a.GameObjectId,b.GameObjectId];Check(!new AreaSelection().GetMostCanTargetObjects([anchor],[tank,a,b],1).Any());Check(new Targeting(DataCenter.PartyMembers).FindHealTarget(.8f)==tank);});
      Test("area minimum count still enforced near emergency target",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var anchor=Add();anchor.Covers=[tank.GameObjectId,doomed.GameObjectId];Check(!new AreaSelection().GetMostCanTargetObjects([anchor],[tank,doomed],3).Any());});
      Test("occluded critical tank does not block otherwise useful group heal",()=>{var tank=Add(100,role:JobRole.Tank);tank.Visible=false;var doomed=Add(990,StatusID.Doom_1769);var area=new AreaSelection { CoreResult=new TargetResult(doomed,[doomed],null) };Check(area.FindTarget(false,false,false,TargetType.None)!=null);});
      Test("occluded critical tank does not block area anchors",()=>{var tank=Add(100,role:JobRole.Tank);tank.Visible=false;var doomed=Add(990,StatusID.Doom_1769);var anchor=Add();anchor.Covers=[doomed.GameObjectId];Check(new AreaSelection().GetMostCanTargetObjects([anchor],[tank,doomed],1).Single()==anchor);});
      Test("anchor selection respects explicit non heal override",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var anchor=Add();anchor.Covers=[doomed.GameObjectId];Check(new AreaSelection().GetMostCanTargetObjects([anchor],[tank,doomed],1,TargetType.Tank).Single()==anchor);});
      Test("anchor selection respects explicit heal override",()=>{var tank=Add(100,role:JobRole.Tank);var doomed=Add(990,StatusID.Doom_1769);var anchor=Add();anchor.Covers=[doomed.GameObjectId];var area=new AreaSelection();area.action.Setting.TargetType=TargetType.Tank;Check(!area.GetMostCanTargetObjects([anchor],[tank,doomed],1,TargetType.Heal).Any());});
      Console.WriteLine($"{count} scenarios passed, {failed} failed; native game APIs are stubbed."); Environment.ExitCode=failed==0?0:1;
     }
    }
    ''')
    (out/'Program.cs').write_text('\n'.join(parts),encoding='utf-8')
    (out/'Regression.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>',encoding='utf-8')
    subprocess.run(["dotnet", "run", "--project", str(out / "Regression.csproj"), "-c", "Release"], check=True)


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[2]
    with tempfile.TemporaryDirectory(prefix="rsr-doom-regression-") as directory:
        verify(root, Path(directory))
