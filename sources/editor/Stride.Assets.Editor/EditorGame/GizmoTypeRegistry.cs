// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Reflection;
using Stride.Engine.Gizmos;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The gizmo type of each entity component type, from every registered asset assembly. Both editors share it: the default
/// assets plugin of each editor registers the asset assemblies into it.
/// </summary>
public static class GizmoTypeRegistry
{
    // Replaced, never changed: the editor games read it on their threads while an assembly registers
    private static volatile IReadOnlyDictionary<Type, Type> gizmoTypes = new Dictionary<Type, Type>();
    private static readonly object GizmoTypesLock = new();

    /// <summary>
    /// Entity component type to gizmo type, from every registered asset assembly.
    /// </summary>
    public static IReadOnlyDictionary<Type, Type> GizmoTypeDictionary => gizmoTypes;

    /// <summary>
    /// Adds the gizmos that <paramref name="assembly"/> declares, found through its source-generated scan index. The last
    /// registered type wins: two assemblies may declare one for the same component, and a reload can register the new
    /// copy before the old one goes.
    /// </summary>
    public static void RegisterAssembly(Assembly assembly)
    {
        var types = GetGizmoTypes(assembly);
        lock (GizmoTypesLock)
        {
            var result = new Dictionary<Type, Type>(gizmoTypes);
            foreach (var (componentType, gizmoType) in types)
                result[componentType] = gizmoType;
            gizmoTypes = result;
        }
    }

    /// <summary>
    /// Removes the gizmos that <paramref name="assembly"/> declares. A component that another assembly took since keeps
    /// that assembly's gizmo.
    /// </summary>
    public static void UnregisterAssembly(Assembly assembly)
    {
        var types = GetGizmoTypes(assembly);
        lock (GizmoTypesLock)
        {
            var result = new Dictionary<Type, Type>(gizmoTypes);
            foreach (var (componentType, gizmoType) in types)
            {
                if (result.TryGetValue(componentType, out var current) && current == gizmoType)
                    result.Remove(componentType);
            }
            gizmoTypes = result;
        }
    }

    private static List<KeyValuePair<Type, Type>> GetGizmoTypes(Assembly assembly)
    {
        var types = new List<KeyValuePair<Type, Type>>();
        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(GizmoComponentAttribute)))
        {
            if (type.IsAssignableTo(typeof(IGizmo)) && type.GetCustomAttribute<GizmoComponentAttribute>(true) is { } attribute)
                types.Add(KeyValuePair.Create(attribute.ComponentType, type));
        }
        return types;
    }
}
