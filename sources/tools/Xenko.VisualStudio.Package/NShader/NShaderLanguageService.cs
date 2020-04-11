#region Header Licence
//  ---------------------------------------------------------------------
// 
//  Copyright (c) 2009 Alexandre Mutel and Microsoft Corporation.  
//  All rights reserved.
// 
//  This code module is part of NShader, a plugin for visual studio
//  to provide syntax highlighting for shader languages (hlsl, glsl, cg)
// 
//  ------------------------------------------------------------------
// 
//  This code is licensed under the Microsoft Public License. 
//  See the file License.txt for the license details.
//  More info on: http://nshader.codeplex.com
// 
//  ------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using Xenko.VisualStudio.Classifiers;
using Xenko.VisualStudio.Commands;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VsShell = Microsoft.VisualStudio.Shell.VsShellUtilities;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Xenko.VisualStudio;

namespace NShader
{
    public class NShaderLanguageService : LanguageService
    {
        private VisualStudioThemeEngine themeEngine;
        private NShaderColorableItem[] m_colorableItems;

        private readonly ErrorListProvider errorListProvider;

        private LanguagePreferences m_preferences;
        private int lastChangeCount = -1;
        private readonly Stopwatch clock;

        private const int TriggerParsingDelayInMs = 1000; // Parse after 1s of inactivity and a change in source code

        public NShaderLanguageService(ErrorListProvider errorListProvider)
        {
            this.errorListProvider = errorListProvider;
            clock = new Stopwatch();
        }

        public void InitializeColors()
        {
            // Check if already initialized
            if (m_colorableItems != null)
                return;

            // Initialize theme engine
            themeEngine = new VisualStudioThemeEngine(Site);
            themeEngine.OnThemeChanged += themeEngine_OnThemeChanged;

            var currentTheme = themeEngine.GetCurrentTheme();

            m_colorableItems = new NShaderColorableItem[]
                                   {
                                        /*1*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Keyword", "Xenko Shader Language - Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_AQUAMARINE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(86, 156, 214), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                        /*2*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Comment", "Xenko Shader Language - Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_GREEN, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(87, 166, 74), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                        /*3*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Identifier", "Xenko Shader Language - Identifier", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
                                        /*4*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.String", "Xenko Shader Language - String", COLORINDEX.CI_RED, COLORINDEX.CI_RED, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(214, 157, 133), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                        /*5*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Number", "Xenko Shader Language - Number", COLORINDEX.CI_DARKBLUE, COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(181, 206, 168), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                        /*6*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Intrinsic", "Xenko Shader Language - Intrinsic", COLORINDEX.CI_MAROON, COLORINDEX.CI_CYAN, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(239, 242, 132), Color.Empty, FONTFLAGS.FF_BOLD),
                                        /*7*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Special", "Xenko Shader Language - Special", COLORINDEX.CI_AQUAMARINE, COLORINDEX.CI_MAGENTA, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(78, 201, 176), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                        /*8*/ new NShaderColorableItem(currentTheme, "Xenko.ShaderLanguage.Preprocessor", "Xenko Shader Language - Preprocessor", COLORINDEX.CI_DARKGRAY, COLORINDEX.CI_LIGHTGRAY, COLORINDEX.CI_USERTEXT_BK, Color.Empty, Color.FromArgb(155, 155, 155), Color.Empty, FONTFLAGS.FF_DEFAULT),
                                   };
        }

        public override void Dispose()
        {
            themeEngine.OnThemeChanged -= themeEngine_OnThemeChanged;
            themeEngine.Dispose();

            base.Dispose();
        }

        public override ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView)
        {
            return new NShaderViewFilter(this, mgr, newView);
        }

