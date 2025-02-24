using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
                Text = "Download Subtitle"
            };

            var bulkDownload = new ToolStripMenuItem
            {
                Text = "Download Subtitles for all selected"
            };

            subDownload.Click += (sender, e) => DownloadSub();

            bulkDownload.Click += (sender, e) => BulkDownload();

            menu.Items.Add(subDownload);
            menu.Items.Add(bulkDownload);

            return menu;
        }

        public async void DownloadSub()
        {
            _service.CheckJwtStillValid();
            var language = File.ReadLines(DATA_PATH).Skip(3).Take(1).First();
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            var fileName = Path.GetFileNameWithoutExtension(SelectedItemPaths.First());


            var subtitleSearchResult = await _service.SearchSubtitle
                (token, fileName, language);

            if (subtitleSearchResult == null) return;

            var languageInFileName = File.ReadLines(DATA_PATH).Skip(6).Take(1).First() == "True"
                ? "." + subtitleSearchResult.Language
                : "";

            var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
            var fullPath = Path.Combine(savePath, $"{fileName}{languageInFileName}.srt");

            var downloadUrl = await _service.GetDownloadUrl(token, subtitleSearchResult.SubtitleId);
            if (downloadUrl == null) return;

            byte[] fileBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            File.WriteAllBytes(fullPath, fileBytes);
        }

        public async void BulkDownload()
        {
            var language = File.ReadLines(DATA_PATH).Skip(3).Take(1).First();
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            _service.CheckJwtStillValid();

            foreach (var selectedItemPath in SelectedItemPaths)
            {
                var fileName = Path.GetFileNameWithoutExtension(selectedItemPath);

                var subtitleSearchResult = await _service.SearchSubtitle
                    (token, fileName, language);

                if (subtitleSearchResult == null) return;

                var languageInFileName = File.ReadLines(DATA_PATH).Skip(6).Take(1).First() == "True"
                    ? "." + subtitleSearchResult.Language
                    : "";

                var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
                var fullPath = Path.Combine(savePath, $"{fileName}{languageInFileName}.srt");


                var downloadUrl = await _service.GetDownloadUrl(token, subtitleSearchResult.SubtitleId);

                if (downloadUrl == null) return;

                var fileBytes = await _httpClient.GetByteArrayAsync(downloadUrl);

                File.WriteAllBytes(fullPath, fileBytes);
            }
        }

        private void LogDebug(string message)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "ShellExtensionLog.txt");

            File.AppendAllText(logPath,
                $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }


    public class CandidateResponse
    {
        [JsonProperty("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonProperty("content")]
        public Content Content { get; set; }
    }

    public class Content
    {
        [JsonProperty("parts")]
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}