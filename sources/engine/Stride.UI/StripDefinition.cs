// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.UI
{
    /// <summary>
    /// Represents the definition of a grid strip.
    /// </summary>
    [DataContract(nameof(StripDefinition))]
    [Display(Expand = ExpandRule.Never)]
    public class StripDefinition
    {
        private float maximumSize = float.PositiveInfinity;
        private float minimumSize;
        private StripType type = StripType.Star;
        private float sizeValue = 1.0f;

        /// <summary>
        /// The actual size of the strip in virtual pixels.
        /// </summary>
        [DataMemberIgnore]
        public float ActualSize { get; internal set; }

        /// <summary>
        /// Creates a 1-Star sized strip definition.
        /// </summary>
        public StripDefinition()
        {
        }

        /// <summary>
        /// Creates a <see cref="StripDefinition"/> with the provided size and type.
        /// </summary>
        /// <param name="type">The type of the strip to create</param>
        /// <param name="sizeValue">The value of the strip to create</param>
        public StripDefinition(StripType type, float sizeValue = 1.0f)
        {
            Type = type;
            SizeValue = sizeValue;
        }

        /// <summary>
        /// The maximum size of the strip in virtual pixels.
        /// </summary>
        /// <remarks>The value is coerced in the range [<see cref="MinimumSize"/>, <see cref="float.PositiveInfinity"/>].</remarks>
        /// <userdoc>The maximum size of the strip in virtual pixels.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [DefaultValue(float.PositiveInfinity)]
        public float MaximumSize
        {
            get { return maximumSize; }
            set
            {
                if (float.IsNaN(value))
                    return;
                CoerceMaximumSize(value);
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The minimum size of the strip in virtual pixels.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The minimum size of the strip in virtual pixels.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [DefaultValue(0)]
        public float MinimumSize
        {
            get { return minimumSize; }
            set
            {
                if (float.IsNaN(value))
                    return;
                minimumSize = MathUtil.Clamp(value, 0.0f, float.MaxValue); ;
                CoerceMaximumSize(maximumSize);
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the type of the strip.
        /// </summary>
        /// <userdoc>The type of the strip.</userdoc>
        [DataMember]
        [DefaultValue(StripType.Star)]
        public StripType Type
        {
            get { return type; }
            set
            {
                if (type == value)
                    return;

                type = value;
                
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the size value of the strip. 
        /// Note that the value is interpreted differently depending on the strip <see cref="Type"/>.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The size value of the strip.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [DefaultValue(1.0f)]
        public float SizeValue
        {
            get { return sizeValue; }
            set
            {
                if (float.IsNaN(value))
                    return;
                sizeValue = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal event EventHandler<EventArgs> DefinitionChanged;

        /// <summary>
        /// Clamp the provided size by the definition's minimum and maximum values.
        /// </summary>
        /// <param name="desiredSize">The size to clamp</param>
        /// <returns>The size clamped by the minimum and maximum values of the strip definition</returns>
        public float ClampSizeByMinimumMaximum(float desiredSize)
        {
            return MathUtil.Clamp(desiredSize, MinimumSize, MaximumSize);
        }

        internal float ValueRelativeMinimum()
        {
            if (sizeValue < MathUtil.ZeroTolerance)
                return 0;
            return MinimumSize / SizeValue;
        }

        internal float ValueRelativeMaximum()
        {
            if (sizeValue < MathUtil.ZeroTolerance)
                return 0;
            return MaximumSize / SizeValue;
        }

        internal class SortByIncreasingStarRelativeMinimumValues : IComparer<StripDefinition>
        {
            public int Compare(StripDefinition def1, StripDefinition def2)
            {
                var val1 = def1.ValueRelativeMinimum();
                var val2 = def2.ValueRelativeMinimum();

                return val1.CompareTo(val2);
            }
        }

        internal class SortByIncreasingStarRelativeMaximumValues : IComparer<StripDefinition>
        {
            public int Compare(StripDefinition def1, StripDefinition def2)
            {
                var val1 = def1.ValueRelativeMaximum();
                var val2 = def2.ValueRelativeMaximum();

                return val1.CompareTo(val2);
            }
        }

        private void CoerceMaximumSize(float newValue)
        {
            maximumSize = MathUtil.Clamp(newValue, minimumSize, float.PositiveInfinity);
        }
    }
}
