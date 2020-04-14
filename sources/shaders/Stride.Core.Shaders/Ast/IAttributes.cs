// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Xenko.Core.Shaders.Ast
{
    public interface IAttributes
    {
        List<AttributeBase> Attributes { get; set; }
    }
}
