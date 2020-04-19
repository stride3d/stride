// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class NumericValueNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            var modelNode = node as AssetMemberNodePresenter;
            var memberDescriptor = modelNode?.MemberDescriptor;
            if (memberDescriptor == null)
                return;

            UpdateNode(node, memberDescriptor.MemberInfo);
        }

        public void UpdateNode(INodePresenter node, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));

            var stepRange = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DataMemberRangeAttribute>(memberInfo);
            var isNumeric = node.Type.IsNumeric();
            // If the type is integral numeric, we don't want decimal places
            if (node.Type.IsIntegral())
                node.AttachedProperties.Add(NumericData.DecimalPlacesKey, 0);

            // If the type is just 8 bits, we want to display it with a slider so we attach small/large steps information
            if (node.Type == typeof(byte) || node.Type == typeof(sbyte))
            {
                node.AttachedProperties.Add(NumericData.SmallStepKey, 1);
                node.AttachedProperties.Add(NumericData.LargeStepKey, 5);
            }

            // Get the min/max values from the type.
            if (isNumeric)
                node.AttachedProperties.Add(NumericData.MinimumKey, GetMinimum(node.Type));
            if (isNumeric)
                node.AttachedProperties.Add(NumericData.MaximumKey, GetMaximum(node.Type));

            if (stepRange != null)
            {
                // If we have the attribute, override the attached properties values with what it indicates
                if (stepRange.Minimum != null)
                    node.AttachedProperties.Set(NumericData.MinimumKey, stepRange.Minimum);
                if (stepRange.Maximum != null)
                    node.AttachedProperties.Set(NumericData.MaximumKey, stepRange.Maximum);
                if (stepRange.SmallStep != null)
                    node.AttachedProperties.Set(NumericData.SmallStepKey, stepRange.SmallStep);
                if (stepRange.LargeStep != null)
                    node.AttachedProperties.Set(NumericData.LargeStepKey, stepRange.LargeStep);
                if (stepRange.DecimalPlaces != null)
                    node.AttachedProperties.Set(NumericData.DecimalPlacesKey, stepRange.DecimalPlaces);
            }
        }

        /// <summary>
        /// Gets the minimum value for the given numeric type.
        /// </summary>
        /// <param name="type">The type for which to get the minimum value.</param>
        /// <returns>The minimum value of the given type.</returns>
        /// <exception cref="ArgumentException">The given type is not a numeric type.</exception>
        /// <remarks>A type is numeric when <see cref="TypeExtensions.IsNumeric"/> returns true.</remarks>
        public static object GetMinimum(Type type)
        {
            if (type == typeof(sbyte))
                return SByte.MinValue;
            if (type == typeof(short))
                return Int16.MinValue;
            if (type == typeof(int))
                return Int32.MinValue;
            if (type == typeof(long))
                return Int64.MinValue;
            if (type == typeof(byte))
                return Byte.MinValue;
            if (type == typeof(ushort))
                return UInt16.MinValue;
            if (type == typeof(uint))
                return UInt32.MinValue;
            if (type == typeof(ulong))
                return UInt64.MinValue;
            if (type == typeof(float))
                return Single.MinValue;
            if (type == typeof(double))
                return Double.MinValue;
            if (type == typeof(decimal))
                return Decimal.MinValue;

            throw new ArgumentException("Numeric type expected");
        }

        /// <summary>
        /// Gets the maximum value for the given numeric type.
        /// </summary>
        /// <param name="type">The type for which to get the maximum value.</param>
        /// <returns>The maximum value of the given type.</returns>
        /// <exception cref="ArgumentException">The given type is not a numeric type.</exception>
        /// <remarks>A type is numeric when <see cref="TypeExtensions.IsNumeric"/> returns true.</remarks>
        public static object GetMaximum(Type type)
        {
            if (type == typeof(sbyte))
                return SByte.MaxValue;
            if (type == typeof(short))
                return Int16.MaxValue;
            if (type == typeof(int))
                return Int32.MaxValue;
            if (type == typeof(long))
                return Int64.MaxValue;
            if (type == typeof(byte))
                return Byte.MaxValue;
            if (type == typeof(ushort))
                return UInt16.MaxValue;
            if (type == typeof(uint))
                return UInt32.MaxValue;
            if (type == typeof(ulong))
                return UInt64.MaxValue;
            if (type == typeof(float))
                return Single.MaxValue;
            if (type == typeof(double))
                return Double.MaxValue;
            if (type == typeof(decimal))
                return Decimal.MaxValue;

            throw new ArgumentException("Numeric type expected");
        }
    }
}
