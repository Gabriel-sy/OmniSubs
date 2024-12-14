using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SubsDownloaderExtension
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");

        public ApiService()
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");
        }

        void SaveToken(string token, string username, string password)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(DATA_PATH)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DATA_PATH)); 
                }
                
                File.WriteAllText(DATA_PATH, token);
                using (var sw = new StreamWriter(DATA_PATH, true))
                {
                    sw.WriteLine("\n" + username);
                    sw.WriteLine(password);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("There was an error when logging you into opensubtitles, try relogging.");
            }
        }
        
        public async void LogIn(string username, string password)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.opensubtitles.com/api/v1/login"),
        
                Content = new StringContent
                ("{\n  \"username\": \"" + username + "\",\n  \"password\": \"" + password +
                 "\"\n}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
        
            using (var response = await _httpClient.SendAsync(request))
            {
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Try logging in again.");
                }
                else
                {
                    var stream = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var responseBody = JsonConvert.DeserializeObject<Jwt>(stream);
                        SaveToken(responseBody.Token, username, password);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
        
                }
        
            }
        }
    }
}
