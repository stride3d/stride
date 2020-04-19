// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
// Some parts of this file were inspired by OxyPlot (https://github.com/oxyplot/oxyplot)
/*
The MIT license (MTI)
https://opensource.org/licenses/MIT

Copyright (c) 2014 OxyPlot contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;

namespace Stride.Assets.Presentation.CurveEditor
{
    /// <summary>
    /// Defines change types for the <see cref="AxisBase.AxisChanged" /> event.
    /// </summary>
    public enum AxisChangeTypes
    {
        /// <summary>
        /// The axis was zoomed by the user.
        /// </summary>
        Zoom,

        /// <summary>
        /// The axis was panned by the user.
        /// </summary>
        Pan,

        /// <summary>
        /// The axis zoom/pan was reset by the user.
        /// </summary>
        Reset
    }

    /// <summary>
    /// Provides additional data for the <see cref="AxisBase.AxisChanged" /> event.
    /// </summary>
    public class AxisChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisChangedEventArgs" /> class.
        /// </summary>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="deltaMinimum">The delta minimum.</param>
        /// <param name="deltaMaximum">The delta maximum.</param>
        public AxisChangedEventArgs(AxisChangeTypes changeType, double deltaMinimum, double deltaMaximum)
        {
            ChangeType = changeType;
            DeltaMinimum = deltaMinimum;
            DeltaMaximum = deltaMaximum;
        }

        /// <summary>
        /// Gets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        public AxisChangeTypes ChangeType { get; private set; }

        /// <summary>
        /// Gets the delta for the minimum.
        /// </summary>
        /// <value>The delta.</value>
        public double DeltaMinimum { get; private set; }

        /// <summary>
        /// Gets the delta for the maximum.
        /// </summary>
        /// <value>The delta.</value>
        public double DeltaMaximum { get; private set; }
    }
}
