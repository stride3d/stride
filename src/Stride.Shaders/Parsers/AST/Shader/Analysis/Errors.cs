namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

using Eto.Parse;


public class ErrorInfo 
{
    public Match Match {get;set;}
    public string Message {get;set;}

    public ErrorInfo(Match mtc, string msg)
    {
        Match = mtc;
        Message = msg;
    }
}

public class ErrorList : List<ErrorInfo>
{
    
}