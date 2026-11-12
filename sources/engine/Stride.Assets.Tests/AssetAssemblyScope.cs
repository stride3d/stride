// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Reflection;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Registers an assembly as an asset assembly, as loading its package does, and unregisters it at the end unless it was
    /// registered already.
    /// </summary>
    internal sealed class AssetAssemblyScope : IDisposable
    {
        private readonly Assembly assembly;
        private readonly bool registered;

        private AssetAssemblyScope(Assembly assembly, bool registered)
        {
            this.assembly = assembly;
            this.registered = registered;
        }

        public static AssetAssemblyScope Register(Assembly assembly)
        {
            // Its module initializer registers its scan index
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
            if (AssemblyRegistry.Find(AssemblyCommonCategories.Assets).Contains(assembly))
                return new AssetAssemblyScope(assembly, registered: false);

            AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
            return new AssetAssemblyScope(assembly, registered: true);
        }

        /// <summary>
        /// Whether an Editor package of the engine's plugins is loaded in the process.
        /// </summary>
        public static bool AnyEditorPackageLoaded()
        {
            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (loaded.GetName().Name is { } name && name.StartsWith("Stride.", StringComparison.Ordinal) && name.EndsWith(".Editor", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (registered)
                AssemblyRegistry.Unregister(assembly);
        }
    }
}
