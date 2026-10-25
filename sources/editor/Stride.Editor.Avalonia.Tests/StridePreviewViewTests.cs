// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Avalonia.Tests.Helpers;

namespace Stride.Editor.Avalonia.Tests;

public sealed class StridePreviewViewTests
{
    [AvaloniaFact]
    public void TheTemplateHostsTheViewOfTheBuilder()
    {
        var gameView = new Border();
        var view = new StridePreviewView();
        var window = new Window { Content = view };
        view.InitializeView(new FakePreviewBuilder(gameView), new FakeAssetPreview());

        window.Show();

        var presenter = Assert.Single(view.GetVisualDescendants().OfType<ContentPresenter>().Where(x => x.Name == StridePreviewView.PartStrideView));
        Assert.Same(gameView, presenter.Content);
        Assert.Same(presenter, gameView.GetVisualParent());
    }

    [AvaloniaFact]
    public void TheViewOfANewPreviewTakesTheGameViewFromThePreviousOne()
    {
        // The previews share the game view: selecting an asset of another type replaces the view while the old one still
        // holds the game view
        var gameView = new Border();
        var builder = new FakePreviewBuilder(gameView);
        var previousView = new StridePreviewView();
        previousView.InitializeView(builder, new FakeAssetPreview());
        var window = new Window { Content = previousView };
        window.Show();

        var nextView = new StridePreviewView();
        nextView.InitializeView(builder, new FakeAssetPreview());
        window.Content = nextView;
        Dispatcher.UIThread.RunJobs();

        var presenter = Assert.Single(nextView.GetVisualDescendants().OfType<ContentPresenter>().Where(x => x.Name == StridePreviewView.PartStrideView));
        Assert.Same(gameView, presenter.Content);
        Assert.Same(presenter, gameView.GetVisualParent());
    }

    [AvaloniaFact]
    public void InitializingTheViewAttachesThePreviewToItsViewModel()
    {
        var viewModel = new FakePreviewViewModel();
        var preview = new FakeAssetPreview { PreviewViewModel = viewModel };
        var view = new StridePreviewView();

        view.InitializeView(new FakePreviewBuilder(new Border()), preview);

        Assert.Equal([preview], viewModel.Attached);
        Assert.Same(viewModel, view.DataContext);
    }

    [AvaloniaFact]
    public void TheViewReportsItsAttachmentOnceLoaded()
    {
        var preview = new FakeAssetPreview();
        var view = new StridePreviewView();
        var window = new Window { Content = view };
        view.InitializeView(new FakePreviewBuilder(new Border()), preview);
        Assert.Equal(0, preview.ViewAttachedCount);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, preview.ViewAttachedCount);
    }

    [AvaloniaFact]
    public void TheLaidOutSizeIsThatOfTheGameViewInPixels()
    {
        var gameView = new Border { Width = 120, Height = 80 };
        var view = new StridePreviewView();
        var window = new Window { Width = 300, Height = 200, Content = view };
        view.InitializeView(new FakePreviewBuilder(gameView), new FakeAssetPreview());

        // Not laid out yet
        Assert.Null(view.GetLaidOutViewSize());

        window.Show();

        Assert.Equal(new Vector2(120, 80), view.GetLaidOutViewSize());
    }

    [AvaloniaFact]
    public void TheSizeIsUnknownWithoutAGameView()
    {
        var view = new StridePreviewView();
        var window = new Window { Content = view };
        view.InitializeView(new FakePreviewBuilder(null), new FakeAssetPreview());
        window.Show();

        Assert.Null(view.GetLaidOutViewSize());
    }

    [AvaloniaFact]
    public void UpdatingTheViewReattachesTheViewModelAndFollowsTheBuilder()
    {
        var viewModel = new FakePreviewViewModel();
        var preview = new FakeAssetPreview { PreviewViewModel = viewModel };
        var builder = new FakePreviewBuilder(new Border());
        var view = new StridePreviewView();
        var window = new Window { Content = view };
        view.InitializeView(builder, preview);
        window.Show();

        // The preview game was recreated (e.g. after a device loss): the view shows the new control
        var newGameView = new Border();
        builder.View = newGameView;
        view.UpdateView(preview);

        Assert.Equal([preview, preview], viewModel.Attached);
        var presenter = Assert.Single(view.GetVisualDescendants().OfType<ContentPresenter>().Where(x => x.Name == StridePreviewView.PartStrideView));
        Assert.Same(newGameView, presenter.Content);
    }
}
