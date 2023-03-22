using Microsoft.JSInterop;

namespace ResxEditor.States;

public class AppState
{
    // @TODO: Add authentication shit here (ApiClient etc etc.)
    public string Namespace { get; set; }
    public string FileName { get; set; }
    public bool IsNewDocument { get; set; }

    public event Action StateHasChanged;

    private readonly IJSRuntime _jsRuntime;

    public AppState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    #region Global Events

    public async Task OnFileMenuItemClick(bool isNew)
    {
        IsNewDocument = isNew;
        await DisplayModal(true);

        StateHasChanged?.Invoke();
    }

    public async Task DisplayModal(bool show)
    {
        await _jsRuntime.InvokeVoidAsync("toggleModal", "#inputModal", show ? "show" : "hide");

        StateHasChanged?.Invoke();
    }

    public async Task OpenFilePicker()
    {
        await _jsRuntime.InvokeVoidAsync("openFilePicker");

        StateHasChanged?.Invoke();
    }

    #endregion
}
