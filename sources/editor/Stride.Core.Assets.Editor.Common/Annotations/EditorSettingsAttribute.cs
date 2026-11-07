// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Reflection;
using Stride.Core.Settings;

#nullable enable

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Declares a static class holding editor settings keys (static fields or properties): Game Studio registers them with
/// the plugin of the class's assembly and removes them when that assembly is unloaded. The assembly needs a plugin
/// class (<c>AssetsPlugin</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[AssemblyScan]
public sealed class EditorSettingsAttribute : Attribute
{
    /// <summary>
    /// The settings keys of the <see cref="EditorSettingsAttribute"/> classes of <paramref name="assembly"/>.
    /// </summary>
    public static IEnumerable<SettingsKey> GetDeclaredKeys(Assembly assembly)
        => AssemblyRegistry.GetScanTypes(assembly, typeof(EditorSettingsAttribute)).SelectMany(GetKeys);

    /// <summary>
    /// The settings keys held by the static fields and properties of <paramref name="type"/>.
    /// </summary>
    public static IEnumerable<SettingsKey> GetKeys(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var fields = type.GetFields(flags).Where(x => typeof(SettingsKey).IsAssignableFrom(x.FieldType)).Select(x => x.GetValue(null));
        var properties = type.GetProperties(flags).Where(x => typeof(SettingsKey).IsAssignableFrom(x.PropertyType) && x.GetIndexParameters().Length == 0).Select(x => x.GetValue(null));
        // An auto-property's backing field holds the same key
        return fields.Concat(properties).OfType<SettingsKey>().Distinct();
    }
}
