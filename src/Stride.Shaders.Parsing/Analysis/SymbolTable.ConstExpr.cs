using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.Analysis;

public partial class SymbolTable
{
    public bool IsConstantExpression(Expression expression)
    {
        if(expression is TernaryExpression tern)
            return IsConstantExpression(tern.Condition) && IsConstantExpression(tern.Left) && IsConstantExpression(tern.Right);
        else if(expression is BinaryExpression bin)
            return IsConstantExpression(bin.Left) && IsConstantExpression(bin.Right);
        else if(expression is Identifier identifier)
            return TryFind(identifier, SymbolKind.Constant, out _);
        else if(expression is NumberLiteral || expression is BoolLiteral)
            return true;
        else return false;
    }

    // public bool TryFold(Expression expression, out Expression result)
    // {
    //     if(expression is TernaryExpression tern)
    //     {
    //         if(TryFold(tern.Condition, out var cond))
    //             tern.Condition = cond;
    //         if(TryFold(tern.Left, out var left))
    //             tern.Left = left;
    //         if(TryFold(tern.Right, out var right))
    //             tern.Right = right;
    //         result = tern;
    //         return true;
    //     }
    //     else if(expression is BinaryExpression bexp)
    //     {
    //         if(bexp.Left is not NumberLiteral || bexp.Left is not BoolLiteral)
    //             if(TryFold(bexp.Left, out var bleft))
    //                 bexp.Left = bleft;
    //         if(bexp.Right is not NumberLiteral || bexp.Right is not BoolLiteral)
    //             if(TryFold(bexp.Right, out var bright))
    //                 bexp.Right = bright;
    //         result = (bexp.Left, bexp.Op, bexp.Right) switch
    //         {
    //             (BoolLiteral l, Operator.LogicalAND, BoolLiteral r) => new BoolLiteral(false, bexp.Info),
    //             (BoolLiteral l, Operator.LogicalOR, BoolLiteral r) => new BoolLiteral(false, bexp.Info),
    //             (IntegerLiteral l, Operator.Plus, IntegerLiteral r) => new IntegerLiteral(l.Suffix, l.Value + r.Value, bexp.Info),
    //             (IntegerLiteral l, Operator.Plus, FloatLiteral r) => new IntegerLiteral(l.Suffix, (long)(l.Value + r.Value), bexp.Info),
    //             (FloatLiteral l, Operator.Plus, IntegerLiteral r) => new IntegerLiteral(l.Suffix, (long)(l.Value + r.Value), bexp.Info),
    //             (FloatLiteral l, Operator.Plus, FloatLiteral r) => new IntegerLiteral(l.Suffix, (long)(l.Value + r.Value), bexp.Info),
    //             _ => bexp
    //         };
    //         return true;
    //     }
    //     else if(expression is Identifier identifier)
    //     {
    //         if(TryFind(identifier, SymbolKind.Constant, out var symbol))
    //         {
    //             if(symbol.DefaultValue is null)
    //                 throw new NotImplementedException();
    //             result = null!;
    //             return true;
    //         }
    //     }
    //     else
    //     {
    //         result = expression;
    //         return false;
    //     }
    //     result = null!;
    //     return false;
    // }
}