// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The property grid follows a node whose object value is replaced, as a paste or a "Reset to base value" does.
/// </summary>
public sealed class PropertyGridRefreshTests
{
    private static bool providersRegistered;

    [AvaloniaFact]
    public void TheInlinedValueFollowsAReplacedObject()
    {
        RegisterDefaultProviders();
        var context = new NodeViewModelContext(new InlineScalarUpdater());
        var holder = new ScalarHolder();
        var node = context.MemberOf(holder, nameof(ScalarHolder.Scalar));
        var window = new Window { Content = new ContentControl { Content = node, ContentTemplate = PropertyViewHelper.EditorProviders } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("1.5", FindValueText(window));

        // The paste replaces the whole ComputeFloat-like object: the node of its inlined value is a new one
        node.NodeValue = new InlinedScalar { InlinedProperty = 4f };
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("4", FindValueText(window));
        window.Close();
    }

    // The text the user sees in the numeric field of the inlined value
    private static string? FindValueText(Window window)
    {
        var valueBox = Assert.Single(window.GetVisualDescendants().OfType<NumericTextBox>());
        return valueBox.GetVisualDescendants().OfType<TextBox>().First().Text;
    }

    // The editor registers the providers of the plugins the same way (PluginService), once per process
    private static void RegisterDefaultProviders()
    {
        if (providersRegistered)
            return;

        providersRegistered = true;
        foreach (var provider in new DefaultPropertyTemplateProviders().Values.OfType<TemplateProviderBase>())
        {
            var selector = PropertyViewHelper.GetTemplateCategory(provider) switch
            {
                PropertyViewHelper.Category.PropertyHeader => PropertyViewHelper.HeaderProviders,
                PropertyViewHelper.Category.PropertyFooter => PropertyViewHelper.FooterProviders,
                _ => PropertyViewHelper.EditorProviders,
            };
            selector.RegisterTemplateProvider(provider);
        }
    }

    // What InlineMemberNodeUpdater and NumericValueNodeUpdater do for a ComputeFloat, without an asset
    private sealed class InlineScalarUpdater : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node)
        {
            if (node.Type == typeof(InlinedScalar))
                node.AttachedProperties.Set(InlineData.InlineMemberKey, true);
            if (node.Type == typeof(float))
            {
                node.AttachedProperties.Set(NumericData.MinimumKey, float.MinValue);
                node.AttachedProperties.Set(NumericData.MaximumKey, float.MaxValue);
            }
        }

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class ScalarHolder
{
    public InlinedScalar Scalar { get; set; } = new() { InlinedProperty = 1.5f };
}

public sealed class InlinedScalar
{
    public float InlinedProperty { get; set; }
}
