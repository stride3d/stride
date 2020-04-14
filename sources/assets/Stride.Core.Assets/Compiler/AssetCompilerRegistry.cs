// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// A registry containing the asset compilers of the assets.
    /// </summary>
    public sealed class AssetCompilerRegistry
    {
        private readonly HashSet<Assembly> assembliesToRegister = new HashSet<Assembly>();
        private readonly Logger log = GlobalLogger.GetLogger("AssetsCompiler.AttributeBasedRegistry");

        private readonly HashSet<Assembly> registeredAssemblies = new HashSet<Assembly>();
        private readonly Dictionary<CompilerTypeData, IAssetCompiler> typeToCompiler = new Dictionary<CompilerTypeData, IAssetCompiler>();
        private bool assembliesChanged;

        /// <summary>
        /// Create an instance of that registry
        /// </summary>
        public AssetCompilerRegistry()
        {
            // Statically find all assemblies related to assets and register them
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
            foreach (var assembly in assemblies)
            {
                RegisterAssembly(assembly);
            }

            AssemblyRegistry.AssemblyRegistered += AssemblyRegistered;
            AssemblyRegistry.AssemblyUnregistered += AssemblyUnregistered;
        }

        /// <summary>
        /// Gets or sets the default compiler to use when no compiler are explicitly registered for a type.
        /// </summary>
        public IAssetCompiler DefaultCompiler { get; set; }

        /// <summary>
        /// Gets the compiler associated to an <see cref="Asset"/> type.
        /// </summary>
        /// <param name="type">The type of the <see cref="Asset"/></param>
        /// <param name="context"></param>
        /// <returns>The compiler associated the provided asset type or null if no compiler exists for that type.</returns>
        public IAssetCompiler GetCompiler(Type type, Type context)
        {
            AssertAssetType(type);

            EnsureTypes();

            var typeData = new CompilerTypeData(context, type);

            IAssetCompiler compiler;
            if (!typeToCompiler.TryGetValue(typeData, out compiler))
            {
                //support nested context types!
                if (context.BaseType != typeof(object))
                {
                    return GetCompiler(type, context.BaseType);
                }

                compiler = DefaultCompiler;
            }

            return compiler;
        }

        /// <summary>
        /// Register a compiler for a given <see cref="Asset"/> type.
        /// </summary>
        /// <param name="type">The type of asset the compiler can compile</param>
        /// <param name="compiler">The compiler to use</param>
        /// <param name="context"></param>
        public void RegisterCompiler(Type type, IAssetCompiler compiler, Type context)
        {
            if (compiler == null) throw new ArgumentNullException(nameof(compiler));

            AssertAssetType(type);

            var typeData = new CompilerTypeData(context, type);

            typeToCompiler[typeData] = compiler;
        }

        private void UnregisterCompilersFromAssembly(Assembly assembly)
        {
            foreach (var typeToRemove in typeToCompiler.Where(typeAndCompile => typeAndCompile.Key.Type.Assembly == assembly || typeAndCompile.Value.GetType().Assembly == assembly).Select(e => e.Key).ToList())
            {
                typeToCompiler.Remove(typeToRemove);
            }
        }

        private static void AssertAssetType(Type assetType)
        {
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));

            if (!typeof(Asset).IsAssignableFrom(assetType))
                throw new ArgumentException("Type [{0}] must be assignable to Asset".ToFormat(assetType), nameof(assetType));
        }

        private void AssemblyRegistered(object sender, AssemblyRegisteredEventArgs e)
        {
            // Handle delay-loading assemblies
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                RegisterAssembly(e.Assembly);
        }

        private void AssemblyUnregistered(object sender, AssemblyRegisteredEventArgs e)
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                UnregisterAssembly(e.Assembly);
        }

        private void EnsureTypes()
        {
            if (assembliesChanged)
            {
                Assembly[] assembliesToRegisterCopy;
                lock (assembliesToRegister)
                {
                    assembliesToRegisterCopy = assembliesToRegister.ToArray();
                    assembliesToRegister.Clear();
                    assembliesChanged = false;
                }

                foreach (var assembly in assembliesToRegisterCopy)
                {
                    if (!registeredAssemblies.Contains(assembly))
                    {
                        RegisterCompilersFromAssembly(assembly);
                        registeredAssemblies.Add(assembly);
                    }
                }
            }
        }

        private void ProcessAttribute(AssetCompilerAttribute compilerCompilerAttribute, Type type)
        {
            if (!typeof(ICompilationContext).IsAssignableFrom(compilerCompilerAttribute.CompilationContext))
            {
                log.Error($"Invalid compiler context type [{compilerCompilerAttribute.CompilationContext}], must inherit from ICompilerContext");
                return;
            }

            var assetType = AssemblyRegistry.GetType(compilerCompilerAttribute.TypeName);
            if (assetType == null)
            {
                log.Error($"Unable to find asset [{compilerCompilerAttribute.TypeName}] for compiler [{type}]");
                return;
            }

            var compilerInstance = Activator.CreateInstance(type) as IAssetCompiler;
            if (compilerInstance == null)
            {
                log.Error($"Invalid compiler type [{type}], must inherit from IAssetCompiler");
                return;
            }

            RegisterCompiler(assetType, compilerInstance, compilerCompilerAttribute.CompilationContext);
        }

        private void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            lock (assembliesToRegister)
            {
                assembliesToRegister.Add(assembly);
                assembliesChanged = true;
            }
        }

        private void RegisterCompilersFromAssembly(Assembly assembly)
        {
            // Process Asset types.
            foreach (var type in assembly.GetTypes())
            {
                // Only process Asset types
                if (!typeof(IAssetCompiler).IsAssignableFrom(type) || !type.IsClass)
                    continue;

                // Asset compiler
                var compilerAttribute = type.GetCustomAttribute<AssetCompilerAttribute>();

                if (compilerAttribute == null) // no compiler attribute in this asset
                    continue;

                try
                {
                    ProcessAttribute(compilerAttribute, type);
                }
                catch (Exception ex)
                {
                    log.Error($"Unable to instantiate compiler [{compilerAttribute.TypeName}]", ex);
                }
            }
        }

        private void UnregisterAssembly(Assembly assembly)
        {
            registeredAssemblies.Remove(assembly);
            UnregisterCompilersFromAssembly(assembly);
            assembliesChanged = true;
        }

        private struct CompilerTypeData : IEquatable<CompilerTypeData>
        {
            public readonly Type Context;
            public readonly Type Type;

            public CompilerTypeData(Type context, Type type)
            {
                Context = context;
                Type = type;
            }

            /// <inheritdoc />
            public bool Equals(CompilerTypeData other)
            {
                return Context == other.Context && Type == other.Type;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null)) return false;
                return obj is CompilerTypeData && Equals((CompilerTypeData)obj);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Context != null ? Context.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                }
            }

            public static bool operator ==(CompilerTypeData left, CompilerTypeData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CompilerTypeData left, CompilerTypeData right)
            {
                return !left.Equals(right);
            }
        }
    }
}
