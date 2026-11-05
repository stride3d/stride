// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Assets.Presentation.AssemblyReloading
{
    /// <summary>
    /// Unloads a project assembly the editor should no longer load (loadability flip), without reloading it.
    /// </summary>
    public class UnloadAssembliesOperation : DirtyingOperation
    {
        private AssemblyContainer assemblyContainer;
        private Package package;
        private PackageLoadedAssembly loadedAssembly;
        // The undo puts back instances of this copy's types: it loads this copy again, not a new one from the file
        private Assembly unloadedAssembly;
        private UnloadedAssembly unloaded;

        public UnloadAssembliesOperation(AssemblyContainer assemblyContainer, Package package, PackageLoadedAssembly loadedAssembly, IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            this.assemblyContainer = assemblyContainer;
            this.package = package;
            this.loadedAssembly = loadedAssembly;
        }

        public void Execute(ILogger log)
        {
            unloadedAssembly = loadedAssembly.Assembly;
            unloaded = ReloadAssembliesOperation.UnloadAssembly(log, assemblyContainer, loadedAssembly);
            package.LoadedAssemblies.Remove(loadedAssembly);
        }

        protected override void FreezeContent()
        {
            assemblyContainer = null;
            package = null;
            loadedAssembly = null;
            unloadedAssembly = null;
            unloaded = null;
        }

        protected override void Undo()
        {
            if (unloadedAssembly == null || unloaded == null)
                return;

            // The container resolves what an assembly references through the assembly that asks
            if (unloaded.ContainerAssembly != null)
                assemblyContainer.RestoreAssembly(unloaded.ContainerAssembly);

            loadedAssembly.Assembly = unloadedAssembly;
            // Its module initializer registered some categories and runs once only: it takes back the ones it had
            AssemblyRegistry.Register(unloadedAssembly, unloaded.Categories.Count > 0 ? unloaded.Categories : new[] { AssemblyCommonCategories.Assets });
            DataSerializerFactory.RegisterSerializationAssembly(unloadedAssembly);
            package.LoadedAssemblies.Add(loadedAssembly);
        }

        protected override void Redo()
        {
            Execute(null);
        }
    }
}
