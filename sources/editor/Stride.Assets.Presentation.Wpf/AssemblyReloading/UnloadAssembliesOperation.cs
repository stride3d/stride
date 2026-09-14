// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
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

        public UnloadAssembliesOperation(AssemblyContainer assemblyContainer, Package package, PackageLoadedAssembly loadedAssembly, IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            this.assemblyContainer = assemblyContainer;
            this.package = package;
            this.loadedAssembly = loadedAssembly;
        }

        public void Execute(ILogger log)
        {
            ReloadAssembliesOperation.UnloadAssembly(log, assemblyContainer, loadedAssembly);
            package.LoadedAssemblies.Remove(loadedAssembly);
        }

        protected override void FreezeContent()
        {
            assemblyContainer = null;
            package = null;
            loadedAssembly = null;
        }

        protected override void Undo()
        {
            var assembly = assemblyContainer.LoadAssemblyFromPath(loadedAssembly.Path);
            if (assembly == null)
                return;

            loadedAssembly.Assembly = assembly;
            AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
            DataSerializerFactory.RegisterSerializationAssembly(assembly);
            package.LoadedAssemblies.Add(loadedAssembly);
        }

        protected override void Redo()
        {
            Execute(null);
        }
    }
}
