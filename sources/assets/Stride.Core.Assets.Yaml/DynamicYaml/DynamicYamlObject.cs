// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Dynamic;
using System.Globalization;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml;

public abstract class DynamicYamlObject : DynamicObject
{
    protected static YamlNode ConvertFromDynamic(object? obj)
    {
        return obj switch
        {
            null => new YamlScalarNode("null"),
            string str => new YamlScalarNode(str),
            YamlNode node => node,
            DynamicYamlMapping mapping => mapping.Node,
            DynamicYamlArray array => array.node,
            DynamicYamlScalar scalar => scalar.node,
            bool b => new YamlScalarNode(b ? "true" : "false"),
            _ => new YamlScalarNode(string.Format(CultureInfo.InvariantCulture, "{0}", obj))
        };
    }

    public static object ConvertToDynamic(object obj)
    {
        return obj switch
        {
            YamlScalarNode scalar => new DynamicYamlScalar(scalar),
            YamlMappingNode mapping => new DynamicYamlMapping(mapping),
            YamlSequenceNode sequence => new DynamicYamlArray(sequence),
            _ => obj
        };
    }
}
