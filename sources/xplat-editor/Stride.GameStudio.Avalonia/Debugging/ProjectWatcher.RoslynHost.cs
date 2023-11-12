// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Composition.Hosting;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Stride.GameStudio.Avalonia.Debugging;

internal sealed partial class ProjectWatcher
{
    private readonly Lazy<Task<RoslynHost>> roslynHost = new(GetRoslynHostAsync);

    private static Task<RoslynHost> GetRoslynHostAsync()
    {
        return Task.Run(() =>
        {
            var host = new RoslynHost();
            return host;
        });
    }

    // TODO: refactor/move this when we have a script editor
    internal sealed class RoslynHost
    {
        private readonly CompositionHost compositionContext;
        private readonly MefHostServices hostServices;
        
        public RoslynHost()
        {
            compositionContext = CreateCompositionContext();

            // Create MEF host services
            hostServices = MefHostServices.Create(compositionContext);
        }

        public MefHostServices HostServices => hostServices;

        private static CompositionHost CreateCompositionContext()
        {
            var assemblies = new[]
            {
                Assembly.Load("Microsoft.CodeAnalysis.Workspaces"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces"),
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.Workspaces.MSBuild"),
                // FIXME xplat-editor ideally those dependencies should be hard-coded here
                //typeof(IRoslynHost).Assembly, // RoslynPad.Roslyn
                //typeof(SymbolDisplayPartExtensions).Assembly, // RoslynPad.Roslyn.Windows
                //typeof(AvalonEditTextContainer).Assembly, // RoslynPad.Editor.Windows
            };

            var partTypes = assemblies
                .SelectMany(x => x.DefinedTypes)
                .Select(x => x.AsType());

            return new ContainerConfiguration()
                .WithParts(partTypes)
                .CreateContainer();
        }
    }
}
