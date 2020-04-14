// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
    public class ReloadAssembliesOperation : DirtyingOperation
    {
        private class ReloadedAssembly
        {
            public readonly PackageLoadedAssembly PackageLoadedAssembly;
            public readonly string OldAssemblyPath;
            public readonly string NewAssemblyPath;
            public readonly Assembly OriginalAssembly;
            public Assembly NewAssembly;

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
                        loadedAssembly.NewAssembly = assemblyContainer.LoadAssemblyFromPath(loadedAssembly.PackageLoadedAssembly.Path);

                    // Load assembly
                    assembly = newVersion
                        ? loadedAssembly.NewAssembly
                        : loadedAssembly.OriginalAssembly;

                    log?.Info($"Loading assembly {assembly}");

                    loadedAssembly.PackageLoadedAssembly.Assembly = assembly;

                    // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
                    AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);

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

                // Already unloaded or never loaded?
                if (assembly == null)
                    continue;

                log?.Info($"Unloading assembly {assembly}");

                // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
                AssemblyRegistry.Unregister(assembly);

                // Unload binary serialization
                DataSerializerFactory.UnregisterSerializationAssembly(assembly);

                // Unload assembly
                assemblyContainer.UnloadAssembly(assembly);

                loadedAssembly.PackageLoadedAssembly.Assembly = null;
            }
        }
    }
}
