// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;

namespace Xenko.Core.Serialization
{
    internal static class StringHashHelper
    {
        public static uint GetSerializerHashCode([NotNull] this string param)
        {
            uint result = 0;
            foreach (var c in param)
            {
                result ^= result << 4;
                result ^= result << 24;
                result ^= c;
            }
            return result;
        }
    }
}
