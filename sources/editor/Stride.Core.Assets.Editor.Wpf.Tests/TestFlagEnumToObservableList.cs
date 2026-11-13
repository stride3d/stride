// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Stride.Core.Assets.Editor.View.ValueConverters;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Xunit;

#nullable enable

namespace Stride.Core.Assets.Editor.Wpf.Tests
{
    public sealed class TestFlagEnumToObservableList
    {
        [Flags]
        public enum Options
        {
            None = 0,
            A = 1,
            B = 2,
            AB = A | B,
            C = 4,
        }

        [DataContract]
        public class Container
        {
            public Options Flags { get; set; }
        }

        [Theory]
        [InlineData(Options.A, Options.B, true, Options.AB)]
        [InlineData(Options.None, Options.AB, true, Options.AB)]
        [InlineData(Options.AB, Options.A, false, Options.B)]
        [InlineData(Options.AB | Options.C, Options.AB, false, Options.C)]
        public void ToggleFlagUpdatesValueAndSelection(Options initial, Options flag, bool add, Options expected)
        {
            var container = new Container { Flags = initial };
            var binding = new PropertyGridBinding(container, nameof(Container.Flags));
            Assert.Equal(initial.GetAllFlags(), binding.SelectedItems.Cast<Enum>());

            if (add)
                binding.SelectedItems.Add(flag);
            else
                binding.SelectedItems.Remove(flag);

            Assert.Equal(expected, container.Flags);
            Assert.Equal(expected.GetAllFlags(), binding.SelectedItems.Cast<Enum>());
        }

        /// <summary>
        /// Runs the converter again when the node value changes, as the multi-binding of the property grid does, and adds a second
        /// handler to the collection, as the selected items behavior of the list box does.
        /// </summary>
        private sealed class PropertyGridBinding
        {
            private readonly NodeViewModel node;

            public PropertyGridBinding(object instance, string memberName)
            {
                var nodeContainer = new NodeContainer();
                var serviceProvider = new ViewModelServiceProvider();
                serviceProvider.RegisterService(new NullDispatcherService());
                serviceProvider.RegisterService(new GraphViewModelService(nodeContainer));
                var graph = GraphViewModel.Create(serviceProvider, [new PropertyProvider(nodeContainer.GetOrCreateNode(instance))])!;
                node = graph.RootNode.GetChild(memberName)!;
                node.PropertyChanged += NodePropertyChanged;
                SelectedItems = Convert();
            }

            public ObservableList<object> SelectedItems { get; private set; }

            private ObservableList<object> Convert()
            {
                var list = (ObservableList<object>)new FlagEnumToObservableList().Convert([node, node.NodeValue!], typeof(IObservableList<object>), null!, CultureInfo.InvariantCulture);
                list.CollectionChanged += SelectedItemsChanged;
                return list;
            }

            private void NodePropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != nameof(NodeViewModel.NodeValue))
                    return;

                SelectedItems.CollectionChanged -= SelectedItemsChanged;
                SelectedItems = Convert();
            }

            private void SelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
            }
        }

        private sealed class PropertyProvider(IObjectNode rootNode) : IPropertyProviderViewModel
        {
            public bool CanProvidePropertiesViewModel => true;

            public IObjectNode GetRootNode() => rootNode;

            public bool ShouldConstructMember(IMemberNode member) => true;

            public bool ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
        }
    }
}
