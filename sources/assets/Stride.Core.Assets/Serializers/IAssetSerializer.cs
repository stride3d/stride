// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Serializers
{
    public interface IAssetSerializerFactory
    {
        IAssetSerializer TryCreate(string assetFileExtension);
    }

    public interface IAssetSerializer
    {
        object Load(Stream stream, UFile filePath, ILogger log, bool clearBrokenObjectReferences, out bool aliasOccurred, out AttachedYamlAssetMetadata yamlMetadata);

        void Save(Stream stream, object asset, AttachedYamlAssetMetadata yamlMetadata, ILogger log = null);
    }
}
