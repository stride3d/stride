// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    public interface IAssetNodeChangeEventArgs : INodeChangeEventArgs
    {
        OverrideType PreviousOverride { get; }

        OverrideType NewOverride { get; }

        ItemId ItemId { get; }
    }

    public class AssetMemberNodeChangeEventArgs : MemberNodeChangeEventArgs, IAssetNodeChangeEventArgs
    {
        public AssetMemberNodeChangeEventArgs([NotNull] MemberNodeChangeEventArgs e, OverrideType previousOverride, OverrideType newOverride, ItemId itemId)
            : base(e.Member, e.OldValue, e.NewValue)
        {
            PreviousOverride = previousOverride;
            NewOverride = newOverride;
            ItemId = itemId;
        }

        public OverrideType PreviousOverride { get; }

        public OverrideType NewOverride { get; }

        public ItemId ItemId { get; }
    }

    public class AssetItemNodeChangeEventArgs : ItemChangeEventArgs, IAssetNodeChangeEventArgs
    {
        public AssetItemNodeChangeEventArgs([NotNull] ItemChangeEventArgs e, OverrideType previousOverride, OverrideType newOverride, ItemId itemId)
            : base(e.Collection, e.Index, e.ChangeType, e.OldValue, e.NewValue)
        {
            PreviousOverride = previousOverride;
            NewOverride = newOverride;
            ItemId = itemId;
        }

        public OverrideType PreviousOverride { get; }

        public OverrideType NewOverride { get; }

        public ItemId ItemId { get; }
    }
}
