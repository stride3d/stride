// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class DisplayData
    {
        public const string AttributeDisplayName = nameof(AttributeDisplayName);
        public const string AutoExpandRule = nameof(AutoExpandRule);
        public const string UnloadableObjectInfo = nameof(UnloadableObjectInfo);

        public static readonly PropertyKey<string> AttributeDisplayNameKey = new PropertyKey<string>(AttributeDisplayName, typeof(DisplayData));
        public static readonly PropertyKey<ExpandRule> AutoExpandRuleKey = new PropertyKey<ExpandRule>(AutoExpandRule, typeof(DisplayData));
    }
}
