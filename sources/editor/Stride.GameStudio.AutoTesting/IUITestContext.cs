// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.AutoTesting;

/// <summary>
/// Wait/screenshot/exit primitives handed to the test fixture by the AutoTesting runner.
/// </summary>
public interface IUITestContext
{
    /// <summary>Opens the .sln pre-positioned on disk by the runner. No-op when no project test is configured.</summary>
    Task OpenProject();

    /// <summary>Returns when the editor's asset build queue is empty for two consecutive frames.</summary>
    Task WaitForAssetBuild();

    /// <summary>Returns when the shader compile queue is empty for two consecutive frames.</summary>
    Task WaitForShaders();

    /// <summary>Returns when the WPF dispatcher has drained to ApplicationIdle priority.</summary>
    Task WaitDispatcherIdle();

    /// <summary>Awaits N successive render frames.</summary>
    Task WaitFrames(int n = 1);

    /// <summary>Convenience: awaits asset build, shaders, dispatcher idle, one trailing frame, and rendering.</summary>
    Task WaitIdle();

    /// <summary>
    /// Returns when every active <c>EditorServiceGame</c> instance — the embedded scene/prefab/UI/sprite
    /// document games and the shared asset-preview game — has advanced its <c>DrawTime.FrameCount</c>
    /// by at least <paramref name="frames"/> since the call started, ensuring swap-chains have
    /// presented real content. No-op if no games are active. Times out after
    /// <paramref name="timeoutSeconds"/> with a log message — never throws.
    /// </summary>
    Task WaitForRendering(int frames = 60, double timeoutSeconds = 30);

    /// <summary>Captures the active main window to a PNG named <paramref name="name"/>.</summary>
    Task Screenshot(string name);

    /// <summary>
    /// Resizes a top-level window (looked up by class name) to a fixed <paramref name="width"/> ×
    /// <paramref name="height"/> and centers it on the primary screen. Used by fixtures to pin the
    /// main editor window to a deterministic capture size before <see cref="Screenshot"/>.
    /// </summary>
    Task SetWindowSize(string windowTypeName, int width, int height);

    /// <summary>
    /// Floats a single docked panel or document into its own window sized to <paramref name="width"/>
    /// × <paramref name="height"/>, captures it via WGC, then restores its original docked / auto-hide
    /// state. Lookup by <c>ContentId</c> for anchorable panels (e.g. "AssetView", "PropertyGrid",
    /// "SolutionExplorer", "BuildLog", "References") or by <c>Title</c> for asset-editor documents
    /// (e.g. "MainScene") which are added with empty ContentId and Title=asset.Url.
    /// </summary>
    Task CapturePanel(string idOrTitle, string name, int width = 1200, int height = 900);

    /// <summary>
    /// Polls the WPF Application.Windows set until a Window of class name
    /// <paramref name="windowTypeName"/> is visible and loaded, or <paramref name="timeoutSeconds"/>
    /// elapses. Returns true on success.
    /// </summary>
    Task<bool> WaitForWindow(string windowTypeName, double timeoutSeconds = 120);

    /// <summary>
    /// Selects a template in the ProjectSelectionWindow by template id and returns true if found.
    /// The dialog stays open; close it via <see cref="CloseModalWithOk"/>.
    /// </summary>
    Task<bool> SelectTemplate(Guid templateId);

    /// <summary>
    /// Closes a modal dialog with <c>DialogResult.Ok</c> (equivalent to clicking OK / Create).
    /// Returns true if the window was found and closed.
    /// </summary>
    Task<bool> CloseModalWithOk(string windowTypeName);

    /// <summary>
    /// Equivalent of pressing F5 in GameStudio: builds the current project and launches the resulting
    /// .exe. Returns the launched process id (or -1 on failure).
    /// </summary>
    Task<int> RunProject();

    /// <summary>
    /// Polls the process by id until its <see cref="System.Diagnostics.Process.MainWindowHandle"/> is
    /// non-zero, then calls <c>Process.WaitForInputIdle</c>. Returns the HWND or <c>IntPtr.Zero</c> on
    /// timeout / process exit.
    /// </summary>
    Task<IntPtr> WaitForGameWindow(int pid, double timeoutSeconds = 60);

    /// <summary>
    /// Returns when WGC has delivered <paramref name="minFrames"/> frames AND
    /// <paramref name="postFirstFrameDelaySeconds"/> have elapsed since the first one (lets TAA-style
    /// post-effects converge). Default timeout absorbs cold shader-cache builds.
    /// </summary>
    Task WaitForGameFrames(IntPtr hwnd, int minFrames = 100, double postFirstFrameDelaySeconds = 2.0, double timeoutSeconds = 90);

    /// <summary>Captures a specific HWND (e.g. a game window from a child process) to a PNG.</summary>
    Task ScreenshotHwnd(IntPtr hwnd, string name);

    /// <summary>
    /// Sends <c>WM_CLOSE</c> to the game window and waits for the process to exit. Force-kills if
    /// the process doesn't exit within the timeout.
    /// </summary>
    Task CloseGameWindow(int pid, double timeoutSeconds = 30);

    /// <summary>
    /// Invokes the same <c>RunAssetTemplate</c> path the asset-templates dialog uses on OK. Pass
    /// <paramref name="templateName"/> to disambiguate when several templates share an Id (e.g.
    /// procedural-model variants). Returns the created asset's id, or <see cref="Guid.Empty"/> on
    /// failure.
    /// </summary>
    Task<Guid> AddAssetFromTemplate(Guid templateId, string templateName = null);

    /// <summary>
    /// Registers a one-shot handler for the next <c>AssetPickerWindow</c>: selects the asset by
    /// <c>Name</c> and confirms. Pass <c>null</c> to cancel the picker.
    /// </summary>
    Task QueueAssetPickerResponse(string assetName, double timeoutSeconds = 30);

    /// <summary>
    /// Adds an entity to the open scene with a <c>ModelComponent</c> referencing
    /// <paramref name="modelAssetId"/>, at <paramref name="position"/>. Goes through
    /// <c>CreateEntityInRootCommand</c> with a custom <c>IEntityFactory</c>.
    /// </summary>
    Task<bool> AddEntityToScene(string entityName, Guid modelAssetId, Stride.Core.Mathematics.Vector3 position);

    /// <summary>Sets the process exit code and shuts the editor down.</summary>
    void Exit(int exitCode = 0);
}
