using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;

namespace RotationSolver.UI;

internal sealed class FirstStartTutorialWindow : Window
{
    private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize;
    private int _stepIndex;

    private static readonly string[] StarterMacros =
    [
		"/rotation Settings AoEType Full\r\n/rotation Auto",
		"/rotation Settings AoEType Cleave\r\n/rotation Manual",
        "/rotation Off",
    ];

	private static readonly TutorialStep[] Steps =
	[
		new(
			"Welcome!",
			"This walkthrough explains how to set up Rotation Solver Reborn and what each section controls and includes recommended macros.",
			Bullets:
			[
				"Open the config with /rotation or the plugin UI button.",
				"Use Next/Back to move through sections and apply changes as you go.",
				"Most settings are safe to change while logged in, but avoid in-combat tweaks until you’re comfortable.",
				"Right-click any setting or action label to copy its macro command."
			]),
		new(
			"Main Screen",
			"Main is your overview hub: plugin info, compatibility info, links, and macro list live here.",
			RotationConfigWindowTab.Main,
			[
				"Use this tab to verify incompatible plugins and open support links.",
				"Read the macros section to learn quick chat commands.",
				"If something breaks after an update, check this tab first."
			]),
		new(
			"Job Settings",
			"Job config controls rotation selection and job-specific options for your current class.",
			RotationConfigWindowTab.Job,
			[
				"Pick the rotation preset you want to run by clicking the rotation name (ie. Reborn).",
				"Adjust job priorities (e.g., DNC partner, SGE Kardia) when applicable.",
				"If a job feels off, start here before touching global settings."
			]),
		new(
			"Actions",
			"Actions config decides what abilities RSR can use and how they behave.",
			RotationConfigWindowTab.Actions,
			[
				"Click an action icon in a category to see settings to enable/disable it or change its usage rules.",
				"Use intercept if you want RSR to fire actions you queue manually.",
				"Toggle cooldown window inclusion so overlays show only what you want."
			]),
		new(
			"Auto",
			"Auto controls global action usage, AoE logic, interrupts, tinctures, and healing behavior.",
			RotationConfigWindowTab.Auto,
			[
				"Here you can adjust your AOE logic, (Off, Cleave, and Full).",
				"Adjust healer thresholds and non-healer support options.",
				"If you want a more conservative rotation, tighten these settings first."
			]),
		new(
			"Basic",
			"Basic contains core timing and automation behaviors that affect all jobs.",
			RotationConfigWindowTab.Basic,
			[
				"Action Ahead affects weave count and clipping—smaller values = more oGCDs. You typically don't need to change this.",
				"Min Updating Time trades performance for responsiveness.",
				"Auto Switch controls when RSR turns on/off automatically (countdowns, deaths, duty events)."
			]),
		new(
			"UI",
			"UI controls overlays, info windows, and Teaching Mode highlights.",
			RotationConfigWindowTab.UI,
			[
				"Enable Control, Next Action, Cooldown, and Timeline windows here.",
				"Use Teaching Mode to highlight hotbar buttons and learn rotations visually.",
				"If you want windows to only show in duty/with enemies, toggle that option here."
			]),
		new(
			"Target",
			"Target controls what enemies or allies RSR considers valid.",
			RotationConfigWindowTab.Target,
			[
				"Tune vision cone and engage behavior to avoid unwanted pulls.",
				"Configure target priority rules (FATE, quest mobs, markers).",
				"If targeting feels wrong, adjust filters before changing rotations."
			]),
		new(
			"List",
			"List manages curated status lists: dispels, priority targets, knockbacks, and more.",
			RotationConfigWindowTab.List,
			[
				"Use Reset and Update to restore curated lists when needed.",
				"Add or remove statuses by ID or name using the + buttons.",
				"These lists drive smart reactions across all jobs."
			]),
		new(
			"Duty",
			"Duty holds encounter‑specific toggles for special behavior.",
			RotationConfigWindowTab.Duty,
			[
				"Most of these at the moment can be left enabled but there will be more granular controls in the future.",
				"These settings override general targeting/rotation behavior in specific fights."
			]),
		new(
			"Extra",
			"Extra is for advanced or experimental tweaks.",
			RotationConfigWindowTab.Extra,
			[
				"Animation lock and cooldown delay tweaks for those not using BMR.",
				"Only change these if you understand the side effects.",
			]),
		new(
			"Macros",
			"Starter macros let you control RSR quickly without opening the UI.",
			RotationConfigWindowTab.Main,
			[
				"Use the macros below to toggle Auto/Manual/Off instantly.",
				"Right-click any setting or action to copy its macro command.",
				"Build a small macro bar for fast in combat control."
			],
			StarterMacros),
	];

