#if XENKO_PLATFORM_ANDROID && XENKO_VIDEO_MEDIACODEC

using System;
using System.Collections.Concurrent;
using Xenko.Core.Diagnostics;
using Xenko.Core;
using Xenko.Media;

using Android.Media;
using Android.Views;
using System.Threading;
using Xenko.Audio;

namespace Xenko.Video
{
    public abstract class MediaCodecExtractorBase<T>: DisposeBase, IMediaExtractor, IMediaPlayer, IMediaReader where T: class
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger(nameof(MediaCodecExtractorBase<T>));
        
        public abstract MediaType MediaType { get; }

        public T MediaMetadata { get; protected set; }

        public TimeSpan MediaDuration { get; private set; }

        public TimeSpan MediaCurrentTime { get; private set; }

        public float SpeedFactor { get; set; } = 1f;

        public bool HasAudioTrack { get; private set; } = false;

        //The media scheduler will check this field to determine whether he can stop waiting for the extractors getting ready
        public volatile bool isSeekRequestCompleted = true;
        
        private enum SchedulerAsyncCommandEnum
        {
            Undefined,
            Play,
            Pause,
            Stop,
            SeekAndSyncUp,
            Dispose,
        }

        private struct SchedulerAsyncCommand
        {
            public readonly SchedulerAsyncCommandEnum Command;
            public readonly TimeSpan SeekTargetTime;

            public SchedulerAsyncCommand(SchedulerAsyncCommandEnum command, TimeSpan? targetTime = null)
            {
                Command = command;
                SeekTargetTime = targetTime ?? TimeSpan.Zero;
            }
        }

        private static MediaCodecList ListSupportedMediaCodecs = new MediaCodecList(MediaCodecListKind.RegularCodecs);

        private readonly ConcurrentQueue<SchedulerAsyncCommand> commands = new ConcurrentQueue<SchedulerAsyncCommand>();
        
        private Surface decoderOutputSurface = null;  //The surface on which the codec will extract the video

        private SchedulerAsyncCommandEnum currentState;

        private bool isInitialized;
        protected VideoInstance VideoInstance;
        private Thread workerThread;

        private Java.IO.File inputFile = null;
        private Java.IO.FileInputStream inputFileDescriptor;

        //the media extractor and decoder
        private MediaExtractor mediaExtractor = null;
        protected MediaCodec MediaDecoder = null;
        private int mediaTrackIndex = -1;

        //Variables used for managing the synchornization and the main extraction loop
        private bool inputExtractionDone;
        private volatile bool isEOF;

        protected MediaSynchronizer Scheduler { get; }

        protected MediaCodecExtractorBase(VideoInstance videoInstance, MediaSynchronizer scheduler, Surface decoderOutputSurface = null)
        {
            Scheduler = scheduler;
            VideoInstance = videoInstance;
            this.decoderOutputSurface = decoderOutputSurface;

            currentState = SchedulerAsyncCommandEnum.Stop;
        }

        public void Initialize(IServiceRegistry services, string url, long startPosition, long length)
        {
            if (isInitialized)
                return;

            try
            {
                inputFile = new Java.IO.File(url);
                if (!inputFile.CanRead())
                    throw new Exception(string.Format("Unable to read: {0} ", inputFile.AbsolutePath));

                inputFileDescriptor = new Java.IO.FileInputStream(inputFile);

                // ===================================================================================================
                // Initialize the audio media extractor
                mediaExtractor = new MediaExtractor();
                mediaExtractor.SetDataSource(inputFileDescriptor.FD, startPosition, length);

                var videoTrackIndex = FindTrack(mediaExtractor, MediaType.Video);
                var audioTrackIndex = FindTrack(mediaExtractor, MediaType.Audio);
                HasAudioTrack = audioTrackIndex >= 0;

                mediaTrackIndex = MediaType == MediaType.Audio ? audioTrackIndex : videoTrackIndex;
                if (mediaTrackIndex < 0)
                    throw new Exception(string.Format($"No {MediaType} track found in: {inputFile.AbsolutePath}"));

                mediaExtractor.SelectTrack(mediaTrackIndex);

                var trackFormat = mediaExtractor.GetTrackFormat(mediaTrackIndex);
                MediaDuration = TimeSpanExtensions.FromMicroSeconds(trackFormat.GetLong(MediaFormat.KeyDuration));

                ExtractMediaMetadata(trackFormat);

                // Create a MediaCodec mediadecoder, and configure it with the MediaFormat from the mediaExtractor
                // It's very important to use the format from the mediaExtractor because it contains a copy of the CSD-0/CSD-1 codec-specific data chunks.
                var mime = trackFormat.GetString(MediaFormat.KeyMime);
                MediaDecoder = MediaCodec.CreateDecoderByType(mime);
                MediaDecoder.Configure(trackFormat, decoderOutputSurface, null, 0);

                isInitialized = true;

                StartWorker();
            }
            catch (Exception e)
            {
                Release();
                throw e;
            }
        }

