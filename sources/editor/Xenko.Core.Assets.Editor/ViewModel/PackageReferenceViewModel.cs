// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Dirtiables;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Abstract base class that represents a package being referenced by another one.
    /// </summary>
    public abstract class PackageReferenceViewModel : SessionObjectViewModel, IComparable<PackageReferenceViewModel>
    {
        private readonly DependencyCategoryViewModel dependencies;

        protected PackageReferenceViewModel(PackageViewModel target, PackageViewModel referencer, DependencyCategoryViewModel dependencies)
            : base(target.SafeArgument(nameof(target)).Session)
        {
            this.dependencies = dependencies;
            Referencer = referencer;
            Target = target;
        }

        /// <summary>
        /// Gets the referencer package of this package reference.
        /// </summary>
        public PackageViewModel Referencer { get; }

        /// <summary>
        /// Gets the target package of this package reference.
        /// </summary>
        public PackageViewModel Target { get; }

        public override string TypeDisplayName => "Package Reference";

        public override IEnumerable<IDirtiable> Dirtiables => dependencies.Dirtiables;

        public override bool IsEditable => Referencer.IsEditable && Target.IsEditable;

        /// <inheritdoc/>
        public int CompareTo(PackageReferenceViewModel other)
        {
            return other != null ? string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) : -1;
        }

        public abstract void AddReference();

        public abstract void RemoveReference();

        public void Delete()
        {
            IsDeleted = true;
        }

        protected override void UpdateIsDeletedStatus()
        {
            if (IsDeleted)
            {
                dependencies.Content.Remove(this);
                RemoveReference();
            }
            else
            {
                dependencies.Content.Add(this);
                AddReference();
            }
        }
    }

    /// <summary>
    /// Implementation of the <see cref="PackageReferenceViewModel"/> for local package references.
    /// </summary>
    public class LocalPackageReferenceViewModel : PackageReferenceViewModel
    {
        private readonly PackageReference reference;

        /// <summary>
        /// Creates a new instance of <see cref="LocalPackageReferenceViewModel"/>.
        /// </summary>
        /// <param name="reference">The package reference asset.</param>
        /// <param name="target">The target of the reference.</param>
        /// <param name="referencer">The referencer.</param>
        /// <param name="dependencies">The dependencies.</param>
        /// <param name="canUndoRedoCreation">Indicates whether the creation of this view model should create a transaction in the undo/redo service</param>
        public LocalPackageReferenceViewModel(PackageReference reference, PackageViewModel target, PackageViewModel referencer, DependencyCategoryViewModel dependencies, bool canUndoRedoCreation)
            : base(target, referencer, dependencies)
        {
            this.reference = reference;
            InitialUndelete(canUndoRedoCreation);
        }

        /// <summary>
        /// Gets the name of the referenced package.
        /// </summary>
        public override string Name
        {
            get { return reference.Location.GetFileNameWithoutExtension(); }
            set { throw new InvalidOperationException("The name of a package reference cannot be set"); }
        }

        public override void AddReference()
        {
            if (!Referencer.Package.LocalDependencies.Contains(reference))
            {
                Referencer.Package.LocalDependencies.Add(reference);
            }
        }

        public override void RemoveReference()
        {
            Referencer.Package.LocalDependencies.Remove(reference);
        }
    }

    /// <summary>
    /// Implementation of the <see cref="PackageReferenceViewModel"/> for store package references.
    /// </summary>
    public class StorePackageReferenceViewModel : PackageReferenceViewModel
    {
        private readonly PackageDependency dependency;

        /// <summary>
        /// Creates a new instance of <see cref="StorePackageReferenceViewModel"/>.
        /// </summary>
        /// <param name="dependency">The package dependency asset.</param>
        /// <param name="target">The target of the reference.</param>
        /// <param name="referencer">The referencer.</param>
        /// <param name="dependencies">The dependencies.</param>
        /// <param name="canUndoRedoCreation">Indicates whether the creation of this view model should create a transaction in the undo/redo service</param>
        public StorePackageReferenceViewModel(PackageDependency dependency, PackageViewModel target, PackageViewModel referencer, DependencyCategoryViewModel dependencies, bool canUndoRedoCreation)
            : base(target, referencer, dependencies)
        {
            this.dependency = dependency;
            InitialUndelete(canUndoRedoCreation);
        }

        /// <summary>
        /// Gets the name of the referenced package.
        /// </summary>
        public override string Name
        {
            get { return dependency.Name; }
            set { throw new InvalidOperationException("The name of a package reference cannot be set"); }
        }

        public override void AddReference()
        {
            if (!Referencer.Package.Meta.Dependencies.Contains(dependency))
            {
                Referencer.Package.Meta.Dependencies.Add(dependency);
            }
        }

        public override void RemoveReference()
        {
            Referencer.Package.Meta.Dependencies.Remove(dependency);
        }
    }
}
