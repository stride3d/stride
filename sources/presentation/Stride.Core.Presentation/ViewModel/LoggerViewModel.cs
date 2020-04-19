// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// A view model that monitors messages from one or several loggers and update an observable collection of <see cref="ILogMessage"/> using the dispatcher.
    /// The updates are grouped together after a customizable delay to prevent blocking the UI thread.
    /// </summary>
    public class LoggerViewModel : DispatcherViewModel
    {
        /// <summary>
        /// The default delay to wait before updating the <see cref="Messages"/> collection, after a message has been received.
        /// </summary>
        public const int DefaultUpdateInterval = 300;

        protected readonly Dictionary<Logger, List<ILogMessage>> Loggers = new Dictionary<Logger, List<ILogMessage>>();
        private readonly ObservableList<ILogMessage> messages = new ObservableList<ILogMessage>();
        private readonly List<Tuple<Logger, ILogMessage>> pendingMessages = new List<Tuple<Logger, ILogMessage>>();

        private int updateInterval = DefaultUpdateInterval;
        private bool updatePending;
        private bool hasNewMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="Services.IDispatcherService"/> to use for this view model.</param>
        public LoggerViewModel([NotNull] IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            AddLoggerCommand = new AnonymousCommand<Logger>(serviceProvider, AddLogger);
            RemoveLoggerCommand = new AnonymousCommand<Logger>(serviceProvider, RemoveLogger);
            ClearLoggersCommand = new AnonymousCommand(serviceProvider, ClearLoggers);
            ClearMessagesCommand = new AnonymousCommand(serviceProvider, ClearMessages);
            ClearNewMessageFlagCommand = new AnonymousCommand(serviceProvider, () => HasNewMessages = false);
            messages.CollectionChanged += MessagesCollectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerViewModel"/> class with a single logger.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="Services.IDispatcherService"/> to use for this view model.</param>
        /// <param name="logger">The <see cref="Logger"/> to monitor.</param>
        public LoggerViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] Logger logger)
            : this(serviceProvider)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            Loggers.Add(logger, new List<ILogMessage>());
            logger.MessageLogged += MessageLogged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerViewModel"/> class with multiple loggers.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="Services.IDispatcherService"/> to use for this view model.</param>
        /// <param name="loggers">The collection of <see cref="Logger"/> to monitor.</param>
        public LoggerViewModel([NotNull] IViewModelServiceProvider serviceProvider, [ItemNotNull, NotNull] IEnumerable<Logger> loggers)
            : this(serviceProvider)
        {
            if (loggers == null) throw new ArgumentNullException(nameof(loggers));
            foreach (var logger in loggers)
            {
                Loggers.Add(logger, new List<ILogMessage>());
                logger.MessageLogged += MessageLogged;
            }
            ClearMessagesCommand = new AnonymousCommand(serviceProvider, ClearMessages);
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            ClearLoggers();
            base.Destroy();
        }

        /// <summary>
        /// Gets the collection of messages currently contained in this view model.
        /// </summary>
        [NotNull]
        public IReadOnlyObservableCollection<ILogMessage> Messages => messages;

        /// <summary>
        /// Gets or sets the interval in milliseconds between updates of the <see cref="Messages"/> collection. When a message is logged into one of the loggers,
        /// the view model will wait this interval before actually updating the message collection to catch other potential messages in a single shot.
        /// </summary>
        /// <remarks>The default value is equal to <see cref="DefaultUpdateInterval"/>.</remarks>
        public int UpdateInterval { get { return updateInterval; } set { SetValue(ref updateInterval, value); } }

        /// <summary>
        /// Gets whether the monitored logs have warnings.
        /// </summary>
        /// <remarks>This property does not raise the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events.</remarks>
        public bool HasWarnings { get; private set; }

        /// <summary>
        /// Gets whether the monitored logs have errors.
        /// </summary>
        /// <remarks>This property does not raise the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events.</remarks>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets whether the monitored logs have new messages.
        /// </summary>
        public bool HasNewMessages { get { return hasNewMessages; } private set { SetValue(ref hasNewMessages, value); } }

        /// <summary>
        /// Gets the minimum level of message that will be recorded by this view model.
        /// </summary>
        public LogMessageType MinLevel { get; set; } = LogMessageType.Debug;

        /// <summary>
        /// Gets the command that will add a logger to monitor.
        /// </summary>
        /// <remarks>An instance of <see cref="Logger"/> must be passed as parameter of this command.</remarks>
        [NotNull]
        public ICommandBase AddLoggerCommand { get; }

        /// <summary>
        /// Gets the command that will remove a logger from monitoring.
        /// </summary>
        [NotNull]
        public ICommandBase RemoveLoggerCommand { get; }

        /// <summary>
        /// Gets the command that will remove all loggers from monitoring. 
        /// </summary>
        [NotNull]
        public ICommandBase ClearLoggersCommand { get; }

        /// <summary>
        /// Gets the command that will clear the <see cref="Messages"/> collection.
        /// </summary>
        [NotNull]
        public ICommandBase ClearMessagesCommand { get; }

        /// <summary>
        /// Gets the command that will reset the <see cref="HasNewMessages"/> flag.
        /// </summary>
        [NotNull]
        public ICommandBase ClearNewMessageFlagCommand { get; }
        
        /// <summary>
        /// Adds a <see cref="Logger"/> to monitor.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to monitor.</param>
        public virtual void AddLogger([NotNull] Logger logger)
        {
            Loggers.Add(logger, new List<ILogMessage>());
            logger.MessageLogged += MessageLogged;
        }

        /// <summary>
        /// Removes a <see cref="Logger"/> from monitoring.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> to remove from monitoring.</param>
        public virtual void RemoveLogger([NotNull] Logger logger)
        {
            Loggers.Remove(logger);
            logger.MessageLogged -= MessageLogged;
        }

        /// <summary>
        /// Removes all loggers from monitoring.
        /// </summary>
        public virtual void ClearLoggers()
        {
            foreach (var logger in Loggers)
            {
                logger.Key.MessageLogged -= MessageLogged;
            }
            Loggers.Clear();
        }

        /// <summary>
        /// Flushes the pending log messages to add them immediately in the view model.
        /// </summary>
        public void Flush()
        {
            // Temporary cut the update interval. We use the backing field directly to
            // prevent triggering a PropertyChanged event.
            var interval = updateInterval;
            updateInterval = 0;
            Dispatcher.Invoke(UpdateMessages);
            updateInterval = interval;
        }

        /// <summary>
        /// Clears the <see cref="Messages"/> collection.
        /// </summary>
        public void ClearMessages()
        {
            messages.Clear();
            foreach (var logger in Loggers)
            {
                logger.Value.Clear();
            }
        }

        /// <summary>
        /// Removes messages that comes from the given logger from the <see cref="Messages"/> collection.
        /// </summary>
        public void ClearMessages([NotNull] Logger logger)
        {
            List<ILogMessage> messagesToRemove;
            if (Loggers.TryGetValue(logger, out messagesToRemove))
            {
                foreach (var messageToRemove in messagesToRemove)
                {
                    messages.Remove(messageToRemove);
                }
            }
        }

        /// <summary>
        /// The callback of the <see cref="Logger.MessageLogged"/> event, used to monitor incoming messages.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event argument.</param>
        private void MessageLogged(object sender, [NotNull] MessageLoggedEventArgs args)
        {
            lock (pendingMessages)
            {
                if (args.Message.IsAtLeast(MinLevel))
                {
                    pendingMessages.Add(Tuple.Create((Logger)sender, args.Message));
                    if (!updatePending)
                    {
                        updatePending = true;
                        Dispatcher.InvokeAsync(UpdateMessages);
                    }
                }
            }
        }

        /// <summary>
        /// This methods waits the <see cref="UpdateInterval"/> delay and then updates the <see cref="Messages"/> collection by adding all pending messages.
        /// </summary>
        private async void UpdateMessages()
        {
            if (UpdateInterval >= 0)
            {
                await Task.Delay(UpdateInterval);
            }
            List<Tuple<Logger, ILogMessage>> messagesToAdd = null;
            lock (pendingMessages)
            {
                if (pendingMessages.Count > 0)
                {
                    messagesToAdd = pendingMessages.ToList();
                    pendingMessages.Clear();
                }
                updatePending = false;
            }
            if (messagesToAdd != null)
            {
                foreach (var messageToAdd in messagesToAdd)
                {
                    messages.Add(messageToAdd.Item2);
                    List<ILogMessage> logger;
                    if (Loggers.TryGetValue(messageToAdd.Item1, out logger))
                    {
                        logger.Add(messageToAdd.Item2);
                    }
                }
            }
        }

        /// <summary>
        /// Raised when the messages collection is changed. Updates <see cref="HasWarnings"/> and <see cref="HasErrors"/> properties.
        /// </summary>
        private void MessagesCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (ILogMessage newMessage in e.NewItems)
                    {
                        switch (newMessage.Type)
                        {
                            case LogMessageType.Warning:
                                HasWarnings = true;
                                break;
                            case LogMessageType.Error:
                            case LogMessageType.Fatal:
                                HasErrors = true;
                                break;
                        }
                    }
                }
                HasNewMessages = true;
            }
            else
            {
                HasWarnings = messages.Any(x => x.Type == LogMessageType.Warning);
                HasErrors = messages.Any(x => x.Type == LogMessageType.Error || x.Type == LogMessageType.Fatal);
            }
        }
    }
}
