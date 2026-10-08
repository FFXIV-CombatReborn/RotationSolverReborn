using Dalamud.Interface.Textures.TextureWraps;
using RebornMaterial;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.UI.SearchableConfigs;

internal class CheckBoxCondition(PropertyInfo property, params ISearchable[] children)
	: CheckBoxSearch(property, children)
{
	private ConditionBoolean? Condition => _property.GetValue(Service.Config) as ConditionBoolean;

	protected override bool Value
	{
		get => Condition?.Value ?? false;
		set
		{
			if (Condition is { } condition)
			{
				condition.Value = value;
			}
		}
	}

	public override void ResetToDefault()
	{
		Condition?.ResetValue();
	}
}

internal class CheckBoxSearchNoCondition(PropertyInfo property, params ISearchable[] children)
	: CheckBoxSearch(property, children)
{
	protected override bool Value
	{
		get => (bool)_property.GetValue(Service.Config)!;
		set => _property.SetValue(Service.Config, value);
	}

	public override void ResetToDefault()
	{
		_property.SetValue(Service.Config, false);
	}
}

internal abstract class CheckBoxSearch : Searchable
{
	public List<ISearchable> Children { get; } = [];

	public ActionID Action { get; init; } = ActionID.None;

	public override string Description => Action == ActionID.None ? base.Description : Action.ToString();

	internal CheckBoxSearch(PropertyInfo property, params ISearchable[] children)
		: base(property)
	{
		Action = property.GetCustomAttribute<UIAttribute>()?.Action ?? ActionID.None;
		foreach (var child in children)
		{
			AddChild(child);
		}
	}

	public void AddChild(ISearchable child)
	{
		child.Parent = this;
		Children.Add(child);
	}

	protected abstract bool Value { get; set; }

	protected virtual void DrawChildren()
	{
		foreach (var child in Children)
		{
			child.Draw();
		}
	}

	protected override void DrawMain()
	{
		IDalamudTextureWrap? texture = null;
		if (Action != ActionID.None)
		{
			_ = Action.GetTexture(out texture);
		}

		var enable = Value;
		var row = M3SettingRow.Begin(Name, SupportingText, M3Widgets.SwitchSize(),
			leadingIcon: RowIcon, leadingTexture: texture);

		ImGui.SetCursorScreenPos(row.ControlPosition);
		if (M3Widgets.Switch($"##{ID}_switch", ref enable))
		{
			Value = enable;
		}

		RowInteractions(row);
		M3SettingRow.End(row);

		if (!enable || Children.Count == 0)
		{
			return;
		}

		using var group = M3SubGroup.Begin(M3.Scheme.Primary);
		DrawChildren();
	}
}
