using Stride.Rendering;

namespace Stride.Updater.New;

internal sealed class ObjectParameterKeyAccessor<T> : UpdatableMember<ParameterCollection, T>, IUpdatableMember<ObjectParameterKeyAccessor<T>, ParameterCollection, T> where T : class
{
    private readonly string name;
    private readonly ObjectParameterKey<T> parameterKey;
    private readonly UpdatableType<T> type;

    public ObjectParameterKeyAccessor(string name, ObjectParameterKey<T> parameterKey, UpdatableType<T> type)
    {
        this.name = name;
        this.parameterKey = parameterKey;
        this.type = type;
    }

    public static bool SupportsByReference => false;

    public override string Name => name;

    internal override bool IsIndexer => true;

    public static T GetValue(ObjectParameterKeyAccessor<T> @this, ParameterCollection parent) => parent.Get(@this.parameterKey);

    public static void SetValue(ObjectParameterKeyAccessor<T> @this, ParameterCollection parent, T value) => parent.Set(@this.parameterKey, value);

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override unsafe void Update(ParameterCollection parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref ParameterCollection parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