        void themeEngine_OnThemeChanged(object sender, EventArgs e)
        {
            var colorUtilities = Site.GetService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorUtilities;
            var currentTheme = themeEngine.GetCurrentTheme();
            var isDarkTheme = currentTheme == VisualStudioTheme.Dark || currentTheme == VisualStudioTheme.UnknownDark;

            var store = Package.GetGlobalService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorStorage;
            if (store == null)
                return;

            if (store.OpenCategory(DefGuidList.guidTextEditorFontCategory, (uint)(__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES)) != VSConstants.S_OK)
                return;

            try
            {
                // Update each colorable item
                foreach (var colorableItem in m_colorableItems)
                {
                    string canonicalName;
                    var colorInfos = new ColorableItemInfo[1];
                    if (colorableItem.GetCanonicalName(out canonicalName) == VSConstants.S_OK && store.GetItem(canonicalName, colorInfos) == VSConstants.S_OK)
                    {
                        // Get new color
                        var hiColor = isDarkTheme ? colorableItem.HiForeColorDark : colorableItem.HiForeColorLight;
                        var colorIndex = isDarkTheme ? colorableItem.ForeColorDark : colorableItem.ForeColorLight;

                        if (hiColor != Color.Empty)
                            colorInfos[0].crForeground = hiColor.R | ((uint)hiColor.G << 8) | ((uint)hiColor.B << 16);
                        else
                            colorUtilities.EncodeIndexedColor(colorIndex, out colorInfos[0].crForeground);

                        // Update color in settings
                        store.SetItem(canonicalName, colorInfos);
                    }
                }
            }
            finally
            {
                store.CloseCategory();
            }
        }

        public override int GetItemCount(out int count)
        {
            count = m_colorableItems.Length;
            return VSConstants.S_OK;
        }

        public override int GetColorableItem(int index, out IVsColorableItem item)
        {
            if (index < 1)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            item = m_colorableItems[index-1];
            return VSConstants.S_OK;
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (m_preferences == null)
            {
                m_preferences = new LanguagePreferences(this.Site,
                                                        typeof(NShaderLanguageService).GUID,
                                                        this.Name);
                m_preferences.Init();
            }
            return m_preferences;
        }

        public override IScanner GetScanner(IVsTextLines buffer)
        {
            string filePath = FilePathUtilities.GetFilePath(buffer);
            // Return dynamic scanner based on file extension
            return NShaderScannerFactory.GetShaderScanner(filePath);
        }

        public override Source CreateSource(IVsTextLines buffer)
        {
            return new NShaderSource(this, buffer, GetColorizer(buffer));
        }

        public override Colorizer GetColorizer(IVsTextLines buffer)
        {
            // Clear font cache
            // http://social.msdn.microsoft.com/Forums/office/en-US/54064c52-727d-4015-af70-c72e44d116a7/vs2012-fontandcolors-text-editor-category-for-language-service-colors?forum=vsx
            IVsFontAndColorStorage storage;
            Guid textMgrIID = new Guid(
//#if VISUALSTUDIO_11_0
		            "{E0187991-B458-4F7E-8CA9-42C9A573B56C}" /* 'Text Editor Language Services Items' category discovered in the registry. Resetting TextEditor has no effect. */
//#else
//		            FontsAndColorsCategory.TextEditor
//#endif
	        );
	        if (null != (storage = GetService(typeof(IVsFontAndColorStorage)) as IVsFontAndColorStorage) &&
		        VSConstants.S_OK == storage.OpenCategory(ref textMgrIID, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)))
	        {
		        bool missingColor = false;
		        try
		        {
			        ColorableItemInfo[] info = new ColorableItemInfo[1];
                    for (int i = 0; i < m_colorableItems.Length; ++i)
                    {
                        string colorName;
                        m_colorableItems[i].GetDisplayName(out colorName);
				        if (ErrorHandler.Failed(storage.GetItem(colorName, info)))
				        {
					        missingColor = true;
					        break;
				        }
			        }
		        }
		        finally
		        {
			        storage.CloseCategory();
		        }
		        if (missingColor)
		        {
			        IOleServiceProvider oleProvider;
			        // The service and interface guids are different, so we need to go to the OLE layer to get the service
			        Guid iid = typeof(IVsFontAndColorCacheManager).GUID;
			        Guid sid = typeof(SVsFontAndColorCacheManager).GUID;
			        IntPtr pCacheManager;
			        if (null != (oleProvider = GetService(typeof(IOleServiceProvider)) as IOleServiceProvider) &&
				        VSConstants.S_OK == oleProvider.QueryService(ref sid, ref iid, out pCacheManager) &&
				        pCacheManager != IntPtr.Zero)
			        {
				        try
				        {
					        IVsFontAndColorCacheManager cacheManager = (IVsFontAndColorCacheManager)Marshal.GetObjectForIUnknown(pCacheManager);
					        cacheManager.ClearCache(ref textMgrIID);
				        }
				        finally
				        {
					        Marshal.Release(pCacheManager);
				        }
			        }
		        }
            }

