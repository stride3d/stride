// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Metrics;

namespace Stride.Core.Diagnostics;

/// <summary>
/// A key to identify a specific profile.
/// </summary>
public class ProfilingKey
{
    internal static readonly HashSet<ProfilingKey> AllKeys = [];
    internal bool Enabled;
    internal ProfilingKeyFlags Flags;

    // .NET Core profiling meter - allows consuming the data with dotnet-counters
    internal static Meter profilingMeter = new("Stride.Profiler");
    internal readonly Histogram<double> PerformanceMeasurement;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingKey" /> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public ProfilingKey(string name, ProfilingKeyFlags flags = ProfilingKeyFlags.None)
    {
        Name = ValidateNameNotEmpty(name);
        Children = [];
        Flags = flags;
        PerformanceMeasurement = profilingMeter.CreateHistogram<double>(Name, "ms", "Duration");

        lock (AllKeys)
        {
            AllKeys.Add(this);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingKey" /> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="System.ArgumentNullException">parent</exception>
    public ProfilingKey(ProfilingKey parent, string name, ProfilingKeyFlags flags = ProfilingKeyFlags.None)
        : this($"{parent}.{name}", flags)
    {
        ValidateNameNotEmpty(name);
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        lock (parent.Children)
        {
            // Register ourself in parent's children.
            parent.Children.Add(this);
        }
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the group.
    /// </summary>
    /// <value>The group.</value>
    public ProfilingKey? Parent { get; }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <value>
    /// The children.
    /// </value>
    public List<ProfilingKey> Children { get; }

    public override string ToString()
    {
        return Name;
    }

    private static string ValidateNameNotEmpty(string name) =>
        !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty", nameof(name));
}
