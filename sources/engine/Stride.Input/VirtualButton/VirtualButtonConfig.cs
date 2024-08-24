// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Describes the configuration composed by a collection of <see cref="VirtualButtonBinding"/>.
    /// </summary>
    public class VirtualButtonConfig : TrackingCollection<VirtualButtonBinding>
    {
        private readonly Dictionary<object, List<VirtualButtonBinding>> mapBindings;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualButtonConfig" /> class.
        /// </summary>
        public VirtualButtonConfig()
        {
            mapBindings = new Dictionary<object, List<VirtualButtonBinding>>();
            CollectionChanged += Bindings_CollectionChanged;
        }

        /// <summary>
        /// Gets the binding names.
        /// </summary>
        /// <value>The binding names.</value>
        public IEnumerable<object> BindingNames
        {
            get
            {
                return mapBindings.Keys;
            }
        }

        /// <summary>
        /// Gets the value for a particular binding.
        /// </summary>
        /// <returns>Value of the binding</returns>
        public virtual float GetValue(InputManager inputManager, object name)
        {
            float value = 0.0f;
            if (mapBindings.TryGetValue(name, out List<VirtualButtonBinding> bindingsPerName))
            {
                foreach (var virtualButtonBinding in bindingsPerName)
                {
                    float newValue = virtualButtonBinding.GetValue(inputManager);
                    if (Math.Abs(newValue) > Math.Abs(value))
                    {
                        value = newValue;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Gets the pressed state for a particular binding.
        /// </summary>
        /// <returns><c>true</c> when pressed since the last frame; otherwise, <c>false</c>.</returns>
        public virtual bool IsPressed(InputManager inputManager, object name)
        {
            if (mapBindings.TryGetValue(name, out List<VirtualButtonBinding> bindingsPerName))
            {
                foreach (var virtualButtonBinding in bindingsPerName)
                {
                    if (virtualButtonBinding.IsPressed(inputManager))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        /// <summary>
        /// Gets the held down state for a particular binding.
        /// </summary>
        /// <returns><c>true</c> when currently held down; otherwise, <c>false</c>.</returns>
        public virtual bool IsDown(InputManager inputManager, object name)
        {
            if (mapBindings.TryGetValue(name, out List<VirtualButtonBinding> bindingsPerName))
            {
                foreach (var virtualButtonBinding in bindingsPerName)
                {
                    if (virtualButtonBinding.IsDown(inputManager))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        /// <summary>
        /// Gets the pressed state for a particular binding.
        /// </summary>
        /// <returns><c>true</c> when released since the last frame; otherwise, <c>false</c>.</returns>
        public virtual bool IsReleased(InputManager inputManager, object name)
        {
            if (mapBindings.TryGetValue(name, out List<VirtualButtonBinding> bindingsPerName))
            {
                foreach (var virtualButtonBinding in bindingsPerName)
                {
                    if (virtualButtonBinding.IsReleased(inputManager))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Bindings_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            var virtualButtonBinding = (VirtualButtonBinding)e.Item;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddBinding(virtualButtonBinding);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveBinding(virtualButtonBinding);
                    break;
            }
        }

        private void AddBinding(VirtualButtonBinding virtualButtonBinding)
        {
            List<VirtualButtonBinding> bindingsPerName;
            if (!mapBindings.TryGetValue(virtualButtonBinding.Name, out bindingsPerName))
            {
                bindingsPerName = new List<VirtualButtonBinding>();
                mapBindings.Add(virtualButtonBinding.Name, bindingsPerName);
            }
            bindingsPerName.Add(virtualButtonBinding);
        }

        private void RemoveBinding(VirtualButtonBinding virtualButtonBinding)
        {
            List<VirtualButtonBinding> bindingsPerName;
            if (mapBindings.TryGetValue(virtualButtonBinding.Name, out bindingsPerName))
            {
                bindingsPerName.Remove(virtualButtonBinding);
            }
        }
    }
}
