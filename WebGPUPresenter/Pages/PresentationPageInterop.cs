using Microsoft.JSInterop;

namespace WebGPUPresenter.Pages;

internal sealed class PresentationPageInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Pages/Home.razor.js";
    private readonly Lazy<Task<IJSObjectReference>> _module = new(() => js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());

    public async ValueTask ScrollToNewestViewerAsync()
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("scrollToNewestViewer");
    }

    public async ValueTask RegisterKeyboardShortcutsAsync(object dotNetReference)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("registerKeyboardShortcuts", dotNetReference);
    }

    public async ValueTask UnregisterKeyboardShortcutsAsync()
    {
        if (!_module.IsValueCreated)
            return;

        var module = await _module.Value;
        await module.InvokeVoidAsync("unregisterKeyboardShortcuts");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_module.IsValueCreated)
            return;

        try
        {
            var module = await _module.Value;
            await module.InvokeVoidAsync("unregisterKeyboardShortcuts");
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The local debugging page may be closed while a TVGL call is blocked.
        }
    }
}
