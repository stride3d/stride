using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;
using System.Collections.Immutable;
using System.Reflection;

internal class SymbolInfo
{
    public bool IsInterface { get; internal set; }
    public string DataMemberMode { get; internal set; }
    internal virtual bool IsEmpty { get; } = false;
    internal string Name { get; set; }
    internal string Type { get; set; }
    internal string Namespace { get; set; }
    internal SymbolKind TypeKind { get; set; }
    internal IContentModeInfo MemberGenerator { get; set; }
    internal bool IsGeneric { get; set; }
    internal bool IsAbstract { get; set; }
    internal string Tag {  get => $$"""emitter.Tag($"!{typeof({{Name}})},{{Namespace}}")""";}
    internal DataMemberContext Context { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is SymbolInfo info &&
               IsInterface == info.IsInterface &&
               DataMemberMode == info.DataMemberMode &&
               IsEmpty == info.IsEmpty &&
               Name == info.Name &&
               Type == info.Type &&
               Namespace == info.Namespace &&
               TypeKind == info.TypeKind &&
               EqualityComparer<IContentModeInfo>.Default.Equals(MemberGenerator, info.MemberGenerator) &&
               IsGeneric == info.IsGeneric &&
               IsAbstract == info.IsAbstract &&
               Tag == info.Tag &&
               EqualityComparer<DataMemberContext>.Default.Equals(Context, info.Context);
    }

    public override int GetHashCode()
    {
        var hashCode = 164138036;
        hashCode = hashCode * -1521134295 + IsInterface.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataMemberMode);
        hashCode = hashCode * -1521134295 + IsEmpty.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
        hashCode = hashCode * -1521134295 + TypeKind.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<IContentModeInfo>.Default.GetHashCode(MemberGenerator);
        hashCode = hashCode * -1521134295 + IsGeneric.GetHashCode();
        hashCode = hashCode * -1521134295 + IsAbstract.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Tag);
        hashCode = hashCode * -1521134295 + EqualityComparer<DataMemberContext>.Default.GetHashCode(Context);
        return hashCode;
    }
}
