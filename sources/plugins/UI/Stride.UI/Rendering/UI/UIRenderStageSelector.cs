// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Stride.Rendering.UI;

/// <summary>
///   Defines how a UI <see cref="RenderObject"/> is assigned to a <see cref="RenderStage"/>
///   according to its properties:
///   <list type="bullet">
///     <item>
///       Full-screen UI elements are assigned to the "after post-processing effects" stage if available.
///     </item>
///     <item>
///       Other UI elements (or all of them if no after post-processing effects stage is available)
///       are assigned to the transparent stage.
///     </item>
///   </list>
/// </summary>
public class UIRenderStageSelector : RenderStageSelector
{
    /// <summary>
    ///   The mask that determines which render groups this selector applies to.
    /// </summary>
    /// <value>
    ///   The render group mask that determines which render groups this selector applies to.
    ///   Defaults to <see cref="RenderGroupMask.All"/>.
    /// </value>
    [DefaultValue(RenderGroupMask.All)]
    public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

    /// <summary>
    ///   The transparent render stage to which non-fullscreen UI elements are assigned.
    /// </summary>
    public RenderStage TransparentRenderStage { get; set; }

    /// <summary>
    ///   The "after post-processing effects" render stage to which full-screen UI elements are assigned if available.
    /// </summary>
    /// <value>
    ///   The "after post-processing effects" render stage. As this stage may not always be available (it's optional),
    ///   this property may be <see langword="null"/> if the stage is not available.
    /// </value>
    public RenderStage AfterPostEffectsRenderStage { get; set; }

    /// <summary>
    ///   The name of the effect to use for the active render stage.
    /// </summary>
    public string EffectName { get; set; }


    /// <summary>
    ///   Processes the specified render object and assigns it to the appropriate render stage based on its properties.
    /// </summary>
    /// <param name="renderObject">The render object to process.</param>
    public override void Process(RenderObject renderObject)
    {
        // Discard render objects that are not in the selected render group
        if (((RenderGroupMask)(1u << (int)renderObject.RenderGroup) & RenderGroup) == 0)
            return;

        // If the render object is a full-screen UI element and an after post effects stage is available, use it;
        // otherwise, use the transparent stage as the default stage for UI elements.
        var stage = renderObject is RenderUIElement { IsFullScreen: true } && AfterPostEffectsRenderStage != null
            ? AfterPostEffectsRenderStage
            : TransparentRenderStage;

        if (stage != null)
        {
            renderObject.ActiveRenderStages[stage.Index] = new ActiveRenderStage(EffectName);
        }
    }
}
