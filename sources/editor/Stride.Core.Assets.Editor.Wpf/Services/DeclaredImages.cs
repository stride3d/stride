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
using Stride.Core.Reflection;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The images the registered assemblies (runtime, Assets or Editor packages) declare with
/// <see cref="TypeImageAttribute"/> and <see cref="EnumImageAttribute"/>, read from their embedded resources. An
/// assembly that unregisters takes its images with it.
/// </summary>
public static class DeclaredImages
{
    private static readonly Lock ImagesLock = new();
    private static readonly Dictionary<Type, (Assembly Assembly, BitmapImage Image)> TypeImages = [];
    private static readonly Dictionary<object, (Assembly Assembly, BitmapImage Image)> EnumImages = [];

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

    /// <summary>
    /// The image declared for the enum value <paramref name="value"/>, or null.
    /// </summary>
    public static object? GetEnumImage(object value)
    {
        lock (ImagesLock)
            return EnumImages.TryGetValue(value, out var image) ? image.Image : null;
    }

    /// <summary>
    /// Whether an image is declared for a value of <paramref name="enumType"/>.
    /// </summary>
    public static bool HasEnumImages(Type enumType)
    {
        lock (ImagesLock)
            return EnumImages.Keys.Any(x => x.GetType() == enumType);
    }

    private static void Register(Assembly assembly)
    {
        foreach (var declared in DeclaredImageResources.Read(assembly))
        {
            var image = Decode(declared.Data);
            lock (ImagesLock)
            {
                if (declared.Target is Type type)
                    TypeImages[type] = (assembly, image);
                else
                    EnumImages[declared.Target] = (assembly, image);
            }
        }
    }

    private static void Unregister(Assembly assembly)
    {
        lock (ImagesLock)
        {
            foreach (var type in TypeImages.Where(x => x.Value.Assembly == assembly).Select(x => x.Key).ToList())
                TypeImages.Remove(type);
            foreach (var value in EnumImages.Where(x => x.Value.Assembly == assembly).Select(x => x.Key).ToList())
                EnumImages.Remove(value);
        }
    }

    /// <summary>
    /// Decodes a declared image (the content of a PNG or JPEG file) into a frozen WPF image, usable from any thread.
    /// </summary>
    public static BitmapImage Decode(byte[] data)
    {
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
