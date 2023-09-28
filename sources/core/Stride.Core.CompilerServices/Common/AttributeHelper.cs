using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.CompilerServices.Common;
public static class AttributeHelper
{
    public static bool ShouldBeIgnored(Compilation compilation,ISymbol symbol,SerializationContext context)
    {
        if(symbol == null)
            return true;
        var ignoreAttribute = WellKnownReferences.DataMemberIgnoreAttribute(compilation);
        
        if (ignoreAttribute is null )
            return true;

        if(SerializationContext.YamlSerializer == context)
        {
            if(WellKnownReferences.HasAttribute(symbol,ignoreAttribute))
            {
                return true;
            }
            return false;
        }
        else if (SerializationContext.DataSerializer == context)
        {
            if (WellKnownReferences.HasAttribute(symbol, ignoreAttribute))
            {
                var updateableAttribute = WellKnownReferences.DataMemberUpdatableAttribute(compilation);
                if(updateableAttribute is null)
                    return true;
                // UpdateableAttribute negates the Effect of DataMemberIgnoreAttribute
                if (WellKnownReferences.HasAttribute(symbol, updateableAttribute))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        return true;
    }
    public enum SerializationContext
    {
        YamlSerializer,
        DataSerializer
    }
}
