
using System.Text;

namespace Stride.Shaders;

 public abstract class ShaderClassCode : ShaderSource
{
    public string ClassName { get; set; }
    public string[] GenericArguments { get; set; }

    public Dictionary<string, string> GenericParametersArguments { get; set; }

    public string ToClassName()
    {
        if (GenericArguments == null)
            return ClassName;

        var result = new StringBuilder();
        result.Append(ClassName);
        if (GenericArguments != null && GenericArguments.Length > 0)
        {
            result.Append('<');
            result.Append(string.Join(",", GenericArguments));
            result.Append('>');
        }

        return result.ToString();
    }
    
    public override string ToString()
    {
        return ToClassName();
    }
}