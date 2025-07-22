// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// Abstract base class that represents a package being referenced by another one.
/// </summary>
public abstract class PackageReferenceViewModel : SessionObjectViewModel, IComparable<PackageReferenceViewModel>
{
    private readonly DependencyCategoryViewModel dependencies;

    protected PackageReferenceViewModel(PackageViewModel referencer, DependencyCategoryViewModel dependencies)
        : base(referencer.SafeArgument(nameof(referencer)).Session)
    {
        this.dependencies = dependencies;
        Referencer = referencer;
    }

    /// <summary>
    /// Gets the referencer package of this package reference.
    /// </summary>
    public PackageViewModel Referencer { get; }

    /// <summary>
    /// Gets the target package of this package reference.
    /// </summary>
    public PackageViewModel? Target { get; protected set; }

    public override string TypeDisplayName => "Package Reference";

    public override IEnumerable<IDirtiable> Dirtiables => dependencies.Dirtiables;

    public override bool IsEditable => Referencer.IsEditable;

    /// <inheritdoc/>
    public int CompareTo(PackageReferenceViewModel? other)
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
