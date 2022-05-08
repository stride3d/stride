using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shader.Parsing
{
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
            var anyComments = WhiteSpace | singleLineComment | blockComment;
            Comments.Add(
                anyComments.Repeat(0)
                .Then(AnyChar.Repeat(0).Until(commentStart).Named("ActualCode"))
                .Repeat(0)
            );
            Inner = Comments;
        }
    }
}