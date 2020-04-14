// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Audio
{
    /// <summary>
    /// Processor in charge of creating and updating the <see cref="AudioListener"/> data associated to the scene <see cref="AudioListenerComponent"/>s.
    /// </summary>
    /// <remarks>
    /// The processor updates only <see cref="AudioListener"/> associated to <see cref="AudioListenerComponent"/>s 
    /// The processor is subscribing to the <see cref="audioSystem"/> <see cref="AudioListenerComponent"/> collection events to be informed of required <see cref="AudioEmitter"/> updates.
    /// When a <see cref="AudioListenerComponent"/> is added to the <see cref="audioSystem"/>, the processor set the associated <see cref="AudioEmitter"/>.
    /// When a <see cref="AudioListenerComponent"/> is removed from the entity system, 
    /// the processor set the <see cref="AudioEmitter"/> reference of the <see cref="AudioSystem"/> to null 
    /// but do not remove the <see cref="AudioListenerComponent"/> from its collection.
    /// </remarks>
    public class AudioListenerProcessor : EntityProcessor<AudioListenerComponent>
    {
        /// <summary>
        /// Reference to the <see cref="AudioSystem"/> of the game instance.
        /// </summary>
        private AudioSystem audioSystem;

        /// <summary>
        /// Create a new instance of AudioListenerProcessor.
        /// </summary>
        public AudioListenerProcessor()
            : base(typeof(AudioListenerComponent))
        {
        }

        protected internal override void OnSystemAdd()
        {
            audioSystem = Services.GetService<AudioSystem>();
        }

        protected internal override void OnSystemRemove()
        {
            audioSystem.Listeners.Clear();
        }

        protected override void OnEntityComponentAdding(Entity entity, AudioListenerComponent component, AudioListenerComponent data)
        {
            component.Listener = new AudioListener(audioSystem.AudioEngine);

            audioSystem.Listeners.Add(component, component.Listener);
        }

        protected override void OnEntityComponentRemoved(Entity entity, AudioListenerComponent component, AudioListenerComponent data)
        {
            audioSystem.Listeners.Remove(component);

            component.Listener.Dispose();
        }

        public override void Draw(RenderContext context)
        {
            foreach (var listenerData in ComponentDatas.Values)
            {
                if (!listenerData.Enabled) // skip all updates if the listener is not used.
                    continue;

                var listener = listenerData.Listener;
                listener.WorldTransform = listenerData.Entity.Transform.WorldMatrix;
                var newPosition = listener.WorldTransform.TranslationVector;
                listener.Velocity = newPosition - listener.Position; // estimate velocity from last and new position
                listener.Position = newPosition;
                listener.Forward = Vector3.Normalize((Vector3)listener.WorldTransform.Row3);
                listener.Up = Vector3.Normalize((Vector3)listener.WorldTransform.Row2);

                listener.Update();
            }
        }
    }
}
