using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.Configuration.Conditions;
using RotationSolver.UI.SearchableConfigs;

namespace RotationSolver.UI.SearchableSettings;

internal class CheckBoxSearchCondition(PropertyInfo property, params ISearchable[] children)
    : CheckBoxSearch(property,
    [
        new CheckBoxEnable(property),
        new CheckBoxDisable(property),
        .. children,
    ])
{
    private abstract class CheckBoxConditionAbstract : CheckBoxSearch
    {
        protected readonly ConditionBoolean _condition;
        public override string SearchingKeys => string.Empty;

        public override string Command => string.Empty;

        public override LinkDescription[]? Tooltips => null;

        public override string ID => base.ID + Name;

        public override bool ShowInChild => Service.Config.UseAdditionalConditions;

        public CheckBoxConditionAbstract(PropertyInfo property) : base(property)
        {
            _condition = (ConditionBoolean)property.GetValue(Service.Config)!;
            AdditionalDraw = () =>
            {
                if (DataCenter.CurrentRotation == null) return;
                GetCondition()?.DrawMain(DataCenter.CurrentRotation);
            };
        }

        protected abstract ConditionSet GetCondition();

        public override void ResetToDefault()
        {
            Value = false;
        }
    }

    private class CheckBoxDisable(PropertyInfo property) : CheckBoxConditionAbstract(property)
    {
        public override string Name => "Use Forced Disable Condition";

        public override string Description => "The conditions that force this to be false";

        protected override bool Value
        {
            get => _condition.Disable;
            set => _condition.Disable = value;
        }

        protected override ConditionSet GetCondition()
        {
            return DataCenter.CurrentConditionValue.GetDisableCondition(_condition.Key);
        }
    }

    private class CheckBoxEnable(PropertyInfo property) : CheckBoxConditionAbstract(property)
    {
        public override string Name => "Use Forced Enable Condition";

        public override string Description => "The conditions that force this to be true";

        protected override bool Value
        {
            get => _condition.Enable;
            set => _condition.Enable = value;
        }

        protected override ConditionSet GetCondition()
        {
            return DataCenter.CurrentConditionValue.GetEnableCondition(_condition.Key);
        }
    }

    private ConditionBoolean Condition => (ConditionBoolean)_property.GetValue(Service.Config)!;

    public override bool AlwaysShowChildren => Service.Config.UseAdditionalConditions;

    protected override bool Value
    {
        get => Condition.Value;
        set => Condition.Value = value;
    }

    public override void ResetToDefault()
    {
        Condition.ResetValue();
    }

    protected override void DrawMiddle()
    {
        if (AlwaysShowChildren)
        {
            ConditionRenderer.DrawCondition(Condition);
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
        var lastIs = false;
        foreach (var child in Children)
        {
            if (!child.ShowInChild) continue;

            var thisIs = child is CheckBoxSearch c && c.Action != ActionID.None && IconSet.GetTexture(c.Action, out var texture);
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
        var hasChild = Children != null && Children.Any(c => c.ShowInChild);
        var hasAdditional = AdditionalDraw != null;
        var hasSub = hasChild || hasAdditional;
        IDalamudTextureWrap? texture = null;
        var hasIcon = Action != ActionID.None && IconSet.GetTexture(Action, out texture);

        var enable = Value;
        if (ImGui.Checkbox($"##{ID}", ref enable))
        {
            Value = enable;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        var name = $"{Name}##Config_{ID}{GetHashCode()}";
        if (hasIcon)
        {
            ImGui.BeginGroup();
            var cursor = ImGui.GetCursorPos();
            var size = ImGuiHelpers.GlobalScale * 32;
            if (ImGuiHelper.NoPaddingNoColorImageButton(texture!.ImGuiHandle, Vector2.One * size, ID))
            {
                Value = enable;
            }
            ImGuiHelper.DrawActionOverlay(cursor, size, enable ? 1 : 0);
            ImGui.EndGroup();

            if (ImGui.IsItemHovered()) ShowTooltip();
        }
        else if (hasSub)
        {
            if (enable || AlwaysShowChildren)
            {
                var x = ImGui.GetCursorPosX();
                DrawMiddle();
                var drawBody = ImGui.TreeNode(name);
                if (ImGui.IsItemHovered()) ShowTooltip();

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
                ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                if (ImGui.IsItemHovered()) ShowTooltip(false);

                ImGui.PopStyleColor(2);
            }
        }
        else
        {
            ImGui.TextWrapped(Name);
            if (ImGui.IsItemHovered()) ShowTooltip(false);
        }
        // Draw job icon if IsJob is true
        if (IsJob) DrawJobIcon();
    }
}

