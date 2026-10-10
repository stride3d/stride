// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Thumbnails;

namespace Stride.Editor.Annotations;

/// <summary>
/// Declares a fixed thumbnail image for <see cref="AssetType"/>, an embedded resource of the attributed assembly.
/// <see cref="ResourceName"/> is the full manifest resource name or its end, such as <c>VideoThumbnail.png</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class StaticThumbnailAttribute : Attribute, IAssetCompilerDeclaration
{
    public StaticThumbnailAttribute(Type assetType, string resourceName)
    {
        AssetType = assetType;
        ResourceName = resourceName;
    }

    public Type AssetType { get; }

    public string ResourceName { get; }

    Type IAssetCompilerDeclaration.CompilationContext => typeof(ThumbnailCompilationContext);

    IAssetCompiler IAssetCompilerDeclaration.CreateCompiler(Assembly assembly)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = resourceNames.FirstOrDefault(x => x == ResourceName)
            ?? resourceNames.FirstOrDefault(x => x.EndsWith("." + ResourceName, StringComparison.Ordinal))
            ?? throw new FileNotFoundException($"Embedded resource [{ResourceName}] not found in [{assembly.GetName().Name}]");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return (IAssetCompiler)Activator.CreateInstance(typeof(StaticThumbnailCompiler<>).MakeGenericType(AssetType), memory.ToArray())!;
    }
}
