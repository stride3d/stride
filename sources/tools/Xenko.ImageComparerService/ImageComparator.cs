// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xenko.ImageComparerService;

namespace Xenko.Graphics.Regression
{
    class ImageComparator
    {
        #region Private static members

        /// <summary>
        /// A object to prevent concurrent json writing.
        /// </summary>
        private static readonly object WriterLocker = new object();

        /// <summary>
        /// Image Magick bin directory.
        /// </summary>
        private static string imageMagickDir;

        /// <summary>
        /// The directory where the gold images are.
        /// </summary>
        private const string GoldFolder = @"gold\";

        /// <summary>
        /// The directory where the json files should be saved.
        /// </summary>
        private const string JsonFolder = @"json\";

        /// <summary>
        /// The directory where to save the generated images.
        /// </summary>
        private const string BuildFolder = @"build\";

        #endregion

        /// <summary>
        /// A flag to enable the json file writing.
        /// </summary>
        public bool SaveJson;

        public ImageComparator()
        {
            imageMagickDir = System.Environment.GetEnvironmentVariable("IMAGEMAGICK_DIR");
            if (imageMagickDir == null)
                imageMagickDir = Environment.CurrentDirectory;

            SaveJson = false;
        }

        /// <summary>
        /// Performs the comparison between the generated image and its base.
        /// </summary>
        /// <param name="receivedImage">The received image.</param>
        /// <returns></returns>
        public bool Compare_RMSE(TestResultServerImage receivedImage, string resultTempFileName)
        {
            bool copyOnShare = (receivedImage.Client.Connection.Flags & ImageComparisonFlags.CopyOnShare) != 0;

            var testPerformed = false;

            // Test if gold file exists (if not, create it)
            if (!File.Exists(receivedImage.GoldFileName))
            {
                Console.WriteLine(@"Generating a new gold image.");
                File.Delete(receivedImage.ResultFileName); // remove the old version of the result file, if it already exists.
                File.Copy(resultTempFileName, receivedImage.ResultFileName);
                File.Copy(resultTempFileName, receivedImage.GoldFileName);
                receivedImage.MeanSquareError = 0; // we don't want a error on request status after creating a new gold image
            }
            else
            {
                var tempGoldFileName = Path.GetTempPath() + Guid.NewGuid() + ".png";
                var tempDiffFileName = Path.GetTempPath() + Guid.NewGuid() + ".png";

                try
                {
                    File.Copy(receivedImage.GoldFileName, tempGoldFileName);

                    Console.WriteLine(@"Gold image found. Performing tests.");
                    var output = RunImageMagickCommand(
                        @"compare",
                        string.Format(
                            "-metric rmse \"{0}\" \"{1}\" \"{2}\"",
                            tempGoldFileName,
                            resultTempFileName,
                            tempDiffFileName));

                    var outputResult = Regex.Match(output.OutputErrors[0], @"(\S*) \((\S*)\)");
                    var mse = 0.0f;
                    if (!float.TryParse(outputResult.Groups[1].Value, out mse))
                    {
                        var stringBuild = new StringBuilder();
                        stringBuild.Append("Errors:\n");
                        foreach (var err in output.OutputErrors)
                        {
                            stringBuild.Append("    ");
                            stringBuild.Append(err);
                            stringBuild.Append("\n");
                        }
                        stringBuild.Append("Messages:\n");
                        foreach (var mess in output.OutputLines)
                        {
                            stringBuild.Append("    ");
                            stringBuild.Append(mess);
                            stringBuild.Append("\n");
                        }

                        Console.WriteLine(@"Unable to read the value of the error between the two images.");
                        Console.WriteLine(stringBuild.ToString());
                    }
                    else
                    {
                        Console.WriteLine(@"Error: " + mse);

                        //copy files to server
                        if (mse != 0.0f)
                        {
                            Console.WriteLine(@"Existing difference with gold image. Continuing tests.");

                            // visually compute difference
                            RunImageMagickCommand(
                                @"composite",
                                string.Format(
                                    "\"{0}\" \"{1}\" -compose difference \"{2}\"",
                                    tempGoldFileName,
                                    resultTempFileName,
                                    tempDiffFileName));

                            // normalize the output
                            RunImageMagickCommand(
                                @"convert",
                                string.Format(
                                    "\"{0}\" -auto-level \"{1}\"",
                                    tempDiffFileName,
                                    receivedImage.DiffNormFileName));

                            File.Copy(resultTempFileName, receivedImage.ResultFileName, true);
                            File.Copy(tempDiffFileName, receivedImage.DiffFileName, true);
                        }
                        else
                        {
                            Console.WriteLine(@"No difference with gold image.");

                            // Delete diff image (that might have been generated by previous run)
                            File.Delete(receivedImage.DiffFileName);
                            File.Delete(receivedImage.DiffNormFileName);
                        }

                        receivedImage.MeanSquareError = mse;

                        if (SaveJson)
                            WriteJson(receivedImage);

                        testPerformed = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"An exception occurred in ImageMagick.");
                    Console.WriteLine(ex);
                }
                finally
                {
                    File.Delete(tempDiffFileName);
                    File.Delete(tempGoldFileName);
                }
            }
            return testPerformed;
        }

        #region Private methods

        /// <summary>
        /// Run an imageMagick shell command.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="commandParameters">The arguments of the command.</param>
        /// <returns>The output of the command.</returns>
        private static ProcessOutputs RunImageMagickCommand(string commandName, string commandParameters)
        {
            return ShellHelper.RunProcessAndGetOutput(Path.Combine(imageMagickDir, commandName), commandParameters);
        }
        
        /// <summary>
        /// Get or Create the Json file for this build.
        /// </summary>
        /// <param name="receivedImage">The received image information.</param>
        /// <returns>The file stream.</returns>
        private Stream GetJsonFileStream(TestResultServerImage receivedImage)
        {
            var fileName = GetJsonFileName(receivedImage);
            bool fileExists = File.Exists(fileName);
            var stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            
            if (fileExists)
            {
                stream.Seek(-1, SeekOrigin.End);
                WriteInStream(",", stream);
            }
            else
            {
                WriteInStream("[", stream);
            }
            return stream;
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Write a string in the stream.
        /// </summary>
        /// <param name="name">The string to write.</param>
        /// <param name="stream">The stream to write into.</param>
        public static void WriteInStream(string name, Stream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(name), 0, Encoding.ASCII.GetByteCount(name));
        }

        /// <summary>
        /// Get the name of the json for this test.
        /// </summary>
        /// <param name="receivedImage">The received image information.</param>
        /// <returns>The name of the json file.</returns>
        private static string GetJsonFileName(TestResultServerImage receivedImage)
        {
            return Path.Combine(receivedImage.JsonPath, receivedImage.GetJsonFileName());
        }

        #endregion

        /// <summary>
        /// Add the image to the json.
        /// </summary>
        /// <param name="receivedImage">The image to add.</param>
        /// <param name="baseImage">The address of the base image.</param>
        private void WriteJson(TestResultServerImage receivedImage)
        {
            lock (WriterLocker)
            {
                using (var stream = GetJsonFileStream(receivedImage))
                {
                    var newEntry = receivedImage.GetJsonString();
                    WriteInStream(newEntry, stream);
                    WriteInStream("]", stream);
                }
            }
        }
    }
}
