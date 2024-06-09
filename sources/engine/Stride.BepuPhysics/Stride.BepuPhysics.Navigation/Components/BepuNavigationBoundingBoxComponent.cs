// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Navigation.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Navigation;

namespace Stride.BepuPhysics.Navigation.Components;
[DefaultEntityComponentProcessor(typeof(RecastMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu")]
[DataContract("BepuNavigationBoundingBoxComponent")]
public class BepuNavigationBoundingBoxComponent : NavigationBoundingBoxComponent
{
}
