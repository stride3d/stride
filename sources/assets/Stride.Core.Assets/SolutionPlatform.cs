// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Settings;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Defines a solution platform.
    /// </summary>
    [DataContract("SolutionPlatform")]
    public class SolutionPlatform : SolutionPlatformPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionPlatform"/> class.
        /// </summary>
        public SolutionPlatform()
        {
            PlatformsPart = new SolutionPlatformPartCollection();
            DefineConstants = new List<string>();
        }

        /// <summary>
        /// Gets the alternative names that will appear in the .sln file equivalent to this platform.
        /// </summary>
        /// <value>The alternative names.</value>
        [DataMember(20)]
        public SolutionPlatformPartCollection PlatformsPart { get; private set; }

        /// <summary>
        /// Gets or sets the type of the platform.
        /// </summary>
        /// <value>The type.</value>
        [DataMember(30)]
        public PlatformType Type { get; set; }

        /// <summary>
        /// Gets or sets the type of the platform.
        /// </summary>
        /// <value>The type.</value>
        [DataMember(35)]
        public string TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets the runtime identifier.
        /// </summary>
        /// <value>The runtime identifier.</value>
        [DataMember(35)]
        public string RuntimeIdentifier { get; set; }

        /// <summary>
        /// Gets the define constants that will be used by the csproj of the platform.
        /// </summary>
        /// <value>The define constants.</value>
        [DataMember(40)]
        public List<string> DefineConstants { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SolutionPlatform"/> is available on this machine.
        /// </summary>
        /// <value><c>true</c> if available; otherwise, <c>false</c>.</value>
        [DataMember(50)]
        public bool IsAvailable { get; set; }

        /// <summary>
        /// The list of templates. If empty, no choice will be given to user and default one will be created by concatening ProjectExecutable and <see cref="SolutionPlatformPart.Name"/>.
        /// </summary>
        [DataMember(60)]
        public List<SolutionPlatformTemplate> Templates { get; } = new List<SolutionPlatformTemplate>();

        /// <summary>
        /// Gets the all <see cref="SolutionPlatformPart"/>.
        /// </summary>
        /// <returns>IEnumerable&lt;SolutionPlatformPart&gt;.</returns>
        public IEnumerable<SolutionPlatformPart> GetParts()
        {
            // Returns solution platform in alphabetical order
            var parts = new List<SolutionPlatformPart>(1 + PlatformsPart.Count) { this };
            parts.AddRange(PlatformsPart);

            return parts.OrderBy(part => part.SolutionName ?? part.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        public IEnumerable<string> GetConfigurationProperties(SolutionPlatformPart part, string configuration)
        {
            if (part == null) throw new ArgumentNullException("part");
            if (part.Configurations.Contains(configuration))
            {
                foreach (var property in part.Configurations[configuration].Properties)
                {
                    yield return property;
                }
            }

            if (part.InheritConfigurations && !ReferenceEquals(part, this))
            {
                foreach (var property in Configurations[configuration].Properties)
                {
                    yield return property;
                }
            }
        }

        public override string ToString()
        {
            return $"SolutionPlatform {Type}";
        }
    }

    /// <summary>
    /// Class SolutionAlternativePlatform.
    /// </summary>
    [DebuggerDisplay("Solution {Name} Configs [{Configurations.Count}]")]
    public class SolutionPlatformPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionPlatformPart"/> class.
        /// </summary>
        public SolutionPlatformPart()
        {
            UseWithExecutables = true;
            UseWithLibraries = true;
            IncludeInSolution = true;
            Configurations = new SolutionConfigurationCollection { new SolutionConfiguration("Debug") { IsDebug = true }, new SolutionConfiguration("Release") };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionPlatformPart"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public SolutionPlatformPart(string name) : this()
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the alternative platform.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the solution name. If null, use the <see cref="Name"/>
        /// </summary>
        /// <value>The name.</value>
        public string SolutionName { get; set; }

        /// <summary>
        /// Gets or sets the display name. If null, use the <see cref="Name"/>.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets the name of the solution from <see cref="SolutionName"/>, using <see cref="Name"/> as a fallback.
        /// </summary>
        /// <value>The name of the safe solution.</value>
        public string SafeSolutionName
        {
            get
            {
                return SolutionName ?? Name;
            }
        }

        /// <summary>
        /// Gets or sets the CPU name, if this platform is CPU specific.
        /// </summary>
        /// <value>
        /// The CPU name.
        /// </value>
        public string Cpu { get; set; }

        /// <summary>
        /// Gets or sets the name of the alias. If != null, then this platform is only available in the solution and remapped
        /// to the platform with this <see cref="Alias"/>
        /// </summary>
        /// <value>The name of the alias.</value>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether inherit configurations from parent <see cref="SolutionPlatform"/>
        /// </summary>
        /// <value><c>true</c> if [inherit configurations]; otherwise, <c>false</c>.</value>
        public bool InheritConfigurations { get; set; }

        /// <summary>
        /// Gets the configurations supported by this platform (by default contains 'Debug' and 'Release')
        /// </summary>
        public SolutionConfigurationCollection Configurations { get; private set; }

        public bool UseWithExecutables { get; set; }
        public bool UseWithLibraries { get; set; }
        public bool IncludeInSolution { get; set; }

        public string LibraryProjectName { get; set; }
        public string ExecutableProjectName { get; set; }

        /// <summary>
        /// Determines whether the project is handled by this platform.
        /// </summary>
        /// <param name="projectType">Type of the project.</param>
        /// <returns><c>true</c> if the project is handled by this platform; otherwise, <c>false</c>.</returns>
        public bool IsProjectHandled(ProjectType projectType)
        {
            return (projectType != ProjectType.Executable || UseWithExecutables) && (projectType != ProjectType.Library || UseWithLibraries);
        }

        /// <summary>
        /// Gets the name of the project configuration from <see cref="Alias"/>, with <see cref="SafeSolutionName"/> as a fallback.
        /// </summary>
        /// <value>The name of the safe solution.</value>
        public string GetProjectName(ProjectType projectType)
        {
            switch (projectType)
            {
                case ProjectType.Executable:
                    return ExecutableProjectName ?? Alias ?? SafeSolutionName;
                case ProjectType.Library:
                    return LibraryProjectName ?? Alias ?? SafeSolutionName;
                default:
                    throw new ArgumentOutOfRangeException("projectType");
            }
        }

        public override string ToString()
        {
            return $"SolutionPlatformPart {Name} ({Configurations.Count} configs)";
        }
    }

    /// <summary>
    /// A collection of <see cref="SolutionPlatformPart"/>
    /// </summary>
    [DataContract("SolutionPlatformPartCollection")]
    public class SolutionPlatformPartCollection : KeyedCollection<string, SolutionPlatformPart>
    {
        protected override string GetKeyForItem(SolutionPlatformPart item)
        {
            return item.Name;
        }
    }

    /// <summary>
    /// A collection of <see cref="SolutionConfiguration"/>
    /// </summary>
    [DataContract("SolutionConfigurationCollection")]
    public class SolutionConfigurationCollection : KeyedCollection<string, SolutionConfiguration>
    {
        protected override string GetKeyForItem(SolutionConfiguration item)
        {
            return item.Name;
        }
    }

    /// <summary>
    /// A solution configuration used by <see cref="SolutionPlatform"/>
    /// </summary>
    [DataContract("SolutionConfiguration")]
    [DebuggerDisplay("Config [{Name}]")]
    public class SolutionConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionConfiguration"/> class.
        /// </summary>
        public SolutionConfiguration(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            Name = name;
            Properties = new List<string>();
        }

        /// <summary>
        /// Gets or sets the configuration name (e.g. Debug, Release)
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a debug configuration.
        /// </summary>
        /// <value><c>true</c> if this instance is debug; otherwise, <c>false</c>.</value>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Gets the additional msbuild properties for a specific configuration (Debug or Release)
        /// </summary>
        /// <value>The msbuild configuration properties.</value>
        public List<string> Properties { get; private set; }
    }
}
