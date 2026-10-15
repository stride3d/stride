// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Keys used for texture projection by lights.
    /// </summary>
    public static class TextureProjectionKeys
    {
        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> ProjectionTexture = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(TextureProjectionKeys), "TextureProjection.ProjectionTexture");

        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> UVScale = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(), typeof(TextureProjectionKeys), "TextureProjection.UVScale");

        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> UVOffset = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(), typeof(TextureProjectionKeys), "TextureProjection.UVOffset");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TextureProjectionKeys).TypeHandle);
        }
    }
}
