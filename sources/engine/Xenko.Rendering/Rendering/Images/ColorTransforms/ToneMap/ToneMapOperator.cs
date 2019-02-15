// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Base class for a tonemap operator.
    /// </summary>
    [DataContract]
    public abstract class ToneMapOperator : ColorTransformBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapOperator"/> class.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <exception cref="System.ArgumentNullException">effectName</exception>
        protected ToneMapOperator(string effectName) : base(effectName)
        {
        }

        [DataMemberIgnore]
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
            }
        }
    }
}
