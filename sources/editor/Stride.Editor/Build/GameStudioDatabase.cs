// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Assets;
using Stride.Editor.Preview;
using Stride.Graphics;

namespace Stride.Editor.Build
{
    public class GameStudioDatabase : IDisposable
    {
        private readonly Dictionary<ObjectUrl, OutputObject> database = new Dictionary<ObjectUrl, OutputObject>();
        private readonly HashSet<string> pendingReplacementTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object assetReplacementsLock = new object();
        private Dictionary<string, AssetItem> cachedAssetReplacements;
        private bool assetReplacementsValid;
        private readonly GameStudioBuilderService assetBuilderService;
        private readonly GameSettingsProviderService settingsProvider;
        internal readonly AssetCompilerContext CompilerContext = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
        private readonly MicroThreadLock databaseLock = new MicroThreadLock();
        private readonly GameSettingsAsset databaseGameSettings;
        internal readonly AssetDependenciesCompiler AssetDependenciesCompiler = new AssetDependenciesCompiler(typeof(EditorGameCompilationContext));
        private bool isDisposed;

        public GameStudioDatabase(GameStudioBuilderService assetBuilderService, GameSettingsProviderService settingsProvider)
        {
            this.assetBuilderService = assetBuilderService;
            this.settingsProvider = settingsProvider;

            CompilerContext.Platform = PlatformType.Windows;

            databaseGameSettings = GameSettingsFactory.Create();
            //// TODO: get the best available between 10 and 11
            //databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0;
            CompilerContext.SetGameSettingsAsset(databaseGameSettings);
            CompilerContext.CompilationContext = typeof(EditorGameCompilationContext);

            UpdateGameSettings(settingsProvider.CurrentGameSettings);
            settingsProvider.GameSettingsChanged += OnGameSettingsChanged;

            ((AssetDependencyManager)assetBuilderService.SessionViewModel.DependencyManager).AssetChanged += OnAssetChanged;
            assetBuilderService.SessionViewModel.AssetReplacementsChanged += OnAssetReplacementsChanged;
        }

        private void OnAssetChanged(AssetItem sender, bool oldValue, bool newValue)
        {
            //if (newValue)
            //{
            //    AssetDependenciesCompiler.BuildDependencyManager.AssetChanged(sender);
            //}
        }

        private void OnAssetReplacementsChanged(object sender, EventArgs e)
        {
            lock (assetReplacementsLock)
                assetReplacementsValid = false;
        }

        public void Dispose()
        {
            isDisposed = true;
            databaseLock.Dispose();
            settingsProvider.GameSettingsChanged -= OnGameSettingsChanged;
            assetBuilderService.SessionViewModel.AssetReplacementsChanged -= OnAssetReplacementsChanged;
            ((AssetDependencyManager)assetBuilderService.SessionViewModel.DependencyManager).AssetChanged -= OnAssetChanged;
            CompilerContext.Dispose();
            database.Clear();
        }

        public ILogger Logger { get; }

        public Task<ISyncLockable> ReserveSyncLock() => databaseLock.ReserveSyncLock();

        public Task<IDisposable> LockAsync() => databaseLock.LockAsync();

        public async Task<IDisposable> MountInCurrentMicroThread()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(GameStudioDatabase));
            if (Scheduler.CurrentMicroThread == null) throw new InvalidOperationException("The database can only be mounted in a micro-thread.");

            var lockObject = await databaseLock.LockAsync();
            // Return immediately if the database was disposed when waiting for the lock
            if (isDisposed)
                return lockObject;

