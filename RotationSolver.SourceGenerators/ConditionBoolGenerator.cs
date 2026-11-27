using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace RotationSolver.SourceGenerators;

/// <summary>
/// Source generator for creating properties from fields marked with the ConditionBoolAttribute.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ConditionBoolGenerator : IIncrementalGenerator
{
	private static readonly DiagnosticDescriptor NamingWarningDescriptor = new(
		"RS001",
		"Naming Warning",
		"Please don't use Pascal Case to name your field!",
		"Naming",
		DiagnosticSeverity.Warning,
		true);

	private static readonly DiagnosticDescriptor TypeWarningDescriptor = new(
		"RS002",
		"Type Warning",
		"Field type must be System.Boolean",
		"Type",
		DiagnosticSeverity.Warning,
		true);

	private sealed class ConditionBoolModel : IEquatable<ConditionBoolModel>
	{
		public string NamespaceName { get; }
		public ImmutableArray<TypeHierarchyPart> TypeHierarchy { get; }
		public string VariableName { get; }
		public string PropertyName { get; }
		public ImmutableArray<string> Attributes { get; }
		public DiagnosticSpec? Diagnostic { get; }

		public ConditionBoolModel(
			string namespaceName,
			ImmutableArray<TypeHierarchyPart> typeHierarchy,
			string variableName,
			string propertyName,
			ImmutableArray<string> attributes,
			DiagnosticSpec? diagnostic)
		{
			NamespaceName = namespaceName;
			TypeHierarchy = typeHierarchy;
			VariableName = variableName;
			PropertyName = propertyName;
			Attributes = attributes;
			Diagnostic = diagnostic;
		}

		public bool Equals(ConditionBoolModel? other)
			=> other is not null
			&& NamespaceName == other.NamespaceName
			&& VariableName == other.VariableName
			&& PropertyName == other.PropertyName
			&& Nullable.Equals(Diagnostic, other.Diagnostic)
			&& Util.SequenceEquals(TypeHierarchy, other.TypeHierarchy)
			&& Util.StringSequenceEquals(Attributes, other.Attributes);

		public override bool Equals(object? obj) => Equals(obj as ConditionBoolModel);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = NamespaceName.GetHashCode();
				hash = (hash * 397) ^ VariableName.GetHashCode();
				hash = (hash * 397) ^ PropertyName.GetHashCode();
				hash = (hash * 397) ^ (Diagnostic?.GetHashCode() ?? 0);
				hash = (hash * 397) ^ Util.GetSequenceHashCode(TypeHierarchy);
				hash = (hash * 397) ^ Util.GetSequenceHashCode(Attributes);
				return hash;
			}
		}
	}

	/// <summary>
	/// Initializes the incremental generator.
	/// </summary>
	/// <param name="context">The initialization context.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"RotationSolver.Engine.Attributes.ConditionBoolAttribute",
			static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
			static (n, _) => Transform(n))
			.Where(static m => m is not null);

		context.RegisterSourceOutput(provider.Collect(), Execute);
	}

	private static ConditionBoolModel? Transform(GeneratorAttributeSyntaxContext context)
	{
		if (context.TargetNode is not VariableDeclaratorSyntax variableInfo
			|| context.TargetSymbol is not IFieldSymbol fieldSymbol
			|| variableInfo.Parent?.Parent is not FieldDeclarationSyntax field)
		{
			return null;
		}

		var containingType = fieldSymbol.ContainingType;
		var namespaceName = Util.GetNamespaceDisplay(containingType);
		var typeHierarchy = Util.GetTypeHierarchy(containingType);
		var variableName = variableInfo.Identifier.ValueText;
		var propertyName = variableName.ToPascalCase();

		if (variableName == propertyName)
		{
			return new ConditionBoolModel(
				namespaceName,
				typeHierarchy,
				variableName,
				propertyName,
				ImmutableArray<string>.Empty,
				new DiagnosticSpec("RS001", variableInfo.Identifier.GetLocation()));
		}

		if (fieldSymbol.Type.SpecialType != SpecialType.System_Boolean)
		{
			return new ConditionBoolModel(
				namespaceName,
				typeHierarchy,
				variableName,
				propertyName,
				ImmutableArray<string>.Empty,
				new DiagnosticSpec("RS002", variableInfo.GetLocation()));
		}

		var attributes = Util.GetAllowedAttributes(
			field,
			context.SemanticModel,
			"RotationSolver.Engine.Attributes.UIAttribute",
			"RotationSolver.Engine.Attributes.UnitAttribute",
			"RotationSolver.Engine.Attributes.RangeAttribute",
			"RotationSolver.Engine.Attributes.LinkDescriptionAttribute");

		return new ConditionBoolModel(namespaceName, typeHierarchy, variableName, propertyName, attributes, null);
	}

	private static void Execute(SourceProductionContext context, ImmutableArray<ConditionBoolModel?> array)
	{
		var groups = new Dictionary<string, (string NamespaceName, ImmutableArray<TypeHierarchyPart> Hierarchy, List<ConditionBoolModel> Items)>(StringComparer.Ordinal);
		foreach (var item in array)
		{
			if (item == null)
			{
				continue;
			}

			if (item.Diagnostic is { } diag)
			{
				var descriptor = diag.Id == "RS001" ? NamingWarningDescriptor : TypeWarningDescriptor;
				context.ReportDiagnostic(Diagnostic.Create(descriptor, diag.ToLocation()));
				continue;
			}

			var key = Util.GetSafeHintPrefix(item.NamespaceName, item.TypeHierarchy);
			if (!groups.TryGetValue(key, out var group))
			{
				group = (item.NamespaceName, item.TypeHierarchy, new List<ConditionBoolModel>());
				groups[key] = group;
			}
			group.Items.Add(item);
		}

		foreach (var kv in groups)
		{
			var (namespaceName, hierarchy, items) = kv.Value;
			if (items.Count == 0)
			{
				continue;
			}

			var propertyCodes = new List<string>(items.Count);
			foreach (var item in items)
			{
				var attributeStr = item.Attributes.Length == 0 ? string.Empty : $"[{string.Join(", ", item.Attributes)}]";
				var propertyCode = $$"""
                {{attributeStr}}
                public ConditionBoolean {{item.PropertyName}} { get; private set; } = new({{item.VariableName}}, "{{item.PropertyName}}");
            """;
				propertyCodes.Add(propertyCode);
			}

			var code = GenerateClassCode(namespaceName, hierarchy, propertyCodes);
			context.AddSource($"{kv.Key}_ConditionBool.g.cs", code);
		}
	}

	private static string GenerateClassCode(string namespaceName, ImmutableArray<TypeHierarchyPart> hierarchy, List<string> propertyCodes)
	{
		var sb = new StringBuilder();
		sb.AppendLine("using RotationSolver.Engine.Data;");
		sb.AppendLine();

		var indent = string.Empty;
		if (!string.IsNullOrEmpty(namespaceName))
		{
			sb.AppendLine($"namespace {namespaceName}");
			sb.AppendLine("{");
			indent = "    ";
		}

		for (var i = 0; i < hierarchy.Length; i++)
		{
			sb.AppendLine($"{indent}partial {hierarchy[i].Keyword} {hierarchy[i].NameWithTypeParams}");
			sb.AppendLine($"{indent}{{");
			indent += "    ";
		}

		foreach (var propertyCode in propertyCodes)
		{
			sb.AppendLine(indent + propertyCode);
			sb.AppendLine();
		}

		for (var i = hierarchy.Length - 1; i >= 0; i--)
		{
			indent = indent.Substring(0, indent.Length - 4);
			sb.AppendLine($"{indent}}}");
		}

		if (!string.IsNullOrEmpty(namespaceName))
		{
			sb.AppendLine("}");
		}

		return sb.ToString();
	}
}