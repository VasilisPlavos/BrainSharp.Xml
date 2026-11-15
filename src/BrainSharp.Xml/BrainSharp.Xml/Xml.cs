using System.Xml.Linq;

namespace BrainSharp.Xml;

public class Xml
{
    [Obsolete("Xml will become a static class in 2.0.0. Call the static methods directly.")]
    public Xml() { }

    public static List<string> ExplainDifference(XDocument doc1, XDocument doc2, bool ignoreNodeValues = false)
    {
        return CompareNodes(GetRoot(doc1), GetRoot(doc2), ignoreNodeValues);
    }

    public static List<string> ExplainDifference(string xml1Content, string xml2Content, bool ignoreNodeValues = false)
    {
        var doc1 = Parse(xml1Content);
        var doc2 = Parse(xml2Content);
        return CompareNodes(GetRoot(doc1), GetRoot(doc2), ignoreNodeValues);
    }

    public static List<string> ExplainDifferenceFromFiles(string file1Path, string file2Path, bool ignoreNodeValues = false)
    {
        var doc1 = ParseFromFile(file1Path);
        var doc2 = ParseFromFile(file2Path);
        return CompareNodes(GetRoot(doc1), GetRoot(doc2), ignoreNodeValues);
    }

    public static Task<List<string>> ExplainDifferenceFromFilesAsync(string file1Path, string file2Path, bool ignoreNodeValues = false)
        => ExplainDifferenceFromFilesAsync(file1Path, file2Path, ignoreNodeValues, CancellationToken.None);

    public static async Task<List<string>> ExplainDifferenceFromFilesAsync(string file1Path, string file2Path, bool ignoreNodeValues, CancellationToken cancellationToken)
    {
        var doc1 = await ParseFromFileAsync(file1Path, cancellationToken);
        var doc2 = await ParseFromFileAsync(file2Path, cancellationToken);
        return CompareNodes(GetRoot(doc1), GetRoot(doc2), ignoreNodeValues);
    }

    public static bool IsEqual(XDocument doc1, XDocument doc2, bool ignoreNodeValues = false)
    {
        var differences = CompareNodes(GetRoot(doc1), GetRoot(doc2), ignoreNodeValues);
        return differences.Count == 0;
    }

    /// <summary>
    /// Create a new <see cref="XDocument"/> from a string containing XML.
    /// </summary>
    /// <param name="text">A string containing XML.</param>
    /// <returns>
    /// An <see cref="XDocument"/> containing an XML tree initialized from the passed in XML string.
    /// </returns>
    public static XDocument Parse(string text)
    {
        return XDocument.Parse(text);
    }

    public static XDocument ParseFromFile(string path)
    {
        return XDocument.Load(path);
    }

    public static Task<XDocument> ParseFromFileAsync(string path)
        => ParseFromFileAsync(path, CancellationToken.None);

    public static async Task<XDocument> ParseFromFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
    }

    private static XElement GetRoot(XDocument doc)
    {
        return doc.Root ?? throw new ArgumentException("The XML document has no root element.", nameof(doc));
    }

    private static List<string> CompareNodes(XElement node1, XElement node2, bool ignoreNodeValues = false)
    {
        var differences = new List<string>();

        if (node1.Name != node2.Name)
        {
            differences.Add($"Tags differ: {node1.Name} vs {node2.Name}");
            return differences;
        }

        // Text comparison only applies when the first node has exactly one child node
        var firstChild = node1.FirstNode;
        if (!ignoreNodeValues && firstChild != null && firstChild.NextNode == null)
        {
            if (node1.Value.Trim() != node2.Value.Trim())
            {
                differences.Add($"Texts differ on node {node1.Name}: {node1.Value} vs {node2.Value}");
            }
        }

        // Compare attributes
        var node1Attributes = node1.Attributes().ToDictionary(a => a.Name, a => a.Value);
        var node2Attributes = node2.Attributes().ToDictionary(a => a.Name, a => a.Value);

        foreach (var attr1 in node1Attributes)
        {
            if (!node2Attributes.TryGetValue(attr1.Key, out var attr2Value))
            {
                differences.Add($"Missing attribute on second xml {attr1.Key}");
                continue;
            }

            if (attr1.Value != attr2Value)
            {
                differences.Add($"Attributes differ on key {attr1.Key} with values {attr1.Value} vs {attr2Value}");
            }
        }

        foreach (var attr2 in node2Attributes)
        {
            if (!node1Attributes.ContainsKey(attr2.Key))
            {
                differences.Add($"Missing attribute on first xml {attr2.Key}");
            }
        }

        CompareChildElements(node1, node2, ignoreNodeValues, differences);
        return differences;
    }

    /// <summary>
    /// Pairs the child elements of both nodes in two passes: first identical elements,
    /// then (for what is left) elements with the same local name. Namespaces are ignored on the second pass.
    /// </summary>
    private static void CompareChildElements(XElement node1, XElement node2, bool ignoreNodeValues, List<string> differences)
    {
        var elements1 = node1.Elements().ToList();
        var elements2 = node2.Elements().ToList();
        if (elements1.Count == 0 && elements2.Count == 0) return;

        var pool = new Dictionary<string, List<XElement>>();
        foreach (var element in elements2)
        {
            if (!pool.TryGetValue(element.Name.LocalName, out var group))
            {
                pool[element.Name.LocalName] = group = new List<XElement>();
            }
            group.Add(element);
        }

        // Pass 1: identical elements need no further comparison
        var unpaired = new List<XElement>();
        foreach (var element1 in elements1)
        {
            var matched = false;
            if (pool.TryGetValue(element1.Name.LocalName, out var group))
            {
                var index = group.FindIndex(x => XNode.DeepEquals(x, element1));
                if (index >= 0)
                {
                    group.RemoveAt(index);
                    matched = true;
                }
            }

            if (!matched) unpaired.Add(element1);
        }

        // Pass 2: same local name, compare recursively
        foreach (var element1 in unpaired)
        {
            if (!pool.TryGetValue(element1.Name.LocalName, out var group) || group.Count == 0)
            {
                differences.Add($"Missing child node on second xml: {element1.Name}");
                continue;
            }

            var element2 = group[0];
            group.RemoveAt(0);
            differences.AddRange(CompareNodes(element1, element2, ignoreNodeValues));
        }

        // Whatever is left in the second xml has no counterpart in the first
        var leftovers = new HashSet<XElement>(pool.Values.SelectMany(g => g));
        foreach (var element2 in elements2)
        {
            if (leftovers.Contains(element2))
            {
                differences.Add($"Missing child node on first xml: {element2.Name}");
            }
        }
    }
}
