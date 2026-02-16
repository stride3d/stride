using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;


public record struct ParameterizedOperand(string? Name, OperandKind Kind);
public readonly record struct ParameterizedOperandKey(OperandKind Kind, int Value);

public class OperandParameters : Dictionary<ParameterizedOperandKey, ParameterizedOperand[]>;

/// <summary>
/// Information on SPIR-V instruction operands 
/// </summary>
public readonly partial record struct LogicalOperand(string? Name, OperandKind? Kind, OperandQuantifier? Quantifier, OperandParameters Parameters)
{
    public LogicalOperand(OperandKind? kind, OperandQuantifier? quantifier, string? name = null) : this(name, kind, quantifier, []) { }
    public LogicalOperand(string kind, string quantifier, string? name = null) : this(name, Enum.Parse<OperandKind>(kind), Enum.Parse<OperandQuantifier>(quantifier), []) { }
}