	public FirstStartTutorialWindow()
        : base("RSR First Start Tutorial", BaseFlags)
    {
        Size = new Vector2(720, 530);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = true;
    }

	public override bool DrawConditions()
	{
		return DataCenter.PlayerAvailable();
	}

	public override void Draw()
	{
		TutorialStep step = Steps[_stepIndex];

		ImGui.PushFont(FontManager.GetFont(ImGui.GetFontSize() + 6));
		ImGui.TextColored(ImGuiColors.ParsedGold, step.Title);
		ImGui.PopFont();

		DrawWrappedText(step.Description);
		ImGui.Spacing();

		if (step.Bullets is { Length: > 0 })
		{
			foreach (string bullet in step.Bullets)
			{
				DrawWrappedBullet(bullet);
			}
			ImGui.Spacing();
		}

		if (step.RecommendedMacros is { Length: > 0 })
		{
			ImGui.TextColored(ImGuiColors.HealerGreen, "Recommended macros:");
			for (int i = 0; i < step.RecommendedMacros.Length; i++)
			{
				string macro = step.RecommendedMacros[i];
				DrawWrappedBullet(macro);

				ImGui.SameLine();
				string buttonId = $"Copy##TutorialMacro_{i}";
				if (ImGui.SmallButton(buttonId))
				{
					ImGui.SetClipboardText(macro);
					Svc.Toasts.ShowNormal("Macro copied to clipboard.");
				}
			}

			ImGui.Spacing();
		}

		if (step.Tab != null)
		{
			if (ImGui.Button($"Open {step.Tab} tab"))
			{
				RotationSolverPlugin.ShowConfigWindow(step.Tab.Value);
			}
			ImGui.Spacing();
		}

		ImGui.Separator();
		DrawNavigation();
	}

	public override void OnClose()
	{
        MarkTutorialComplete();
		base.OnClose();
	}

	private static void DrawWrappedText(string text)
	{
		float wrapPos = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
		ImGui.PushTextWrapPos(wrapPos);
		ImGui.TextWrapped(text);
		ImGui.PopTextWrapPos();
	}

	private static void DrawWrappedBullet(string text)
	{
		ImGui.Bullet();
		ImGui.SameLine();
		DrawWrappedText(text);
	}

	private void DrawNavigation()
    {
        ImGui.BeginDisabled(_stepIndex == 0);
        if (ImGui.Button("Back"))
        {
            _stepIndex = Math.Max(0, _stepIndex - 1);
        }
        ImGui.EndDisabled();

        ImGui.SameLine();

        if (_stepIndex < Steps.Length - 1)
        {
            if (ImGui.Button("Next"))
            {
                _stepIndex = Math.Min(Steps.Length - 1, _stepIndex + 1);
            }
        }
        else
        {
            if (ImGui.Button("Finish"))
            {
                FinishTutorial();
            }
        }
    }

    private void FinishTutorial()
    {
        MarkTutorialComplete();
        IsOpen = false;
    }

	private static void MarkTutorialComplete()
	{
		Service.Config.TutorialDone = true;
		Service.Config.Save();
	}

    private sealed record TutorialStep(
        string Title,
        string Description,
        RotationConfigWindowTab? Tab = null,
        string[]? Bullets = null,
        string[]? RecommendedMacros = null);
}