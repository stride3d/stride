// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Assets.Yaml;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets.Serializers
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
