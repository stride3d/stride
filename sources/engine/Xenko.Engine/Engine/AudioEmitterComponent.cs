// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xenko.Audio;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Engine.Design;

namespace Xenko.Engine
{
    /// <summary>
    /// Component representing an audio emitter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associate this component to an entity to simulate a 3D localized source of sound coming from the entity center.
    /// </para>
    /// <para>
    /// Several sounds can be associated to a single AudioEmitterComponent.
    /// Use the <see cref="Sounds"/> dictionary to associate or dissociate a <see cref="SoundBase"/> to the emitter component.
    /// Each SoundBase associated to the emitter component can be controlled (played, paused, stopped, ...) independently for the others.
    /// Once attached to the emitter component, a SoundBase is controlled using a <see cref="AudioEmitterSoundController"/>.
    /// To get the AudioEmitterSoundController associated to a SoundBase use the readonly <see cref="AudioEmitterComponent.Item(string)"/> indexer.
    /// </para>
    /// </remarks>
    [Display("Audio emitter", Expand = ExpandRule.Once)]
    [DataContract("AudioEmitterComponent")]
    [DefaultEntityComponentProcessor(typeof(AudioEmitterProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentOrder(7000)]
    [ComponentCategory("Audio")]
    public sealed class AudioEmitterComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Dictionary associating each SoundBase to a single soundController.
        /// The controller a valid as long as the corresponding SoundBase is present in the dictionary.
        /// </summary>
        internal readonly Dictionary<SoundBase, AudioEmitterSoundController> SoundToController = new Dictionary<SoundBase, AudioEmitterSoundController>();

        /// <summary>
        /// Event argument class used to signal the <see cref="AudioEmitterProcessor"/> that a new AudioEmitterSoundController has new added or removed to the component.
        /// </summary>
        internal class ControllerCollectionChangedEventArgs
        {
            /// <summary>
            /// The entity associated the current component.
            /// </summary>
            public Entity Entity;

            /// <summary>
            /// The controller that have been added or removed to the component.
            /// </summary>
            public AudioEmitterSoundController Controller;

            /// <summary>
            /// The AudioEmitterComponent itself
            /// </summary>
            public AudioEmitterComponent EmitterComponent;

            /// <summary>
            /// Action indication if the controller has been added or removed.
            /// </summary>
            public NotifyCollectionChangedAction Action;

            public ControllerCollectionChangedEventArgs(Entity entity, AudioEmitterSoundController controller, AudioEmitterComponent component, NotifyCollectionChangedAction action)
            {
                Entity = entity;
                Controller = controller;
                EmitterComponent = component;
                Action = action;
            }
        }

        /// <summary>
        /// Event triggered when an <see cref="AudioEmitterSoundController"/> has be attached or detached to the component.
        /// </summary>
        internal event EventHandler<ControllerCollectionChangedEventArgs> ControllerCollectionChanged;

        /// <summary>
        /// The sounds this audio emitter can play and use
        /// </summary>
        [DataMember(10)]
        public TrackingDictionary<string, Sound> Sounds = new TrackingDictionary<string, Sound>();

        /// <summary>
        /// The sound controllers associated with the sounds this audio emitter can play and use, use this to access and play sounds.
        /// </summary>
        /// <param name="soundName">The name of the sound you want to access.</param>
        /// <returns>The sound controller.</returns>
        [DataMemberIgnore]
        public AudioEmitterSoundController this[string soundName] => SoundToController[Sounds[soundName]];

        /// <summary>
        /// If possible use a more complex HRTF algorithm to perform 3D sound simulation
        /// </summary>
        /// <userdoc>
        /// If possible use a more complex HRTF algorithm to perform 3D sound simulation
        /// </userdoc>
        [DataMember(20)]
        // ReSharper disable once InconsistentNaming
        public bool UseHRTF { get; set; }

        /// <summary>
        /// If 0 the sound will be omnidirectional, 1 fully directional
        /// </summary>
        /// <userdoc>
        /// If 0 the sound will be omnidirectional, 1 fully directional
        /// </userdoc>
        [DataMember(30)]
        [DataMemberRange(0.0, 1.0, 0.1, 0.2, 3)]
        public float DirectionalFactor { get; set; }

        /// <summary>
        /// The reverberation model that this emitter will use
        /// </summary>
        /// <userdoc>
        /// The reverberation model that this emitter will use
        /// </userdoc>
        [DataMember(40)]
        public HrtfEnvironment Environment { get; set; }

        /// <summary>
        /// Attach a <see cref="SoundBase"/> to this emitter component.
        /// Once attached a <see cref="AudioEmitterSoundController"/> can be queried using readonly <see cref="AudioEmitterComponent.Item(string)"/> indexer to control the attached SoundBase.
        /// </summary>
        /// <param name="sound">The SoundBase to attach</param>
        /// <exception cref="ArgumentNullException">The provided <paramref name="sound"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The provided <paramref name="sound"/> can not be localized (contains more than one channel).</exception>
        /// <remarks>Attaching a SoundBase already attached has no effects.</remarks>
        public AudioEmitterSoundController AttachSound([NotNull] SoundBase sound)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (sound.Channels > 1)
                throw new InvalidOperationException("The provided Sound has more than one channel. It can not be localized in the 3D scene, please check the spatialized option in the Sound asset.");

            if (SoundToController.TryGetValue(sound, out var existingController))
                return existingController;

            var newController = new AudioEmitterSoundController(this, sound);
            SoundToController[sound] = newController;
            ControllerCollectionChanged?.Invoke(this, new ControllerCollectionChangedEventArgs(Entity, newController, this, NotifyCollectionChangedAction.Add));

            return newController;
        }

        /// <summary>
        /// Detach a <see cref="SoundBase"/> from this emitter component.
        /// Once detach the controller previously associated to the SoundBase is invalid.
        /// </summary>
        /// <param name="sound">The SoundBase to detach.</param>
        /// <exception cref="ArgumentNullException">The provided <paramref name="sound"/> is null.</exception>
        /// <exception cref="ArgumentException">The provided <paramref name="sound"/> is not currently attached to the emitter component.</exception>
        public void DetachSound([NotNull] SoundBase sound)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (!SoundToController.ContainsKey(sound))
                throw new ArgumentException("The provided Sound is not currently attached to this emitter component.");

            var oldController = SoundToController[sound];
            SoundToController.Remove(sound);
            ControllerCollectionChanged?.Invoke(this, new ControllerCollectionChangedEventArgs(Entity, oldController, this, NotifyCollectionChangedAction.Remove));
        }

        private void OnSoundsOnCollectionChanged(object sender, TrackingCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AttachSound((SoundBase)args.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    DetachSound((SoundBase)args.Item);
                    break;
            }
        }

        internal void AttachToProcessor()
        {
            Sounds.CollectionChanged += OnSoundsOnCollectionChanged;

            foreach (var sound in Sounds)
            {
                if (sound.Value != null)
                {
                    AttachSound(sound.Value);
                }
            }
        }

        internal void DetachFromProcessor()
        {
            foreach (var sound in Sounds)
            {
                if (sound.Value != null)
                {
                    DetachSound(sound.Value);
                }
            }

            Sounds.CollectionChanged -= OnSoundsOnCollectionChanged;
        }
    }
}
