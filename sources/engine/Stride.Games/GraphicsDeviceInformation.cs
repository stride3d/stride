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

using System;

using Stride.Graphics;

namespace Stride.Games;

public class GraphicsDeviceInformation : IEquatable<GraphicsDeviceInformation>
{
    public GraphicsAdapter Adapter { get; set; }

    public GraphicsProfile GraphicsProfile { get; set; }

    public PresentationParameters PresentationParameters { get; set; }

    public DeviceCreationFlags DeviceCreationFlags { get; set; }


    public GraphicsDeviceInformation()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceInformation" /> class.
        /// </summary>
        /// <summary>
        /// Gets or sets the adapter.
        /// </summary>
        /// <value>The adapter.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        /// <summary>
        /// Gets or sets the graphics profile.
        /// </summary>
        /// <value>The graphics profile.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        /// <summary>
        /// Gets or sets the presentation parameters.
        /// </summary>
        /// <value>The presentation parameters.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        /// <summary>
        /// Gets or sets the creation flags.
        /// </summary>
        /// <value>The creation flags.</value>
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A new copy-instance of this GraphicsDeviceInformation.</returns>
        Adapter = GraphicsAdapterFactory.DefaultAdapter;
        PresentationParameters = new PresentationParameters();
    }


    public bool Equals(GraphicsDeviceInformation? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Equals(Adapter, other.Adapter)
            && GraphicsProfile == other.GraphicsProfile
            && Equals(PresentationParameters, other.PresentationParameters);
    }

    public override bool Equals(object? obj)
    {
        // TODO: Can GraphicsDeviceInformation be sealed? (No GetType())
        return obj?.GetType() != GetType() && Equals((GraphicsDeviceInformation) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Adapter, GraphicsProfile, PresentationParameters);
    }

    public static bool operator ==(GraphicsDeviceInformation left, GraphicsDeviceInformation right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GraphicsDeviceInformation left, GraphicsDeviceInformation right)
    {
        return !Equals(left, right);
    }

    public GraphicsDeviceInformation Clone()
    {
        var newValue = (GraphicsDeviceInformation) MemberwiseClone();
        newValue.PresentationParameters = PresentationParameters.Clone();
        return newValue;
    }
}
