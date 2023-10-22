using System;

namespace Stride.Core.Reflection;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ObjectDescriptorAttribute : Attribute
{
    public ObjectDescriptorAttribute(Type type) { Type = type; }
    public Type Type { get; set; }
}
