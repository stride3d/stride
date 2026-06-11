// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>The test registered for this run, or null if none (non-test build).</summary>
    internal static IScreenshotTest? RegisteredTest { get; private set; }

    /// <summary>
    /// Registers the screenshot-test driver for this sample. Called from a [ModuleInitializer] in
    /// the test fixture: invoking a method here force-loads this assembly (so the
    /// <see cref="ScreenshotTestRunner"/>'s [ModuleInitializer] runs and hooks
    /// <see cref="Game.GameStarted"/>), and hands over the concrete instance directly — no reflection,
    /// keeping the discovery path trim/AOT-clean.
    /// </summary>
    public static void RegisterTest(IScreenshotTest test)
    {
        ArgumentNullException.ThrowIfNull(test);
        if (RegisteredTest is not null && RegisteredTest.GetType() != test.GetType())
            throw new InvalidOperationException(
                $"Stride.Games.AutoTesting: '{RegisteredTest.GetType().FullName}' is already registered; " +
                $"exactly one [ScreenshotTest] per sample is allowed (tried to register '{test.GetType().FullName}').");
        RegisteredTest = test;
    }
}

/// <summary>
/// Installs native-crash diagnostics for the sample run from a [ModuleInitializer].
/// See <see cref="NativeCrashHandler"/> (shared with Stride.Graphics.Regression).
/// </summary>
internal static class CrashDiagnostics
{
    [ModuleInitializer]
    internal static void Initialize() => NativeCrashHandler.Install();
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
        // OnGameStarted fires before graphicsDeviceManager.CreateDevice, so the flags are in effect
        // by the time the swap chain is created.
        var deviceManager = (GraphicsDeviceManager)game.GraphicsDeviceManager;
        deviceManager.SkipBackBufferClampToWindow = true;

        // Present without vsync: tests don't need display pacing, and compositor pacing is
        // unreliable for hidden CI windows (throttled or unserviced presents slow the run).
        deviceManager.SynchronizeWithVerticalRetrace = false;

        // The test fixture self-registers from a [ModuleInitializer] (see AutoTestingBootstrap.RegisterTest).
        var test = AutoTestingBootstrap.RegisteredTest;
        if (test is null)
            return;

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
            ExceptionInfo? exceptionInfo = null;
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
        WriteDoneJson("crashed", e.ExceptionObject is Exception ex ? SerializeException(ex) : new ExceptionInfo(null, e.ExceptionObject?.ToString(), null));
        Console.Error.WriteLine(e.ExceptionObject);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteDoneJson("crashed", SerializeException(e.Exception));
        Console.Error.WriteLine(e.Exception);
        e.SetObserved();
    }

    // Hand-written with Utf8JsonWriter so it works under NativeAOT, which disables reflection-based
    // serialization. Property names/casing match what the orchestrator and comparator read back.
    private void WriteDoneJson(string status, ExceptionInfo? exceptionInfo)
    {
        try
        {
            var donePath = Path.Combine(outputDir, DoneFileName);
            using var stream = File.Create(donePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            writer.WriteString("status", status);

            writer.WriteStartArray("screenshots");
            foreach (var shot in captured)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", shot.Name);
                writer.WriteNumber("Threshold", shot.Threshold);
                // null (no fallback) / true (generic prompt) / string (extra guidance)
                switch (shot.ClaudeFallback)
                {
                    case bool b: writer.WriteBoolean("ClaudeFallback", b); break;
                    case string s: writer.WriteString("ClaudeFallback", s); break;
                    default: writer.WriteNull("ClaudeFallback"); break;
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            if (exceptionInfo is null)
            {
                writer.WriteNull("exception");
            }
            else
            {
                writer.WriteStartObject("exception");
                writer.WriteString("type", exceptionInfo.Type);
                writer.WriteString("message", exceptionInfo.Message);
                writer.WriteString("stack", exceptionInfo.Stack);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Stride.Games.AutoTesting: failed to write {DoneFileName}: {ex}");
        }
    }

    // ClaudeFallback is null (no fallback), true (generic prompt), or a string (extra guidance).
    private sealed record CapturedScreenshot(string Name, float Threshold, object? ClaudeFallback);

    private sealed record ExceptionInfo(string? Type, string? Message, string? Stack);

    private static ExceptionInfo SerializeException(Exception ex) => new(ex.GetType().FullName, ex.Message, ex.ToString());

    // DXGI ignores backbuffer alpha; PNG viewers don't. Force-opaque so the saved frame
    // matches what the user sees on screen.
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
