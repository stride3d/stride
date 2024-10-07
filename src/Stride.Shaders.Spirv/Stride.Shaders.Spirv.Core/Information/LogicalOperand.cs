using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core;

public readonly partial struct LogicalOperand
{
    public string? Name { get; init; }
    public string? SpvClass { get; init; }
    public OperandKind? Kind { get; init; }
    public OperandQuantifier? Quantifier { get; init; }

    public LogicalOperand() { }

    public LogicalOperand(OperandKind? kind, OperandQuantifier? quantifier, string? name = null, string? spvClass = null)
    {
        Name = name;
        Kind = kind;
        SpvClass = spvClass;
        Quantifier = quantifier;
    }
    public LogicalOperand(string kind, string quantifier, string? name = null, string? spvClass = null)
    {
        Name = name;
        Kind = Enum.Parse<OperandKind>(kind);
        SpvClass = spvClass;
        Quantifier = Enum.Parse<OperandQuantifier>(quantifier);
    }
}
