// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AvalonDock.Layout;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.GameStudio.AssetsEditors;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Editor.Build;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.GameStudio.ViewModels;
using Stride.Rendering;

namespace Stride.GameStudio.AutoTesting;

/// <summary>
/// Loads the test DLL, polls for a settled WPF window, runs the chosen <see cref="IUITest"/>
/// fixture, and provides the <see cref="IUITestContext"/> impl (waits + WGC capture).
/// </summary>
internal sealed class UITestHost
{
    // Output dir suffix is the runtime monitor DPI percentage so per-DPI captures stay separate
    // (baselines are likewise stored under tests/editor/baselines/dpi<N>/). The runner detects
    // DPI at startup via GetDpiForMonitor(MDT_EFFECTIVE_DPI), which returns the user-set scale
    // factor regardless of process DPI-awareness.
    private const string OutDirNamePrefix = "ui-test-out-dpi";
    private const string ScreenshotsDir = "screenshots";
    private const string DoneFileName = "done.json";
    private const string LogFileName = "log.txt";

    // Window types that indicate the editor has finished startup; transients like
    // WorkProgressWindow are intentionally excluded.
    private static readonly HashSet<string> ReadyWindowTypeNames = new(StringComparer.Ordinal)
    {
        GameStudioWindowNames.GameStudio,
        GameStudioWindowNames.ProjectSelection,
    };

    private readonly Dispatcher dispatcher;
    private readonly string testDllPath;
    private readonly string? testClassName;
    private readonly string outputDir;
    private StreamWriter? logWriter;
    private readonly List<string> capturedNames = new();
    private string lastSeenWindowsSummary = "";

    public int ExitCode { get; private set; }

