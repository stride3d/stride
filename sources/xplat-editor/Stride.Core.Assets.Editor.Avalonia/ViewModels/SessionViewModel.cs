// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.ViewModels;

public sealed class SessionViewModel : DispatcherViewModel, ISessionViewModel
{
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly PackageSession session;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session)
        : base(serviceProvider)
    {
        this.session = session;
        this.session.Projects.ForEach(x =>
        {
            var package = CreateProjectViewModel(x, true);
            AllPackages.Add(package);
        });
    }

    public ObservableList<PackageViewModel> AllPackages { get; } = [];

    public static async Task<SessionViewModel> OpenSessionAsync(UFile path, IViewModelServiceProvider serviceProvider, CancellationToken token = default)
    {
        // TODO register a bunch of services
        //serviceProvider.RegisterService(new CopyPasteService());

        var result = await Task.Run(
            () => PackageSession.Load(path, new PackageLoadParameters
            {
                CancelToken = token,
                AutoCompileProjects = false,
                AutoLoadTemporaryAssets = false,
                LoadAssemblyReferences = false,
                LoadMissingDependencies = false
            }), token);

        var viewModel = new SessionViewModel(serviceProvider, result.Session);
        return viewModel;
    }

    private PackageViewModel CreateProjectViewModel(PackageContainer packageContainer, bool packageAlreadyInSession)
    {
        switch (packageContainer)
        {
            case SolutionProject project:
                {
                    var packageContainerViewModel = new ProjectViewModel(this, project);
                    packageMap.Add(packageContainerViewModel, project);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(project);
                    return packageContainerViewModel;
                }
            case StandalonePackage standalonePackage:
                {
                    var packageContainerViewModel = new PackageViewModel(this, standalonePackage);
                    packageMap.Add(packageContainerViewModel, standalonePackage);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(standalonePackage);
                    return packageContainerViewModel;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(packageContainer));
        }
    }
}
