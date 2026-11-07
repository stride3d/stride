// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The editor of a float in the property grid shows no float noise (0.64999997 for 0.65), and steps by the member's steps.
/// </summary>
public sealed class FloatEditorTests
{
    [AvaloniaFact]
    public void AFloatIsShownWithSixDecimalPlacesByDefault()
    {
        // No updater gives the node a minimum or a maximum, as for the items of a collection
        var (window, box) = ShowEditor(new FloatHolder { Value = 0.65f }, nameof(FloatHolder.Value));

        Assert.Equal("0.65", Text(box));
        Assert.Equal(6, box.DecimalPlaces);
        // The missing keys give the fallbacks, not 0 that would clamp the value and make the arrows do nothing
        Assert.Equal(double.MinValue, box.Minimum);
        Assert.Equal(double.MaxValue, box.Maximum);
        Assert.Equal(1, box.SmallChange);
        Assert.Equal(10, box.LargeChange);
        window.Close();
    }

    [AvaloniaFact]
    public void TheMemberDecimalPlacesAndStepsApply()
    {
        var holder = new FloatHolder { Ranged = 0.123f };
        var (window, box) = ShowEditor(holder, nameof(FloatHolder.Ranged), new RangeUpdater());

        Assert.Equal("0.12", Text(box));
        Assert.Equal(0.25, box.SmallChange);
        Assert.Equal(2, box.LargeChange);

        // The arrow commits the stepped value to the property at once
        box.GetVisualDescendants().OfType<TextBox>().Single().Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.373f, holder.Ranged, 3);
        window.Close();
    }

    private static (Window Window, NumericTextBox Box) ShowEditor(FloatHolder holder, string member, params INodePresenterUpdater[] updaters)
    {
        DefaultProviders.Register();
        var node = new NodeViewModelContext(updaters).MemberOf(holder, member);
        var window = new Window { Content = new ContentControl { Content = node, ContentTemplate = PropertyViewHelper.EditorProviders } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, Assert.Single(window.GetVisualDescendants().OfType<NumericTextBox>()));
    }

    private static string? Text(NumericTextBox box) => box.GetVisualDescendants().OfType<TextBox>().First().Text;

    // What NumericValueNodeUpdater does for the member of an asset
    private sealed class RangeUpdater : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node)
        {
            if (node.Parent?.Type == typeof(FloatHolder))
                new NumericValueNodeUpdater().UpdateNode(node, typeof(FloatHolder).GetProperty(node.Name)!);
        }

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class FloatHolder
{
    public float Value { get; set; }

    [DataMemberRange(0, 1, 0.25, 2, 2)]
    public float Ranged { get; set; }
}
