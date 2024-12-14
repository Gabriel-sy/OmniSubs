using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
        public void DownloadSub()
        {
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            var username = File.ReadLines(DATA_PATH).Skip(1).Take(1).First();
            var password = File.ReadLines(DATA_PATH).Skip(2).Take(1).First();
            
            
        }
    }
    
}