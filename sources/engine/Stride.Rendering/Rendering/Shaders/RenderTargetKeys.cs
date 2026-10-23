// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    /// <summary>
    /// Keys used for render target settings.
    /// </summary>
    public static class RenderTargetKeys
    {
        /// <summary>
        /// The depth stencil buffer key.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> DepthStencil = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(RenderTargetKeys), "RenderTarget.DepthStencil");

        /// <summary>
        /// The depth stencil buffer key used as an input shader resource.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> DepthStencilSource = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(RenderTargetKeys), "RenderTarget.DepthStencilSource");

        /// <summary>
        /// The render target key.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> RenderTarget = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(RenderTargetKeys), "RenderTarget.RenderTarget");

        /// <summary>
        /// The render target key.
        /// </summary>
        public static readonly ObjectParameterKey<Buffer> StreamTarget = (ObjectParameterKey<Buffer>)ParameterKeys.Merge(ParameterKeys.NewObject<Buffer>(), typeof(RenderTargetKeys), "RenderTarget.StreamTarget");

        /// <summary>
        /// Used to notify that a renderer requires support for depth stencil as shader resource
        /// </summary>
        public static readonly PropertyKey<bool> RequireDepthStencilShaderResource = new PropertyKey<bool>("RequireDepthStencilShaderResource", typeof(RenderTargetKeys));

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(RenderTargetKeys).TypeHandle);
        }
    }
}
