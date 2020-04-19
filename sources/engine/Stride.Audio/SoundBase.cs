using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Audio
{
    /// <summary>
    /// Base class for a Sound content.
    /// </summary>
    /// <remarks>
    /// Sound is played with a <see cref="SoundInstance"/>.
    /// </remarks>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public abstract class SoundBase : ComponentBase
    {
        /// <summary>
        /// Create the audio engine to the sound base instance.
        /// </summary>
        /// <param name="engine">A valid AudioEngine.</param>
        /// <exception cref="ArgumentNullException">The engine argument is null.</exception>
        internal void AttachEngine(AudioEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            AudioEngine = engine;
        }

        [DataMemberIgnore]
        internal AudioEngine AudioEngine { get; private set; }

        /// <summary>
        /// Current instances of the SoundEffect.
        /// We need to keep track of them to stop and dispose them when the soundEffect is disposed.
        /// </summary>
        [DataMemberIgnore]
        internal readonly List<SoundInstance> Instances = new List<SoundInstance>();
        
        internal int Channels { get; set; } = 2;
        
        [DataMemberIgnore]
        internal AudioEngineState EngineState => AudioEngine.State;

        internal int MaxPacketLength { get; set; }

        internal int NumberOfPackets { get; set; }

        internal int SampleRate { get; set; } = 44100;

        internal bool Spatialized { get; set; }

        /// <summary>
        /// The number of SoundEffect Created so far. Used only to give a unique name to the SoundEffect.
        /// </summary>
        private static int soundEffectCreationCount;

        /// <summary>
        /// Gets the total length in time of the Sound.
        /// </summary>
        public TimeSpan TotalLength => TimeSpan.FromSeconds(((double)NumberOfPackets * (double)CompressedSoundSource.SamplesPerFrame) / (double)SampleRate);

        /// <summary>
        /// Create a new sound effect instance of the sound effect. 
        /// Each instance that can be played and localized independently from others.
        /// </summary>
        /// <returns>A new sound instance</returns>
        /// <exception cref="ObjectDisposedException">The sound has already been disposed</exception>
        public abstract SoundInstance CreateInstance(AudioListener listener = null, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small);

        internal void Attach(AudioEngine engine)
        {
            AttachEngine(engine);

            Name = "Sound Effect " + Interlocked.Add(ref soundEffectCreationCount, 1);

            // register the sound to the AudioEngine so that it will be properly freed if AudioEngine is disposed before this.
            AudioEngine.RegisterSound(this);
        }

        public int GetCountChannels()
        {
            return Channels;
        }

        internal void CheckNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("this");
        }

        /// <summary>
        /// Stop all registered instances of the <see cref="SoundBase"/>.
        /// </summary>
        internal void StopAllInstances()
        {
            foreach (var instance in Instances)
                instance.Stop();
        }

        /// <summary>
        /// Stop all registered instances different from the provided main instance
        /// </summary>
        /// <param name="mainInstance">The main instance of the sound effect</param>
        internal void StopConcurrentInstances(SoundInstance mainInstance)
        {
            foreach (var instance in Instances)
            {
                if (instance != mainInstance)
                    instance.Stop();
            }
        }

        /// <summary>
        /// Unregister a disposed Instance.
        /// </summary>
        /// <param name="instance"></param>
        internal void UnregisterInstance(SoundInstance instance)
        {
            if (!Instances.Remove(instance))
                throw new AudioSystemInternalException("Tried to unregister soundEffectInstance while not contained in the instance list.");
        }

        /// <summary>
        /// Register a new instance to the soundEffect.
        /// </summary>
        /// <param name="instance">new instance to register.</param>
        protected void RegisterInstance(SoundInstance instance)
        {
            Instances.Add(instance);
            intancesCreationCount++;
        }

        /// <summary>
        /// The number of Instances Created so far by this SoundEffect. Used only to give a unique name to the SoundEffectInstance.
        /// </summary>
        protected int intancesCreationCount;

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        protected override void Destroy()
        {
            if (AudioEngine == null || AudioEngine.State == AudioEngineState.Invalidated)
                return;
        }
    }
}
