// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

namespace Stride.Graphics;

/// <summary>
///   Defines common DXGI constants used by Stride.
/// </summary>
internal static class DxgiConstants
{
    /// <summary>
    ///   A DXGI object that was being queried has not been found.
    /// </summary>
    public const int ErrorNotFound = unchecked((int) 0x887A0002);

    /// <summary>
    ///   A DXGI object that was being queried has not been found.
    /// </summary>
    public const int ErrorNotCurrentlyAvailable = unchecked((int) 0x887A0022);

    /// <summary>
    ///   A resource was still in use by the GPU when there was a try to map the resource to CPU memory immediately.
    /// </summary>
    public const int ErrorWasStillDrawing = unchecked((int) 0x887A000A);

    /// <summary>
    ///   A resource access flag that identifies a shared resource with write access.
    /// </summary>
    public const uint SharedAccessResourceWrite = 1;

    /// <summary>
    ///   A factory creation flag that instructs the runtime to load the debug layer (<c>DXGIDebug.dll</c>)
    ///   alongside the factory object.
    /// </summary>
    public const uint CreateFactoryDebug = 1;

    /// <summary>
    ///   A flag that instructs the runtime to not use the <c>ALT+ENTER</c> key combination
    ///   to switch to full screen.
    /// </summary>
    public const uint WindowAssociation_NoAltEnter = 2;

    /// <summary>
    ///   Identifies the reason a graphics device was unavailable when a command tried to be executed on it.
    /// </summary>
    public enum DeviceRemoveReason
    {
        // From DXGI_ERROR constants in Winerror.h

        None = 0,   // S_OK -- No error

        DeviceHung = unchecked((int) 0x887A0006),           // DEVICE_HUNG
        DeviceRemoved = unchecked((int) 0x887A0005),        // DEVICE_REMOVED
        DeviceReset = unchecked((int) 0x887A0007),          // DEVICE_RESET
        DriverInternalError = unchecked((int) 0x887A0020),  // DRIVER_INTERNAL_ERROR
        InvalidCall = unchecked((int) 0x887A0001)           // INVALID_CALL
    }
}

#endif
