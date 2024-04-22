using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Core;
using System.Runtime.Serialization;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal class DataMemberContext
{
    internal static DataMemberContext Create(ISymbol symbol, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol DataMemberMode, INamedTypeSymbol dataMemberIgnoreAttribute)
    {
        var context = new DataMemberContext();
        if (symbol.TryGetAttribute(dataMemberAttribute, out var attributeData))
        {
            context.Exists = true;
            context.Mode = GetDataMemberMode(attributeData, DataMemberMode);
            context.Order = 0;
        }
        else
        {
            context.Exists = false;
        }

        return context;
    }
    internal string MemberMode { get; private set; }
    internal void CreateMemberMode(int mode)
    {
        var x = mode switch
        {
            0 => "Default",
            1 => "Assign",
            2 => "Content",
            3 => "Binary",
            4 => "Never",
            _ => "Assign"
        };
        MemberMode = "StrideCoreAlias.DataMemberMode." + x;

    }
    static int GetDataMemberMode(AttributeData attributeData, INamedTypeSymbol dataMemberAttribute)
    {
        var modeParameter = attributeData.ConstructorArguments.FirstOrDefault(x => x.Type?.Equals(dataMemberAttribute, SymbolEqualityComparer.Default) ?? false);

        if (modeParameter.Value is null)
            return 0;
        return (int)modeParameter.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is DataMemberContext context &&
               MemberMode == context.MemberMode &&
               Exists == context.Exists &&
               Mode == context.Mode &&
               Order == context.Order;
    }

    public override int GetHashCode()
    {
        var hashCode = 43144658;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MemberMode);
        hashCode = hashCode * -1521134295 + Exists.GetHashCode();
        hashCode = hashCode * -1521134295 + Mode.GetHashCode();
        hashCode = hashCode * -1521134295 + Order.GetHashCode();
        return hashCode;
    }

    public bool Exists { get; set; }
    public int Mode { get; set; }
    public int Order { get; set; }
}
