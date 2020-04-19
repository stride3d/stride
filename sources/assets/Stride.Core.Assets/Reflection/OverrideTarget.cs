// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Reflection
{
    /// <summary>
    /// Describes what is targeted by an override information.
    /// </summary>
    public enum OverrideTarget
    {
        /// <summary>
        /// The content itself.
        /// </summary>
        Content,
        /// <summary>
        /// An item of the content if it's a collection, or a value of the content if it's a dictionary.
        /// </summary>
        Item,
        /// <summary>
        /// A key of the content. This is valid only for dictionary.
        /// </summary>
        Key
    }
}
