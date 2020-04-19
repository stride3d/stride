// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    /// <summary>
    /// Arguments of the <see cref="INodePresenter.ValueChanged"/> event.
    /// </summary>
    public class ValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value of the node.</param>
        public ValueChangedEventArgs(object oldValue)
        {
            OldValue = oldValue;
        }

        /// <summary>
        /// The old value of the node.
        /// </summary>
        public object OldValue { get; }
    }
}
