// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Irony.Parsing;
using System.IO;
using System.Threading;

namespace Irony.GrammarExplorer {
  /// <summary>
  /// Maintains grammar assemblies, reloads updated files automatically.
  /// </summary>
  class GrammarLoader {
    private TimeSpan _autoRefreshDelay = TimeSpan.FromMilliseconds(500);
    private Dictionary<string, CachedAssembly> _cachedAssemblies = new Dictionary<string, CachedAssembly>();

    class CachedAssembly {
      public long FileSize;
      public DateTime LastWriteTime;
      public FileSystemWatcher Watcher;
      public Assembly Assembly;
    }

    public event EventHandler AssemblyUpdated;

    public GrammarItem SelectedGrammar { get; set; }

    public Parsing.Grammar CreateGrammar() {
      if (SelectedGrammar == null)
        return null;

      var type = SelectedGrammarAssembly.GetType(SelectedGrammar.TypeName, true, true);
      return Activator.CreateInstance(type) as Parsing.Grammar;
    }

    Assembly SelectedGrammarAssembly {
      get {
        if (SelectedGrammar == null)
          return null;

        // create assembly cache entry as needed
        var location = SelectedGrammar.Location;
        if (!_cachedAssemblies.ContainsKey(location)) {
          var fileInfo = new FileInfo(location);
          _cachedAssemblies[location] =
            new CachedAssembly {
              LastWriteTime = fileInfo.LastWriteTime,
              FileSize = fileInfo.Length,
              Assembly = null
            };

          // set up file system watcher
          _cachedAssemblies[location].Watcher = CreateFileWatcher(location);
        }

        // get loaded assembly from cache if possible
        var assembly = _cachedAssemblies[location].Assembly;
        if (assembly == null) {
          assembly = LoadAssembly(location);
          _cachedAssemblies[location].Assembly = assembly;
        }

        return assembly;
      }
    }

    private FileSystemWatcher CreateFileWatcher(string location) {
      var folder = Path.GetDirectoryName(location);
      var watcher = new FileSystemWatcher(folder);
      watcher.Filter = Path.GetFileName(location);

      watcher.Changed += (s, args) => {
        if (args.ChangeType != WatcherChangeTypes.Changed)
          return;

        // check if assembly was changed indeed to work around multiple FileSystemWatcher event firing
        var cacheEntry = _cachedAssemblies[location];
        var fileInfo = new FileInfo(location);
        if (cacheEntry.LastWriteTime == fileInfo.LastWriteTime && cacheEntry.FileSize == fileInfo.Length)
          return;

        // clear cached assembly and save last file update time
        cacheEntry.LastWriteTime = fileInfo.LastWriteTime;
        cacheEntry.FileSize = fileInfo.Length;
        cacheEntry.Assembly = null;

        // delay auto-refresh for safety reasons
        ThreadPool.QueueUserWorkItem(_ => {
          Thread.Sleep(_autoRefreshDelay);
          OnAssemblyUpdated(location);
        });
      };

      watcher.EnableRaisingEvents = true;
      return watcher;
    }

    private void OnAssemblyUpdated(string location) {
      if (AssemblyUpdated == null || SelectedGrammar == null || SelectedGrammar.Location != location)
        return;
      AssemblyUpdated(this, EventArgs.Empty);
    }

    Assembly LoadAssembly(string fileName) {
      // 1. Assembly.Load doesn't block the file
      // 2. Assembly.Load doesn't check if the assembly is already loaded in the current AppDomain
      return Assembly.LoadFile(fileName);
    }
  }
}
