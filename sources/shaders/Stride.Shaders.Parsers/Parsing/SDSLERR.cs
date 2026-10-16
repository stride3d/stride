namespace Stride.Shaders.Parsing;

/// <summary>
/// Error messages for SDSL
/// </summary>
public static class SDSLErrorMessages
{
    // Parse errors
    public const string SDSL0001 = "SDSL0001: Unexpected token";
    public const string SDSL0002 = "SDSL0002: vector size not supported";
    public const string SDSL0003 = "SDSL0003: matrix size not supported";
    public const string SDSL0004 = "SDSL0004: Unfinished vector declaration";
    public const string SDSL0005 = "SDSL0005: Too many values";
    public const string SDSL0006 = "SDSL0006: wrong vector size";
    public const string SDSL0007 = "SDSL0007: Expecting number or vector value";
    public const string SDSL0008 = "SDSL0008: Unfinished vector declaration";
    public const string SDSL0009 = "SDSL0009: Expected <EOF>";
    public const string SDSL0010 = "SDSL0010: Expected statement";
    public const string SDSL0011 = "SDSL0011: Expected statement or end of block";
    public const string SDSL0012 = "SDSL0012: Expected parameter or end of block";
    public const string SDSL0013 = "SDSL0013: Expected semi colon";
    public const string SDSL0014 = "SDSL0014: Expected assignment or semi colon";
    public const string SDSL0015 = "SDSL0015: Expected expression";
    public const string SDSL0016 = "SDSL0016: Expected at least one space";
    public const string SDSL0017 = "SDSL0017: Expected identifier";
    public const string SDSL0018 = "SDSL0018: Expected closing parenthesis";
    public const string SDSL0019 = "SDSL0019: Expected closing bracket";
    public const string SDSL0020 = "SDSL0020: Expected postfix expression";
    public const string SDSL0021 = "SDSL0021: Expected accessor expression";
    public const string SDSL0022 = "SDSL0022: Expected And expression";
    public const string SDSL0023 = "SDSL0023: Expected Or expression";
    public const string SDSL0024 = "SDSL0024: Expected BitWiseOr expression";
    public const string SDSL0025 = "SDSL0025: Expected BitWiseXor expression";
    public const string SDSL0026 = "SDSL0026: Expected BitWiseAnd expression";
    public const string SDSL0027 = "SDSL0027: Expected equality expression";
    public const string SDSL0028 = "SDSL0028: Expected relational expression";
    public const string SDSL0029 = "SDSL0029: Expected shift expression";
    public const string SDSL0030 = "SDSL0030: Expected additive expression";
    public const string SDSL0031 = "SDSL0031: Expected multiplicative expression";
    public const string SDSL0032 = "SDSL0032: Expected variable name";
    public const string SDSL0033 = "SDSL0033: Expected semi colon";
    public const string SDSL0034 = "SDSL0034: Expected closing chevron";
    public const string SDSL0035 = "SDSL0035: Expected open parenthesis";
    public const string SDSL0036 = "SDSL0036: Expected initializer expression";
    public const string SDSL0037 = "SDSL0037: Expected condition expression";
    public const string SDSL0038 = "SDSL0038: Expected increment expression";
    public const string SDSL0039 = "SDSL0039: Expected shader class or effect";
    public const string SDSL0040 = "SDSL0040: Expected body declaration";
    public const string SDSL0041 = "SDSL0041: Expected expression or semi colon";
    public const string SDSL0042 = "SDSL0042: Expected prefix expression";
    public const string SDSL0043 = "SDSL0043: Unexpected <EOF>";
    public const string SDSL0048 = "SDSL0048: '{0}' is a reserved word and can't be a name. Rename it";
    public const string SDSL0049 = "SDSL0049: Code after the end of shader {0}. Only shader, effect and params declarations can follow: remove it, or move it into the shader";
    public const string SDSL0050 = "SDSL0050: {0} {1} is declared inside {2} {3}, but it is not a member of a {2}. Move it out of the block";


    // Semantic errors

