// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Serializers;

public interface IAssetSerializerFactory
{
    IAssetSerializer? TryCreate(string assetFileExtension);
}

public interface IAssetSerializer
{
    /// <param name="expectedType">
    /// The type the caller expects (<see cref="Asset"/>, <see cref="Package"/>). The file's own type tag wins; this
    /// is what an asset whose type is not loaded falls back to, so it can be read as an unloadable one.
    /// </param>
    object Load(Stream stream, UFile filePath, ILogger? log, bool clearBrokenObjectReferences, out bool aliasOccurred, out AttachedYamlAssetMetadata yamlMetadata, string? assetNamespace = null, Type? expectedType = null);

    void Save(Stream stream, object asset, AttachedYamlAssetMetadata? yamlMetadata, ILogger? log = null, string? assetNamespace = null);
}
