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
public sealed class GraphicsRequirementCollector
{
    private readonly List<GraphicsRequirement> requirements = [];

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsRequirementCollector"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The device that renderers evaluate their requirements against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
    public GraphicsRequirementCollector(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        GraphicsDevice = graphicsDevice;
    }

    /// <summary>
    ///   The device that renderers evaluate their requirements against.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    ///   Every requirement, in the order the renderers declared it.
    /// </summary>
    public IReadOnlyList<GraphicsRequirement> Requirements => requirements;

    /// <summary>
    ///   Whether the device fails to provide a <see cref="GraphicsRequirementSeverity.Required"/> capability.
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
    /// <param name="capability">What is needed.</param>
    /// <param name="isMet">Whether the device provides it. The renderer decides this.</param>
    /// <param name="reason">Why the renderer needs it.</param>
    public void Require(object source, string capability, bool isMet, string reason)
    {
        requirements.Add(new GraphicsRequirement(NameOf(source), capability, isMet, GraphicsRequirementSeverity.Required, reason, fallback: null));
    }

    /// <summary>
    ///   Declares a capability the renderer uses when the device provides it, and works without when the
    ///   device does not.
    /// </summary>
    /// <param name="source">The renderer that declares it. Its type name identifies it in the report.</param>
    /// <param name="capability">What is wanted.</param>
    /// <param name="isMet">Whether the device provides it. The renderer decides this.</param>
    /// <param name="reason">Why the renderer wants it.</param>
    /// <param name="fallback">What the renderer does instead.</param>
    public void Prefer(object source, string capability, bool isMet, string reason, string fallback)
    {
        requirements.Add(new GraphicsRequirement(NameOf(source), capability, isMet, GraphicsRequirementSeverity.Preferred, reason, fallback));
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

            if (!requirement.IsMet && requirement.Fallback is not null)
                report.Append(". Falling back to ").Append(requirement.Fallback);

            report.AppendLine();
        }

        return report.ToString().TrimEnd();
    }

    /// <summary>
    ///   Throws when the device fails to provide a <see cref="GraphicsRequirementSeverity.Required"/>
    ///   capability. The message names every one of them.
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
                   .Append(requirement.Capability).Append(": ").Append(requirement.Reason);
        }

        message.AppendLine().Append("Device: ").Append(GraphicsDevice.Adapter?.Description ?? "unknown adapter")
               .Append(" (").Append(GraphicsDevice.Platform).Append(", ")
               .Append(GraphicsDevice.Features.CurrentProfile).Append(')');

        throw new GraphicsException(message.ToString());
    }

    private static string NameOf(object source) => source?.GetType().Name ?? "Unknown renderer";
}
