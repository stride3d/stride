// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Components.FixReferences;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// What the WPF copies of the core services did and the agnostic copies took over: argument checks, the node presenters of a
/// properties view model, a type registered on an open generic base.
/// </summary>
public sealed class TestCoreServices
{
    private sealed class TestPropertiesViewModel(IViewModelServiceProvider serviceProvider, NodeContainer nodeContainer)
        : PropertiesViewModel(serviceProvider, nodeContainer)
    {
        protected override string EmptySelectionFallbackMessage => string.Empty;

        protected override bool CanDisplaySelectedObjects(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, out string? fallbackMessage)
        {
            fallbackMessage = null;
            return true;
        }

        protected override void FeedbackException(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, Exception exception, out string? fallbackMessage)
        {
            fallbackMessage = null;
        }
    }

    private sealed class TestItemViewModel : ViewModelBase;

    private sealed class TestReferenceReplacementViewModel(FixReferencesViewModel<TestItemViewModel> fixReferences, TestItemViewModel objectToFix, TestItemViewModel referencer)
        : ReferenceReplacementViewModel<TestItemViewModel>(fixReferences, objectToFix, referencer, typeof(TestItemViewModel))
    {
        public override void ClearReference()
        {
        }

        protected override void ReplaceReference()
        {
        }
    }

    private readonly ViewModelServiceProvider serviceProvider = new([new NullDispatcherService()]);

    [Fact]
    public void TheCopyPasteServiceChecksItsArguments()
    {
        var service = new CopyPasteService();

        Assert.Throws<ArgumentNullException>(() => service.CopyFromAssets(null!, typeof(object)));
        Assert.Throws<ArgumentNullException>(() => service.CopyFromAssets([], null!));
        Assert.Throws<ArgumentNullException>(() => service.CanPaste("text", null!, typeof(object)));
        Assert.Throws<ArgumentNullException>(() => service.DeserializeCopiedData("text", null!, typeof(object)));
        Assert.Throws<ArgumentNullException>(() => service.RegisterProcessor((ICopyProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => service.RegisterProcessor((IPasteProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => service.RegisterProcessor((IAssetPostPasteProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => service.UnregisterProcessor((ICopyProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => service.UnregisterProcessor((IPasteProcessor)null!));
        Assert.Throws<ArgumentNullException>(() => service.UnregisterProcessor((IAssetPostPasteProcessor)null!));
    }

    [Fact]
    public void TheSelectionServiceChecksTheScopeItRegisters()
    {
        var service = new SelectionService(new NullDispatcherService());
        var collection = new ObservableList<object>();

        Assert.Throws<ArgumentNullException>(() => service.RegisterSelectionScope(null!, _ => null, collection));
        Assert.Throws<ArgumentNullException>(() => service.RegisterSelectionScope(_ => null, null!, collection));
        Assert.Throws<ArgumentNullException>(() => service.RegisterSelectionScope(_ => null, _ => null, (INotifyCollectionChanged[])null!));
    }

    [Fact]
    public void AReferenceReplacementNeedsItsFixReferencesViewModel()
    {
        var item = new TestItemViewModel();

        Assert.Throws<ArgumentNullException>(() => new TestReferenceReplacementViewModel(null!, item, item));
    }

    [Fact]
    public void APropertiesViewModelNeedsANodeContainerAndListsItsNodePresenters()
    {
        Assert.Throws<ArgumentNullException>(() => new TestPropertiesViewModel(serviceProvider, null!));

        var properties = new TestPropertiesViewModel(serviceProvider, new AssetNodeContainer());
        var command = new MoveItemCommand();
        properties.RegisterNodePresenterCommand(command);

        Assert.Contains(command, properties.NodePresenterCommands);
        Assert.Contains(properties.NodePresenterCommands, x => x is AddNewItemCommand);
        Assert.NotEmpty(properties.NodePresenterUpdaters);
        properties.UnregisterNodePresenterCommand(command);
        Assert.DoesNotContain(command, properties.NodePresenterCommands);
    }
}
