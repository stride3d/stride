// ***********************************************************************
// Copyright (c) 2009 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnitLite.Runner;

namespace NUnitLite.Tests
{
    class Program
    {
        // The main program executes the tests. Output may be routed to
        // various locations, depending on the arguments passed.
        //
        // Arguments:
        //
        //  Arguments may be names of assemblies or options prefixed with '/'
        //  or '-'. Normally, no assemblies are passed and the calling
        //  assembly (the one containing this Main) is used. The following
        //  options are accepted:
        //
        //    -test:<testname>  Provides the name of a test to be exected.
        //                      May be repeated. If this option is not used,
        //                      all tests are run.
        //
        //    -out:PATH         Path to a file to which output is written.
        //                      If omitted, Console is used, which means the
        //                      output is lost on a platform with no Console.
        //
        //    -full             Print full report of all tests.
        //
        //    -result:PATH      Path to a file to which the XML test result is written.
        //
        //    -explore[:Path]   If specified, list tests rather than executing them. If a
        //                      path is given, an XML file representing the tests is written
        //                      to that location. If not, output is to the Console.
        //
        //    -noheader,noh     Suppress display of the initial message.
        //
        //    -wait             Wait for a keypress before exiting.
        //
        // Examples:
        //
        //  Sending output to the console works on desktop Windows and Windows CE.
        //
        //      new ConsoleUI().Execute( args );
        //
        //  Debug output works on all platforms.
        //
        //      new DebugUI().Execute( args );
        //
        //  Sending output to a file works on all platforms, but care must be taken
        //  to write to an accessible location on some platforms.
        //
        //      string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        //      string path = System.IO.Path.Combine( myDocs, "TestResult.txt" );
        //      System.IO.TextWriter writer = new System.IO.StreamWriter(output);
        //      new TextUI( writer ).Execute( args );
        //      writer.Close();
        //
        //  NOTE: Sending output to TCP doesn't work yet
        //
        static void Main(string[] args)
        {
            new TextUI().Execute(args);
        }
    }
}