// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Tests
{
    public class TestCecilExtensions
    {
        class Nested
        {
        }

        private readonly BaseAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();

        public TestCecilExtensions()
        {
            // Add location of current assembly to MonoCecil search path.
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(typeof(TestCecilExtensions).Assembly.Location));
        }

        private string GenerateNameCecil(Type type)
        {
            var typeReference = type.GenerateTypeCecil(assemblyResolver);

            return typeReference.ConvertAssemblyQualifiedName();
        }

        private static string GenerateNameDotNet(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        private void CheckGeneratedNames(Type type)
        {
            var nameCecil = GenerateNameCecil(type);
            var nameDotNet = GenerateNameDotNet(type);
            Assert.Equal(nameDotNet, nameCecil);
        }

        [Fact]
        public void TestCecilDotNetAssemblyQualifiedNames()
        {
            // Primitive value type
            CheckGeneratedNames(typeof(bool));

            // Primitive class
            CheckGeneratedNames(typeof(string));

            // User class
            CheckGeneratedNames(typeof(TestCecilExtensions));

            // Closed generics
            CheckGeneratedNames(typeof(Dictionary<string, object>));

            // Open generics
            CheckGeneratedNames(typeof(Dictionary<,>));

            // Nested types
            CheckGeneratedNames(typeof(Nested));

            // Arrays
            CheckGeneratedNames(typeof(string[]));
            CheckGeneratedNames(typeof(Dictionary<string, object>[]));

            // Nullable
            CheckGeneratedNames(typeof(bool?));
        }
    }
}
