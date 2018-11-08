// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Serialization;
using Xenko.Assets.Effect;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.ConnectionRouter;
using Xenko.Engine.Network;
using Xenko.Shaders.Compiler;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Extensions;

namespace Xenko.Assets.Presentation
{
    /// <summary>
    /// Handle connection to EffectCompilerServer for a given <see cref="SessionViewModel"/>.
    /// It will let user knows that some new effects were compiled and might need to be imported in your assets.
    /// </summary>
    class EffectCompilerServerSession : IDisposable
    {
        class TrackedPackage : IDisposable
        {
            private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            public CancellationToken CancellationToken => cancellationTokenSource.Token;

            public PackageViewModel Package { get; }

            public TrackedPackage(PackageViewModel package)
            {
                Package = package;
            }

            public void Dispose()
            {
                cancellationTokenSource.Cancel();
            }
        }

        private readonly SessionViewModel session;
        private readonly IDispatcherService dispatcher;
        private HashSet<EffectCompileRequest> pendingEffects = new HashSet<EffectCompileRequest>();
        private readonly Task routerLaunchedTask;
        private readonly List<TrackedPackage> trackedPackages = new List<TrackedPackage>();

        // Updated by CheckEffectLogAsset()
        private EffectLogViewModel effectLogViewModel;
        private string effectLogText; // to test for change
        private EffectLogStore effectLogStore;
        private MemoryStream effectLogStream;
        private bool isDisposed;

        public EffectCompilerServerSession(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            this.session = session;
            this.dispatcher = session.ServiceProvider.Get<IDispatcherService>();

            routerLaunchedTask = Task.Run(() =>
            {
                RouterHelper.EnsureRouterLaunched();
            });

            TrackPackages(session.LocalPackages);
            session.LocalPackages.CollectionChanged += LocalPackages_CollectionChanged;

            session.ImportEffectLogCommand = new AnonymousCommand(session.ServiceProvider, () =>
            {
                if (session.CurrentProject != null)
                    ImportEffectLog(session.CurrentProject);
            }, () => session.CurrentProject?.Package != null);
        }

        private async void Start(PackageViewModel package, CancellationToken cancellationToken)
        {
            // Load existing effect log
            try
            {
                // Connect to effect compiler server
                await routerLaunchedTask;
                var effectCompilerServerSocket = await RouterClient.RequestServer($"/service/Xenko.EffectCompilerServer/{XenkoVersion.NuGetVersion}/Xenko.EffectCompilerServer.exe?mode=gamestudio&packagename={package.Package.Meta.Name}");

                // Cancellation by closing the socket handle
                cancellationToken.Register(effectCompilerServerSocket.Dispose);

                var effectCompilerMessageLayer = new SocketMessageLayer(effectCompilerServerSocket, false);

                // Load existing asset
                dispatcher.Invoke(() => CheckEffectLogAsset(package));

                effectCompilerMessageLayer.AddPacketHandler<RemoteEffectCompilerEffectRequested>(packet => HandleEffectCompilerRequestedPacket(packet, package));

                // Run main socket loop
                Task.Run(() => effectCompilerMessageLayer.MessageLoop());
            }
            catch
            {
                // TODO: Log error
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;

                // Untrack packages
                session.LocalPackages.CollectionChanged -= LocalPackages_CollectionChanged;
                UntrackPackages(trackedPackages.Select(x => x.Package));

                // Remove import effect log command
                session.ImportEffectLogCommand = null;
            }
        }

        private void LocalPackages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                TrackPackages(e.NewItems.OfType<PackageViewModel>());
            }

