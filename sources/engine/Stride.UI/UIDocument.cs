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
    
    /// <summary>
    /// Specifies the sampling method to be used for this UI.
    /// </summary>
    public UIElementSampler Sampler { get; set; }
    
    /// <summary>
    /// Determines whether the UI should be full screen.
    /// </summary>
    public bool IsFullScreen { get; set; }
    
    /// <summary>
    /// The virtual resolution of the UI in virtual pixels.
    /// </summary>
    public Vector3 Resolution { get; set; }
    
    /// <summary>
    /// The actual size of the UI in world units. This value is ignored in fullscreen mode.
    /// </summary>
    public Vector3 Size { get; set; }
    
    /// <summary>
    /// specifies how the virtual resolution value should be interpreted.
    /// </summary>
    public ResolutionStretch ResolutionStretch { get; set; }
    
    /// <summary>
    /// Determines whether the UI should be displayed as billboard.
    /// </summary>
    /// <remarks>A billboard is rendered in world space and automatically rotated parallel to the screen.</remarks>
    public bool IsBillboard { get; set; }
    
    /// <summary>
    /// Determines whether the UI texts should be snapped to closest pixel.
    /// </summary>
    /// <remarks>If <c>true</c>, all the text of the UI is snapped to the closest pixel (pixel perfect). This is only effective if IsFullScreen or IsFixedSize is set, as well as IsBillboard.</remarks>
    public bool SnapText { get; set; }
    
    /// <summary>
    /// Determines whether the UI should be always a fixed size on the screen.
    /// </summary>
    /// <remarks>A fixed size component with a height of 1 unit will be 0.1 of the screen size.</remarks>
    public bool IsFixedSize { get; set; }
    
    /// <summary>
    /// Last registered position of the pointer on the <see cref="UIDocument"/>.
    /// </summary>
    /// <remarks>Ignores other <see cref="UIDocument"/>s.</remarks>
    public Vector2 LastPointerPosition { get; set; }

    /// <summary>
    /// Last element over which the pointer was registered on the <see cref="UIDocument"/>.
    /// </summary>
    /// <remarks>Ignores other <see cref="UIDocument"/>s.</remarks>
    public UIElement LastPointerOverElement { get; set; }

    /// <summary>
    /// Last element which received a pointer event on the <see cref="UIDocument"/>.
    /// </summary>
    /// <remarks>Ignores other <see cref="UIDocument"/>s.</remarks>
    public UIElement LastInteractedElement { get; set; }

    /// <summary>
    /// The last point in UI virtual world space that the pointer was over an element on the <see cref="UIDocument"/>..
    /// </summary>
    /// <remarks>Ignores other <see cref="UIDocument"/>s.</remarks>
    public Vector3 LastIntersectionPoint { get; set; }
}
