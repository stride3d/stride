// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.UI;

namespace Stride.UI;

/// <summary>
/// Describes a UI and the data for displaying and interacting with it in <see cref="UISystem"/>.
/// </summary>
public class UIDocument
{
    /// <summary>
    /// Whether the UI is active and will be rendered and interacted with by user input.
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// A world space matrix that describes the position, rotation, and scale of the UI in a scene when the UI is not a FullScreen UI.
    /// </summary>
    public Matrix WorldMatrix { get; set; }
    
    /// <summary>
    /// The group to render the UI in.
    /// </summary>
    public RenderGroup RenderGroup { get; set; }
    
    /// <summary>
    /// A page containing the hierarchy that describes the UI.
    /// </summary>
    public UIPage Page { get; set; }
    
    public UIElementSampler Sampler { get; set; }
    public bool IsFullScreen { get; set; }
    public Vector3 Resolution { get; set; }
    public Vector3 Size { get; set; }
    public ResolutionStretch ResolutionStretch { get; set; }
    public bool IsBillboard { get; set; }
    public bool SnapText { get; set; }
    public bool IsFixedSize { get; set; }
    
    /// <summary>
    /// Last registered position of the mouse
    /// </summary>
    public Vector2 LastMousePosition { get; set; }

    /// <summary>
    /// Last element over which the mouse cursor was registered
    /// </summary>
    public UIElement LastPointerOverElement { get; set; }

    /// <summary>
    /// Last element which received a touch/click event
    /// </summary>
    public UIElement LastTouchedElement { get; set; }

    public Vector3 LastIntersectionPoint { get; set; }
}
