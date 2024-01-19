namespace SDSL.Parsing.AST.Shader;


public class Goto : Statement
{
    public Label Label { get; set; }
}

public class Label : Statement
{
    public string Name { get; set; }
}

public class IfGoto : Statement
{
    public Expression Condition { get; set; }
    public Label True { get; set; }
    public Label False { get; set; }
}