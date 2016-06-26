using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MithrasSoftware.UpdateSankakuComplexFavorites.ServiceAgents
{
    public class IQDBServiceAgent : IDisposable
    {
        private static readonly Uri BaseUri = new Uri("https://iqdb.org/");

        private const string PostUrl = "/";
        private const string FileParameterName = "file";
        private const string ServicesParameterName = "service[]";
        private static readonly Regex MatchRegex = new Regex(@"(?>Best|Additional|Possible) match.+?href=""//(.+?/(\d+))""><img src='.+?/([^/]+?)\.");

        private readonly HttpClient _httpClient;


        public IQDBServiceAgent()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = BaseUri
            };
        }


        public async Task<IEnumerable<IQDBMatch>> FindMatches(string fileName, ServiceEnum[] services)
        {
            using (var formDataContent = new MultipartFormDataContent($"FormBoundary-{Guid.NewGuid()}"))
            {
                var name = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                formDataContent.Add(new StreamContent(File.OpenRead(fileName)), FileParameterName, name);
                foreach (var service in services)
                {
                    formDataContent.Add(new StringContent(((int)service).ToString()), ServicesParameterName);
                }

                using (var response = await _httpClient.PostAsync(PostUrl, formDataContent))
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var matchCollection = MatchRegex.Matches(responseContent);
                    var matches = (from match in matchCollection.Cast<Match>()
                                   select new IQDBMatch
                                   {
                                       Url = $"https://{match.Groups[1].Value}",
                                       Id = match.Groups[2].Value,
                                       Name = match.Groups[3].Value
                                   }).ToList();
                    return matches;
                }
            }
        }


        public void Dispose()
        {
            _httpClient?.Dispose();
        }


        public enum ServiceEnum
        {
            Danbooru = 1,
            Konachan = 2,
            Yandere = 3,
            Gelbooru = 4,
            SankakuChannel = 5,
            Eshuushuu = 6,
            TheAnimeGallery = 10,
            Zerochan = 11,
            MangaDrawing = 12,
            AnimePictures = 13
        }
    }

    public class IQDBMatch
    {
        public string Url { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }
}