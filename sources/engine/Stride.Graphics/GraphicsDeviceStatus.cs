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

namespace Stride.Graphics
{
    /// <summary>
    /// Describes the current status of a <see cref="GraphicsDevice"/>.
    /// </summary>
    public enum GraphicsDeviceStatus
    {
        /// <summary>
        /// The device is running fine.
        /// </summary>
        Normal,

        /// <summary>
        /// The video card has been physically removed from the system, or a driver upgrade for the video card has occurred. The application should destroy and recreate the device.
        /// </summary>
        Removed,

        /// <summary>
        /// The application's device failed due to badly formed commands sent by the application. This is an design-time issue that should be investigated and fixed.
        /// </summary>
        Hung,

        /// <summary>
        /// The device failed due to a badly formed command. This is a run-time issue; The application should destroy and recreate the device.
        /// </summary>
        Reset,

        /// <summary>
        /// The driver encountered a problem and was put into the device removed state.
        /// </summary>
        InternalError,

        /// <summary>
        /// The application provided invalid parameter data; this must be debugged and fixed before the application is released.
        /// </summary>
        InvalidCall,
    }
}
