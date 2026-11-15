// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Rendering.LightProbes;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;

/// <summary>
/// The light probes of a scene or prefab editor: the volumes between the light probes (a wireframe), and the computation
/// of their coefficients (the lighting toolbar of the WPF scene editor).
/// </summary>
public interface IEditorGameLightProbeService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets or sets whether the volumes between the light probes show.
    /// </summary>
    bool IsLightProbeVolumesVisible { get; set; }

    /// <summary>
    /// Computes one bounce of the light probes of the scene (game thread).
    /// </summary>
    /// <remarks>
    /// This doesn't reset the coefficients of the light probes.
    /// </remarks>
    /// <returns>The coefficients of each light probe, by the id of its entity; empty when the scene has no light probe.</returns>
    Task<Dictionary<Guid, List<Color3>>> RequestLightProbesStep();

    /// <summary>
    /// Transfers the coefficients of the light probes to the renderer (game thread), from <see cref="LightProbeComponent.Coefficients"/>
    /// to <see cref="LightProbeRuntimeData.Coefficients"/>, through <see cref="LightProbeProcessor.UpdateLightProbeCoefficients"/>.
    /// </summary>
    Task UpdateLightProbeCoefficients();
}
