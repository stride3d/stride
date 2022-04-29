using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    private CharTerminal Hash = Set("#");

    private SequenceParser HashIf = new();
    private SequenceParser HashIfDef = new();
    private SequenceParser HashIfNDef = new();
    
    private SequenceParser HashElse = new();
    private SequenceParser HashElif = new();
    private SequenceParser HashEndIf = new();


    //TODO: Identifier Parser doesn't work here ?
    public SequenceParser IfDefDirective = new();
    public SequenceParser IfNDefDirective = new();

    public SDSLGrammar UsingIfDefDirective()
    {
        Inner = IfDefDirective;
        return this;
    }
    public void CreateDirectives()
    {

        HashIf = Hash.Then("if").WithName("HashIf");
        HashIfDef = Hash.Then("ifdef").WithName("HashIfDef");
        HashIfNDef = Hash.Then("ifndef").WithName("HashIfNDef");
        
        HashElse = Hash.Then("else").WithName("HashElse");
        HashElif = Hash.Then("elif").WithName("HashElif");
        HashEndIf = Hash.Then("endif").WithName("HashEndIf");

        IfDefDirective = HashIfDef.WithName("directive").Then(SingleLineWhiteSpace).Then(Identifier).WithName("IfDef");
        IfNDefDirective = HashIfNDef.WithName("directive").Then(SingleLineWhiteSpace).Then(Identifier).WithName("IfNDef");

    }
    
}