    public const string SDSL0100 = "SDSL0100: Variable is not declared";
    public const string SDSL0101 = "SDSL0101: Variable is already declared";
    public const string SDSL0102 = "SDSL0102: Variable is not a constant";
    public const string SDSL0103 = "SDSL0103: Variable cannot be assigned to";
    public const string SDSL0104 = "SDSL0104: Cannot infer type";
    public const string SDSL0105 = "SDSL0105: Unrecognized node";
    public const string SDSL0106 = "SDSL0106: Unsupported type";
    public const string SDSL0107 = "SDSL0107: Binary expression between vector and matrix is not implemented";
    public const string SDSL0108 = "SDSL0108: Couldn't figure out type for binary operation between {0} and {1}";
    public const string SDSL0109 = "SDSL0109: Could not resolve method {0} in type {1}";
    public const string SDSL0110 = "SDSL0110: Use of undeclared identifier '{0}'";
    public const string SDSL0111 = "SDSL0111: Unimplemented: {0}";
    public const string SDSL0112 = "SDSL0112: Could not resolve member {0} in expression {1} of type {2}";
    public const string SDSL0113 = "SDSL0113: Could not resolve member {0} in structure of type {1}";
    public const string SDSL0114 = "SDSL0114: {0}.{1}({2}) at line {3} implements abstract method {4}.{1} but is not marked 'override'; it is compiled as an override. Add the 'override' keyword.";
    public const string SDSL0115 = "SDSL0115: {0}.{1}({2}) at line {3} hides inherited method {4}.{1} but is not marked 'override'. Add the 'override' keyword, or rename the method.";
    public const string SDSL0145 = "SDSL0145: Swizzle {0} writes the same element twice. Write each element once.";
    public const string SDSL0160 = "SDSL0160: {0} takes {1}, but the call gives {2}.";
    public const string SDSL0161 = "SDSL0161: Argument {0} of the call to {1} has type {2}, which can't be converted to {3}.";
    public const string SDSL0162 = "SDSL0162: No overload of {0} can take these arguments:{1}";
    public const string SDSL0163 = "SDSL0163: The call {0} is ambiguous: these overloads match equally well:{1}\nCast the arguments to the parameter types of the one to call.";
    public const string SDSL0170 = "SDSL0170: [StreamOutput] on {0} is not supported: Stride shaders have no geometry shader stream output. Remove the attribute; to keep generated vertices, write them to a RWStructuredBuffer from a compute shader instead.";
    public const string SDSL0171 = "SDSL0171: Attribute [{0}] on method {1} is not supported. Remove it.";
    public const string SDSL0172 = "SDSL0172: Stream {0} has an initial value ({1}), but a stream has none. Remove it, and assign the value in the stage that writes the stream first (streams.{0} = {1};).";
    public const string SDSL0173 = "SDSL0173: The initial value of member {0} ({1}) is not a constant. A member's initial value is its default value, set by the application, so it must be a constant. To compute it from other members, declare it static (static {2} {0} = {1};), or compute it where it is used.";
    public const string SDSL0174 = "SDSL0174: cbuffer {0} mixes stage and non-stage members ({1} is stage, {2} is not). Split it: declare the stage members in a second cbuffer {0} block (blocks with the same name are merged into one cbuffer).";
    public const string SDSL0175 = "SDSL0175: {0}.{1}({2}) returns {3}, but the method it overrides, {4}.{1}({2}), returns {5}. Use the same return type.";
    public const string SDSL0176 = "SDSL0176: {0}.{1}({2}) is marked 'override', but no inherited method has this name and these parameters. Inherited: {3}. Use the parameters of the method to override, or remove 'override'.";
    public const string SDSL0177 = "SDSL0177: Not all code paths of {0}.{1} return a value. Add a return at the end of the method (a switch needs a default that returns).";
    public const string SDSL0164 = "SDSL0164: Argument {0} of the call to {1} is written back (out or inout parameter), but {2} can't be written to. Pass a variable, a member or an array element; for a swizzle, pass a temporary variable and copy it after the call.";
    public const string SDSL0165 = "SDSL0165: composition {0} is used as a value, but a composition is a shader. Call its method instead, for example {1}.";
    public const string SDSL0166 = "SDSL0166: {0} calls abstract method {1}, but no shader of the effect overrides it. Mix in a shader that implements {1}.";
    public const string SDSL0156 = "SDSL0156: {0} needs {1} values, but the initializer list has {2}. As in HLSL, braces only group: the values fill {0} in order.";
    public const string SDSL0157 = "SDSL0157: a {0} can't be in an initializer list. Only numbers, vectors, matrices, and structs and arrays of them can.";
    public const string SDSL0155 = "SDSL0155: {0} (a matrix with one row or one column) is not supported yet. Use {1}.";

    // HLSL warnings: valid HLSL that is probably a mistake, reported when a class compiles
    public const string SDSL0219 = "SDSL0219: comma expression ({0}): the values before the last are dropped, the result is {1}. To build a vector, write its type: float4(...).";
    public const string SDSL0220 = "SDSL0220: {0} on {1} is ignored: Stride lays out cbuffers and binds resources itself. Remove it.";

    // SDSL warnings
    public const string SDSL0236 = "SDSL0236: the value of composition {0} is ignored: a composition gets its shader from the effect, not from a value. Remove '= {1}'.";
    public const string SDSL0232 = "SDSL0232: {0} has lower precedence than {1}: {2} computes {3} first. Add parentheses to make it clear.";
}
