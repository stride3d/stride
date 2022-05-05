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
            var singleLineComment = Literal("//").Then(AnyChar.Repeat(0).Until(Eol,false,true)).WithName("LineComment"); 
		    var blockComment = Literal("/*").Then(AnyChar.Repeat(0).Until("*/",false,true)).WithName("BlockComment"); 
            var anyComments = singleLineComment | blockComment;
            Comments.Add(
                anyComments.Repeat(0).SeparatedBy(WhiteSpace.Repeat(0)).Named("Comments")
                .Then(AnyChar.Repeat(0).Until(commentStart).Named("ActualCode"))
                .Repeat(0)
            );
            Inner = Comments;
        }
    }
}