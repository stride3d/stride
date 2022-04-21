using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public class SDSLGrammar : Grammar
{
    public SDSLGrammar() : base("sdsl")
    {
        EnableMatchEvents = false;
        CaseSensitive = true;

        var ws = new RepeatCharTerminal(char.IsWhiteSpace);

        var eof = End;

        var eol = Eol;
        var eolo = eol.Optional();
        
        var shaderDeclaration = Set("shader");
        
        var ldu = LetterOrDigit | "_";
        var identifier = LetterOrDigit.Then(ldu.Repeat().Optional()).WithName("Identifier");
        
        var lbr = Set("{");
        var rbr = Set("}");

        var floatParser = new NumberParser{AllowSign = true, AllowDecimal = true, AllowExponent = true,ValueType = typeof(double), Name = "ConstantDouble", AddMatch = true, AddError = true};
        var intParser = new NumberParser{AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "ConstantInteger", AddMatch = true, AddError = true};
        var boolParser = new BooleanTerminal{CaseSensitive = true, TrueValues = new string[]{"true"}, FalseValues = new string[]{"false"}, AddError = true, AddMatch = true, Name = "ConstantBoolean"};

        var constants = floatParser.Or(intParser).Or(boolParser).WithName("Constant"); 

        //Step 1 parse addition

        var primary_expr = identifier.Or(constants).WithName("PrimaryExpression");

        var assign_op = 
            Set("=")
            | "*="
            | "/="
            | "%="
            | "+="
            | "-="
            | "<<="
            | ">>="
            | "&="
            | "^="
            | "|=";
        
        var unary_op =
            Set("&")
            | "*"
            | "+"
            | "-"
            | "~"
            | "!";
            
        var const_exp = new SequenceParser();
        var cond_exp = new SequenceParser();
        var lor_exp = new SequenceParser();
        var land_exp = new SequenceParser();
        var or_exp = new SequenceParser();
        var xor_exp = new SequenceParser();
        var and_exp = new SequenceParser();
        var eq_exp = new SequenceParser();
        var rel_exp = new SequenceParser();
        var shift_exp = new SequenceParser();
        var add_exp = new SequenceParser();
        var mul_exp = new SequenceParser();
        var cast_exp = new SequenceParser();
        var unary_exp = new SequenceParser();
        var postfix_exp = new SequenceParser();
        var expr = new SequenceParser();

        cond_exp.Add(
            lor_exp.Then(expr.Then(":").Then(cond_exp).Optional())
        );

        lor_exp.Add(
            land_exp 
            | lor_exp.Then("||").Then(land_exp)
        );

        land_exp.Add(
            or_exp 
            | land_exp.Then("&&").Then(or_exp)
        );
        or_exp.Add(
            xor_exp 
            | or_exp.Then("|").Then(xor_exp)
        );

        xor_exp.Add(
            and_exp 
            | xor_exp.Then("|").Then(and_exp)
        );

        and_exp.Add(
            eq_exp 
            | and_exp.Then("|").Then(eq_exp)
        );

        eq_exp.Add(
            rel_exp
            |   eq_exp.Then("==").Or("!=").Then(rel_exp)
        );

        rel_exp.Add(
            shift_exp
            | rel_exp.Then(
                    Set("<") 
                    | ">"
                    | "<="
                    | ">="
                )
                .Then(shift_exp)
        );


        
        
        
        
        
        
            

        // var assign = identifier.Then(Set("=")).Then(primary_expr).WithName("AssignExpression");
        // assign.SeparateChildrenBy(ws);

        


        // var mul_expr = new SequenceParser();
        
        // mul_expr.Add(primary_expr.WithName("MultLeft").Then(Set("*").Or("/").Or("%").Then(mul_expr.WithName("MultRight")).Optional()));
        // mul_expr.WithName("MultExpression");
        // mul_expr.SeparateChildrenBy(ws);
        // mul_expr.AddError = true;


        // var add_expr = new SequenceParser();

        // add_expr.Add(mul_expr.WithName("AddLeft").Then(Set("+").Or("-").Then(add_expr.WithName("AddRight")).Optional()));
        // add_expr.WithName("AddExpression");
        // add_expr.SeparateChildrenBy(ws);
        // add_expr.AddError = true;

        // var parenthesis_expr = new SequenceParser();
        // parenthesis_expr.Add(
        //     add_expr
        //     .Or(
        //         Set("(").Then(parenthesis_expr).Then(")")
        //     )
        // );
        // parenthesis_expr.WithName("ParenthesisExpression");
        // parenthesis_expr.SeparateChildrenBy(ws);
        // parenthesis_expr.AddError = true;

        
        
        Inner = add_exp;

    }
}
