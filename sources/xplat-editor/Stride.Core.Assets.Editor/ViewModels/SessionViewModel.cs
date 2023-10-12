// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class SessionViewModel : ViewModelBase
{
    private PackageSession? session;

    public PackageSession? Session
    {
        get => session;
        set => SetProperty(ref session, value);
    }

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

        var viewModel = new SessionViewModel
        {
            Session = result.Session
        };
        return viewModel;
    }
}
