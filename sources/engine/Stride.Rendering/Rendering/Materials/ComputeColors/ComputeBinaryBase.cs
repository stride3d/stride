// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A node that describe a binary operation between two <see cref="IComputeNode"/>
    /// </summary>
    [DataContract(Inherited = true)]
    [Display("Binary Operator")]
    [InlineProperty]
    public abstract class ComputeBinaryBase<T> : ComputeNode where T : class, IComputeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeBinaryBase{T}"/> class.
        /// </summary>
        protected ComputeBinaryBase()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeBinaryBase{T}"/> class.
        /// </summary>
        /// <param name="leftChild">The left child.</param>
        /// <param name="rightChild">The right child.</param>
        /// <param name="binaryOperator">The material binary operand.</param>
        protected ComputeBinaryBase(T leftChild, T rightChild, BinaryOperator binaryOperator)
        {
            LeftChild = leftChild;
            RightChild = rightChild;
            Operator = binaryOperator;
        }

        /// <summary>
        /// The operation to blend the nodes.
        /// </summary>
        /// <userdoc>
        /// The operation between the left (background) and the right (foreground) sub-nodes.
        /// </userdoc>
        [DataMember(10)]
        [InlineProperty]
        public BinaryOperator Operator { get; set; }

        /// <summary>
        /// The left (background) child node.
        /// </summary>
        /// <userdoc>
        /// The map used for the left (background) node.
        /// </userdoc>
        [DataMember(20)]
        [Display("Left")]
        public T LeftChild { get; set; }

        /// <summary>
        /// The right (foreground) child node.
        /// </summary>
        /// <userdoc>
        /// The map used for the right (foreground) node.
        /// </userdoc>
        [DataMember(30)]
        [Display("Right")]
        public T RightChild { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            if (LeftChild != null)
                yield return LeftChild;
            if (RightChild != null)
                yield return RightChild;
        }

        private const string BackgroundCompositionName = "color1";
        private const string ForegroundCompositionName = "color2";

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var leftShaderSource = LeftChild?.GenerateShaderSource(context, baseKeys);
            var rightShaderSource = RightChild?.GenerateShaderSource(context, baseKeys);

            var shaderSource = new ShaderClassSource(GetCorrespondingShaderSourceName(Operator));
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(shaderSource);
            if (leftShaderSource != null)
                mixin.AddComposition(BackgroundCompositionName, leftShaderSource);
            if (rightShaderSource != null)
                mixin.AddComposition(ForegroundCompositionName, rightShaderSource);

            return mixin;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Binary operation";
        }

        /// <summary>
        /// Get the name of the ShaderClassSource corresponding to the operation
        /// </summary>
        /// <param name="binaryOperand">The operand.</param>
        /// <returns>The name of the ShaderClassSource.</returns>
        private static string GetCorrespondingShaderSourceName(BinaryOperator binaryOperand)
        {
            switch (binaryOperand)
            {
                case BinaryOperator.Add:
                    return "ComputeColorAdd3ds"; //TODO: change this (ComputeColorAdd?)
                case BinaryOperator.Average:
                    return "ComputeColorAverage";
                case BinaryOperator.Color:
                    return "ComputeColorColor";
                case BinaryOperator.ColorBurn:
                    return "ComputeColorColorBurn";
                case BinaryOperator.ColorDodge:
                    return "ComputeColorColorDodge";
                case BinaryOperator.Darken:
                    return "ComputeColorDarken3ds"; //"ComputeColorDarkenMaya" //TODO: change this
                case BinaryOperator.Desaturate:
                    return "ComputeColorDesaturate";
                case BinaryOperator.Difference:
                    return "ComputeColorDifference3ds"; //"ComputeColorDifferenceMaya" //TODO: change this
                case BinaryOperator.Divide:
                    return "ComputeColorDivide";
                case BinaryOperator.Exclusion:
                    return "ComputeColorExclusion";
                case BinaryOperator.HardLight:
                    return "ComputeColorHardLight";
                case BinaryOperator.HardMix:
                    return "ComputeColorHardMix";
                case BinaryOperator.Hue:
                    return "ComputeColorHue";
                case BinaryOperator.Illuminate:
                    return "ComputeColorIlluminate";
                case BinaryOperator.In:
                    return "ComputeColorIn";
                case BinaryOperator.Lighten:
                    return "ComputeColorLighten3ds"; //"ComputeColorLightenMaya" //TODO: change this
                case BinaryOperator.LinearBurn:
                    return "ComputeColorLinearBurn";
                case BinaryOperator.LinearDodge:
                    return "ComputeColorLinearDodge";
                case BinaryOperator.Mask:
                    return "ComputeColorMask";
                case BinaryOperator.Multiply:
                    return "ComputeColorMultiply"; //return "ComputeColorMultiply3ds"; //"ComputeColorMultiplyMaya" //TODO: change this
                case BinaryOperator.Out:
                    return "ComputeColorOut";
                case BinaryOperator.Over:
                    return "ComputeColorOver3ds"; //TODO: change this to "ComputeColorLerpAlpha"
                case BinaryOperator.Overlay:
                    return "ComputeColorOverlay3ds"; //"ComputeColorOverlayMaya" //TODO: change this
                case BinaryOperator.PinLight:
                    return "ComputeColorPinLight";
                case BinaryOperator.Saturate:
                    return "ComputeColorSaturate";
                case BinaryOperator.Saturation:
                    return "ComputeColorSaturation";
                case BinaryOperator.Screen:
                    return "ComputeColorScreen";
                case BinaryOperator.SoftLight:
                    return "ComputeColorSoftLight";
                case BinaryOperator.Subtract:
                    return "ComputeColorSubtract"; // "ComputeColorSubtract3ds" "ComputeColorSubtractMaya" //TODO: change this
                case BinaryOperator.SubstituteAlpha:
                    return "ComputeColorSubstituteAlpha";
                case BinaryOperator.Threshold:
                    return "ComputeColorThreshold";
                default:
                    throw new ArgumentOutOfRangeException("binaryOperand");
            }
        }
    }
}
