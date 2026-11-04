// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

public class DebugAssetNodeViewModel : DispatcherViewModel
{
    public const string Null = "(NULL)";

    protected readonly IGraphNode? Node;

    public DebugAssetNodeViewModel(IViewModelServiceProvider serviceProvider, IGraphNode? node)
        : base(serviceProvider)
    {
        Node = node;
        BreakCommand = new AnonymousCommand(ServiceProvider, Break);
    }

    public string Name => (Node as IMemberNode)?.Name ?? Node?.Type.Name ?? Null;

    public string Value => Node?.Retrieve()?.ToString() ?? Null;

    public string ContentType => GetContentType();

    public Type? Type => Node?.Type;

    public ICommandBase BreakCommand { get; }

    private string GetContentType()
    {
        return Node switch
        {
            IMemberNode => "Member",
            BoxedNode => "Object (boxed)",
            IObjectNode => "Object",
            _ => "Unknown"
        };
    }

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void Break()
    {
        if (Debugger.IsAttached)
            Debugger.Break();
    }
}
