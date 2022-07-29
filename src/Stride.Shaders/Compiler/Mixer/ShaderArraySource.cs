using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Mixer;

public class ShaderArraySource : ShaderSource, IEnumerable<ShaderSource>, IEquatable<ShaderArraySource>
{

    public ShaderArraySource()
    {
        Values = new();
    }
    public ShaderArraySource(IEnumerable<ShaderSource> values)
    {
        Values = new ShaderSourceCollection(values);
    }

    public ShaderSourceCollection Values {get;set;}

    public void Add(ShaderSource shader)
    {
        Values.Add(shader);
    }
    public void Add(string shader)
    {
        Values.Add(new ShaderClassString(shader));
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object against)
    {
        throw new NotImplementedException();
    }

    public bool Equals(ShaderArraySource? other)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<ShaderSource> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
