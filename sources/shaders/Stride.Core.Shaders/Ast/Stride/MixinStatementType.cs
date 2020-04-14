// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// Type of a mixin.
    /// </summary>
    public enum MixinStatementType
    {
        /// <summary>
        /// The default mixin (standard mixin).
        /// </summary>
        Default,

        /// <summary>
        /// The compose mixin used to specifiy a composition.
        /// </summary>
        Compose,

        /// <summary>
        /// The child mixin used to specifiy a children shader.
        /// </summary>
        Child,

        /// <summary>
        /// The clone mixin to clone the current mixins where the clone is emitted.
        /// </summary>
        Clone,

        /// <summary>
        /// The remove mixin to remove a mixin from current mixins.
        /// </summary>
        Remove,

        /// <summary>
        /// The macro mixin to declare a variable to be exposed in the mixin
        /// </summary>
        Macro
    }
}
