// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Marks a default plugin of an editor: the editor host registers it itself (<see cref="AssetsPlugin.RegisterPlugin"/>), and
/// the plugin discovery (<see cref="AssetsPlugin.DiscoverPlugins"/>, <see cref="AssetsPlugin.RegisterPlugins"/>) skips it.
/// </summary>
/// <remarks>
/// A shared assembly can then hold the default plugins of one editor and be loaded by another editor that has its own.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditorDefaultPluginAttribute : Attribute
{
}
