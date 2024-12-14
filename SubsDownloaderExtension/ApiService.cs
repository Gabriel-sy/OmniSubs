using System.Net.Http;

namespace SubsDownloaderExtension
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");
        }

        
    }
}