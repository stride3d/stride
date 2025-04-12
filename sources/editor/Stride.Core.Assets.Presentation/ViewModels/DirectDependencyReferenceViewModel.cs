// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public class DirectDependencyReferenceViewModel : PackageReferenceViewModel
{
    private readonly DependencyRange dependency;

    public DirectDependencyReferenceViewModel(DependencyRange dependency, PackageViewModel referencer, DependencyCategoryViewModel dependencies, bool canUndoRedoCreation)
        : base(referencer, dependencies)
    {
        this.dependency = dependency;
        InitialUndelete(canUndoRedoCreation);
    }

    public override string Name
    {
        get => dependency.Name;
        set => throw new InvalidOperationException("The name of a package reference cannot be set");
    }

    public override void AddReference()
    {
        if (!Referencer.Package.Container.DirectDependencies.Contains(dependency))
        {
            Referencer.Package.Container.DirectDependencies.Add(dependency);
        }
    }

    public override void RemoveReference()
    {
        Referencer.Package.Container.DirectDependencies.Remove(dependency);
    }
}
