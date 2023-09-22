using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public static class PropertyAttributeFinderExtension
{
    /// <summary>
    /// Determines whether a property should be ignored based on the presence of a specific attribute.
    /// </summary>
    /// <param name="finder">The <see cref="IPropertyFinder"/> used to locate properties.</param>
    /// <param name="property">The <see cref="IPropertySymbol"/> to be checked for the presence of the "DataMemberIgnore" attribute.</param>
    /// <returns>
    /// <c>true</c> if the property has the "DataMemberIgnore" attribute from the "Stride.Core" namespace; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks the attributes of the given property to determine if it should be ignored.
    /// It specifically looks for the "DataMemberIgnore" attribute from the "Stride.Core" namespace.
    /// If the attribute is found, the method returns <c>true</c>, indicating that the property should be ignored.
    /// Otherwise, it returns <c>false</c>.
    /// </remarks>
    public static bool ShouldBeIgnored(this IViolationReporter reporter, IPropertySymbol property)
    {
        if (property.DeclaredAccessibility == Accessibility.Private ||
            property.DeclaredAccessibility == Accessibility.ProtectedAndInternal ||
            property.DeclaredAccessibility == Accessibility.NotApplicable ||
            property.DeclaredAccessibility == Accessibility.Protected)
        {
            return true;
        }
        var attributes = property.GetAttributes();
        foreach (var attribute in attributes)
        {
            var attributeType = attribute.AttributeClass;

            if (attributeType != null)
            {
                if (attributeType.Name == "DataMemberIgnoreAttribute"
                     && attributeType.ContainingNamespace.ContainingModule.Name == "Stride.Core.dll")
                {
                    return true;
                }
            }
        }
        return false;
    }
    public static bool HasDataMemberAnnotation(this IViolationReporter reporter, IPropertySymbol property)
    {
        var attributes = property.GetAttributes();
        foreach (var attribute in attributes)
        {
            var attributeType = attribute.AttributeClass;
            if (attributeType != null)
            {
                if (attributeType.Name == "DataMemberAttribute" && attributeType.ContainingNamespace.ContainingModule.Name == "Stride.Core.dll")
                {
                    return true;
                }
            }
        }
        return false;
    }
}
