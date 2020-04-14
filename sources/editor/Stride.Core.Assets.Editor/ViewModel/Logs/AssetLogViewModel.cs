// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel.Logs
{
    public class AssetLogViewModel : LoggerViewModel
    {
        private readonly SessionViewModel session;
        private readonly Dictionary<LogKey, Logger> loggers = new Dictionary<LogKey, Logger>();
        private bool waiting;
        private int errorCount;

        public AssetLogViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session)
            : base(serviceProvider)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            MinLevel = LogMessageType.Warning;
            this.session = session;
            FilteredMessages = new ObservableList<ILogMessage>();
            session.ActiveAssetView.SelectedAssets.CollectionChanged += (s, e) => RefreshFilteredMessages();
            Messages.CollectionChanged += (s, e) => RefreshFilteredMessages();

            // Listen if IBuildService gets registered
            ServiceProvider.ServiceRegistered += ServiceRegistered;
            // Initial check, in case it is already registered
            CheckBuildService();
        }

        public override void Destroy()
        {
            ServiceProvider.ServiceRegistered -= ServiceRegistered;
            base.Destroy();
        }

        public ObservableList<ILogMessage> FilteredMessages { get; private set; }

        public int ErrorCount { get { return errorCount; } set { SetValue(ref errorCount, value); } }

        private void ServiceRegistered(object sender, ServiceRegistrationEventArgs e)
        {
            CheckBuildService();
        }

        private void CheckBuildService()
        {
            var service = ServiceProvider.TryGet<IBuildService>();
            if (service != null)
            {
                service.AssetBuilt += AssetBuilt;
                ServiceProvider.ServiceRegistered -= ServiceRegistered;
            }
        }

        private async void RefreshFilteredMessages()
        {
            if (waiting)
                return;

            waiting = true;
            await Task.Delay(500);
            waiting = false;

            var selection = await Dispatcher.InvokeAsync(() => session.ActiveAssetView.SelectedAssets.ToList());
            FilteredMessages.Clear();
            var ids = await Task.Run(() =>
            {
                var result = new HashSet<AssetId>();
                foreach (var asset in selection)
                {
                    result.Add(asset.Id);
                    var dependencyManager = asset.AssetItem.Package.Session.DependencyManager;
                    var dependencies = dependencyManager.ComputeDependencies(asset.AssetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.All);
                    if (dependencies != null)
                    {
                        foreach (var reference in dependencies.LinksOut)
                        {
                            result.Add(reference.Item.Id);
                        }
                    }
                }
                return result;
            });
            var selectedLoggers = loggers.Where(x => ids.Contains(x.Key.AssetId));
            foreach (var loggerResult in selectedLoggers.Select(x => x.Value).OfType<LoggerResult>())
            {
                FilteredMessages.AddRange(loggerResult.Messages);
            }
            ErrorCount = FilteredMessages.Count(x => x.IsAtLeast(LogMessageType.Warning));
        }

        private void AssetBuilt(object sender, AssetBuiltEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                AssetViewModel asset = session.GetAssetById(e.AssetItem.Id);
                if (asset != null)
                {
                    var key = LogKey.Get(e.AssetItem.Id, "Build");
                    ClearMessages(key);
                    RemoveLogger(key);
                    AddLogger(key, e.BuildLog);
                }
            });
        }

        public void AddLogger(LogKey key, Logger logger)
        {
            loggers.Add(key, logger);
            AddLogger(logger);
        }

        public override void AddLogger(Logger logger)
        {
            base.AddLogger(logger);
            var loggerResult = logger as LoggerResult;
            if (loggerResult != null)
            {
                var messages = (ObservableList<ILogMessage>)Messages;
                Loggers[logger].AddRange(loggerResult.Messages);
                messages.AddRange(loggerResult.Messages);
            }
        }

        public bool RemoveLogger(LogKey key)
        {
            var logger = GetLogger(key, false);
            if (logger != null)
            {
                RemoveLogger(logger);
                loggers.Remove(key);
                return true;
            }
            return false;
        }

        public Logger GetLogger(LogKey key, bool create = true)
        {
            Logger logger;
            if (!loggers.TryGetValue(key, out logger) && create)
            {
                logger = GlobalLogger.GetLogger(key.ToString());
                loggers.Add(key, logger);
            }
            return logger;
        }

        public void ClearMessages(LogKey key)
        {
            var logger = GetLogger(key, false);
            if (logger != null)
            {
                ClearMessages(logger);
            }
        }
    }
}
