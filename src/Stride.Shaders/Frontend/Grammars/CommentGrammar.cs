using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shaders.Parsing.Grammars.Comments;

public class CommentGrammar : Grammar
{
    public SequenceParser Comments = new();
        
    public CommentGrammar() : base("comments-sdsl")
    {
        var commentStart = 
            Literal("//")
            | Literal("/*");
        var singleLineComment = Literal("//").Then(AnyChar.Repeat(0).Until(Eol,false,true)).WithName("Comment"); 
		var blockComment = Literal("/*").Then(AnyChar.Repeat(0).Until("*/",false,true)).WithName("Comment"); 
        var anyComments = singleLineComment | blockComment;
        var actualCode = AnyChar.Repeat(0).Until(End | "//" | "/*" ).Named("ActualCode");
        Comments.Add(
            (anyComments | actualCode).Repeat(0).Until(End)
        );
        Inner = Comments;
    }
}
