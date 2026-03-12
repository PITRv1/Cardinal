using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;

namespace Cardinal.Backend
{
    public class GenerateContentSimpleText
    {
        public static async Task main()
        {
            Console.WriteLine("yellow");
            // The client gets the API key from the environment variable `GEMINI_API_KEY`.
            var client = new Client();
            var response = await client.Models.GenerateContentAsync(
              model: "gemini-3-flash-preview", contents: "Explain how AI works in a few words"
            );
            Console.WriteLine(response.Candidates[0].Content.Parts[0].Text);
        }
    }
    public class AISolver : ISolver
    {
        readonly HttpClient http = new();

        readonly Map map;
        int hours;
        readonly List<List<Point>> clusters;
        public List<LogEntry> MissionLog { get; set; } = new();

        public AISolver(Map map, List<List<Point>> clusters, int hours)
        {
            this.map = map;
            this.hours = hours;
            this.clusters = clusters;
        }
        public Result Solve()
        {
            //var prompt = BuildPrompt(map, hours);
            //var response = CallAPI(prompt).Result;
            //var order = ParseResponse(response);
            //var bruh = new GenerateContentSimpleText();
            var respone = CallAPIA("bruh");
            //Console.WriteLine(order.Length);
            return new Result();
        }
        async Task<string> CallAPIA(string prompt)
        {
            await GenerateContentSimpleText.main();
            return "bruh";
        }
        string BuildPrompt(Map map, int hours)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are planning a Mars rover route.");
            sb.AppendLine($"Time budget: {hours * 2} ticks. Each mining op = 1 tick. Movement speed ~2 blocks/tick.");
            sb.AppendLine("Return the cluster visit order as a JSON array of cluster indices, best first.");
            sb.AppendLine("Clusters (index, size, dist_from_start, dist_to_home):");
            foreach (var c in clusters)
            {
                double cx = c.Average(t => t.X);
                double cy = c.Average(t => t.Y);
                var representative = c.OrderBy(t =>
                    Math.Abs(t.X - cx) + Math.Abs(t.Y - cy)).First();
                var distToHome = Pathfinder.AStar(map, map.Start, representative);
                var distFromStart = Pathfinder.AStar(map, representative, map.Start);
                if (distToHome != null && distFromStart != null)
                {
                    sb.AppendLine($"  {{\"i\":{clusters.IndexOf(c)},\"size\":{c.Count},\"d_start\":{distFromStart.Points.Count - 1},\"d_home\":{distToHome.Points.Count - 1}}}");
                }
            }
            sb.AppendLine("Respond ONLY with: {\"order\": [1, 5, 3, ...]}");
            return sb.ToString();
        }
    }
}
