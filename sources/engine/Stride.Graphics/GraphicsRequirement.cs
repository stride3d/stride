// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A capability a renderer declared it needs, and what it got.
/// </summary>
public readonly struct GraphicsRequirement
{
    internal GraphicsRequirement(string source, GraphicsCapability capability, GraphicsRequirementOutcome outcome,
                                 GraphicsRequirementSeverity severity, string reason, string fallback)
    {
        Source = source;
        Capability = capability;
        Outcome = outcome;
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
    public GraphicsCapability Capability { get; }

    /// <summary>
    ///   Whether the renderer got it, and if not, why not.
    /// </summary>
    public GraphicsRequirementOutcome Outcome { get; }

    /// <summary>
    ///   What happens when the renderer does not get it.
    /// </summary>
    public GraphicsRequirementSeverity Severity { get; }

    /// <summary>
    ///   Why the renderer needs it.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    ///   What the renderer does instead when it does not get it. A
    ///   <see cref="GraphicsRequirementSeverity.Required"/> capability has no alternative, so this is
    ///   <see langword="null"/>.
    /// </summary>
    public string Fallback { get; }

    /// <summary>
    ///   Whether the renderer got what it declared.
    /// </summary>
    public bool IsMet => Outcome == GraphicsRequirementOutcome.Available;

    /// <inheritdoc/>
    public override string ToString()
    {
        var outcome = Outcome switch
        {
            GraphicsRequirementOutcome.Available => "ok",
            GraphicsRequirementOutcome.NotImplementedByBackend => "NOT IMPLEMENTED",
            _ => Severity == GraphicsRequirementSeverity.Required ? "UNMET" : "degraded"
        };

        return $"[{outcome}] {Source}: {Capability.Name}";
    }
}
