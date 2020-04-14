// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Win32;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Stride.VisualStudio.BuildEngine
{

    /// <summary>
    /// This class implements an MSBuild logger that output events to VS outputwindow and tasklist.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), ComVisible(true)]
    internal sealed class IDEBuildLogger : Logger
    {
        #region fields
        // TODO: Remove these constants when we have a version that suppoerts getting the verbosity using automation.
        private static RegistryKey userRegistryRoot;
        private const string buildVerbosityRegistrySubKey = @"General";
        private const string buildVerbosityRegistryKey = "MSBuildLoggerVerbosity";
        // TODO: Re-enable this constants when we have a version that suppoerts getting the verbosity using automation.
        //private const string EnvironmentCategory = "Environment";
        //private const string ProjectsAndSolutionSubCategory = "ProjectsAndSolution";
        //private const string BuildAndRunPage = "BuildAndRun";

        private int currentIndent;
        private IVsOutputWindowPane outputWindowPane;
        private string errorString = "error";
        private string warningString = "warning";
        private bool isLogTaskDone;
        private TaskProvider taskProvider;
        private IVsHierarchy hierarchy;
        private IServiceProvider serviceProvider;

        #endregion

        #region properties
        public string WarningString
        {
            get { return this.warningString; }
            set { this.warningString = value; }
        }
        public string ErrorString
        {
            get { return this.errorString; }
            set { this.errorString = value; }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsLogTaskDone
        {
            get { return this.isLogTaskDone; }
            set { this.isLogTaskDone = value; }
        }
        /// <summary>
        /// When building from within VS, setting this will
        /// enable the logger to retrive the verbosity from
        /// the correct registry hive.
        /// </summary>
        internal static RegistryKey UserRegistryRoot
        {
            get { return userRegistryRoot; }
            set { userRegistryRoot = value; }
        }
        /// <summary>
        /// Set to null to avoid writing to the output window
        /// </summary>
        internal IVsOutputWindowPane OutputWindowPane
        {
            get { return outputWindowPane; }
            set { outputWindowPane = value; }
        }
        #endregion

        #region ctors
        /// <summary>
        /// Constructor.  Inititialize member data.
        /// </summary>
        public IDEBuildLogger(IVsOutputWindowPane output, TaskProvider taskProvider, IVsHierarchy hierarchy)
        {
            if (taskProvider == null)
                throw new ArgumentNullException("taskProvider");
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            this.taskProvider = taskProvider;
            this.outputWindowPane = output;
            this.hierarchy = hierarchy;
            IOleServiceProvider site;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hierarchy.GetSite(out site));
            this.serviceProvider = new ServiceProvider(site);
        }
        #endregion

        #region overridden methods
        /// <summary>
        /// Overridden from the Logger class.
        /// </summary>
        public override void Initialize(IEventSource eventSource)
        {
            if (null == eventSource)
            {
                throw new ArgumentNullException("eventSource");
            }
            eventSource.BuildStarted += new BuildStartedEventHandler(BuildStartedHandler);
            eventSource.BuildFinished += new BuildFinishedEventHandler(BuildFinishedHandler);
            eventSource.ProjectStarted += new ProjectStartedEventHandler(ProjectStartedHandler);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(ProjectFinishedHandler);
            eventSource.TargetStarted += new TargetStartedEventHandler(TargetStartedHandler);
            eventSource.TargetFinished += new TargetFinishedEventHandler(TargetFinishedHandler);
            eventSource.TaskStarted += new TaskStartedEventHandler(TaskStartedHandler);
            eventSource.TaskFinished += new TaskFinishedEventHandler(TaskFinishedHandler);
            eventSource.CustomEventRaised += new CustomBuildEventHandler(CustomHandler);
            eventSource.ErrorRaised += new BuildErrorEventHandler(ErrorHandler);
            eventSource.WarningRaised += new BuildWarningEventHandler(WarningHandler);
            eventSource.MessageRaised += new BuildMessageEventHandler(MessageHandler);
        }
        #endregion

        #region event delegates
        /// <summary>
        /// This is the delegate for error events.
        /// </summary>
        private void ErrorHandler(object sender, BuildErrorEventArgs errorEvent)
        {
            AddToErrorList(
                errorEvent,
                errorEvent.Code,
                errorEvent.File,
                errorEvent.LineNumber,
                errorEvent.ColumnNumber);
        }

        /// <summary>
        /// This is the delegate for warning events.
        /// </summary>
        private void WarningHandler(object sender, BuildWarningEventArgs errorEvent)
        {
            AddToErrorList(
                errorEvent,
                errorEvent.Code,
                errorEvent.File,
                errorEvent.LineNumber,
                errorEvent.ColumnNumber);
        }

        /// <summary>
        /// Add the error/warning to the error list and potentially to the output window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void AddToErrorList(
            BuildEventArgs errorEvent,
            string errorCode,
            string file,
            int line,
            int column)
        {
            TaskPriority priority = (errorEvent is BuildErrorEventArgs) ? TaskPriority.High : TaskPriority.Normal;
            if (OutputWindowPane != null
                && (this.Verbosity != LoggerVerbosity.Quiet || errorEvent is BuildErrorEventArgs))
            {
                // Format error and output it to the output window
                string message = FormatMessage(errorEvent.Message);
                CompilerError e = new CompilerError(file,
                                                    line,
                                                    column,
                                                    errorCode,
                                                    message);
                e.IsWarning = (errorEvent is BuildWarningEventArgs);

                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(OutputWindowPane.OutputStringThreadSafe(GetFormattedErrorMessage(e)));
            }

            // Add error to task list
            ErrorTask task = new ErrorTask();
            task.Document = file;
            task.Line = line - 1; // The task list does +1 before showing this number.
            task.Column = column;
            task.Text = errorEvent.Message;
            task.Priority = priority;
            task.Category = TaskCategory.BuildCompile;
            task.HierarchyItem = hierarchy;
            task.Navigate += new EventHandler(NavigateTo);
            if (errorEvent is BuildWarningEventArgs)
                task.ErrorCategory = TaskErrorCategory.Warning;
            this.taskProvider.Tasks.Add(task);
        }


        /// <summary>
        /// This is the delegate for Message event types
        /// </summary>		
        private void MessageHandler(object sender, BuildMessageEventArgs messageEvent)
        {
            if (LogAtImportance(messageEvent.Importance))
            {
                LogEvent(sender, messageEvent);
            }
        }

        private void NavigateTo(object sender, EventArgs arguments)
        {
            Microsoft.VisualStudio.Shell.Task task = sender as Microsoft.VisualStudio.Shell.Task;
            if (task == null)
                throw new ArgumentException("Sender is not a Microsoft.VisualStudio.Shell.Task", "sender");

            // Get the doc data for the task's document
            if (String.IsNullOrEmpty(task.Document))
                return;

            IVsUIShellOpenDocument openDoc = serviceProvider.GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (openDoc == null)
                return;

            IVsWindowFrame frame;
            IOleServiceProvider sp;
            IVsUIHierarchy hier;
            uint itemid;
            Guid logicalView = VSConstants.LOGVIEWID_Code;

            if (Microsoft.VisualStudio.ErrorHandler.Failed(openDoc.OpenDocumentViaProject(task.Document, ref logicalView, out sp, out hier, out itemid, out frame)) || frame == null)
                return;

            object docData;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData));

            // Get the VsTextBuffer
            VsTextBuffer buffer = docData as VsTextBuffer;
            if (buffer == null)
            {
                IVsTextBufferProvider bufferProvider = docData as IVsTextBufferProvider;
                if (bufferProvider != null)
                {
                    IVsTextLines lines;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(bufferProvider.GetTextBuffer(out lines));
                    buffer = lines as VsTextBuffer;
                    Debug.Assert(buffer != null, "IVsTextLines does not implement IVsTextBuffer");
                    if (buffer == null)
                        return;
                }
            }

            // Finally, perform the navigation.
            IVsTextManager mgr = serviceProvider.GetService(typeof(VsTextManagerClass)) as IVsTextManager;
            if (mgr == null)
                return;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(mgr.NavigateToLineAndColumn(buffer, ref logicalView, task.Line, task.Column, task.Line, task.Column));
        }

        /// <summary>
        /// This is the delegate for BuildStartedHandler events.
        /// </summary>
        private void BuildStartedHandler(object sender, BuildStartedEventArgs buildEvent)
        {
            if (LogAtImportance(MessageImportance.Low))
            {
                LogEvent(sender, buildEvent);
            }
            // Remove all errors and warnings since we are rebuilding
            taskProvider.Tasks.Clear();
        }

        /// <summary>
        /// This is the delegate for BuildFinishedHandler events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buildEvent"></param>
        private void BuildFinishedHandler(object sender, BuildFinishedEventArgs buildEvent)
        {
            if (LogAtImportance(buildEvent.Succeeded ? MessageImportance.Low :
                                                       MessageImportance.High))
            {
                if (this.outputWindowPane != null)
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(this.outputWindowPane.OutputStringThreadSafe(Environment.NewLine));
                LogEvent(sender, buildEvent);
            }
        }


        /// <summary>
        /// This is the delegate for ProjectStartedHandler events.
        /// </summary>
        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs buildEvent)
        {
            if (LogAtImportance(MessageImportance.Low))
            {
                LogEvent(sender, buildEvent);
            }
        }

        /// <summary>
        /// This is the delegate for ProjectFinishedHandler events.
        /// </summary>
        private void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs buildEvent)
        {
            if (LogAtImportance(buildEvent.Succeeded ? MessageImportance.Low
                                                     : MessageImportance.High))
            {
                LogEvent(sender, buildEvent);
            }
        }

        /// <summary>
        /// This is the delegate for TargetStartedHandler events.
        /// </summary>
        private void TargetStartedHandler(object sender, TargetStartedEventArgs buildEvent)
        {
            if (LogAtImportance(MessageImportance.Normal))
            {
                LogEvent(sender, buildEvent);
            }
            ++this.currentIndent;
        }


        /// <summary>
        /// This is the delegate for TargetFinishedHandler events.
        /// </summary>
        private void TargetFinishedHandler(object sender, TargetFinishedEventArgs buildEvent)
        {
            --this.currentIndent;
            if ((IsLogTaskDone) &&
                LogAtImportance(buildEvent.Succeeded ? MessageImportance.Low
                                                     : MessageImportance.High))
            {
                LogEvent(sender, buildEvent);
            }
        }


        /// <summary>
        /// This is the delegate for TaskStartedHandler events.
        /// </summary>
        private void TaskStartedHandler(object sender, TaskStartedEventArgs buildEvent)
        {
            if (LogAtImportance(MessageImportance.Normal))
            {
                LogEvent(sender, buildEvent);
            }
            ++this.currentIndent;
        }


        /// <summary>
        /// This is the delegate for TaskFinishedHandler events.
        /// </summary>
        private void TaskFinishedHandler(object sender, TaskFinishedEventArgs buildEvent)
        {
            --this.currentIndent;
            if ((IsLogTaskDone) &&
                LogAtImportance(buildEvent.Succeeded ? MessageImportance.Normal
                                                     : MessageImportance.High))
            {
                LogEvent(sender, buildEvent);
            }
        }


        /// <summary>
        /// This is the delegate for CustomHandler events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buildEvent"></param>
        private void CustomHandler(object sender, CustomBuildEventArgs buildEvent)
        {
            LogEvent(sender, buildEvent);
        }

        #endregion

        #region helpers
        /// <summary>
        /// This method takes a MessageImportance and returns true if messages
        /// at importance i should be loggeed.  Otherwise return false.
        /// </summary>
        private bool LogAtImportance(MessageImportance importance)
        {
            // If importance is too low for current settings, ignore the event
            bool logIt = false;

            this.SetVerbosity();

            switch (this.Verbosity)
            {
                case LoggerVerbosity.Quiet:
                    logIt = false;
                    break;
                case LoggerVerbosity.Minimal:
                    logIt = (importance == MessageImportance.High);
                    break;
                case LoggerVerbosity.Normal:
                // Falling through...
                case LoggerVerbosity.Detailed:
                    logIt = (importance != MessageImportance.Low);
                    break;
                case LoggerVerbosity.Diagnostic:
                    logIt = true;
                    break;
                default:
                    Debug.Fail("Unknown Verbosity level. Ignoring will cause everything to be logged");
                    break;
            }

            return logIt;
        }

        /// <summary>
        /// This is the method that does the main work of logging an event
        /// when one is sent to this logger.
        /// </summary>
        private void LogEvent(object sender, BuildEventArgs buildEvent)
        {
            // Fill in the Message text
            if (OutputWindowPane != null && !String.IsNullOrEmpty(buildEvent.Message))
            {
                StringBuilder msg = new StringBuilder(this.currentIndent + buildEvent.Message.Length + 1);
                if (this.currentIndent > 0)
                {
                    msg.Append('\t', this.currentIndent);
                }
                msg.AppendLine(buildEvent.Message);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(this.OutputWindowPane.OutputStringThreadSafe(msg.ToString()));
            }
        }

        /// <summary>
        /// Format error messages for the task list
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private string GetFormattedErrorMessage(CompilerError e)
        {
            if (e == null) return String.Empty;

            string errCode = (e.IsWarning) ? this.WarningString : this.ErrorString;
            StringBuilder fileRef = new StringBuilder();

            if (!string.IsNullOrEmpty(e.FileName))
            {
                fileRef.AppendFormat(CultureInfo.CurrentUICulture, "{0}({1},{2}):",
                                        e.FileName, e.Line, e.Column);
            }
            fileRef.AppendFormat(CultureInfo.CurrentUICulture, " {0} {1}: {2}", errCode, e.ErrorNumber, e.ErrorText);

            return fileRef.ToString();
        }

        /// <summary>
        /// Formats the message that is to be output.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <returns>The new message</returns>
        private static string FormatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Environment.NewLine;
            }

            StringBuilder sb = new StringBuilder(message.Length + Environment.NewLine.Length);

            sb.AppendLine(message);
            return sb.ToString();
        }

        /// <summary>
        /// Sets the verbosity level.
        /// </summary>
        private void SetVerbosity()
        {
            // TODO: This should be replaced when we have a version that supports automation.

            if (userRegistryRoot != null)
            {
                using (RegistryKey subKey = userRegistryRoot.OpenSubKey(buildVerbosityRegistrySubKey))
                {
                    if (subKey != null)
                    {
                        object valueAsObject = subKey.GetValue(buildVerbosityRegistryKey);
                        if (valueAsObject != null)
                        {
                            this.Verbosity = (LoggerVerbosity)((int)valueAsObject);
                        }
                    }
                }
            }

            // TODO: Continue this code to get the Verbosity when we have a version that supports automation to get the Verbosity.
            //EnvDTE.DTE dte = this.serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            //EnvDTE.Properties properties = dte.get_Properties(EnvironmentCategory, ProjectsAndSolutionSubCategory);
        }
        #endregion
    }

}
