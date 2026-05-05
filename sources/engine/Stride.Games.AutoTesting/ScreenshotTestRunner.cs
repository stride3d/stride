// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Stride.Games.AutoTesting;

/// <summary>
/// Drives a <see cref="IScreenshotTest"/> attached to the entry assembly. Hooks
/// <see cref="Game.GameStarted"/>, wires simulated input, schedules the script as a Stride
/// micro-thread, captures back-buffer PNGs, and writes a <c>done.json</c> completion record
/// before exiting the game.
/// </summary>
public static class AutoTestingBootstrap
{
    /// <summary>
    /// No-op invoked from a consumer's [ModuleInitializer] to force-load this assembly so the
    /// <see cref="ScreenshotTestRunner"/>'s own [ModuleInitializer] runs. The .NET runtime
    /// defers loading until a method from the assembly is actually invoked, so referencing a
    /// type with typeof() isn't enough on its own.
    /// </summary>
    public static void EnsureLoaded() { }
}

internal sealed class ScreenshotTestRunner
{
    private const string OutputDirName = "screenshot-test";
    private const string ScreenshotsSubDir = "screenshots";
    private const string DoneFileName = "done.json";
    private const string ErrorLogName = "error.log";

    private readonly Game game;
    private readonly IScreenshotTest test;
    private readonly string outputDir;
    private readonly string screenshotsDir;
    private readonly List<CapturedScreenshot> captured = [];
    private readonly ConcurrentQueue<(string Name, float Threshold, object? ClaudeFallback, TaskCompletionSource Tcs)> pendingScreenshots = new();
    private InputSourceSimulated simulatedInput = null!;
    private KeyboardSimulated keyboard = null!;
    private MouseSimulated mouse = null!;
    private bool exitRequested;
    private int exitCode;

    [ModuleInitializer]
    internal static void RegisterAutoTestHook()
    {
        // Default to software rendering for deterministic captures; STRIDE_TESTS_GPU=1 opts back into the GPU.
        if (Environment.GetEnvironmentVariable("STRIDE_TESTS_GPU") != "1")
            Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");
        Game.GameStarted += OnGameStarted;
    }


