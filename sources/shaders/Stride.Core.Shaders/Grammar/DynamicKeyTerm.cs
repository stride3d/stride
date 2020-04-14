// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Irony.Parsing;

namespace Stride.Core.Shaders.Grammar
{
    internal abstract class DynamicKeyTerm : KeyTerm
    {
        protected DynamicKeyTerm(string text, string name)
            : base(text, name)
        {
        }

        public abstract void Match(Tokenizer toknizer, out Token token);
    }
}
