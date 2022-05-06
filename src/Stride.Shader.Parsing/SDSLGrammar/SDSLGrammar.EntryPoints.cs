using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser VSMain = new();
    public AlternativeParser GSMain = new();
    public AlternativeParser PSMain = new();
    public AlternativeParser CSMain = new();
    public AlternativeParser HSMain = new();
    public AlternativeParser HSConstantsMain = new();
    public AlternativeParser DSMain = new();
    public AlternativeParser Entries = new();
    
    

    public SDSLGrammar UsingEntry()
    {
        Inner = Entries;
        return this;
    }
    public void CreateEntryPoints()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);  

        var vs = Literal("VSMain");
        var ps = Literal("PSMain");
        var gs = Literal("GSMain");
        var cs = Literal("CSMain");
        var ds = Literal("DSMain");
        var hs = Literal("HSMain");
        var hsc = Literal("HSConstantsMain");
        var abstractM = Literal("abstract");


        VSMain.Add(
            abstractM.Optional().Then(Void).Then(vs).SeparatedBy(ws1)
            .Then(LeftParen).Then(RightParen)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );
        
        PSMain.Add(
            abstractM.Optional().Then(Void).Then(ps).SeparatedBy(ws1)
            .Then(LeftParen).Then(RightParen)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        GSMain.Add(
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(abstractM.Optional()).Then(Void).Then(gs).SeparatedBy(ws1)
            .Then(ParameterList)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        CSMain.Add(
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(abstractM.Optional()).Then(Void).Then(cs).SeparatedBy(ws1)
            .Then(LeftParen).Then(RightParen)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        HSMain.Add(
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(abstractM.Optional()).Then(Void).Then(hs).SeparatedBy(ws1)
            .Then(ParameterList)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        HSConstantsMain.Add(
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(abstractM.Optional()).Then(Void).Then(hsc).SeparatedBy(ws1)
            .Then(ParameterList)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        DSMain.Add(
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(abstractM.Optional()).Then(Void).Then(ds).SeparatedBy(ws1)
            .Then(ParameterList)
            .Then(LeftBrace)
                .Then(Statement.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace)
            .SeparatedBy(ws)
        );

        Entries.Add(
            VSMain
            | PSMain
        );        
    }
}