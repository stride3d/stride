// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Components.Recovery;

/// <summary>
/// Keeps recovery copies of the unsaved changes of a session outside of its project, so that they can be restored after a crash.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Interval"/>, while the session has unsaved changes, the service writes the content of each changed or new asset
/// (as a save would write it) and the list of the deleted assets to a folder of its process under the recovery root. It never writes
/// the files of the project, changes no dirty flag and adds nothing to the undo history. A save of the session or a clean close removes
/// the copies.
/// </para>
/// </remarks>
public sealed class SessionRecoveryService : IDisposable
{
    /// <summary>
    /// The default time between two recovery copies.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    private static readonly Logger Log = GlobalLogger.GetLogger("Recovery");

    private readonly SessionViewModel session;
    private readonly RecoveryStore store;
    private readonly List<Func<bool>> skipConditions = [];
    // The assets of each editable package as their files are on the disk (package path to assets), to tell the deleted assets
    private Dictionary<string, Dictionary<AssetId, RecoveredAsset>> savedAssets;
    private ITimer? timer;
    // Counts the changes of the session; the copies are written again only when it has changed
    private int changeCount;
    private int copiedChangeCount = -1;
    private bool hasCopies;
    private bool isWriting;
    private bool savedWhileWriting;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRecoveryService"/> class for a session that has just been opened: what its
    /// editable packages hold now is taken as what is on the disk.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="sessionPath">The file the session was opened from (solution or package), which names its recovery folder.</param>
    /// <param name="rootPath">The folder of the recovery copies of all the sessions, outside of any project (<see cref="EditorPath.RecoveryPath"/>).</param>
    public SessionRecoveryService(SessionViewModel session, UFile sessionPath, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sessionPath);
        this.session = session;
        store = new RecoveryStore(rootPath, sessionPath.ToOSPath());
        savedAssets = CaptureSavedAssets();
    }

    /// <summary>
    /// Gets or sets the time between two recovery copies.
    /// </summary>
    public TimeSpan Interval
    {
        get;
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            field = value;
            UpdateTimer();
        }
    } = DefaultInterval;

    /// <summary>
    /// Gets or sets whether the recovery copies are written. Turning it off removes the copies of this editor.
    /// </summary>
    public bool IsEnabled
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            UpdateTimer();
            if (!value)
                RemoveCopies();
        }
    } = true;

    /// <summary>
    /// Gets when the last recovery copies were written (local time), or <c>null</c> if none were.
    /// </summary>
    public DateTime? LastCopyTime { get; private set; }

    /// <summary>
    /// Gets the folder of the recovery copies of this editor, once <see cref="Start"/> has been called.
    /// </summary>
    public string? ProcessFolder { get; private set; }

    /// <summary>
    /// Gets or sets the clock of the timer; the tests give one that they drive.
    /// </summary>
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Raised on the UI thread when recovery copies have been written.
    /// </summary>
    public event EventHandler? CopiesWritten;

    /// <summary>
    /// Adds a condition under which the timer writes no copies, such as a build or a modal dialog of the editor.
    /// </summary>
    /// <param name="condition">Returns <c>true</c> when the copies must wait; called on the UI thread.</param>
    public void AddSkipCondition(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        skipConditions.Add(condition);
    }

    /// <summary>
    /// Marks the session as open in this process and starts the timer.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (ProcessFolder is not null)
            return;

        ProcessFolder = store.WriteMarker(store.CreateCurrentMarker());
        session.UndoRedoService.Done += SessionChanged;
        session.UndoRedoService.Undone += SessionChanged;
        session.UndoRedoService.Redone += SessionChanged;
        session.PropertyChanged += SessionPropertyChanged;
        session.SessionSaved += SessionSaved;
        UpdateTimer();
    }

    /// <summary>
    /// Writes the recovery copies when they are due: the service is on, nothing that the copies must wait for is going on (a save, a
    /// change in progress, a skip condition), and the session has changed since the last copies. The timer calls it on the UI thread.
    /// </summary>
    /// <returns><c>true</c> if copies were written.</returns>
    public Task<bool> WriteCopiesIfDueAsync()
    {
        if (disposed || !IsEnabled || ProcessFolder is null || IsBusy())
            return Task.FromResult(false);
        return WriteCopiesAsync();
    }

    /// <summary>
    /// Writes the recovery copies of the unsaved changes of the session now, unless they were written since its last change; removes
    /// them when the session has no unsaved change. Called on the UI thread; the files are written on a background thread.
    /// </summary>
    /// <returns><c>true</c> if copies were written.</returns>
    public async Task<bool> WriteCopiesAsync()
    {
        session.Dispatcher.EnsureAccess();
        if (disposed || ProcessFolder is null || isWriting)
            return false;

        isWriting = true;
        savedWhileWriting = false;
        try
        {
            var changes = Volatile.Read(ref changeCount);
            if (!session.HasUnsavedAssets())
            {
                copiedChangeCount = changes;
                if (hasCopies)
                    await RemoveCopiesAsync();
                return false;
            }
            if (hasCopies && changes == copiedChangeCount)
                return false;

            // The content of the assets is read on the UI thread, where they change; the files are written on another thread
            var (manifest, contents) = CreateCopies();
            var folder = ProcessFolder;
            await Task.Run(() => store.WriteCopies(folder, manifest, contents));
            hasCopies = true;
            copiedChangeCount = changes;

            // A save that ran meanwhile wrote the changes to the project: the copies are not needed
            if (savedWhileWriting && !session.HasUnsavedAssets())
            {
                await RemoveCopiesAsync();
                return false;
            }

            LastCopyTime = manifest.Time.ToLocalTime();
            CopiesWritten?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception e)
        {
            Log.Error("The recovery copies of the session could not be written.", e);
            return false;
        }
        finally
        {
            isWriting = false;
        }
    }

    /// <summary>
    /// Stops the timer and removes the folder of this editor, with its copies: the session closes cleanly, saved or not.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        timer?.Dispose();
        timer = null;
        if (ProcessFolder is null)
            return;

        session.UndoRedoService.Done -= SessionChanged;
        session.UndoRedoService.Undone -= SessionChanged;
        session.UndoRedoService.Redone -= SessionChanged;
        session.PropertyChanged -= SessionPropertyChanged;
        session.SessionSaved -= SessionSaved;
        store.DeleteProcessFolder(ProcessFolder);
        ProcessFolder = null;
    }

    private bool IsBusy()
    {
        // A save writes the same assets; a load, a transaction or an undo leaves the assets half changed
        if (session.IsSaving || !session.Initialization.IsCompleted)
            return true;
        var undoRedoService = session.UndoRedoService;
        if (undoRedoService.TransactionInProgress || undoRedoService.UndoRedoInProgress)
            return true;
        // The editor's own conditions: a close, a build, a modal dialog (and later the game that plays)
        return skipConditions.Any(x => x());
    }

    private void UpdateTimer()
    {
        if (disposed || ProcessFolder is null || !IsEnabled)
        {
            timer?.Dispose();
            timer = null;
            return;
        }
        if (timer is null)
            timer = TimeProvider.CreateTimer(_ => OnTimer(), null, Interval, Interval);
        else
            timer.Change(Interval, Interval);
    }

    private void OnTimer()
    {
        if (disposed)
            return;
        session.Dispatcher.InvokeTask(WriteCopiesIfDueAsync).Forget();
    }

    private void SessionChanged(object? sender, EventArgs e) => Interlocked.Increment(ref changeCount);

    private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The text of a script changes without a transaction
        if (e.PropertyName == nameof(SessionViewModel.HasUnsavedChanges))
            Interlocked.Increment(ref changeCount);
    }

    private void SessionSaved(object? sender, EventArgs e)
    {
        // What the packages hold is on the disk now, apart from what could not be saved, which stays dirty
        savedAssets = CaptureSavedAssets();
        Interlocked.Increment(ref changeCount);
        if (isWriting)
        {
            savedWhileWriting = true;
            return;
        }
        if (!session.HasUnsavedAssets())
            RemoveCopies();
    }

    private void RemoveCopies()
    {
        if (!hasCopies || ProcessFolder is null || isWriting)
            return;
        hasCopies = false;
        RecoveryStore.DeleteCopies(ProcessFolder);
    }

    private async Task RemoveCopiesAsync()
    {
        var folder = ProcessFolder;
        hasCopies = false;
        if (folder is not null)
            await Task.Run(() => RecoveryStore.DeleteCopies(folder));
    }

    private IEnumerable<PackageViewModel> EditablePackages => session.LocalPackages.Where(x => x.IsEditable && x.PackagePath is not null);

    private Dictionary<string, Dictionary<AssetId, RecoveredAsset>> CaptureSavedAssets()
    {
        var result = new Dictionary<string, Dictionary<AssetId, RecoveredAsset>>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in EditablePackages)
        {
            result[package.PackagePath.ToOSPath()] = package.Assets.ToDictionary(x => x.Id, x => Describe(package, x));
        }
        return result;
    }

    private static RecoveredAsset Describe(PackageViewModel package, AssetViewModel asset, int file = 0) => new()
    {
        Id = (Guid)asset.Id,
        Location = asset.AssetItem.Location,
        FilePath = asset.AssetItem.FullPath.MakeRelative(package.PackagePath.GetFullDirectory()).ToString(),
        File = file,
    };

    private (RecoveryManifest Manifest, List<byte[]> Contents) CreateCopies()
    {
        var manifest = new RecoveryManifest { Time = DateTime.UtcNow };
        var contents = new List<byte[]>();
        var logger = new LoggerResult();
        foreach (var package in EditablePackages)
        {
            var recoveredPackage = new RecoveredPackage { Name = package.Name, Path = package.PackagePath.ToOSPath() };
            var currentIds = new HashSet<AssetId>();
            foreach (var asset in package.Assets)
            {
                currentIds.Add(asset.Id);
                if (!asset.IsDirty && !asset.AssetItem.IsDirty)
                    continue;
                // Code and shaders are files of their project, which the session writes as they are
                if (asset.Asset is IProjectAsset)
                    continue;

                asset.PrepareSave(logger);
                recoveredPackage.Assets.Add(Describe(package, asset, contents.Count));
                contents.Add(ExternalAssetChangeTracker.SerializeAsSaved(asset.AssetItem, logger));
            }
            if (savedAssets.TryGetValue(recoveredPackage.Path, out var saved))
                recoveredPackage.DeletedAssets.AddRange(saved.Where(x => !currentIds.Contains(x.Key)).Select(x => x.Value));
            if (package.IsDirty)
                recoveredPackage.RootAssets = package.RootAssets.Select(x => (Guid)x.Id).ToList();

            if (recoveredPackage.Assets.Count > 0 || recoveredPackage.DeletedAssets.Count > 0 || recoveredPackage.RootAssets is not null)
                manifest.Packages.Add(recoveredPackage);
        }
        return (manifest, contents);
    }
}

