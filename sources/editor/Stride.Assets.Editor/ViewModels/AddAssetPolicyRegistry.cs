// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core.Reflection;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The add-asset policies of every registered asset assembly: what an entity hierarchy editor does with an asset dropped
/// on it. Both editors share it: the default assets plugin of each editor registers the asset assemblies into it.
/// </summary>
public static class AddAssetPolicyRegistry
{
    // Replaced, never changed: the editors read it while an assembly registers
    private static volatile IReadOnlyList<Type> policyTypes = [];
    private static readonly object PolicyTypesLock = new();

    /// <summary>
    /// The <see cref="IAddAssetPolicy"/> implementations of every registered asset assembly, in registration order.
    /// </summary>
    public static IReadOnlyList<Type> PolicyTypes => policyTypes;

    /// <summary>
    /// Adds the policies that <paramref name="assembly"/> declares, found through its source-generated scan index.
    /// </summary>
    public static void RegisterAssembly(Assembly assembly)
    {
        var types = AssemblyRegistry.GetScanTypes(assembly, typeof(IAddAssetPolicy)).Where(IsAddAssetPolicy).ToList();
        lock (PolicyTypesLock)
            policyTypes = [.. policyTypes, .. types.Except(policyTypes)];
    }

    /// <summary>
    /// Removes the policies that <paramref name="assembly"/> declares.
    /// </summary>
    public static void UnregisterAssembly(Assembly assembly)
    {
        var types = AssemblyRegistry.GetScanTypes(assembly, typeof(IAddAssetPolicy)).ToHashSet();
        lock (PolicyTypesLock)
            policyTypes = policyTypes.Where(x => !types.Contains(x)).ToList();
    }

    /// <summary>
    /// Creates one instance of each registered policy.
    /// </summary>
    public static IReadOnlyList<IAddAssetPolicy> CreatePolicies()
    {
        return [.. PolicyTypes.Select(x => (IAddAssetPolicy)Activator.CreateInstance(x)!)];
    }

    private static bool IsAddAssetPolicy(Type type)
    {
        return type.IsAssignableTo(typeof(IAddAssetPolicy)) && type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
