// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Stride.Core.Diagnostics;
using Stride.Graphics;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// The state of a <see cref="GpuFramePresenter"/>.
/// </summary>
internal enum GpuFramePresenterState
{
    /// <summary>
    /// The compositor was not asked yet (the view is not in a visual tree): the frames are read back meanwhile.
    /// </summary>
    Pending,

    /// <summary>
    /// The compositor accepted the shared texture: the frames go through the GPU.
    /// </summary>
    Active,

    /// <summary>
    /// The compositor has no GPU interop, or none for the game's graphics API, or an update failed: the frames are read
    /// back (<see cref="GpuFramePresenter.FallbackReason"/>).
    /// </summary>
    Fallback,
}

/// <summary>
/// Hands the frames of an offscreen game to Avalonia's compositor through a texture they share
/// (<see cref="ICompositionGpuInterop"/>), with no copy through the CPU.
/// </summary>
/// <remarks>
/// <para>
/// Two pairs are supported, the ones Avalonia 12 imports:
/// </para>
/// <list type="bullet">
///   <item>Direct3D 11 (the editor's API on Windows) with Avalonia on ANGLE (its default on Windows): textures created
///   with the <c>SharedKeyedMutex</c> and <c>SharedNtHandle</c> options, shared through their NT handle (the handle type
///   Direct3D 12 and Vulkan share through too, so a later API reuses this path) and synchronized by their keyed mutex
///   (the game acquires key 0 and releases key 1, the compositor acquires key 1 and releases key 0). Two textures are
///   used in turn: the game copies a frame into one while the compositor draws the other, and drops a frame only when
///   both are in use.</item>
///   <item>Vulkan (Linux) with Avalonia on Vulkan or on OpenGL (with <c>GL_EXT_memory_object_fd</c> and
///   <c>GL_EXT_semaphore_fd</c>): an <see cref="ExportableTexture"/>, synchronized by a pair of binary semaphores, with
///   one frame in flight at a time.</item>
/// </list>
/// <para>
/// When the compositor has no interop, or none of these pairs, or an update fails, the presenter falls back to the readback
/// of <see cref="OffscreenFrameExporter"/> on the same view, and logs the reason once.
/// </para>
/// </remarks>
internal sealed class GpuFramePresenter : IDisposable
{
    private const string D3D11NtHandle = KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle;
    private const string VulkanFd = KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaquePosixFileDescriptor;
    private const string VulkanSemaphoreFd = KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor;

    // Direct3D 11's TextureOptions.SharedKeyedMutex | SharedNtHandle, which only exist in the Direct3D 11 build of Stride.Graphics
    private const TextureOptions SharedKeyedMutexNtHandle = (TextureOptions)(256 | 2048);

    private readonly OffscreenGameView view;
    private readonly Logger logger;
    private volatile GpuFramePresenterState state;
    private volatile bool frameInFlight;
    private string? handleType;

    // Direct3D 11: the shared textures, used in turn
    private readonly D3DSlot[] d3dSlots = [new D3DSlot(), new D3DSlot()];

    // Game thread
    private int nextD3DSlot;
    private D3DSlot? pendingD3DSlot;
    private ExportableTexture? exportableTexture;
    private int textureVersion;
    private bool firstFrame;
    private bool signalPending;

    // UI thread
    private ICompositionGpuInterop? interop;
    private CompositionDrawingSurface? surface;
    private int importedVersion = -1;
    private ICompositionImportedGpuImage? importedImage;
    private ICompositionImportedGpuSemaphore? readySemaphore;
    private ICompositionImportedGpuSemaphore? releasedSemaphore;

