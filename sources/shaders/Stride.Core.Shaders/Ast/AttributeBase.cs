// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// An abstract class for attribute definition.
    /// </summary>
    public abstract partial class AttributeBase : Node
    {
    }

    /// <summary>
    /// An abstract class for a post attribute definition.
    /// </summary>
    public abstract partial class PostAttributeBase : AttributeBase
    {
    }
}
