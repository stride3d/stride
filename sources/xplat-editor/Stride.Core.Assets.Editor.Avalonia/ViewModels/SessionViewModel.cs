// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.ViewModels;

public sealed class SessionViewModel : DispatcherViewModel
{
    private PackageSession? session;

    public SessionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public PackageSession? Session
    {
        get => session;
        set => SetValue(ref session, value);
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

        var viewModel = new SessionViewModel(serviceProvider)
        {
            Session = result.Session
        };
        return viewModel;
    }
}
