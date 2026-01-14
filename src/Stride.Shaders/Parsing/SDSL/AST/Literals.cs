using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Stride.Shaders.Parsing.SDSL.AST;



public abstract class Literal(TextLocation info) : Expression(info);
public abstract class ValueLiteral(TextLocation info) : Literal(info);
public abstract class ScalarLiteral(TextLocation info) : ValueLiteral(info);

public class StringLiteral(string value, TextLocation info) : Literal(info)
{
    public string Value { get; set; } = value;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;

        var i = context.Add(new OpConstantStringSDSL(context.Bound++, Value));
        // Note: we rely on undefined type (0); we assume those string literals will be used in only very specific cases where we expect them (i.e. generic instantiation parameters) and will be removed
        return new SpirvValue(i.IdResult.Value, 0);
    }

    public override SpirvValue CompileAsValue(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        // Since we use type 0, CompileAsValue won't work
        return CompileImpl(table, compiler);
    }

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}

public abstract class NumberLiteral(TextLocation info) : ScalarLiteral(info)
{
    public abstract double DoubleValue { get; }
    public abstract int IntValue { get; }
    public abstract long LongValue { get; }

}
public abstract class NumberLiteral<T>(Suffix suffix, T value, TextLocation info) : NumberLiteral(info)
    where T : struct, INumber<T>
{
    public Suffix Suffix { get; set; } = suffix;
    public T Value { get; set; } = value;
    public override double DoubleValue => Convert.ToDouble(Value);
    public override long LongValue => Convert.ToInt64(Value);
    public override int IntValue => Convert.ToInt32(Value);

    public override string ToString()
    {
        return $"{Value}{Suffix}";
    }

}

public class IntegerLiteral(Suffix suffix, long value, TextLocation info) : NumberLiteral<long>(suffix, value, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        // If expectedType is float, handle it:
        if (expectedType is ScalarType { TypeName: "float" })
        {
            Type = expectedType;
            return compiler.Context.CompileConstantLiteral(new FloatLiteral(new(32, true, true), value, null, info));
        }

        return compiler.Context.CompileConstantLiteral(this);
    }
}

public sealed class FloatLiteral(Suffix suffix, double value, int? exponent, TextLocation info) : NumberLiteral<double>(suffix, value, info)
{
    public int? Exponent { get; set; } = exponent;
    public static implicit operator FloatLiteral(double v) => new(new(), v, null, new());

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        return compiler.Context.CompileConstantLiteral(this);
    }
}

public sealed class HexLiteral(ulong value, TextLocation info) : IntegerLiteral(new(value > uint.MaxValue ? 64 : 32, false, false), (long)value, info)
{
    public override SymbolType? Type => Suffix.Size > 32 ? ScalarType.From("ulong") : ScalarType.From("uint");
}


public class BoolLiteral(bool value, TextLocation info) : ScalarLiteral(info)
{
    public bool Value { get; set; } = value;
    public override SymbolType? Type => ScalarType.From("bool");

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        return compiler.Context.CompileConstantLiteral(this);
    }
}

public class ExpressionLiteral(Expression value, TypeName typeName, TextLocation info) : ValueLiteral(info)
{
    public Expression Value { get; set; } = value;
    public TypeName TypeName { get; set; } = typeName;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var castType = TypeName.ResolveType(table, context);
        var value = Value.CompileAsValue(table, compiler);

        Type = castType;

        return builder.Convert(context, value, castType);
    }
}

public abstract class CompositeLiteral(TextLocation info) : ValueLiteral(info)
{
    public List<Expression> Values { get; set; } = [];

    public bool IsConstant()
    {
        foreach (var v in Values)
            if (v is not NumberLiteral or BoolLiteral)
                return false;
        return true;
    }

    public abstract SymbolType GenerateType(SymbolTable table, SpirvContext context, SymbolType expectedType);

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;

        Type = GenerateType(table, context, expectedType);
        if (Type is ArrayType t2 && t2.Size == -1)
            Type = new ArrayType(t2.BaseType, Values.Count);
        
        (var compositeCount, var totalCount, var expectedElementType) = Type switch
        {
            VectorType v => (v.Size, v.Size, v.BaseType),
            MatrixType m => (m.Rows, m.Columns * m.Rows, m.BaseType),
            ArrayType t => (t.Size, t.Size, t.BaseType),
        };

        Span<int> values = stackalloc int[totalCount];
        Span<int> compositeValues = stackalloc int[compositeCount];

