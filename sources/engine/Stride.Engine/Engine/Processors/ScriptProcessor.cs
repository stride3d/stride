// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// Manage scripts
    /// </summary>
    public sealed class ScriptProcessor : EntityProcessor<ScriptComponent>
    {
        private ScriptSystem scriptSystem;

        public ScriptProcessor()
        {
            // Script processor always running before others
            Order = -100000;
        }

        protected internal override void OnSystemAdd()
        {
            scriptSystem = Services.GetService<ScriptSystem>();
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentAdding(Entity entity, ScriptComponent component, ScriptComponent associatedData)
        {
            // Add current list of scripts
            scriptSystem.Add(component);
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentRemoved(Entity entity, ScriptComponent component, ScriptComponent associatedData)
        {
            scriptSystem.Remove(component);
        }
    }
}
