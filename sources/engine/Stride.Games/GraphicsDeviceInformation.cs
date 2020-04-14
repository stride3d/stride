// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Xenko.Graphics;

namespace Xenko.Games
{
    public class GraphicsDeviceInformation : IEquatable<GraphicsDeviceInformation>
    {
        #region Fields

        private GraphicsAdapter adapter;

        private GraphicsProfile graphicsProfile;

        private PresentationParameters presentationParameters;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceInformation" /> class.
        /// </summary>
        public GraphicsDeviceInformation()
        {
            Adapter = GraphicsAdapterFactory.Default;
            PresentationParameters = new PresentationParameters();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the adapter.
        /// </summary>
        /// <value>The adapter.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        public GraphicsAdapter Adapter
        {
            get
            {
                return adapter;
            }

            set
            {
                adapter = value;
            }
        }

        /// <summary>
        /// Gets or sets the graphics profile.
        /// </summary>
        /// <value>The graphics profile.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        public GraphicsProfile GraphicsProfile
        {
            get
            {
                return graphicsProfile;
            }

            set
            {
                graphicsProfile = value;
            }
        }

        /// <summary>
        /// Gets or sets the presentation parameters.
        /// </summary>
        /// <value>The presentation parameters.</value>
        /// <exception cref="System.ArgumentNullException">if value is null</exception>
        public PresentationParameters PresentationParameters
        {
            get
            {
                return presentationParameters;
            }

            set
            {
                presentationParameters = value;
            }
        }

        /// <summary>
        /// Gets or sets the creation flags.
        /// </summary>
        /// <value>The creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags { get; set; }

        #endregion

        #region Public Methods and Operators

        public bool Equals(GraphicsDeviceInformation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(adapter, other.adapter) && graphicsProfile == other.graphicsProfile && Equals(presentationParameters, other.presentationParameters);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GraphicsDeviceInformation)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (adapter != null ? adapter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)graphicsProfile;
                hashCode = (hashCode * 397) ^ (presentationParameters != null ? presentationParameters.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(GraphicsDeviceInformation left, GraphicsDeviceInformation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GraphicsDeviceInformation left, GraphicsDeviceInformation right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A new copy-instance of this GraphicsDeviceInformation.</returns>
        public GraphicsDeviceInformation Clone()
        {
            var newValue = (GraphicsDeviceInformation)MemberwiseClone();
            newValue.PresentationParameters = PresentationParameters.Clone();
            return newValue;
        }

        #endregion
    }
}
