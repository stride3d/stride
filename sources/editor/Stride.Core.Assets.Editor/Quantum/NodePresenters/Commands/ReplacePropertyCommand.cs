// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public class ReplacePropertyCommand : PastePropertyCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "ReplaceProperty";

    /// <inheritdoc />
    public override string Name => CommandName;

    /// <inheritdoc />
    public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

    /// <inheritdoc />
    public override Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        return DoPasteAsync(nodePresenter, true);
    }
}
