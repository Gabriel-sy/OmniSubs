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
    [COMServerAssociation(AssociationType.AllFiles)]
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
            var supportedExtensions = new[] { ".mp4", ".mkv", ".avi" };
            var fileExtension = Path.GetExtension(SelectedItemPaths.First()).ToLowerInvariant();
            return supportedExtensions.Contains(fileExtension);
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
            _service.CheckJwtStillValid();
            
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            var fileName = Path.GetFileNameWithoutExtension(SelectedItemPaths.First());
            
            var subtitleId = await _service.SearchSubtitle
                (token, fileName);

            if (subtitleId == null) return;
            
            var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
            
            var fullPath = Path.Combine(savePath, $"{fileName}.srt");

            var downloadUrl = await _service.GetDownloadUrl(token, subtitleId);

            if (downloadUrl == null) return;
            
            byte[] fileBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            
            File.WriteAllBytes(fullPath, fileBytes);
        }
    }
    
}