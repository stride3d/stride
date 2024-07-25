// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.BepuPhysics;
[DataContract]
[Display("Bepu Configuration")]
public class BepuConfiguration : Configuration
{
    public List<BepuSimulation> BepuSimulations = new();
}
