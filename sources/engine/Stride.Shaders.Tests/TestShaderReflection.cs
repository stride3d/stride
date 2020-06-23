// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;
using System.Linq;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Shaders.Ast;
using Stride.Core.Mathematics;

namespace Stride.Shaders.Tests
{
    public class TestShaderReflection
    {
        public EffectCompiler Compiler;

        public LoggerResult ResultLogger;

        public CompilerParameters MixinParameters;

        private void Init()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);

            Compiler = new EffectCompiler(databaseFileProvider);
            Compiler.SourceDirectories.Add("shaders");
            MixinParameters = new CompilerParameters();
            MixinParameters.EffectParameters.Platform = GraphicsPlatform.Direct3D11;
            MixinParameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;
            ResultLogger = new LoggerResult();
        }

        /// <summary>
        /// Tests whether default values defined in a shader are present in the shader reflection.
        /// The shader source code is generated on the fly to ensure that the generated keys haven't been
        /// created beforehand by the shader key generator tool.
        /// </summary>
        [Fact]
        public void TestDefaultValuesBeingPresentInReflection()
        {
            Init();

            var shaderClassName = "DefaultValuesTest";

            var variables = new List<(string name, string type, string value, object clrValue)>();
            variables.Add((name: "floatVar", type: "float", value: "1", clrValue: 1f));
            variables.Add((name: "doubleVar", type: "double", value: "1", clrValue: 1d));
            variables.Add((name: "intVar", type: "int", value: "1", clrValue: 1));
            variables.Add((name: "uintVar", type: "uint", value: "1", clrValue: 1u));
            variables.Add((name: "boolVar", type: "bool", value: "true", clrValue: true));
            AddVectorVariable(VectorType.Float2, 1f, Vector2.One);
            AddVectorVariable(VectorType.Float3, 1f, Vector3.One);
            AddVectorVariable(VectorType.Float4, 1f, Vector4.One);
            AddVectorVariable(VectorType.Double2, 1d, Double2.One);
            AddVectorVariable(VectorType.Double3, 1d, Double3.One);
            AddVectorVariable(VectorType.Double4, 1d, Double4.One);
            // error X3650: global variables cannot use the 'half' type in vs_5_0. To treat this variable as a float, use the backwards compatibility flag.
            //AddVectorVariable(VectorType.Half2, (Half)1f, Half2.One);
            //AddVectorVariable(VectorType.Half3, (Half)1f, Half3.One);
            //AddVectorVariable(VectorType.Half4, (Half)1f, Half4.One);
            AddVectorVariable(VectorType.Int2, 1, Int2.One);
            AddVectorVariable(VectorType.Int3, 1, Int3.One);
            AddVectorVariable(VectorType.Int4, 1, Int4.One);
            AddVectorVariable(VectorType.UInt4, 1u, UInt4.One);
            AddVectorVariable(new MatrixType(ScalarType.Float, 4, 4), 1f, new Matrix(1f));

            var assignments = new StringBuilder();
            foreach (var v in variables)
            {
                assignments.AppendLine($"{v.type} {v.name} = {v.value};");
            }

            var mixinSource = new ShaderMixinSource() { Name = shaderClassName };
            mixinSource.Mixins.Add(CreateShaderClassCode(shaderClassName, assignments.ToString()));
            var byteCodeTask = Compiler.Compile(mixinSource, MixinParameters.EffectParameters, MixinParameters);

            Assert.False(byteCodeTask.Result.CompilationLog.HasErrors);

            var byteCode = byteCodeTask.Result.Bytecode;
            var members = byteCode.Reflection.ConstantBuffers[0].Members;
            foreach (var v in variables)
            {
                var defaultValue = members.FirstOrDefault(k => k.KeyInfo.KeyName == $"{shaderClassName}.{v.name}").DefaultValue;
                Assert.NotNull(defaultValue);
                Assert.Equal(v.clrValue, defaultValue);
            }

            unsafe void AddVectorVariable<TVector, TComponent>(TypeBase type, TComponent scalarValue, TVector vectorValue)
                where TVector : unmanaged
                where TComponent : unmanaged
            {
                var name = $"{typeof(TVector).Name}Var";
                var dimension = sizeof(TVector) / sizeof(TComponent);
                var components = string.Join(", ", Enumerable.Repeat(scalarValue, dimension));
                variables.Add((
                    name: name,
                    type: type.ToString(),
                    value: $"{type}({components})",
                    clrValue: vectorValue));

                variables.Add((
                    name: $"{name}_Promoted",
                    type: type.ToString(),
                    value: $"{scalarValue}",
                    clrValue: vectorValue));

                variables.Add((
                    name: $"{name}_Array",
                    type: type.ToString(),
                    value: $"{{{components}}}",
                    clrValue: vectorValue));

                var aliasType =
                    type is MatrixType m ? $"{m.Type}{m.RowCount}x{m.ColumnCount}" :
                    type is VectorType v ? $"{v.Type}{v.Dimension}" :
                    default;
                if (aliasType != null)
                {
                    // Check type alias like float4 for vector<float, 4>
                    variables.Add((
                        name: $"{name}_Alias",
                        type: aliasType,
                        value: $"{{{components}}}",
                        clrValue: vectorValue));
                }
            }
        }

        /// <summary>
        /// Tests whether default values defined in a shader are being updated after modifications to the shader.
        /// </summary>
        [Fact]
        public void TestDefaultValuesGettingUpdatedAfterRecompile()
        {
            Init();

            var shaderClassName = "DefaultValuesBeingUpdatedTest";
            var variableName = "floatVar";

            // First register the key as it would've been done by the generator
            var initialKey = ParameterKeys.NewValue(1f, $"{shaderClassName}.{variableName}");
            ParameterKeys.Merge(initialKey, null, initialKey.Name);

            GenerateAndCheck("1", 1f);

            Compiler.ResetCache(new HashSet<string>() { shaderClassName });

            GenerateAndCheck("2", 2f);

            void GenerateAndCheck(string stringValue, float value)
            {
                var variables = new List<(string name, TypeBase type, string value, object clrValue)>();
                variables.Add((name: variableName, type: ScalarType.Float, value: stringValue, clrValue: value));

                var assignments = new StringBuilder();
                foreach (var v in variables)
                    assignments.AppendLine($"{v.type} {v.name} = {v.value};");

                var mixinSource = new ShaderMixinSource() { Name = shaderClassName };
                mixinSource.Mixins.Add(CreateShaderClassCode(shaderClassName, assignments.ToString()));
                var byteCodeTask = Compiler.Compile(mixinSource, MixinParameters.EffectParameters, MixinParameters);

                Assert.False(byteCodeTask.Result.CompilationLog.HasErrors);

                var byteCode = byteCodeTask.Result.Bytecode;
                using (var graphicsDevice = GraphicsDevice.New())
                {
                    // The effect constructor updates the effect reflection
                    var effect = new Effect(graphicsDevice, byteCode);

                    var members = byteCode.Reflection.ConstantBuffers[0].Members;
                    foreach (var v in variables)
                    {
                        // Fetch the default value via the key - the previous test already checked whether the default value is present in the value description
                        var effectValueDescription = members.FirstOrDefault(k => k.KeyInfo.KeyName == $"{shaderClassName}.{v.name}");
                        var defaultValue = effectValueDescription.KeyInfo.Key.DefaultValueMetadata.GetDefaultValue();
                        Assert.NotNull(defaultValue);
                        Assert.Equal(v.clrValue, defaultValue);
                    }
                }
            }
        }

        static ShaderClassCode CreateShaderClassCode(string className, string initializer)
        {
            return new ShaderClassString(className, @"
shader " + className + @"
{
    // Use a logical group which prevents the variables from being optimized away by EffectCompiler
    cbuffer Globals.Test
    {
" + initializer + @"
    }

    // Declare Vertex shader main method
    stage void VSMain() {}

    // Declare Pixel shader main method
    stage void PSMain() {}
};
            ");
        }
    }
}
