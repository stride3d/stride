// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Media;

namespace Xenko.Audio
{
    /// <summary>
    /// This class is used to control a <see cref="SoundBase"/> associated to a <see cref="AudioEmitterComponent"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class can not be directly created by the user, but need to queried from an <see cref="AudioEmitterComponent"/>
    /// instance using the readonly <see cref="AudioEmitterComponent.Item(string)"/> indexer.
    /// </para>
    /// <para>
    /// An instance <see cref="AudioEmitterSoundController"/> is not valid anymore if any of those situations arrives:
    /// <list type="bullet">
    ///  <item><description>The underlying <see cref="sound"/> is disposed.</description></item>
    ///  <item><description>The <see cref="AudioEmitterComponent"/> is detached from its entity.</description></item>
    ///  <item><description>The entity to which it is attached is removed from the Entity System.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Controller for {sound.Name}")]
    public class AudioEmitterSoundController : IPlayableSound, IMediaPlayer
    {
        private readonly SoundBase sound;
        private readonly AudioEmitterComponent emitter;

        public bool IsDisposed { get; set; }

        /// <summary>
        /// The instances of <see cref="sound"/> currently created by this controller (one for each listener).
        /// </summary>
        [DataMemberIgnore]
        internal readonly Dictionary<SoundInstance, AudioListenerComponent> InstanceToListener = new Dictionary<SoundInstance, AudioListenerComponent>();

        /// <summary>
        /// Created a new <see cref="AudioEmitterSoundController"/> instance.
        /// </summary>
        /// <param name="parent">The parent AudioEmitterComponent to which the controller is associated.</param>
        /// <param name="sound">The underlying SoundBase to be controlled</param>
        /// <remarks>A <see cref="sound"/> can be associated to several controllers.</remarks>
        internal AudioEmitterSoundController(AudioEmitterComponent parent, SoundBase sound)
        {
            this.sound = sound ?? throw new ArgumentNullException(nameof(sound));
            emitter = parent;

            Volume = 1;
        }

        /// <summary>
        /// Create an new instance of underlying sound, and register it in the controller's sound instance list.
        /// </summary>
        /// <returns>The new sound effect instance created</returns>
        internal SoundInstance CreateSoundInstance(AudioListenerComponent listener, bool forget)
        {
            var newInstance = sound.CreateInstance(listener.Listener, emitter.UseHRTF, emitter.DirectionalFactor, emitter.Environment);

            if (!forget)
                InstanceToListener.Add(newInstance, listener);

            return newInstance;
        }

        internal void DestroySoundInstance(SoundInstance instance)
        {
            InstanceToListener.Remove(instance);
            instance.Dispose();
        }

        internal void DestroySoundInstances(AudioListenerComponent listener)
        {
            var deferRemoval = new List<SoundInstance>();

            foreach (var instance in InstanceToListener.Keys)
            {
                deferRemoval.Add(instance);
            }

            foreach (var soundInstance in deferRemoval)
            {
                DestroySoundInstance(soundInstance);
            }

            for (var i = 0; i < FastInstances.Count; i++)
            {
                var instance = FastInstances[i];
                if (instance.Listener == listener.Listener)
                {
                    //Decrement the loop counter to iterate this index again, since later elements will get moved down during the remove operation.
                    FastInstances.RemoveAt(i--);
                    instance.Dispose();
                }
            }
        }

        /// <summary>
        /// Dispose and removes all the controller sound instances.
        /// </summary>
        internal void DestroyAllSoundInstances()
        {
            foreach (var instance in InstanceToListener)
            {
                instance.Key.Dispose();
            }
            InstanceToListener.Clear();

            foreach (var soundInstance in FastInstances)
            {
                soundInstance.Dispose();
            }
            FastInstances.Clear();
        }

        private PlayState playState;

