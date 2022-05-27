using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
namespace Stride.Shader.Parsing.Grammars.Directive;
public partial class DirectiveGrammar : Grammar
{
    
    private LiteralTerminal Bool = new();
    private AlternativeParser Uint =  new();
    private LiteralTerminal Int = new();
    private LiteralTerminal Long = new();


    private LiteralTerminal Half = new();
    private LiteralTerminal Float = new();
    private LiteralTerminal Double = new();

    private LiteralTerminal LeftParen = new();
    private LiteralTerminal RightParen = new();
    private LiteralTerminal LeftBracket = new();
    private LiteralTerminal RightBracket = new();
    private LiteralTerminal LeftBrace = new();
    private LiteralTerminal RightBrace = new();

    private LiteralTerminal LeftShift = new();
    private LiteralTerminal RightShift = new();
    private LiteralTerminal Plus = new();
    private LiteralTerminal PlusPlus = new();
    private LiteralTerminal Minus = new();
    private LiteralTerminal MinusMinus = new();
    private LiteralTerminal Star = new();
    private LiteralTerminal Div = new();
    private LiteralTerminal Mod = new();
    private LiteralTerminal And = new();
    private LiteralTerminal Or = new();
    private LiteralTerminal AndAnd = new();
    private LiteralTerminal OrOr = new();
    private LiteralTerminal Caret = new();
    private LiteralTerminal Not = new();
    private LiteralTerminal Tilde = new();
    private LiteralTerminal Equal = new();
    private LiteralTerminal NotEqual = new();
    private LiteralTerminal Less = new();
    private LiteralTerminal LessEqual = new();
    private LiteralTerminal Greater = new();
    private LiteralTerminal GreaterEqual = new();
    private LiteralTerminal Question = new();
    private LiteralTerminal Colon = new();
    private LiteralTerminal ColonColon = new();
    private LiteralTerminal Semi = new();
    private LiteralTerminal Comma = new();
    private LiteralTerminal Assign = new();
    private LiteralTerminal StarAssign = new();
    private LiteralTerminal DivAssign = new();
    private LiteralTerminal ModAssign = new();
    private LiteralTerminal PlusAssign = new();
    private LiteralTerminal MinusAssign = new();
    private LiteralTerminal LeftShiftAssign = new();
    private LiteralTerminal RightShiftAssign = new();
    private LiteralTerminal AndAssign = new();
    private LiteralTerminal XorAssign = new();
    private LiteralTerminal OrAssign = new();

    private LiteralTerminal Dot = new();
    

    public void CreateTokens()
    {
    
        Bool = Literal("bool");
        Uint.Add("uint","unsigned int", "dword");
        Int = Literal("int");
        Long = Literal("long");

        Half = Literal("half");
        Float = Literal("float");
        Double = Literal("double");
        
        LeftParen =  Literal("(");
        RightParen = Literal(")");
        LeftBracket = Literal("[");
        RightBracket = Literal("]");
        LeftBrace = Literal("{");
        RightBrace = Literal("}");

        LeftShift = Literal("<<");
        RightShift = Literal(">>");
        Plus = Literal("+");
        PlusPlus = Literal("++");
        Minus = Literal("-");
        MinusMinus = Literal("--");
        Star = Literal("*");
        Div = Literal("/");
        Mod = Literal("%");
        And = Literal("&");
        Or = Literal("|");
        AndAnd = Literal("&&");
        OrOr = Literal("||");
        Caret = Literal("^");
        Not = Literal("!");
        Tilde = Literal("~");
        Equal = Literal("==");
        NotEqual = Literal("!=");
        Less = Literal("<");
        LessEqual = Literal("<=");
        Greater = Literal(">");
        GreaterEqual = Literal(">=");
        Question = Literal("?");
        Colon = Literal(":");
        ColonColon = Literal("::");
        Semi = Literal(";");
        Comma = Literal(",");
        Assign = Literal("=");
        StarAssign = Literal("*=");
        DivAssign = Literal("/=");
        ModAssign = Literal("%=");
        PlusAssign = Literal("+=");
        MinusAssign = Literal("-=");
        LeftShiftAssign = Literal("<<=");
        RightShiftAssign = Literal(">>=");
        AndAssign = Literal("&=");
        XorAssign = Literal("^=");
        OrAssign = Literal("|=");

        Dot = Literal(".");
        
    }
}
