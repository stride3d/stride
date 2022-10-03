using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Reflection;
using static Stride.Core.CompilerServices.Tests.Serialization.SerializerGeneratorCompilationUtils;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices.Tests.Serialization.DataContract
{
    public class DataContractSourceCodeTests
    {
        [Fact]
        public void PocoClass_WithPrimitiveMembers_GetsEmittedCorrectly()
        {
            const string sourceCode = """
                namespace UserCode
                {
                    using System;
                    using Stride.Core;

                    [DataContract]
                    public class PocoWithPrimitveMembers
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                        public int Balance { get; set; }
                        public float Amount { get; set; }
                        public bool IsBalanced { get; set; }
                        public Guid Id { get; set; }
                        public byte[] Bytes { get; set; }

                        public double D;
                        public ushort U16;
                        public short I16;
                        public uint U32;
                        public ulong U64;
                        public long I64;
                        public char C;
                        public byte B;
                        public sbyte SB;
                        public int? Nullable1;
                    }
                }
                """;

            var (compilation, diagnostics) = CompilerUtils.CompileWithGenerator<SerializerGenerator>("PocoWithPrimitiveMembers", sourceCode);
            
            Assert.Empty(diagnostics);

            var serializerFactory = compilation.GetSerializationFactory();
            var serializerGlobalAttributes = serializerFactory.GetAttributes(compilation, "Stride.Core.Serialization.DataSerializerGlobalAttribute");
            Assert.Collection(serializerGlobalAttributes,
                // global attribute from DataContract
                attr => attr.AssertDataSerializerGlobal(
                    compilation.GetTypeByMetadataName("Stride.Core.DataSerializers.V2.UserCode_PocoWithPrimitveMembersSerializer"),
                    compilation.GetTypeByMetadataName("UserCode.PocoWithPrimitveMembers"),
                    DataSerializerGenericMode.None,
                    false,
                    true),
                // global attribute for int? specialization of NullableSerializer
                attr => attr.AssertDataSerializerGlobal(
                    compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.NullableSerializer`1")
                        .Construct(compilation.GetSpecialType(SpecialType.System_Int32)),
                    compilation.GetTypeByMetadataName("System.Nullable`1")
                        .Construct(compilation.GetSpecialType(SpecialType.System_Int32)),
                    DataSerializerGenericMode.None,
                    false,
                    false));
        }

        [Fact]
        public void PocoClass_WithClassMember_WhenClassIsSerializable_GetsEmittedCorrectly()
        {
            const string sourceCode = """
                namespace UserCode
                {
                    using System;
                    using Stride.Core;

                    [DataContract]
                    public class PocoWithClassMember
                    {
                        public SecondClass C { get; set; }
                    }

                    [DataContract]
                    public class SecondClass
                    {
                        public string A { get; set; }
                    }
                }
                """;

            var (compilation, diagnostics) = CompilerUtils.CompileWithGenerator<SerializerGenerator>("PocoWithClassMember", sourceCode);

            Assert.Empty(diagnostics);

            var serializerFactory = compilation.GetSerializationFactory();
            var serializerGlobalAttributes = serializerFactory.GetAttributes(compilation, "Stride.Core.Serialization.DataSerializerGlobalAttribute");
            Assert.Collection(serializerGlobalAttributes,
                // global attribute from DataContract for PocoWithClassMember
                attr => attr.AssertDataSerializerGlobal(
                    compilation.GetTypeByMetadataName("Stride.Core.DataSerializers.V2.UserCode_PocoWithClassMemberSerializer"),
                    compilation.GetTypeByMetadataName("UserCode.PocoWithClassMember"),
                    DataSerializerGenericMode.None,
                    false,
                    true),
                // global attribute from DataContract for SecondClass
                attr => attr.AssertDataSerializerGlobal(
                    compilation.GetTypeByMetadataName("Stride.Core.DataSerializers.V2.UserCode_SecondClassSerializer"),
                    compilation.GetTypeByMetadataName("UserCode.SecondClass"),
                    DataSerializerGenericMode.None,
                    false,
                    true));
        }

        [Fact]
        public void PocoClass_WithClassMember_WhenClassIsNotSerializable_EmitsWarning()
        {
            const string sourceCode = """
                namespace UserCode
                {
                    using System;
                    using Stride.Core;

                    [DataContract]
                    public class PocoWithClassMember
                    {
                        public SecondClass C { get; set; }
                    }

                    public class SecondClass
                    {
                        public string A { get; set; }
                    }
                }
                """;

            var (compilation, diagnostics) = CompilerUtils.CompileWithGenerator<SerializerGenerator>("PocoWithClassMember", sourceCode);

            Assert.Collection(diagnostics,
                diag =>
                {
                    Assert.Equal("STR2003", diag.Id);
                    Assert.Contains($"Member {"C"} of class {"PocoWithClassMember"} is of type {"SecondClass"} that cannot be serialized.", diag.ToString());
                });
        }
    }

    
}
