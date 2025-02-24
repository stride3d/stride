using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Stride.Core.Storage;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stride.StorageTool;

public partial class MainWindow : Window
{
    public MainWindow(string? bundlePath)
    {
        InitializeComponent();
        ObjectDataGrid.IsVisible = false;
        WelcomePanel.IsVisible = true;

        if (File.Exists(bundlePath))
        {
            using FileStream fs = File.OpenRead(bundlePath);
            LoadObjectEntries(fs);
        }
    }

    private void Exit(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private async void OpenBundle(object sender, RoutedEventArgs e)
    {
        var bundleFiles = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            FileTypeFilter =
            [
                new("Bundle package") { Patterns = ["*.bundle"], }
            ],
            Title = "Choose .bundle package",
            AllowMultiple = false,
        });
         
        
        if(bundleFiles.Count == 0)
            return;
        
        var bundleFile = bundleFiles[0];

        var bundleStream = await bundleFile.OpenReadAsync();
        
        LoadObjectEntries(bundleStream);
    }

    private void LoadObjectEntries(Stream bundleStream)
    {
        BundleDescription bundle = BundleOdbBackend.ReadBundleDescription(bundleStream);

        var objectInfos = bundle.Objects.ToDictionary(x => x.Key, x => x.Value);
        
        var entries = new List<ObjectEntry>();
        foreach (var locationIds in bundle.Assets)
        {
            var entry = new ObjectEntry { Location = locationIds.Key, Id = locationIds.Value.ToString() };

            if (objectInfos.TryGetValue(locationIds.Value, out var objectInfo))
            {
                entry.Size = objectInfo.EndOffset - objectInfo.StartOffset;
                entry.SizeNotCompressed = objectInfo.SizeNotCompressed;
            }   
            entries.Add(entry);
        }
        
        ObjectDataGrid.IsVisible = true;
        WelcomePanel.IsVisible = false;
        ObjectDataGrid.ItemsSource = entries;
    }
}
