// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Stride.Assets.Presentation;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// Another editor (the WPF Game Studio) loads the shared assemblies too: loading them must not register the default plugins of
/// the cross-platform editor, which its host registers.
/// </summary>
public sealed class DefaultPluginLoadingTests
{
    /// <summary>
    /// Loads the editor assemblies of the test output again, apart from the ones this process uses, so that their plugin
    /// registry starts empty.
    /// </summary>
    private sealed class IsolatedContext(string directory) : AssemblyLoadContext("Isolated editor assemblies", isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = Path.Combine(directory, assemblyName.Name + ".dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }
    }

    [Fact]
    public void LoadingTheSharedAssembliesRegistersNoDefaultPlugin()
    {
        var context = new IsolatedContext(Path.GetDirectoryName(typeof(StrideEditorPlugin).Assembly.Location)!);
        try
        {
            var presentation = context.LoadFromAssemblyName(typeof(EntityComponentMetadata).Assembly.GetName());
            var editor = context.LoadFromAssemblyName(typeof(StrideEditorPlugin).Assembly.GetName());
            RuntimeHelpers.RunModuleConstructor(presentation.ManifestModule.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(editor.ManifestModule.ModuleHandle);

            // The plugin registry of the isolated copy, which only these module initializers could fill
            var plugins = context.LoadFromAssemblyName(typeof(AssetsPlugin).Assembly.GetName()).GetType(typeof(AssetsPlugin).FullName!, throwOnError: true)!;
            Assert.NotSame(typeof(AssetsPlugin), plugins);
            var registered = Names((IEnumerable<object>)plugins.GetProperty(nameof(AssetsPlugin.RegisteredPlugins))!.GetValue(null)!);
            Assert.DoesNotContain(typeof(StrideDefaultAssetsPlugin).FullName, registered);
            Assert.DoesNotContain(typeof(StrideEditorPlugin).FullName, registered);

            // The discovery of these assemblies, which the WPF editor runs on every assets assembly, skips them too
            var registerPlugins = plugins.GetMethod(nameof(AssetsPlugin.RegisterPlugins))!;
            var discovered = new[] { presentation, editor }.SelectMany(x => Names((IEnumerable<object>)registerPlugins.Invoke(null, [x])!)).ToList();
            Assert.DoesNotContain(typeof(StrideDefaultAssetsPlugin).FullName, discovered);
            Assert.DoesNotContain(typeof(StrideEditorPlugin).FullName, discovered);
        }
        finally
        {
            context.Unload();
        }

        static List<string?> Names(IEnumerable<object> plugins) => plugins.Select(x => x.GetType().FullName).ToList();
    }
}
