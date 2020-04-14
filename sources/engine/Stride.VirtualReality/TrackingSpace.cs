// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.VirtualReality
{
    /// <summary>
    /// Identifies which style of tracking origin the application wants to use for the poses it is requesting
    /// </summary>
    public enum TrackingSpace
    {
        /// <summary>
        /// Poses are provided relative to the seated zero pose
        /// </summary>
        Seated,
        /// <summary>
        /// Poses are provided relative to the safe bounds configured by the user
        /// </summary>
        Standing,
        /// <summary>
        /// Poses are provided in the coordinate system defined by the driver.  It has Y up and is unified for devices of the same driver. You usually don't want this one.
        /// </summary>
        RawAndUncalibrated
    }
}
