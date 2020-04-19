// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.VirtualReality
{
    /// <summary>
    /// Describes what kind of object is being tracked at a given ID
    /// </summary>
    public enum DeviceClass
    {
        /// <summary>
        /// There is no device at this index
        /// </summary>
        Invalid,
        /// <summary>
        /// The device at this index is an HMD
        /// </summary>
        HMD,
        /// <summary>
        /// The device is a controller
        /// </summary>
        Controller,
        /// <summary>
        /// The device is a tracker
        /// </summary>
        GenericTracker,
        /// <summary>
        /// The device is a camera, Lighthouse base station, or other device that supplies tracking ground truth.
        /// </summary>
        TrackingReference,
        /// <summary>
        /// Accessories that aren't necessarily tracked themselves, but may redirect video output from other tracked devices
        /// </summary>
        DisplayRedirect
    }
}
