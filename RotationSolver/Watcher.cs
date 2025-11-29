using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration;
using System.Text.RegularExpressions;

namespace RotationSolver;

public static class Watcher
{
    public static void Enable()
    {
        ActionEffect.ActionEffectEvent += ActionFromEnemy;
        ActionEffect.ActionEffectEvent += ActionFromSelf;
    }

    public static void Disable()
    {
        ActionEffect.ActionEffectEvent -= ActionFromEnemy;
        ActionEffect.ActionEffectEvent -= ActionFromSelf;
    }

    public static string ShowStrSelf { get; private set; } = string.Empty;
    public static string ShowStrEnemy { get; private set; } = string.Empty;

    private static string? _cachedBranch = null;

    public static string DalamudBranch()
    {
        if (_cachedBranch != null)
            return _cachedBranch;

        const string release = "release";
        string result = release; // Default to "release" instead of "other"

        if (DalamudReflector.TryGetDalamudStartInfo(out Dalamud.Common.DalamudStartInfo? startinfo, Svc.PluginInterface))
        {
            if (!string.IsNullOrEmpty(startinfo?.ConfigurationPath) && File.Exists(startinfo.ConfigurationPath))
            {
                try
                {
                    using var fs = File.OpenRead(startinfo.ConfigurationPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(fs);
                    if (doc.RootElement.TryGetProperty("DalamudBetaKind", out var kindProp))
                    {
                        string? type = kindProp.GetString();
                        result = string.IsNullOrEmpty(type) ? release : type; // Return the actual string or default to "release"
                    }
                }
                catch
                {
                    result = release; // Default to "release" on error
                }
            }
        }

        _cachedBranch = result;
        return result;
    }

    private static void ActionFromEnemy(ActionEffectSet set)
    {
        try
        {
            if (set.Source is not IBattleChara battle || !set.Source.IsEnemy())
                return;

            IPlayerCharacter playerObject = Player.Object;
            if (playerObject == null)
                return;

            float damageRatio = 0;
            ulong playerId = playerObject.GameObjectId;
            uint maxHp = playerObject.MaxHp;
            uint denom = Math.Max(1u, maxHp); // avoid division by zero

            foreach (var effect in set.TargetEffects)
            {
                if (effect.TargetID == playerId)
                {
                    effect.ForEach(entry =>
                    {
                        if (entry.type == ActionEffectType.Damage)
                            damageRatio += (float)entry.value / denom;
                    });
                }
            }

            DataCenter.AddDamageRec(damageRatio);
            ShowStrEnemy = $"Damage Ratio: {damageRatio}\n{set}";

            foreach (var effect in set.TargetEffects)
            {
                if (effect.TargetID != playerId)
                    continue;

                if (effect.GetSpecificTypeEffect(ActionEffectType.Knockback, out var entry))
                {
                    var knock = Svc.Data.GetExcelSheet<Knockback>()?.GetRow(entry.value);
                    if (knock != null)
                    {
                        DataCenter.KnockbackStart = DateTime.Now;
                        if (knock.Value.Speed > 0)
                        {
                            DataCenter.KnockbackFinished = DateTime.Now + TimeSpan.FromSeconds(knock.Value.Distance / (float)knock.Value.Speed);
                        }

                        if (set.Action.HasValue && Service.Config.RecordKnockbackies)
                        {
                            bool isContained = false;
                            foreach (var id in OtherConfiguration.HostileCastingKnockback)
                            {
                                if (id == set.Action.Value.RowId)
                                {
                                    isContained = true;
                                    break;
                                }
                            }

                            if (!isContained)
                            {
                                _ = OtherConfiguration.HostileCastingKnockback.Add(set.Action.Value.RowId);
                                _ = OtherConfiguration.Save();
                            }
                        }
                    }
                    break;
                }
            }

            var partyMembers = DataCenter.PartyMembers;
            int partyMemberCount = partyMembers.Count;

            if (set.Header.ActionType == ActionType.Action && partyMemberCount >= 4 && set.Action?.Cast100ms > 0)
            {
                var type = set.Action?.GetActionCate();
                if (type is ActionCate.Spell or ActionCate.Weaponskill or ActionCate.Ability)
                {
                    int damageEffectCount = 0;

                    var partyIds = new HashSet<ulong>();
                    foreach (var pm in partyMembers)
                    {
                        partyIds.Add(pm.GameObjectId);
                    }

                    foreach (var effect in set.TargetEffects)
                    {
                        bool isPartyMember = false;
                        foreach (var pId in partyIds)
                        {
                            if (pId == effect.TargetID)
                            {
                                isPartyMember = true;
                                break;
                            }
                        }

                        if (isPartyMember &&
                            effect.GetSpecificTypeEffect(ActionEffectType.Damage, out var damageEffect) &&
                            (damageEffect.value > 0 || (damageEffect.param0 & 6) == 6))
                        {
                            damageEffectCount++;
                        }
                    }

                    if (damageEffectCount == partyMemberCount && Service.Config.RecordCastingArea)
                    {
                        _ = OtherConfiguration.HostileCastingArea.Add(set.Action!.Value.RowId);
                        _ = OtherConfiguration.SaveHostileCastingArea();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Error in ActionFromEnemy: {ex}");
        }
    }

    private static void ActionFromSelf(ActionEffectSet set)
    {
        try
        {
            IPlayerCharacter playerObject = Player.Object;
            if (set.Source == null || playerObject == null)
            {
                return;
            }

            if (set.Source.GameObjectId != playerObject.GameObjectId)
            {
                return;
            }

            if (set.Header.ActionType is not ActionType.Action and not ActionType.Item)
            {
                return;
            }

            if (set.Action == null)
            {
                return;
            }

            if (set.Action?.ActionCategory.Value.RowId == (uint)ActionCate.Autoattack)
            {
                return;
            }

            if (set.TargetEffects.Length == 0)
            {
                return;
            }

            Lumina.Excel.Sheets.Action? action = set.Action;
            IGameObject? tar = set.Target;

            // Record
            DataCenter.AddActionRec(action!.Value);
            ShowStrSelf = set.ToString();

            DataCenter.HealHP = set.GetSpecificTypeEffect(ActionEffectType.Heal);

            // Ensure ApplyStatus dictionary is non-null, then merge source-applied effects
            DataCenter.ApplyStatus = set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectTarget) ?? [];
            var sourceApply = set.GetSpecificTypeEffect(ActionEffectType.ApplyStatusEffectSource);
            if (sourceApply is { Count: > 0 })
            {
                foreach (KeyValuePair<ulong, uint> effect in sourceApply)
                {
                    DataCenter.ApplyStatus[effect.Key] = effect.Value;
                }
            }

            uint mpGain = 0;
            var mpEffects = set.GetSpecificTypeEffect(ActionEffectType.MpGain);
            if (mpEffects != null)
            {
                foreach (KeyValuePair<ulong, uint> effect in mpEffects)
                {
                    if (effect.Key == playerObject.GameObjectId)
                    {
                        mpGain += effect.Value;
                    }
                }
            }
            DataCenter.MPGain = mpGain;

            DataCenter.EffectTime = DateTime.Now;
            DataCenter.EffectEndTime = DateTime.Now.AddSeconds(set.Header.AnimationLockTime + 1);

            Queue<(ulong id, DateTime time)> attackedTargets = DataCenter.AttackedTargets;
            int attackedTargetsCount = DataCenter.AttackedTargetsCount;

            if (attackedTargetsCount > 0)
            {
                foreach (TargetEffect effect in set.TargetEffects)
                {
                    if (!effect.GetSpecificTypeEffect(ActionEffectType.Damage, out _))
                    {
                        continue;
                    }

                    // Check if the target is already in the attacked targets list
                    bool targetExists = false;
                    foreach ((ulong id, DateTime time) in attackedTargets)
                    {
                        if (id == effect.TargetID)
                        {
                            targetExists = true;
                            break;
                        }
                    }
                    if (targetExists)
                    {
                        continue;
                    }

                    // Ensure the current target is not dequeued
                    while (attackedTargets.Count >= attackedTargetsCount && attackedTargets.Count > 0)
                    {
                        (ulong id, DateTime time) = attackedTargets.Peek();
                        if (id == effect.TargetID)
                        {
                            // If the oldest target is the current target, break the loop to avoid dequeuing it
                            break;
                        }
                        _ = attackedTargets.Dequeue();
                    }

                    // Enqueue the new target
                    attackedTargets.Enqueue((effect.TargetID, DateTime.Now));
                }
            }

            // Macro
            RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            var eventsList = Service.Config.Events ?? [];
            var actionName = action.Value.Name.ExtractText() ?? string.Empty;
            if (!string.IsNullOrEmpty(actionName))
            {
                foreach (ActionEventInfo item in eventsList)
                {
                    if (string.IsNullOrWhiteSpace(item.Name))
                    {
                        continue;
                    }

                    bool isMatch;
                    try
                    {
                        isMatch = Regex.IsMatch(actionName, item.Name, regexOptions);
                    }
                    catch (ArgumentException ex)
                    {
                        PluginLog.Warning($"Invalid regex in ActionEventInfo.Name: \"{item.Name}\". {ex.Message}");
                        continue;
                    }

                    if (!isMatch)
                    {
                        continue;
                    }

                    if (item.AddMacro(tar))
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Error in ActionFromSelf: {ex}");
        }
    }
}