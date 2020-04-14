// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class InlineData
    {
        public const string InlineMember = nameof(InlineMember);
        public const string InlinedProperty = nameof(InlinedProperty);
        public static readonly PropertyKey<bool> InlineMemberKey = new PropertyKey<bool>(InlineMember, typeof(InlineData));
        public static readonly PropertyKey<bool> InlinedPropertyKey = new PropertyKey<bool>(InlinedProperty, typeof(InlineData));
    }
}
