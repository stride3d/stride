// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace StrideAssetPlugin;

// Runtime content produced by the Assets package's SpinAsset compiler.
[DataContract]
[ContentSerializer(typeof(DataContentSerializer<SpinData>))]
public class SpinData
{
    public float Speed { get; set; }
}