            if (e.OldItems != null)
            {
                UntrackPackages(e.OldItems.OfType<PackageViewModel>());
            }
        }

        private void TrackPackages(IEnumerable<PackageViewModel> packages)
        {
            foreach (var package in packages)
            {
                var trackedPackage = new TrackedPackage(package);
                trackedPackages.Add(trackedPackage);

                Task.Run(() => Start(package, trackedPackage.CancellationToken));
            }
        }

        private void UntrackPackages(IEnumerable<PackageViewModel> packages)
        {
            var packagesCopy = packages.ToList();

            trackedPackages.RemoveAll(trackedPackage =>
            {
                if (packagesCopy.Contains(trackedPackage.Package))
                {
                    trackedPackage.Dispose();
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        /// Checks if the effect log asset changed since last check.
        /// </summary>
        private void CheckEffectLogAsset(PackageViewModel package)
        {
            var newEffectLogViewModel = package.Assets.FirstOrDefault(x => x.Name == EffectLogAsset.DefaultFile) as EffectLogViewModel;
            var newEffectLogText = newEffectLogViewModel?.Text;

            if (newEffectLogText != effectLogText // Asset changed?
                || effectLogStore == null) // First run?
            {
                effectLogText = newEffectLogText;
                effectLogViewModel = newEffectLogViewModel;

                // Asset changed, update with new data
                effectLogStream = new MemoryStream();

                if (effectLogText != null)
                {
                    var effectLogData = Encoding.UTF8.GetBytes(effectLogText);
                    effectLogStream.Write(effectLogData, 0, effectLogData.Length);
                    effectLogStream.Position = 0;
                }

                effectLogStore = new EffectLogStore(effectLogStream);
                effectLogStore.LoadNewValues();

                // Update pending effects count (against new asset)
                int importEffectLogPendingCount = 0;
                foreach (var effectCompilerResult in pendingEffects)
                {
                    if (!effectLogStore.Contains(effectCompilerResult))
                    {
                        importEffectLogPendingCount++;
                    }
                }

                UpdateImportEffectLogPendingCount(importEffectLogPendingCount);
            }
        }

        /// <summary>
        /// Imports the effect log as an asset.
        /// </summary>
        private void ImportEffectLog(PackageViewModel package)
        {
            using (var transaction = session.UndoRedoService.CreateTransaction())
            {
                CheckEffectLogAsset(package);

                // Create asset (on first time)
                if (effectLogViewModel == null)
                {
                    var effectLogAsset = new EffectLogAsset();
                    var assetItem = new AssetItem(EffectLogAsset.DefaultFile, effectLogAsset);

                    // Add created asset to project
                    effectLogViewModel = (EffectLogViewModel)package.CreateAsset(package.AssetMountPoint, assetItem, true, null);

                    CheckEffectLogAsset(package);
                }

                // Import shaders
                foreach (var effectCompilerResult in pendingEffects)
                {
                    if (!effectLogStore.Contains(effectCompilerResult))
                        effectLogStore[effectCompilerResult] = true;
                }

                // Reset current list of shaders to import
                var oldPendingEffects = pendingEffects;
                session.UndoRedoService.PushOperation(new AnonymousDirtyingOperation(Enumerable.Empty<IDirtiable>(),
                    () => { pendingEffects = oldPendingEffects; session.ImportEffectLogPendingCount = oldPendingEffects.Count; },
                    () => { pendingEffects = new HashSet<EffectCompileRequest>(); session.ImportEffectLogPendingCount = 0; }));

                pendingEffects = new HashSet<EffectCompileRequest>();
                session.ImportEffectLogPendingCount = 0;

                // Extract current asset data
                var effectLogData = effectLogStream.ToArray();

                // Update asset
                effectLogViewModel.Text = Encoding.UTF8.GetString(effectLogData, 0, effectLogData.Length);
                effectLogText = effectLogViewModel.Text;

                // Select current asset
                session.ActiveAssetView.SelectAssets(new[] { effectLogViewModel });

                session.UndoRedoService.SetName(transaction, "Import effect log");
            }
        }

        private void HandleEffectCompilerRequestedPacket(RemoteEffectCompilerEffectRequested packet, PackageViewModel package)
        {
            // Received a shader requested notification, add it to list of "pending shaders", and update count in UI

            dispatcher.InvokeAsync(() =>
            {
                CheckEffectLogAsset(package);

                // Try to decode request
                try
                {
                    // Deserialize as an object
                    var binaryReader = new BinarySerializationReader(new MemoryStream(packet.Request));
                    EffectCompileRequest effectCompileRequest = null;
                    binaryReader.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
                    binaryReader.SerializeExtended(ref effectCompileRequest, ArchiveMode.Deserialize, null);

                    // Record in list of pending effects and check if it would result in a new shader
                    // (it is still recorded in list of pending effect, in case EffectLog asset is deleted in the meantime)
                    if (pendingEffects.Add(effectCompileRequest) && !effectLogStore.Contains(effectCompileRequest))
                    {
                        UpdateImportEffectLogPendingCount(session.ImportEffectLogPendingCount + 1);
                    }
                }
                catch
                {
                    // TODO Log error
                    //Log.Warning("Received an effect compilation request which could not be decoded. Make sure Windows project compiled successfully and is up to date.");
                }
            });
        }

        private void UpdateImportEffectLogPendingCount(int importEffectLogPendingCount)
        {
            bool displayNotification = session.ImportEffectLogPendingCount == 0 && importEffectLogPendingCount > 0;
            session.ImportEffectLogPendingCount = importEffectLogPendingCount;
            if (displayNotification)
            {
                var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
                dialogService.ShowNotificationWindow("New effects to import", "New effects have been compiled by the game runtime and can be imported in the effect library. Click here to import them.", session.ImportEffectLogCommand, null);
            }
        }
    }
}
