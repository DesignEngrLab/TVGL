using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace WebGPUPresenter.Components;

public sealed class PlotlyInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./Components/PlotViewer.razor.js";
    private IJSObjectReference? module;
    private async ValueTask<IJSObjectReference> ModuleAsync() => module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
    public async ValueTask RenderAsync(ElementReference element, PlotRequest plot, string title)
        => await (await ModuleAsync()).InvokeVoidAsync("render", element, plot, title);
    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try { await module.InvokeVoidAsync("dispose"); await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }
}
