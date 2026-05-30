using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ScopusSwaggerTester
{
    public class ScopusService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "e0d47adf81d855fff163e582a5a4659b";
        private const string BaseUrl = "https://api.elsevier.com/content";

        public ScopusService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-ELS-APIKey", ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<string> SearchScopusAsync(string query, int count = 25, int start = 0)
        {
            var builder = new UriBuilder($"{BaseUrl}/search/scopus");
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["query"] = query;
            queryParams["count"] = count.ToString();
            queryParams["start"] = start.ToString();
            builder.Query = queryParams.ToString();

            var response = await _httpClient.GetAsync(builder.ToString());
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SearchAuthorAsync(string query, int count = 25, int start = 0)
        {
            var builder = new UriBuilder($"{BaseUrl}/search/author");
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["query"] = query;
            queryParams["count"] = count.ToString();
            queryParams["start"] = start.ToString();
            builder.Query = queryParams.ToString();

            var response = await _httpClient.GetAsync(builder.ToString());
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAuthorDetailsAsync(string authorId)
        {
            var url = $"{BaseUrl}/author/author_id/{authorId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAbstractAsync(string scopusId, string? view = "META_ABS")
        {
            var url = $"{BaseUrl}/abstract/scopus_id/{scopusId}";
            if (!string.IsNullOrEmpty(view))
            {
                url += $"?view={view}";
            }
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAbstractByDoiAsync(string doi, string? view = "META_ABS")
        {
            // DOI needs to be URL encoded or passed as a segment
            var url = $"{BaseUrl}/abstract/doi/{HttpUtility.UrlEncode(doi)}";
            if (!string.IsNullOrEmpty(view))
            {
                url += $"?view={view}";
            }
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetSerialTitleByTitleAsync(string title, string? view = "CITESCORE")
        {
            var builder = new UriBuilder($"{BaseUrl}/serial/title");
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["title"] = title;
            queryParams["view"] = view ?? "CITESCORE";
            builder.Query = queryParams.ToString();
            var response = await _httpClient.GetAsync(builder.ToString());
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SearchScienceDirectAsync(string query, int count = 25, int start = 0)
        {
            var builder = new UriBuilder($"{BaseUrl}/search/scidir");
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["query"] = query;
            queryParams["count"] = count.ToString();
            queryParams["start"] = start.ToString();
            queryParams["field"] = "title,authors,publicationName,coverDate,doi,description,issn,citedByCount,openaccess";
            builder.Query = queryParams.ToString();

            var response = await _httpClient.GetAsync(builder.ToString());
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
