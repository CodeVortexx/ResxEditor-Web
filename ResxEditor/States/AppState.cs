using Microsoft.JSInterop;

namespace ResxEditor.States;

public class AppState
{
    // @TODO: Add authentication shit here (ApiClient etc etc.)
    public string Namespace { get; set; }
    public string FileName { get; set; }
    public bool IsNewDocument { get; set; }

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
    }

    public async Task DisplayModal(bool show)
    {
        await _jsRuntime.InvokeVoidAsync("toggleModal", "#inputModal", show ? "show" : "hide");
    }

    public async Task OpenFilePicker()
    {
        await _jsRuntime.InvokeVoidAsync("openFilePicker");
    }

    #endregion
}
