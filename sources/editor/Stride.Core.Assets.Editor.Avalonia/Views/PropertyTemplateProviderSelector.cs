// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Avalonia.Controls;
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

            var shouldClear = true;
            if (lastControls.TryGetValue(node, out var lastControl) && lastControl.IsAlive)
            {
                shouldClear = false;
            }
            // In any other case, we clear the list of used providers.
            if (shouldClear)
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
                lastControls.Add(node, new WeakReference(newControl));
                return newControl;
            }

            return null;
        }

        return base.Build(item);
    }
}
