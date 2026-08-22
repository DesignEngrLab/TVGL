using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
namespace WebGPUPresenter;

/// <summary>Owns the loopback web server and synchronizes TVGL's blocking debugger calls with its one browser UI.</summary>
public sealed class LocalPresenterHost
{
    private readonly object gate = new();
    private TaskCompletionSource<bool> ready = NewTcs();
    private TaskCompletionSource<bool>? pending;
    private SceneRequest? active;
    public string Url { get; }
    internal event Func<SceneRequest, Task>? SceneRequested;
    public LocalPresenterHost()
    {
        var port = FindPort(); Url = $"http://127.0.0.1:{port}";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(Url);
        // Testing.exe is the entry assembly, but WebGPUPresenter owns the browser assets. Load its
        // runtime manifest so UseStaticFiles receives the browser project's composite file provider.
        builder.WebHost.UseSetting(WebHostDefaults.StaticWebAssetsKey,
            Path.Combine(AppContext.BaseDirectory, "WebGPUPresenter.staticwebassets.runtime.json"));
        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddFluentUIComponents();
        builder.Services.AddSingleton(this);
        var app = builder.Build();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapStaticAssets(Path.Combine(AppContext.BaseDirectory, "WebGPUPresenter.staticwebassets.endpoints.json"));
        app.MapGet("/_framework/blazor.web.js", () =>
        {
            var stream = typeof(LocalPresenterHost).Assembly.GetManifestResourceStream("WebGPUPresenter.Framework.blazor.web.js")
                ?? throw new InvalidOperationException("The embedded Blazor framework script was not found.");
            return Results.Stream(stream, "text/javascript");
        });
        app.MapRazorComponents<Components.App>().AddInteractiveServerRenderMode();
        app.Start(); LaunchBrowser();
    }
    public void WaitReady() => ready.Task.GetAwaiter().GetResult();
    public void Show(SceneRequest scene)
    {
        WaitReady();
        TaskCompletionSource<bool> completion;
        Func<SceneRequest, Task> deliver;
        lock (gate)
        {
            active = scene;
            completion = pending = NewTcs();
            deliver = SceneRequested ?? throw new InvalidOperationException("The browser presenter is not connected.");
        }
        deliver(scene).GetAwaiter().GetResult();
        completion.Task.GetAwaiter().GetResult();
    }
    internal Task Ready()
    {
        SceneRequest? replay;
        lock (gate) { ready.TrySetResult(true); replay = active; }
        return replay is null || SceneRequested is null ? Task.CompletedTask : SceneRequested(replay);
    }
    internal Task Release() { lock (gate) { active = null; pending?.TrySetResult(true); pending = null; } return Task.CompletedTask; }
    internal void BrowserDisconnected() { lock (gate) ready = NewTcs(); LaunchBrowser(); }
    private void LaunchBrowser() { Console.WriteLine($"TVGL browser presenter: {Url}"); try { Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true }); } catch { Console.WriteLine($"Open {Url} in a browser."); } }
    private static int FindPort() { if (int.TryParse(Environment.GetEnvironmentVariable("TVGL_PRESENTER_PORT"), out var p) && p > 0 && Free(p)) return p; using var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); return ((IPEndPoint)l.LocalEndpoint).Port; }
    private static bool Free(int p) { try { using var l = new TcpListener(IPAddress.Loopback, p); l.Start(); return true; } catch (SocketException) { return false; } }
    private static TaskCompletionSource<bool> NewTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
