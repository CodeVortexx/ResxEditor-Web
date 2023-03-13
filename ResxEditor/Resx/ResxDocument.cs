using System.Text.RegularExpressions;
using System.Xml;

namespace ResxEditor.Resx;

public partial class ResxDocument
{
    public SortedDictionary<string, Dictionary<string, string>> Values { get; } = new();

    public readonly string Name;
    public readonly string Locale = "en-US";

    public ResxDocument(string fileName)
    {
        Name = Path.GetFileNameWithoutExtension(fileName);

        var match = LocaleRegex().Match(fileName);
        if (string.IsNullOrEmpty(match.Value))
            return;

        Locale = match.Value;
    }

    /// <summary>
    /// Parses the ResxDocument
    /// </summary>
    /// <param name="xmlStream"></param>
    public void Parse(Stream xmlStream)
    {
        Values.Clear();

        xmlStream.Position = 0;

        var xml = new XmlDocument();
        xml.Load(xmlStream);

        var root = xml.DocumentElement;
        if (root == null)
            return;

        foreach (XmlNode node in root.ChildNodes)
        {
            if (node.Name != "data")
                continue;

            var name = node.Attributes?["name"]?.Value;
            if (name == null)
                continue;

            var valueNode = node.SelectSingleNode("value");
            if (valueNode == null)
                continue;

            var value = valueNode.InnerText;

            if (!Values.ContainsKey(name))
                Values.Add(name, new Dictionary<string, string>());

            Values[name].Add(Locale, value);
        }
    }

    /// <summary>
    /// Merges the <see cref="Values"/> container from a different <see cref="ResxDocument"/>
    /// </summary>
    /// <param name="otherDocument"></param>
    public void Merge(ResxDocument otherDocument)
    {
        // We cannot merge into locale resx files.
        if (Locale != "en-US" || Locale == otherDocument.Locale)
            return;

        // We cannot merge files with a different start of the name
        if (!otherDocument.Name.StartsWith(Name))
            return;

        foreach (var (key, dict) in otherDocument.Values)
        {
            // Add the dictionary from the other document to this document
            // And add a placeholder for this document's locale
            if (!Values.ContainsKey(key))
            {
                Values.Add(key, dict);

                foreach (var (_, _) in dict)
                    Values[key].Add(Locale, "");
            }
            else
            {
                foreach (var (locale, value) in dict)
                {
                    Values[key].Add(locale, value);
                }
            }
        }
    }

    [GeneratedRegex("\\w{2}(?:-\\w{2})")]
    private static partial Regex LocaleRegex();
}
