// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Collections;

namespace Xenko.Rendering
{
    public class ParameterCollectionLayout
    {
        public FastListStruct<ParameterKeyInfo> LayoutParameterKeyInfos = new FastListStruct<ParameterKeyInfo>(0);
        public int ResourceCount;
        public int BufferSize;
    }
}
