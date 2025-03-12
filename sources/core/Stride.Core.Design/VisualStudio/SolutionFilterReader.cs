#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.

#endregion

using System.Text.Json;

namespace Stride.Core.VisualStudio;

internal sealed class SolutionFilterReader : IDisposable
{
    private readonly string solutionFilterPath;
    private readonly string solutionFilterDirectory;
    private StreamReader? reader;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFilterReader"/> class.
    /// </summary>
    /// <param name="solutionFilterPath">The solution filter path.</param>
    public SolutionFilterReader(string solutionFilterPath) 
        : this(solutionFilterPath, new FileStream(solutionFilterPath, FileMode.Open, FileAccess.Read))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFilterReader"/> class.
    /// </summary>
    /// <param name="solutionFilterPath">The solution filter path.</param>
    /// <param name="stream">The stream containing the solution filter data.</param>
    public SolutionFilterReader(string solutionFilterPath, Stream stream)
    {
        this.solutionFilterPath = solutionFilterPath;
        solutionFilterDirectory = Path.GetDirectoryName(solutionFilterPath) ?? string.Empty;
        reader = new StreamReader(stream);
    }

    /// <summary>
    /// Reads the solution filter file and returns a SolutionFilter instance.
    /// </summary>
    /// <returns>A populated SolutionFilter instance.</returns>
    public SolutionFilter ReadSolutionFilterFile()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (disposed) throw new ObjectDisposedException(nameof(SolutionFilterReader));
#endif
        
        var solutionFilter = new SolutionFilter();
        
        try
        {
            // Read and deserialize the JSON content
            var jsonContent = reader!.ReadToEnd();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var filterData = JsonSerializer.Deserialize<SolutionFilterData>(jsonContent, options);
            
            if (filterData?.Solution?.Path is null)
            {
                throw new SolutionFileException($"Invalid solution filter file: {solutionFilterPath}. Missing or invalid 'solution.path' property.");
            }
            
            // Resolve the solution path relative to the solution filter
            var relativeSolutionPath = filterData.Solution.Path.Replace('\\', Path.DirectorySeparatorChar);
            solutionFilter.SolutionPath = Path.GetFullPath(Path.Combine(solutionFilterDirectory, relativeSolutionPath));
            
            // Process project paths
            if (filterData.Solution.Projects is not null)
            {
                foreach (var projectPath in filterData.Solution.Projects)
                {
                    if (!string.IsNullOrEmpty(projectPath))
                    {
                        solutionFilter.ProjectPaths.Add(projectPath.Replace('\\', Path.DirectorySeparatorChar));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            throw new SolutionFileException($"Error parsing solution filter file: {solutionFilterPath}", ex);
        }
        
        return solutionFilter;
    }

    /// <summary>
    /// Disposes resources used by the reader.
    /// </summary>
    public void Dispose()
    {
        disposed = true;
        if (reader is not null)
        {
            reader.Dispose();
            reader = null;
        }
    }
}
