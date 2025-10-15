using System.Text;

namespace Stride.Shaders.Parsing.SDSL;

public abstract class ShaderSource
{

}

public sealed class ShaderClassSource(string className) : ShaderSource, IEquatable<ShaderClassSource>
{
    /// <summary>
    /// Gets the name of the class.
    /// </summary>
    /// <value>The name of the class.</value>
    public string ClassName { get; set; } = className;

    /// <summary>
    /// Gets the generic parameters.
    /// </summary>
    /// <value>The generic parameters.</value>
    public string[] GenericArguments { get; set; } = [];

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

    public bool Equals(ShaderClassSource shaderClassSource)
    {
        if (ReferenceEquals(null, shaderClassSource)) return false;
        if (ReferenceEquals(this, shaderClassSource)) return true;
        return
            string.Equals(ClassName, shaderClassSource.ClassName) &&
            GenericArguments.SequenceEqual(shaderClassSource.GenericArguments);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ShaderClassSource)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = ClassName?.GetHashCode() ?? 0;
            if (GenericArguments != null)
            {
                foreach (var current in GenericArguments)
                    hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
            }

            return hashCode;
        }
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