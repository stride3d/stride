// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Reflection;

namespace Stride.Games;

/// <summary>
/// Declares a game system that every game creates with its <see cref="Stride.Core.IServiceRegistry"/> constructor and
/// registers as a service. Only assemblies registered when the game is created count.
/// </summary>
[AssemblyScan]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GameSystemAttribute : Attribute
{
}
