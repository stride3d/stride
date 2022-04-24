namespace Stride.Shader.Parser;

using Eto.Parse;
using Eto.Parse.Grammars;
using System.Text;
public static class StrideGrammar
{
    public static Grammar New()
    {
        return new EbnfGrammar(EbnfStyle.W3c).Build(Encoding.UTF8.GetString(GrammarResource.grammar),"shader");
    }
}   