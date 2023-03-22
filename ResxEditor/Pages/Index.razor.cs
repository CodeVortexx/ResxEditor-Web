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
    private string _selectedMainKey;
    private ResxDocument _mainDocument;

    protected override void OnInitialized()
    {
        AppState.StateHasChanged += () => InvokeAsync(StateHasChanged);

        base.OnInitialized();
    }

    #region Events

    private void OnLoadFiles(InputFileChangeEventArgs e)
    {
        _loadedFiles = e.GetMultipleFiles()
            .OrderByDescending(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var firstFile = _loadedFiles.FirstOrDefault();
        if (firstFile == null)
            return;

        AppState.FileName = Path.GetFileNameWithoutExtension(firstFile.Name);
        StateHasChanged();
    }

    private async Task OnOpenClick()
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
                    _mainDocument = new(JsRuntime, file.Name, namespaceString: AppState.Namespace);
                    _mainDocument.Parse(stream, true);

                    _loadedResxFiles.Add(_mainDocument);
                }
                else
                {
                    // Locale is already loaded
                    if (_locales.Contains(cultureInfo.Name))
                        continue;

                    _locales.Add(cultureInfo.Name);

                    var document = new ResxDocument(JsRuntime, file.Name, cultureInfo.Name, AppState.Namespace);
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

        await AppState.DisplayModal(false);

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

    private void OnCellClick(string selectedKey = null)
    {
        _selectedKey = null;
        _selectedItem = (null, null, null);

        if (!string.IsNullOrEmpty(selectedKey))
            _selectedMainKey = selectedKey;

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
        _mainDocument.DeleteKey(_selectedMainKey);
    }

    #endregion
}
