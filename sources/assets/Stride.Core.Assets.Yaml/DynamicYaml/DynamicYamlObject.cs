// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Dynamic;
using System.Globalization;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    public abstract class DynamicYamlObject : DynamicObject
    {
        protected static YamlNode ConvertFromDynamic(object obj)
        {
            if (obj == null)
                return new YamlScalarNode("null");

            if (obj is string)
            {
                return new YamlScalarNode((string)obj);
            }

            if (obj is YamlNode)
                return (YamlNode)obj;

            if (obj is DynamicYamlMapping)
                return ((DynamicYamlMapping)obj).Node;
            if (obj is DynamicYamlArray)
                return ((DynamicYamlArray)obj).node;
            if (obj is DynamicYamlScalar)
                return ((DynamicYamlScalar)obj).node;

            if (obj is bool)
                return new YamlScalarNode((bool)obj ? "true" : "false");

            return new YamlScalarNode(string.Format(CultureInfo.InvariantCulture, "{0}", obj));
        }

        public static object ConvertToDynamic(object obj)
        {
            if (obj is YamlScalarNode)
                return new DynamicYamlScalar((YamlScalarNode)obj);
            if (obj is YamlMappingNode)
                return new DynamicYamlMapping((YamlMappingNode)obj);
            if (obj is YamlSequenceNode)
                return new DynamicYamlArray((YamlSequenceNode)obj);

            return obj;

        }
    }
}
