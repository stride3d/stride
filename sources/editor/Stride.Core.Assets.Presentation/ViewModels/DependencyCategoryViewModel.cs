// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class DependencyCategoryViewModel : CategoryViewModel<PackageViewModel, PackageReferenceViewModel>, IChildViewModel
{
    public DependencyCategoryViewModel(string name, PackageViewModel parent, ISessionViewModel session, RootAssetCollection packageRootAssets, IComparer<PackageReferenceViewModel>? childComparer = null)
        : base(name, parent, session, childComparer)
    {
    }

    public override IEnumerable<IDirtiable> Dirtiables => base.Dirtiables.Concat(Parent.Dirtiables);

    IChildViewModel IChildViewModel.GetParent()
    {
        return Parent;
    }

    string IChildViewModel.GetName()
    {
        return Name;
    }
}
