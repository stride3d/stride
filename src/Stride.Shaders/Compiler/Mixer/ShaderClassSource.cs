using System.Globalization;

namespace Stride.Shaders.Mixer;

public sealed class ShaderClassSource : ShaderClassCode, IEquatable<ShaderClassSource>
{
    public override string ShaderName => throw new NotImplementedException();

    public override IEnumerable<string> MixinNames => throw new NotImplementedException();

    public ShaderClassSource()
    {
    }

    public ShaderClassSource(string className)
        : this(className, null)
    {
    }

    
    public ShaderClassSource(string className, params string[] genericArguments)
    {
        ClassName = className;
        GenericArguments = genericArguments;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <param name="genericArguments">The generic parameters.</param>
    public ShaderClassSource(string className, params object[] genericArguments)
    {
        ClassName = className;
        if (genericArguments != null)
        {
            GenericArguments = new string[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; ++i)
            {
                var genArg = genericArguments[i];
                if (genArg is bool)
                    GenericArguments[i] = ((bool)genArg) ? "true" : "false";
                else
                    GenericArguments[i] = genArg == null ? "null" : Convert.ToString(genArg, CultureInfo.InvariantCulture);
            }
        }
    }

    public bool Equals(ShaderClassSource shaderClassSource)
    {
        if (shaderClassSource is null) return false;
        if (ReferenceEquals(this, shaderClassSource)) return true;
        return string.Equals(ClassName, shaderClassSource.ClassName) 
            && GenericArguments.OrderBy(x => x).SequenceEqual(shaderClassSource.GenericArguments.OrderBy(x => x));
        // Utilities.Compare(GenericArguments, shaderClassSource.GenericArguments);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
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

    public override object Clone()
    {
        return new ShaderClassSource(ClassName, GenericArguments = GenericArguments != null ? GenericArguments.ToArray() : null);
    }

    public override string ToString()
    {
        return ToClassName();
    }

    public override void EnumerateMixins(SortedSet<ShaderSource> shaderSources)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="ShaderClassSource"/>.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator ShaderClassSource(string className)
    {
        return new ShaderClassSource(className);
    }
}