// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal abstract class GestureRecognizer
    {
        private static readonly ThreadLocal<List<int>> FingerIdsCache = new ThreadLocal<List<int>>(() => new List<int>());
        
        protected readonly Dictionary<int, Vector2> FingerIdToBeginPositions = new Dictionary<int, Vector2>();

        protected readonly Dictionary<int, Vector2> FingerIdsToLastPos = new Dictionary<int, Vector2>();

        protected PoolListStruct<GestureEvent> CurrentGestureEvents;
        protected TimeSpan ElapsedSinceBeginning;
        protected TimeSpan ElapsedSinceLast;

        // avoid reallocation of the dictionary at each update call
        private readonly Dictionary<int, Vector2> fingerIdsToLastMovePos = new Dictionary<int, Vector2>();

        private bool hasGestureStarted;
        
        protected GestureRecognizer(GestureConfig config, float screenRatio)
        {
            CurrentGestureEvents = new PoolListStruct<GestureEvent>(8, NewEventFactory);
            Config = config;
            ScreenRatio = screenRatio;
        }
        
        internal float ScreenRatio { get; set; }

        protected virtual GestureConfig Config { get; }

        protected bool HasGestureStarted
        {
            get { return hasGestureStarted; }
            set
            {
                if (value && !hasGestureStarted)
                {
                    ElapsedSinceBeginning = TimeSpan.Zero;
                    ElapsedSinceLast = TimeSpan.Zero;
                }

                hasGestureStarted = value;
            }
        }
        
        protected virtual int NbOfFingerOnScreen => FingerIdsToLastPos.Count;
        
        public void ProcessPointerEvents(TimeSpan deltaTime, List<PointerEvent> events, List<GestureEvent> outputEvents)
        {
            ElapsedSinceBeginning += deltaTime;
            ElapsedSinceLast += deltaTime;

            CurrentGestureEvents.Clear();
            ProcessPointerEventsImpl(deltaTime, events);
            for (int i = 0; i < CurrentGestureEvents.Count; i++)
            {
                outputEvents.Add(CurrentGestureEvents[i]);
            }
        }

        protected abstract GestureEvent NewEventFactory();

        protected virtual void ProcessPointerEventsImpl(TimeSpan deltaTime, List<PointerEvent> events)
        {
            AnalysePointerEvents(events);
        }

        protected Vector2 ComputeMeanPosition(IEnumerable<Vector2> positions)
        {
            var count = 0;
            var accuPos = Vector2.Zero;
            foreach (var position in positions)
            {
                accuPos += position;
                ++count;
            }

            return accuPos / count;
        }

        protected void AnalysePointerEvents(List<PointerEvent> events)
        {
            foreach (var pointerEvent in events)
            {
                var eventType = pointerEvent.EventType;
                var id = pointerEvent.PointerId;
                var pos = pointerEvent.Position;

                switch (eventType)
                {
                    case PointerEventType.Pressed:
                        ProcessDownEventPointer(id, UnnormalizeVector(pos));
                        break;
                    case PointerEventType.Moved:
                        // just memorize the last position to avoid useless processing on move events
                        if (FingerIdToBeginPositions.ContainsKey(id))
                            fingerIdsToLastMovePos[id] = pos;
                        break;
                    case PointerEventType.Released:
                    case PointerEventType.Canceled:
                        // process previous move events
                        ProcessAndClearMovePointerEvents();

                        // process the up event
                        ProcessUpEventPointer(id, UnnormalizeVector(pos));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // process move events not followed by an 'up' event
            ProcessAndClearMovePointerEvents();
        }

        protected Vector2 NormalizeVector(Vector2 inputVector)
        {
            return ScreenRatio > 1 ? new Vector2(inputVector.X, inputVector.Y * ScreenRatio) : new Vector2(inputVector.X / ScreenRatio, inputVector.Y);
        }

        protected Vector2 UnnormalizeVector(Vector2 inputVector)
        {
            return ScreenRatio > 1 ? new Vector2(inputVector.X, inputVector.Y / ScreenRatio) : new Vector2(inputVector.X * ScreenRatio, inputVector.Y);
        }

        private void ProcessAndClearMovePointerEvents()
        {
            if (fingerIdsToLastMovePos.Count > 0)
            {
                FingerIdsCache.Value.Clear();
                FingerIdsCache.Value.AddRange(fingerIdsToLastMovePos.Keys);

                // Unnormalizes vectors here before utilization
                foreach (var id in FingerIdsCache.Value)
                    fingerIdsToLastMovePos[id] = UnnormalizeVector(fingerIdsToLastMovePos[id]);

                ProcessMoveEventPointers(fingerIdsToLastMovePos);
                fingerIdsToLastMovePos.Clear();
            }
        }

        protected abstract void ProcessDownEventPointer(int id, Vector2 pos);

        protected abstract void ProcessMoveEventPointers(Dictionary<int, Vector2> fingerIdsToMovePos);

        protected abstract void ProcessUpEventPointer(int id, Vector2 pos);
    }
}
