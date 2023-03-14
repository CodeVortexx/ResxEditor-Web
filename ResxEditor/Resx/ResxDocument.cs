using System.Text.RegularExpressions;
using System.Xml;
using ResxEditor.Writers;

namespace ResxEditor.Resx;

public partial class ResxDocument
{
    public SortedDictionary<string, string> Values { get; } = new();

    public readonly string Name;
    public readonly string Namespace;
    public readonly string Locale = "en-US";

    private readonly Dictionary<string, ResxDocument> _subDocuments = new();
    private bool _isMainDocument;

    public ResxDocument(string fileName, string namespaceString = "")
    {
        Name = Path.GetFileNameWithoutExtension(fileName);

        var match = LocaleRegex().Match(fileName);
        if (string.IsNullOrEmpty(match.Value))
            return;

        Locale = match.Value;

        if (!string.IsNullOrEmpty(namespaceString))
            Namespace = namespaceString;
    }

    /// <summary>
    /// Parses the ResxDocument
    /// </summary>
    /// <param name="xmlStream"></param>
    /// <param name="isMainDocument"></param>
    public void Parse(Stream xmlStream, bool isMainDocument = false)
    {
        _isMainDocument = isMainDocument;
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
            Values.Add(name, value);
        }
    }

    /// <summary>
    /// Merges the <see cref="Values"/> container from a different <see cref="ResxDocument"/>
    /// </summary>
    /// <param name="otherDocument"></param>
    public void AddSubDocument(ResxDocument otherDocument)
    {
        // We cannot add into non-locale resx files.
        if (!_isMainDocument)
            return;

        // We cannot add files with a different start of the name
        if (!otherDocument.Name.StartsWith(Name))
            return;

        _subDocuments.Add(otherDocument.Locale, otherDocument);
    }

    /// <summary>
    /// Retrieve the values
    /// </summary>
    public SortedDictionary<string, Dictionary<string, string>> GetValues()
    {
        if (!_isMainDocument)
            return default;

        var values = new SortedDictionary<string, Dictionary<string, string>>();
        foreach (var (key, value) in Values)
        {
            values.Add(key, new());
            values[key].Add(Locale, value);
        }

        foreach (var (locale, otherDocument) in _subDocuments)
        {
            var missingValues = Values.Keys.Except(otherDocument.Values.Keys).ToList();
            foreach (var missingValue in missingValues.Where(missingValue => !otherDocument.Values.ContainsKey(missingValue)))
                otherDocument.Values.Add(missingValue, "");

            foreach (var (key, value) in otherDocument.Values)
            {
                if (!values.ContainsKey(key))
                {
                    values.Add(key, new());
                    values[key].Add(Locale, "");
                }
                else
                {
                    values[key].Add(locale, value);
                }
            }
        }

        return values;
    }

    /// <summary>
    /// Alter a value with a select key and locale
    /// </summary>
    /// <param name="locale"></param>
    /// <param name="key"></param>
    /// <param name="newValue"></param>
    public void EditValue(string locale, string key, string newValue)
    {
        if (!Values.ContainsKey(key))
            return;

        if (_subDocuments.TryGetValue(locale, out var document))
        {
            document.Values[key] = newValue;
        }
        else
        {
            Values[key] = newValue;
        }
    }

    /// <summary>
    /// Export all files and locales and let the user download these.
    /// </summary>
    public void Export()
    {
        if (!_isMainDocument)
            return;

        // Export .resx files first with template
        ResxWriter.Write(this);

        foreach (var (_, document) in _subDocuments)
            ResxWriter.Write(document);

        // Export .designer.cs file for main document
    }

    [GeneratedRegex("\\w{2}(?:-\\w{2})")]
    private static partial Regex LocaleRegex();
}
