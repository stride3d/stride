// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

public class LiteralFoldingTests
{
    static Expression Parse(string text)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        Assert.True(ExpressionParser.Expression(ref scanner, result, out var parsed), text);
        Assert.True(scanner.IsEof, text);
        return parsed;
    }

    // HLSL folds unsuffixed integer literals in 64 bits, then gives the result the type of its context
    public static TheoryData<string, string?, object> FoldedCases => new()
    {
        { "1 / 2", "float", 0.0f },
        { "1 / 2.0", "float", 0.5f },
        { "5 / 2", null, 2 },
        { "3 / 2 * 2", "float", 2.0f },
        { "7 % 4", "float", 3.0f },
        { "-7 % 4", null, -3 },
        { "-(1 / 2)", "float", 0.0f },
        { "65536 * 65536", "float", 4294967296.0f },
        { "65536 * 65536", "int", 0 },
        { "65536 * 65536 / 3", "uint", 1431655765u },
        { "65536 * 65536 / 3", "float", 1431655765.0f },
        { "2147483647 + 1", "float", 2147483648.0f },
        { "2147483647 + 1", "int", int.MinValue },
        { "1 << 31 >> 31", "int", 1 },
        { "0 - 1", "uint", uint.MaxValue },
        { "~0", "uint", uint.MaxValue },
        { "(6 & 3) | 8 ^ 1", null, 11 },
        { "1 / 2", "float3", 0.0f },
        { "1 + 2", "double", 3.0 },
        { "1.5 + 1", "float", 2.5f },
        { "3000000000 + 1", "uint", 3000000001u },
        { "-2147483648 - 1", "float", -2147483649.0f },
        // A float stays a float in an integer context: the consumer converts it like any float
        { "1.5 + 1", "int", 2.5f },
    };

    [Theory]
    [MemberData(nameof(FoldedCases))]
    public void FoldsLiteralOperations(string text, string? context, object expected)
    {
        Assert.True(LiteralFolding.TryFold(Parse(text), out var value), text);

        var expectedType = context switch
        {
            null => null,
            "float3" => new VectorType(ScalarType.Float, 3),
            _ => (SymbolType)ScalarType.From(context),
        };
        var (type, constant) = LiteralFolding.ToConstant(value, expectedType);
        // Assert.Equal on object compares the type too: 0u is not 0
        Assert.Equal(expected, constant);
        Assert.Equal(expected.GetType(), type.Type switch
        {
            Scalar.Int => typeof(int),
            Scalar.UInt => typeof(uint),
            Scalar.Float => typeof(float),
            Scalar.Double => typeof(double),
            _ => null,
        });
    }

    // An operand with a type of its own takes the operation out of literal folding: 5u - 6 is a uint subtraction
    [Theory]
    [InlineData("5u - 6")]
    [InlineData("5u")]
    [InlineData("0xFF + 1")]
    [InlineData("1.0h + 1")]
    [InlineData("1.0f / 3")]
    [InlineData("5l - 6")]
    [InlineData("18446744073709551615 + 1")]
    [InlineData("1.0d * 2")]
    [InlineData("i / 4")]
    [InlineData("(float)1 / 2")]
    [InlineData("1 < 2")]
    [InlineData("1 / 0")]
    [InlineData("1.5 & 1")]
    public void LeavesTypedOperationsAlone(string text)
    {
        Assert.False(LiteralFolding.TryFold(Parse(text), out _), text);
    }
}
