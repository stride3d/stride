// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// A gesture recognizer for continuous motions.
    /// </summary>
    internal abstract class GestureRecognizerContMotion : GestureRecognizer
    {
        protected bool GestureBeganEventSent { get; private set; }

        protected GestureRecognizerContMotion(GestureConfig config, float screenRatio)
            : base(config, screenRatio)
        {
        }

        protected override void ProcessDownEventPointer(int id, Vector2 pos)
        {
            UpdateGestureStartEndStatus(true, id, pos);
        }

        protected override void ProcessUpEventPointer(int id, Vector2 pos)
        {
            if (!FingerIdsToLastPos.ContainsKey(id))
                return;

            UpdateGestureStartEndStatus(false, id, pos);
        }

        private void UpdateGestureStartEndStatus(bool isKeyDown, int id, Vector2 pos)
        {
            var gestureWasStarted = HasGestureStarted;
            HasGestureStarted = (NbOfFingerOnScreen + (isKeyDown ? 1 : -1) == Config.RequiredNumberOfFingers);

            UpdateFingerDictionaries(isKeyDown, id, pos);

            if (HasGestureStarted) // beginning of a new gesture
            {
                InitializeGestureVariables();
            }
            else if (gestureWasStarted && GestureBeganEventSent) // end of the current gesture
            {
                AddGestureEventToCurrentList(GestureState.Ended);
            }
        }

        protected abstract void InitializeGestureVariables();
        
        protected override void ProcessMoveEventPointers(Dictionary<int, Vector2> fingerIdsToMovePos)
        {
            // update current finger positions.
            foreach (var id in fingerIdsToMovePos.Keys)
                FingerIdsToLastPos[id] = fingerIdsToMovePos[id];

            if (!HasGestureStarted) // nothing more to do is the gesture has not started yet
                return;

            UpdateGestureVarsAndPerfomChecks();

            if (!GestureBeganEventSent && HasGestureStarted && GestureBeginningConditionFulfilled())
            {
                AddGestureEventToCurrentList(GestureState.Began);
                GestureBeganEventSent = true;
            }

            if (GestureBeganEventSent)
                AddGestureEventToCurrentList(HasGestureStarted ? GestureState.Changed : GestureState.Ended);
        }

        protected abstract void UpdateGestureVarsAndPerfomChecks();

        protected abstract bool GestureBeginningConditionFulfilled();

        private void UpdateFingerDictionaries(bool isKeyDown, int id, Vector2 pos)
        {
            if (isKeyDown)
            {
                FingerIdToBeginPositions[id] = pos;
                FingerIdsToLastPos[id] = pos;
            }
            else
            {
                FingerIdToBeginPositions.Remove(id);
                FingerIdsToLastPos.Remove(id);
            }
        }

        protected virtual void AddGestureEventToCurrentList(GestureState state)
        {
            ElapsedSinceLast = TimeSpan.Zero;

            if (state == GestureState.Ended)
                GestureBeganEventSent = false;
        }
    }
}
