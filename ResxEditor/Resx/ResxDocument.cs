using System.Text;
using System.Xml;
using Microsoft.JSInterop;
using ResxEditor.Writers;

namespace ResxEditor.Resx;

public class ResxDocument
{
    public SortedDictionary<string, string> Values { get; } = new();

    public readonly string Name;
    public readonly string Namespace;
    public readonly string Locale;

    private readonly IJSRuntime _jsRuntime;

    private readonly Dictionary<string, ResxDocument> _subDocuments = new();
    private bool _isMainDocument;

    public ResxDocument(IJSRuntime jsRuntime, string fileName, string locale = "en-US", string namespaceString = "")
    {
        _jsRuntime = jsRuntime;

        Name = Path.GetFileNameWithoutExtension(fileName);
        Locale = locale;

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
    /// Add a new key to <see cref="ResxDocument"/> instance and sub documents.
    /// </summary>
    public void AddKey()
    {
        Values.Add("temp_key", string.Empty);

        foreach (var (_, document) in _subDocuments)
            document.AddKey();
    }

    /// <summary>
    /// Alter a key in a <see cref="ResxDocument"/> instance and sub documents.
    /// </summary>
    /// <param name="oldKey"></param>
    /// <param name="newKey"></param>
    public void EditKey(string oldKey, string newKey)
    {
        var value = Values[oldKey];
        Values.Remove(oldKey);
        Values.Add(newKey, value);

        foreach (var (_, document) in _subDocuments)
            document.EditKey(oldKey, newKey);
    }

    /// <summary>
    /// Deletes a key in a <see cref="ResxDocument"/> instance and sub documents.
    /// </summary>
    /// <param name="key"></param>
    public void DeleteKey(string key)
    {
        if (!Values.ContainsKey(key))
            return;

        Values.Remove(key);

        foreach (var (_, document) in _subDocuments)
            document.DeleteKey(key);
    }

    /// <summary>
    /// Export all files and locales and let the user download these.
    /// </summary>
    public async void Export()
    {
        if (!_isMainDocument)
            return;

        // Export .resx files first with template
        var documentString = ResxWriter.Write(this);
        await _jsRuntime.InvokeVoidAsync("downloadFileFromArray", $"{Name}.resx", Encoding.UTF8.GetBytes(documentString));

        foreach (var (_, document) in _subDocuments)
        {
            var subDocumentString = ResxWriter.Write(document);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromArray", $"{document.Name}.resx", Encoding.UTF8.GetBytes(subDocumentString));
        }

        // Export .designer.cs file for main document
        var designString = DesignerWriter.Write(this);
        await _jsRuntime.InvokeVoidAsync("downloadFileFromArray", $"{Name}.Designer.cs", Encoding.UTF8.GetBytes(designString));
    }
}
