using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpIntermediate.Code
{
    public class AsyncWebApi : AsyncScript
    {
        public override async Task Execute()
        {

            while (Game.IsRunning)
            {
                DebugText.Print($"Press G to load Api data and Log it", new Int2(500, 200));
                if (Input.IsKeyPressed(Stride.Input.Keys.G)){
                    await RetrieveStrideRepos();
                }

                await Script.NextFrame();
            }
        }

        private async Task RetrieveStrideRepos()
        {
            var sw = new Stopwatch();
            sw.Start();

            // We can use an HttpClient to make requests to web api's
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://opencollective.com/stride3d/events.json?limit=4");

            Log.Info(sw.Elapsed.ToString());

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // We store the contents of the response in a string
                string responseContent = await response.Content.ReadAsStringAsync();

                // We serialze the string in to an object
                var openCollectiveEvents = JsonConvert.DeserializeObject<List<OpenCollectiveEvent>>(responseContent);

                foreach (var @event in openCollectiveEvents)
                {
                    Log.Info($"{@event.Name} took place at {@event.StartsAt}");
                }
            }
        }

        public class OpenCollectiveEvent
        {
            public string Name { get; set; }

            public string StartsAt { get; set; }
        }
    }
}
