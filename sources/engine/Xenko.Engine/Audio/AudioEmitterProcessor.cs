// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Media;
using Xenko.Rendering;

namespace Xenko.Audio
{
    /// <summary>
    /// Processor in charge of updating the <see cref="AudioEmitterComponent"/>s.
    /// </summary>
    /// <remarks>
    /// <para>More precisely it updates the <see cref="AudioEmitter"/>s and 
    /// then applies 3D localization to each couple <see cref="AudioEmitterComponent"/>-<see cref="AudioListenerComponent"/>.
    /// When a new emitter or a new listener is added to the system, its creates the required SoundInstances and associate them with the new emitter/listener tuples.
    /// </para> 
    /// </remarks>
    public class AudioEmitterProcessor : EntityProcessor<AudioEmitterComponent, AudioEmitterProcessor.AssociatedData>
    {
        /// <summary>
        /// Reference to the audioSystem.
        /// </summary>
        private AudioSystem audioSystem;

        /// <summary>
        /// Data associated to each <see cref="Entity"/> instances of the system having an <see cref="AudioEmitterComponent"/> and an <see cref="TransformComponent"/>.
        /// </summary>
        public class AssociatedData
        {
            /// <summary>
            /// The <see cref="Xenko.Audio.AudioEmitter"/> associated to the <see cref="AudioEmitterComponent"/>.
            /// </summary>
            public AudioEmitter AudioEmitter;

            /// <summary>
            /// The <see cref="Engine.AudioEmitterComponent"/> associated to the entity
            /// </summary>
            public AudioEmitterComponent AudioEmitterComponent;

            /// <summary>
            /// The <see cref="TransformComponent"/> associated to the entity
            /// </summary>
            public TransformComponent TransformComponent;

            /// <summary>
            /// If this emitter has some instances playing
            /// </summary>
            public bool IsPlaying;
        }

        /// <summary>
        /// Create a new instance of the processor.
        /// </summary>
        public AudioEmitterProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected internal override void OnSystemAdd()
        {
            audioSystem = Services.GetSafeServiceAs<AudioSystem>();

            audioSystem.Listeners.CollectionChanged += OnListenerCollectionChanged;
        }

