// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Allows to manipulate dynamically a YAML content.
    /// </summary>
    public class DynamicYaml
    {
        private static readonly SerializerSettings DefaultSettings = new SerializerSettings();
        private readonly YamlStream yamlStream;
        private DynamicYamlMapping dynamicRootNode;

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicYaml"/> from the specified stream.
        /// </summary>
        /// <param name="stream">A stream that contains a YAML content. The stream will be disposed</param>
        /// <param name="disposeStream">Dispose the stream when reading the YAML content is done. <c>true</c> by default</param>
        public DynamicYaml(Stream stream, bool disposeStream = true)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            // transform the stream into string.
            string assetAsString;
            try
            {
                using (var assetStreamReader = new StreamReader(stream, Encoding.UTF8))
                {
                    assetAsString = assetStreamReader.ReadToEnd();
                }
            }
            finally
            {
                if (disposeStream)
                {
                    stream.Dispose();
                }
            }

            // Load the asset as a YamlNode object
            var input = new StringReader(assetAsString);
            yamlStream = new YamlStream();
            yamlStream.Load(input);
            
            if (yamlStream.Documents.Count != 1 || !(yamlStream.Documents[0].RootNode is YamlMappingNode))
                throw new YamlException("Unable to load the given stream");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicYaml"/> from the specified stream.
        /// </summary>
        /// <param name="text">A text that contains a YAML content</param>
        public DynamicYaml(string text) : this(GetSafeStream(text))
        {
        }
        /// <summary>
        /// Gets the root YAML node.
        /// </summary>
        public YamlMappingNode RootNode => (YamlMappingNode)yamlStream.Documents[0].RootNode;

        /// <summary>
        /// Gets a dynamic YAML node around the <see cref="RootNode"/>.
        /// </summary>
        public dynamic DynamicRootNode => dynamicRootNode ?? (dynamicRootNode = new DynamicYamlMapping(RootNode));

        /// <summary>
        /// Writes the content of this YAML node to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to output YAML to.</param>
        /// <param name="settings">The settings to use to generate YAML. If null, a default <see cref="SerializerSettings"/> will be used.</param>
        public void WriteTo(Stream stream, SerializerSettings settings)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var streamWriter = new StreamWriter(stream))
            {
                WriteTo(streamWriter, DefaultSettings);
            }
        }

        /// <summary>
        /// Writes the content of this YAML node to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to output YAML to.</param>
        /// <param name="settings">The settings to use to generate YAML. If null, a default <see cref="SerializerSettings"/> will be used.</param>
        public void WriteTo(TextWriter writer, SerializerSettings settings)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var preferredIndent = (settings ?? DefaultSettings).PreferredIndent;
            yamlStream.Save(writer, true, preferredIndent);
            writer.Flush();
        }

        public override string ToString()
        {
            using (var streamWriter = new StringWriter())
            {
                WriteTo(streamWriter, DefaultSettings);
                return streamWriter.ToString();
            }
        }

        private static Stream GetSafeStream(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }
}
