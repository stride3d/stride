using Stride.Rendering;

namespace Stride.Updater.New;

internal sealed class ValueParameterKeyAccessor<T> : UpdatableMember<ParameterCollection, T>, IUpdatableMember<ValueParameterKeyAccessor<T>, ParameterCollection, T> where T : struct
{
    private readonly string name;
    private readonly ValueParameterKey<T> parameterKey;
    private readonly UpdatableType<T> type;

    public ValueParameterKeyAccessor(string name, ValueParameterKey<T> parameterKey, UpdatableType<T> type)
    {
        this.name = name;
        this.parameterKey = parameterKey;
        this.type = type;
    }

    public static bool SupportsByReference => false;

    public override string Name => name;

    internal override bool IsIndexer => true;

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    public static T GetValue(ValueParameterKeyAccessor<T> @this, ParameterCollection parent) => parent.Get(@this.parameterKey);

    public static void SetValue(ValueParameterKeyAccessor<T> @this, ParameterCollection parent, T value) => parent.Set(@this.parameterKey, value);

    public override unsafe void Update(ParameterCollection parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref ParameterCollection parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
