// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast;

namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class LiteralIdentifier : Identifier
    {
        public LiteralIdentifier()
        {
        }

        public LiteralIdentifier(Literal valueName)
            : base(valueName.ToString())
        {
            Value = valueName;
        }


        public Literal Value { get; set; }
    }
}
