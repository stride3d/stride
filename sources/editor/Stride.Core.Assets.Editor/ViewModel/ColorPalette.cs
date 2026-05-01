// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.ViewModel
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<ColorPalette>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<ColorPalette>), Profile = "Content")]
    public class ColorPalette
    {
        public Dictionary<string, Color3> Colors { get; set; } = new Dictionary<string, Color3>();
    }
}