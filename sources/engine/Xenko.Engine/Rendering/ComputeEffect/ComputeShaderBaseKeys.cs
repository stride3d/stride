// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.ComputeEffect
{
    public class ComputeShaderBaseKeys
    {   
        public static readonly ValueParameterKey<Int3> ThreadGroupCountGlobal = ParameterKeys.NewValue<Int3>();
    }
}
