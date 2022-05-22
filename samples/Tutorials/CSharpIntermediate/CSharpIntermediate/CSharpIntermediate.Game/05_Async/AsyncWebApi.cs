// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpIntermediate.Code
{
    public class AsyncWebApi : AsyncScript
    {
        private List<OpenCollectiveEvent> openCollectiveEvents;

        public override async Task Execute()
        {
            openCollectiveEvents = new List<OpenCollectiveEvent>();

            while (Game.IsRunning)
            {
                int drawX = 500, drawY = 600;
                DebugText.Print($"Press A to get Api data from https://opencollective.com/stride3d", new Int2(drawX, drawY));

                if (Input.IsKeyPressed(Stride.Input.Keys.G))
                {
                    await RetrieveStrideRepos();
                }

                foreach (var openCollectiveEvent in openCollectiveEvents)
                {
                    drawY += 20;
                    DebugText.Print(openCollectiveEvent.Name new Int2(drawX, drawY));
                }

                // We have to await the next frame. If we don't do this, our game will be stuck in an infinite loop
                await Script.NextFrame();
            }
        }

        private async Task RetrieveStrideRepos()
        {
            // We can use an HttpClient to make requests to web api's
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://opencollective.com/stride3d/events.json?limit=4");

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // We store the contents of the response in a string
                string responseContent = await response.Content.ReadAsStringAsync();

                // We serialze the string in to an object
                openCollectiveEvents = JsonConvert.DeserializeObject<List<OpenCollectiveEvent>>(responseContent);
            }
        }

        public class OpenCollectiveEvent
        {
            public string Name { get; set; }

            public string StartsAt { get; set; }
        }
    }
}
