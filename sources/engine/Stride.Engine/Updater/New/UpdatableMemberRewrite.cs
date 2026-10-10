using System.Collections.Generic;

namespace Stride.Updater.New;

internal readonly record struct UpdatableMemberRewrite<TParent>(
    UpdatableMember<TParent> Member,
    IReadOnlyList<UpdatableMember<TParent>> PromotedMembers)
{
}
