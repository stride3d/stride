// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.BepuPhysics.Definitions;

namespace Stride.BepuPhysics.Systems;

/// <summary>
/// See <see cref="DefaultValueAttribute"/>,
/// this one specifies that the default value for the member is <see cref="SceneBasedSimulationSelector.Shared"/>
/// </summary>
public class DefaultValueIsSceneBasedAttribute() : DefaultValueAttribute(SceneBasedSimulationSelector.Shared);
