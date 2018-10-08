// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.VirtualReality
{
    public enum DeviceState
    {
        Invalid,
        OutOfRange,
        Valid,
    }

    public enum DeviceClass
    {
        Invalid,            //There is no device at this index
        HMD,                //The device at this index is an HMD
        Controller,         //The device is a controller
        GenericTracker,     //The device is a tracker
        TrackingReference,  //The device is a camera, Lighthouse base station, or other device that supplies tracking ground truth.
        DisplayRedirect     //Accessories that aren't necessarily tracked themselves, but may redirect video output from other tracked devices
    }
}
