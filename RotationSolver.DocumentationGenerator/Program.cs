using System.Text.RegularExpressions;
using System.Xml;

namespace RotationSolver.DocumentationGenerator
{
	internal class Program
	{
		private class Entry
		{
			private static Regex slugRegex = new("[^a-z0-9-]", RegexOptions.Compiled);

			public bool HasRef { get; set; } = false;
			public required string Page { get; init; }
			public required string Name { get; init; }
			public required string Section { get; init; }
			public required string Subsection { get; init; }
			public bool IsSubsection { get; init; } = false;
			public required XmlElement Contents { get; init; }

			private string MarkdownSlug => slugRegex.Replace(Name.ToLowerInvariant().Replace(' ', '-'), "");

			public string ProcessContent(Dictionary<string, Entry> entries)
			{
				if (!HasRef)
				{
					return RemoveWhitespace(Contents.InnerText);
				}

				var contents = "";

				foreach (XmlNode node in Contents.ChildNodes)
				{
					switch (node.NodeType)
					{
						case XmlNodeType.Text:
							contents += node.Value;
							break;
						case XmlNodeType.Element:
							var cref = GetCref((node as XmlElement)!);

							if (!entries.ContainsKey(cref))
							{
								contents += node;
								break;
							}

							contents += $"[{node.InnerText}]({entries[cref].Page}#{entries[cref].MarkdownSlug})";
							break;
					}
				}

				return RemoveWhitespace(contents);
			}

			private string GetCref(XmlElement node)
			{
				return node.Attributes!["cref"]!.Value[2..];
			}

			private string RemoveWhitespace(string data)
			{
				var parts = data.Replace("\r", "").Trim().Split("\n");
				var sb = new System.Text.StringBuilder();
				for (int i = 0; i < parts.Length; i++)
				{
					string line = parts[i];
					string processed = line == "\n" ? line : "> " + line.Trim();
					if (i > 0) sb.Append('\n');
					sb.Append(processed);
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// File => Section => Subsection => Entry[]
		/// </summary>
		private static Dictionary<string, Dictionary<string, Dictionary<string, List<Entry>>>> Files = new();

		/// <summary>
		/// Fully-qualified type => Entry
		/// </summary>
		private static Dictionary<string, Entry> Entries = new();

		static void Main(string[] files)
		{
			foreach (var file in files)
			{
				var doc = new XmlDocument();

				try
				{
					doc.Load(file);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to parse {file}. Skipping. {e.StackTrace}");

					continue;
				}


				foreach (XmlElement node in doc.GetElementsByTagName("markdown")!)
				{
					var entry = ParseNode(node);

					Entries.Add(node.ParentNode!.Attributes!["name"]!.Value[2..], entry);
					var fileName = node.Attributes!["file"]!.Value;
					if (!Files.ContainsKey(fileName))
					{
						Files.Add(fileName, new());
					}

					if (!Files[fileName].ContainsKey(entry.Section))
					{
						Files[fileName].Add(entry.Section, new());
					}

					if (!Files[fileName][entry.Section].ContainsKey(entry.Subsection))
					{
						Files[fileName][entry.Section].Add(entry.Subsection, new());
					}

					Files[fileName][entry.Section][entry.Subsection].Add(entry);
				}
			}

			if (!Directory.Exists("_doc"))
				Directory.CreateDirectory("_doc");

			foreach (var (fileName, sections) in Files)
			{
				var fileContents = "";

				foreach (var (section, subsections) in sections)
				{
					if (!string.IsNullOrEmpty(section))
						fileContents += $"# {section}\n";

					foreach (var (subsection, entries) in subsections)
					{
						if (!string.IsNullOrEmpty(subsection))
						{
							fileContents += $"## {Entries[subsection].Name}\n";
							fileContents += Entries[subsection].ProcessContent(Entries);
							fileContents += "\n\n";
						}

						foreach (var entry in entries)
						{
							if (entry.IsSubsection) continue;
							fileContents += $"### {entry.Name}\n";
							fileContents += entry.ProcessContent(Entries);
							fileContents += "\n\n";
						}
					}
				}

				File.WriteAllText(Path.Join("_doc", $"{fileName}.md"), fileContents);
				Console.WriteLine($"Created {fileName}.md");
			}

			// Now create the home page
			File.WriteAllText(Path.Join("_doc", "Home.md"), "# Welcome to the RSR Wiki!");
		}

		private static Entry ParseNode(XmlElement element)
		{
			return new Entry
			{
				Page = element.Attributes["file"]!.Value,
				Name = element.Attributes["name"]!.Value,
				Section = element.Attributes["section"]?.Value ?? "",
				IsSubsection = (element.Attributes["isSubsection"]?.Value ?? "") == "1",
				Subsection = element.Attributes["subsection"]?.Value ?? "",
				Contents = element,
				HasRef = element.SelectSingleNode("see") != null,
			};
		}
	}
}