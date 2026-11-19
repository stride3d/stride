// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The slider of a number with a range sets the member once per slide or key press: one change to undo.
/// </summary>
public sealed class RangedSliderTests
{
    [AvaloniaFact]
    public void ASlideSetsTheMemberOnceOnRelease()
    {
        var (window, slider, box, holder) = Show(2);

        window.MouseDown(At(window, slider, 0.5), MouseButton.Left);
        window.MouseMove(At(window, slider, 0.6));
        window.MouseMove(At(window, slider, 0.8));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(slider.Value, box.Value);
        Assert.Equal(0, holder.Sets);

        window.MouseUp(At(window, slider, 0.8), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, holder.Sets);
        Assert.Equal((float)slider.Value, holder.Ranged);
        Assert.InRange(holder.Ranged, 7, 9);
        window.Close();
    }

    [AvaloniaFact]
    public void EachKeyPressSetsTheMemberOnce()
    {
        var (window, slider, _, holder) = Show(2);
        slider.Focus();

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(4f, holder.Ranged);
        Assert.Equal(2, holder.Sets);
        window.Close();
    }

    private static (Window Window, Slider Slider, NumericTextBox Box, RangedHolder Holder) Show(float value)
    {
        DefaultProviders.Register();
        var holder = new RangedHolder { Ranged = value };
        holder.ResetSets();
        var node = new NodeViewModelContext(new RangeUpdater()).MemberOf(holder, nameof(RangedHolder.Ranged));
        var window = new Window { Width = 500, Height = 200, Content = new ContentControl { Content = node, ContentTemplate = PropertyViewHelper.EditorProviders } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var slider = Assert.Single(window.GetVisualDescendants().OfType<Slider>());
        Assert.Equal(value, slider.Value);
        return (window, slider, Assert.Single(window.GetVisualDescendants().OfType<NumericTextBox>()), holder);
    }

    private static Point At(Window window, Slider slider, double fraction)
    {
        return slider.TranslatePoint(new Point(slider.Bounds.Width * fraction, slider.Bounds.Height / 2), window)!.Value;
    }

    // What NumericValueNodeUpdater does for the member of an asset
    private sealed class RangeUpdater : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node)
        {
            if (node.Parent?.Type == typeof(RangedHolder))
                new NumericValueNodeUpdater().UpdateNode(node, typeof(RangedHolder).GetProperty(node.Name)!);
        }

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class RangedHolder
{
    /// <summary>
    /// The number of times the member was set.
    /// </summary>
    public int Sets { get; private set; }

    [DataMemberRange(0, 10, 1, 1, 0)]
    public float Ranged
    {
        get;
        set
        {
            field = value;
            Sets++;
        }
    }

    public void ResetSets() => Sets = 0;
}
