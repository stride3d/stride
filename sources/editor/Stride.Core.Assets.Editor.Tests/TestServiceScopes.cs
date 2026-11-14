// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The scopes of the view model service providers: lookups through a parent made before or after the child, factory
/// registrations and their lifetimes, removal and the events, disposal, and constructor injection.
/// </summary>
public sealed class TestServiceScopes
{
    private interface IGreeter
    {
        string Name { get; }
    }

    private sealed class Greeter(string name) : IGreeter
    {
        public string Name { get; } = name;
    }

    private sealed class OtherGreeter(string name) : IGreeter
    {
        public string Name { get; } = name;
    }

    private sealed class Tracked : IDisposable
    {
        public IServiceProvider? MadeWith { get; init; }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class Asset(string name)
    {
        public string Name { get; } = name;
    }

    // A fixed signature, (Asset), and a longer constructor that also asks for a service
    private sealed class Editor
    {
        public Editor(Asset asset)
        {
            Asset = asset;
        }

        public Editor(Asset asset, IGreeter greeter)
        {
            Asset = asset;
            Greeter = greeter;
        }

        public Asset Asset { get; }

        public IGreeter? Greeter { get; }
    }

    private sealed class EditorWithScope(Asset asset, IViewModelServiceProvider services)
    {
        public Asset Asset { get; } = asset;

        public IViewModelServiceProvider Services { get; } = services;
    }

    [Fact]
    public void ChildSeesServicesRegisteredInItsParentAfterItWasMade()
    {
        var editor = new ViewModelServiceProvider();
        var session = new ViewModelServiceProvider(editor);
        var tab = new ViewModelServiceProvider(session);

        var greeter = new Greeter("late");
        editor.RegisterService(greeter);

        Assert.Same(greeter, tab.TryGet<IGreeter>());
        Assert.Same(greeter, session.Get<Greeter>());

        editor.UnregisterService(greeter);
        Assert.Null(tab.TryGet<IGreeter>());
    }

    [Fact]
    public void ChildServiceHidesTheParentOne()
    {
        var editor = new ViewModelServiceProvider([new Greeter("editor")]);
        var session = new ViewModelServiceProvider(editor, [new Greeter("session")]);

        Assert.Equal("session", session.Get<IGreeter>().Name);
        Assert.Equal("editor", editor.Get<IGreeter>().Name);
    }

    [Fact]
    public void ThreeScopesEachKeepTheirOwnServices()
    {
        var editor = new ViewModelServiceProvider([new Greeter("editor")]);
        using var session = new ViewModelServiceProvider(editor, [new Tracked()]);
        using var tab = new ViewModelServiceProvider(session, [new Asset("tab")]);

        Assert.NotNull(tab.TryGet<IGreeter>());
        Assert.NotNull(tab.TryGet<Tracked>());
        Assert.NotNull(tab.TryGet<Asset>());
        Assert.Null(session.TryGet<Asset>());
        Assert.Null(editor.TryGet<Tracked>());
    }

    [Fact]
    public void SeveralMatchingInstancesThrowUnlessAFactoryNamesTheServiceType()
    {
        var provider = new ViewModelServiceProvider([new Greeter("a"), new OtherGreeter("b")]);
        Assert.Throws<InvalidOperationException>(() => provider.TryGet<IGreeter>());

        using var registration = provider.Register<IGreeter>(sp => sp.GetRequiredService<Greeter>());

        Assert.Equal("a", provider.Get<IGreeter>().Name);
        // The other types still go by the instances
        Assert.Equal("b", provider.Get<OtherGreeter>().Name);
    }