    public UITestHost(Dispatcher dispatcher, string testDllPath, string? testClassName)
    {
        this.dispatcher = dispatcher;
        this.testDllPath = testDllPath;
        this.testClassName = testClassName;
        outputDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(testDllPath))!, OutDirNamePrefix + DpiUtil.DetectDpiPercent());
        Directory.CreateDirectory(Path.Combine(outputDir, ScreenshotsDir));
        try { logWriter = new StreamWriter(new FileStream(Path.Combine(outputDir, LogFileName), FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true }; }
        catch (Exception ex) { Console.Error.WriteLine($"UITestHost: failed to open log: {ex.Message}"); }
    }

    public void Start()
    {
        Log($"Start: testDllPath={testDllPath} testClassName={testClassName ?? "(auto)"}");
        var test = LoadTest();
        Log($"Test loaded: {test.GetType().FullName}");
        var ctx = new Context(this);

        // Background polling loop that marshals each window check onto the dispatcher.
        var fired = false;
        Task.Run(async () =>
        {
            await Task.Delay(2000).ConfigureAwait(false);
            for (var i = 0; i < 1500; i++)
            {
                if (fired) return;
                bool ready;
                try { ready = await dispatcher.InvokeAsync(HasReadyWindow).Task.ConfigureAwait(false); }
                catch (Exception ex) { Log($"Poll: InvokeAsync failed: {ex.Message}"); return; }
                if (ready)
                {
                    fired = true;
                    await dispatcher.InvokeAsync(() => RunTest(test, ctx)).Task.ConfigureAwait(false);
                    return;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }
            Log("Poll: gave up after 1500 iterations (~5min).");
        });
    }

    private void Log(string message)
    {
        var line = $"{DateTime.UtcNow:HH:mm:ss.fff} {message}";
        Console.Error.WriteLine(line);
        try { logWriter?.WriteLine(line); }
        catch { /* best-effort */ }
    }

    private bool HasReadyWindow()
    {
        var app = Application.Current;
        if (app is null) return false;
        var summary = string.Join(", ", app.Windows.OfType<Window>().Select(w =>
            $"{w.GetType().Name}[Title='{w.Title}'](visible={w.IsVisible},loaded={w.IsLoaded},{w.ActualWidth}x{w.ActualHeight})"));
        if (summary != lastSeenWindowsSummary)
        {
            Log($"windows: {summary}");
            lastSeenWindowsSummary = summary;
        }
        foreach (var win in app.Windows.OfType<Window>())
        {
            if (!win.IsVisible || !win.IsLoaded) continue;
            if (win.ActualWidth < 100 || win.ActualHeight < 100) continue;
            if (!ReadyWindowTypeNames.Contains(win.GetType().Name)) continue;
            Log($"ready: '{win.GetType().Name}' Title='{win.Title}' Size={win.ActualWidth}x{win.ActualHeight}");
            return true;
        }
        return false;
    }

    private IUITest LoadTest()
    {
        var asm = Assembly.LoadFrom(testDllPath);
        var candidates = asm.GetTypes()
            .Where(t => t.GetCustomAttribute<UITestAttribute>() is not null)
            .ToList();
        if (candidates.Count == 0)
            throw new InvalidOperationException($"No [UITest] class found in '{testDllPath}'.");

        Type chosen;
        if (testClassName is not null)
        {
            chosen = candidates.FirstOrDefault(t => t.Name == testClassName || t.FullName == testClassName)
                ?? throw new InvalidOperationException($"No [UITest] class named '{testClassName}' in '{testDllPath}'. Available: {string.Join(", ", candidates.Select(t => t.FullName))}");
        }
        else if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else
        {
            throw new InvalidOperationException($"Multiple [UITest] classes in '{testDllPath}'; pass --test-name to select. Available: {string.Join(", ", candidates.Select(t => t.FullName))}");
        }

        if (!typeof(IUITest).IsAssignableFrom(chosen))
            throw new InvalidOperationException($"[UITest] class {chosen.FullName} must implement IUITest.");

        return (IUITest)Activator.CreateInstance(chosen)!;
    }

    private void RunTest(IUITest test, Context ctx)
    {
        Task.Run(async () =>
        {
            var status = "ok";
            object? exceptionInfo = null;
            try
            {
                await test.Run(ctx);
            }
            catch (Exception ex)
            {
                status = "error";
                exceptionInfo = SerializeException(ex);
                Console.Error.WriteLine(ex);
                ExitCode = 1;
            }
            finally
            {
                WriteDoneJson(status, exceptionInfo);
                ctx.ShutdownInternal();
            }
        });
    }

    private void WriteDoneJson(string status, object? exceptionInfo)
    {
        try
        {
            var donePath = Path.Combine(outputDir, DoneFileName);
            // Editor UI drifts nondeterministically; Claude fallback fires only when LPIPS exceeds
            // threshold so cost is bounded.
            var payload = new
            {
                status,
                screenshots = capturedNames.Select(n => new { name = n, claudeFallback = true }),
                exception = exceptionInfo,
            };
            File.WriteAllText(donePath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UITestHost: failed to write done.json: {ex}");
        }
    }

    private static object SerializeException(Exception ex) => new
    {
        type = ex.GetType().FullName,
        message = ex.Message,
        stack = ex.ToString(),
    };

    private sealed class Context : IUITestContext
    {
        private readonly UITestHost host;
        public Context(UITestHost host) { this.host = host; }

        public Task OpenProject() => Task.CompletedTask; // project is loaded by GameStudio's positional-arg path before the test runs

        public Task WaitForAssetBuild() => WaitForQueueDrained("asset-build", () =>
        {
            var session = TryGetSession();
            return session?.ServiceProvider.TryGet<AssetBuilderService>()?.QueuedBuildUnitCount ?? 0;
        });

        public Task WaitForShaders() => WaitForQueueDrained("shader-compile", () =>
        {
            var session = TryGetSession();
            return session?.ServiceProvider.TryGet<GameStudioBuilderService>()?.PendingShaderCompilationCount ?? 0;
        });

        public Task WaitDispatcherIdle()
        {
            var tcs = new TaskCompletionSource();
            host.dispatcher.BeginInvoke(() => tcs.SetResult(), DispatcherPriority.ApplicationIdle);
            return tcs.Task;
        }

        public async Task WaitFrames(int n = 1)
        {
            for (var i = 0; i < n; i++)
                await WaitDispatcherIdle();
        }

        public async Task WaitIdle()
        {
            await WaitForAssetBuild();
            await WaitForShaders();
            await WaitDispatcherIdle();
            await WaitFrames(1);
            await WaitForRendering();
        }

        public async Task WaitForRendering(int frames = 60, double timeoutSeconds = 30)
        {
            // Snapshot the (game, startFrameCount) pairs on the dispatcher; reading EditorServiceGame
            // state from the WPF UI thread is the safe path. PreviewGame lives on its own thread but
            // its DrawTime.FrameCount property is just an int read.
            var watched = await host.dispatcher.InvokeAsync(EnumerateActiveGames).Task.ConfigureAwait(false);
            if (watched.Count == 0)
            {
                host.Log("WaitForRendering: no active EditorServiceGame instances — skipping");
                return;
            }
            host.Log($"WaitForRendering: watching {watched.Count} game(s) for ≥{frames} frames each (timeout {timeoutSeconds}s)");

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(100).ConfigureAwait(false);
                var allReady = true;
                foreach (var w in watched)
                {
                    int current;
                    try { current = w.Game.DrawTime?.FrameCount ?? w.StartFrame; }
                    catch { continue; }  // game disposed mid-wait — treat as ready (drop from watch list semantically)
                    if (current - w.StartFrame < frames) { allReady = false; break; }
                }
                if (allReady)
                {
                    host.Log($"WaitForRendering: all watched games advanced ≥{frames} frames");
                    return;
                }
            }
            var snapshot = string.Join(", ", watched.Select(w =>
            {
                int? cur; try { cur = w.Game.DrawTime?.FrameCount; } catch { cur = null; }
                return $"{w.Game.GetType().Name}={(cur is null ? "?" : (cur - w.StartFrame).ToString())}";
            }));
            host.Log($"WaitForRendering: timed out after {timeoutSeconds}s — advances {snapshot}");
        }

        private readonly record struct WatchedGame(EditorServiceGame Game, int StartFrame);

        /// <summary>
        /// Walks the session's preview service + open asset-editor list and returns one entry per
        /// running <see cref="EditorServiceGame"/> with its current <see cref="GameTime.FrameCount"/>.
        /// Reflection-based: <c>AssetEditorsManager.assetEditors</c> is private, and
        /// <c>EditorGameController&lt;T&gt;.Game</c> is a protected field — neither is reachable from
        /// the AutoTesting assembly without [InternalsVisibleTo], which we don't need to add for
        /// this read-only diagnostic walk.
        /// </summary>
        private List<WatchedGame> EnumerateActiveGames()
        {
            var list = new List<WatchedGame>();
            var session = TryGetSession();
            if (session is null) return list;

            // 1) Shared asset-preview game (runs on its own STA thread, drives thumbnail rendering).
            var previewSvc = session.ServiceProvider.TryGet<GameStudioPreviewService>();
            if (previewSvc?.PreviewGame is { IsRunning: true } previewGame)
                list.Add(new WatchedGame(previewGame, previewGame.DrawTime?.FrameCount ?? 0));

            // 2) Each open asset editor's embedded game.
            if (session.ServiceProvider.TryGet<IAssetEditorsManager>() is not AssetEditorsManager aem) return list;
            foreach (var editorVm in aem.EditorViewModels)
            {
                if (editorVm is GameEditorViewModel { Controller: IEditorGameAccess access } && access.EditorGame is { IsRunning: true } game)
                    list.Add(new WatchedGame(game, game.DrawTime?.FrameCount ?? 0));
            }
            return list;
        }

        /// <summary>
        /// Returns when <paramref name="getCount"/> reads zero on two consecutive idle ticks.
        /// The two-tick rule absorbs the race where one drain seeds the queue from a follow-up.
        /// </summary>
        private async Task WaitForQueueDrained(string label, Func<int> getCount)
        {
            const int RequiredStableTicks = 2;
            var deadline = DateTime.UtcNow.AddSeconds(120);
            var stable = 0;
            var lastLogged = -1;
            var nextLogAt = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                await WaitDispatcherIdle();
                var count = await host.dispatcher.InvokeAsync(getCount, DispatcherPriority.ApplicationIdle);
                if (DateTime.UtcNow >= nextLogAt && count != lastLogged)
                {
                    host.Log($"WaitForQueueDrained('{label}') count={count}");
                    lastLogged = count;
                    nextLogAt = DateTime.UtcNow.AddSeconds(2);
                }
                if (count == 0)
                {
                    if (++stable >= RequiredStableTicks) return;
                }
                else
                {
                    stable = 0;
                }
            }
            host.Log($"WaitForQueueDrained('{label}') timed out after 120s.");
        }

        private static SessionViewModel? TryGetSession()
        {
            var app = Application.Current;
            if (app is null) return null;
            foreach (var w in app.Windows.OfType<Window>())
            {
                if (w.DataContext is GameStudioViewModel gs) return gs.Session;
            }
            return null;
        }

        public async Task Screenshot(string name)
        {
            var window = await host.dispatcher.InvokeAsync(ResolveCaptureWindow).Task.ConfigureAwait(false);
            if (window is null)
            {
                host.Log("Screenshot: no window to capture.");
                return;
            }
            var (winInfo, hwnd) = await host.dispatcher.InvokeAsync(() =>
                ($"'{window.GetType().Name}' Title='{window.Title}' Size={window.ActualWidth}x{window.ActualHeight}",
                 new WindowInteropHelper(window).Handle)).Task.ConfigureAwait(false);
            host.Log($"Screenshot: capturing {winInfo}");

            // Force a fresh WPF render so DWM has a frame for WGC to capture.
            await host.dispatcher.InvokeAsync(() =>
            {
                window.Activate();
                window.InvalidateVisual();
                window.UpdateLayout();
            }, DispatcherPriority.Normal).Task.ConfigureAwait(false);
            await host.dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render).Task.ConfigureAwait(false);
            await Task.Delay(150).ConfigureAwait(false);

            try
            {
                var path = Path.Combine(host.outputDir, ScreenshotsDir, name + ".png");
                if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Window has no HWND yet.");
                await GraphicsCaptureClient.CaptureToPngAsync(hwnd, path).ConfigureAwait(false);
                host.capturedNames.Add(name);
            }
            catch (Exception ex)
            {
                host.Log($"Screenshot('{name}') failed: {ex}");
            }
        }

        public async Task<bool> WaitForWindow(string windowTypeName, double timeoutSeconds = 120)
        {
            host.Log($"WaitForWindow: '{windowTypeName}' (timeout={timeoutSeconds}s)");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var found = await host.dispatcher.InvokeAsync(() =>
                {
                    var app = Application.Current;
                    return app?.Windows.OfType<Window>().Any(w =>
                        w.GetType().Name == windowTypeName && w.IsVisible && w.IsLoaded
                        && w.ActualWidth >= 100 && w.ActualHeight >= 100) ?? false;
                }).Task.ConfigureAwait(false);
                if (found)
                {
                    host.Log($"WaitForWindow: '{windowTypeName}' ready");
                    return true;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }
            host.Log($"WaitForWindow: '{windowTypeName}' timed out after {timeoutSeconds}s");
            return false;
        }

        public async Task<string?> WaitForAnyWindow(string[] windowTypeNames, double timeoutSeconds = 180)
        {
            host.Log($"WaitForAnyWindow: [{string.Join(",", windowTypeNames)}] (timeout={timeoutSeconds}s)");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var found = await host.dispatcher.InvokeAsync(() =>
                {
                    var app = Application.Current;
                    if (app is null) return (string?)null;
                    foreach (var name in windowTypeNames)
                    {
                        if (app.Windows.OfType<Window>().Any(w =>
                            w.GetType().Name == name && w.IsVisible && w.IsLoaded
                            && w.ActualWidth >= 100 && w.ActualHeight >= 100))
                            return name;
                    }
                    return null;
                }).Task.ConfigureAwait(false);
                if (found is not null)
                {
                    host.Log($"WaitForAnyWindow: '{found}' ready");
                    return found;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }
            host.Log($"WaitForAnyWindow: none of [{string.Join(",", windowTypeNames)}] within {timeoutSeconds}s");
            return null;
        }

        public Task<bool> SelectTemplate(Guid templateId) =>
            host.dispatcher.InvokeAsync(() =>
            {
                host.Log($"SelectTemplate: {templateId}");
                var app = Application.Current;
                if (app is null) return false;
                var win = app.Windows.OfType<ProjectSelectionWindow>().FirstOrDefault();
                if (win is null) { host.Log("SelectTemplate: ProjectSelectionWindow not found"); return false; }
                var collection = win.Templates;
                if (collection is null) { host.Log("SelectTemplate: ProjectSelectionWindow.Templates is null"); return false; }
                // Templates is the per-group filtered view; full set lives behind RootGroups.
                var candidates = collection.Templates
                    .Concat(collection.RootGroups.SelectMany(g => g.GetTemplatesRecursively()))
                    .ToList();
                var match = candidates.FirstOrDefault(t => t.Id == templateId);
                if (match is null)
                { host.Log($"SelectTemplate: no template with Id={templateId} in {candidates.Count} candidates"); return false; }
                collection.SelectedTemplate = match;
                host.Log($"SelectTemplate: selected '{match.GetType().Name}' (Id={match.Id})");
                return true;
            }).Task;

        public Task<bool> CloseModalWithOk(string windowTypeName) =>
            host.dispatcher.InvokeAsync(() =>
            {
                host.Log($"CloseModalWithOk: '{windowTypeName}'");
                var app = Application.Current;
                if (app is null) return false;
                var win = app.Windows.OfType<Window>().FirstOrDefault(w => w.GetType().Name == windowTypeName);
                if (win is null) { host.Log($"CloseModalWithOk: '{windowTypeName}' not found"); return false; }
                if (win is ModalWindow modal)
                {
                    modal.RequestClose(DialogResult.Ok);
                    host.Log($"CloseModalWithOk: RequestClose(Ok) on '{windowTypeName}'");
                    return true;
                }
                win.Close();
                host.Log($"CloseModalWithOk: Close() on '{windowTypeName}' (not a ModalWindow)");
                return true;
            }).Task;

        public async Task SetWindowSize(string windowTypeName, int width, int height) =>
            await host.dispatcher.InvokeAsync(() =>
            {
                var work = SystemParameters.WorkArea;
                // Clamp to work area so the window stays fully on-screen — partially off-screen
                // windows confuse DWM redirection and break WGC capture downstream.
                var w = Math.Min(width, (int)work.Width);
                var h = Math.Min(height, (int)work.Height);
                host.Log($"SetWindowSize: '{windowTypeName}' → req {width}x{height} clamped {w}x{h} (work={work.Width}x{work.Height})");
                var win = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w0 => w0.GetType().Name == windowTypeName);
                if (win is null) { host.Log($"SetWindowSize: '{windowTypeName}' not found"); return; }
                win.SetCurrentValue(Window.WindowStateProperty, WindowState.Normal);
                win.SetCurrentValue(Window.SizeToContentProperty, SizeToContent.Manual);
                win.SetCurrentValue(Window.WidthProperty, (double)w);
                win.SetCurrentValue(Window.HeightProperty, (double)h);
                win.SetCurrentValue(Window.LeftProperty, work.Left + Math.Max(0.0, (work.Width - w) / 2.0));
                win.SetCurrentValue(Window.TopProperty, work.Top + Math.Max(0.0, (work.Height - h) / 2.0));
                win.UpdateLayout();
            }, DispatcherPriority.Render).Task.ConfigureAwait(false);

        public async Task CapturePanel(string idOrTitle, string name, int width = 1200, int height = 900)
        {
            host.Log($"CapturePanel: id='{idOrTitle}' name='{name}' size={width}x{height}");
            var path = Path.Combine(host.outputDir, ScreenshotsDir, name + ".png");
            LayoutAnchorable? anchorable = null;
            AnchorableState? originalState = null;
            try
            {
                anchorable = await host.dispatcher.InvokeAsync(() => FindAnchorable(idOrTitle)).Task.ConfigureAwait(false);
                if (anchorable is null) { host.Log($"CapturePanel: '{idOrTitle}' not found."); return; }

                originalState = await host.dispatcher.InvokeAsync(() => FloatAnchorable(anchorable, width, height)).Task.ConfigureAwait(false);

                // Let the floating window realize and lay out.
                await WaitDispatcherIdle();
                await Task.Delay(250).ConfigureAwait(false);
                await WaitDispatcherIdle();

                await host.dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render).Task.ConfigureAwait(false);
                await Task.Delay(150).ConfigureAwait(false);

                var (winInfo, hwnd) = await host.dispatcher.InvokeAsync(() =>
                {
                    var floatWin = FindFloatingWindow(idOrTitle)
                        ?? throw new InvalidOperationException($"Floating window for '{idOrTitle}' not found after Float().");
                    floatWin.UpdateLayout();
                    return ($"'{floatWin.GetType().Name}' Size={floatWin.ActualWidth}x{floatWin.ActualHeight}",
                            new WindowInteropHelper(floatWin).Handle);
                }).Task.ConfigureAwait(false);
                host.Log($"CapturePanel: capturing {winInfo}");
                if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Floating window has no HWND yet.");

                // WGC captures DWM composition output, including D3DImage interop content like the
                // embedded scene preview's swap-chain.
                await GraphicsCaptureClient.CaptureToPngAsync(hwnd, path).ConfigureAwait(false);
                host.Log($"CapturePanel: wrote → {path}");
                host.capturedNames.Add(name);
            }
            catch (Exception ex)
            {
                host.Log($"CapturePanel('{idOrTitle}','{name}') failed: {ex}");
            }
            finally
            {
                if (anchorable is not null && originalState is not null)
                {
                    try
                    {
                        await host.dispatcher.InvokeAsync(() => RestoreAnchorable(anchorable, originalState.Value)).Task.ConfigureAwait(false);
                    }
                    catch (Exception ex) { host.Log($"CapturePanel: restore failed: {ex}"); }
                }
            }
        }

        /// <summary>
        /// Walks <see cref="Application.Windows"/> and returns the first top-level Window whose visual
        /// tree contains the LayoutAnchorable for <paramref name="contentId"/> — i.e. the floating
        /// window AvalonDock spawned by <c>Float()</c>. Skips the main GameStudioWindow.
        /// </summary>
        private static Window? FindFloatingWindow(string contentId)
        {
            var app = Application.Current;
            if (app is null) return null;
            foreach (var w in app.Windows.OfType<Window>())
            {
                if (w.GetType().Name == GameStudioWindowNames.GameStudio) continue;
                if (SearchTree(w, contentId, returnElement: false) is not null)
                    return w;
            }
            return null;
        }

        /// <summary>Finds the AvalonDock <see cref="LayoutAnchorable"/> with the matching <c>ContentId</c>.</summary>
        private static LayoutAnchorable? FindAnchorable(string contentId)
        {
            var app = Application.Current;
            if (app is null) return null;
            foreach (var w in app.Windows.OfType<Window>())
            {
                if (SearchTree(w, contentId, returnElement: false) is LayoutAnchorable hit) return hit;
            }
            return null;
        }

        private static object? SearchTree(DependencyObject node, string idOrTitle, bool returnElement)
        {
            // Anchorables (panels) match by ContentId; documents (asset editors) typically have
            // an empty ContentId and identify via Title (the asset URL). Asset URLs may carry a
            // /Namespace/ prefix, so a bare name also matches its last path segment.
            if (node is FrameworkElement fe && fe.DataContext is LayoutContent lc
                && (Matches(lc.ContentId) || Matches(lc.Title)))
                return returnElement ? fe : lc;

            bool Matches(string? candidate)
                => string.Equals(candidate, idOrTitle, StringComparison.Ordinal)
                   || (candidate is not null && candidate.EndsWith("/" + idOrTitle, StringComparison.Ordinal));
            var count = VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                var hit = SearchTree(child, idOrTitle, returnElement);
                if (hit is not null) return hit;
            }
            return null;
        }

        private readonly record struct AnchorableState(bool WasAutoHidden, bool WasFloating, double OldFloatingWidth, double OldFloatingHeight);

        private static AnchorableState FloatAnchorable(LayoutAnchorable anchorable, int width, int height)
        {
            var state = new AnchorableState(anchorable.IsAutoHidden, anchorable.IsFloating, anchorable.FloatingWidth, anchorable.FloatingHeight);
            // Auto-hidden panels must be expanded before Float() can move them; otherwise the
            // anchorable stays parented to the auto-hide pane and Float() no-ops.
            if (state.WasAutoHidden) anchorable.ToggleAutoHide();
            anchorable.FloatingWidth = width;
            anchorable.FloatingHeight = height;
            if (!state.WasFloating) anchorable.Float();
            return state;
        }

        private static void RestoreAnchorable(LayoutAnchorable anchorable, AnchorableState state)
        {
            if (!state.WasFloating) anchorable.Dock();
            anchorable.FloatingWidth = state.OldFloatingWidth;
            anchorable.FloatingHeight = state.OldFloatingHeight;
            if (state.WasAutoHidden && !anchorable.IsAutoHidden) anchorable.ToggleAutoHide();
        }

        public Task<int> RunProject() =>
            host.dispatcher.InvokeAsync(async () =>
            {
                var debugging = TryGetDebugging();
                if (debugging is null) { host.Log("RunProject: GameStudioViewModel not found"); return -1; }
                host.Log("RunProject: invoking RunProjectAsync");
                var (ok, proc) = await debugging.RunProjectAsync().ConfigureAwait(true);
                if (!ok || proc is null) { host.Log("RunProject: failed"); return -1; }
                host.Log($"RunProject: launched pid={proc.Id}");
                return proc.Id;
            }).Task.Unwrap();

        public async Task<IntPtr> WaitForGameWindow(int pid, double timeoutSeconds = 60)
        {
            host.Log($"WaitForGameWindow: pid={pid} timeout={timeoutSeconds}s");
            Process proc;
            try { proc = Process.GetProcessById(pid); }
            catch (ArgumentException) { host.Log($"WaitForGameWindow: pid {pid} not found"); return IntPtr.Zero; }

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                if (proc.HasExited) { host.Log($"WaitForGameWindow: pid {pid} exited"); return IntPtr.Zero; }
                proc.Refresh();
                var hwnd = proc.MainWindowHandle;
                if (hwnd != IntPtr.Zero)
                {
                    try { proc.WaitForInputIdle(2000); } catch { /* not a GUI app, or already idle */ }
                    host.Log($"WaitForGameWindow: hwnd=0x{hwnd.ToInt64():X}");
                    return hwnd;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }
            host.Log($"WaitForGameWindow: timed out after {timeoutSeconds}s");
            return IntPtr.Zero;
        }

        public async Task WaitForGameFrames(IntPtr hwnd, int minFrames = 100, double postFirstFrameDelaySeconds = 2.0, double timeoutSeconds = 90)
        {
            host.Log($"WaitForGameFrames: hwnd=0x{hwnd.ToInt64():X} minFrames={minFrames} postFirstFrame={postFirstFrameDelaySeconds}s timeout={timeoutSeconds}s");
            await GraphicsCaptureClient.WaitForFramesAsync(hwnd, minFrames, postFirstFrameDelaySeconds, timeoutSeconds).ConfigureAwait(false);
        }

        public async Task ScreenshotHwnd(IntPtr hwnd, string name)
        {
            if (hwnd == IntPtr.Zero) { host.Log($"ScreenshotHwnd('{name}'): hwnd is zero"); return; }
            var path = Path.Combine(host.outputDir, ScreenshotsDir, name + ".png");
            try
            {
                await GraphicsCaptureClient.CaptureToPngAsync(hwnd, path).ConfigureAwait(false);
                host.capturedNames.Add(name);
                host.Log($"ScreenshotHwnd: wrote → {path}");
            }
            catch (Exception ex) { host.Log($"ScreenshotHwnd('{name}') failed: {ex}"); }
        }

        public async Task CloseGameWindow(int pid, double timeoutSeconds = 30)
        {
            host.Log($"CloseGameWindow: pid={pid} timeout={timeoutSeconds}s");
            Process proc;
            try { proc = Process.GetProcessById(pid); }
            catch (ArgumentException) { host.Log($"CloseGameWindow: pid {pid} not found"); return; }
            if (proc.HasExited) { host.Log($"CloseGameWindow: pid {pid} already exited"); return; }
            proc.Refresh();
            var hwnd = proc.MainWindowHandle;
            if (hwnd != IntPtr.Zero) PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            else host.Log("CloseGameWindow: no MainWindowHandle, will rely on Kill");
            if (!await Task.Run(() => proc.WaitForExit((int)(timeoutSeconds * 1000))).ConfigureAwait(false))
            {
                host.Log("CloseGameWindow: WM_CLOSE timed out, killing");
                try { proc.Kill(entireProcessTree: true); } catch (Exception ex) { host.Log($"Kill failed: {ex.Message}"); }
            }
            // Process.ExitCode requires a handle the runtime retained from Start; pids opened via
            // GetProcessById don't qualify. Just log exit.
            host.Log($"CloseGameWindow: pid={pid} exited");
        }

        private const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private static DebuggingViewModel TryGetDebugging()
        {
            var app = Application.Current;
            if (app is null) return null;
            foreach (var w in app.Windows.OfType<Window>())
            {
                if (w.DataContext is GameStudioViewModel gs) return gs.Debugging;
            }
            return null;
        }

        public Task<Guid> AddAssetFromTemplate(Guid templateId, string templateName = null) =>
            host.dispatcher.InvokeAsync(async () =>
            {
                var session = TryGetSession();
                if (session is null) { host.Log("AddAssetFromTemplate: no session"); return Guid.Empty; }
                // Procedural-model variants all share the generator's TemplateId; Name disambiguates.
                var matches = session.FindTemplates(TemplateScope.Asset).Where(t => t.Id == templateId).ToList();
                var template = templateName is null
                    ? matches.FirstOrDefault()
                    : matches.FirstOrDefault(t => string.Equals(t.Name, templateName, StringComparison.Ordinal));
                if (template is null)
                {
                    host.Log($"AddAssetFromTemplate: template id={templateId} name='{templateName ?? "*"}' not found ({matches.Count} sharing Id: {string.Join(",", matches.Select(t => t.Name))})");
                    return Guid.Empty;
                }
                var assetView = session.ActiveAssetView;
                if (assetView is null) { host.Log("AddAssetFromTemplate: ActiveAssetView is null"); return Guid.Empty; }
                // RunAssetTemplate's target-folder lookup uses SelectedLocations and pops a modal
                // MessageBox if nothing is selected (or if the selected package isn't editable —
                // the case for per-platform exec projects, whose package surfaces as read-only).
                // Force-select the first editable package's asset root so creation proceeds
                // without UI prompts. Note SelectedLocations stores objects (UI binding), so
                // clearing + re-adding rather than swapping.
                var editablePackage = session.LocalPackages.FirstOrDefault(p => p.IsEditable);
                if (editablePackage is null) { host.Log("AddAssetFromTemplate: no editable LocalPackages to default-select"); return Guid.Empty; }
                var rootDir = editablePackage.AssetMountPoint;
                assetView.SelectedLocations.Clear();
                assetView.SelectedLocations.Add(rootDir);
                host.Log($"AddAssetFromTemplate: default-selected '{rootDir.Path}' in package '{editablePackage.Name}' as creation target");
                var templateVm = new TemplateDescriptionViewModel(session.ServiceProvider, template);
                var created = await assetView.RunAssetTemplate(templateVm, null).ConfigureAwait(true);
                if (created is null || created.Count == 0) { host.Log("AddAssetFromTemplate: RunAssetTemplate returned no asset"); return Guid.Empty; }
                host.Log($"AddAssetFromTemplate: created '{created[0].Url}' (id={created[0].Id})");
                return (Guid)created[0].Id;
            }).Task.Unwrap();

        public Task<string?> OpenAssetEditor(string assetUrl) =>
            host.dispatcher.InvokeAsync(async () =>
            {
                var session = TryGetSession();
                if (session is null) { host.Log("OpenAssetEditor: no session"); return (string?)null; }
                // Exact Url first, then a trailing-segment match so callers can pass just the script name.
                var asset = session.AllAssets.FirstOrDefault(a => string.Equals(a.Url, assetUrl, StringComparison.OrdinalIgnoreCase))
                         ?? session.AllAssets.FirstOrDefault(a => a.Url.EndsWith(assetUrl, StringComparison.OrdinalIgnoreCase));
                if (asset is null)
                {
                    host.Log($"OpenAssetEditor: '{assetUrl}' not found among {session.AllAssets.Count()} assets");
                    return (string?)null;
                }
                if (session.ServiceProvider.TryGet<IAssetEditorsManager>() is not { } aem) { host.Log("OpenAssetEditor: no IAssetEditorsManager"); return (string?)null; }
                await aem.OpenAssetEditorWindow(asset).ConfigureAwait(true);
                host.Log($"OpenAssetEditor: opened '{asset.Url}' (id={asset.Id})");
                return asset.Url;
            }).Task.Unwrap();

        public async Task<bool> WaitForSyntaxHighlighting(string title, double timeoutSeconds = 60)
        {
            host.Log($"WaitForSyntaxHighlighting: '{title}' (timeout={timeoutSeconds}s)");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            var colors = 0;
            while (DateTime.UtcNow < deadline)
            {
                colors = await host.dispatcher.InvokeAsync(() => CountEditorTextColors(title)).Task.ConfigureAwait(false);
                if (colors >= 3)
                {
                    host.Log($"WaitForSyntaxHighlighting: '{title}' shows {colors} text colors");
                    return true;
                }
                await Task.Delay(250).ConfigureAwait(false);
            }
            host.Log($"WaitForSyntaxHighlighting: '{title}' timed out with {colors} text color(s)");
            return false;
        }

        /// <summary>
        /// Counts distinct foreground colors across the visible lines of the AvalonEdit text view
        /// hosted by the document pane titled <paramref name="title"/>. One color = plain
        /// (unclassified) text; several = Roslyn classification applied. 0 = pane/view not found
        /// or not rendered yet.
        /// </summary>
        private static int CountEditorTextColors(string title)
        {
            if (FindAnchorable(title)?.Content is not DependencyObject content) return 0;
            var textView = FindVisualDescendant<ICSharpCode.AvalonEdit.Rendering.TextView>(content);
            if (textView is null || !textView.VisualLinesValid) return 0;
            var colors = new HashSet<System.Windows.Media.Color>();
            foreach (var line in textView.VisualLines)
            {
                foreach (var element in line.Elements)
                {
                    if (element.TextRunProperties?.ForegroundBrush is SolidColorBrush brush)
                        colors.Add(brush.Color);
                }
            }
            return colors.Count;
        }

        private static T? FindVisualDescendant<T>(DependencyObject node) where T : DependencyObject
        {
            if (node is T hit) return hit;
            var count = VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                if (FindVisualDescendant<T>(VisualTreeHelper.GetChild(node, i)) is { } childHit)
                    return childHit;
            }
            return null;
        }

        public async Task QueueAssetPickerResponse(string assetName, double timeoutSeconds = 30)
        {
            host.Log($"QueueAssetPickerResponse: assetName='{assetName ?? "<cancel>"}' timeout={timeoutSeconds}s");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            // Poll for the AssetPickerWindow to appear, then resolve it on the dispatcher.
            while (DateTime.UtcNow < deadline)
            {
                var resolved = await host.dispatcher.InvokeAsync(() => TryResolveAssetPicker(assetName)).Task.ConfigureAwait(false);
                if (resolved) return;
                await Task.Delay(150).ConfigureAwait(false);
            }
            host.Log($"QueueAssetPickerResponse: timed out — no AssetPickerWindow appeared within {timeoutSeconds}s");
        }

        private bool TryResolveAssetPicker(string assetName)
        {
            var picker = Application.Current?.Windows.OfType<AssetPickerWindow>().FirstOrDefault(w => w.IsLoaded && w.IsVisible);
            if (picker is null) return false;
            if (assetName is null)
            {
                picker.RequestClose(DialogResult.Cancel);
                host.Log("QueueAssetPickerResponse: cancelled picker");
                return true;
            }
            var asset = picker.Session.AllAssets.FirstOrDefault(a => string.Equals(a.Name, assetName, StringComparison.Ordinal));
            if (asset is null)
            {
                host.Log($"QueueAssetPickerResponse: asset '{assetName}' not found; cancelling");
                picker.RequestClose(DialogResult.Cancel);
                return true;
            }
            picker.AssetView.SelectAssets(new[] { asset });
            picker.RequestClose(DialogResult.Ok);
            host.Log($"QueueAssetPickerResponse: selected '{asset.Url}' (id={asset.Id}) and confirmed");
            return true;
        }

        public Task<bool> AddEntityToScene(string entityName, Guid modelAssetId, Vector3 position) =>
            host.dispatcher.InvokeAsync(() =>
            {
                var session = TryGetSession();
                if (session is null) { host.Log("AddEntityToScene: no session"); return false; }
                var modelAsset = session.AllAssets.FirstOrDefault(a => (Guid)a.Id == modelAssetId);
                if (modelAsset is null) { host.Log($"AddEntityToScene: model asset id={modelAssetId} not found"); return false; }
                var sceneEditor = TryGetOpenSceneEditor();
                if (sceneEditor is null) { host.Log("AddEntityToScene: no SceneEditorViewModel open"); return false; }
                var factory = new ModelEntityFactory(entityName, modelAsset.Id, modelAsset.Url, position);
                sceneEditor.CreateEntityInRootCommand.Execute(factory);
                host.Log($"AddEntityToScene: '{entityName}' factory dispatched (modelAsset='{modelAsset.Url}', position={position})");
                return true;
            }).Task;

        private static SceneEditorViewModel TryGetOpenSceneEditor()
        {
            var session = TryGetSession();
            if (session?.ServiceProvider.TryGet<IAssetEditorsManager>() is not AssetEditorsManager aem) return null;
            return aem.EditorViewModels.OfType<SceneEditorViewModel>().FirstOrDefault();
        }

        /// <summary>
        /// Entity with <c>ModelComponent</c> + transform pre-set. <c>CreateEntityInRootCommand</c>
        /// preserves the factory-set position (its mouse-position branch is skipped).
        /// </summary>
        private sealed class ModelEntityFactory : IEntityFactory
        {
            private readonly string entityName;
            private readonly Stride.Core.Assets.AssetId modelId;
            private readonly string modelUrl;
            private readonly Vector3 position;

            public ModelEntityFactory(string entityName, Stride.Core.Assets.AssetId modelId, string modelUrl, Vector3 position)
            {
                this.entityName = entityName;
                this.modelId = modelId;
                this.modelUrl = modelUrl;
                this.position = position;
            }

            public Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
            {
                var entity = new Entity(entityName);
                entity.Transform.Position = position;
                var modelRef = AttachedReferenceManager.CreateProxyObject<Model>(modelId, modelUrl);
                entity.Add(new ModelComponent { Model = modelRef });
                return Task.FromResult(entity);
            }
        }

        public Task<int> CountUnloadable() =>
            host.dispatcher.InvokeAsync(() =>
            {
                var session = TryGetSession();
                if (session is null) { host.Log("CountUnloadable: no session"); return 0; }
                var found = new List<string>();
                foreach (var asset in session.AllAssets)
                    foreach (var u in UnloadableObjectRemover.Discover(asset.Asset))
                        found.Add($"  {asset.Url}: {u.MemberPath}");
                if (found.Count > 0)
                    host.Log($"CountUnloadable: {found.Count} unloadable object(s):\n{string.Join("\n", found)}");
                else
                    host.Log("CountUnloadable: none");
                return found.Count;
            }).Task;

        public void Exit(int newExitCode = 0)
        {
            host.ExitCode = newExitCode;
            ShutdownInternal();
        }

        public void ShutdownInternal()
        {
            host.dispatcher.BeginInvoke(() =>
            {
                Environment.ExitCode = host.ExitCode;
                var app = Application.Current;
                if (app is null) return;
                foreach (var win in app.Windows.Cast<Window>().ToList())
                {
                    try { win.Close(); } catch { /* best-effort */ }
                }
                app.Shutdown(host.ExitCode);
            });
        }

        private static Window? ResolveCaptureWindow()
        {
            var app = Application.Current;
            if (app is null) return null;
            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? app.Windows.OfType<Window>().LastOrDefault(w => w.IsLoaded)
                ?? app.MainWindow;
        }
    }
}
