// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// Base class for sound that creates voices
    /// </summary>
    public class SoundInstance : ComponentBase, IPositionableSound
    {
        protected DynamicSoundSource soundSource;
        protected SoundBase sound;
        protected AudioEngine engine;

        protected bool isLooping;
        protected float pan;
        protected float pitch;
        protected float volume;
        protected bool spatialized;
        protected PlayState playState = PlayState.Stopped;

        internal AudioLayer.Source Source;

        internal AudioListener Listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundInstance"/> class using a dynamic sound source.
        /// </summary>
        /// <param name="engine">The audio engine that will be used to play this instance</param>
        /// <param name="listener">The listener of this instance</param>
        /// <param name="dynamicSoundSource">The source from where the PCM data will be fetched</param>
        /// <param name="sampleRate">The sample rate of this audio stream</param>
        /// <param name="mono">Set to true if the souce is mono, false if stereo</param>
        /// <param name="spatialized">If the SoundInstance will be used for spatialized audio set to true, if not false, if true mono must also be true</param>
        /// <param name="useHrtf">If the engine should use Hrtf for spatialization</param>
        /// <param name="directionalFactor"></param>
        /// <param name="environment"></param>
        public SoundInstance(AudioEngine engine, AudioListener listener, DynamicSoundSource dynamicSoundSource, int sampleRate, bool mono, bool spatialized = false, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            Listener = listener;
            this.engine = engine;
            this.spatialized = spatialized;
            soundSource = dynamicSoundSource;

            if (engine.State == AudioEngineState.Invalidated)
                return;

            Source = AudioLayer.SourceCreate(listener.Listener, sampleRate, dynamicSoundSource.MaxNumberOfBuffers, mono, spatialized, true, useHrtf, directionalFactor, environment);
            if (Source.Ptr == IntPtr.Zero)
            {
                throw new Exception("Failed to create an AudioLayer Source");
            }

            ResetStateToDefault();
        }

        internal SoundInstance() { }

        internal SoundInstance(Sound staticSound, AudioListener listener, bool forceLoadInMemory, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            Listener = listener;
            engine = staticSound.AudioEngine;
            sound = staticSound;
            spatialized = staticSound.Spatialized;

            var streamed = staticSound.StreamFromDisk && !forceLoadInMemory;

            if (engine.State == AudioEngineState.Invalidated)
                return;

            Source = AudioLayer.SourceCreate(listener.Listener, staticSound.SampleRate, streamed ? CompressedSoundSource.NumberOfBuffers : 1, staticSound.Channels == 1, spatialized, streamed, useHrtf, directionalFactor, environment);
            if (Source.Ptr == IntPtr.Zero)
            {
                throw new Exception("Failed to create an AudioLayer Source");
            }

            if (streamed)
            {
                soundSource = new CompressedSoundSource(this, staticSound.FileProvider, staticSound.CompressedDataUrl, staticSound.NumberOfPackets, staticSound.Samples, staticSound.SampleRate, staticSound.Channels, staticSound.MaxPacketLength);
            }
            else
            {
                if (staticSound.PreloadedBuffer.Ptr == IntPtr.Zero)
                {
                    staticSound.LoadSoundInMemory(); //this should be already loaded by the serializer, but in the case of forceLoadInMemory might not be the case yet.
                }
                AudioLayer.SourceSetBuffer(Source, staticSound.PreloadedBuffer);
            }

            ResetStateToDefault();
        }
        
        /// <summary>
        /// Gets or sets whether the sound is automatically looping from beginning when it reaches the end.
        /// </summary>
        public bool IsLooping
        {
            get => isLooping;
            set
            {
                isLooping = value;

                if (engine.State == AudioEngineState.Invalidated)
                    return;

                if (soundSource == null) AudioLayer.SourceSetLooping(Source, isLooping);
                else soundSource.SetLooped(isLooping);
            }
        }

        /// <summary>
        /// Set the sound balance between left and right speaker.
        /// </summary>
        /// <remarks>Panning is ranging from -1.0f (full left) to 1.0f (full right). 0.0f is centered. Values beyond this range are clamped.
        /// Panning modifies the total energy of the signal (Pan == -1 => Energy = 1 + 0, Pan == 0 => Energy = 1 + 1, Pan == 0.5 => Energy = 1 + 0.5, ...)
        /// </remarks>
        public float Pan
        {
            get => pan;
            set
            {
                pan = value;

                if (engine.State == AudioEngineState.Invalidated)
                    return;

                AudioLayer.SourceSetPan(Source, value);
            }
        }

        /// <summary>
        /// The global volume at which the sound is played.
        /// </summary>
        /// <remarks>Volume is ranging from 0.0f (silence) to 1.0f (full volume). Values beyond those limits are clamped.</remarks>
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;

                if (engine.State == AudioEngineState.Invalidated)
                    return;

                AudioLayer.SourceSetGain(Source, volume);
            }
        }

        /// <summary>
        /// Gets or sets the pitch of the sound, might conflict with spatialized sound spatialization.
        /// </summary>
        public float Pitch
        {
            get => pitch;
            set
            {
                pitch = value;

                if (engine.State == AudioEngineState.Invalidated)
                    return;

                AudioLayer.SourceSetPitch(Source, pitch);
            }
        }

        /// <summary>
        /// A task that completes when the sound is ready to play
        /// </summary>
        /// <returns>Returns a task that will complete when the sound has been buffered and ready to play</returns>
        public async Task<bool> ReadyToPlay()
        {
            if (soundSource == null) return await Task.FromResult(true);
            return await soundSource.ReadyToPlay.Task;
        }

        /// <summary>
        /// Applies 3D positioning to the sound.
        /// More precisely adjust the channel volumes and pitch of the sound,
        /// such that the sound source seems to come from the <paramref name="emitter"/> to the listener/>.
        /// </summary>
        /// <param name="emitter">The emitter that correspond to this sound</param>
        /// <remarks>
        /// <see cref="Apply3D"/> can be used only on mono-sounds.
        /// <para>
        /// The final resulting pitch depends on the listener and emitter relative velocity.
        /// The final resulting channel volumes depend on the listener and emitter relative positions and the value of <see cref="IPlayableSound.Volume"/>.
        /// </para>
        /// </remarks>
        public void Apply3D(AudioEmitter emitter)
        {
            if (engine.State == AudioEngineState.Invalidated)
                return;

            if (!spatialized) return;

            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));

            emitter.Apply3D(Source);
        }

        /// <summary>
        /// Pause the sounds.
        /// </summary>
        /// <remarks>A call to Pause when the sound is already paused or stopped has no effects.</remarks>
        public void Pause()
        {
            if (engine.State == AudioEngineState.Invalidated)
                return;

            if (PlayState != PlayState.Playing)
                return;

            if (soundSource == null)
            {
                AudioLayer.SourcePause(Source);
            }
            else
            {
                soundSource.Pause();
            }

            playState = PlayState.Paused;
        }

        /// <summary>
        /// Play or resume the sound effect instance.
        /// </summary>
        public void Play()
        {
            Play(false); //this is the same behavior in AudioEmitterProcessor and Controllers
        }

        /// <summary>
        /// Play or resume the sound effect instance, stopping sibling instances.
        /// </summary>
        public void PlayExclusive()
        {
            Play(true);
        }

        /// <summary>
        /// Stop playing the sound immediately and reset the sound to the beginning of the track.
        /// </summary>
        /// <remarks>A call to Stop when the sound is already stopped has no effects.</remarks>
        public void Stop()
        {
            if (engine.State == AudioEngineState.Invalidated)
                return;

            if (playState == PlayState.Stopped)
                return;

            if (soundSource == null)
            {
                AudioLayer.SourceStop(Source);
            }
            else
            {
                soundSource.Stop();
            }

            playState = PlayState.Stopped;
        }

        internal void ResetStateToDefault()
        {
            Pan = 0;
            Pitch = 1;
            Volume = 1;
            IsLooping = false;
            Stop();
        }

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        protected override void Destroy()
        {
            base.Destroy();

            if (IsDisposed)
                return;

            Stop();

            sound?.UnregisterInstance(this);

            if (engine.State == AudioEngineState.Invalidated)
                return;

            if (soundSource == null)
            {
                AudioLayer.SourceDestroy(Source);
            }
            else
            {
                soundSource.Dispose();
            }
        }

        /// <summary>
        /// Play the sound instance.
        /// </summary>
        /// <param name="stopSiblingInstances">if true any other istance of the same Sound will be stopped.</param>
        protected void Play(bool stopSiblingInstances)
        {
            if (engine.State == AudioEngineState.Invalidated || engine.State == AudioEngineState.Paused)
                return;

            if (PlayState == PlayState.Playing)
                return;

            if (stopSiblingInstances)
            {
                sound?.StopConcurrentInstances(this);
            }

            if (soundSource == null)
            {
                AudioLayer.SourcePlay(Source);
            }
            else
            {
                soundSource.Play();
            }

            playState = PlayState.Playing;
        }

        /// <summary>
        /// Gets the state of the SoundInstance.
        /// </summary>
        public PlayState PlayState
        {
            get
            {
                if (engine.State == AudioEngineState.Invalidated)
                    return PlayState.Stopped;

                if (playState == PlayState.Playing && (!soundSource?.IsPausedOrPlaying ?? !AudioLayer.SourceIsPlaying(Source)))
                {
                    Stop();
                }

                return playState;
            }
        }

        /// <summary>
        /// Gets the DynamicSoundSource, might be null if the sound is not using DynamicSoundSource, e.g. not streamed from disk or not using a DynamicSoundSource derived class as backing.
        /// </summary>
        public DynamicSoundSource DynamicSoundSource => soundSource;

        /// <summary>
        /// Sets the range of the sound to play.
        /// </summary>
        /// <param name="range">a PlayRange structure that describes the starting offset and ending point of the sound to play in seconds.</param>
        public void SetRange(PlayRange range)
        {
            if (engine.State == AudioEngineState.Invalidated)
                return;

            var state = PlayState;

            if (state == PlayState.Playing)
            {
                Stop();
            }

            if (soundSource == null)
            {
                AudioLayer.SourceSetRange(Source, range.Start.TotalSeconds, range.End.TotalSeconds);
            }
            else
            {
                soundSource.PlayRange = range;
            }

            if (state == PlayState.Playing)
            {
                Play();
            }
        }

        /// <summary>
        /// Gets the position in time of this playing instance.
        /// </summary>
        public TimeSpan Position
        {
            get
            {
                if (engine.State == AudioEngineState.Invalidated || !AudioLayer.SourceIsPlaying(Source) || PlayState == PlayState.Stopped)
                    return TimeSpan.Zero;
                var rangeStart = soundSource?.PlayRange.Start ?? TimeSpan.Zero;
                var position = TimeSpan.FromSeconds(AudioLayer.SourceGetPosition(Source)) + rangeStart;
                return position > sound.TotalLength ? rangeStart : position;
            }
        }
    }
}
