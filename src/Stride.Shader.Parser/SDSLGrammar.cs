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

        var incr_op = 
            Set("++")
            | "--";
        
            
        var const_exp = new AlternativeParser{Name = "Constant"};
        var cond_exp = new AlternativeParser{Name = "cond_exp"};
        var lor_exp = new AlternativeParser{Name = "lor_exp"};
        var land_exp = new AlternativeParser{Name = "land_exp"};
        var or_exp = new AlternativeParser{Name = "or_exp"};
        var xor_exp = new AlternativeParser{Name = "xor_exp"};
        var and_exp = new AlternativeParser{Name = "and_exp"};
        var eq_exp = new AlternativeParser{Name = "eq_exp"};
        var rel_exp = new AlternativeParser{Name = "rel_exp"};
        var shift_exp = new AlternativeParser{Name = "shift_exp"};
        var add_exp = new AlternativeParser{Name = "add_exp"};
        var mul_exp = new AlternativeParser{Name = "mul_exp"};
        var cast_exp = new AlternativeParser{Name = "cast_exp"};
        var unary_exp = new AlternativeParser{Name = "unary_exp"};
        var postfix_exp = new AlternativeParser{Name = "postfix_exp"};
        var assign_exp = new AlternativeParser{Name = "assign_exp"};

        var expr = new SequenceParser();

        cond_exp.Add(
            lor_exp.Then(expr.Then(":").Then(cond_exp).Optional())
        );

        lor_exp.Add(land_exp); 
        lor_exp.Add(lor_exp.Then("||").Then(land_exp));

        land_exp.Add(or_exp); 
        land_exp.Add(land_exp.Then("&&").Then(or_exp));

        or_exp.Add(xor_exp); 
        or_exp.Add(or_exp.Then("|").Then(xor_exp));

        xor_exp.Add(and_exp); 
        xor_exp.Add(xor_exp.Then("|").Then(and_exp));

        and_exp.Add(eq_exp); 
        and_exp.Add(and_exp.Then("|").Then(eq_exp));

        eq_exp.Add(rel_exp);
        eq_exp.Add(eq_exp.Then("==").Or("!=").Then(rel_exp));

        rel_exp.Add(shift_exp);
        rel_exp.Add(rel_exp, Set("<") | ">" | "<=" | ">=", shift_exp);
        
        
        shift_exp.Add(add_exp.WithName("SingleTerm"));
        shift_exp.Add(shift_exp.WithName("LeftTerm").Then("<<").Or(">>").Then(add_exp).WithName("RightTerm"));

        add_exp.Add(mul_exp.WithName("SingleTerm"));
        add_exp.Add(add_exp.WithName("LeftTerm").Then("+").Or("-").Then(mul_exp.WithName("RightTerm")));

        mul_exp.Add(cast_exp);
        mul_exp.Add(mul_exp.Then("*").Or("/").Or("%").Then(cast_exp));

        cast_exp.Add(unary_exp);
        cast_exp.Add(Set("(").Then(identifier).Then(cast_exp));

        unary_exp.Add(postfix_exp);
        unary_exp.Add(Set("++") |"--" , unary_exp);
        unary_exp.Add(unary_op, cast_exp);

        // postfix_exp.Add(primary_expr.WithName("PrimaryPostFix"));
        var pp = Set("+").Or("-").Repeat(2,2);
        var access = Set("[") & primary_expr & "]";

        var p1 = primary_expr;
        // var p2 = p1 | p1.
        // postfix_exp.Add(
        //     primary_expr
        //     | tt.Then(access.Optional())
        //     | tt.Then(pp.Optional().Named("Increment")).Optional());

        // postfix_exp.Add(postfix_exp.Named("Expression"), access.Optional().Named("Accessor"));
            // | postfix_exp.Then("(").Then(assign_exp).Then(")")
            // | postfix_exp.Then(".").Then(identifier)
        

        expr.Add(assign_exp);
        expr.Add(expr.Then(",").Then(assign_exp));

        assign_exp.Add(cond_exp);
        assign_exp.Add(unary_exp.Then(assign_op).Then(assign_exp));



        
        var a = 0;
        var b = ++a+3*1;
        
        
        
        
            

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

        var parenthesis_expr = new AlternativeParser();
        parenthesis_expr.Add(primary_expr);
        parenthesis_expr.Add(Set("(").Then(parenthesis_expr).Then(")"));
        // parenthesis_expr.WithName("ParenthesisExpression");
        // parenthesis_expr.SeparateChildrenBy(ws);
        // parenthesis_expr.AddError = true;
    
        
        // tmp.PreventRecursion(true);
        Inner = postfix_exp;

    }
}
