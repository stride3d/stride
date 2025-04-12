// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;

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
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => byte.MinValue,
            TypeCode.SByte => sbyte.MinValue,
            TypeCode.Int16 => short.MinValue,
            TypeCode.UInt16 => ushort.MinValue,
            TypeCode.Int32 => int.MinValue,
            TypeCode.UInt32 => uint.MinValue,
            TypeCode.Int64 => long.MinValue,
            TypeCode.UInt64 => ulong.MinValue,
            TypeCode.Single => float.MinValue,
            TypeCode.Double => double.MinValue,
            TypeCode.Decimal => decimal.MinValue,
            _ => new ArgumentException("Numeric type expected")
        };
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
    return Type.GetTypeCode(type) switch
    {
        TypeCode.Byte => byte.MaxValue,
        TypeCode.SByte => sbyte.MaxValue,
        TypeCode.Int16 => short.MaxValue,
        TypeCode.UInt16 => ushort.MaxValue,
        TypeCode.Int32 => int.MaxValue,
        TypeCode.UInt32 => uint.MaxValue,
        TypeCode.Int64 => long.MaxValue,
        TypeCode.UInt64 => ulong.MaxValue,
        TypeCode.Single => float.MaxValue,
        TypeCode.Double => double.MaxValue,
        TypeCode.Decimal => decimal.MaxValue,
        _ => new ArgumentException("Numeric type expected")
    };
}
}
