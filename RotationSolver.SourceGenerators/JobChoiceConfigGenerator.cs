using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace RotationSolver.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class JobChoiceConfigGenerator : IIncrementalGenerator
{
	private static readonly DiagnosticDescriptor NamingWarningDescriptor = new(
		"RS004",
		"Field name should not be in Pascal Case",
		"Please don't use Pascal Case to name your field '{0}'",
		"Naming",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private sealed class JobChoiceConfigModel : IEquatable<JobChoiceConfigModel>
	{
		public string NamespaceName { get; }
		public ImmutableArray<TypeHierarchyPart> TypeHierarchy { get; }
		public string VariableName { get; }
		public string PropertyName { get; }
		public string FieldTypeDisplay { get; }
		public bool CanInstantiateDefault { get; }
		public ImmutableArray<string> Attributes { get; }
		public DiagnosticSpec? Diagnostic { get; }

		public JobChoiceConfigModel(
			string namespaceName,
			ImmutableArray<TypeHierarchyPart> typeHierarchy,
			string variableName,
			string propertyName,
			string fieldTypeDisplay,
			bool canInstantiateDefault,
			ImmutableArray<string> attributes,
			DiagnosticSpec? diagnostic)
		{
			NamespaceName = namespaceName;
			TypeHierarchy = typeHierarchy;
			VariableName = variableName;
			PropertyName = propertyName;
			FieldTypeDisplay = fieldTypeDisplay;
			CanInstantiateDefault = canInstantiateDefault;
			Attributes = attributes;
			Diagnostic = diagnostic;
		}

		public bool Equals(JobChoiceConfigModel? other)
			=> other is not null
			&& NamespaceName == other.NamespaceName
			&& VariableName == other.VariableName
			&& PropertyName == other.PropertyName
			&& FieldTypeDisplay == other.FieldTypeDisplay
			&& CanInstantiateDefault == other.CanInstantiateDefault
			&& Nullable.Equals(Diagnostic, other.Diagnostic)
			&& Util.SequenceEquals(TypeHierarchy, other.TypeHierarchy)
			&& Util.StringSequenceEquals(Attributes, other.Attributes);

		public override bool Equals(object? obj) => Equals(obj as JobChoiceConfigModel);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = NamespaceName.GetHashCode();
				hash = (hash * 397) ^ VariableName.GetHashCode();
				hash = (hash * 397) ^ PropertyName.GetHashCode();
				hash = (hash * 397) ^ FieldTypeDisplay.GetHashCode();
				hash = (hash * 397) ^ CanInstantiateDefault.GetHashCode();
				hash = (hash * 397) ^ (Diagnostic?.GetHashCode() ?? 0);
				hash = (hash * 397) ^ Util.GetSequenceHashCode(TypeHierarchy);
				hash = (hash * 397) ^ Util.GetSequenceHashCode(Attributes);
				return hash;
			}
		}
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"RotationSolver.Engine.Attributes.JobChoiceConfigAttribute",
			static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
			static (n, _) => Transform(n))
			.Where(static m => m is not null);

		context.RegisterSourceOutput(provider.Collect(), Execute);
	}

	private static JobChoiceConfigModel? Transform(GeneratorAttributeSyntaxContext context)
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
			return new JobChoiceConfigModel(
				namespaceName,
				typeHierarchy,
				variableName,
				propertyName,
				string.Empty,
				false,
				ImmutableArray<string>.Empty,
				new DiagnosticSpec("RS004", variableInfo.Identifier.GetLocation(), variableName));
		}

		var fieldType = fieldSymbol.Type;
		if (fieldType.TypeKind == TypeKind.Error)
		{
			return null;
		}

		var fieldTypeDisplay = fieldType.GetFullyQualifiedTypeName();
		var canInstantiateDefault = fieldType.CanInstantiateWithParameterlessCtor();
		var attributes = Util.GetAllowedAttributes(
			field,
			context.SemanticModel,
			"RotationSolver.Engine.Attributes.UIAttribute",
			"RotationSolver.Engine.Attributes.UnitAttribute",
			"RotationSolver.Engine.Attributes.RangeAttribute",
			"RotationSolver.Engine.Attributes.JobChoiceConfigAttribute",
			"RotationSolver.Engine.Attributes.LinkDescriptionAttribute");

		return new JobChoiceConfigModel(
			namespaceName,
			typeHierarchy,
			variableName,
			propertyName,
			fieldTypeDisplay,
			canInstantiateDefault,
			attributes,
			null);
	}

	private static void Execute(SourceProductionContext context, ImmutableArray<JobChoiceConfigModel?> array)
	{
		var groups = new Dictionary<string, (string NamespaceName, ImmutableArray<TypeHierarchyPart> Hierarchy, List<JobChoiceConfigModel> Items)>(StringComparer.Ordinal);
		foreach (var item in array)
		{
			if (item == null)
			{
				continue;
			}

			if (item.Diagnostic is { } diag)
			{
				context.ReportDiagnostic(Diagnostic.Create(NamingWarningDescriptor, diag.ToLocation(), diag.Arg));
				continue;
			}

			var key = Util.GetSafeHintPrefix(item.NamespaceName, item.TypeHierarchy);
			if (!groups.TryGetValue(key, out var group))
			{
				group = (item.NamespaceName, item.TypeHierarchy, new List<JobChoiceConfigModel>());
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
				var createExpr = item.CanInstantiateDefault ? $"new {item.FieldTypeDisplay}()" : item.VariableName;
				var propertyCode = $$"""
                    [JsonProperty]
                    private global::System.Collections.Concurrent.ConcurrentDictionary<Job, global::System.Collections.Concurrent.ConcurrentDictionary<string, {{item.FieldTypeDisplay}}>> {{item.VariableName}}Dict = new();

                    [JsonIgnore]
                    {{attributeStr}}
                    public {{item.FieldTypeDisplay}} {{item.PropertyName}}
                    {
                        get
                        {
                            var choiceKey = (DataCenter.IsPvP ? PvPRotationChoice : RotationChoice) ?? string.Empty;
                            var dict = {{item.VariableName}}Dict.GetOrAdd(DataCenter.Job, static _ => new global::System.Collections.Concurrent.ConcurrentDictionary<string, {{item.FieldTypeDisplay}}>());
                            if (dict.TryGetValue(choiceKey, out var value)) return value;
                            return dict.GetOrAdd(choiceKey, {{createExpr}});
                        }
                        set
                        {
                            var choiceKey = (DataCenter.IsPvP ? PvPRotationChoice : RotationChoice) ?? string.Empty;
                            var dict = {{item.VariableName}}Dict.GetOrAdd(DataCenter.Job, static _ => new global::System.Collections.Concurrent.ConcurrentDictionary<string, {{item.FieldTypeDisplay}}>());
                            dict[choiceKey] = value;
                        }
                    }
                """;
				propertyCodes.Add(propertyCode);
			}

			var sb = new StringBuilder();
			sb.AppendLine("using ECommons.ExcelServices;");
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

			sb.AppendLine(string.Join("\n\n", propertyCodes));

			for (var i = hierarchy.Length - 1; i >= 0; i--)
			{
				indent = indent.Substring(0, indent.Length - 4);
				sb.AppendLine($"{indent}}}");
			}

			if (!string.IsNullOrEmpty(namespaceName))
			{
				sb.AppendLine("}");
			}

			context.AddSource($"{kv.Key}_JobChoiceConfig.g.cs", sb.ToString());
		}
	}
}