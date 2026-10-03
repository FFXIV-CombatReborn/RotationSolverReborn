using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.UI.Material;

namespace RotationSolver.UI;

public partial class MainWindow
{
	private static void DrawExtra()
	{
		DrawPageIntro(UiString.ConfigWindow_Extra_Description.GetDescription());
		_extraHeader?.Draw();
	}

	private static readonly CollapsingHeaderGroup _extraHeader = BuildHeaderGroup(
		new Dictionary<Func<string>, Action>
		{
			{ () => UiString.ConfigWindow_EventItem.GetDescription(), DrawEventTab },
			{ () => UiString.ConfigWindow_Internal.GetDescription(), DrawInternalTab },
			{
				() => UiString.ConfigWindow_Extra_Others.GetDescription(),
				() => _allSearchable.DrawItems(Configs.Extra)
			},
		},
		(UiString.ConfigWindow_EventItem, FontAwesomeIcon.CalendarCheck),
		(UiString.ConfigWindow_Internal, FontAwesomeIcon.Database),
		(UiString.ConfigWindow_Extra_Others, FontAwesomeIcon.PuzzlePiece));

	private static void DrawInternalTab()
	{
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant))
		{
			ImGui.TextWrapped($"Configs and backups live in {Svc.PluginInterface.ConfigFile.Directory}");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		if (M3Widgets.Button("##backup_configs", "Back up configs", M3ButtonStyle.Tonal, FontAwesomeIcon.Save))
		{
			Service.Config.Backup();
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##restore_configs", "Restore configs", M3ButtonStyle.Outlined, FontAwesomeIcon.UndoAlt))
		{
			Service.Config.Restore();
		}
	}

	private static void DrawEventTab()
	{
		DrawPageIntro(UiString.ConfigWindow_Events_Description.GetDescription());

		if (M3Widgets.Button("##add_event", UiString.ConfigWindow_Events_AddEvent.GetDescription(),
			M3ButtonStyle.Tonal, FontAwesomeIcon.Plus))
		{
			Service.Config.Events.Add(new ActionEventInfo());
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));
		M3Widgets.SectionLabel(UiString.ConfigWindow_Events_DutyStart.GetDescription());
		Service.Config.DutyStart.DisplayMacro();

		M3Widgets.SectionLabel(UiString.ConfigWindow_Events_DutyEnd.GetDescription());
		Service.Config.DutyEnd.DisplayMacro();

		if (Service.Config.Events.Count == 0)
		{
			return;
		}

		M3Widgets.SectionLabel(UiString.ConfigWindow_EventItem.GetDescription());

		for (var i = 0; i < Service.Config.Events.Count; i++)
		{
			var eve = Service.Config.Events[i];

			using var card = M3Card.Begin($"event_{eve.GetHashCode()}", null, style: M3CardStyle.Outlined);
			eve.DisplayEvent();

			ImGui.Dummy(new Vector2(0f, M3.Space1));
			if (M3Widgets.Button($"##remove_event_{eve.GetHashCode()}",
				UiString.ConfigWindow_Events_RemoveEvent.GetDescription(), M3ButtonStyle.Text, FontAwesomeIcon.Trash))
			{
				Service.Config.Events.RemoveAt(i);
				i--;
			}
		}
	}
}
