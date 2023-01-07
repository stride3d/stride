using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace SDSL.Parsing.Grammars.Directive;
public partial class DirectiveGrammar : Grammar
{
    public AlternativeParser IncOperators = new();

    public AlternativeParser Operators = new();  
    public AlternativeParser AssignOperators = new();

    public AlternativeParser ValueTypes = new();
    
    public void CreateTokenGroups()
    {
        IncOperators.Add(
            PlusPlus,
            MinusMinus
        );

        Operators.Add(
            PlusPlus,
            Plus,
            MinusMinus,
            Minus,
            Star,
            Div,
            Mod,
            LeftShift,
            RightShift,
            AndAnd,
            And,
            OrOr,
            Or,
            "^",
            Equal,
            "==",
            NotEqual,
            Question

        );
        
        AssignOperators.Add(
            Assign,
            StarAssign,
            DivAssign,
            ModAssign,
            PlusAssign,
            MinusAssign,
            LeftShiftAssign,
            RightShiftAssign,
            AndAssign,
            XorAssign,
            OrAssign
        );


        ValueTypes.Add(
            Bool,
            Half,
            Float,
            Double,
            Int,
            Uint,
            Long
        );        
    }
}