// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class PropertyTemplateProviderSelector : TemplateProviderSelector
{
    private readonly ConditionalWeakTable<NodeViewModel, List<string>> usedProviders = new();
    private readonly ConditionalWeakTable<NodeViewModel, WeakReference> lastControls = new();

    public override Control? Build(object? item)
    {
        if (item is NodeViewModel node)
        {
            var usedProvidersForItem = usedProviders.GetOrCreateValue(node);

            // The last control built for this node is still in the window: the request comes from a template of a provider that
            // falls back on the next one inside it, so the providers
            // already used are kept. In any other case, we clear the list of used providers.
            if (!lastControls.TryGetValue(node, out var lastControl) || !lastControl.IsAlive)
            {
                usedProvidersForItem.Clear();
            }
            lastControls.Remove(node);

            var availableSelectors = templateProviders.Where(x => x.Match(item)).ToList();
            var provider = availableSelectors.FirstOrDefault(x => !usedProvidersForItem.Contains(x.Name));
            if (provider != null)
            {
                usedProvidersForItem.Add(provider.Name);
                var newControl = provider.Template.Build(item);
                if (newControl is not null)
                {
                    lastControls.Add(node, new WeakReference(newControl));
                    newControl.DetachedFromLogicalTree += OnDetached;
                }
                return newControl;

                // A control that leaves the window is no longer the last one: when the property moves (its panel is moved, or the
                // layout is reset), its presenters build their templates again, from the first provider
                void OnDetached(object? sender, LogicalTreeAttachmentEventArgs e)
                {
                    newControl.DetachedFromLogicalTree -= OnDetached;
                    if (lastControls.TryGetValue(node, out var last) && last.Target == newControl)
                        lastControls.Remove(node);
                }
            }

            return null;
        }

        return base.Build(item);
    }
}
