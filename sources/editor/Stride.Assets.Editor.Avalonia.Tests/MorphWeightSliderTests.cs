// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// A morph weight slider shows the dragged weight in the scene and edits the asset once, on release.
/// </summary>
public sealed class MorphWeightSliderTests
{
    [AvaloniaFact]
    public void ADragPreviewsTheWeightsAndAppliesTheLastOneOnRelease()
    {
        var previews = new List<double>();
        var holder = new WeightHolder();
        var node = new NodeViewModelContext(new SliderUpdater(previews)).MemberOf(holder, nameof(WeightHolder.Smile___MorphWeight));
        var provider = new EntityPropertyTemplateProviders().Values.OfType<MorphWeightTemplateProvider>().Single();
        Assert.True(provider.Match(node));
        var window = new Window { Width = 400, Height = 100, Content = provider.Template.Build(node) };
        ((Control)window.Content).DataContext = node;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var slider = window.GetVisualDescendants().OfType<Slider>().Single();
        var origin = slider.TranslatePoint(new Point(0, slider.Bounds.Height / 2), window)!.Value;
        var center = origin + new Point(slider.Bounds.Width / 2, 0);

        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseMove(center + new Point(slider.Bounds.Width / 4, 0));
        Dispatcher.UIThread.RunJobs();
        // The asset is not edited during the drag, the scene shows the weight
        Assert.Equal(0f, holder.Smile___MorphWeight);
        Assert.NotEmpty(previews);
        window.MouseMove(center + new Point(slider.Bounds.Width * 3 / 8, 0));
        window.MouseUp(center + new Point(slider.Bounds.Width * 3 / 8, 0), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(holder.Smile___MorphWeight > 0.5f, $"weight {holder.Smile___MorphWeight}");
        Assert.Equal((float)slider.Value, holder.Smile___MorphWeight, 3);
        Assert.Equal(slider.Value, previews.Last(), 3);
        window.Close();
    }

    // What ModelComponentNodeUpdater sets on a weight slider
    private sealed class SliderUpdater(List<double> previews) : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node)
        {
            if (node.Type != typeof(float))
                return;

            node.AttachedProperties.Set(NumericData.MinimumKey, -1f);
            node.AttachedProperties.Set(NumericData.MaximumKey, 1f);
            node.AttachedProperties.Set(ModelComponentData.MorphWeightPreviewKey, previews.Add);
        }

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class WeightHolder
{
    // The name of a weight slider node ends with ModelComponentData.MorphWeightNodeNameSuffix
    public float Smile___MorphWeight { get; set; }
}
