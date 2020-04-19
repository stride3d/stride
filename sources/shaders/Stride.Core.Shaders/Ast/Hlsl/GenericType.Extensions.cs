// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    public static class GenericTypeExtensions
    {
         
        public static TypeBase MakeGenericInstance(this GenericBaseType genericType, TypeBase genericTemplateType)
        {
            // TODO cache generic instance that are using predefined hlsl types
            var newType = genericTemplateType.DeepClone();

            var genericParameters = ((IGenerics)newType).GenericParameters;
            var genericArguments = ((IGenerics)newType).GenericArguments;
            var genericInstanceParameters = genericType.Parameters;

            var genericParameterTypes = new TypeBase[genericParameters.Count];
            var genericBaseParameterTypes = new TypeBase[genericParameters.Count];

            // Look for parameter instance types
            for (int i = 0; i < genericInstanceParameters.Count; i++)
            {
                var genericInstanceParameter = genericInstanceParameters[i];
                if (genericInstanceParameter is TypeBase)
                {
                    var genericInstanceParameterType = (TypeBase)genericInstanceParameter;
                    genericParameterTypes[i] = genericInstanceParameterType;
                    genericBaseParameterTypes[i] = TypeBase.GetBaseType(genericInstanceParameterType);
                    genericParameters[i] = genericParameterTypes[i];
                    genericArguments.Add(genericInstanceParameterType);
                }
            }

            // Replace all references to template arguments to their respective generic instance types
            SearchVisitor.Run(
                newType,
                node =>
                {
                    var typeInferencer = node as ITypeInferencer;
                    if (typeInferencer != null && typeInferencer.TypeInference.Declaration is GenericDeclaration)
                    {
                        var genericDeclaration = (GenericDeclaration)typeInferencer.TypeInference.Declaration;
                        var i = genericDeclaration.Index;
                        var targeType = genericDeclaration.IsUsingBase ? genericBaseParameterTypes[i] : genericParameterTypes[i];

                        if (node is TypeBase)
                            return targeType.ResolveType();
                    }

                    return node;
                });


            return newType;
        }
    }
}
