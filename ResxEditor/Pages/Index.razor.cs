using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

using ResxEditor.Resx;
using ResxEditor.Service;

namespace ResxEditor.Pages;

public partial class Index : ComponentBase
{
    [Inject] public CultureInfoService CultureInfoService { get; init; }
    [Inject] public IJSRuntime JsRuntime { get; init; }

    private readonly List<string> _locales = new();
    private readonly List<ResxDocument> _loadedResxFiles = new();

    private List<IBrowserFile> _loadedFiles = new();
    private (string Key, string Locale, string Value) _selectedItem;
    private string _oldKeyValue;
    private string _selectedKey;
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
                var nameSplit = Path.GetFileNameWithoutExtension(file.Name).Split('.');
                var locale = nameSplit.LastOrDefault() ?? string.Empty;

                var cultureInfo = CultureInfoService.GetCultureInfo(locale);
                var isMainDocument = cultureInfo == null;

                using var stream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(stream);

                if (isMainDocument)
                {
                    // No locale found in name, so this is the main document
                    _mainDocument = new(JsRuntime, file.Name, namespaceString: _namespace);
                    _mainDocument.Parse(stream, true);

                    _loadedResxFiles.Add(_mainDocument);
                }
                else
                {
                    // Locale is already loaded
                    if (_locales.Contains(cultureInfo.Name))
                        continue;

                    _locales.Add(cultureInfo.Name);

                    var document = new ResxDocument(JsRuntime, file.Name, cultureInfo.Name, _namespace);
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

    private void OnValueCellDoubleClick(string key, KeyValuePair<string, string> value)
    {
        _selectedItem = (key, value.Key, value.Value);
        _selectedKey = null;

        StateHasChanged();
    }

    private void OnKeyCellDoubleClick(string value)
    {
        _oldKeyValue = value;
        _selectedKey = value;
        _selectedItem = (null, null, null);

        StateHasChanged();
    }

    private void OnDeselectClick()
    {
        _selectedKey = null;
        _selectedItem = (null, null, null);
        StateHasChanged();
    }

    private void OnValueInputTextChanged(string key, string locale, string newValue)
    {
        _mainDocument?.EditValue(locale, key, newValue);
        StateHasChanged();
    }

    private void OnKeyInputTextChanged(string key)
    {
        _mainDocument?.EditKey(_oldKeyValue, key);
        StateHasChanged();
    }

    private void OnExportClick()
    {
        _mainDocument.Export();
        StateHasChanged();
    }

    private void OnAddKeyClick()
    {
        _mainDocument.AddKey();
        StateHasChanged();
    }

    private void OnRemoveKeyClick()
    {

    }

    #endregion
}
