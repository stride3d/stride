// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Microsoft.CodeAnalysis;

namespace Stride.Assets.Scripts
{
    public struct SlotGeneratorContext
    {
        public SlotGeneratorContext(Compilation compilation)
        {
            Compilation = compilation;
        }

        public Compilation Compilation { get; }
    }
}
