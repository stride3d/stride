// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


namespace Stride.Particles.VertexLayouts
{
    public interface IAttributeTransformer<T, U>
    {
        void Transform(ref T attribute, ref U transformer);
    }
}
