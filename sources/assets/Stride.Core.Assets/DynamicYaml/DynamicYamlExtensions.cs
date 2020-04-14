// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    // TODO: this works only for asset now. Allow to select the YamlSerializer to use to make it work for other scenario
    public static class DynamicYamlExtensions
    {
        public static T ConvertTo<T>(IDynamicYamlNode yamObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                // convert Yaml nodes to string
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    var yamlStream = new YamlStream { new YamlDocument(yamObject.Node) };
                    yamlStream.Save(streamWriter, true, AssetYamlSerializer.Default.GetSerializerSettings().PreferredIndent);

                    streamWriter.Flush();
                    memoryStream.Position = 0;

                    // convert string to object
                    return (T)AssetYamlSerializer.Default.Deserialize(memoryStream, typeof(T));
                }
            }
        }

        public static IDynamicYamlNode ConvertFrom<T>(T dataObject)
        {
            using (var stream = new MemoryStream())
            {
                // convert data to string
                AssetYamlSerializer.Default.Serialize(stream, dataObject);

                stream.Position = 0;

                // convert string to Yaml nodes
                using (var reader = new StreamReader(stream))
                {
                    var yamlStream = new YamlStream();
                    yamlStream.Load(reader);
                    return (IDynamicYamlNode)DynamicYamlObject.ConvertToDynamic(yamlStream.Documents[0].RootNode);
                }
            }
        }
    }
}
