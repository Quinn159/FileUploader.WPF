using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileUploader.WPF.Services
{
    public class HttpService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HttpService(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(1000);
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}{url}");
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(data),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}{url}", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent);
        }

        public async Task<bool> PutFileChunkAsync(string url, byte[] data, string contentType)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            var response = await _httpClient.PutAsync(url, content);
            return response.IsSuccessStatusCode;
        }
    }
}


