using Dalamud.Interface.Windowing;
using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.UI;

/// <summary>
/// Per-target PvPSmart score-breakdown table. Rendered when
/// <see cref="RotationSolver.Basic.Configuration.Configs.PvPSmartShowDebugOverlay"/>
/// is true and the player is in a PvP zone. Intended for empirical weight tuning;
/// not part of normal user-facing UI.
///
/// <para>
/// Reads a fresh <see cref="DataCenter.AllHostileTargets"/> snapshot and calls
/// <see cref="PvPTargetScorer.Explain"/> per hostile each draw. Cost is one extra
/// <c>Compose</c> per hostile per frame while open; trivial under CC's 5 hostiles.
/// </para>
/// </summary>
internal class PvPSmartDebugOverlay : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse;

    public PvPSmartDebugOverlay() : base("PvPSmart Score Breakdown", BaseFlags)
    {
        Size = new Vector2(820, 260);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = true;
        IsOpen = true;
    }

    public override bool DrawConditions()
    {
        return Service.Config.PvPSmartShowDebugOverlay && DataCenter.IsPvP;
    }

    public override void OnClose()
    {
        // Keep the checkbox and the window state aligned: closing via the X
        // turns the config flag off.
        Service.Config.PvPSmartShowDebugOverlay = false;
        IsOpen = true; // re-arm; DrawConditions decides visibility on the next frame
        base.OnClose();
    }

    public override void Draw()
    {
        var hostiles = DataCenter.AllHostileTargets;
        if (hostiles == null || hostiles.Count == 0)
        {
            ImGui.TextUnformatted("No hostile targets in range.");
            return;
        }

        var preset = Service.Config.PvPScoringPreset;
        var weights = preset == ScoringPreset.Custom
            ? Service.Config.PvPScoringWeights
            : ScoringWeights.ForPreset(preset);

        var context = new ScoringContext(
            Weights: weights,
            MitigationDatabase: PvPMitigationDatabaseProvider.Current,
            LBDatabase: PvPLBDatabaseProvider.Current,
            PreviousTargetId: DataCenter.LastPvPSmartTargetId,
            CrystalCarrierObjectId: CrystalCarrierState.GetCurrentCarrierId(),
            Hostiles: hostiles,
            ThreatenedAllyIds: ThreatenedAllyState.BuildThreatenedAllyIds(),
            EffectiveRangeYalms: 25f);

        ImGui.TextUnformatted($"Preset: {preset} | Hostiles: {hostiles.Count} | Selected: 0x{DataCenter.LastPvPSmartTargetId ?? 0:X}");
        ImGui.Separator();

        const ImGuiTableFlags TableFlags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##pvpsmart_breakdown", 13, TableFlags))
        {
            return;
        }

        ImGui.TableSetupColumn("Sel");
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Total");
        ImGui.TableSetupColumn("Role");
        ImGui.TableSetupColumn("Finish");
        ImGui.TableSetupColumn("-Mitig");
        ImGui.TableSetupColumn("-Dist");
        ImGui.TableSetupColumn("Sticky");
        ImGui.TableSetupColumn("Carrier");
        ImGui.TableSetupColumn("LB");
        ImGui.TableSetupColumn("Isol");
        ImGui.TableSetupColumn("Threat");
        ImGui.TableSetupColumn("Invuln");
        ImGui.TableHeadersRow();

        for (var i = 0; i < hostiles.Count; i++)
        {
            var hostile = hostiles[i];
            if (hostile == null) continue;

            ScoreBreakdown b;
            try
            {
                b = PvPTargetScorer.Explain(hostile, context);
            }
            catch
            {
                continue;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(hostile.GameObjectId == DataCenter.LastPvPSmartTargetId ? ">" : "");

            ImGui.TableNextColumn();
            var name = hostile.Name?.TextValue ?? "?";
            if (name.Length > 18) name = name[..18];
            ImGui.TextUnformatted(name);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(double.IsNegativeInfinity(b.Total) ? "-inf" : b.Total.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Role.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Finish.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Mitigation.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Distance.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Sticky.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Carrier.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.LB.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Isolation.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Threat.ToString("F3"));
            ImGui.TableNextColumn(); ImGui.TextUnformatted(b.Invuln ? "Y" : "");
        }

        ImGui.EndTable();
    }
}
