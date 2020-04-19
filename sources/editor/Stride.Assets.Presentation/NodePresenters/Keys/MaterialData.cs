// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Assets.Presentation.NodePresenters.Keys
{
    public static class MaterialData
    {
        public const string AvailableEffectShaders = nameof(AvailableEffectShaders);
        public static readonly PropertyKey<IEnumerable<string>> Key = new PropertyKey<IEnumerable<string>>(AvailableEffectShaders, typeof(MaterialData));
    }
}
