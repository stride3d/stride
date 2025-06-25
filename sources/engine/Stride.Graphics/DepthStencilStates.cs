// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public static class DepthStencilStates
{
    static DepthStencilStates()
    {
        var defaultDescription = new DepthStencilStateDescription();
        defaultDescription.SetDefaults();
        Default = defaultDescription;
    }


    /// <summary>
    /// Known values for <see cref="DepthStencilStateDescription"/>.
    /// </summary>
        /// <summary>
        /// A built-in state object with default settings for using a depth stencil buffer.
        /// </summary>
    public static readonly DepthStencilStateDescription Default;

        /// <summary>
        /// A built-in state object with default settings using greater comparison for Z.
        /// </summary>
    public static readonly DepthStencilStateDescription DefaultInverse = new(depthEnable: true, depthWriteEnable: true)
    {
        DepthBufferFunction = CompareFunction.GreaterEqual
    };

        /// <summary>
        /// A built-in state object with settings for enabling a read-only depth stencil buffer.
        /// </summary>
    public static readonly DepthStencilStateDescription DepthRead = new(depthEnable: true, depthWriteEnable: false);

        /// <summary>
        /// A built-in state object with settings for not using a depth stencil buffer.
        /// </summary>
    public static readonly DepthStencilStateDescription None = new(depthEnable: false, depthWriteEnable: false);
}