            return base.GetColorizer(buffer);
        }

        public override async void OnIdle(bool periodic)
        {
            var source = GetCurrentNShaderSource();
            if (source != null)
            {
                if (lastChangeCount != source.ChangeCount)
                {
                    clock.Restart();
                    lastChangeCount = source.ChangeCount;
                }

                if (clock.IsRunning)
                {
                    if (clock.ElapsedMilliseconds > TriggerParsingDelayInMs)
                    {
                        clock.Stop();
                        clock.Reset();

                        var text = source.GetText();
                        var sourcePath = source.GetFilePath();

                        var projectFile = LocateProject(sourcePath);

                        Trace.WriteLine(string.Format("Parsing Change: {0} Time: {1}", source.ChangeCount, DateTime.Now));
                        try
                        {
                            var result = await Task.Run(() => AnalyzeAndGoToDefinition(projectFile, text, new RawSourceSpan(sourcePath, 1, 1))).ConfigureAwait(true);
                            OutputAnalysisMessages(result, source);
                        }
                        catch (Exception ex)
                        {
                            lock (errorListProvider)
                            {
                                errorListProvider.Tasks.Add(new ErrorTask(ex.InnerException ?? ex));
                            }
                        }

                    }
                }
            }

            base.OnIdle(periodic);
        }

        public string LocateProject(string sourcePath)
        {
            // Try to locate containing project
            var dte = (DTE)GetService(typeof(DTE));
            var projectItem = dte.Solution.FindProjectItem(sourcePath);
            string projectFile = null;
            if (projectItem != null && projectItem.ContainingProject != null && !string.IsNullOrEmpty(projectItem.ContainingProject.FileName))
            {
                projectFile = projectItem.ContainingProject.FileName;
            }

            return projectFile;
        }

        public RawShaderNavigationResult AnalyzeAndGoToDefinition(string projectFile, string text, RawSourceSpan span)
        {
            return XenkoCommandsProxy.GetProxy()?.AnalyzeAndGoToDefinition(projectFile, text, span) ?? new RawShaderNavigationResult();
        }

        private NShaderSource GetCurrentNShaderSource()
        {
            IVsTextView vsTextView = this.LastActiveTextView;
            if (vsTextView == null)
                return null;
            return GetNShaderSource(vsTextView);
        }

        private NShaderSource GetNShaderSource(IVsTextView textView)
        {
            return this.GetSource(textView) as NShaderSource;
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            // req.FileName
            return new TestAuthoringScope();
        }

        public override string GetFormatFilterList()
        {
            return "";
        }

        public override string Name
        {
            get { return "Xenko Shader Language"; }
        }

        public void OutputAnalysisAndGotoLocation(RawShaderNavigationResult result, IVsTextView textView)
        {
            if (result == null) throw new ArgumentNullException("result");
            if (textView == null) throw new ArgumentNullException("textView");
            var source = GetNShaderSource(textView);

            OutputAnalysisMessages(result, source);
            GoToLocation(result.DefinitionSpan, null, false);
        }

        private void OutputAnalysisMessages(RawShaderNavigationResult result, NShaderSource source = null)
        {
            lock (errorListProvider)
            {
                try
                {
                    var taskProvider = source != null ? source.GetTaskProvider() : null;
                    if (taskProvider != null)
                    {
                        taskProvider.Tasks.Clear();
                    }

                    errorListProvider.Tasks.Clear(); // clear previously created
                    foreach (var message in result.Messages)
                    {
                        var errorCategory = TaskErrorCategory.Message;
                        if (message.Type == "warning")
                        {
                            errorCategory = TaskErrorCategory.Warning;
                        }
                        else if (message.Type == "error")
                        {
                            errorCategory = TaskErrorCategory.Error;
                        }

                        // Make sure that we won't pass nay null to VS as it will crash it
                        var filePath = message.Span.File ?? string.Empty;
                        var messageText = message.Text ?? string.Empty;


                        if (taskProvider != null && errorCategory == TaskErrorCategory.Error)
                        {
                            var task = source.CreateErrorTaskItem(ConvertToTextSpan(message.Span), filePath, messageText, TaskPriority.High, TaskCategory.CodeSense, MARKERTYPE.MARKER_CODESENSE_ERROR, TaskErrorCategory.Error);
                            taskProvider.Tasks.Add(task);
                        }
                        else
                        {
                            var newError = new ErrorTask()
                            {
                                ErrorCategory = errorCategory,
                                Category = TaskCategory.BuildCompile,
                                Text = messageText,
                                Document = filePath,
                                Line = Math.Max(0, message.Span.Line - 1),
                                Column = Math.Max(0, message.Span.Column - 1),
                                // HierarchyItem = hierarchyItem // TODO Add hierarchy the file is associated to
                            };

                            // Install our navigate to source 
                            newError.Navigate += NavigateToSourceError;
                            errorListProvider.Tasks.Add(newError); // add item
                        }
                    }

                    if (result.Messages.Count > 0)
                    {
                        errorListProvider.Show(); // make sure it is visible 
                    }
                    else
                    {
                        errorListProvider.Refresh();
                    }

                    if (taskProvider != null)
                    {
                        taskProvider.Refresh();
                    }
                }
                catch (Exception ex)
                {
                    errorListProvider.Tasks.Add(new ErrorTask(ex.InnerException ?? ex));
                }
            }
        }

        private static TextSpan ConvertToTextSpan(RawSourceSpan span)
        {
            return new TextSpan()
            {
                iStartIndex = Math.Max(0, span.Column-1),
                iStartLine = Math.Max(0, span.Line-1),
                iEndIndex = Math.Max(0, span.EndColumn-1),
                iEndLine = Math.Max(0, span.EndLine-1)
            };
        }

        private void NavigateToSourceError(object sender, EventArgs e)
        {
            var task = sender as Microsoft.VisualStudio.Shell.Task;
            if (task != null)
            {
                GoToLocation(new RawSourceSpan(task.Document, task.Line + 1, task.Column + 1), null, false);
            }
        }

        private void GoToLocation(RawSourceSpan loc, string caption, bool asReadonly)
        {
            // Code taken from Nemerle https://github.com/rsdn/nemerle/blob/master/snippets/VS2010/Nemerle.VisualStudio/LanguageService/NemerleLanguageService.cs#L565
            // TODO: Add licensing
            if (loc == null || loc.File == null)
                return;

            // Opens the document
            var span = new TextSpan { iStartLine = loc.Line - 1, iStartIndex = loc.Column - 1, iEndLine = loc.EndLine - 1, iEndIndex = loc.EndColumn - 1 };
            uint itemID;
            IVsUIHierarchy hierarchy;
            IVsWindowFrame docFrame;
            IVsTextView textView;
            VsShell.OpenDocument(Site, loc.File, VSConstants.LOGVIEWID_Code, out hierarchy, out itemID, out docFrame, out textView);

            // If we need readonly, set the buffer to read-only
            if (asReadonly)
            {
                IVsTextLines buffer;
                ErrorHandler.ThrowOnFailure(textView.GetBuffer(out buffer));
                var stream = (IVsTextStream)buffer;
                stream.SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            }

            // Need to use a different caption?
            if (caption != null)
            {
                ErrorHandler.ThrowOnFailure(docFrame.SetProperty((int)__VSFPROPID.VSFPROPID_OwnerCaption, caption));
            }

            // Show the frame
            ErrorHandler.ThrowOnFailure(docFrame.Show());

            // Go to the specific location
            if (textView != null && loc.Line != 0)
            {
                try
                {
                    ErrorHandler.ThrowOnFailure(textView.SetCaretPos(span.iStartLine, span.iStartIndex));
                    TextSpanHelper.MakePositive(ref span);
                    //ErrorHandler.ThrowOnFailure(textView.SetSelection(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex));
                    ErrorHandler.ThrowOnFailure(textView.EnsureSpanVisible(span));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }

        internal class TestAuthoringScope : AuthoringScope
        {

            public override string GetDataTipText(int line, int col, out TextSpan span)
            {
                span = new TextSpan();
                return null;
            }

            public override Declarations GetDeclarations(IVsTextView view,
                                                         int line,
                                                         int col,
                                                         TokenInfo info,
                                                         ParseReason reason)
            {
                return null;
            }

            public override Methods GetMethods(int line, int col, string name)
            {
                return null;
            }

            public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
            {
                span = new TextSpan();
                return null;
            }
        }

    }
}
