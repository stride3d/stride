using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Stride.Shaders.Spirv.Generators;


public static class SpirvGeneratorExtensions
{
    public static SourceText ToSourceText(this StringBuilder builder)
    {
        return SourceText.From(
            SyntaxFactory
            .ParseCompilationUnit(builder.ToString())
            .NormalizeWhitespace()
            .ToFullString(), 
            Encoding.UTF8
        );

    }
}