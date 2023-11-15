// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// The chrome tracing profile writer exports diagnotic events into the chrome tracing format.
    /// You view the file using chrome://tracing in your browser.
    /// </summary>
    public class ChromeTracingProfileWriter
    {
        /// <summary>
        /// Create a tracing file at <paramref name="outputPath"/> and start writing events to it.
        /// </summary>
        /// <param name="outputPath">Path where to create the tracing file.</param>
        /// <param name="indentOutput">Whether to indent output JSON. False by default for perfomance/size over readability.</param>
        public void Start(string outputPath, bool indentOutput = false)
        {
            eventReader = Profiler.Subscribe();
            writerTask = Task.Run(async () =>
            {
                var pid = Process.GetCurrentProcess().Id;

                using FileStream fs = File.Create(outputPath, 1024 * 1024);
                using var writer = new Utf8JsonWriter(fs, options: new JsonWriterOptions { Indented = indentOutput, SkipValidation = true });

                JsonObject root = new JsonObject();

                writer.WriteStartObject();
                writer.WriteStartArray("traceEvents");

                writer.WriteStartObject();
                writer.WriteString("name", "thread_name");
                writer.WriteString("ph", "M");
                writer.WriteNumber("pid", pid);
                writer.WriteNumber("tid", int.MaxValue);
                writer.WriteStartObject("args");
                writer.WriteString("name", "GPU");
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteStartObject();
                writer.WriteString("name", "thread_sort_index");
                writer.WriteString("ph", "M");
                writer.WriteNumber("pid", pid);
                writer.WriteNumber("tid", int.MaxValue);
                writer.WriteStartObject("args");
                writer.WriteNumber("sort_index", int.MaxValue);
                writer.WriteEndObject();
                writer.WriteEndObject();

                await foreach (var e in eventReader.ReadAllAsync())
                {
                    //gc scopes currently start at negative timestamps and should be filtered out,
                    //because they don't represent durations.
                    if (e.TimeStamp.Ticks < 0)
                        continue;

                    double startTimeInMicroseconds = e.TimeStamp.TotalMilliseconds * 1000.0;
                    double durationInMicroseconds = e.ElapsedTime.TotalMilliseconds * 1000.0;

                    Debug.Assert(durationInMicroseconds >= 0);

                    writer.WriteStartObject();
                    writer.WriteString("name", e.Key.Name);
                    if (e.Key.Parent != null)
                        writer.WriteString("cat", e.Key.Parent.Name);
                    writer.WriteString("ph", "X");
                    writer.WriteNumber("ts", startTimeInMicroseconds);
                    writer.WriteNumber("dur", durationInMicroseconds);
                    writer.WriteNumber("tid", e.ThreadId >= 0 ? e.ThreadId : int.MaxValue);
                    writer.WriteNumber("pid", pid);
                    if (e.Attributes.Count > 0)
                    {
                        writer.WriteStartObject("args");
                        foreach (var (k, v) in e.Attributes)
                        {
                            writer.WriteString(k, v.ToString());
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();

                    if (writer.BytesPending >= 1024 * 1024)
                    {
                        await writer.FlushAsync();
                    }
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            });
        }

        /// <summary>
        /// Stop the profiling session and wait for the file to be flushed.
        /// </summary>
        public void Stop()
        {
            if (eventReader != null)
            {
                Profiler.Unsubscribe(eventReader);
                writerTask?.Wait();
            }
        }

#nullable enable
        ChannelReader<ProfilingEvent>? eventReader;
        Task? writerTask;
#nullable disable
    }

}
