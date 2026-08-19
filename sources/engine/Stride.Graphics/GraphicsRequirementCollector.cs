// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Graphics;

/// <summary>
///   Collects the capabilities that renderers declare they need. The compositor reports them together,
///   instead of each renderer failing where it runs.
/// </summary>
/// <remarks>
///   A renderer states what it needs. This class answers whether it gets it, so no renderer answers on
///   the device's behalf.
/// </remarks>
public sealed class GraphicsRequirementCollector
{
    private readonly List<GraphicsRequirement> requirements = [];

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsRequirementCollector"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The device to answer declarations against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
    public GraphicsRequirementCollector(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        GraphicsDevice = graphicsDevice;
    }

    /// <summary>
    ///   The device declarations are answered against.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    ///   Every requirement, in the order the renderers declared it.
    /// </summary>
    public IReadOnlyList<GraphicsRequirement> Requirements => requirements;

    /// <summary>
    ///   Whether any <see cref="GraphicsRequirementSeverity.Required"/> capability is missing.
    /// </summary>
    public bool HasUnmetRequirements
    {
        get
        {
            foreach (var requirement in requirements)
                if (!requirement.IsMet && requirement.Severity == GraphicsRequirementSeverity.Required)
                    return true;

            return false;
        }
    }

    /// <summary>
    ///   Declares a capability the renderer cannot work without.
    /// </summary>
    /// <param name="source">The renderer that declares it. Its type name identifies it in the report.</param>
    /// <param name="capability">What the renderer needs.</param>
    /// <param name="reason">Why the renderer needs it.</param>
    /// <returns>What the renderer got. Branch on this so the report and the renderer cannot disagree.</returns>
    public GraphicsRequirement Require(object source, GraphicsCapability capability, string reason)
    {
        return Add(source, capability, GraphicsRequirementSeverity.Required, reason, fallback: null);
    }

    /// <summary>
    ///   Declares a capability the renderer uses when it can, and works without when it cannot.
    /// </summary>
    /// <param name="source">The renderer that declares it. Its type name identifies it in the report.</param>
    /// <param name="capability">What the renderer wants.</param>
    /// <param name="reason">Why the renderer wants it.</param>
    /// <param name="fallback">What the renderer does instead.</param>
    /// <returns>What the renderer got. Branch on this so the report and the renderer cannot disagree.</returns>
    public GraphicsRequirement Prefer(object source, GraphicsCapability capability, string reason, string fallback)
    {
        return Add(source, capability, GraphicsRequirementSeverity.Preferred, reason, fallback);
    }

    /// <summary>
    ///   Builds the report of the device and every declared requirement.
    /// </summary>
    public string BuildReport()
    {
        var report = new StringBuilder();

        report.Append("Graphics device: ").Append(GraphicsDevice.Adapter?.Description ?? "unknown adapter")
              .Append(" (").Append(GraphicsDevice.Platform).AppendLine(")");
        report.Append("  ").AppendLine(GraphicsDevice.Features.ToString());

        if (requirements.Count == 0)
        {
            report.Append("  No renderer declared any capability requirement.");
            return report.ToString();
        }

        foreach (var requirement in requirements)
        {
            report.Append("  ").Append(requirement).Append(" - ").Append(requirement.Reason);

            if (requirement.Outcome == GraphicsRequirementOutcome.NotImplementedByBackend)
                report.Append(". The ").Append(GraphicsDevice.Platform).Append(" backend does not implement this");

            if (!requirement.IsMet && requirement.Fallback is not null)
                report.Append(". Falling back to ").Append(requirement.Fallback);

            report.AppendLine();
        }

        return report.ToString().TrimEnd();
    }

    /// <summary>
    ///   Throws when a <see cref="GraphicsRequirementSeverity.Required"/> capability is missing. The
    ///   message names every one of them.
    /// </summary>
    /// <exception cref="GraphicsException">At least one required capability is missing.</exception>
    public void ThrowIfUnmet()
    {
        if (!HasUnmetRequirements)
            return;

        var message = new StringBuilder("This graphics device cannot run the current graphics compositor.");

        foreach (var requirement in requirements)
        {
            if (requirement.IsMet || requirement.Severity != GraphicsRequirementSeverity.Required)
                continue;

            message.AppendLine().Append("  ").Append(requirement.Source).Append(" requires ")
                   .Append(requirement.Capability.Name).Append(": ").Append(requirement.Reason);

            if (requirement.Outcome == GraphicsRequirementOutcome.NotImplementedByBackend)
                message.Append(" (the ").Append(GraphicsDevice.Platform).Append(" backend does not implement this)");
        }

        message.AppendLine().Append("Device: ").Append(GraphicsDevice.Adapter?.Description ?? "unknown adapter")
               .Append(" (").Append(GraphicsDevice.Platform).Append(", ")
               .Append(GraphicsDevice.Features.CurrentProfile).Append(')');

        throw new GraphicsException(message.ToString());
    }

    private GraphicsRequirement Add(object source, GraphicsCapability capability, GraphicsRequirementSeverity severity,
                                    string reason, string fallback)
    {
        ArgumentNullException.ThrowIfNull(capability);

        var requirement = new GraphicsRequirement(NameOf(source), capability, Evaluate(capability), severity,
                                                  reason, fallback);
        requirements.Add(requirement);

        return requirement;
    }

    private GraphicsRequirementOutcome Evaluate(GraphicsCapability capability)
    {
        ref readonly var features = ref GraphicsDevice.Features;

        // The backend has to implement the kind before the device's answer means anything.
        if (!features.IsImplementedByBackend(capability.Kind))
            return GraphicsRequirementOutcome.NotImplementedByBackend;

        return capability.IsProvidedByDevice(features)
            ? GraphicsRequirementOutcome.Available
            : GraphicsRequirementOutcome.NotProvidedByDevice;
    }

    private static string NameOf(object source) => source?.GetType().Name ?? "Unknown renderer";
}