    [Fact]
    public void SingletonIsMadeOnceWithTheProviderItIsRegisteredIn()
    {
        using var session = new ViewModelServiceProvider(new ViewModelServiceProvider());
        using var tab1 = new ViewModelServiceProvider(session);
        using var tab2 = new ViewModelServiceProvider(session);
        var made = 0;
        session.Register(sp => { ++made; return new Tracked { MadeWith = sp }; });

        Assert.Equal(0, made);
        var instance = tab1.Get<Tracked>();
        Assert.Same(instance, tab2.Get<Tracked>());
        Assert.Same(instance, session.Get<Tracked>());
        Assert.Equal(1, made);
        Assert.Same(session, instance.MadeWith);
    }

    [Fact]
    public void PerScopeGivesEachScopeItsOwnInstance()
    {
        using var session = new ViewModelServiceProvider(new ViewModelServiceProvider());
        var tab1 = new ViewModelServiceProvider(session);
        var tab2 = new ViewModelServiceProvider(session);
        session.Register(sp => new Tracked { MadeWith = sp }, ServiceLifetime.PerScope);

        var inTab1 = tab1.Get<Tracked>();
        var inTab2 = tab2.Get<Tracked>();
        var inSession = session.Get<Tracked>();

        Assert.NotSame(inTab1, inTab2);
        Assert.NotSame(inTab1, inSession);
        Assert.Same(inTab1, tab1.Get<Tracked>());
        Assert.Same(tab1, inTab1.MadeWith);
        Assert.Same(tab2, inTab2.MadeWith);

        tab1.Dispose();
        Assert.True(inTab1.IsDisposed);
        Assert.False(inTab2.IsDisposed);
        Assert.False(inSession.IsDisposed);

        tab2.Dispose();
        Assert.True(inTab2.IsDisposed);
        // A late lookup from a disposed tab makes nothing that would never be disposed
        var made = 0;
        session.Register(_ => { ++made; return new Asset("scoped"); }, ServiceLifetime.PerScope);
        Assert.Null(tab1.TryGet<Asset>());
        Assert.Equal(0, made);
        Assert.NotNull(session.TryGet<Asset>());
        Assert.Equal(1, made);
    }

    [Fact]
    public void RemovingAFactoryRegistrationDisposesWhatItMadeAndRaisesTheEvent()
    {
        var provider = new ViewModelServiceProvider();
        var registered = new List<Type>();
        var unregistered = new List<object?>();
        provider.ServiceRegistered += (_, e) => registered.Add(e.ServiceType);
        provider.ServiceUnregistered += (_, e) => unregistered.Add(e.Service);

        var registration = provider.Register(_ => new Tracked());
        Assert.Equal([typeof(Tracked)], registered);
        var instance = provider.Get<Tracked>();

        registration.Dispose();
        Assert.Null(provider.TryGet<Tracked>());
        Assert.True(instance.IsDisposed);
        Assert.Equal([instance], unregistered);

        // A second removal does nothing
        registration.Dispose();
        Assert.Single(unregistered);
    }

    [Fact]
    public void AFactoryRegisteredTwiceInTheSameProviderThrows()
    {
        var provider = new ViewModelServiceProvider();
        using var registration = provider.Register(_ => new Tracked());
        Assert.Throws<InvalidOperationException>(() => provider.Register(_ => new Tracked()));
    }

    [Fact]
    public void AFactoryThatAsksForItsOwnServiceThrows()
    {
        var provider = new ViewModelServiceProvider();
        using var registration = provider.Register<IGreeter>(sp => sp.GetRequiredService<IGreeter>());
        Assert.Throws<InvalidOperationException>(() => provider.TryGet<IGreeter>());
    }

    [Fact]
    public void ChildRaisesTheEventsOfItsParentWhileItHasHandlers()
    {
        var editor = new ViewModelServiceProvider();
        using var session = new ViewModelServiceProvider(editor);
        var registered = new List<object?>();
        var unregistered = new List<object?>();
        EventHandler<ServiceRegistrationEventArgs> onRegistered = (_, e) => registered.Add(e.Service);
        session.ServiceRegistered += onRegistered;
        session.ServiceUnregistered += (_, e) => unregistered.Add(e.Service);

        var greeter = new Greeter("editor");
        editor.RegisterService(greeter);
        editor.UnregisterService(greeter);
        Assert.Equal([greeter], registered);
        Assert.Equal([greeter], unregistered);

        session.ServiceRegistered -= onRegistered;
        editor.RegisterService(new Greeter("unseen"));
        Assert.Single(registered);
    }

