// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Dynamic version of <see cref="YamlScalarNode"/>.
    /// </summary>
    public class DynamicYamlScalar : DynamicYamlObject, IDynamicYamlNode
    {
        internal YamlScalarNode node;

        public YamlScalarNode Node => node;

        YamlNode IDynamicYamlNode.Node => Node;

        public DynamicYamlScalar(YamlScalarNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            this.node = node;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = binder.Type.IsEnum
                ? Enum.Parse(binder.Type, node.Value)
                : Convert.ChangeType(node.Value, binder.Type, CultureInfo.InvariantCulture);

            return true;
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            var str = arg as string;
            if (str != null)
            {
                if (binder.Operation == ExpressionType.Equal)
                {
                    result = node.Value == str;
                    return true;
                }
                if (binder.Operation == ExpressionType.NotEqual)
                {
                    result = node.Value != str;
                    return true;
                }
            }
            return base.TryBinaryOperation(binder, arg, out result);
        }

        public override string ToString()
        {
            return node.Value;
        }
    }
}
