// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Diagnostics;

namespace Xenko.Media
{
    public class MediaSynchronizer
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger(nameof(MediaSynchronizer));

        public TimeSpan CurrentPresentationTime { get; private set; }

        public bool IsLooping { get; set; } = false;

        public PlayRange LoopRange { get; set; }

        public PlayState State { get; private set; } = PlayState.Stopped;

        public bool ReachedEndOfStream { get; private set; }

        //We can ask the scheduler to hold on to give some time for all media extractors to get ready (after a Seek requests for example)
        private TimeSpan syncUpWaitingTime = TimeSpan.Zero;
        private readonly TimeSpan defaultSyncUpWaitingTime = TimeSpan.FromSeconds(2);  //Max time we wait while the media extractors gets ready to play

        private TimeSpan userStateSeekTimeRequest;
        private TimeSpan timeToWaitBeforeCheckingForTerminationConditions;

        private CommandRequestStateEnum commandStateRequest = CommandRequestStateEnum.Undefined;

        private List<IMediaExtractor> mediaExtractors = new List<IMediaExtractor>();
        private List<IMediaPlayer> mediaPlayers = new List<IMediaPlayer>();
        private List<IMediaReader> mediaReaders = new List<IMediaReader>();

        //The media scheduler can sync up on the audio extractor
        private IMediaExtractor audioMediaExtractor = null;

        private static object lockObject = new object();

        private PlayRange playRange;
        private float speedFactor = 1f;

        //=================================================================================================
        //Command requests
        private enum CommandRequestStateEnum
        {
            Play,
            Pause,
            Stop,
            Seek,
            Undefined,
        }

        public float SpeedFactor
        {
            get => speedFactor;
            set
            {
                if (speedFactor == value)
                    return;

                speedFactor = value;
                ForEachSafe(mediaReaders, r => r.SpeedFactor = value);
            }
        }

        public PlayRange PlayRange
        {
            get => playRange;
            set
            {
                if (playRange == value)
                    return;

                playRange = value;
                if (playRange.Start > CurrentPresentationTime)
                    Seek(playRange.Start);
            }
        }

        public TimeSpan MediaDuration
        {
            get
            {
                lock (lockObject)
                {
                    //Return the duration of the first media (all media are expected to have the same duration)
                    return mediaExtractors.Count > 0 ? mediaExtractors[0].MediaDuration : TimeSpan.Zero;
                }
            }
        }

        public void Play()
        {
            ReachedEndOfStream = false;
            commandStateRequest = CommandRequestStateEnum.Play;
        }

        public void Pause()
        {
            commandStateRequest = CommandRequestStateEnum.Pause;
        }

        //seek value between [0, 1]
        public void Seek(double timePercentage)
        {
            Seek(TimeSpan.FromSeconds(timePercentage * MediaDuration.TotalSeconds));
        }

        public void Seek(TimeSpan seekTime)
        {
            commandStateRequest = CommandRequestStateEnum.Seek;
            userStateSeekTimeRequest = seekTime;
        }

        public void Stop()
        {
            commandStateRequest = CommandRequestStateEnum.Stop;
        }

        public void RegisterExtractor(IMediaExtractor extractor)
        {
            Register(mediaReaders, extractor);
            Register(mediaExtractors, extractor);

            if (extractor.MediaType == MediaType.Audio)
                audioMediaExtractor = extractor;
        }
        public void RegisterReader(IMediaReader reader)
        {
            Register(mediaReaders, reader);
        }

        public void RegisterPlayer(IMediaPlayer player)
        {
            Register(mediaReaders, player);
            Register(mediaPlayers, player);
        }

        private void Register<T>(List<T> list, T item)
        {
            if (item == null)
                return;

            lock (lockObject)
            {
                if (list.Contains(item))
                    return;

                list.Add(item);
            }
        }

        public void UnregisterExtractor(IMediaExtractor extractor)
        {
            Unregister(mediaExtractors, extractor);

            if (audioMediaExtractor == extractor)
                audioMediaExtractor = null;
        }

        public void UnregisterReader(IMediaReader reader)
        {
            Unregister(mediaReaders, reader);
        }

        public void UnregisterReader(IMediaPlayer player)
        {
            Unregister(mediaPlayers, player);
        }

        public void Unregister<T>(List<T> list, T item)
        {
            lock (lockObject)
            {
                list.Remove(item);
            }
        }

        private void SeekInternal(TimeSpan seekTime)
        {
            Logger.Verbose("Scheduler seeking at: " + seekTime);
            
            CurrentPresentationTime = seekTime;
            syncUpWaitingTime = defaultSyncUpWaitingTime;
            ForEachSafe(mediaReaders, r => r.Seek(seekTime));
        }

