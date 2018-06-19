// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Editor.EditorGame.ViewModels;
using Xenko.Engine;
using Xenko.Rendering.LightProbes;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// Services to create light probes and display their gizmo.
    /// </summary>
    public interface IEditorGameLightProbeService : IEditorGameViewModelService
    {
        /// <summary>
        /// True if light probe volumes (wireframe) should be visible, false otherwise.
        /// </summary>
        bool IsLightProbeVolumesVisible { get; set; }

        /// <summary>
        /// Compute one bounce of light probes .
        /// </summary>
        /// <remarks>This won't reset light probe coefficients.</remarks>
        /// <returns></returns>
        Task<Dictionary<Guid, FastList<Color3>>> RequestLightProbesStep();

        /// <summary>
        /// Transfers light probes coefficients by calling <see cref="LightProbeProcessor.UpdateLightProbeCoefficients"/> (from <see cref="LightProbeComponent.Coefficients"/> to <see cref="LightProbeRuntimeData.Coefficients"/>).
        /// </summary>
        /// <returns></returns>
        Task UpdateLightProbeCoefficients();
    }
}