    private GpuFramePresenter(OffscreenGameView view, Logger logger)
    {
        this.view = view;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the state of the presenter.
    /// </summary>
    public GpuFramePresenterState State => state;

    /// <summary>
    /// Gets whether the compositor accepted the shared texture: until then, and after a failure, the frames are read back.
    /// </summary>
    public bool IsActive => state == GpuFramePresenterState.Active;

    /// <summary>
    /// Gets why the frames are read back, once <see cref="State"/> is <see cref="GpuFramePresenterState.Fallback"/>.
    /// </summary>
    public string? FallbackReason { get; private set; }

    /// <summary>
    /// Gets the type of handle the compositor imports, once <see cref="State"/> is <see cref="GpuFramePresenterState.Active"/>.
    /// </summary>
    public string? HandleType => handleType;

    /// <summary>
    /// Creates the presenter of a view. Its interop is set up once the view is in a visual tree, and <see cref="State"/>
    /// says whether the frames go through the GPU or through a readback.
    /// </summary>
    /// <remarks>Call it before the game creates its graphics device: a Vulkan device must enable the export extensions.</remarks>
    public static GpuFramePresenter Create(OffscreenGameView view, Logger logger)
    {
        ExportableTexture.EnableExternalObjects = true;
        var presenter = new GpuFramePresenter(view, logger);
        Dispatcher.UIThread.Post(() =>
        {
            if (view.Compositor != null)
                _ = presenter.InitializeAsync();
            else
                view.AttachedToVisualTree += presenter.OnViewAttached;
        });
        return presenter;
    }

    /// <summary>
    /// Gets the identifier (LUID) of the GPU the editor's compositor renders with, or <c>null</c> when it has no GPU interop
    /// or doesn't say (game thread, before the game creates its device).
    /// </summary>
    /// <remarks>
    /// A shared texture only opens on the GPU that created it. Stride prefers the high-performance GPU while Avalonia
    /// (ANGLE) uses the default one: on a machine with two GPUs (integrated and discrete), the game must render on the
    /// compositor's GPU.
    /// </remarks>
    public static long? TryGetCompositorAdapterUid(Logger logger)
    {
        try
        {
            var task = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var lifetime = global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                var window = lifetime?.MainWindow;
                var compositor = window != null ? ElementComposition.GetElementVisual(window)?.Compositor : null;
                var interop = compositor != null ? await compositor.TryGetCompositionGpuInterop() : null;
                return interop?.DeviceLuid;
            });
            if (!task.Wait(TimeSpan.FromSeconds(5)))
            {
                logger.Warning("Viewport: no answer from the UI thread about the compositor's GPU.");
                return null;
            }
            var luid = task.Result;
            return luid is { Length: 8 } ? BitConverter.ToInt64(luid, 0) : null;
        }
        catch (Exception ex)
        {
            logger.Warning("Viewport: could not get the compositor's GPU.", ex);
            return null;
        }
    }

    /// <summary>
    /// Copies the back buffer into the shared texture (game thread, before the command list of the frame is closed).
    /// </summary>
    /// <returns><c>false</c> when the compositor still uses the previous frames and this one is dropped.</returns>
    public bool CopyFrame(CommandList commandList, Texture backBuffer)
    {
        try
        {
            if (handleType == D3D11NtHandle)
                return CopyFrameD3D11(commandList, backBuffer);
            if (frameInFlight)
                return false;
            return CopyFrameVulkan(commandList, backBuffer);
        }
        catch (Exception e)
        {
            Fail($"The game could not share its frame: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Signals the compositor that the frame is ready (game thread, after the command list was submitted).
    /// </summary>
    public void AfterSubmit()
    {
        if (!signalPending)
            return;
        signalPending = false;
        exportableTexture?.SignalReady();
        firstFrame = false;

        var d3dSlot = pendingD3DSlot;
        pendingD3DSlot = null;
        var version = d3dSlot?.Version ?? textureVersion;
        var vkTexture = exportableTexture;
        Dispatcher.UIThread.Post(() => Present(version, d3dSlot, vkTexture), DispatcherPriority.Render);
    }

    /// <summary>
    /// Releases the shared textures (game thread, once the game ended) and the compositor's imports of them.
    /// </summary>
    public void Dispose()
    {
        state = GpuFramePresenterState.Fallback;
        Dispatcher.UIThread.Post(ReleaseImports);
        foreach (var slot in d3dSlots)
            slot.Texture?.Dispose();
        exportableTexture?.Dispose();
    }

    private bool CopyFrameD3D11(CommandList commandList, Texture backBuffer)
    {
        // The textures are used in turn: the next one must be done with (the compositor drew it and released its mutex)
        var slot = d3dSlots[nextD3DSlot];
        if (slot.InFlight)
            return false;

        if (slot.Texture == null || slot.Texture.Width != backBuffer.Width || slot.Texture.Height != backBuffer.Height)
        {
            // The compositor opened its own reference to the texture: this one can go
            slot.Texture?.Dispose();
            slot.Texture = Texture.New2D(commandList.GraphicsDevice, backBuffer.Width, backBuffer.Height, Stride.Graphics.PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.RenderTarget | TextureFlags.ShaderResource, options: SharedKeyedMutexNtHandle);
            slot.Version++;
        }

        // A new keyed mutex is free for key 0; afterwards the compositor releases it with key 0
        if (!slot.Texture.AcquireSync(0, 0))
            return false;
        // The sRGB back buffer and the UNorm texture are in the same typeless group: a plain copy keeps the bytes
        commandList.Copy(backBuffer, slot.Texture);
        slot.Texture.ReleaseSync(1);
        slot.InFlight = true;
        pendingD3DSlot = slot;
        nextD3DSlot = (nextD3DSlot + 1) % d3dSlots.Length;
        signalPending = true;
        return true;
    }

    private bool CopyFrameVulkan(CommandList commandList, Texture backBuffer)
    {
        if (exportableTexture == null || exportableTexture.Texture.Width != backBuffer.Width || exportableTexture.Texture.Height != backBuffer.Height)
        {
            if (exportableTexture == null && !ExportableTexture.IsSupported(commandList.GraphicsDevice))
            {
                Fail($"The Vulkan device ({commandList.GraphicsDevice.Adapter.Description}) has no VK_KHR_external_memory_fd and VK_KHR_external_semaphore_fd.");
                return false;
            }
            // The compositor imported its own reference to the memory: this one can go
            exportableTexture?.Dispose();
            exportableTexture = ExportableTexture.Create(commandList.GraphicsDevice, backBuffer.Width, backBuffer.Height);
            textureVersion++;
            firstFrame = true;
        }

        // The compositor signaled the released semaphore when its last update completed
        if (!firstFrame)
            exportableTexture.WaitReleased();
        commandList.Copy(backBuffer, exportableTexture.Texture);
        // The compositor reads the image in the transfer source layout
        commandList.ResourceBarrierTransition(exportableTexture.Texture, BarrierLayout.CopySource);
        frameInFlight = true;
        signalPending = true;
        return true;
    }

    private void OnViewAttached(object? sender, global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        view.AttachedToVisualTree -= OnViewAttached;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var compositor = view.Compositor;
            interop = compositor != null ? await compositor.TryGetCompositionGpuInterop() : null;
            if (interop == null)
            {
                Fail("The compositor has no GPU interop (software rendering, or a renderer without external objects).");
                return;
            }

            var imageTypes = string.Join(", ", interop.SupportedImageHandleTypes);
            var semaphoreTypes = string.Join(", ", interop.SupportedSemaphoreTypes);
            if (GraphicsDevice.Platform == GraphicsPlatform.Direct3D11
                && interop.SupportedImageHandleTypes.Contains(D3D11NtHandle)
                && interop.GetSynchronizationCapabilities(D3D11NtHandle).HasFlag(CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex))
            {
                handleType = D3D11NtHandle;
            }
            else if (GraphicsDevice.Platform == GraphicsPlatform.Vulkan
                && interop.SupportedImageHandleTypes.Contains(VulkanFd)
                && interop.SupportedSemaphoreTypes.Contains(VulkanSemaphoreFd)
                && interop.GetSynchronizationCapabilities(VulkanFd).HasFlag(CompositionGpuImportedImageSynchronizationCapabilities.Semaphores))
            {
                handleType = VulkanFd;
            }
            else
            {
                Fail($"The compositor can't import a {GraphicsDevice.Platform} texture (images [{imageTypes}], semaphores [{semaphoreTypes}]).");
                return;
            }

            surface = view.CreateDrawingSurface();
            if (surface == null)
            {
                Fail("The view has no compositor.");
                return;
            }
            state = GpuFramePresenterState.Active;
            logger.Info($"Viewport: GPU interop with {handleType} (compositor images [{imageTypes}], semaphores [{semaphoreTypes}]).");
        }
        catch (Exception e)
        {
            Fail($"The GPU interop could not be set up: {e.Message}");
        }
    }

    private void Present(int version, D3DSlot? d3dSlot, ExportableTexture? vkTexture)
    {
        void Release()
        {
            if (d3dSlot != null)
                d3dSlot.InFlight = false;
            else
                frameInFlight = false;
        }

        if (state != GpuFramePresenterState.Active || surface == null || interop == null)
        {
            Release();
            return;
        }

        try
        {
            ICompositionImportedGpuImage image;
            Task update;
            if (d3dSlot != null)
            {
                if (d3dSlot.ImportedVersion != version)
                    ImportD3D11(d3dSlot, version);
                image = d3dSlot.Image!;
                update = surface.UpdateWithKeyedMutexAsync(image, 1, 0);
            }
            else
            {
                if (importedVersion != version)
                    ImportVulkan(version, vkTexture);
                image = importedImage!;
                update = surface.UpdateWithSemaphoresAsync(image, readySemaphore!, releasedSemaphore!);
            }
            update.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // An image whose import failed makes every update throw "context lost": the import says why
                    var importError = image.ImportCompleted.IsFaulted ? image.ImportCompleted.Exception?.GetBaseException() : null;
                    var reason = importError != null ? $"the import failed: {importError.Message}" : task.Exception?.GetBaseException().Message;
                    Fail($"The compositor could not update the surface (image lost: {image.IsLost}): {reason}");
                }
                Release();
            }, TaskScheduler.Default);
        }
        catch (Exception e)
        {
            Fail($"The compositor could not import the frame: {e.Message}");
            Release();
        }
    }

    private void ImportD3D11(D3DSlot slot, int version)
    {
        _ = slot.Image?.DisposeAsync();
        slot.Image = null;
        var texture = slot.Texture ?? throw new InvalidOperationException("No shared texture");
        slot.Image = interop!.ImportImage(new PlatformHandle(texture.SharedHandle, handleType!), new PlatformGraphicsExternalImageProperties
        {
            Width = texture.Width,
            Height = texture.Height,
            Format = PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
        });
        slot.ImportedVersion = version;
    }

    private void ImportVulkan(int version, ExportableTexture? vkTexture)
    {
        ReleaseImports();
        var texture = vkTexture ?? throw new InvalidOperationException("No exportable texture");
        // The compositor owns the file descriptors
        importedImage = interop!.ImportImage(new PlatformHandle(texture.ExportMemory(), VulkanFd), new PlatformGraphicsExternalImageProperties
        {
            Width = texture.Texture.Width,
            Height = texture.Texture.Height,
            Format = PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
            MemorySize = texture.MemorySize,
        });
        readySemaphore = interop.ImportSemaphore(new PlatformHandle(texture.ExportReadySemaphore(), VulkanSemaphoreFd));
        releasedSemaphore = interop.ImportSemaphore(new PlatformHandle(texture.ExportReleasedSemaphore(), VulkanSemaphoreFd));
        importedVersion = version;
    }

    private void ReleaseImports()
    {
        _ = importedImage?.DisposeAsync();
        _ = readySemaphore?.DisposeAsync();
        _ = releasedSemaphore?.DisposeAsync();
        importedImage = null;
        readySemaphore = releasedSemaphore = null;
        importedVersion = -1;
        foreach (var slot in d3dSlots)
        {
            _ = slot.Image?.DisposeAsync();
            slot.Image = null;
            slot.ImportedVersion = -1;
        }
    }

    private void Fail(string reason)
    {
        if (state == GpuFramePresenterState.Fallback)
            return;
        state = GpuFramePresenterState.Fallback;
        FallbackReason = reason;
        Dispatcher.UIThread.Post(view.RemoveDrawingSurface);
        logger.Warning($"Viewport: the frames are read back through the CPU, the GPU interop is unavailable. {reason}");
    }

    /// <summary>
    /// A Direct3D 11 texture shared with the compositor, and its imported image.
    /// </summary>
    private sealed class D3DSlot
    {
        // Game thread (and the UI thread while the slot is in flight)
        public Texture? Texture;
        public int Version;

        // Set by the game thread when it hands the frame over, cleared once the compositor drew it
        public volatile bool InFlight;

        // UI thread
        public int ImportedVersion = -1;
        public ICompositionImportedGpuImage? Image;
    }
}