        private void StopInternal()
        {
            State = PlayState.Stopped;
            SeekInternal(playRange.Start);
            ForEachSafe(mediaPlayers, p => p.Stop());
        }

        public bool IsWaitingForSynchronization()
        {
            return syncUpWaitingTime > TimeSpan.Zero;
        }

        private bool HaveAllExtractorsReachedEOF()
        {
            lock (lockObject)
            {
                foreach (var extractor in mediaExtractors)
                {
                    if (!extractor.ReachedEndOfMedia())
                        return false;
                }
                return true;
            }
        }

        private bool HaveAllExtractorCompletedSeekRequest()
        {
            lock (lockObject)
            {
                foreach (var extractor in mediaExtractors)
                {
                    if (!extractor.SeekRequestCompleted())
                        return false;
                }
                return true;
            }
        }

        private void CheckAndUnregisterDisposedMedia()
        {
            lock (lockObject)
            {
                CheckAndUnregisterDisposedMedia(mediaExtractors);
                CheckAndUnregisterDisposedMedia(mediaPlayers);
                CheckAndUnregisterDisposedMedia(mediaReaders);

                if (audioMediaExtractor?.IsDisposed ?? true)
                    audioMediaExtractor = null;
            }
        }

        private void CheckAndUnregisterDisposedMedia<T>(List<T> itemsList) where T : IMediaReader
        {
            for (var i = itemsList.Count - 1; i >= 0; --i)
            {
                if (itemsList[i].IsDisposed)
                    itemsList.RemoveAt(i);
            }
        }

        public void Update(TimeSpan timeElapsed)
        {
            //check if some medias are disposed
            CheckAndUnregisterDisposedMedia();

            // waiting for the media extractors to synchronize
            if (IsWaitingForSynchronization())
            {
                syncUpWaitingTime -= timeElapsed;

                if (syncUpWaitingTime > TimeSpan.Zero && !HaveAllExtractorCompletedSeekRequest())
                    return;

                syncUpWaitingTime = TimeSpan.Zero;
            }

            // Update the media presentation time
            if (State == PlayState.Playing)
            {
                if (SpeedFactor != 1.0f)
                    timeElapsed = TimeSpan.FromMilliseconds(timeElapsed.TotalMilliseconds * SpeedFactor);

                lock (lockObject)
                {
                    CurrentPresentationTime += timeElapsed;

                    //If there is an audio extractor, we syncup out time with its own time
                    if (audioMediaExtractor != null)
                    {
                        //We sync up the time with the audio extractor in the case of we get unsync
                        var audioTime = audioMediaExtractor.MediaCurrentTime;
                        if (audioTime > TimeSpan.Zero)
                            CurrentPresentationTime = audioTime;
                    }
                }
            }

            //Process the user request
            switch (commandStateRequest)
            {
                case CommandRequestStateEnum.Play:
                    State = PlayState.Playing;
                    ForEachSafe(mediaPlayers, player => player.Play());
                    break;

                case CommandRequestStateEnum.Pause:
                    State = PlayState.Paused;
                    ForEachSafe(mediaPlayers, p => p.Pause());
                    break;

                case CommandRequestStateEnum.Stop:
                    StopInternal();
                    break;

                case CommandRequestStateEnum.Seek:
                    SeekInternal(userStateSeekTimeRequest);
                    break;

                case CommandRequestStateEnum.Undefined:
                    break;
            }
            commandStateRequest = CommandRequestStateEnum.Undefined;

            //Check if the medias have reached their EOF
            if (timeToWaitBeforeCheckingForTerminationConditions > TimeSpan.Zero)
            {
                timeToWaitBeforeCheckingForTerminationConditions -= timeElapsed;
            }
            else
            {
                var terminate = false;
                var currentTime = CurrentPresentationTime;

                //Check if we've met a termination condition
                if (playRange.End != TimeSpan.Zero && currentTime > playRange.End)
                {
                    terminate = true;
                }
                else if (IsLooping && LoopRange.End != TimeSpan.Zero && currentTime > LoopRange.End)
                {
                    terminate = true;
                }
                else if (State == PlayState.Playing)
                {
                    terminate = HaveAllExtractorsReachedEOF();
                }

                if (terminate)
                {
                    //we will check for new termination conditions after a small delay
                    timeToWaitBeforeCheckingForTerminationConditions = TimeSpan.FromMilliseconds(200);

                    if (IsLooping)
                        SeekInternal(LoopRange.Start); // this is setting CurrentPresentationTime to LoopRange.Start

                    if (CurrentPresentationTime > playRange.End)
                    {
                        StopInternal();
                        ReachedEndOfStream = true;
                    }
                }
            }
        }

        private void ForEachSafe<T>(List<T> itemsList, Action<T> action)
        {
            lock (lockObject)
            {
                foreach (var item in itemsList)
                    action(item);
            }
        }
    }
}
