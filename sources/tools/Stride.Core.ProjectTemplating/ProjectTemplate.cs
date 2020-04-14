// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.TextTemplating;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;

namespace Stride.Core.ProjectTemplating
{
    /// <summary>
    /// Defines a project template that allows automated creation of a project structure with files.
    /// </summary>
    [DataContract("ProjectTemplate")]
    [NonIdentifiableCollectionItems]
    public class ProjectTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectTemplate"/> class.
        /// </summary>
        public ProjectTemplate()
        {
            Files = new List<ProjectTemplateItem>();
            Assemblies = new List<UFile>();
        }

        /// <summary>
        /// Gets or sets the template file path.
        /// </summary>
        /// <value>The template path.</value>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this template description is dynamic (itself requiring T4 parsing before 
        /// generating content files)
        /// </summary>
        /// <value><c>true</c> if this instance is a dynamic template; otherwise, <c>false</c>.</value>
        public bool IsDynamicTemplate { get; private set; }

        /// <summary>
        /// Gets or sets the files part of the template.
        /// </summary>
        /// <value>The files.</value>
        public List<ProjectTemplateItem> Files { get; }

        /// <summary>
        /// Gets or sets the assemblies.
        /// </summary>
        /// <value>The assemblies.</value>
        public List<UFile> Assemblies { get; }

