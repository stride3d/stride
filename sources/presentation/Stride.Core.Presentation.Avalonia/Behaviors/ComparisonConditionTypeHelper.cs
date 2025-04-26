// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

internal static class ComparisonConditionTypeHelper
{
    public static bool Compare(object? leftOperand, ComparisonConditionType operatorType, object? rightOperand)
    {
        if (leftOperand is not null && rightOperand is not null)
        {
            if (rightOperand is string value)
            {
                if (TypeConverterHelper.TryConvert(value, leftOperand.GetType(), out var converted))
                    rightOperand = converted;
            }
        }

        var leftComparableOperand = leftOperand as IComparable;
        var rightComparableOperand = rightOperand as IComparable;
        if (leftComparableOperand is not null && rightComparableOperand is not null)
        {
            return EvaluateComparable(leftComparableOperand, operatorType, rightComparableOperand);
        }

        switch (operatorType)
        {
            case ComparisonConditionType.Equal:
                return Equals(leftOperand, rightOperand);

            case ComparisonConditionType.NotEqual:
                return !Equals(leftOperand, rightOperand);

            case ComparisonConditionType.LessThan:
            case ComparisonConditionType.LessThanOrEqual:
            case ComparisonConditionType.GreaterThan:
            case ComparisonConditionType.GreaterThanOrEqual:
                {
                    throw leftComparableOperand switch
                    {
                        null when rightComparableOperand is null => new ArgumentException(string.Format(
                            CultureInfo.CurrentCulture,
                            "Binding property of type {0} and Value property of type {1} cannot be used with operator {2}.",
                            leftOperand?.GetType().Name ?? "null", rightOperand?.GetType().Name ?? "null",
                            operatorType.ToString())),
                        null => new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                            "Binding property of type {0} cannot be used with operator {1}.",
                            leftOperand?.GetType().Name ?? "null", operatorType.ToString())),
                        _ => new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                            "Value property of type {0} cannot be used with operator {1}.",
                            rightOperand?.GetType().Name ?? "null", operatorType.ToString()))
                    };
                }
        }

        return false;
    }

    /// <summary>
    /// Evaluates both operands that implement the IComparable interface.
    /// </summary>
    private static bool EvaluateComparable(IComparable leftOperand, ComparisonConditionType operatorType, IComparable rightOperand)
    {
        object? convertedOperand = null;
        try
        {
            convertedOperand = Convert.ChangeType(rightOperand, leftOperand.GetType(), CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            // FormatException: Convert.ChangeType("hello", typeof(double), ...);
        }
        catch (InvalidCastException)
        {
            // InvalidCastException: Convert.ChangeType(4.0d, typeof(Rectangle), ...);
        }

        if (convertedOperand is null)
        {
            return operatorType == ComparisonConditionType.NotEqual;
        }

        var comparison = leftOperand.CompareTo((IComparable)convertedOperand);
        return operatorType switch
        {
            ComparisonConditionType.Equal => comparison == 0,
            ComparisonConditionType.NotEqual => comparison != 0,
            ComparisonConditionType.LessThan => comparison < 0,
            ComparisonConditionType.LessThanOrEqual => comparison <= 0,
            ComparisonConditionType.GreaterThan => comparison > 0,
            ComparisonConditionType.GreaterThanOrEqual => comparison >= 0,
            _ => false
        };
    }
}
