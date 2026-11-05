// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors;

[DataContract("ComputeClampedLerpScalar")]
[Display("Lerp Operator")]
public class ComputeClampedLerpScalar : ComputeClampedLerpBase<IComputeScalar>, IComputeScalar;
