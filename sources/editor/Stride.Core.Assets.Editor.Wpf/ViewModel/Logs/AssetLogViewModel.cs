// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel.Logs
{
    public class AssetLogViewModel : LoggerViewModel
    {
        // The names of the logs (see LogKey.Name); each one is a source that can be hidden
        public const string BuildLogName = "Build";
        public const string ThumbnailLogName = "Thumbnail";
        public const string AssetReplacementLogName = "AssetReplacement";
        public const string SessionLogName = "Session";
        // Followed by the package name: one log per package
        public const string ConsistencyLogName = "Consistency";

        private readonly SessionViewModel session;
        private readonly Dictionary<LogKey, Logger> loggers = new Dictionary<LogKey, Logger>();
        private readonly HashSet<Logger> createdLoggers = new HashSet<Logger>();
        private bool waiting;
        private int errorCount;
        private AssetLogScope scope;

        public AssetLogViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session)
            : base(serviceProvider)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            MinLevel = LogMessageType.Warning;
            this.session = session;
            FilteredMessages = new ObservableList<ILogMessage>();

            scope = InternalSettings.AssetLogDisplayScope.GetValue();
            var hiddenSources = InternalSettings.AssetLogHiddenSources.GetValue();
            AssetLogSourceViewModel CreateSource(string id, string displayName) => new(serviceProvider, id, displayName, !hiddenSources.Contains(id), SourcesChanged);
            Sources =
            [
                CreateSource(BuildLogName, Tr._p("Log", "Asset builds")),
                CreateSource(ThumbnailLogName, Tr._p("Log", "Thumbnails")),
                CreateSource(AssetReplacementLogName, Tr._p("Log", "Asset replacements")),
                CreateSource(SessionLogName, Tr._p("Log", "Project load")),
                CreateSource(ConsistencyLogName, Tr._p("Log", "Package consistency")),
            ];
            ShowAllSourcesCommand = new AnonymousCommand(serviceProvider, () => Sources.ForEach(x => x.IsShown = true));
            UpdateSourcesState();

            session.ActiveAssetView.SelectedAssets.CollectionChanged += (s, e) =>
            {
                if (Scope == AssetLogScope.SelectedAssets)
                    RefreshFilteredMessages();
            };
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

        /// <summary>
        /// Gets or sets which assets the messages are shown for. Messages about no asset are shown in both cases.
        /// </summary>
        public AssetLogScope Scope
        {
            get => scope;
            set => SetValue(ref scope, value, () =>
            {
                InternalSettings.AssetLogDisplayScope.SetValue(value);
                RefreshFilteredMessages();
            });
        }

        /// <summary>
        /// Gets the sources of messages, which can be hidden.
        /// </summary>
        public IReadOnlyList<AssetLogSourceViewModel> Sources { get; }

        /// <summary>
        /// Gets the header of the sources menu, which tells when some sources are hidden.
        /// </summary>
        public string SourcesHeader { get; private set => SetValue(ref field, value); }

        public ICommandBase ShowAllSourcesCommand { get; }

        private void SourcesChanged()
        {
            InternalSettings.AssetLogHiddenSources.SetValue(Sources.Where(x => !x.IsShown).Select(x => x.Id).ToList());
            UpdateSourcesState();
            RefreshFilteredMessages();
        }

        private void UpdateSourcesState()
        {
            var shownCount = Sources.Count(x => x.IsShown);
            SourcesHeader = shownCount == Sources.Count
                ? Tr._p("Log", "Sources")
                : string.Format(Tr._p("Log", "Sources ({0}/{1})"), shownCount, Sources.Count);
            ShowAllSourcesCommand.IsEnabled = shownCount < Sources.Count;
        }

        private bool IsSourceShown(LogKey key)
        {
            var id = key.Name.StartsWith(ConsistencyLogName, StringComparison.Ordinal) ? ConsistencyLogName : key.Name;
            // A log from elsewhere (e.g. a plugin) has no entry: always shown
            return Sources.FirstOrDefault(x => x.Id == id)?.IsShown ?? true;
        }

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

            var selectedOnly = Scope == AssetLogScope.SelectedAssets;
            var selection = selectedOnly ? await Dispatcher.InvokeAsync(() => session.ActiveAssetView.SelectedAssets.ToList()) : [];
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
            foreach (var (key, logger) in loggers)
            {
                if (logger is not LoggerResult loggerResult || !IsSourceShown(key))
                    continue;

                if (key.AssetId != AssetId.Empty)
                {
                    if (!selectedOnly || ids.Contains(key.AssetId))
                    {
                        // A build message often does not name its asset: add the asset of the log (asset column, double-click)
                        var assetLocation = session.GetAssetById(key.AssetId)?.AssetItem.Location;
                        FilteredMessages.AddRange(loggerResult.Messages.Select(x => assetLocation != null && GetAssetId(x) == null ? new AssetEntryMessage(x, key.AssetId, assetLocation) : x));
                    }
                }
                else
                {
                    // A log that is not about one asset (session load, consistency): its messages about no asset or about a selected one
                    FilteredMessages.AddRange(loggerResult.Messages.Where(x => !selectedOnly || GetAssetId(x) is not { } id || ids.Contains(id)));
                }
            }
            ErrorCount = FilteredMessages.Count(x => x.IsAtLeast(LogMessageType.Warning));
        }

        /// <summary>
        /// A message of the log of an asset that does not name the asset: shown with the asset of the log, and its own text.
        /// </summary>
        public sealed class AssetEntryMessage : AssetSerializableLogMessage
        {
            private readonly ILogMessage message;

            public AssetEntryMessage(ILogMessage message, AssetId assetId, UFile assetUrl)
                : base(assetId, assetUrl, message.Type, message.Text, message.ExceptionInfo)
            {
                this.message = message;
                Module = message.Module;
            }

            public override string ToString() => message.ToString();
        }

        private static AssetId? GetAssetId(ILogMessage message)
        {
            var id = message switch
            {
                AssetLogMessage assetMessage => assetMessage.AssetReference?.Id,
                AssetSerializableLogMessage assetMessage => assetMessage.AssetId,
                _ => null,
            };
            return id == AssetId.Empty ? null : id;
        }

        private void AssetBuilt(object sender, AssetBuiltEventArgs e)
        {
            SetAssetLog(e.AssetItem.Id, BuildLogName, e.BuildLog);
        }

        /// <summary>
        /// Replaces the log of the given name for the given asset.
        /// </summary>
        /// <param name="assetId">The id of the asset.</param>
        /// <param name="name">The name of the log, for example "Build".</param>
        /// <param name="log">The new log, or null to only remove the previous one.</param>
        public void SetAssetLog(AssetId assetId, string name, LoggerResult log)
        {
            Dispatcher.InvokeAsync(() =>
            {
                AssetViewModel asset = session.GetAssetById(assetId);
                if (asset != null)
                {
                    var key = LogKey.Get(assetId, name);
                    ClearMessages(key);
                    RemoveLogger(key);
                    if (log != null)
                        AddLogger(key, log);
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
                createdLoggers.Remove(logger);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the logger with the given key. A logger created here is shown in this log.
        /// </summary>
        public Logger GetLogger(LogKey key, bool create = true)
        {
            Logger logger;
            if (!loggers.TryGetValue(key, out logger) && create)
            {
                logger = new LoggerResult(key.ToString());
                createdLoggers.Add(logger);
                AddLogger(key, logger);
            }
            return logger;
        }

        public void ClearMessages(LogKey key)
        {
            var logger = GetLogger(key, false);
            if (logger != null)
            {
                ClearMessages(logger);
                // The filtered messages are read from the loggers: a logger created here is reused, so it must forget its messages too
                if (createdLoggers.Contains(logger))
                    ((LoggerResult)logger).Clear();
            }
        }
    }
}
