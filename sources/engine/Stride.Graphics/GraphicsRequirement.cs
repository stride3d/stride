// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A capability a renderer declared it needs, and whether the device provides it.
/// </summary>
public readonly struct GraphicsRequirement
{
    internal GraphicsRequirement(string source, string capability, bool isMet, GraphicsRequirementSeverity severity, string reason, string fallback)
    {
        Source = source;
        Capability = capability;
        IsMet = isMet;
        Severity = severity;
        Reason = reason;
        Fallback = fallback;
    }

    /// <summary>
    ///   The name of the renderer that declared this requirement.
    /// </summary>
    public string Source { get; }

    /// <summary>
    ///   What the renderer needs.
    /// </summary>
    public string Capability { get; }

    /// <summary>
    ///   Whether the device provides <see cref="Capability"/>.
    /// </summary>
    public bool IsMet { get; }

    /// <summary>
    ///   What happens when the device does not provide it.
    /// </summary>
    public GraphicsRequirementSeverity Severity { get; }

    /// <summary>
    ///   Why the renderer needs it.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    ///   What the renderer does instead when the device does not provide it. A
    ///   <see cref="GraphicsRequirementSeverity.Required"/> capability has no alternative, so this is
    ///   <see langword="null"/>.
    /// </summary>
    public string Fallback { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var outcome = IsMet
            ? "ok"
            : Severity == GraphicsRequirementSeverity.Required ? "UNMET" : "degraded";

        return $"[{outcome}] {Source}: {Capability}";
    }
}
