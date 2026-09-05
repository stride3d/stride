// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Media.Imaging;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The images the registered assemblies (runtime, Assets or Editor packages) declare with
/// <see cref="TypeImageAttribute"/>, read from their embedded resources. An assembly that unregisters takes its
/// images with it.
/// </summary>
public static class DeclaredImages
{
    private static readonly Lock ImagesLock = new();
    private static readonly Dictionary<Type, (Assembly Assembly, BitmapImage Image)> TypeImages = [];
    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(DeclaredImages));

    static DeclaredImages()
    {
        AssemblyRegistry.AssemblyRegistered += (_, e) => Register(e.Assembly);
        AssemblyRegistry.AssemblyUnregistered += (_, e) => Unregister(e.Assembly);
        foreach (var assembly in AssemblyRegistry.FindAll())
            Register(assembly);
    }

    /// <summary>
    /// The image declared for <paramref name="type"/>, or null.
    /// </summary>
    public static object? GetTypeImage(Type type)
    {
        lock (ImagesLock)
            return TypeImages.TryGetValue(type, out var image) ? image.Image : null;
    }

    private static void Register(Assembly assembly)
    {
        List<TypeImageAttribute> attributes;
        try
        {
            attributes = assembly.GetCustomAttributes<TypeImageAttribute>().ToList();
        }
        catch (Exception e)
        {
            Log.Warning($"Could not read the type images of [{assembly.GetName().Name}]", e);
            return;
        }

        foreach (var attribute in attributes)
        {
            if (LoadImage(assembly, attribute.ResourceName) is not { } image)
            {
                Log.Warning($"The type image [{attribute.ResourceName}] of [{attribute.Type.Name}] is not an embedded resource of [{assembly.GetName().Name}].");
                continue;
            }

            lock (ImagesLock)
                TypeImages[attribute.Type] = (assembly, image);
        }
    }

    private static void Unregister(Assembly assembly)
    {
        lock (ImagesLock)
        {
            foreach (var type in TypeImages.Where(x => x.Value.Assembly == assembly).Select(x => x.Key).ToList())
                TypeImages.Remove(type);
        }
    }

    private static BitmapImage? LoadImage(Assembly assembly, string resourceName)
    {
        if (DeclaredResources.Read(assembly, resourceName) is not { } data)
            return null;

        // Frozen: created on the loader thread, used on the UI thread
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(data);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
