// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Assets.Presentation.AssemblyReloading
{
    /// <summary>
    /// What an unloaded assembly is missing to work again: its place in the container it was loaded by, and the
    /// categories it was registered with.
    /// </summary>
    public class UnloadedAssembly
    {
        public UnloadedAssembly(LoadedAssembly containerAssembly, IReadOnlyCollection<string> categories)
        {
            ContainerAssembly = containerAssembly;
            Categories = categories;
        }

        /// <summary>What the container knew about the assembly, null when no container held it.</summary>
        public LoadedAssembly ContainerAssembly { get; }

        public IReadOnlyCollection<string> Categories { get; }
    }

    public class ReloadAssembliesOperation : DirtyingOperation
    {
        private class ReloadedAssembly
        {
            public readonly PackageLoadedAssembly PackageLoadedAssembly;
            public readonly string OldAssemblyPath;
            public readonly string NewAssemblyPath;
            public readonly Assembly OriginalAssembly;
            public Assembly NewAssembly;
            // What was taken away from each version, to give it back to the one being loaded again
            public UnloadedAssembly OriginalUnloaded;
            public UnloadedAssembly NewUnloaded;

            public ReloadedAssembly(PackageLoadedAssembly packageLoadedAssembly, string newAssemblyPath)
            {
                PackageLoadedAssembly = packageLoadedAssembly;
                OldAssemblyPath = PackageLoadedAssembly.Path;
                NewAssemblyPath = newAssemblyPath;
                OriginalAssembly = PackageLoadedAssembly.Assembly;
            }
        }

        private AssemblyContainer assemblyContainer;
        private List<ReloadedAssembly> loadedAssemblies;

        public ReloadAssembliesOperation(AssemblyContainer assemblyContainer, Dictionary<PackageLoadedAssembly, string> loadedAssemblies, IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            this.assemblyContainer = assemblyContainer;
            this.loadedAssemblies = loadedAssemblies.Select(x => new ReloadedAssembly(x.Key, x.Value)).ToList();
        }

        public void Execute(ILogger log)
        {
            // Unload old assemblies and load new ones
            UnloadAssemblies(log, assemblyContainer, loadedAssemblies);
            loadedAssemblies.ForEach(x => x.PackageLoadedAssembly.Path = x.NewAssemblyPath);
            LoadAssemblies(log, assemblyContainer, loadedAssemblies, true, true);
        }

        protected override void FreezeContent()
        {
            assemblyContainer = null;
            loadedAssemblies = null;
        }

        protected override void Undo()
        {
            UnloadAssemblies(null, assemblyContainer, loadedAssemblies);
            LoadAssemblies(null, assemblyContainer, loadedAssemblies, false, false);
        }

        protected override void Redo()
        {
            UnloadAssemblies(null, assemblyContainer, loadedAssemblies);
            LoadAssemblies(null, assemblyContainer, loadedAssemblies, true, false);
        }

        private static void LoadAssemblies(ILogger log, AssemblyContainer assemblyContainer, List<ReloadedAssembly> loadedAssemblies, bool newVersion, bool firstTime)
        {
            foreach (var loadedAssembly in loadedAssemblies)
            {
                loadedAssembly.PackageLoadedAssembly.Path = newVersion ? loadedAssembly.NewAssemblyPath : loadedAssembly.OldAssemblyPath;
                Assembly assembly = null;
                try
                {
                    // If first time, load assembly
                    if (firstTime)
                        loadedAssembly.NewAssembly = assemblyContainer.LoadAssemblyFromPath(loadedAssembly.PackageLoadedAssembly.Path, log);

                    // Load assembly
                    assembly = newVersion
                        ? loadedAssembly.NewAssembly
                        : loadedAssembly.OriginalAssembly;

                    var unloaded = newVersion ? loadedAssembly.NewUnloaded : loadedAssembly.OriginalUnloaded;

                    // The container resolves what an assembly references through the assembly that asks, so an
                    // assembly loaded again belongs to it again
                    if (!firstTime && unloaded?.ContainerAssembly != null)
                        assemblyContainer.RestoreAssembly(unloaded.ContainerAssembly);

                    log?.Info($"Loading assembly {assembly}");

                    loadedAssembly.PackageLoadedAssembly.Assembly = assembly;

                    // An assembly loaded again takes back the categories it had: its module initializer registered
                    // some of them (Engine, from the assembly processor) and runs once only. A freshly loaded one
                    // has just run it, and is an asset assembly of this session on top.
                    var categories = unloaded != null && unloaded.Categories.Count > 0
                        ? unloaded.Categories
                        : (IReadOnlyCollection<string>)new[] { AssemblyCommonCategories.Assets };
                    AssemblyRegistry.Register(assembly, categories);

                    DataSerializerFactory.RegisterSerializationAssembly(assembly);
                }
                catch (Exception e)
                {
                    log?.Error($"Error loading assembly {assembly?.ToString() ?? Path.GetFileNameWithoutExtension(loadedAssembly.PackageLoadedAssembly.Path)}: ", e);
                }
            }
        }

        private static void UnloadAssemblies(ILogger log, AssemblyContainer assemblyContainer, List<ReloadedAssembly> loadedAssemblies)
        {
            for (int index = loadedAssemblies.Count - 1; index >= 0; index--)
            {
                var loadedAssembly = loadedAssemblies[index];
                var assembly = loadedAssembly.PackageLoadedAssembly.Assembly;
                var unloaded = UnloadAssembly(log, assemblyContainer, loadedAssembly.PackageLoadedAssembly);
                if (unloaded == null)
                    continue;

                if (assembly == loadedAssembly.OriginalAssembly)
                    loadedAssembly.OriginalUnloaded = unloaded;
                else
                    loadedAssembly.NewUnloaded = unloaded;
            }
        }

        /// <summary>
        /// Unloads and unregisters a single loaded assembly (no-op when not loaded). Returns what was taken away from
        /// it, which loading it again gives back.
        /// </summary>
        public static UnloadedAssembly UnloadAssembly(ILogger log, AssemblyContainer assemblyContainer, PackageLoadedAssembly loadedAssembly)
        {
            var assembly = loadedAssembly.Assembly;

            // Already unloaded or never loaded?
            if (assembly == null)
                return null;

            log?.Info($"Unloading assembly {assembly}");

            // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
            var categories = AssemblyRegistry.GetCategories(assembly);
            AssemblyRegistry.Unregister(assembly);

            // Unload binary serialization
            DataSerializerFactory.UnregisterSerializationAssembly(assembly);

            // Unload assembly
            var containerAssembly = assemblyContainer.RemoveAssembly(assembly);

            loadedAssembly.Assembly = null;
            return new UnloadedAssembly(containerAssembly, categories);
        }
    }
}
