// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Microsoft.Xaml.Behaviors;

using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// A <see cref="Behavior"/> that allow to execute a command when the value of a dependency property of its associated 
    /// object changes, or when the source of the dependency property binding is updated.
    /// </summary>
    public class OnPropertyChangedCommandBehavior : Behavior<FrameworkElement>
    {
        private string propertyName;
        private readonly DependencyPropertyWatcher propertyWatcher = new DependencyPropertyWatcher();
        private DependencyProperty dependencyProperty;

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(OnPropertyChangedCommandBehavior));

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(OnPropertyChangedCommandBehavior));
            
        /// <summary>
        /// Identifies the <see cref="ExecuteOnlyOnSourceUpdate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExecuteOnlyOnSourceUpdateProperty = DependencyProperty.Register(nameof(ExecuteOnlyOnSourceUpdate), typeof(bool), typeof(OnPropertyChangedCommandBehavior));

        /// <summary>
        /// Identifies the <see cref="PassValueAsParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PassValueAsParameterProperty = DependencyProperty.Register(nameof(PassValueAsParameter), typeof(bool), typeof(OnPropertyChangedCommandBehavior));
        
        /// <summary>
        /// Gets or sets the name of the dependency property that will trigger the associated command.
        /// </summary>
        /// <remarks>Changing this property after the behavior has been attached will have no effect.</remarks>
        public string PropertyName { get { return propertyName; } set { if (AssociatedObject == null) propertyName = value; } }

        /// <summary>
        /// Gets or sets the command to execute when the property is modified.
        /// </summary>
        public ICommand Command { get { return (ICommand)GetValue(CommandProperty); } set { SetValue(CommandProperty, value); } }

        /// <summary>
        /// Gets or sets the parameter of the command to execute when the property is modified.
        /// </summary>
        public object CommandParameter { get { return GetValue(CommandParameterProperty); } set { SetValue(CommandParameterProperty, value); } }

        /// <summary>
        /// Gets or set whether the command should be executed only when the source of the binding associated to the dependency property is updated.
        /// </summary>
        /// <remarks>If set to <c>true</c>, this property requires that a binding exists on the dependency property and that it has <see cref="Binding.NotifyOnSourceUpdated"/> set to <c>true</c>.</remarks>
        public bool ExecuteOnlyOnSourceUpdate { get { return (bool)GetValue(ExecuteOnlyOnSourceUpdateProperty); } set { SetValue(ExecuteOnlyOnSourceUpdateProperty, value.Box()); } }
        
        /// <summary>
        /// Gets or sets whether the value of the property should be used as the parameter of the command to execute when the property is modified.
        /// </summary>
        public bool PassValueAsParameter { get { return (bool)GetValue(PassValueAsParameterProperty); } set { SetValue(PassValueAsParameterProperty, value.Box()); } }

        protected override void OnAttached()
        {
            if (PropertyName == null)
                throw new ArgumentException($"The PropertyName property must be set on behavior '{GetType().FullName}'.");

            dependencyProperty = AssociatedObject.GetDependencyProperties(true).FirstOrDefault(dp => dp.Name == PropertyName);
            if (dependencyProperty == null)
                throw new ArgumentException($"Unable to find property '{PropertyName}' on object of type '{AssociatedObject.GetType().FullName}'.");

            propertyWatcher.Attach(AssociatedObject);
            // TODO: Register/Unregister handlers when the PropertyName changes
            propertyWatcher.RegisterValueChangedHandler(dependencyProperty, OnPropertyChanged);
            Binding.AddSourceUpdatedHandler(AssociatedObject, OnSourceUpdated);
        }

        protected override void OnDetaching()
        {
            propertyWatcher.Detach();
            base.OnDetaching();
        }

        private void OnSourceUpdated(object sender, [NotNull] DataTransferEventArgs e)
        {
            if (ExecuteOnlyOnSourceUpdate && e.Property == dependencyProperty)
            {
                ExecuteCommand();
            }
        }

        private void OnPropertyChanged(object sender, EventArgs e)
        {
            if (!ExecuteOnlyOnSourceUpdate)
            {
                ExecuteCommand();
            }
        }

        private void ExecuteCommand()
        {
            var parameter = PassValueAsParameter ? AssociatedObject.GetValue(dependencyProperty) : CommandParameter;
            if (Command == null || !Command.CanExecute(parameter))
                return;

            Command.Execute(parameter);
        }
    }
}
