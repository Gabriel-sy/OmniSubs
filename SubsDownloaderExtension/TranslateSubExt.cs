using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace SubsDownloaderExtension
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class TranslateSubExt : SharpContextMenu
    {
        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");
        private Form _loadingForm = new Form
        {
            Text = "Loading...",
            Size = new System.Drawing.Size(200, 100),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.None,
            ControlBox = false
        };
        public TranslateSubExt()
        {
        }
        protected override bool CanShowMenu()
        {
            var supportedExtensions = new[] { ".srt" };
            var fileExtension = Path.GetExtension(SelectedItemPaths.First()).ToLowerInvariant();
            return supportedExtensions.Contains(fileExtension);
        }

        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var tranlateSubtitle = new ToolStripMenuItem
            {
                Text = "Translate subtitle"
            };

            tranlateSubtitle.Click += (sender, e) => TranslateSub();
            
            menu.Items.Add(tranlateSubtitle);

            return menu;
        }

        public async void TranslateSub()
        {
            ShowLoadingBox();
            var fileContent = File.ReadLines(SelectedItemPaths.First()).ToList();
            List<Task<string>> translationTasks = new List<Task<string>>();
            List<List<string>> textsToTranslate = new List<List<string>>();
            StringBuilder translatedTextBuilder = new StringBuilder();
            var service = new GeminiService(10);
            var maxLines = fileContent.Count;
            var cutAmount = (int)Math.Ceiling((double)maxLines / 800);
            var takeAmount = 0;
            var basePrompt =
                $"I have a .srt subtitle file, and i need you to translate it into spanish, " +
                "try to take into consideration the context of the phrasing to translate it correctly, " +
                "do not answer with anything but the translation, also keep the same white spaces and blank lines and do not change the Subtitle Number from the original, your response should follow the following format: Subtitle Number \\n Timestamp: Start and end time for the subtitle, in HH:MM:SS,milliseconds --> HH:MM:SS,milliseconds format.\\n Subtitle Text: One or more lines of text for the subtitle. \\n Blank Line: Separates subtitle blocks";

            while (cutAmount > 0)
            {
                textsToTranslate.Add(fileContent.Skip(takeAmount).Take(800).ToList());
                takeAmount += 800;
                cutAmount--;
            }

            foreach (var list in textsToTranslate)
            {
                var singleString = string.Join(" ", list);
                var prompt = basePrompt + singleString;
                translationTasks.Add(service.TranslateSubtitle(prompt));
            }

            string[] translatedParts = await Task.WhenAll(translationTasks);
            
            foreach (var part in translatedParts)
            {
                translatedTextBuilder.Append(part);
            }
            var translatedText = translatedTextBuilder.ToString();
            
            var fileName = Path.GetFileNameWithoutExtension(SelectedItemPaths.First());
            var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
            var fullPath = Path.Combine(savePath, $"{fileName}.translated.srt");
            File.WriteAllText(fullPath, translatedText);
            CloseLoadingBox();
        }
        
        private void LogDebug(string message)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "ShellExtensionLog.txt");
                
            File.AppendAllText(logPath, 
                $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
        private void ShowLoadingBox()
        {
            Label loadingLabel = new Label
            {
                Text = "Please wait...",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            _loadingForm.Controls.Add(loadingLabel);
            _loadingForm.Show();
        }

        private void CloseLoadingBox()
        {
            _loadingForm.Close();
        }
        
    }
}