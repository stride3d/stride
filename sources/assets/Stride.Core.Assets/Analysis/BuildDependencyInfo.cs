
using Stride.Core.Assets.Compiler;

namespace Stride.Core.Assets.Analysis;

/// <summary>
/// A structure representing information related to a build dependency between one source assets and a target type of asset.
/// </summary>
public readonly struct BuildDependencyInfo : IEquatable<BuildDependencyInfo>
{
    /// <summary>
    /// The compilation context in which to compile the target asset.
    /// </summary>
    /// <remarks>This context is not relevant if the asset is not compiled, like when <see cref="DependencyType"/> is <see cref="BuildDependencyType.CompileAsset"/></remarks>
    public readonly Type CompilationContext;
    /// <summary>
    /// The type of asset targeted by this dependency.
    /// </summary>
    public readonly Type AssetType;
    /// <summary>
    /// The type of dependency, indicating whether the target asset must actually be compiled, and whether it should be compiled before the referecing asset or can be at the same time.
    /// </summary>
    public readonly BuildDependencyType DependencyType;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDependencyInfo"/> structure.
    /// </summary>
    /// <param name="assetType">The type of asset targeted by this dependency info.</param>
    /// <param name="compilationContext">The compilation context in which to compile the target asset.</param>
    /// <param name="dependencyType">The type of dependency.</param>
    public BuildDependencyInfo(Type assetType, Type compilationContext, BuildDependencyType dependencyType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType)) throw new ArgumentException($@"{nameof(assetType)} should inherit from Asset", nameof(assetType));
        if (!typeof(ICompilationContext).IsAssignableFrom(compilationContext)) throw new ArgumentException($@"{nameof(compilationContext)} should inherit from ICompilationContext", nameof(compilationContext));
        AssetType = assetType;
        CompilationContext = compilationContext;
        DependencyType = dependencyType;
    }

    /// <inheritdoc/>
    public readonly bool Equals(BuildDependencyInfo other)
    {
        return ReferenceEquals(CompilationContext, other.CompilationContext) && ReferenceEquals(AssetType, other.AssetType) && DependencyType == other.DependencyType;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        return obj is BuildDependencyInfo info && Equals(info);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(CompilationContext, AssetType, DependencyType);
    }

    /// <inheritdoc/>
    public static bool operator ==(BuildDependencyInfo left, BuildDependencyInfo right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(BuildDependencyInfo left, BuildDependencyInfo right)
    {
        return !left.Equals(right);
    }
}
