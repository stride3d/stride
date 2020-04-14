// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// Redirects drag and drop to one or several view models implementing <see cref="IAddChildrenPropertiesProviderViewModel"/>.
    /// </summary>
    public class PropertyViewDragDropBehavior : DragDropBehavior<PropertyView, FrameworkElement>
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(GraphViewModel), typeof(PropertyViewDragDropBehavior));

        /// <summary>
        /// Gets or sets the drop view model target.
        /// </summary>
        public GraphViewModel Target { get { return (GraphViewModel)GetValue(TargetProperty); } set { SetValue(TargetProperty, value); } }

        /// <inheritdoc/>
        protected override IEnumerable<object> GetItemsToDrag(FrameworkElement container)
        {
            return Enumerable.Empty<object>();
        }

        /// <inheritdoc/>
        protected override IAddChildViewModel GetDropTargetItem(FrameworkElement container)
        {
            if (Target == null)
                return null;

            // Recurse through view models to find their property providers (they will be stored in child for view models that are combined)
            var propertyProviders = new List<IAddChildrenPropertiesProviderViewModel>();
            propertyProviders.AddRange(Target.RootNode.NodePresenters.Select(x => x.PropertyProvider).OfType<IAddChildrenPropertiesProviderViewModel>());

            if (propertyProviders.Count == 0)
                return null;

            return new AddChildrenPropertiesProviderViewModelWrapper(Target.ServiceProvider, propertyProviders);
        }

        /// <summary>
        /// Helper class to forward <see cref="IAddChildViewModel"/> to one or multiple <see cref="IAddChildrenPropertiesProviderViewModel"/>.
        /// </summary>
        class AddChildrenPropertiesProviderViewModelWrapper : IAddChildViewModel
        {
            private readonly IViewModelServiceProvider serviceProvider;
            private readonly List<IAddChildrenPropertiesProviderViewModel> propertyProviders;

            public AddChildrenPropertiesProviderViewModelWrapper(IViewModelServiceProvider serviceProvider, List<IAddChildrenPropertiesProviderViewModel> propertyProviders)
            {
                if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
                if (propertyProviders.Count == 0)
                    throw new ArgumentException(nameof(propertyProviders));

                this.serviceProvider = serviceProvider;
                this.propertyProviders = propertyProviders;
            }

            /// <inheritdoc/>
            public bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
            {
                var messageBuilder = new StringBuilder();

                foreach (var propertyProvider in propertyProviders)
                {
                    if (!propertyProvider.CanAddChildren(children, modifiers, out message))
                        return false;

                    messageBuilder.AppendLine(message);
                }

                // Combined message
                message = messageBuilder.ToString();

                return true;
            }

            /// <inheritdoc/>
            public void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
            {
                var actionService = serviceProvider.Get<IUndoRedoService>();
                using (var transaction = actionService.CreateTransaction())
                {
                    foreach (var propertyProvider in propertyProviders)
                    {
                        propertyProvider.AddChildren(children, modifiers);
                    }

                    // TODO: If there is only a single transaction, take its name instead
                    actionService.SetName(transaction, "Add new component(s)");
                }
            }
        }
    }
}
