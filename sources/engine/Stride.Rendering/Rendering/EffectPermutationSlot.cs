// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering
{
    /// <summary>
    /// Defines an effect permutation slot for a <see cref="RootRenderFeature"/>.
    /// </summary>
    /// Each time an object is added to the render system, multiple effect instantiations might live concurrently. This define one such instantiation.
    /// To give a concrete example, a typical <see cref="MeshRenderFeature"/> might define a slot for main effect, one for gbuffer effect and one for shadow mapping effect.
    public struct EffectPermutationSlot
    {
        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly EffectPermutationSlot Invalid = new EffectPermutationSlot(-1);

        public readonly int Index;

        internal EffectPermutationSlot(int index)
        {
            Index = index;
        }
    }
}
