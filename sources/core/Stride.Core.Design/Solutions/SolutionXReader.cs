using System.Xml.Linq;

namespace Stride.Core.Solutions
{
    /// <summary>
    /// Reads a Visual Studio XML based solution file (<c>.slnx</c>) into a <see cref="Solution"/> model.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd">schema</see> for the <c>.slnx</c> file format.
    /// </remarks>
    internal class SolutionXReader
    {
        /// <summary>
        /// Reads a <c>.slnx</c> solution from the specified stream.
        /// </summary>
        /// <param name="solutionFullPath">The full path of the solution, used to resolve relative project paths.</param>
        /// <param name="stream">The stream containing the XML solution.</param>
        /// <returns>The parsed <see cref="Solution"/>.</returns>
        /// <exception cref="SolutionFileException">The file is not a valid <c>.slnx</c> document.</exception>
        public static Solution ReadSolutionFile(string solutionFullPath, Stream stream)
        {
            var solution = new Solution { FullPath = solutionFullPath };

            XDocument document;
            try
            {
                document = XDocument.Load(stream);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new SolutionFileException(
                    $"Invalid .slnx file '{solutionFullPath}': the file is not a well-formed XML document.",
                    ex
                );
            }

            var root =
                document.Root
                ?? throw new SolutionFileException(
                    $"Invalid .slnx file '{solutionFullPath}': the document is empty."
                );

            if (!string.Equals(root.Name.LocalName, "Solution", StringComparison.Ordinal))
            {
                throw new SolutionFileException(
                    $"Invalid .slnx file '{solutionFullPath}': expected root element 'Solution' but found '{root.Name.LocalName}'."
                );
            }

            var solutionDirectory = Path.GetDirectoryName(solutionFullPath) ?? string.Empty;

            ReadContainer(root, Guid.Empty, solution, solutionDirectory);

            return solution;
        }

        private static void ReadContainer(
            XElement container,
            Guid parentGuid,
            Solution solution,
            string solutionDirectory
        )
        {
            foreach (var element in container.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Project":
                        solution.Projects.Add(ReadProject(element, parentGuid, solutionDirectory));
                        break;

                    case "Folder":
                        var folder = ReadFolder(element, parentGuid);
                        solution.Projects.Add(folder);
                        // Projects and nested folders declared inside a folder are parented to it.
                        ReadContainer(element, folder.Guid, solution, solutionDirectory);
                        break;
                }
            }
        }

        private static Project ReadProject(
            XElement element,
            Guid parentGuid,
            string solutionDirectory
        )
        {
            var path =
                (string?)element.Attribute("Path")
                ?? throw new SolutionFileException(
                    "Invalid .slnx file: a 'Project' element is missing the required 'Path' attribute."
                );

            var relativePath = NormalizePath(path);
            var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativePath));

            var displayName = (string?)element.Attribute("DisplayName");
            var name = !string.IsNullOrEmpty(displayName)
                ? displayName!
                : Path.GetFileNameWithoutExtension(relativePath);

            var typeGuid = ResolveProjectTypeGuid((string?)element.Attribute("Type"), relativePath);

            return new Project(
                Guid.NewGuid(),
                typeGuid,
                name,
                fullPath,
                parentGuid,
                [],
                null,
                null
            );
        }

        private static Project ReadFolder(XElement element, Guid parentGuid)
        {
            var folderPath =
                (string?)element.Attribute("Name")
                ?? throw new SolutionFileException(
                    "Invalid .slnx file: a 'Folder' element is missing the required 'Name' attribute."
                );

            // Folder names are expressed as a path (e.g. "/Group/SubGroup/"); the display name is the last segment.
            var name = folderPath.Trim('/', '\\');
            var lastSeparator = name.LastIndexOfAny(['/', '\\']);
            if (lastSeparator >= 0)
            {
                name = name[(lastSeparator + 1)..];
            }

            return new Project(
                Guid.NewGuid(),
                KnownProjectTypeGuid.SolutionFolder,
                name,
                folderPath,
                parentGuid,
                [],
                null,
                null
            );
        }

        private static Guid ResolveProjectTypeGuid(string? type, string relativePath)
        {
            // The 'Type' attribute is optional and, when present, may contain an explicit project type GUID.
            if (!string.IsNullOrWhiteSpace(type) && Guid.TryParse(type, out var explicitGuid))
            {
                return explicitGuid;
            }

            // Otherwise the type is inferred from the project file extension, as Visual Studio does.
            return Path.GetExtension(relativePath).ToLowerInvariant() switch
            {
                ".csproj" => KnownProjectTypeGuid.CSharpNewSystem,
                ".vbproj" => KnownProjectTypeGuid.VisualBasic,
                ".fsproj" => KnownProjectTypeGuid.FSharp,
                ".vcxproj" => KnownProjectTypeGuid.VisualC,
                _ => Guid.Empty,
            };
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
