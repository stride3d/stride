// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Graphics.Regression;

namespace Xenko.ImageComparerService
{
    class ImageComparerClient
    {
        public ImageTestResultConnection Connection = new ImageTestResultConnection();

        public Dictionary<string, List<TestResultServerImage>> Images = new Dictionary<string, List<TestResultServerImage>>();
    }

    class TestResultServerImage
    {
        public TestResultServerImage(ImageComparerClient client)
        {
            Client = client;
            ClientImage = new TestResultImage();
        }

        // Results
        public string GoldPath;
        public string OutputPath;
        public string JsonPath;

        public string GoldFileName;
        public string ResultFileName;
        public string DiffFileName;
        public string DiffNormFileName;
        public float MeanSquareError = -1.0f;

        public int FrameIndex;

        public ImageComparerClient Client;
        public TestResultImage ClientImage;

        public string GetFileName()
        {
            return GetBaseFileName() + ".png";
        }

        public string GetDiffFileName()
        {
            return GetBaseFileName() + "_diff.png";
        }

        public string GetNormDiffFileName()
        {
            return GetBaseFileName() + "_normDiff.png";
        }

        public string GetBaseFileName()
        {
            return string.Format("{0}_v{1}_f{2}", ClientImage.TestName, ClientImage.CurrentVersion, ClientImage.Frame);
        }

        public string GetJsonFileName()
        {
            return string.Format("{0}_{1}_{2}{3}.json", Client.Connection.Platform, Client.Connection.DeviceName, Client.Connection.Serial,
                (Client.Connection.BuildNumber != -1) ? "_build" + Client.Connection.BuildNumber.ToString("D4") : string.Empty);
        }

        public string GetGoldDirectory()
        {
            return string.Format("gold\\{0}_{1}\\", Client.Connection.Platform, Client.Connection.DeviceName);
        }

        public string GetOutputDirectory()
        {
            // Build server format?
            if (Client.Connection.BuildNumber != -1)
                return string.Format("build\\{0}_{1}_{2}\\{3}\\", Client.Connection.Platform, Client.Connection.DeviceName, Client.Connection.Serial, Client.Connection.BuildNumber);

            // User format
            return string.Format("user\\{0}_{1}_{2}\\", Client.Connection.Platform, Client.Connection.DeviceName, Client.Connection.Serial);
        }

        public string GetJsonString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append('{');

            stringBuilder.Append("\"TestName\":\"");
            stringBuilder.Append(ClientImage.TestName);
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append("\"BranchName\":\"");
            stringBuilder.Append(Client.Connection.BranchName ?? "");
            stringBuilder.Append('"');
            stringBuilder.Append(','); 
            stringBuilder.Append("\"Platform\":\"");
            stringBuilder.Append(Client.Connection.Platform);
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append("\"Device\":\"");
            stringBuilder.Append(Client.Connection.DeviceName);
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append("\"Serial\":\"");
            stringBuilder.Append(Client.Connection.Serial);
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append("\"FrameIndex\":");
            stringBuilder.Append(FrameIndex);
            stringBuilder.Append(',');
            stringBuilder.Append("\"BuildNumber\":");
            stringBuilder.Append(Client.Connection.BuildNumber);
            stringBuilder.Append(',');
            stringBuilder.Append("\"ComputedImage\":\"");
            stringBuilder.Append(GetFileName());
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append("\"Error\":");
            stringBuilder.Append(MeanSquareError);
            if (MeanSquareError != 0.0)
            {
                stringBuilder.Append(',');
                stringBuilder.Append("\"DiffImage\":\"");
                stringBuilder.Append(GetDiffFileName());
                stringBuilder.Append('"');
                stringBuilder.Append(',');
                stringBuilder.Append("\"NormDiffImage\":\"");
                stringBuilder.Append(GetNormDiffFileName());
                stringBuilder.Append('"');
            }

            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }
    }
}
