using System.Net.Http;
using System.Threading.Tasks;
using Stride.Engine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSharpIntermediate.Code
{
    public class AsyncWebApi : AsyncScript
    {
        public override async Task Execute()
        {

            while (Game.IsRunning)
            {
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
                var githubRepos = JsonConvert.DeserializeObject<List<OpenCollectiveEvent>>(responseContent);

                foreach (var repo in githubRepos)
                {
                    Log.Info($"{repo.Name} took place at {repo.StartsAt}");
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
