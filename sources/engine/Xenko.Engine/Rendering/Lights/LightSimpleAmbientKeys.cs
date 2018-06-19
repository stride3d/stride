// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    public static partial class LightSimpleAmbientKeys
    {
        static LightSimpleAmbientKeys()
        {
            AmbientLight = ParameterKeys.NewValue(new Color3(1.0f, 1.0f, 1.0f));
        }
    }
}
