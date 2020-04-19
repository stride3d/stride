// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    /// <summary>
    /// Represents a specific value for <see cref="CreateNewInstanceCommand"/>.
    /// </summary>
    public sealed class AbstractNodeValue : AbstractNodeEntry
    {
        /// <summary>
        /// An object that can be passed as parameter to the command, in order to set the value of the node to <c>null</c>.
        /// </summary>
        public static AbstractNodeValue Null { get; } = new AbstractNodeValue(null, "None", -100);

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractNodeValue"/> class.
        /// </summary>
        /// <param name="value">The value corresponding to this entry.</param>
        /// <param name="displayValue">The display name of this entry.</param>
        /// <param name="order">An arbitrary order for this entry.</param>
        public AbstractNodeValue(object value, [NotNull] string displayValue, int order)
        {
            if (displayValue == null) throw new ArgumentNullException(nameof(displayValue));
            Value = value;
            DisplayValue = displayValue;
            Order = order;
        }

        /// <summary>
        /// The value.
        /// </summary>
        public object Value { get; }

        public override int Order { get; }

        /// <inheritdoc/>
        public override bool Equals(AbstractNodeEntry other)
        {
            var abstractNodeValue = other as AbstractNodeValue;
            if (abstractNodeValue == null)
                return false;

            return Equals(Value, abstractNodeValue.Value);
        }

        /// <inheritdoc/>
        public override string DisplayValue { get; }

        /// <inheritdoc/>
        public override object GenerateValue(object currentValue) => Value;

        /// <inheritdoc/>
        public override bool IsMatchingValue(object value) => ReferenceEquals(Value, value);

        /// <inheritdoc/>
        protected override int ComputeHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }
    }
}
