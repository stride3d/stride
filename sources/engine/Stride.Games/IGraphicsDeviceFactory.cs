// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

using Stride.Graphics;

namespace Stride.Games;

/// <summary>
///   Base interface for a factory that creates <see cref="GraphicsDevice"/> instances.
/// </summary>
/// <remarks>
///   <para>
///     The implementers of <see cref="IGraphicsDeviceFactory"/> are responsible for creating
///     <see cref="GraphicsDevice"/> instances, and for selecting the best device based on the
///     preferred configuration.
///   </para>
///   <para>
///     A good example of a factory is the <see cref="GamePlatform"/> class, which not only
///     abstracts the platform, but also the windowing system and the Graphics Device creation.
///   </para>
/// </remarks>
public interface IGraphicsDeviceFactory
{
    /// <summary>
    ///   Returns a list of <see cref="GraphicsDeviceInformation"/> instances, representing
    ///   the best found Graphics Adapters and their corresponding configuration to create a Graphics Device
    ///   based on the given graphics parameters.
    /// </summary>
    /// <param name="graphicsParameters">The preferred graphics configuration.</param>
    /// <returns>
    ///   A list of the best found configurations for creating a Graphics Device.
    /// </returns>
    List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters graphicsParameters);

    /// <summary>
    ///   Changes an existing Graphics Device or creates a new one with the specified configuration.
    /// </summary>
    /// <param name="currentDevice">
    ///   An optional <see cref="GraphicsDevice"/> instance to reconfigure.
    ///   Specify <see langword="null"/> to create a new device.
    /// </param>
    /// <param name="deviceInformation">
    ///   The <see cref="GraphicsDeviceInformation"/> containing the Graphics Adapter, Graphics Profile, and
    ///   other relevant flags and parameters required to configure the Graphics Device.
    /// </param>
    /// <returns>The created (or changed) Graphics Device.</returns>
    GraphicsDevice ChangeOrCreateDevice(GraphicsDevice? currentDevice, GraphicsDeviceInformation deviceInformation);
}
