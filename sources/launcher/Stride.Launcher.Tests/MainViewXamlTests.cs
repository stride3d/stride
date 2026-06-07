using System.Xml.Linq;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class MainViewXamlTests
{
    [Fact]
    public void RecentProjectOpenWithMenu_PassesSelectedVersionToCommand()
    {
        var xaml = XDocument.Load(FindMainViewPath());
        XNamespace avalonia = "https://github.com/avaloniaui";

        var openWithButton = Assert.Single(
            xaml.Descendants(avalonia + "Button"),
            element =>
                (string?)element.Attribute("Command")
                == "{Binding $parent[ScrollViewer].((vm:RecentProjectViewModel)DataContext).OpenWithCommand}");

        Assert.Equal("{Binding}", (string?)openWithButton.Attribute("CommandParameter"));
    }

    private static string FindMainViewPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "sources", "launcher", "Stride.Launcher", "Views", "MainView.axaml");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find MainView.axaml.");
    }
}
