// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestProjectReference
{
    [Fact]
    public void TestDefaultConstructor()
    {
        var reference = new ProjectReference();

        Assert.Equal(Guid.Empty, reference.Id);
        Assert.Null(reference.Location);
    }

    [Fact]
    public void TestConstructorWithParameters()
    {
        var id = Guid.NewGuid();
        var location = new UFile(@"C:\Projects\MyProject.csproj");
        var type = ProjectType.Executable;

        var reference = new ProjectReference(id, location, type);

        Assert.Equal(id, reference.Id);
        Assert.Equal(location, reference.Location);
        Assert.Equal(type, reference.Type);
    }

    [Fact]
    public void TestIdProperty()
    {
        var reference = new ProjectReference();
        var id = Guid.NewGuid();

        reference.Id = id;

        Assert.Equal(id, reference.Id);
    }

    [Fact]
    public void TestLocationProperty()
    {
        var reference = new ProjectReference();
        var location = new UFile(@"Project.csproj");

        reference.Location = location;

        Assert.Equal(location, reference.Location);
    }

    [Fact]
    public void TestTypeProperty()
    {
        var reference = new ProjectReference();

        reference.Type = ProjectType.Library;
        Assert.Equal(ProjectType.Library, reference.Type);

        reference.Type = ProjectType.Executable;
        Assert.Equal(ProjectType.Executable, reference.Type);
    }

    [Fact]
    public void TestRootNamespaceProperty()
    {
        var reference = new ProjectReference { RootNamespace = "MyCompany.MyProject" };

        Assert.Equal("MyCompany.MyProject", reference.RootNamespace);
    }

    [Fact]
    public void TestEqualityWithSameValues()
    {
        var id = Guid.NewGuid();
        var location = new UFile(@"Project.csproj");

        var ref1 = new ProjectReference(id, location, ProjectType.Library);
        var ref2 = new ProjectReference(id, location, ProjectType.Library);

        Assert.True(ref1.Equals(ref2));
    }

    [Fact]
    public void TestInequalityWithDifferentIds()
    {
        var location = new UFile(@"Project.csproj");

        var ref1 = new ProjectReference(Guid.NewGuid(), location, ProjectType.Library);
        var ref2 = new ProjectReference(Guid.NewGuid(), location, ProjectType.Library);

        Assert.False(ref1.Equals(ref2));
    }

    [Fact]
    public void TestEqualityWithNull()
    {
        var reference = new ProjectReference(Guid.NewGuid(), new UFile("Project.csproj"), ProjectType.Library);

        Assert.False(reference.Equals(null));
    }

    [Fact]
    public void TestEqualityWithSameReference()
    {
        var reference = new ProjectReference(Guid.NewGuid(), new UFile("Project.csproj"), ProjectType.Library);

        Assert.True(reference.Equals(reference));
    }

    [Fact]
    public void TestGetHashCode()
    {
        var id = Guid.NewGuid();
        var location = new UFile("Project.csproj");
        var ref1 = new ProjectReference(id, location, ProjectType.Library);
        var ref2 = new ProjectReference(id, location, ProjectType.Library);

        // Equal references must produce equal hash codes
        Assert.Equal(ref1, ref2);
        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }

    [Fact]
    public void TestIIdentifiableInterface()
    {
        var id = Guid.NewGuid();
        var reference = new ProjectReference(id, new UFile("Project.csproj"), ProjectType.Library);

        IIdentifiable identifiable = reference;
        Assert.Equal(id, identifiable.Id);
    }
}