        public PlayState PlayState
        {
            get
            {
                // force the play status to 'stopped' if there is no listeners.
                if (!InstanceToListener.Any())
                    return PlayState.Stopped;

                // return the controller playStatus if not started playing.
                if (playState != PlayState.Playing || ShouldBePlayed)
                    return playState;

                // returns the playStatus of the underlying instances if controller is playing

                // A desynchronization between instances' playState can appear due to asynchronous callbacks
                // setting the state of the sound to Stopped when reaching the end of the track.
                // For coherency, we consider a controller as stopped only when all its instances are stopped.
                // (if not the case, a play call to a stopped controller would restart only some of the underlying instances)
                if (InstanceToListener.Any(x => x.Key.PlayState == PlayState.Playing))
                    return PlayState.Playing;

                return playState = PlayState.Stopped;
            }
        }

        private bool isLooping;

        /// <summary>
        /// Gets or sets whether the sound is automatically looping from beginning when it reaches the end.
        /// </summary>
        public bool IsLooping
        {
            get => isLooping;
            set
            {
                foreach (var instance in InstanceToListener)
                {
                    instance.Key.IsLooping = value;
                }
                isLooping = value;
            }
        }

        private float pitch = 1.0f;

        public float Pitch
        {
            get => pitch;
            set
            {
                foreach (var instance in InstanceToListener)
                {
                    instance.Key.Pitch = value;
                }
                pitch = value;
            }
        }

        /// <summary>
        /// Indicate the <see cref="AudioListenerProcessor"/> if the controller's sound instances need to be played.
        /// This variable is need because <see cref="Play"/> is asynchronous and actually starts playing only on next system update.
        /// </summary>
        internal volatile bool ShouldBePlayed;

        public void Play()
        {
            playState = PlayState.Playing;

            // Controller play function is asynchronous.
            // underlying sound instances actually start playing only after the next system update.
            // Such a asynchronous behavior is required in order to be able to update the associated AudioEmitter
            // and apply localization to the sound before starting to play.

            ShouldBePlayed = true;  // tells the EmitterProcessor to start playing the underlying instances.
        }

        internal volatile bool FastInstancePlay;
        internal List<SoundInstance> FastInstances = new List<SoundInstance>();

        /// <summary>
        /// Plays the attached sound in a new instance and let's the engine handle it's disposal.
        /// This is useful for very fast overlapping sounds, gun shots, machine gun etc. Where you don't care about controlling each sound.
        /// </summary>
        public void PlayAndForget()
        {
            FastInstancePlay = true; // tells the EmitterProcessor to create and start playing a temporary instances.
        }

        public void Pause()
        {
            if (PlayState != PlayState.Playing)
                return;

            playState = PlayState.Paused;

            foreach (var instance in InstanceToListener)
            {
                instance.Key.Pause();
            }

            ShouldBePlayed = false;
        }

        public void Stop()
        {
            playState = PlayState.Stopped;

            foreach (var instance in InstanceToListener)
            {
                instance.Key.Stop();
            }

            ShouldBePlayed = false;
        }

        private float volume;

        public float Volume
        {
            get => volume;
            set
            {
                volume = value;

                foreach (var instance in InstanceToListener)
                {
                    instance.Key.Volume = volume;
                }
            }
        }

        public float SpeedFactor
        {
            get => pitch;
            set
            {
                foreach (var instance in InstanceToListener)
                {
                    var mediaReader = instance.Key as IMediaReader;
                    if (mediaReader != null)
                        mediaReader.SpeedFactor = value;
                }
                Pitch = value;
            }
        }

        /// <summary>
        /// Sets the range of the sound to play.
        /// </summary>
        /// <param name="range">a PlayRange structure that describes the starting offset and ending point of the sound to play in seconds.</param>
        /// <remarks>This will not be valid if the sound is played with PlayAndForget</remarks>
        public void SetRange(PlayRange range)
        {
            foreach (var instance in InstanceToListener)
            {
                instance.Key.SetRange(range);
            }
        }

        public void Seek(TimeSpan mediaTime)
        {
            foreach (var instance in InstanceToListener)
            {
                var mediaReader = instance.Key as IMediaReader;
                if (mediaReader != null)
                    mediaReader.Seek(mediaTime);
            }
        }
    }
}
