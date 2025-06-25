// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Describes how many Descriptor of a specific type will need to be allocated in a <see cref="DescriptorPool"/>.
/// </summary>
/// <param name="Type">The type of the Descriptors to allocate.</param>
/// <param name="Count">The number of Descriptors that need to be allocated.</param>
public readonly record struct DescriptorTypeCount(EffectParameterClass Type, int Count);
