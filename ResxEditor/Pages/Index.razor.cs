using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ResxEditor.Resx;

namespace ResxEditor.Pages;

public partial class Index : ComponentBase
{
    private readonly List<string> _locales = new();
    private readonly List<IBrowserFile> _loadedFiles = new();

    private bool _isLoading;
    private (string, string) _selectedItem;

    private ResxDocument _mainDocument;

    private async void OnLoadFiles(InputFileChangeEventArgs e)
    {
        _isLoading = true;
        StateHasChanged();

        _loadedFiles.Clear();
        _locales.Clear();

        var files = e.GetMultipleFiles();
        files = files.OrderByDescending(x => x.Name, StringComparer.Ordinal)
            .ToList();

        // Retrieve the locales and load all the documents
        foreach (var file in files)
        {
            try
            {
                _loadedFiles.Add(file);

                using var stream = new MemoryStream();
                await file.OpenReadStream().CopyToAsync(stream);

                var match = LocaleRegex().Match(file.Name);
                if (match.Length == 0 || match.Name == "en-US")
                {
                    // No locale found in name, so this is the main document
                    _mainDocument = new ResxDocument(file.Name);
                    _mainDocument.Parse(stream);
                }
                else
                {
                    // Locale is already loaded
                    if (_locales.Contains(match.Value))
                        continue;

                    _locales.Add(match.Value);

                    var document = new ResxDocument(file.Name);
                    document.Parse(stream);

                    _mainDocument?.Merge(document);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        _isLoading = false;
        StateHasChanged();
    }

    private void OnCellDoubleClick(MouseEventArgs e, KeyValuePair<string, string> value)
    {
        _selectedItem = (value.Key, value.Value);

        StateHasChanged();
    }

    [GeneratedRegex("\\w{2}(?:-\\w{2})")]
    private static partial Regex LocaleRegex();
}
