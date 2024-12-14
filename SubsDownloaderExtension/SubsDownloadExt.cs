using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace SubsDownloaderExtension
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".mp4", ".mkv", ".avi")]
    public class SubsDownloadExt : SharpContextMenu
    {
        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");
        private readonly HttpClient _httpClient;
        private ApiService _service = new ApiService();

        public SubsDownloadExt()
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");
        }
        
        protected override bool CanShowMenu()
        {
            return true;
        }
        
        private void LogDebug(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "ShellExtensionLog.txt");
                
                File.AppendAllText(logPath, 
                    $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch { }
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var subDownload = new ToolStripMenuItem
            {
                Text = "Download Subtitles"
            };

            subDownload.Click += (sender, e) => DownloadSub();
            
            menu.Items.Add(subDownload);

            return menu;
        }
        public async void DownloadSub()
        {
            CheckJwtStillValid();
            
            var token = File.ReadLines(DATA_PATH).Take(1).First();

            var subtitleId = await SearchSubtitle(token);
            
            var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
            
            var fullPath = Path.Combine(savePath, $"{Path.GetFileNameWithoutExtension(SelectedItemPaths.First())}.srt");

            var downloadUrl = await GetDownloadUrl(token, subtitleId);
            
            byte[] fileBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            
            File.WriteAllBytes(fullPath, fileBytes);
        }

        public async Task<string> GetDownloadUrl(string token, string subtitleId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.opensubtitles.com/api/v1/download"),
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                },
                Content = new StringContent("{\n  \"file_id\": " + subtitleId + "\n}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
            using (var response = await _httpClient.SendAsync(request))
            {
                var body = await response.Content.ReadAsStringAsync();
                
                var responseBody = JsonConvert.DeserializeObject<DownloadResult>(body);
                return responseBody.Link;
            }
        }

        public async void CheckJwtStillValid()
        {
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            var username = File.ReadLines(DATA_PATH).Skip(1).Take(1).First();
            var password = File.ReadLines(DATA_PATH).Skip(2).Take(1).First();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.opensubtitles.com/api/v1/download"),
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                },
                Content = new StringContent("{\n  \"file_id\": 8964616\n}")
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
                    _service.LogIn(username, password);
                }
            }
        }
        
        public async Task<string> SearchSubtitle(string token)
        {
            var language = File.ReadLines(DATA_PATH).Skip(3).Take(1).First();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = 
                    new Uri($"https://api.opensubtitles.com/api/v1/subtitles?query={Path.GetFileNameWithoutExtension(SelectedItemPaths.First())}&languages={language}"),
                Headers =
                {
                    { "User-Agent", "subsdown" },
                    { "Authorization", $"Bearer {token}" },
                    { "Api-Key", "4QvhsW4PzmhnDLkome6HhV3R26mg4Dht" },
                },
            };
            using (var response = await _httpClient.SendAsync(request))
            {
                
                var body = await response.Content.ReadAsStringAsync();
                
                var responseBody = JsonConvert.DeserializeObject<Result>(body);
                var data = responseBody.Data.OrderByDescending(s => s.Attributes.From_trusted)
                    .ThenByDescending(s => s.Attributes.New_download_count).ThenByDescending(s => s.Attributes.Download_count);
                return data.First().Attributes.Files.First().File_id.ToString();
            }
        }
        
        
    }
    
}