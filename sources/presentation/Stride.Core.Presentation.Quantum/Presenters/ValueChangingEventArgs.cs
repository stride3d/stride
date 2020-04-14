// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    /// <summary>
    /// Arguments of the <see cref="INodePresenter.ValueChanging"/> event.
    /// </summary>
    public class ValueChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueChangingEventArgs"/> class.
        /// </summary>
        /// <param name="newValue">The new value of the node.</param>
        public ValueChangingEventArgs(object newValue)
        {
            NewValue = newValue;
        }

        /// <summary>
        /// The new value of the node.
        /// </summary>
        public object NewValue { get; private set; }
    }
}