    private static void OnGameStarted(object? sender, EventArgs e)
    {
        if (sender is not Game game)
            return;

        // Skip back-buffer clamp so portrait samples don't get cropped on smaller host desktops.
        // OnGameStarted fires before graphicsDeviceManager.CreateDevice, so the flag is in effect
        // by the time the swap chain is created.
        ((GraphicsDeviceManager)game.GraphicsDeviceManager).SkipBackBufferClampToWindow = true;

        // [ScreenshotTest] typically lives in the .Game assembly (library), not the .Windows entry
        // assembly (exe). Scan every currently-loaded assembly that references Stride.Games.AutoTesting.
        var harness = typeof(ScreenshotTestAttribute).Assembly.GetName().Name;
        var testTypes = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic)
                continue;
            // Cheap filter: assembly must reference our harness (or be it).
            if (asm.GetName().Name != harness && !asm.GetReferencedAssemblies().Any(r => r.Name == harness))
                continue;
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.GetCustomAttribute<ScreenshotTestAttribute>() is not null)
                        testTypes.Add(t);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var t in ex.Types.OfType<Type>())
                {
                    if (t.GetCustomAttribute<ScreenshotTestAttribute>() is not null)
                        testTypes.Add(t);
                }
            }
        }

        if (testTypes.Count == 0)
            return;

        if (testTypes.Count > 1)
            throw new InvalidOperationException(
                $"Stride.Games.AutoTesting: found {testTypes.Count} [ScreenshotTest] classes; exactly one is allowed. " +
                $"Found: {string.Join(", ", testTypes.Select(t => t.FullName))}");

        var testType = testTypes[0];
        if (!typeof(IScreenshotTest).IsAssignableFrom(testType))
            throw new InvalidOperationException($"[ScreenshotTest] class {testType.FullName} must implement {nameof(IScreenshotTest)}.");

        var test = (IScreenshotTest)Activator.CreateInstance(testType)!;
        var runner = new ScreenshotTestRunner(game, test);
        runner.Start();
    }

    private ScreenshotTestRunner(Game game, IScreenshotTest test)
    {
        this.game = game;
        this.test = test;

        var exeDir = AppContext.BaseDirectory;
        outputDir = Path.Combine(exeDir, OutputDirName);
        screenshotsDir = Path.Combine(outputDir, ScreenshotsSubDir);
    }

    private void Start()
    {
        try
        {
            Directory.CreateDirectory(screenshotsDir);
        }
        catch (Exception ex)
        {
            // Output dir not writable -> stderr only; we still try to run so the orchestrator sees a process.
            Console.Error.WriteLine($"Stride.Games.AutoTesting: cannot create output dir '{outputDir}': {ex}");
        }

        // Mirror error stream into error.log alongside the test artifacts.
        // FileShare.ReadWrite|Delete so the orchestrator can copy / overwrite this file while we
        // still hold it open — otherwise a forced timeout-kill races against the orchestrator's
        // artifact copy and triggers IOException("file is being used by another process").
        try
        {
            var errorLogPath = Path.Combine(outputDir, ErrorLogName);
            var errorLogStream = new FileStream(
                errorLogPath, FileMode.Create, FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete);
            var errorLog = new StreamWriter(errorLogStream) { AutoFlush = true };
            Console.SetError(new TeeWriter(Console.Error, errorLog));
        }
        catch
        {
            // Best-effort logging.
        }

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Wire simulated input so the script can press keys / tap regardless of platform window state.
        simulatedInput = new InputSourceSimulated();
        game.Input.Sources.Clear();
        game.Input.Sources.Add(simulatedInput);
        keyboard = simulatedInput.AddKeyboard();
        mouse = simulatedInput.AddMouse();

        // CaptureSystem runs at the end of every Draw and processes pendingScreenshots queue.
        game.GameSystems.Add(new CaptureSystem(this));

        var ctx = new Context(this);
        game.Script.AddTask(async () =>
        {
            string status = "ok";
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
                exitCode = 1;
            }
            finally
            {
                WriteDoneJson(status, exceptionInfo);
                Environment.ExitCode = exitCode;
                if (!exitRequested)
                {
                    exitRequested = true;
                    game.Exit();
                }
            }
        });
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        WriteDoneJson("crashed", e.ExceptionObject is Exception ex ? SerializeException(ex) : new { message = e.ExceptionObject?.ToString() });
        Console.Error.WriteLine(e.ExceptionObject);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteDoneJson("crashed", SerializeException(e.Exception));
        Console.Error.WriteLine(e.Exception);
        e.SetObserved();
    }

    private void WriteDoneJson(string status, object? exceptionInfo)
    {
        try
        {
            var donePath = Path.Combine(outputDir, DoneFileName);
            var payload = new
            {
                status,
                screenshots = captured,
                exception = exceptionInfo,
            };
            File.WriteAllText(donePath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Stride.Games.AutoTesting: failed to write {DoneFileName}: {ex}");
        }
    }

    // ClaudeFallback is null (no fallback), true (generic prompt), or a string (extra guidance).
    private sealed record CapturedScreenshot(string Name, float Threshold, object? ClaudeFallback);

    private static object SerializeException(Exception ex) => new
    {
        type = ex.GetType().FullName,
        message = ex.Message,
        stack = ex.ToString(),
    };

    // DXGI ignores backbuffer alpha; PNG viewers don't. Force-opaque so the saved frame
    // matches what the user sees (FreeImage then strips the uniform alpha to 3-channel RGB).
    private static unsafe void ForceAlphaOpaque(Image image)
    {
        var format = image.Description.Format;
        if (format != PixelFormat.R8G8B8A8_UNorm && format != PixelFormat.R8G8B8A8_UNorm_SRgb &&
            format != PixelFormat.B8G8R8A8_UNorm && format != PixelFormat.B8G8R8A8_UNorm_SRgb)
            return;

        var buffer = image.PixelBuffer[0];
        var ptr = (byte*)buffer.DataPointer;
        int len = buffer.BufferStride;
        for (int i = 3; i < len; i += 4)
            ptr[i] = 0xFF;
    }

    /// <summary>Game system that drains pending screenshot requests at the end of every Draw.</summary>
    private sealed class CaptureSystem(ScreenshotTestRunner runner) : GameSystemBase(runner.game.Services)
    {
        public CaptureSystem InitOrder()
        {
            // Run after default GameSystems so the back buffer reflects the final composited frame.
            DrawOrder = int.MaxValue;
            Visible = true;
            return this;
        }

        public override void Initialize()
        {
            base.Initialize();
            DrawOrder = int.MaxValue;
            Visible = true;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            while (runner.pendingScreenshots.TryDequeue(out var pending))
            {
                try
                {
                    var path = Path.Combine(runner.screenshotsDir, pending.Name + ".png");
                    var presenter = runner.game.GraphicsDevice.Presenter;
                    var commandList = runner.game.GraphicsContext.CommandList;
                    using var image = presenter.BackBuffer.GetDataAsImage(commandList);
                    ForceAlphaOpaque(image);
                    using var stream = File.Create(path);
                    image.Save(stream, ImageFileType.Png);
                    runner.captured.Add(new CapturedScreenshot(pending.Name, pending.Threshold, pending.ClaudeFallback));
                    pending.Tcs.SetResult();
                }
                catch (Exception ex)
                {
                    pending.Tcs.SetException(ex);
                }
            }
        }
    }

    /// <summary>StreamWriter that mirrors writes to two underlying writers.</summary>
    private sealed class TeeWriter(System.IO.TextWriter primary, System.IO.TextWriter secondary) : System.IO.TextWriter
    {
        public override Encoding Encoding => primary.Encoding;

        public override void Write(char value) { primary.Write(value); secondary.Write(value); }
        public override void Write(string? value) { primary.Write(value); secondary.Write(value); }
        public override void WriteLine(string? value) { primary.WriteLine(value); secondary.WriteLine(value); }
        public override void Flush() { primary.Flush(); secondary.Flush(); }
    }

    /// <summary>Implementation of <see cref="IScreenshotTestContext"/> handed to the user's script.</summary>
    private sealed class Context(ScreenshotTestRunner runner) : IScreenshotTestContext
    {
        public Game Game => runner.game;

        public async Task WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                await runner.game.Script.NextFrame();
        }

        public async Task WaitTime(TimeSpan duration)
        {
            var deadline = runner.game.UpdateTime.Total + duration;
            while (runner.game.UpdateTime.Total < deadline)
                await runner.game.Script.NextFrame();
        }

        public Task Screenshot(string name, float threshold = 0.05f, object? claudeFallback = null)
        {
            // Default null → true (generic prompt). Pass `false` to opt out.
            var tcs = new TaskCompletionSource();
            runner.pendingScreenshots.Enqueue((name, threshold, claudeFallback ?? (object)true, tcs));
            return tcs.Task;
        }

        public void PressKey(Keys key) => runner.keyboard.SimulateDown(key);

        public void ReleaseKey(Keys key) => runner.keyboard.SimulateUp(key);

        public async Task PressKey(Keys key, TimeSpan duration)
        {
            runner.keyboard.SimulateDown(key);
            await WaitTime(duration);
            runner.keyboard.SimulateUp(key);
        }

        public async Task Tap(Vector2 normalizedPosition, TimeSpan duration)
        {
            runner.mouse.SimulatePointer(PointerEventType.Pressed, normalizedPosition);
            await WaitTime(duration);
            runner.mouse.SimulatePointer(PointerEventType.Released, normalizedPosition);
        }

        public void Exit(int exitCode)
        {
            runner.exitCode = exitCode;
            Environment.ExitCode = exitCode;
            runner.exitRequested = true;
            runner.game.Exit();
        }
    }
}
