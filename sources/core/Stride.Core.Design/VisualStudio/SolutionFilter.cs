#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stride.Core.VisualStudio;

/// <summary>
/// Represents a Visual Studio solution filter file (.slnf).
/// </summary>
public class SolutionFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFilter"/> class.
    /// </summary>
    public SolutionFilter()
    {
        SolutionPath = string.Empty;
        ProjectPaths = [];
    }

    /// <summary>
    /// Gets or sets the path to the solution file referenced by this solution filter.
    /// </summary>
    public string SolutionPath { get; set; }

    /// <summary>
    /// Gets the list of project paths included in the solution filter.
    /// </summary>
    public List<string> ProjectPaths { get; } = [];

    /// <summary>
    /// Loads a solution filter from a file path.
    /// </summary>
    /// <param name="solutionFilterPath">The full path to the solution filter file.</param>
    /// <returns>A populated SolutionFilter instance.</returns>
    public static SolutionFilter FromFile(string solutionFilterPath)
    {
        using var stream = new FileStream(solutionFilterPath, FileMode.Open, FileAccess.Read);
        return FromStream(solutionFilterPath, stream);
    }

    /// <summary>
    /// Loads a solution filter from a stream.
    /// </summary>
    /// <param name="solutionFilterPath">The full path to the solution filter file.</param>
    /// <param name="stream">The stream containing the solution filter data.</param>
    /// <returns>A populated SolutionFilter instance.</returns>
    public static SolutionFilter FromStream(string solutionFilterPath, Stream stream)
    {
        using var filterReader = new SolutionFilterReader(solutionFilterPath, stream);
        return filterReader.ReadSolutionFilterFile();
    }
}

/// <summary>
/// JSON model for deserializing solution filter files.
/// </summary>
internal class SolutionFilterData
{
    /// <summary>
    /// Gets or sets the solution information.
    /// </summary>
    [JsonPropertyName("solution")]
    public SolutionInfo? Solution { get; set; }

    /// <summary>
    /// Represents solution information in a solution filter file.
    /// </summary>
    public class SolutionInfo
    {
        /// <summary>
        /// Gets or sets the relative path to the solution file.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the list of project paths in the solution filter.
        /// </summary>
        [JsonPropertyName("projects")]
        public List<string>? Projects { get; set; }
    }
}
