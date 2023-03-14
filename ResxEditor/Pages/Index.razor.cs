using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using ResxEditor.Resx;

namespace ResxEditor.Pages;

public partial class Index : ComponentBase
{
    private readonly List<string> _locales = new();
    private readonly List<ResxDocument> _loadedResxFiles = new();

    private List<IBrowserFile> _loadedFiles = new();
    private (string Locale, string Value) _selectedItem;
    private ResxDocument _mainDocument;

    private string _namespace = "";
    private string _className = "";

    #region Events

    private void OnLoadFiles(InputFileChangeEventArgs e)
    {
        _loadedFiles = e.GetMultipleFiles()
            .OrderByDescending(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var firstFile = _loadedFiles.FirstOrDefault();
        if (firstFile == null)
            return;

        _className = Path.GetFileNameWithoutExtension(firstFile.Name);
    }

    private async void OnOpenClick()
    {
        _locales.Clear();
        _loadedResxFiles.Clear();

        // Retrieve the locales and load all the documents
        foreach (var file in _loadedFiles)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(stream);

                var match = LocaleRegex().Match(file.Name);
                if (match.Length == 0 || match.Name == "en-US")
                {
                    // No locale found in name, so this is the main document
                    _mainDocument = new(file.Name);
                    _mainDocument.Parse(stream, true);

                    _loadedResxFiles.Add(_mainDocument);
                }
                else
                {
                    // Locale is already loaded
                    if (_locales.Contains(match.Value))
                        continue;

                    _locales.Add(match.Value);

                    var document = new ResxDocument(file.Name);
                    document.Parse(stream);

                    _mainDocument?.AddSubDocument(document);
                    _loadedResxFiles.Add(document);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        StateHasChanged();
    }

    private void OnCellDoubleClick(KeyValuePair<string, string> value)
    {
        _selectedItem = (value.Key, value.Value);
        StateHasChanged();
    }

    private void OnDeselectClick()
    {
        _selectedItem = (null, null);
        StateHasChanged();
    }

    private void OnInputTextChanged(string key, string locale, string newValue)
    {
        _mainDocument?.EditValue(locale, key, newValue);
        StateHasChanged();
    }

    private void OnExportClick()
    {
        _mainDocument.Export();
    }

    #endregion

    [GeneratedRegex("\\w{2}(?:-\\w{2})")]
    private static partial Regex LocaleRegex();
}
