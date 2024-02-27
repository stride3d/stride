// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public class PackageCategoryViewModel : CategoryViewModel<PackageViewModel>, IChildViewModel
{
    public PackageCategoryViewModel(string name, ISessionViewModel session, IComparer<PackageViewModel> childComparer = null)
        : base(name, session, childComparer)
    {
    }

    IChildViewModel? IChildViewModel.GetParent()
    {
        return null;
    }

    string IChildViewModel.GetName()
    {
        return Name;
    }
}
