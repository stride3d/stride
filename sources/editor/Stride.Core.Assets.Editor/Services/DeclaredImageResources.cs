// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Finds the images that an assembly declares (<see cref="TypeImageAttribute"/>, <see cref="EnumImageAttribute"/>) and reads
/// them from its embedded resources. Each editor decodes them into images of its toolkit.
/// </summary>
public static class DeclaredImageResources
{
    private static readonly Logger Log = GlobalLogger.GetLogger("DeclaredImages");

    /// <summary>
    /// The images declared by <paramref name="assembly"/>; a declaration whose resource is missing, or whose enum image is
    /// not for an enum value, is reported and left out.
    /// </summary>
    public static IReadOnlyList<DeclaredImage> Read(Assembly assembly)
    {
        List<TypeImageAttribute> typeAttributes;
        List<EnumImageAttribute> enumAttributes;
        try
        {
            typeAttributes = assembly.GetCustomAttributes<TypeImageAttribute>().ToList();
            enumAttributes = assembly.GetCustomAttributes<EnumImageAttribute>().ToList();
        }
        catch (Exception e)
        {
            Log.Warning($"Could not read the images declared by [{assembly.GetName().Name}]", e);
            return [];
        }

        var images = new List<DeclaredImage>();
        foreach (var attribute in typeAttributes)
        {
            if (ReadImage(assembly, attribute.ResourceName, attribute.Type.Name) is { } data)
                images.Add(new DeclaredImage(attribute.Type, data));
        }

        foreach (var attribute in enumAttributes)
        {
            if (attribute.Value is not Enum)
            {
                Log.Warning($"The image [{attribute.ResourceName}] declared by [{assembly.GetName().Name}] is for [{attribute.Value}], which is not an enum value.");
                continue;
            }

            if (ReadImage(assembly, attribute.ResourceName, attribute.Value.ToString()!) is { } data)
                images.Add(new DeclaredImage(attribute.Value, data));
        }
        return images;
    }

    private static byte[]? ReadImage(Assembly assembly, string resourceName, string target)
    {
        if (DeclaredResources.Read(assembly, resourceName) is not { } data)
        {
            Log.Warning($"The image [{resourceName}] of [{target}] is not an embedded resource of [{assembly.GetName().Name}].");
            return null;
        }
        return data;
    }
}
