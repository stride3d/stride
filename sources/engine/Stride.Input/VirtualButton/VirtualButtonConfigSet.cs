// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.ObjectModel;

namespace Stride.Input
{
    /// <summary>
    /// A collection of <see cref="VirtualButtonConfig"/>.
    /// </summary>
    /// <remarks>
    /// Several virtual button configurations can be stored in this instance. 
    /// For example, User0 config could be stored on index 0, User1 on index 1...etc.
    /// </remarks>
    public class VirtualButtonConfigSet : Collection<VirtualButtonConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualButtonConfigSet" /> class.
        /// </summary>
        public VirtualButtonConfigSet()
        {
        }

        /// <summary>
        /// Gets a binding value for the specified name and the specified config extract from the <see cref="VirtualButtonConfigSet"/>.
        /// </summary>
        /// <param name="inputManager">The <see cref="InputManager"/> to used to get the device input.</param>
        /// <param name="configIndex">An index to a <see cref="VirtualButtonConfig"/> stored in the <see cref="VirtualButtonConfigSet"/>.</param>
        /// <param name="name">Name of the binding.</param>
        /// <returns>The value of the binding.</returns>
        public virtual float GetValue(InputManager inputManager, int configIndex, object name)
        {
            if (configIndex < 0 || configIndex >= Count)
            {
                return 0.0f;
            }

            var config = this[configIndex];
            return config != null ? config.GetValue(inputManager, name) : 0.0f;
        }

        /// <summary>
        /// Determines whether the specified binding in the specified config in the <see cref="VirtualButtonConfigSet"/> was pressed since the previous Update. 
        /// </summary>
        /// <param name="inputManager">The <see cref="InputManager"/> to used to get device input.</param>
        /// <param name="configIndex">An index to a <see cref="VirtualButtonConfig"/> stored in the <see cref="VirtualButtonConfigSet"/>.</param>
        /// <param name="name">Name of the binding.</param>
        /// <returns><c>true</c> if the binding was pressed; otherwise, <c>false</c>.</returns>
        public virtual bool IsPressed(InputManager inputManager, int configIndex, object name)
        {
            if (configIndex < 0 || configIndex >= Count)
            {
                return false;
            }

            var config = this[configIndex];
            return config != null ? config.IsPressed(inputManager, name) : false;
        }
        
        /// <summary>
        /// Determines whether the specified binding in the specified config in the <see cref="VirtualButtonConfigSet"/> is currently pressed down. 
        /// </summary>
        /// <param name="inputManager">The <see cref="InputManager"/> to used to get device input.</param>
        /// <param name="configIndex">An index to a <see cref="VirtualButtonConfig"/> stored in the <see cref="VirtualButtonConfigSet"/>.</param>
        /// <param name="name">Name of the binding.</param>
        /// <returns><c>true</c> if the binding is currently pressed down; otherwise, <c>false</c>.</returns>
        public virtual bool IsDown(InputManager inputManager, int configIndex, object name)
        {
            if (configIndex < 0 || configIndex >= Count)
            {
                return false;
            }

            var config = this[configIndex];
            return config != null ? config.IsDown(inputManager, name) : false;
        }
        
        /// <summary>
        /// Determines whether the specified binding in the specified config in the <see cref="VirtualButtonConfigSet"/> was released since the previous Update. 
        /// </summary>
        /// <param name="inputManager">The <see cref="InputManager"/> to used to get device input.</param>
        /// <param name="configIndex">An index to a <see cref="VirtualButtonConfig"/> stored in the <see cref="VirtualButtonConfigSet"/>.</param>
        /// <param name="name">Name of the binding.</param>
        /// <returns><c>true</c> if the binding was released; otherwise, <c>false</c>.</returns>
        public virtual bool IsReleased(InputManager inputManager, int configIndex, object name)
        {
            if (configIndex < 0 || configIndex >= Count)
            {
                return false;
            }

            var config = this[configIndex];
            return config != null ? config.IsReleased(inputManager, name) : false;
        }
    }
}
