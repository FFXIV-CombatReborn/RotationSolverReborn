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
	/// <summary>
	/// Initializes the incremental generator.
	/// </summary>
	/// <param name="context">The initialization context.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"RotationSolver.Basic.Attributes.ConditionBoolAttribute",
			static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
			static (n, ct) => ((VariableDeclaratorSyntax)n.TargetNode, n.SemanticModel))
			.Where(m => m.Item1 != null);

		context.RegisterSourceOutput(provider.Collect(), Execute);
	}

	/// <summary>
	/// Executes the source generation.
	/// </summary>
	/// <param name="context">The source production context.</param>
	/// <param name="array">The array of variable declarators and semantic models.</param>
	private void Execute(SourceProductionContext context, ImmutableArray<(VariableDeclaratorSyntax, SemanticModel SemanticModel)> array)
	{
		var dict = new Dictionary<SyntaxNode, List<(VariableDeclaratorSyntax, SemanticModel)>>();
		foreach (var entry in array)
		{
			var key = entry.Item1.Parent!.Parent!.Parent!;
			if (!dict.TryGetValue(key, out var list))
			{
				list = new List<(VariableDeclaratorSyntax, SemanticModel)>();
				dict[key] = list;
			}
			list.Add((entry.Item1, entry.SemanticModel));
		}

		foreach (var kv in dict)
		{
			var type = (TypeDeclarationSyntax)kv.Key;
			var nameSpace = type.GetParent<BaseNamespaceDeclarationSyntax>()?.Name.ToString() ?? "Null";
			var classType = type is ClassDeclarationSyntax ? "class" : "struct";
			var className = type.Identifier.Text;

			var propertyCodes = GeneratePropertyCodes(kv.Value, context, nameSpace, className);

			if (propertyCodes.Count == 0) continue;

			var code = GenerateClassCode(nameSpace, classType, className, propertyCodes);
			context.AddSource($"{nameSpace}_{className}.g.cs", code);
		}
	}

	/// <summary>
	/// Generates the property codes for the given group of variables.
	/// </summary>
	/// <param name="group">The group of variables.</param>
	/// <param name="context">The source production context.</param>
	/// <param name="nameSpace">The namespace of the class.</param>
	/// <param name="className">The name of the class.</param>
	/// <returns>A list of property code strings.</returns>
	private List<string> GeneratePropertyCodes(IEnumerable<(VariableDeclaratorSyntax, SemanticModel)> group, SourceProductionContext context, string nameSpace, string className)
	{
		var propertyCodes = new List<string>();

		foreach (var (variableInfo, model) in group)
		{
			var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;
			var variableName = variableInfo.Identifier.ToString();
			var propertyName = variableName.ToPascalCase();

			if (variableName == propertyName)
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("RS001", "Naming Warning", "Please don't use Pascal Case to name your field!", "Naming", DiagnosticSeverity.Warning, true), variableInfo.Identifier.GetLocation()));
				continue;
			}

			var fieldType = model.GetTypeInfo(field.Declaration.Type).Type!;
			if (fieldType.GetFullMetadataName() != "System.Boolean")
			{
				context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("RS002", "Type Warning", "Field type must be System.Boolean", "Type", DiagnosticSeverity.Warning, true), variableInfo.GetLocation()));
				continue;
			}

			var attributeStr = GetFieldAttributes(field, model);
			var propertyCode = $$"""
                {{attributeStr}}
                public ConditionBoolean {{propertyName}} { get; private set; } = new({{variableName}}, "{{propertyName}}");
            """;

			propertyCodes.Add(propertyCode);
		}

		return propertyCodes;
	}

	/// <summary>
	/// Generates the class code with the given properties.
	/// </summary>
	/// <param name="nameSpace">The namespace of the class.</param>
	/// <param name="classType">The type of the class (class or struct).</param>
	/// <param name="className">The name of the class.</param>
	/// <param name="propertyCodes">The list of property code strings.</param>
	/// <returns>The generated class code.</returns>
	private string GenerateClassCode(string nameSpace, string classType, string className, List<string> propertyCodes)
	{
		var sb = new StringBuilder();
		sb.AppendLine("using RotationSolver.Basic.Data;");
		sb.AppendLine();
		sb.AppendLine($"namespace {nameSpace}");
		sb.AppendLine("{");
		sb.AppendLine($"    partial {classType} {className}");
		sb.AppendLine("    {");

		foreach (var propertyCode in propertyCodes)
		{
			sb.AppendLine("        " + propertyCode);
			sb.AppendLine();
		}

		sb.AppendLine("    }");
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Gets the attributes of the field as a string.
	/// </summary>
	/// <param name="field">The field declaration syntax.</param>
	/// <param name="model">The semantic model.</param>
	/// <returns>The attributes as a string.</returns>
	private string GetFieldAttributes(FieldDeclarationSyntax field, SemanticModel model)
	{
		var names = new List<string>();

		foreach (var attrSet in field.AttributeLists)
		{
			if (attrSet == null) continue;

			foreach (var attr in attrSet.Attributes)
			{
				if (model.GetSymbolInfo(attr).Symbol?.GetFullMetadataName()
					is "RotationSolver.Basic.Attributes.UIAttribute"
					or "RotationSolver.Basic.Attributes.UnitAttribute"
					or "RotationSolver.Basic.Attributes.RangeAttribute"
					or "RotationSolver.Basic.Attributes.LinkDescriptionAttribute")
				{
					names.Add(attr.ToString());
				}
			}
		}

		return names.Count == 0 ? "" : $"[{string.Join(", ", names)}]";
	}
}