            MicrothreadLocalDatabases.MountDatabase(database.Yield());
            return lockObject;
        }

        public Task Build(AssetItem asset, BuildDependencyType dependencyType = BuildDependencyType.Runtime)
        {
            return Build(asset, GetAssetReplacements(asset.Package?.Session));
        }

        private Dictionary<string, AssetItem> GetAssetReplacements(PackageSession session)
        {
            lock (assetReplacementsLock)
            {
                if (!assetReplacementsValid)
                {
                    cachedAssetReplacements = AssetReplacementAnalysis.CollectSessionReplacements(session);
                    assetReplacementsValid = true;
                }
                return cachedAssetReplacements;
            }
        }

        private async Task Build(AssetItem asset, Dictionary<string, AssetItem> assetReplacements)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(GameStudioDatabase));

            var buildUnit = new EditorGameBuildUnit(asset, CompilerContext, AssetDependenciesCompiler);

            try
            {
                assetBuilderService.PushBuildUnit(buildUnit);
                await buildUnit.Wait();
            }
            catch (Exception e)
            {
                Logger?.Error($"An error occurred while building the scene: {e.Message}", e);
                return;
            }

            // Merge build result into the database
            using ((await databaseLock.ReserveSyncLock()).Lock())
            {
                if (isDisposed)
                    return;

                if (buildUnit.Failed)
                {
                    // Build failed => unregister object
                    // 1. If it is first-time scene loading and one of sub-asset failed, it will be in this state, but we don't care
                    // since database will be empty at that point (it won't have any effect)
                    // 2. The second case (the one we actually care about) happens when reloading a recently rebuilt individual asset (i.e. material or texture),
                    // this will actually remove it from database
                    database.Remove(new ObjectUrl(UrlType.Content, asset.Location));
                }

                foreach (var outputObject in buildUnit.OutputObjects)
                {
                    database[outputObject.Key] = outputObject.Value;
                }
            }

            await ApplyAssetReplacements(buildUnit, assetReplacements);
        }

        /// <summary>
        /// Redirects the database entries of replaced URLs found among the build outputs to their
        /// replacement's compiled content, so the editor game resolves them like the built game does.
        /// </summary>
        private async Task ApplyAssetReplacements(EditorGameBuildUnit buildUnit, Dictionary<string, AssetItem> assetReplacements)
        {
            if (assetReplacements == null || buildUnit.Failed)
                return;

            List<(ObjectUrl Target, AssetItem Replacement)> toApply = null;
            foreach (var output in buildUnit.OutputObjects)
            {
                if (output.Key.Type == UrlType.Content
                    && assetReplacements.TryGetValue(output.Key.Path, out var replacement)
                    && !string.Equals(replacement.Location.FullPath, output.Key.Path, StringComparison.OrdinalIgnoreCase))
                {
                    (toApply ??= new List<(ObjectUrl, AssetItem)>()).Add((output.Key, replacement));
                }
            }
            if (toApply == null)
                return;

            foreach (var (target, replacement) in toApply)
            {
                // Reentrancy guard on the build only: the replacement's own dependency graph can
                // include the replaced URL. The redirect is always (re)applied, so a concurrent
                // build merging the original content back cannot leave the target un-redirected.
                bool buildReplacement;
                lock (pendingReplacementTargets)
                    buildReplacement = pendingReplacementTargets.Add(target.Path);
                try
                {
                    if (buildReplacement)
                        await Build(replacement, assetReplacements);

                    using ((await databaseLock.ReserveSyncLock()).Lock())
                    {
                        if (isDisposed)
                            return;

                        if (database.TryGetValue(new ObjectUrl(UrlType.Content, replacement.Location), out var replacementOutput))
                            database[target] = replacementOutput;
                    }
                }
                finally
                {
                    if (buildReplacement)
                    {
                        lock (pendingReplacementTargets)
                            pendingReplacementTargets.Remove(target.Path);
                    }
                }
            }
        }

        private void OnGameSettingsChanged(object sender, GameSettingsChangedEventArgs e)
        {
            UpdateGameSettings(e.GameSettings);
        }

        private void UpdateGameSettings(GameSettingsAsset currentGameSettings)
        {
            databaseGameSettings.GetOrCreate<EditorSettings>().RenderingMode = currentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
            databaseGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = currentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
            databaseGameSettings.GetOrCreate<Navigation.NavigationSettings>().Groups = currentGameSettings.GetOrDefault<Navigation.NavigationSettings>().Groups;
            databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = currentGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile;
        }
    }
}
