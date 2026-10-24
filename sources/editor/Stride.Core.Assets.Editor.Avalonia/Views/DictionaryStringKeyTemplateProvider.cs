// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class DictionaryStringKeyTemplateProvider : DictionaryTemplateProvider
{
    public override string Name => "DictionaryStringKey";

    /// <summary>
    /// If set to true, this provider will accept nodes representing entries of a string-keyed dictionary.
    /// Otherwise, it will accept nodes representing the string-keyed dictionary itself.
    /// </summary>
    public bool ApplyForItems { get; set; }

    public override bool MatchNode(NodeViewModel node)
    {
        if (ApplyForItems)
        {
            if (node.Parent is not { } parent)
                return false;

            node = parent;
        }

        if (!base.MatchNode(node))
            return false;

        if (node.AssociatedData.TryGetValue(DictionaryNodeUpdater.DictionaryNodeKeyType.Name, out var value))
        {
            var type = (Type)value;
            return type == typeof(string);
        }

        return false;
    }
}
