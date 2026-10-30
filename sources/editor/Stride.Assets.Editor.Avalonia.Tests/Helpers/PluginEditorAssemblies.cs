// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Audio.Editor;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Reflection;
using Stride.Particles.Editor;
using Stride.Physics.Editor;
using Stride.SpriteStudio.Editor;
using Stride.Voxels.Editor;

namespace Stride.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// The assemblies of the toolkit-neutral Editor packages of the plugins, registered as a session that loads their packages does.
/// </summary>
internal static class PluginEditorAssemblies
{
    private static readonly Lock RegisterLock = new();
    private static bool registered;

    public static Assembly[] All { get; } =
    [
        typeof(AudioEditorPlugin).Assembly,
        typeof(ParticlesEditorPlugin).Assembly,
        typeof(PhysicsEditorPlugin).Assembly,
        typeof(SpriteStudioEditorPlugin).Assembly,
        typeof(VoxelVolumeGizmo).Assembly,
    ];

    /// <summary>
    /// Registers the assemblies as asset assemblies (as <c>Package.LoadAssemblies</c> does) and lets the editor plugin follow
    /// them (as it does from the first session on); once per process.
    /// </summary>
    public static void Register()
    {
        lock (RegisterLock)
        {
            if (registered)
                return;
            registered = true;

            PluginRegistrations.Of(new StrideEditorPlugin(), typeof(IAssetEditorView));
            foreach (var assembly in All)
                AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
        }
    }
}
