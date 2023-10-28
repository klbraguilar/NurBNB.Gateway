using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Brotli;
using Newtonsoft.Json.Linq;
using NurBNB.Gateway.Web.DTO;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using System.Net;
using System.Net.Http.Headers;
using System.IO.Compression;

namespace NurBNB.Gateway.Web.Aggregator
{
    public class GatewayAggregator : IDefinedAggregator
    {
        public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {
            List<Posts> posts = new List<Posts>();
            List<User> users = new List<User>();

            foreach (var response in responses)
            {
                string downStreamRouteKey = ((DownstreamRoute)response.Items["DownstreamRoute"]).Key;
                DownstreamResponse downstreamResponse = (DownstreamResponse)response.Items["DownstreamResponse"];
                byte[] downstreamResponseContent = await downstreamResponse.Content.ReadAsByteArrayAsync();

                if (downStreamRouteKey == "posts")
                {
                    posts = JsonConvert.DeserializeObject<List<Posts>>(DeCompressBrotli(downstreamResponseContent));
                }

                if (downStreamRouteKey == "users")
                {
                    users = JsonConvert.DeserializeObject<List<User>>(DeCompressBrotli(downstreamResponseContent));
                }
            }
            return PostByUsername(posts, users);
        }

        public DownstreamResponse PostByUsername(List<Posts> posts, List<User> users)
        {
            JObject postsByUserName = new JObject();
            var postsByUserID = posts.GroupBy(n => n.id);

            foreach (var post in postsByUserID)
            {
                string userName = users.Find(n => n.id == post.Key).name;
                var selectPost = JsonConvert.SerializeObject(post.Select(n => new { n.id, n.title, n.body }));
                var selectPostString = JsonConvert.DeserializeObject<JArray>(selectPost);
                postsByUserName.Add(new JProperty(userName, selectPostString));
            }

            var postsByUsernameString = JsonConvert.SerializeObject(postsByUserName);
            var stringContent = new StringContent(postsByUsernameString)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            return new DownstreamResponse(stringContent, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "OK");
        }

        private string DeCompressBrotli(byte[] xResponseContent)
        {
            int bytesWritten;
            var output = new byte[4096]; // Ajusta el tamaño del búfer de salida según tus necesidades.

            if (BrotliDecoder.TryDecompress(xResponseContent, output, out bytesWritten))
            {
                return System.Text.Encoding.UTF8.GetString(output, 0, bytesWritten);
            }
            else
            {
                // Handle decompression failure here
                return "Decompression failed";
            }
        }
    }
}
