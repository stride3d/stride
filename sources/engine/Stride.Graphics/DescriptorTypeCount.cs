// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

    /// <summary>
    /// Describes how many descriptor of a specific type will need to be allocated in a <see cref="DescriptorPool"/>.
    /// </summary>
namespace Stride.Graphics;

public readonly record struct DescriptorTypeCount(EffectParameterClass Type, int Count);
