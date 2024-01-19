using SDSL.Parsing.AST.Shader;
using SoftTouch.Spirv.Core.Buffers;

namespace SDSL.Analysis;


public record struct CFEdge(Expression Condition, CFNode Destination);

public class CFNode()
{
    public List<Statement> Code = [];
    public List<CFEdge> Outputs = [];
}
public class ControlFlowGraph()
{
    public Dictionary<ShaderMethod, ControlFlowGraph> Graphs = [];
}