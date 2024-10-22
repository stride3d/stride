﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.UI.Events;

namespace Stride.UI;

/// <summary>
/// Routed event arguments associated with a pointer event.
/// </summary>
public class PointerEventArgs : RoutedEventArgs
{
    // Pointer properties... 
    
    /// <inheritdoc cref="PointerEvent.Device"/>
    public IInputDevice Device { get; protected internal set; }

    /// <inheritdoc cref="PointerEvent.PointerId"/>
    public int PointerId { get; internal set; }

    /// <inheritdoc cref="PointerEvent.AbsolutePosition"/>
    public Vector2 AbsolutePosition => Position * Pointer.SurfaceSize;

    /// <inheritdoc cref="PointerEvent.Position"/>
    public Vector2 Position { get; internal set; }

    /// <inheritdoc cref="PointerEvent.AbsoluteDeltaPosition"/>
    public Vector2 AbsoluteDeltaPosition => DeltaPosition * Pointer.SurfaceSize;

    /// <inheritdoc cref="PointerEvent.DeltaPosition"/>
    public Vector2 DeltaPosition { get; internal set; }

    /// <inheritdoc cref="PointerEvent.DeltaTime"/>
    public TimeSpan DeltaTime { get; internal set; }

    /// <inheritdoc cref="PointerEvent.EventType"/>
    public PointerEventType EventType { get; internal set; }

    /// <inheritdoc cref="PointerEvent.IsDown"/>
    public bool IsDown { get; internal set; }

    /// <inheritdoc cref="PointerEvent.Pointer"/>
    public IPointerDevice Pointer => (IPointerDevice)Device;

    // UI specific properties...
    
    /// <summary>
    /// Gets the position of the pointer in the UI virtual world space (in virtual pixels).
    /// </summary>
    /// <remarks>
    /// WorldPosition is between [-resolution/2, +resolution/2]. (-resolution.X/2, -resolution.Y/2) is the top left corner,
    /// (+resolution.X/2, +resolution.Y/2) is the bottom right corner, with (0, 0) being the center.
    /// resolution is the resolution of the page. The Z axis is un-used.
    /// </remarks>
    public Vector3 WorldPosition { get; internal set; }

    /// <summary>
    /// Gets the translation of the pointer in the UI virtual world space (in virtual pixels).
    /// </summary>
    /// <remarks>
    /// World space is between [-resolution/2, +resolution/2]. (-resolution.X/2, -resolution.Y/2) is the top left corner,
    /// (+resolution.X/2, +resolution.Y/2) is the bottom right corner, with (0, 0) being the center.
    /// resolution is the resolution of the page. The Z axis is un-used.
    /// </remarks>
    public Vector3 WorldDeltaPosition { get; internal set; }

    public PointerEventArgs Clone()
    {
        return new PointerEventArgs()
        {
            Device = Device,
            PointerId = PointerId,
            Position = Position,
            DeltaPosition = DeltaPosition,
            DeltaTime = DeltaTime,
            EventType = EventType,
            IsDown = IsDown,
            WorldPosition = WorldPosition,
            WorldDeltaPosition =  WorldDeltaPosition
        };
    }
}