        // Note: There are a lot of opportunity to optimize by working with vector-to-vector copy (if they align correctly) and/or OpVectorShuffle, but it can get quite complex to handle all cases
        //       However, it is probably optimized by SPIRV-Cross or the compiler/driver, so maybe not worth optimzing (due to increased code cases/complexity)
        var elementIndex = 0;
        for (int i = 0; i < Values.Count; i++)
        {
            // Note: we can compute expected element type only if there are as many source values as expected elements
            // i.e. float3(float, float, float) is OK but float3(float, float2) is not as we don't know which element will be which before compiling them (we would need 2-pass compilation for that)
            var value = Values[i].CompileAsValue(table, compiler, expectedElementType);
            var valueType = Values[i].ValueType;
            
            // We expand elements, because float4 can be created from (float, float2, float), or (float2x2)
            if (Type is ScalarType or VectorType or MatrixType)
            {
                var sourceElementType = valueType.GetElementType();
                for (int j = 0; j < valueType.GetElementCount(); ++j)
                {
                    SpirvValue extractedValue = valueType switch
                    {
                        MatrixType m => new(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(sourceElementType), context.Bound++, value.Id, [j / m.Rows, j % m.Rows]))),
                        VectorType v => new(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(sourceElementType), context.Bound++, value.Id, [j]))),
                        ScalarType s => value,
                    };
                    // If too many elments, keep counting so that exception is still thrown a bit later, with total count
                    var currentElementIndex = elementIndex++;
                    if (currentElementIndex >= values.Length)
                        continue;
                    values[currentElementIndex] = builder.Convert(context, extractedValue, expectedElementType).Id;
                }
            }
            else if (Type is ArrayType arrayType)
            {
                values[elementIndex++] = builder.Convert(context, value, expectedElementType).Id;
            }
        }

        if (elementIndex != totalCount)
            throw new InvalidOperationException($"{nameof(VectorLiteral)}: Expecting {totalCount} elements but got {elementIndex} for type {Type}");

        // Regroup by rows (if necessary, only for Matrix)
        int compositeSize = totalCount / compositeCount;
        for (int i = 0; i < compositeCount; ++i)
        {
            compositeValues[i] = Type switch
            {
                MatrixType m => builder.Insert(new OpCompositeConstruct(context.GetOrRegister(new VectorType(m.BaseType, compositeSize)), context.Bound++, [.. values.Slice(i * compositeSize, compositeSize)])).ResultId,
                VectorType v => values[i],
                ArrayType => values[i],
            };
        }

        var instruction = builder.Insert(new OpCompositeConstruct(context.GetOrRegister(Type), context.Bound++, [.. compositeValues]));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class VectorLiteral(TypeName typeName, TextLocation info) : CompositeLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;

    public override SymbolType GenerateType(SymbolTable table, SpirvContext context, SymbolType expectedType) => TypeName.ResolveType(table, context);

    public override string ToString()
    {
        return $"{TypeName}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}


public class MatrixLiteral(TypeName typeName, int rows, int cols, TextLocation info) : CompositeLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
    public int Rows { get; set; } = rows;
    public int Cols { get; set; } = cols;

    public override SymbolType GenerateType(SymbolTable table, SpirvContext context, SymbolType expectedType) => TypeName.ResolveType(table, context);

    public override string ToString()
    {
        return $"{TypeName}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}

public class ArrayLiteral(TextLocation info) : CompositeLiteral(info)
{
    public override SymbolType GenerateType(SymbolTable table, SpirvContext context, SymbolType expectedType)
    {
        // If we have any expected type (i.e. it's being assigned to a variable), use it
        if (expectedType != null)
            return expectedType;

        throw new InvalidOperationException("Can't infer array type");
    }

    public override string ToString()
        => $"{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
}

public class Identifier(string name, TextLocation info) : Literal(info)
{
    internal bool AllowStreamVariables { get; set; }
    public string Name { get; set; } = name;

    public static implicit operator string(Identifier identifier) => identifier.Name;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;

        return CompileSymbol(table, builder, context, builder.CurrentFunction == null);
    }

    private SpirvValue CompileSymbol(SymbolTable table, SpirvBuilder builder, SpirvContext context, bool constantOnly)
    {
        if (Name == "streams")
        {
            var result = builder.Insert(new OpStreamsSDSL(context.Bound++));
            return new(result.ResultId, context.GetOrRegister(new PointerType(new StreamsType(), Specification.StorageClass.Private)));
        }

        if (!table.TryResolveSymbol(Name, out var symbol))
        {
            if (constantOnly)
                throw new NotImplementedException();

            if (!table.ShaderLoader.Exists(Name))
                throw new InvalidOperationException($"Symbol [{Name}] could not be found.");

            // Maybe it's a static variable? try to resolve by loading file
            var classSource = new ShaderClassInstantiation(Name, []);

            // Shader is inherited (TODO: do we want to do something more "selective", i.e. import only the required variable if it's a cbuffer?)
            var inheritedShaderCount = table.InheritedShaders.Count;
            classSource = SpirvBuilder.BuildInheritanceListIncludingSelf(table.ShaderLoader, context, classSource, table.CurrentMacros.AsSpan(), table.InheritedShaders, ResolveStep.Compile);
            for (int i = inheritedShaderCount; i < table.InheritedShaders.Count; ++i)
            {
                table.InheritedShaders[i].Symbol = ShaderClass.LoadAndCacheExternalShaderType(table, context, table.InheritedShaders[i]);
                ShaderClass.Inherit(table, context, table.InheritedShaders[i].Symbol, false);
            }

            // We add the typename as a symbol (similar to static access in C#)
            var shaderId = context.GetOrRegister(classSource.Symbol);
            symbol = new Symbol(new(classSource.Symbol.Name, SymbolKind.Shader), new PointerType(classSource.Symbol, Specification.StorageClass.Private), shaderId);
            table.CurrentFrame.Add(classSource.Symbol.Name, symbol);
        }

        if (symbol.Id.Storage == Storage.Stream && !AllowStreamVariables)
            throw new InvalidOperationException($"Streams member {Name} used without an object");
        Type = symbol.Type;
        return EmitSymbol(builder, context, symbol, constantOnly);
    }

    public static SpirvValue EmitSymbol(SpirvBuilder builder, SpirvContext context, Symbol symbol, bool constantOnly, int? instance = null)
    {
        var resultType = context.GetOrRegister(symbol.Type);
        var result = new SpirvValue(symbol.IdRef, resultType, symbol.Id.Name);

        // Shader symbols are treated separately (we want to return only the shader instance (or this if not specified))
        if (symbol.Id.Kind == SymbolKind.Shader)
        {
            if (constantOnly)
                throw new NotImplementedException();

            if (instance == null)
                instance = builder.Insert(new OpThisSDSL(context.Bound++)).ResultId;
            result.Id = instance.Value;
            return result;
        }

        if (symbol.ExternalConstant is { } externalConstant)
        {
            if (externalConstant.SourceContext != context)
            {
                var bufferForConstant = externalConstant.SourceContext.ExtractConstantAsSpirvBuffer(externalConstant.ConstantId);
                result.Id = context.InsertWithoutDuplicates(null, bufferForConstant);
            }
            else
            {
                result.Id = externalConstant.ConstantId;
            }
        }
        else if (symbol.MemberAccessWithImplicitThis is { } thisType)
        {
            if (constantOnly)
                throw new NotImplementedException();

            var isStage = symbol.Id.IsStage;
            if (instance == null)
            {
                instance = isStage
                    ? builder.Insert(new OpStageSDSL(context.Bound++)).ResultId
                    : builder.Insert(new OpThisSDSL(context.Bound++)).ResultId;
            }
            result.Id = builder.Insert(new OpMemberAccessSDSL(context.GetOrRegister(thisType), context.Bound++, instance.Value, result.Id));
        }
        if (symbol.AccessChain is int accessChainIndex)
        {
            if (constantOnly)
                throw new NotImplementedException();

            var index = context.CompileConstant(accessChainIndex).Id;
            result.Id = builder.Insert(new OpAccessChain(resultType, context.Bound++, result.Id, [index]));
        }

        return result;
    }

    public override void SetValue(SymbolTable table, CompilerUnit compiler, SpirvValue value)
    {
        var (builder, context) = compiler;

        value = builder.AsValue(context, value);
        var target = CompileSymbol(table, builder, context, false);

        if (Type is not PointerType)
            // Throw exception (default behavior)
            base.SetValue(table, compiler, value);

        value = builder.Convert(context, value, ((PointerType)Type).BaseType);
        builder.Insert(new OpStore(target.Id, value.Id, null, []));
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public bool IsVectorSwizzle()
    {
        if (Name.Length > 4)
            return false;

        bool colorMode = false;
        bool vectorMode = false;

        Span<char> colorFields = ['r', 'g', 'b', 'a'];
        Span<char> vectorFields = ['x', 'y', 'z', 'w'];

        if (colorFields.Contains(Name[0]))
            colorMode = true;
        else if (vectorFields.Contains(Name[0]))
            vectorMode = true;

        if (!colorMode && !vectorMode)
            return false;
        var fields = colorMode ? colorFields : vectorFields;
        foreach (var c in Name)
            if (!fields.Contains(c))
                return false;
        return true;
    }

    public bool IsMatrixField()
    {
        return
            Name.Length == 3
            && Name[0] == '_'
            && char.IsDigit(Name[1]) && Name[1] - '0' > 0 && Name[1] - '0' < 5
            && char.IsDigit(Name[2]) && Name[2] - '0' > 0 && Name[2] - '0' < 5;
    }
}

public class TypeName(string name, TextLocation info) : Literal(info)
{
    public string Name { get; set; } = name;
    public bool IsArray => ArraySize != null && ArraySize.Count > 0;
    public List<Expression>? ArraySize { get; set; }
    public List<TypeName> Generics { get; set; } = [];

    public bool TryResolveType(SymbolTable table, SpirvContext context, [MaybeNullWhen(false)] out SymbolType symbolType)
    {
        if (Name == "LinkType")
        {
            symbolType = new GenericParameterType(Specification.GenericParameterKindSDSL.LinkType);
        }
        else if (Name == "Semantic")
        {
            symbolType = new GenericParameterType(Specification.GenericParameterKindSDSL.Semantic);
        }
        else if (Name == "MemberName")
        {
            symbolType = new GenericParameterType(Specification.GenericParameterKindSDSL.MemberName);
        }
        else if (Name == "MemberNameResolved")
        {
            symbolType = new GenericParameterType(Specification.GenericParameterKindSDSL.MemberNameResolved);
        }
        else if (Name == "Streams")
        {
            symbolType = new StreamsType();
        }
        else
        {
            var fullTypeName = GenerateTypeName(includeGenerics: true, includeArray: false);

            if (table.DeclaredTypes.TryGetValue(fullTypeName, out symbolType))
            {

            }
            else if (SymbolType.TryGetNumeric(Name, out var numeric))
            {
                table.DeclaredTypes.Add(fullTypeName, numeric);
                symbolType = numeric;
            }
            else if (!IsArray && Generics.Count == 0 && SymbolType.TryGetBufferType(Name, null, out var bufferType))
            {
                table.DeclaredTypes.Add(fullTypeName, bufferType);
                symbolType = bufferType;
            }
            else if (Generics.Count == 1 && SymbolType.TryGetBufferType(Name, Generics[0].Name, out var genericBufferType))
            {
                table.DeclaredTypes.Add(fullTypeName, genericBufferType);
                symbolType = genericBufferType;
            }
            else if (Name == "SamplerState" || Name == "SamplerComparisonState")
            {
                symbolType = new SamplerType();
            }
        }

        if (symbolType == null)
            return false;

        if (IsArray)
        {
            var fullTypeName = GenerateTypeName(includeGenerics: true, includeArray: true);

            var arraySymbolType = symbolType;
            if (!table.DeclaredTypes.TryGetValue(fullTypeName, out symbolType))
            {
                if (ArraySize != null)
                    arraySymbolType = GenerateArrayType(table, context, arraySymbolType, ArraySize);

                table.DeclaredTypes.Add(fullTypeName, symbolType = arraySymbolType);
            }
        }

        return true;
    }

    public static SymbolType GenerateArrayType(SymbolTable table, SpirvContext context, SymbolType arraySymbolType, List<Expression> arraySizes)
    {
        foreach (var arraySize in arraySizes)
        {
            if (arraySize is EmptyExpression)
                arraySymbolType = new ArrayType(arraySymbolType, -1);
            else
            {
                var arrayComputedSize = -1;
                if (arraySize is IntegerLiteral i)
                    arrayComputedSize = (int)i.Value;

                var constantArraySize = arraySize.CompileConstantValue(table, context);
                if (context.TryGetConstantValue(constantArraySize.Id, out var value, out _, true))
                    arrayComputedSize = (int)value;
                arraySymbolType = arrayComputedSize != -1
                    ? new ArrayType(arraySymbolType, arrayComputedSize)
                    : new ArrayType(arraySymbolType, arrayComputedSize, (constantArraySize.Id, context.GetBuffer()));
            }
        }

        return arraySymbolType;
    }

    public SymbolType ResolveType(SymbolTable table, SpirvContext context)
    {
        if (!TryResolveType(table, context, out var result))
            throw new InvalidOperationException($"Could not resolve type [{Name}]");
        return result;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        throw new NotImplementedException();
    }

    public override string ToString() => GenerateTypeName(includeGenerics: true, includeArray: true);

    public string GenerateTypeName(bool includeGenerics = true, bool includeArray = true)
    {
        // Fast path
        if ((Generics.Count == 0 || !includeGenerics) && (!includeArray || ArraySize == null))
            return Name;

        var builder = new StringBuilder();
        builder.Append(Name);
        if (includeGenerics && Generics.Count > 0)
        {
            builder.Append('<');
            foreach (var g in Generics)
                builder.Append(g.ToString()).Append(", ");
            builder.Append('>');
        }
        if (includeArray && ArraySize != null)
            foreach (var s in ArraySize)
                builder.Append('[').Append(s.ToString()).Append(']');

        return builder.ToString();
    }

    public static string GetTypeNameWithoutGenerics(string typeName)
    {
        var indexGenerics = typeName.IndexOf('<');
        if (indexGenerics != -1)
            typeName = typeName.Substring(0, indexGenerics);
        return typeName;
    }

    public static implicit operator string(TypeName tn) => tn.Name;
}
