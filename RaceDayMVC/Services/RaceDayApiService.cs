using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace RaceDayMVC.Services
{
    public class RaceDayApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string ApiCookieSessionKey = "RaceDayApiSessionCookie";

        public RaceDayApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;

            string? apiBaseUrl = configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new InvalidOperationException(
                    "The API base URL is missing from appsettings.json.");
            }

            _httpClient.BaseAddress = new Uri(apiBaseUrl);
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(
            string endpoint, T data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(data)
            };

            return await SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            return await SendAsync(request);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(
            string endpoint, T data)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = JsonContent.Create(data)
            };

            return await SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            return await SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostMultipartAsync(
            string endpoint, MultipartFormDataContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            return await SendAsync(request);
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            string? apiCookie = httpContext?.Session.GetString(
                ApiCookieSessionKey);

            if (!string.IsNullOrWhiteSpace(apiCookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
            }

            var response = await _httpClient.SendAsync(request);

            if (response.Headers.TryGetValues(
                "Set-Cookie", out var cookieHeaders))
            {
                var sessionCookie = cookieHeaders.FirstOrDefault(cookie =>
                    cookie.StartsWith(
                        ".AspNetCore.Session=",
                        StringComparison.OrdinalIgnoreCase));

                if (sessionCookie != null)
                {
                    string cookieValue = sessionCookie.Split(';')[0];

                    httpContext?.Session.SetString(
                        ApiCookieSessionKey, cookieValue);
                }
            }

            return response;
        }
    }
}