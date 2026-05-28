// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class ServiceRegistryTests
{
    private interface ITestService { }
    private class TestService : ITestService { }
    private interface ITestService2 { }
    private class TestService2 : ITestService2 { }

    [Fact]
    public void AddService_AddsServiceToRegistry()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();

        registry.AddService<ITestService>(service);

        var retrieved = registry.GetService<ITestService>();
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void AddService_WithNullService_ThrowsArgumentNullException()
    {
        var registry = new ServiceRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.AddService<ITestService>(null!));
    }

    [Fact]
    public void AddService_WithDuplicateType_ThrowsArgumentException()
    {
        var registry = new ServiceRegistry();
        var service1 = new TestService();
        var service2 = new TestService();

        registry.AddService<ITestService>(service1);

        Assert.Throws<ArgumentException>(() => registry.AddService<ITestService>(service2));
    }

    [Fact]
    public void AddService_RaisesServiceAddedEvent()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();
        ServiceEventArgs? eventArgs = null;

        registry.ServiceAdded += (sender, args) => eventArgs = args;

        registry.AddService<ITestService>(service);

        Assert.NotNull(eventArgs);
        Assert.Equal(typeof(ITestService), eventArgs.ServiceType);
        Assert.Same(service, eventArgs.Instance);
    }

    [Fact]
    public void GetService_WithUnregisteredService_ReturnsNull()
    {
        var registry = new ServiceRegistry();

        var service = registry.GetService<ITestService>();

        Assert.Null(service);
    }

    [Fact]
    public void RemoveService_RemovesServiceFromRegistry()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();
        registry.AddService<ITestService>(service);

        registry.RemoveService<ITestService>();

        var retrieved = registry.GetService<ITestService>();
        Assert.Null(retrieved);
    }

    [Fact]
    public void RemoveService_RaisesServiceRemovedEvent()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();
        registry.AddService<ITestService>(service);
        ServiceEventArgs? eventArgs = null;

        registry.ServiceRemoved += (sender, args) => eventArgs = args;

        registry.RemoveService<ITestService>();

        Assert.NotNull(eventArgs);
        Assert.Equal(typeof(ITestService), eventArgs.ServiceType);
        Assert.Same(service, eventArgs.Instance);
    }

    [Fact]
    public void RemoveService_WithUnregisteredService_DoesNotRaiseEvent()
    {
        var registry = new ServiceRegistry();
        var eventRaised = false;

        registry.ServiceRemoved += (sender, args) => eventRaised = true;

        registry.RemoveService<ITestService>();

        Assert.False(eventRaised);
    }

    [Fact]
    public void RemoveService_WithServiceObject_RemovesMatchingService()
    {
        var registry = new ServiceRegistry();
        var service = new TestService();
        registry.AddService(service);

        var result = registry.RemoveService(service);

        Assert.True(result);
        Assert.Null(registry.GetService<TestService>());
    }

    [Fact]
    public void RemoveService_WithDifferentServiceObject_ReturnsFalse()
    {
        var registry = new ServiceRegistry();
        var service1 = new TestService();
        var service2 = new TestService();
        registry.AddService(service1);

        var result = registry.RemoveService(service2);

        Assert.False(result);
        Assert.Same(service1, registry.GetService<TestService>());
    }

    [Fact]
    public void GetOrCreate_WithUnregisteredService_CreatesAndAddsService()
    {
        var registry = new ServiceRegistry();

        var service = registry.GetOrCreate<TestServiceWithFactory>();

        Assert.NotNull(service);
        Assert.Same(service, registry.GetService<TestServiceWithFactory>());
    }

    [Fact]
    public void GetOrCreate_WithRegisteredService_ReturnsExistingService()
    {
        var registry = new ServiceRegistry();
        var existingService = new TestServiceWithFactory(registry);
        registry.AddService(existingService);

        var service = registry.GetOrCreate<TestServiceWithFactory>();

        Assert.Same(existingService, service);
    }

    [Fact]
    public void ServiceRegistryKey_IsNotNull()
    {
        Assert.NotNull(ServiceRegistry.ServiceRegistryKey);
        Assert.Equal(nameof(ServiceRegistry.ServiceRegistryKey), ServiceRegistry.ServiceRegistryKey.Name);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAddAndGet_WorksCorrectly()
    {
        var registry = new ServiceRegistry();
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var service = new TestService();
                try
                {
                    registry.AddService<ITestService>(service);
                }
                catch (ArgumentException)
                {
                    // Expected if another thread adds first
                }

                var retrieved = registry.GetService<ITestService>();
                Assert.NotNull(retrieved);
            }));
        }

        await Task.WhenAll(tasks);
    }

    private class TestServiceWithFactory : IService
    {
        public TestServiceWithFactory(IServiceRegistry registry)
        {
        }

        public static IService NewInstance(IServiceRegistry registry)
        {
            return new TestServiceWithFactory(registry);
        }
    }
}
