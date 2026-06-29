// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;

namespace Stride.Core.Assets.Quantum.Templates;

/// <summary>
/// Convenience entry point for editor-side template registration that wraps a
/// <see cref="SessionTemplateGenerator"/> with <see cref="QuantumGraphTemplateGenerator"/> before
/// handing it to <see cref="TemplateManager.Register{T}"/>. CLI / headless flows call
/// <see cref="TemplateManager.Register{T}"/> directly to keep generators unwrapped.
/// </summary>
public static class QuantumTemplateRegistration
{
    /// <summary>
    /// Wraps <paramref name="generator"/> with a Quantum-aware decorator and registers it with
    /// <see cref="TemplateManager"/>.
    /// </summary>
    public static void Register(SessionTemplateGenerator generator)
    {
        TemplateManager.Register(new QuantumGraphTemplateGenerator(generator));
    }
}
