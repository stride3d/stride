// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Engine.Processors;

namespace Stride.Engine
{
    /// <summary>
    /// A script whose <see cref="Update"/> will be called every frame.
    /// </summary>
    public abstract class SyncScript : StartupScript
    {
        [CanBeNull] internal ScriptSystem ScriptSystem;
        internal long ScheduledPriorityForUpdate;

        /// <summary>
        /// Called every frame.
        /// </summary>
        public abstract void Update();

        protected internal override void PriorityUpdated()
        {
            base.PriorityUpdated();
            ScriptSystem?.MarkAsPriorityChanged(this);
        }
    }
}
