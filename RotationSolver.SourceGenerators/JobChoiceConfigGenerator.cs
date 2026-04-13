using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace RotationSolver.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class JobChoiceConfigGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
			"RotationSolver.Basic.Attributes.JobChoiceConfigAttribute",
			static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
			static (n, ct) => ((VariableDeclaratorSyntax)n.TargetNode, n.SemanticModel))
			.Where(m => m.Item1 != null);

		context.RegisterSourceOutput(provider.Collect(), Execute);
	}

	private void Execute(SourceProductionContext context, ImmutableArray<(VariableDeclaratorSyntax, SemanticModel SemanticModel)> array)
	{
		var typeGroups = array.GroupBy(variable => variable.Item1.Parent!.Parent!.Parent!);

		foreach (var group in typeGroups)
		{
			var type = (TypeDeclarationSyntax)group.Key;
			var namespaceName = type.GetParent<BaseNamespaceDeclarationSyntax>()?.Name.ToString() ?? "Null";
			var classType = type is ClassDeclarationSyntax ? "class" : "struct";
			var className = type.Identifier.Text;

			var propertyCodes = new List<string>();
			foreach (var (variableInfo, model) in group)
			{
				try
				{
					var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;
					var variableName = variableInfo.Identifier.ToString();
					var propertyName = variableName.ToPascalCase();

					if (variableName == propertyName)
					{
						// Skip fields with PascalCase names
						continue;
					}

					var fieldTypeStr = field.Declaration.Type;
					var fieldType = model.GetTypeInfo(fieldTypeStr).Type!;
					var fieldStr = fieldType.GetFullMetadataName();

					var attributeNames = new List<string>();
					foreach (var attrSet in field.AttributeLists)
					{
						if (attrSet == null) continue;
						foreach (var attr in attrSet.Attributes)
						{
							var name = model.GetSymbolInfo(attr).Symbol?.GetFullMetadataName();
							if (name is "RotationSolver.Basic.Attributes.UIAttribute"
								or "RotationSolver.Basic.Attributes.UnitAttribute"
								or "RotationSolver.Basic.Attributes.RangeAttribute"
								or "RotationSolver.Basic.Attributes.JobChoiceConfigAttribute"
								or "RotationSolver.Basic.Attributes.LinkDescriptionAttribute")
							{
								attributeNames.Add(attr.ToString());
							}
						}
					}

					var attributeStr = attributeNames.Count == 0 ? "" : $"[{string.Join(", ", attributeNames)}]";
					var propertyCode = $$"""
                        [JsonProperty]
                        private Dictionary<Job, Dictionary<string, {{fieldStr}}>> {{variableName}}Dict = new();

                        [JsonIgnore]
                        {{attributeStr}}
                        public {{fieldStr}} {{propertyName}}
                        {
                            get
                            {
                                if (!{{variableName}}Dict.TryGetValue(DataCenter.Job, out var dict))
                                {
                                    dict = {{variableName}}Dict[DataCenter.Job] = new();
                                }

                                if (!dict.TryGetValue(RotationChoice, out var value))
                                {
                                    value = dict[RotationChoice] = {{variableName}};
                                }

                                return value;
                            }
                            set
                            {
                                {{variableName}}Dict[DataCenter.Job][RotationChoice] = value;
                            }
                        }
                    """;

					propertyCodes.Add(propertyCode);
				}
				catch (Exception ex)
				{
					context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
						"JCCG001",
						"Error generating property",
						$"An error occurred while generating property for {variableInfo.Identifier}: {ex.Message}",
						"JobChoiceConfigGenerator",
						DiagnosticSeverity.Error,
						isEnabledByDefault: true), variableInfo.GetLocation()));
				}
			}

			if (propertyCodes.Count == 0) continue;

			var code = $$"""
                using ECommons.ExcelServices;

                namespace {{namespaceName}}
                {
                    partial {{classType}} {{className}}
                    {
                        {{string.Join("\n\n", propertyCodes)}}
                    }
                }
            """;

			context.AddSource($"{namespaceName}_{className}.g.cs", code);
		}
	}
}