        /// <summary>
        /// Generates this project template to the specified output directory.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="projectGuid">The project unique identifier.</param>
        /// <param name="options">The options arguments that will be made available through the Session property in each template.</param>
        /// <returns>LoggerResult.</returns>
        /// <exception cref="System.ArgumentNullException">outputDirectory
        /// or
        /// projectName</exception>
        /// <exception cref="System.InvalidOperationException">FilePath cannot be null on this instance</exception>
        public LoggerResult Generate(string outputDirectory, string projectName, Guid projectGuid, Dictionary<string, object> options = null)
        {
            if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));
            if (projectName == null) throw new ArgumentNullException(nameof(projectName));
            if (FilePath == null) throw new InvalidOperationException("FilePath cannot be null on this instance");

            var result = new LoggerResult();
            Generate(outputDirectory, projectName, projectGuid, result, options);
            return result;
        }

        /// <summary>
        /// Generates this project template to the specified output directory.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="projectGuid">The project unique identifier.</param>
        /// <param name="log">The log to output errors to.</param>
        /// <param name="options">The options arguments that will be made available through the Session property in each template.</param>
        /// <param name="generatedOutputFiles">The generated files.</param>
        /// <exception cref="System.ArgumentNullException">outputDirectory
        /// or
        /// projectName</exception>
        /// <exception cref="System.InvalidOperationException">FilePath cannot be null on this instance</exception>
        public void Generate(string outputDirectory, string projectName, Guid projectGuid, ILogger log, IReadOnlyDictionary<string, object> options = null, List<string> generatedOutputFiles = null )
        {
            if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));
            if (projectName == null) throw new ArgumentNullException(nameof(projectName));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (FilePath == null) throw new InvalidOperationException("FilePath cannot be null on this instance");

            try
            {
                // Check Project template filepath
                var templateDirectory = new FileInfo(FilePath).Directory;
                if (templateDirectory == null || !templateDirectory.Exists)
                {
                    log.Error($"Invalid ProjectTemplate directory [{FilePath}]");
                    return;
                }

                // Creates the output directory
                var directory = new DirectoryInfo(outputDirectory);
                if (!directory.Exists)
                {
                    directory.Create();
                }

                // Create expando object from options valid for the whole life of generating a project template
                var expandoOptions = new ExpandoObject();
                var expandoOptionsAsDictionary = (IDictionary<string, object>)expandoOptions;
                expandoOptionsAsDictionary["ProjectName"] = projectName;
                expandoOptionsAsDictionary["ProjectGuid"] = projectGuid;
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        expandoOptionsAsDictionary[option.Key] = option.Value;
                    }
                }

                var engine = new TemplatingEngine();

                // In case this project template is dynamic, we need to generate its content first through T4
                if (IsDynamicTemplate)
                {
                    var content = File.ReadAllText(FilePath);
                    var host = new ProjectTemplatingHost(log, FilePath, templateDirectory.FullName, expandoOptions, Assemblies.Select(assembly => assembly.FullPath));
                    var newTemplateAsString = engine.ProcessTemplate(content, host);
                    Files.Clear();
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(newTemplateAsString)))
                    {
                        var newTemplate = (ProjectTemplate)YamlSerializer.Default.Deserialize(stream);
                        Files.AddRange(newTemplate.Files);
                    }
                }

                // Iterate on each files
                foreach (var fileItem in Files)
                {
                    if (fileItem.Source == null)
                    {
                        log.Warning($"Invalid empty file item [{fileItem}] with no source location");
                        continue;
                    }
                    var sourceFilePath = Path.Combine(templateDirectory.FullName, fileItem.Source);
                    var targetLocation = fileItem.Target ?? fileItem.Source;
                    if (Path.IsPathRooted(targetLocation))
                    {
                        log.Error($"Invalid file item [{fileItem}]. TargetLocation must be a relative path");
                        continue;
                    }

                    var targetLocationExpanded = Expand(targetLocation, expandoOptionsAsDictionary, log);

                    // If this is a template file, turn template on by default
                    if (fileItem.IsTemplate)
                    {
                        var targetPath = Path.GetDirectoryName(targetLocationExpanded);
                        var targetFileName = Path.GetFileName(targetLocationExpanded);
                        targetLocationExpanded = targetPath != null ? Path.Combine(targetPath, targetFileName) : targetFileName;
                    }

                    var targetFilePath = Path.Combine(outputDirectory, targetLocationExpanded);
                    try
                    {
                        // Make sure that the target directory does exist
                        var targetDirectory = new FileInfo(targetFilePath).Directory;
                        if (!targetDirectory.Exists)
                        {
                            targetDirectory.Create();
                        }

                        bool fileGenerated = false;
                        if (fileItem.IsTemplate)
                        {
                            var content = File.ReadAllText(sourceFilePath);
                            // Replace the default platform with the selected one from the ProjectItemTemplate.
                            object oldPlatform = null;
                            if (fileItem.CurrentPlatform != null)
                            {
                                if (expandoOptionsAsDictionary.ContainsKey(nameof(fileItem.CurrentPlatform)))
                                {
                                    oldPlatform = expandoOptionsAsDictionary[nameof(fileItem.CurrentPlatform)];
                                }
                                expandoOptionsAsDictionary[nameof(fileItem.CurrentPlatform)] = fileItem.CurrentPlatform;
                            }
                            var host = new ProjectTemplatingHost(log, sourceFilePath, templateDirectory.FullName, expandoOptions, Assemblies.Select(assembly => assembly.FullPath));
                            var newContent = engine.ProcessTemplate(content, host);
                            if (fileItem.CurrentPlatform != null)
                            {
                                if (oldPlatform != null)
                                {
                                    expandoOptionsAsDictionary[nameof(fileItem.CurrentPlatform)] = oldPlatform;
                                }
                                else
                                {
                                    expandoOptionsAsDictionary.Remove(nameof(fileItem.CurrentPlatform));
                                }
                            }
                            if (newContent != null)
                            {
                                fileGenerated = true;
                                File.WriteAllText(targetFilePath, newContent);
                            }
                        }
                        else
                        {
                            fileGenerated = true;
                            File.Copy(sourceFilePath, targetFilePath, true);
                        }

                        if (generatedOutputFiles != null && fileGenerated)
                        {
                            generatedOutputFiles.Add(targetFilePath);
                        }
                    }
                    catch (Exception ex)
                    {

                        log.Error($"Unexpected exception while processing [{fileItem}]", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected exception while processing project template [{projectName}] to directory [{outputDirectory}]", ex);
            }
        }

        public string GeneratePart(string templatePathPart, ILogger log, IReadOnlyDictionary<string, object> options)
        {
            if (templatePathPart == null) throw new ArgumentNullException(nameof(templatePathPart));
            if (log == null) throw new ArgumentNullException(nameof(log));
            var expandoOptions = new ExpandoObject();
            var expandoOptionsAsDictionary = (IDictionary<string, object>)expandoOptions;
            foreach (var option in options)
            {
                expandoOptionsAsDictionary[option.Key] = option.Value;
            }

            var templateDirectory = new FileInfo(FilePath).Directory;
            var sourceFilePath = Path.Combine(templateDirectory.FullName, templatePathPart);
            var content = File.ReadAllText(sourceFilePath);

            var engine = new TemplatingEngine();
            var host = new ProjectTemplatingHost(log, sourceFilePath, templateDirectory.FullName, expandoOptions, Assemblies.Select(assembly => assembly.FullPath));
            return engine.ProcessTemplate(content, host);            
        }

        private static bool HasT4Extension(string filePath)
        {
            return filePath.EndsWith(".tt", StringComparison.InvariantCultureIgnoreCase)
                   || filePath.EndsWith(".t4", StringComparison.InvariantCultureIgnoreCase);
        }

        private static readonly Regex ExpandRegex = new Regex(@"\$(\w+)\$");

        private static string Expand(string str, IDictionary<string, object> properties, ILogger log)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            return ExpandRegex.Replace(str, match =>
            {
                var propertyName = match.Groups[1].Value;
                object propertyValue;
                if (properties.TryGetValue(propertyName, out propertyValue))
                {
                    return propertyValue?.ToString() ?? string.Empty;
                }
                log.Warning("Unable to replace property [{0}] not found in options");
                return match.Value;
            });
        }

        /// <summary>
        /// Loads the a <see cref="ProjectTemplate"/> from the specified file path.
        /// </summary>
        /// <param name="filePath">The project template file.</param>
        /// <returns>An instance of the project template.</returns>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        public static ProjectTemplate Load(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            var fullFilePath = Path.Combine(Environment.CurrentDirectory, filePath);
            var projectFile = File.ReadAllText(fullFilePath);
            ProjectTemplate template;
            // If this a project template?
            if (projectFile.StartsWith("<#@"))
            {
                template = new ProjectTemplate() { IsDynamicTemplate = true };
            }
            else
            {
                using (var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    template = (ProjectTemplate)YamlSerializer.Default.Deserialize(stream);
                }
            }

            template.FilePath = fullFilePath;
            return template;
        }
    }
}