        protected abstract void ExtractMediaMetadata(MediaFormat format);

        public void Play()
        {
            commands.Enqueue(new SchedulerAsyncCommand(SchedulerAsyncCommandEnum.Play));
        }

        public void Pause()
        {
            commands.Enqueue(new SchedulerAsyncCommand(SchedulerAsyncCommandEnum.Pause));
        }

        public void Stop()
        {
            commands.Enqueue(new SchedulerAsyncCommand(SchedulerAsyncCommandEnum.Stop));
        }

        public void Seek(TimeSpan seekTime)
        {
            isSeekRequestCompleted = false;
            commands.Enqueue(new SchedulerAsyncCommand(SchedulerAsyncCommandEnum.SeekAndSyncUp, seekTime));
        }
        public bool SeekRequestCompleted()
        {
            return isSeekRequestCompleted;
        }

        public bool ReachedEndOfMedia()
        {
            return isEOF;
        }

        private void Release()
        {
            Scheduler.UnregisterExtractor(this); //to avoid receiving any more event from the scheduler
            
            MediaDecoder?.Stop();
            MediaDecoder?.Release();
            MediaDecoder = null;

            mediaExtractor?.Release();
            mediaExtractor = null;

            inputFile = null;
            MediaMetadata = null;
            MediaDuration = TimeSpan.Zero;

            inputFileDescriptor?.Close();
            inputFileDescriptor = null;

            isInitialized = false;
        }


        protected override void Destroy()
        {
            commands.Enqueue(new SchedulerAsyncCommand(SchedulerAsyncCommandEnum.Dispose));

            workerThread?.Join();
            workerThread = null;
        }

        protected void SeekMediaAt(TimeSpan expectedTime)
        {
            isSeekRequestCompleted = false;
            MediaDecoder.Flush();
            mediaExtractor.SeekTo(expectedTime.TotalMicroSeconds(), MediaExtractorSeekTo.PreviousSync);
            inputExtractionDone = false;
            isEOF = false;
        }

        private bool ProcessCommandsAndUpdateCurrentState()
        {
            var anyCommandProcessed = !commands.IsEmpty;

            while (!commands.IsEmpty)
            {
                SchedulerAsyncCommand asyncCommand;
                if (!commands.TryDequeue(out asyncCommand))
                    continue;
                
                switch (asyncCommand.Command)
                {
                    case SchedulerAsyncCommandEnum.SeekAndSyncUp:
                        SeekMediaAt(asyncCommand.SeekTargetTime);
                        return anyCommandProcessed;

                    case SchedulerAsyncCommandEnum.Dispose:
                        currentState = SchedulerAsyncCommandEnum.Dispose;
                        Release();
                        return anyCommandProcessed;

                    case SchedulerAsyncCommandEnum.Pause:
                    case SchedulerAsyncCommandEnum.Play:
                    case SchedulerAsyncCommandEnum.Stop:
                        currentState = asyncCommand.Command;
                        break;
                }
            }

            return anyCommandProcessed;
        }

        private void StartWorker()
        {
            if (workerThread != null) // thread already running
                return;

            workerThread = new Thread(ExtractMediaWorkerFunction);
            workerThread.Start();
        }

        private void ExtractMediaWorkerFunction()
        {
            try
            {
                ExtractMedia();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Logger.Debug("Media Extraction done");
                Release();
            }
        }

