// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Grammar
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

