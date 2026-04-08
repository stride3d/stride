// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
///   Base class for a framework Component.
/// </summary>
/// <remarks>
///   <para>
///     A <strong>Component</strong> is an object that can have a <see cref="Name"/> and other meta-information
///     (see <see cref="Tags"/>), and that can hold references to other sub-Components that depend on it.
///   </para>
///   <para>
///     Components have reference-counting lifetime management, so calling <see cref="DisposeBase.Dispose"/> does
///     not immediately releases its underlying resources, but instead decreases an internal reference count.
///     When that reference count reaches zero, it then calls <see cref="Destroy"/> automatically, where the
///     underlying resources can then be released safely.
///   </para>
///   <para>
///     When <see cref="Destroy"/> is called, not only the resources associated with the Component itself should
///     be released, but it cascades the releasing of resources to its contained sub-Components.
///   </para>
/// </remarks>
/// <seealso cref="IComponent"/>
/// <seealso cref="IReferencable"/>
/// <seealso cref="ICollectorHolder"/>
[DataContract]
public abstract class ComponentBase : DisposeBase, IComponent, ICollectorHolder
{
    private string name;
    private ObjectCollector collector;


    /// <summary>
    ///   Initializes a new instance of the <see cref="ComponentBase"/> class.
    /// </summary>
    protected ComponentBase() : this(name: null) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="ComponentBase"/> class.
    /// </summary>
    /// <param name="name">The name attached to the Component, or <see langword="null"/> to use the type's name.</param>
    protected ComponentBase(string? name)
    {
        collector = new ObjectCollector();
        Tags = new PropertyContainer(this);
        this.name = name ?? GetType().Name;
    }


    /// <summary>
    ///   The properties attached to the Component.
    /// </summary>
    [DataMemberIgnore] // Do not try to recreate object (preserve Tags.Owner)
    public PropertyContainer Tags;

    /// <summary>
    ///   Gets or sets the name of the Component.
    /// </summary>
    /// <value>
    ///   The name that identifies the Component. It can be <see langword="null"/> to denote it has no specific name.
    /// </value>
    [DataMemberIgnore] // By default, don't store it, unless derived class are overriding this member
    public virtual string Name
    {
        get => name;
        set
        {
            if (value == name)
                return;

            name = value;

            OnNameChanged();
        }
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        collector.Dispose();
    }

    /// <inheritdoc/>
    ObjectCollector ICollectorHolder.Collector
    {
        get
        {
            collector.EnsureValid();
            return collector;
        }
    }

    /// <summary>
    ///   Called when the <see cref="Name"/> property has changed.
    /// </summary>
    protected virtual void OnNameChanged() { }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{GetType().Name}: {name}";
    }
}
