// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestPackageTypeScope
{
    [Fact]
    public void ScopeHoldsTheTypesOfThePackageItsDependenciesAndTheHost()
    {
        var session = new PackageSession();
        var game = new Package { Meta = { Name = "Game" } };
        var gameProject = new SolutionProject(game, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(gameProject);
        var plugin = AddPackage(session, "Plugin", typeof(XDocument));
        AddPackage(session, "OtherPlugin", typeof(Regex));
        gameProject.FlattenedDependencies.Add(new Dependency(plugin));
        game.LoadedAssemblies.Add(new PackageLoadedAssembly(null, "Game.dll") { Assembly = typeof(FactAttribute).Assembly });

        var scope = PackageTypeScope.For(game);

        Assert.True(scope.Contains(typeof(FactAttribute)));
        Assert.True(scope.Contains(typeof(XDocument)));
        Assert.False(scope.Contains(typeof(Regex)));
        // No package loaded the test assembly: it is the host's
        Assert.True(scope.Contains(typeof(TestPackageTypeScope)));
    }

    [Fact]
    public void AssemblyOfTwoPackagesIsInScopeThroughEither()
    {
        var session = new PackageSession();
        var game = new Package { Meta = { Name = "Game" } };
        var gameProject = new SolutionProject(game, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(gameProject);
        var plugin = AddPackage(session, "Plugin", typeof(XDocument));
        AddPackage(session, "OtherPlugin", typeof(XDocument));

        Assert.False(PackageTypeScope.For(game).Contains(typeof(XDocument)));

        gameProject.FlattenedDependencies.Add(new Dependency(plugin));
        Assert.True(PackageTypeScope.For(game).Contains(typeof(XDocument)));
    }

    [Fact]
    public void EverythingIsInScopeOutsideASession()
    {
        var scope = PackageTypeScope.For(new Package());

        Assert.True(scope.Contains(typeof(Regex)));
    }

    // A dependency package whose loaded assembly is the one of the given type
    private static Package AddPackage(PackageSession session, string name, Type type)
    {
        var package = new Package { Meta = { Name = name } };
        session.Projects.Add(new StandalonePackage(package) { IsDependencyPackage = true });
        package.LoadedAssemblies.Add(new PackageLoadedAssembly(null, $"{name}.dll") { Assembly = type.Assembly });
        return package;
    }
}
