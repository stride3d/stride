using System.Text;

namespace Stride.Shaders.Compilers.SDSL;

public abstract class ShaderSource
{

}

public sealed class ShaderClassSource : ShaderSource
{
    /// <summary>
    /// Gets the name of the class.
    /// </summary>
    /// <value>The name of the class.</value>
    public string ClassName { get; set; }

    /// <summary>
    /// Gets the generic parameters.
    /// </summary>
    /// <value>The generic parameters.</value>
    public string[] GenericArguments { get; set; }

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

public sealed class ShaderMixinSource : ShaderSource
{
    public List<ShaderClassSource> Mixins { get; } = new();

    public Dictionary<string, ShaderMixinSource> Compositions { get; } = new();

    public override string ToString()
    {
        var result = new StringBuilder();

        result.Append("mixin");

        if (Mixins != null && Mixins.Count > 0)
        {
            result.Append(" ");
            for (int i = 0; i < Mixins.Count; i++)
            {
                if (i > 0)
                    result.Append(", ");
                result.Append(Mixins[i]);
            }
        }

        if (Compositions != null && Compositions.Count > 0)
        {
            result.Append(" [");
            var keys = Compositions.Keys.ToList();
            keys.Sort();
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (i > 0)
                    result.Append(", ");
                result.AppendFormat("{{{0} = {1}}}", key, Compositions[key]);
            }
            result.Append("]");
        }
        return result.ToString();
    }
}