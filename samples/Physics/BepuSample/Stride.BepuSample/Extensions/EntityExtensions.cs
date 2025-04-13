using System;
using System.Linq;
using Stride.Engine;

namespace Stride.BepuSample.Extensions;
public static class EntityExtensions
{
    public static T? GetComponentInChildren<T>(this Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var result = entity.OfType<T>().FirstOrDefault();

        if (result is null)
        {
            var children = entity.GetChildren();

            foreach (var child in children)
            {
                result = child.GetComponentInChildren<T>();

                if (result != null)
                {
                    return result;
                }
            }
        }

        return result;
    }
}
