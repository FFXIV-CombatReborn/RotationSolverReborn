using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
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
		if (Condition is { } condition)
		{
			condition.ResetValue();
		}
	}

	private abstract class CheckBoxConditionAbstract : CheckBoxSearch
	{
		protected readonly ConditionBoolean _condition;
		public override string SearchingKeys => string.Empty;

		public override string Command => string.Empty;

		public override string ID => base.ID + Name;

		public override bool ShowInChild => false;

		public CheckBoxConditionAbstract(PropertyInfo property) : base(property)
		{
			_condition = (ConditionBoolean)property.GetValue(Service.Config)!;
			AdditionalDraw = () =>
			{
				if (DataCenter.CurrentRotation == null)
				{
					return;
				}
			};
		}

		public override void ResetToDefault()
		{
			Value = false;
		}
	}

	public override bool AlwaysShowChildren => false;
	protected override void DrawMiddle()
	{
		if (AlwaysShowChildren)
		{
			ImGui.SameLine();
		}
		base.DrawMiddle();
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

	public Action? AdditionalDraw { get; set; } = null;

	public virtual bool AlwaysShowChildren => false;

	public override string Description => Action == ActionID.None ? base.Description : Action.ToString();

	internal CheckBoxSearch(PropertyInfo property, params ISearchable[] children)
		: base(property)
	{
		Action = property.GetCustomAttribute<UIAttribute>()?.Action ?? ActionID.None;
		foreach (ISearchable child in children)
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
		bool lastIs = false;
		foreach (ISearchable child in Children)
		{
			if (!child.ShowInChild)
			{
				continue;
			}

			bool thisIs = child is CheckBoxSearch c && c.Action != ActionID.None && c.Action.GetTexture(out IDalamudTextureWrap? texture);
			if (lastIs && thisIs)
			{
				ImGui.SameLine();
			}
			lastIs = thisIs;

			child.Draw();
		}
	}

	protected virtual void DrawMiddle()
	{

	}

	protected override void DrawMain()
	{
		bool hasChild = false;
		if (Children != null)
		{
			foreach (ISearchable c in Children)
			{
				if (c.ShowInChild)
				{
					hasChild = true;
					break;
				}
			}
		}
		bool hasAdditional = AdditionalDraw != null;
		bool hasSub = hasChild || hasAdditional;
		IDalamudTextureWrap? texture = null;
		bool hasIcon = Action != ActionID.None && Action.GetTexture(out texture);

		bool enable = Value;
		if (ImGui.Checkbox($"##{ID}", ref enable))
		{
			Value = enable;
		}
		if (ImGui.IsItemHovered())
		{
			ShowTooltip();
		}

		ImGui.SameLine();

		string name = $"{Name}##Config_{ID}{GetHashCode()}";
		if (hasIcon)
		{
			ImGui.BeginGroup();
			Vector2 cursor = ImGui.GetCursorPos();
			float size = ImGuiHelpers.GlobalScale * 32;
			if (texture?.Handle != null && ImGuiHelper.NoPaddingNoColorImageButton(texture, Vector2.One * size, ID))
			{
				Value = enable;
			}
			ImGuiHelper.DrawActionOverlay(cursor, size, enable ? 1 : 0);
			ImGui.EndGroup();

			if (ImGui.IsItemHovered())
			{
				ShowTooltip();
			}
		}
		else if (hasSub)
		{
			if (enable || AlwaysShowChildren)
			{
				float x = ImGui.GetCursorPosX();
				DrawMiddle();
				bool drawBody = ImGui.TreeNode(name);
				if (ImGui.IsItemHovered())
				{
					ShowTooltip();
				}

				if (drawBody)
				{
					ImGui.SetCursorPosX(x);
					ImGui.BeginGroup();
					AdditionalDraw?.Invoke();
					if (hasChild)
					{
						DrawChildren();
					}
					ImGui.EndGroup();
					ImGui.TreePop();
				}
			}
			else
			{
				ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x0);
				ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0x0);
				_ = ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
				if (ImGui.IsItemHovered())
				{
					ShowTooltip(false);
				}

				ImGui.PopStyleColor(2);
			}
		}
		else
		{
			ImGui.TextWrapped(Name);
			if (ImGui.IsItemHovered())
			{
				ShowTooltip(false);
			}
		}
		// Draw job icon if IsJob is true
		if (IsJob)
		{
			DrawJobIcon();
		}
	}
}