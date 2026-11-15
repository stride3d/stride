using System;
using Stride.Media;

namespace Stride.Audio
{
    /// <summary>
    /// A Sound Instance where the SoundSource comes from a StreamedBufferSoundSource, and implementing ISynchronizedMediaExtractor interface
    /// </summary>
    public class SoundInstanceStreamedBuffer : SoundInstance, IMediaPlayer
    {
        private StreamedBufferSoundSource streamedSource;
        
        private MediaSynchronizer scheduler;

        public float SpeedFactor
        {
            get => Pitch;
            set
            {
                if (Pitch == value || engine.State == AudioEngineState.Invalidated)
                    return;

                //Hum... It's working for SpeedRate up to 2, but not scaling well with higher values
                Pitch = value;
                streamedSource.SpeedFactor = value;
            }
        }

        internal SoundInstanceStreamedBuffer(MediaSynchronizer scheduler, StreamedBufferSound soundStreamedBuffer, string mediaDataUrl, long startPosition, long length, 
            AudioListener listener, bool useHrtf = false, float directionalFactor = 0.0f, HrtfEnvironment environment = HrtfEnvironment.Small)
        {
            this.scheduler = scheduler;
            Listener = listener;
            engine = soundStreamedBuffer.AudioEngine;
            sound = soundStreamedBuffer;
            spatialized = soundStreamedBuffer.Spatialized;

            if (engine.State == AudioEngineState.Invalidated)
                return;

            //We first create the soundSource so that it gets initialized and can give us sampleRate and Channels info
            soundSource = streamedSource = new StreamedBufferSoundSource(this, this.scheduler, mediaDataUrl, startPosition, length);

            //Create the AudioLayer source
            Source = AudioLayer.SourceCreate(listener.Listener, soundStreamedBuffer.SampleRate, streamedSource.MaxNumberOfBuffers, 
                soundStreamedBuffer.Channels == 1, spatialized, true, useHrtf, directionalFactor, environment);

            if (Source.Ptr == IntPtr.Zero)
                throw new Exception("Failed to create an AudioLayer Source");

            ResetStateToDefault();
        }

        public void Seek(TimeSpan mediaTime)
        {
            streamedSource.Seek(mediaTime);
        }

        public bool ReachedEndOfMedia()
        {
            return streamedSource.ReachedEndOfMedia();
        }

        public bool SeekRequestCompleted()
        {
            return streamedSource.SeekRequestCompleted();
        }
    }
}
