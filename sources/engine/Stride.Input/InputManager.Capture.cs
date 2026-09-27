// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    public partial class InputManager
    {
        private bool maskCapturedInput = true;

        /// <summary>
        /// Raised when the owner that holds a device, or one pointer of a device, changes.
        /// </summary>
        public event EventHandler<DeviceCaptureChangedEventArgs> CaptureChanged;

        internal int CaptureVersion { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether game-facing reads hide captured devices and pointers. The default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, captures are still tracked and <see cref="CaptureChanged"/> is still raised, but no read is masked.
        /// </remarks>
        public bool MaskCapturedInput
        {
            get => maskCapturedInput;
            set
            {
                if (maskCapturedInput == value)
                    return;

                maskCapturedInput = value;
                foreach (var device in devices)
                    device.CaptureState.SetMaskingEnabled(value);
                CaptureVersion++;
            }
        }

        /// <summary>
        /// Captures a whole device for an owner, so that game-facing reads of the device report no input.
        /// </summary>
        /// <param name="device">The device to capture.</param>
        /// <param name="owner">The owner that takes the device.</param>
        /// <param name="priority">The priority of the capture. See <see cref="InputCapturePriority"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="owner"/> holds the device afterwards. A device held by another owner is taken only with a higher priority.
        /// </returns>
        /// <remarks>
        /// The capture lasts until the owner calls <see cref="Release"/> or <see cref="ReleaseAll"/>. Capturing again as the current owner updates the priority.
        /// </remarks>
        public bool TryCapture(IInputDevice device, object owner, int priority = InputCapturePriority.Hover)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(owner);

            var state = device.CaptureState;
            var previous = state.Owner;
            if (previous != null && !ReferenceEquals(previous, owner) && priority <= state.Priority)
                return false;

            state.SetOwner(owner, priority);
            if (!ReferenceEquals(previous, owner))
                OnCaptureChanged(device, null, previous, owner);
            return true;
        }

        /// <summary>
        /// Releases a device held by an owner. Does nothing if the owner does not hold the device.
        /// </summary>
        /// <param name="device">The device to release.</param>
        /// <param name="owner">The owner that holds the device.</param>
        public void Release(IInputDevice device, object owner)
        {
            ArgumentNullException.ThrowIfNull(device);

            var state = device.CaptureState;
            if (owner == null || !ReferenceEquals(state.Owner, owner))
                return;

            state.SetOwner(null, 0);
            OnCaptureChanged(device, null, owner, null);
        }

        /// <summary>
        /// Captures one pointer of a pointer device for an owner, so that game-facing reads of that pointer report no input.
        /// Other pointers of the device are not affected.
        /// </summary>
        /// <param name="device">The pointer device.</param>
        /// <param name="pointerId">The pointer to capture.</param>
        /// <param name="owner">The owner that takes the pointer.</param>
        /// <param name="priority">The priority of the capture. See <see cref="InputCapturePriority"/>.</param>
        /// <returns><c>true</c> if <paramref name="owner"/> holds the pointer afterwards.</returns>
        public bool TryCapturePointer(IPointerDevice device, int pointerId, object owner, int priority = InputCapturePriority.Hover)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentOutOfRangeException.ThrowIfNegative(pointerId);

            var state = device.CaptureState;
            var previous = state.GetPointerOwner(pointerId);
            if (previous != null && !ReferenceEquals(previous, owner) && priority <= state.GetPointerPriority(pointerId))
                return false;

            state.SetPointerOwner(pointerId, owner, priority);
            if (!ReferenceEquals(previous, owner))
                OnCaptureChanged(device, pointerId, previous, owner);
            return true;
        }

        /// <summary>
        /// Releases a pointer held by an owner. Does nothing if the owner does not hold the pointer.
        /// </summary>
        /// <param name="device">The pointer device.</param>
        /// <param name="pointerId">The pointer to release.</param>
        /// <param name="owner">The owner that holds the pointer.</param>
        public void ReleasePointer(IPointerDevice device, int pointerId, object owner)
        {
            ArgumentNullException.ThrowIfNull(device);

            var state = device.CaptureState;
            if (owner == null || !ReferenceEquals(state.GetPointerOwner(pointerId), owner))
                return;

            state.SetPointerOwner(pointerId, null, 0);
            OnCaptureChanged(device, pointerId, owner, null);
        }

        /// <summary>
        /// Releases every device and pointer held by an owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void ReleaseAll(object owner)
        {
            if (owner == null)
                return;

            foreach (var device in devices)
            {
                Release(device, owner);
                if (device is IPointerDevice pointer)
                {
                    for (int pointerId = 0; pointerId < pointer.CaptureState.PointerSlotCount; pointerId++)
                        ReleasePointer(pointer, pointerId, owner);
                }
            }
        }

        private void OnCaptureChanged(IInputDevice device, int? pointerId, object previousOwner, object newOwner)
        {
            CaptureVersion++;
            CaptureChanged?.Invoke(this, new DeviceCaptureChangedEventArgs(device, pointerId, previousOwner, newOwner));
        }

        private void DropCapture(IInputDevice device)
        {
            var state = device.CaptureState;
            if (state.Owner != null)
                Release(device, state.Owner);

            if (device is IPointerDevice pointer)
            {
                for (int pointerId = 0; pointerId < state.PointerSlotCount; pointerId++)
                {
                    if (state.GetPointerOwner(pointerId) is { } pointerOwner)
                        ReleasePointer(pointer, pointerId, pointerOwner);
                }
            }
        }
    }
}
