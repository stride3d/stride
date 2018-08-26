// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Animations
{
    public enum BinaryCurveOperator
    {
        /// <summary>
        /// Add the sampled value of both sides.
        /// </summary>
        Add,

        /// <summary>
        /// Subtracts the right sampled value from the left sampled value.
        /// </summary>
        Subtract,

        /// <summary>
        /// Multiplies the left sampled value by the right sampled value
        /// </summary>
        Multiply,
    }

    /// <summary>
    /// A node which describes a binary operation between two <see cref="IComputeCurve{T}"/>
    /// </summary>
    /// <typeparam name="T">Sampled data's type</typeparam>
    [DataContract(Inherited = true)]
    [Display("Binary Operator")]
    [InlineProperty]
    public abstract class ComputeBinaryCurve<T> : IComputeCurve<T> where T : struct
    {
        private bool hasChanged = true;
        private BinaryCurveOperator operatorMethod = BinaryCurveOperator.Add;
        private IComputeCurve<T> leftChild;
        private IComputeCurve<T> rightChild;

        /// <inheritdoc/>
        public bool UpdateChanges()
        {
            var tmp = hasChanged;
            hasChanged = false;
            tmp |= leftChild?.UpdateChanges() ?? false;
            tmp |= rightChild?.UpdateChanges() ?? false;
            return tmp;
        }

        /// <inheritdoc/>
        public T Evaluate(float location)
        {
            var lValue = LeftChild?.Evaluate(location) ?? new T();
            var rValue = RightChild?.Evaluate(location) ?? new T();

            switch (Operator)
            {
                case BinaryCurveOperator.Add:
                    return Add(lValue, rValue);

                case BinaryCurveOperator.Subtract:
                    return Subtract(lValue, rValue);

                case BinaryCurveOperator.Multiply:
                    return Multiply(lValue, rValue);
            }

            throw new ArgumentException("Invalid Operator argument in ComputeBinaryCurve");
        }

        /// <summary>
        /// The operation used to blend the two values
        /// </summary>
        /// <userdoc>
        /// The operation used to blend the two values
        /// </userdoc>
        [DataMember(10)]
        [InlineProperty]
        public BinaryCurveOperator Operator
        {
            get { return operatorMethod; }
            set
            {
                operatorMethod = value;
                hasChanged = true;
            }
        }

        /// <summary>
        /// The left child node
        /// </summary>
        /// <userdoc>
        /// The left child value
        /// </userdoc>
        [DataMember(20)]
        [Display("Left")]
        public IComputeCurve<T> LeftChild
        {
            get { return leftChild; }
            set
            {
                leftChild = value;
                hasChanged = true;
            }
        }

        /// <summary>
        /// The right child node
        /// </summary>
        /// <userdoc>
        /// The right child value
        /// </userdoc>
        [DataMember(30)]
        [Display("Right")]
        public IComputeCurve<T> RightChild
        {
            get { return rightChild; }
            set
            {
                rightChild = value;
                hasChanged = true;
            }
        }

        /// <summary>
        /// Adds the left value to the right value and retuns their sum
        /// </summary>
        /// <param name="a">Left value A</param>
        /// <param name="b">Right value B</param>
        /// <returns>The sum A + B</returns>
        protected abstract T Add(T a, T b);

        /// <summary>
        /// Subtracts the right value from the left value and retuns the result
        /// </summary>
        /// <param name="a">Left value A</param>
        /// <param name="b">Right value B</param>
        /// <returns>The result A - B</returns>
        protected abstract T Subtract(T a, T b);

        /// <summary>
        /// Multiplies the left value to the right value and retuns the result
        /// </summary>
        /// <param name="a">Left value A</param>
        /// <param name="b">Right value B</param>
        /// <returns>The result A * B</returns>
        protected abstract T Multiply(T a, T b);
    }
}
