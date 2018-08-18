// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    /// <summary>
    ///   Describess how data will be displayed to the screen.
    /// </summary>
    public class PresentationParameters : IEquatable<PresentationParameters>
    {
        #region Fields

        /// <summary>
        ///   A <strong><see cref="SharpDX.DXGI.Format" /></strong> structure describing the display format.
        /// </summary>
        public PixelFormat BackBufferFormat;

        /// <summary>
        ///   A value that describes the resolution height.
        /// </summary>
        public int BackBufferHeight;

        /// <summary>
        ///   A value that describes the resolution width.
        /// </summary>
        public int BackBufferWidth;

        /// <summary>
        /// Gets or sets the depth stencil format
        /// </summary>
        public PixelFormat DepthStencilFormat;

        /// <summary>
        ///   A Window object. See remarks.
        /// </summary>
        /// <remarks>
        ///   A window object is platform dependent:
        ///   <ul>
        ///     <li>On Windows Desktop: This could a low level window/control handle (IntPtr), or directly a Winform Control object.</li>
        ///     <li>On Windows Metro: This could be SwapChainBackgroundPanel or SwapChainPanel object.</li>
        ///   </ul>
        /// </remarks>
        public WindowHandle DeviceWindowHandle;

        /// <summary>
        ///   Gets or sets a value indicating whether the application is in full screen mode.
        /// </summary>
        public bool IsFullScreen;

        /// <summary>
        ///   Gets or sets a value indicating the number of sample locations during multisampling.
        /// </summary>
        public MultisampleCount MultisampleCount;

        /// <summary>
        ///   Gets or sets the maximum rate at which the swap chain's back buffers can be presented to the front buffer.
        /// </summary>
        public PresentInterval PresentationInterval;

        /// <summary>
        ///   A structure describing the refresh rate in hertz
        /// </summary>
        public Rational RefreshRate;

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex;

        /// <summary>
        /// The colorspace used.
        /// </summary>
        public ColorSpace ColorSpace;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class with default values.
        /// </summary>
        public PresentationParameters()
        {
            BackBufferWidth = 800;
            BackBufferHeight = 480;
            BackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
            PresentationInterval = PresentInterval.Immediate;
            DepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            MultisampleCount = MultisampleCount.None;
            IsFullScreen = false;
            RefreshRate = new Rational(60, 1); // by default
            ColorSpace = ColorSpace.Linear;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class with <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
        /// </summary>
        /// <param name="backBufferWidth">Width of the back buffer.</param>
        /// <param name="backBufferHeight">Height of the back buffer.</param>
        /// <param name="deviceWindowHandle">The device window handle.</param>
        public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle deviceWindowHandle)
            : this(backBufferWidth, backBufferHeight, deviceWindowHandle, PixelFormat.R8G8B8A8_UNorm)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class.
        /// </summary>
        /// <param name="backBufferWidth">Width of the back buffer.</param>
        /// <param name="backBufferHeight">Height of the back buffer.</param>
        /// <param name="deviceWindowHandle">The device window handle.</param>
        /// <param name="backBufferFormat">The back buffer format.</param>
        public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle deviceWindowHandle, PixelFormat backBufferFormat)
            : this()
        {
            BackBufferWidth = backBufferWidth;
            BackBufferHeight = backBufferHeight;
            DeviceWindowHandle = deviceWindowHandle;
            BackBufferFormat = backBufferFormat;
        }

        #endregion

        #region Methods

        public PresentationParameters Clone()
        {
            return (PresentationParameters)MemberwiseClone();
        }

        public bool Equals(PresentationParameters other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BackBufferFormat == other.BackBufferFormat && BackBufferHeight == other.BackBufferHeight && BackBufferWidth == other.BackBufferWidth && DepthStencilFormat == other.DepthStencilFormat && Equals(DeviceWindowHandle, other.DeviceWindowHandle) && IsFullScreen == other.IsFullScreen && MultisampleCount == other.MultisampleCount && PresentationInterval == other.PresentationInterval && RefreshRate.Equals(other.RefreshRate) && PreferredFullScreenOutputIndex == other.PreferredFullScreenOutputIndex && ColorSpace == other.ColorSpace;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PresentationParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)BackBufferFormat;
                hashCode = (hashCode * 397) ^ BackBufferHeight;
                hashCode = (hashCode * 397) ^ BackBufferWidth;
                hashCode = (hashCode * 397) ^ (int)DepthStencilFormat;
                hashCode = (hashCode * 397) ^ (DeviceWindowHandle != null ? DeviceWindowHandle.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsFullScreen.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MultisampleCount;
                hashCode = (hashCode * 397) ^ (int)PresentationInterval;
                hashCode = (hashCode * 397) ^ RefreshRate.GetHashCode();
                hashCode = (hashCode * 397) ^ PreferredFullScreenOutputIndex;
                hashCode = (hashCode * 397) ^ (int)ColorSpace;
                return hashCode;
            }
        }

        public static bool operator ==(PresentationParameters left, PresentationParameters right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PresentationParameters left, PresentationParameters right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
