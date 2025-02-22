// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Reflection;

public class DefaultMemberComparer : IComparer<object>
{
    public int Compare(object? x, object? y)
    {
        if (x is IMemberDescriptor left && y is IMemberDescriptor right)
        {
            // If order is defined, first order by order
            if (left.Order.HasValue || right.Order.HasValue)
            {
                var leftOrder = left.Order ?? int.MaxValue;
                var rightOrder = right.Order ?? int.MaxValue;
                return leftOrder.CompareTo(rightOrder);
            }

            // try to order by class hierarchy + token (same as declaration order)
            var leftMember = left.MemberInfo;
            var rightMember = right.MemberInfo;
            if (leftMember != null || rightMember != null)
            {
                var comparison = leftMember.CompareMetadataTokenWith(rightMember);
                if (comparison != 0)
                    return comparison;
            }

            // else order by name (dynamic members, etc...)
            return left.DefaultNameComparer.Compare(left.Name, right.Name);
        }

        if (x is string sx && y is string sy)
        {
            return string.CompareOrdinal(sx, sy);
        }

        if (x is IComparable leftComparable)
        {
            return leftComparable.CompareTo(y);
        }

        var rightComparable = y as IComparable;
        return rightComparable?.CompareTo(y) ?? 0;
    }
}
