using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MithrasSoft.UpdateSankakuComplexFavorites.ServiceAgents
{
    public class SankakucomplexServiceAgent : IDisposable
    {
        private static readonly Uri BaseUri = new Uri("https://chan.sankakucomplex.com/");

        private const string AuthenticateUrl = "/user/authenticate";
        private const string NameParameter = "user[name]";
        private const string PasswordParameter = "user[password]";

        private const string FavoriteCreateUrl = "/favorite/create.json";
        private const string IdParameter = "id";

        private const string OriginalUrlRegex = "Original:.+?href=\"//(.+?)\"";

        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;


        public SankakucomplexServiceAgent()
        {
            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            _httpClient = new HttpClient(httpClientHandler)
            {
                DefaultRequestHeaders =
                {
                    UserAgent =
                    {
                        new ProductInfoHeaderValue("Mozilla", "5.0"),
                        new ProductInfoHeaderValue("AppleWebKit", "537.36"),
                        new ProductInfoHeaderValue("Chrome", "49.0.2623.112"),
                        new ProductInfoHeaderValue("Safari", "537.36"),
                    }
                },
                BaseAddress = BaseUri
            };
            _cookieContainer = httpClientHandler.CookieContainer;
        }


        public async Task Login(string name, string password)
        {
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [NameParameter] = name,
                [PasswordParameter] = password
            });
            var responseMessage = await _httpClient.PostAsync(AuthenticateUrl, formContent);

            var setCookieHeaders = responseMessage.Headers.GetValues("Set-Cookie");
            foreach (var setCookieHeader in setCookieHeaders)
            {
                _cookieContainer.SetCookies(BaseUri, setCookieHeader);
            }
        }

        public async Task<string> GetOriginalUrl(string imageUrl)
        {
            using (var response = await _httpClient.GetAsync(imageUrl))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var originalUrl = Regex.Match(responseContent, OriginalUrlRegex).Groups[1].Value;
                if (string.IsNullOrEmpty(originalUrl))
                {
                    throw new Exception($"GetOriginalUrl failed:{Environment.NewLine}{responseContent}");
                }
                return $"https://{originalUrl}";
            }
        }

        public async Task AddToFavorites(string originalId)
        {
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [IdParameter] = originalId
            });
            using (var response = await _httpClient.PostAsync(FavoriteCreateUrl, formContent))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AddToFavorites failed:{Environment.NewLine}{responseContent}");
                }
            }
        }


        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}