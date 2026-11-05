// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The editor game services (<see cref="EditorGameServiceAttribute"/>) and gizmos (<see cref="GizmoTypeRegistry"/>) that the
/// discovery leaves out in this process, although an asset assembly declares them.
/// </summary>
/// <remarks>
/// An editor that has its own versions of some types of the shared assemblies lists them here, before its plugins register
/// the asset assemblies (the WPF Game Studio, for the light probe and wireframe gizmos and the transformation service of the
/// cross-platform editor).
/// </remarks>
public static class EditorGameDiscovery
{
    // Replaced, never changed: the editor games read it on their threads
    private static volatile HashSet<Type> excludedTypes = new();
    private static readonly object ExcludedTypesLock = new();

    /// <summary>
    /// Whether the discovery leaves <paramref name="type"/> out.
    /// </summary>
    public static bool IsExcluded(Type type) => excludedTypes.Contains(type);

    /// <summary>
    /// Leaves <paramref name="types"/> out of the discovery.
    /// </summary>
    /// <returns>An object that puts them back when disposed.</returns>
    public static IDisposable Exclude(params Type[] types)
    {
        lock (ExcludedTypesLock)
        {
            var added = new List<Type>();
            var result = new HashSet<Type>(excludedTypes);
            foreach (var type in types)
            {
                if (result.Add(type))
                    added.Add(type);
            }
            excludedTypes = result;
            return new AnonymousDisposable(() => Include(added));
        }
    }

    private static void Include(List<Type> types)
    {
        lock (ExcludedTypesLock)
        {
            var result = new HashSet<Type>(excludedTypes);
            result.ExceptWith(types);
            excludedTypes = result;
        }
    }
}
