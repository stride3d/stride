// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public interface ICategoryViewModel : IDirtiable, IIsEditableViewModel
    {
        string Name { get; }

        IEnumerable Content { get; }
    }

    public abstract class CategoryViewModel<TChildren> : SessionObjectViewModel, ICategoryViewModel
    {
        protected CategoryViewModel(string name, SessionViewModel session, IComparer<TChildren> childComparer = null)
            : base(session)
        {
            Name = name;
            Content = new SortedObservableCollection<TChildren>(childComparer);
        }

        public sealed override string Name { get; set; }

        public SortedObservableCollection<TChildren> Content { get; }

        public override bool IsEditable => false;

        IEnumerable ICategoryViewModel.Content => Content;

        public override string TypeDisplayName => "Category";

        protected override void UpdateIsDeletedStatus()
        {
            if (IsDeleted)
                throw new InvalidOperationException("A category cannot be deleted");
        }
    }

    public abstract class CategoryViewModel<TParent, TChildren> : CategoryViewModel<TChildren>
    {
        protected CategoryViewModel(string name, TParent parent, SessionViewModel session, IComparer<TChildren> childComparer = null)
            : base(name, session, childComparer)
        {
            Parent = parent;
        }

        public TParent Parent { get; }
    }

    public class PackageCategoryViewModel : CategoryViewModel<PackageViewModel>, IChildViewModel
    {
        public PackageCategoryViewModel(string name, SessionViewModel session, IComparer<PackageViewModel> childComparer = null)
            : base(name, session, childComparer)
        {
        }

        IChildViewModel IChildViewModel.GetParent()
        {
            return null;
        }

        string IChildViewModel.GetName()
        {
            return Name;
        }
    }

    public class DependencyCategoryViewModel : CategoryViewModel<PackageViewModel, PackageReferenceViewModel>, IChildViewModel
    {
        public DependencyCategoryViewModel(string name, PackageViewModel parent, SessionViewModel session, RootAssetCollection packageRootAssets, IComparer<PackageReferenceViewModel> childComparer = null)
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
}
