namespace Stride.Shaders.Parsing.SDSL.AST;

public abstract record DataType();
public record Scalar() : DataType(), IGenericValue;
public record Vector(int Size) : DataType(), IGenericValue;
public record Matrix(int Rows, int Columns) : DataType(), IGenericValue;
public record Array(int Size) : DataType();
public record Struct(Dictionary<string, DataType> Fields) : DataType();
public record Buffer(DataType BaseType) : DataType(), IGenericValue;
public record Texture(DataType BaseType) : DataType(), IGenericValue;