    [Fact]
    public void DisposedScopeLosesItsServicesAndKeepsTheParentOnes()
    {
        var editor = new ViewModelServiceProvider([new Greeter("editor")]);
        var session = new ViewModelServiceProvider(editor);
        var owned = new Tracked();
        session.RegisterService(owned);
        session.Register(_ => new Asset("made"));
        var made = session.Register<IDisposable>(_ => new Tracked(), ServiceLifetime.PerScope);
        var madeInstance = (Tracked)session.Get<IDisposable>();
        var unregistered = new List<Type>();
        session.ServiceUnregistered += (_, e) => unregistered.Add(e.ServiceType);

        session.Dispose();

        Assert.Null(session.TryGet<Tracked>());
        Assert.Null(session.TryGet<Asset>());
        Assert.Null(session.TryGet<IDisposable>());
        Assert.Equal("editor", session.Get<IGreeter>().Name);
        Assert.Equal([typeof(Tracked), typeof(Asset), typeof(IDisposable)], unregistered);
        // The instances it made go with it; the registered ones belong to whoever registered them
        Assert.True(madeInstance.IsDisposed);
        Assert.False(owned.IsDisposed);
        // Removals after the disposal do nothing
        session.UnregisterService(owned);
        made.Dispose();
        Assert.Throws<ObjectDisposedException>(() => session.RegisterService(new Greeter("late")));
    }

    [Fact]
    public void ProviderIsAnIServiceProvider()
    {
        IViewModelServiceProvider provider = new ViewModelServiceProvider([new Greeter("a")]);
        IServiceProvider services = provider;

        Assert.Equal("a", Assert.IsType<Greeter>(services.GetService(typeof(IGreeter))).Name);
        Assert.Same(provider, services.GetService(typeof(IServiceProvider)));
        Assert.Same(provider, services.GetService(typeof(IViewModelServiceProvider)));
        Assert.Null(services.GetService(typeof(Asset)));

        // The empty provider too
        services = ViewModelServiceProvider.NullServiceProvider;
        Assert.Null(services.GetService(typeof(IGreeter)));
    }

    [Fact]
    public void InjectionKeepsTheFixedSignatureAndAddsServices()
    {
        var asset = new Asset("sphere");

        // Without the service, the fixed signature is the one that can be called
        var bare = new ViewModelServiceProvider();
        var editor = bare.CreateInstance<Editor>(asset);
        Assert.Same(asset, editor.Asset);
        Assert.Null(editor.Greeter);

        // With it, the longest constructor gets the given argument and the service
        var withService = new ViewModelServiceProvider(new ViewModelServiceProvider([new Greeter("injected")]));
        editor = (Editor)withService.CreateInstance(typeof(Editor), asset);
        Assert.Same(asset, editor.Asset);
        Assert.Equal("injected", editor.Greeter?.Name);
    }

    [Fact]
    public void InjectionGivesTheScopeItselfForTheProviderParameter()
    {
        using var tab = new ViewModelServiceProvider(new ViewModelServiceProvider());
        var editor = tab.CreateInstance<EditorWithScope>(new Asset("sphere"));
        Assert.Same(tab, editor.Services);
    }

    [Fact]
    public void InjectionThrowsWhenNoConstructorFits()
    {
        var provider = new ViewModelServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.CreateInstance<EditorWithScope>());
    }
}

file static class ServiceProviderTestExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider) where T : class
    {
        return (T)(provider.GetService(typeof(T)) ?? throw new InvalidOperationException($"No {typeof(T)}."));
    }
}
