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
            var singleLineComment = Literal("//").Then(AnyChar.Repeat(0).Until(Eol)).WithName("LineComment"); 
		    var blockComment = Literal("/*").Then(AnyChar.Repeat(0).Until("*/",false,true)).WithName("BlockComment"); 

            Comments.Add(
                AnyChar.Repeat(0).Until(
                    (singleLineComment| blockComment).Repeat(0),
                    false,
                    false)
                .Repeat(0)
            );
            Inner = Comments;
        }
    }
}