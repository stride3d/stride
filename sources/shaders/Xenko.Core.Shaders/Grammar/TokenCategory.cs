// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Shaders.Grammar
{
    public enum TokenCategory
    {
        WhiteSpace,
        Keyword,
        Typename,
        Number,
        Comment,
        MultilineComment,
        Identifier,
        String,
        Puntuation,
        Operator
    }
}

