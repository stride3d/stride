// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Convertor;

namespace Stride.Shaders.Parser
{
    public static class ShaderExtensions
    {
        // Used as key tag on TypeBase to link it to its actual ShaderClassType (if specified by user)
        internal static string associatedClass = "AssociatedClass";
        // Used as key tag on ShaderRootClassType to define its composition types
        internal static string associatedCompositions = "AssociatedCompositions";
        // Used as key tag on TypeBase to link it to its actual ShaderClassType (if specified by user)
        public static readonly string AssociatedMacrosTag = "AssociatedMacros";

        public static void ReplaceAnnotation(this IAttributes node, string name, params object[] values)
        {
            foreach (var annotation in node.Attributes.OfType<AttributeDeclaration>())
            {
                if (annotation.Name == name && annotation.Parameters.Count >= 1)
                {
                    annotation.Parameters = values.Select(x => new Literal(x)).ToList();
                    return;
                }
            }
            node.Attributes.Add(new AttributeDeclaration { Name = new Identifier(name), Parameters = values.Select(x => new Literal(x)).ToList() });
        }

        public static ShaderRootClassType GetMainShaderClass(this Shader shader)
        {
            var defaultShader = shader.Declarations.OfType<ShaderRootClassType>().FirstOrDefault(x => x.Name == "Shader");
            if (defaultShader == null)
            {
                defaultShader = new ShaderRootClassType("Shader");
                shader.Declarations.Add(defaultShader);
            }
            return defaultShader;
        }

        public static ShaderRootClassType StartMix()
        {
            // TODO: Rename during Shader mixing
            return new ShaderRootClassType("Mix");
        }

        public static ShaderRootClassType Mix(this Shader shader, TypeBase mixinClass)
        {
            // Find the shader class which will drive compilation and add this new mixinClass in the list.
            var mainShaderClass = shader.GetMainShaderClass();
            mainShaderClass.Mix(mixinClass);

            return mainShaderClass;
        }

        public static ShaderRootClassType Mix(this ShaderRootClassType target, TypeBase mixinClass)
        {
            var typeName = new TypeName(mixinClass.Name);
            if (mixinClass is ShaderClassType)
                typeName.SetTag(associatedClass, mixinClass);
            target.BaseClasses.Add(typeName);

            return target;
        }

        public static ShaderRootClassType Compose(this ShaderRootClassType sourceClass, string variableName, params ShaderClassType[] variableTypes)
        {
            var currentVariableTypes = (Dictionary<string, ShaderClassType[]>)sourceClass.GetTag(associatedCompositions);
            if (currentVariableTypes == null)
            {
                currentVariableTypes = new Dictionary<string, ShaderClassType[]>();
                sourceClass.SetTag(associatedCompositions, currentVariableTypes);
            }

            currentVariableTypes[variableName] = variableTypes;

            return sourceClass;
        }

        private class NameEqualityComparer : IEqualityComparer<IDeclaration>
        {
            public bool Equals(IDeclaration x, IDeclaration y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(IDeclaration obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        public static MethodDefinition GetEntryPoint(this Shader shader, ShaderStage type)
        {
            return shader.Declarations.OfType<MethodDefinition>().FirstOrDefault(f => f.Attributes.OfType<AttributeDeclaration>().Any(a => a.Name == "EntryPoint" && (string)a.Parameters[0].Value == type.ToString()));
        }
    }
}
