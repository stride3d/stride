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
        if (obj == null)
            return new YamlScalarNode("null");

        if (obj is string str)
        {
            return new YamlScalarNode(str);
        }

        if (obj is YamlNode node)
            return node;

        if (obj is DynamicYamlMapping mapping)
            return mapping.Node;
        if (obj is DynamicYamlArray array)
            return array.node;
        if (obj is DynamicYamlScalar scalar)
            return scalar.node;

        if (obj is bool b)
            return new YamlScalarNode(b ? "true" : "false");

        return new YamlScalarNode(string.Format(CultureInfo.InvariantCulture, "{0}", obj));
    }

    public static object ConvertToDynamic(object obj)
    {
        if (obj is YamlScalarNode scalar)
            return new DynamicYamlScalar(scalar);
        if (obj is YamlMappingNode mapping)
            return new DynamicYamlMapping(mapping);
        if (obj is YamlSequenceNode sequence)
            return new DynamicYamlArray(sequence);

        return obj;
    }
}