        protected override AssociatedData GenerateComponentData(Entity entity, AudioEmitterComponent component)
        {
            return new AssociatedData
            {
                AudioEmitterComponent = component,
                TransformComponent = entity.Transform,
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, AudioEmitterComponent component, AssociatedData associatedData)
        {
            return
                component == associatedData.AudioEmitterComponent &&
                entity.Transform == associatedData.TransformComponent;
        }

        protected internal override void OnSystemRemove()
        {
            // Destroy all the SoundInstance created by the processor before closing.
            foreach (var soundInstance in ComponentDatas.Values.SelectMany(x => x.AudioEmitterComponent.SoundToController.Values))
                soundInstance.DestroyAllSoundInstances();

            audioSystem.Listeners.CollectionChanged -= OnListenerCollectionChanged;
        }

        protected override void OnEntityComponentAdding(Entity entity, AudioEmitterComponent component, AssociatedData data)
        {
            // initialize the AudioEmitter first position
            data.TransformComponent.UpdateWorldMatrix(); // ensure the worldMatrix is correct
            data.AudioEmitter = new AudioEmitter { Position = data.TransformComponent.WorldMatrix.TranslationVector }; // valid position is needed at first Update loop to compute velocity.

            // create a SoundInstance for each listener activated and for each sound controller of the EmitterComponent.
            foreach (var listener in audioSystem.Listeners.Keys)
            {
                foreach (var soundController in data.AudioEmitterComponent.SoundToController.Values)
                {
                    soundController.CreateSoundInstance(listener, false);
                }
            }

            data.AudioEmitterComponent.ControllerCollectionChanged += OnSoundControllerListChanged;

            component.AttachToProcessor();
        }

        public override void Draw(RenderContext context)
        {
            foreach (var associatedData in ComponentDatas.Values)
            {
                if (!associatedData.AudioEmitterComponent.Enabled)
                {
                    if (associatedData.IsPlaying)
                    {
                        //stop any running instance
                        associatedData.IsPlaying = false;
                        foreach (var controller in associatedData.AudioEmitterComponent.SoundToController.Values)
                        {
                            foreach (var instanceListener in controller.InstanceToListener)
                            {
                                instanceListener.Key.Stop();
                            }
                        }
                    }
                    continue;
                }

                var emitter = associatedData.AudioEmitter;
                emitter.WorldTransform = associatedData.TransformComponent.WorldMatrix;
                var pos = emitter.WorldTransform.TranslationVector;

                // First update the emitter data if required.
                emitter.Velocity = pos - emitter.Position;
                emitter.Position = pos;

                // TODO: if the entity has just been added, it might crash because part of the Transform update is done at the Draw and we might have uninitialized values
                if (emitter.WorldTransform == Matrix.Zero)
                    return;

                emitter.Forward = Vector3.Normalize((Vector3)emitter.WorldTransform.Row3);
                emitter.Up = Vector3.Normalize((Vector3)emitter.WorldTransform.Row2);

                // Then apply 3D localization
                foreach (var controller in associatedData.AudioEmitterComponent.SoundToController.Values)
                {
                    //deal normal instances
                    foreach (var instanceListener in controller.InstanceToListener)
                    {
                        if (!instanceListener.Value.Enabled)
                        {
                            instanceListener.Key.Stop();
                            continue;
                        }

                        // Apply3D localization
                        if (instanceListener.Key.PlayState == PlayState.Playing)
                        {
                            instanceListener.Key.Apply3D(emitter);
                        }

                        //Apply parameters
                        if (instanceListener.Key.Volume != controller.Volume) instanceListener.Key.Volume = controller.Volume; // ensure that instance volume is valid
                        if (instanceListener.Key.IsLooping != controller.IsLooping) instanceListener.Key.IsLooping = controller.IsLooping;

                        //Play if stopped
                        if (instanceListener.Key.PlayState != PlayState.Playing && controller.ShouldBePlayed)
                        {
                            instanceListener.Key.Apply3D(emitter);
                            instanceListener.Key.Play();
                            associatedData.IsPlaying = true;
                        }
                    }

                    controller.ShouldBePlayed = false;

                    //handle Play and forget instances
                    for (var i = 0; i < controller.FastInstances.Count; i++)
                    {
                        var instance = controller.FastInstances[i];
                        if (instance.PlayState != PlayState.Playing)
                        {
                            //Decrement the loop counter to iterate this index again, since later elements will get moved down during the remove operation.
                            controller.FastInstances.RemoveAt(i--);
                            controller.DestroySoundInstance(instance);
                        }
                        else
                        {
                            instance.Apply3D(emitter);
                        }
                    }

                    //Create new play and forget instances
                    if (controller.FastInstancePlay)
                    {
                        foreach (var listeners in audioSystem.Listeners)
                        {
                            if (!listeners.Key.Enabled) continue;

                            var instance = controller.CreateSoundInstance(listeners.Key, true);
                            if (instance == null) continue;

                            instance.Volume = controller.Volume;
                            instance.Pitch = controller.Pitch;
                            instance.Apply3D(emitter);
                            instance.Play();

                            controller.FastInstances.Add(instance);
                        }
                        controller.FastInstancePlay = false;
                    }
                }
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, AudioEmitterComponent component, AssociatedData data)
        {
            component.DetachFromProcessor();

            // dispose and delete all SoundInstances associated to the EmitterComponent.
            foreach (var soundController in data.AudioEmitterComponent.SoundToController.Values)
                soundController.DestroyAllSoundInstances();

            data.AudioEmitterComponent.ControllerCollectionChanged -= OnSoundControllerListChanged;
        }

        private void OnListenerCollectionChanged(object o, TrackingCollectionChangedEventArgs args)
        {
            if (!args.CollectionChanged) // no keys have been added or removed, only one of the values changed
                return;
            
            // A listener have been Added or Removed. 
            // We need to create/destroy all SoundInstances associated to that listener for each AudioEmitterComponent.

            foreach (var associatedData in ComponentDatas.Values)
            {
                var soundControllers = associatedData.AudioEmitterComponent.SoundToController.Values;

                foreach (var soundController in soundControllers)
                {
                    if (args.Action == NotifyCollectionChangedAction.Add) // A new listener have been added
                    {
                        soundController.CreateSoundInstance((AudioListenerComponent)args.Key, false);
                    }
                    else if (args.Action == NotifyCollectionChangedAction.Remove) // A listener have been removed
                    {
                        soundController.DestroySoundInstances((AudioListenerComponent)args.Key);
                    }
                }
            }
        }

        private void OnSoundControllerListChanged(object o, AudioEmitterComponent.ControllerCollectionChangedEventArgs args)
        {
            // A new Sound have been associated to the AudioEmitterComponenent or an old Sound have been deleted.
            // We need to create/destroy the corresponding SoundInstances.
            var listeners = audioSystem.Listeners.Keys;
            foreach (var listener in listeners)
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    args.Controller.CreateSoundInstance(listener, false);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    args.Controller.DestroySoundInstances(listener);
                }
            }
        }
    }
}
