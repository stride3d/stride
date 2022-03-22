using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string DataContractAttributeName = "Stride.Core.DataContractAttribute";
        private const string DataMemberAttributeName = "Stride.Core.DataMemberAttribute";
        private const string DataMemberIgnoreAttributeName = "Stride.Core.DataMemberIgnoreAttribute";

        internal SerializerSpec GenerateSpec(GeneratorExecutionContext context)
        {
            List<SerializerTypeSpec> typeSpecs = new List<SerializerTypeSpec>();
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

                foreach (var classSyntax in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Cast<ClassDeclarationSyntax>())
                {
                    if (CheckForDataContract(semanticModel, classSyntax))
                    {
                        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
                        if (!classSymbol.Constructors.Any(ctor => !ctor.Parameters.Any()))
                        {
                            // complain warning
                            continue;
                        }

                        typeSpecs.Add(GenerateTypeSpec(semanticModel, classSyntax));
                    }
                }
            }

            return new SerializerSpec
            {
                DataContractTypes = typeSpecs,
            };
        }

        private static SerializerTypeSpec GenerateTypeSpec(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax)
        {
            // get members
            var type = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
            var members = new List<SerializerMemberSpec>();

            // TODO: assign Order to each member if it's set in [DataMember] || type.IsSequentialLayout || type.IsExplicitLayout <- based on [FieldOffset]

            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member is IMethodSymbol)
                    continue;

                var attributes = member.GetAttributes();
                var ignoreAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == DataMemberIgnoreAttributeName);
                var memberAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == DataMemberAttributeName);

                if (ignoreAttribute != null)
                {
                    if (memberAttribute != null)
                    {
                        // complain warning
                    }

                    // member was ignored
                    continue;
                }

                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    if (!(member.DeclaredAccessibility == Accessibility.Internal && memberAttribute != null))
                    {
                        // member is not public or internal with attribute 
                        continue;
                    }
                }

                string name = null;
                ITypeSymbol memberType = null;

                if (member is IPropertySymbol prop)
                {
                    if (prop.SetMethod == null && prop.Type.IsValueType)
                    {
                        // structs have to be assignable to properties
                        continue;
                    }

                    if (prop.GetMethod == null)
                    {
                        // a serializable property has to have a get method
                        continue;
                    }

                    name = prop.Name;
                    memberType = prop.Type;
                }
                else if (member is IFieldSymbol field)
                {
                    name = field.Name;
                    memberType = field.Type;
                }

                if (name == null || memberType == null)
                {
                    // member is neither a property or a field?
                    continue;
                }

                members.Add(new SerializerMemberSpec(name, memberType));
            }

            return new SerializerTypeSpec(type, members);
        }

        private bool CheckForDataContract(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax)
        {
            // check if class has [DataContract]
            bool hasDataContract = CheckClassAttributesForDataContract(semanticModel, classSyntax);

            // check if class inherits [DataContract]
            if (!hasDataContract && classSyntax.BaseList != null)
            {
                hasDataContract = CheckBaseClassesForInheritedDataContract(semanticModel, classSyntax);
            }

            return hasDataContract;
        }

        private static bool CheckClassAttributesForDataContract(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax)
        {
            foreach (AttributeListSyntax attributeListSyntax in classSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    // Get Attribute declaration symbol which is a method
                    IMethodSymbol attributeSymbol = semanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                    if (attributeSymbol == null)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // if the name of the type equals to name of DataContractAttribute add it to collection
                    if (fullName == DataContractAttributeName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckBaseClassesForInheritedDataContract(SemanticModel semanticModel, ClassDeclarationSyntax classSyntax)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
            return CheckSymbolBaseClassesForInheritedDataContract(classSymbol);
        }

        private static bool CheckSymbolClassAttributesForDataContract(INamedTypeSymbol classSymbol)
        {
            var attributes = classSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                var attributeFullName = attribute.AttributeClass.ToDisplayString();
                if (attributeFullName == DataContractAttributeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckSymbolBaseClassesForInheritedDataContract(INamedTypeSymbol classSymbol)
        {
            var baseSymbol = classSymbol.BaseType;
            while (baseSymbol != null)
            {
                var attributes = baseSymbol.GetAttributes();

                if (attributes == null)
                {
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    var attributeFullName = attribute.AttributeClass.ToDisplayString();
                    if (attributeFullName == DataContractAttributeName)
                    {
                        foreach (var namedArgument in attribute.NamedArguments)
                        {
                            if (namedArgument.Key == "Inherited" && namedArgument.Value.Value.Equals(true))
                            {
                                return true;
                            }
                        }
                    }
                }

                baseSymbol = baseSymbol.BaseType;
            }

            return false;
        }
    }
}