        private void ExtractMedia()
        {
            if (MediaDecoder == null)
                throw new InvalidOperationException("The Media Codec Extractor has not been initialized");

            if (!isInitialized)
                throw new InvalidOperationException("The Media Codec has not been initialized for a media");
            
            var bufferInfo = new MediaCodec.BufferInfo();
            var waitDefaultTime = TimeSpan.FromMilliseconds(10);

            MediaDecoder.Start();
            while (true)
            {
                var waitTime = waitDefaultTime; // time to wait at the end of the loop iteration

                //Process the commands
                if (ProcessCommandsAndUpdateCurrentState())
                    waitTime = TimeSpan.Zero;

                // terminate the thread on disposal
                if (currentState == SchedulerAsyncCommandEnum.Dispose)
                    return;

                //=================================================================================================
                //Extract video inputs
                if (!inputExtractionDone)
                {
                    int inputBufIndex = MediaDecoder.DequeueInputBuffer(0);
                    if (inputBufIndex >= 0)
                    {
                        waitTime = TimeSpan.Zero;
                        var inputBuffer = MediaDecoder.GetInputBuffer(inputBufIndex);

                        // Read the sample data into the ByteBuffer.  This neither respects nor updates inputBuf's position, limit, etc.
                        int chunkSize = mediaExtractor.ReadSampleData(inputBuffer, 0);
                        if (chunkSize > 0)
                        {
                            if (mediaExtractor.SampleTrackIndex != mediaTrackIndex)
                                throw new Exception($"Got media sample from track {mediaExtractor.SampleTrackIndex}, track expected {mediaTrackIndex}");
                            
                            MediaDecoder.QueueInputBuffer(inputBufIndex, 0, chunkSize, mediaExtractor.SampleTime, 0);
                            mediaExtractor.Advance();
                        }
                        else // End of stream -- send empty frame with EOS flag set.
                        {
                            MediaDecoder.QueueInputBuffer(inputBufIndex, 0, 0, 0L, MediaCodecBufferFlags.EndOfStream);
                            inputExtractionDone = true;
                        }
                    }
                    else
                    {
                        //do nothing: the input buffer queue is full (we need to output them first)
                    }
                }

                //=================================================================================================
                // Process the output buffers
                if (ShouldProcessDequeueOutput(ref waitTime))
                {
                    int indexOutput = MediaDecoder.DequeueOutputBuffer(bufferInfo, 0);
                    switch (indexOutput)
                    {
                        case (int)MediaCodecInfoState.TryAgainLater: // decoder not ready yet (haven't processed input yet)
                        case (int)MediaCodecInfoState.OutputBuffersChanged: //deprecated: we just ignore it
                            break;

                        case (int)MediaCodecInfoState.OutputFormatChanged:
                            Logger.Verbose("decoder output format changed: " + MediaDecoder.OutputFormat.ToString());
                            break;

                        default: // the index of the output buffer

                            if (indexOutput < 0)
                            {
                                Logger.Warning("unexpected index from decoder.dequeueOutputBuffer: " + indexOutput);
                                isEOF = true;
                                break;
                            }

                            if ((bufferInfo.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                            {
                                isEOF = true;
                                MediaDecoder.ReleaseOutputBuffer(indexOutput, false);
                                break;
                            }

                            MediaCurrentTime = TimeSpanExtensions.FromMicroSeconds(bufferInfo.PresentationTimeUs);

                            ProcessOutputBuffer(bufferInfo, indexOutput);

                            break;
                    }
                }
                
                if (waitTime > TimeSpan.Zero)
                {
                    // sleep required time to avoid active looping
                    // Note: do not sleep more than 'waitDefaultTime' to continue processing play commands
                    Utilities.Sleep(TimeSpanExtensions.Min(waitDefaultTime, waitTime)); 
                }
            }
        }

        protected abstract bool ShouldProcessDequeueOutput(ref TimeSpan waitTime);


        protected abstract void ProcessOutputBuffer(MediaCodec.BufferInfo bufferInfo, int outputIndex);
        
        // Selects the video track, if any.
        private static int FindTrack(MediaExtractor extractor, MediaType trackType)
        {
            string prefix = null;
            switch (trackType)
            {
                case MediaType.Video:
                    prefix = "video/";
                    break;
                case MediaType.Audio:
                    prefix = "audio/";
                    break;
                default:
                    return -1;
            }

            // Select the first video track we find, ignore the rest.
            int numTracks = extractor.TrackCount;
            for (int i = 0; i < numTracks; i++)
            {
                MediaFormat format = extractor.GetTrackFormat(i);
                String mime = format.GetString(MediaFormat.KeyMime);
                if (mime.StartsWith(prefix))
                {
                    if (ListSupportedMediaCodecs.FindDecoderForFormat(format) != null)
                    {
                        Logger.Verbose(string.Format("Extractor selected track {0} ({1}): {2}", i, mime, format));
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}

#endif
