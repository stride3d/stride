#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
//
// SLNTools
// Copyright (c) 2009
// by Christian Warren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion

using System.Diagnostics;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Stride.Core.Solutions;

/// <summary>
/// A VisualStudio solution.
/// </summary>
[DebuggerDisplay("Projects = [{Projects.Count}]")]
public class Solution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Solution"/> class.
    /// </summary>
    public Solution()
    {
        FullPath = string.Empty;
        Headers = [];
        Projects = new(this);
        GlobalSections = [];
        Properties = [];
    }

    private Solution(Solution original)
        : this(original.FullPath, original.Headers, original.Projects, original.GlobalSections, original.Properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Solution" /> class.
    /// </summary>
    /// <param name="fullpath">The fullpath.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="projects">The projects.</param>
    /// <param name="globalSections">The global sections.</param>
    /// <param name="properties">The properties.</param>
    public Solution(string fullpath, IEnumerable<string> headers, IEnumerable<Project> projects, IEnumerable<Section> globalSections, IEnumerable<PropertyItem> properties)
    {
        FullPath = fullpath;
        this.Headers = new List<string>(headers);
        this.Projects = new ProjectCollection(this, projects);
        this.GlobalSections = new SectionCollection(globalSections);
        Properties = new PropertyItemCollection(properties);
    }

    /// <summary>
    /// The file extensions recognized as a Visual Studio solution: the classic <c>.sln</c>, the XML
    /// <c>.slnx</c>, and the <c>.slnf</c> solution filter.
    /// </summary>
    public static readonly string[] SolutionExtensions = [".sln", ".slnx", ".slnf"];

    /// <summary>
    /// Determines whether the given path has a recognized solution extension (see <see cref="SolutionExtensions"/>).
    /// </summary>
    public static bool IsSolutionFile(string path)
        => SolutionExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the full path. If it's a solution folder, it should just be the name of the folder.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath { get; set; }

    /// <summary>
    /// The id of the project to mark as the solution's default startup (the <c>.slnx</c>
    /// <c>DefaultStartup</c> attribute); null to leave it unset.
    /// </summary>
    public Guid? StartupProjectGuid { get; set; }

    /// <summary>
    /// The model this solution was loaded from, used on save to preserve everything it already
    /// contained; null for a solution that was not loaded from disk.
    /// </summary>
    internal SolutionModel? SourceModel { get; set; }

    /// <summary>
    /// Gets all projects that are not folders.
    /// </summary>
    /// <value>The children.</value>
    public IEnumerable<Project> Children
    {
        get
        {
            return Projects.Where(project => project.GetParentProject(this) == null);
        }
    }

    /// <summary>
    /// Gets the global sections.
    /// </summary>
    /// <value>The global sections.</value>
    public SectionCollection GlobalSections { get; }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <value>The headers.</value>
    public List<string> Headers { get; }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <value>The projects.</value>
    public ProjectCollection Projects { get; }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <value>The properties.</value>
    public PropertyItemCollection Properties { get; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>Solution.</returns>
    public Solution Clone()
    {
        return new Solution(this);
    }

    /// <summary>
    /// Saves this instance to the <see cref="FullPath"/> path.
    /// </summary>
    /// <param name="onBeforeOverwrite">Invoked with the path just before an existing file is overwritten (only when its content changed).</param>
    public void Save(Action<string>? onBeforeOverwrite = null)
    {
        SaveAs(FullPath, onBeforeOverwrite);
    }

    /// <summary>
    /// Saves this instance to the specified path.
    /// </summary>
    /// <param name="solutionPath">The solution path.</param>
    /// <param name="onBeforeOverwrite">Invoked with the path just before an existing file is overwritten (only when its content changed).</param>
    public void SaveAs(string solutionPath, Action<string>? onBeforeOverwrite = null)
    {
        SolutionSerialization.Write(this, solutionPath, onBeforeOverwrite);
    }

    /// <summary>
    /// Loads the solution from the specified file.
    /// </summary>
    /// <param name="solutionFullPath">The solution full path.</param>
    /// <returns>Solution.</returns>
    public static Solution FromFile(string solutionFullPath)
    {
        return SolutionSerialization.Read(solutionFullPath);
    }

    /// <summary>
    /// Loads the solution from the specified stream.
    /// </summary>
    /// <param name="solutionFullPath">The solution full path.</param>
    /// <param name="stream">The stream.</param>
    /// <returns>Solution.</returns>
    public static Solution FromStream(string solutionFullPath, Stream stream)
    {
        return SolutionSerialization.Read(solutionFullPath, stream);
    }
}
