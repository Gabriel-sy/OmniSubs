using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private GeminiService _translationService;
        private List<Task<string>> _translationTasks = new List<Task<string>>();
        private bool _isTranslateAndDownload = false;
        private string[] _languages = {
            "English",
            "Chinese",
            "Spanish",
            "Arabic",
            "Portuguese (Brazil)",
            "Portuguese (Portugal)",
            "Indonesian",
            "French",
            "Russian",
            "Japanese",
            "German",
            "Turkish",
            "Italian",
            "Ukrainian",
            "Polish",
            "Dutch",
            "Korean",
            "Hindi",
            "Romanian",
            "Swedish",
            "Danish",
            "Finnish",
            "Norwegian",
            "Serbian",
            "Irish",
            "Filipino"
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
            ToolStripItem[] languageItems = new ToolStripItem[_languages.Length];

            for (int i = 0; i < _languages.Length; i++)
            {
                languageItems[i] = new ToolStripMenuItem
                {
                    Text = _languages[i]
                };
            }

            var tranlateSubtitle = new ToolStripMenuItem
            {
                Text = "Translate subtitle into...",
                Image = Properties.Resources.subs_16
            };
            tranlateSubtitle.DropDownItems.AddRange(languageItems);
            tranlateSubtitle.DropDownDirection = ToolStripDropDownDirection.Default;

            tranlateSubtitle.DropDownItemClicked += (sender, e) =>
                {
                    TranslateSub(e.ClickedItem.Text);
                };

            menu.Items.Add(tranlateSubtitle);

            return menu;
        }

        public async void TranslateSub(string language, string path = "")
        {
            var filePath = path.Length > 1 ? path : SelectedItemPaths.First();
            _isTranslateAndDownload = path.Length > 1;
            var loadingForm = new LoadingForm(filePath, _isTranslateAndDownload);
            loadingForm.ShowLoadingBox();
            try
            {
                var fileContent = File.ReadLines(filePath).ToList();
                List<List<string>> textsToTranslate = new List<List<string>>();
                StringBuilder translatedTextBuilder = new StringBuilder();
                _translationService = new GeminiService(15);
                var maxLines = fileContent.Count;
                var cutAmount = (int)Math.Ceiling((double)maxLines / 800);
                var takeAmount = 0;
                var basePrompt =
                    $"I need you to translate ONLY the following text in the following .srt file into {language}, and return the same .srt, but with the translated phrases, without ```srt or similar:";
                var completedParts = 0;

                while (cutAmount > 0)
                {
                    textsToTranslate.Add(fileContent.Skip(takeAmount).Take(800).ToList());
                    takeAmount += 800;
                    cutAmount--;
                }

                AddTranslationTasks(textsToTranslate, basePrompt);

                foreach (var translationTask in _translationTasks)
                {
                    translationTask.ContinueWith(completed =>
                    {
                        completedParts++;
                        loadingForm.UpdateProgressBar(completedParts, textsToTranslate.Count);
                    });
                }

                string[] translatedParts = await Task.WhenAll(_translationTasks);

                foreach (var part in translatedParts)
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        translatedTextBuilder.Append(part);
                    }

                    if (PartHasResponseError(part)) return;
                }

                var translatedText = translatedTextBuilder.ToString();

                GetFullPathAndWriteText(filePath, translatedText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loadingForm.CloseLoadingBox();
            }

        }

        private void AddTranslationTasks(List<List<string>> textsToTranslate, string basePrompt)
        {
            foreach (var list in textsToTranslate)
            {
                var singleString = string.Join("\n", list);
                var prompt = basePrompt + singleString;
                _translationTasks.Add(_translationService.TranslateSubtitle(prompt));
            }
        }

        private bool PartHasResponseError(string part)
        {
            if (part == "429")
            {
                MessageBox.Show("The limit of requests has been reached. If it doesn't work after a minute, you can either: wait until tomorrow, or get your own Gemini API key and place it in the same window you logged in.");
                return true;
            }
            else if (part == "500")
            {
                MessageBox.Show("There was an internal server error at Google. Please try again later");
                return true;
            }
            else if (part == "503")
            {
                MessageBox.Show("Google services are currently unavailable. Please try again later");
                return true;
            }
            else if (part == "401")
            {
                MessageBox.Show("Invalid Gemini API key");
                return true;
            }
            else if (part == "unknown")
            {
                MessageBox.Show("An unknown error occurred. Please try again later");
                return true;
            }

            return false;
        }

        private void GetFullPathAndWriteText(string filePath, string translatedText)
        {
            if (!string.IsNullOrEmpty(translatedText))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var savePath = Path.GetDirectoryName(Path.GetFullPath(filePath));
                var fullPath = Path.Combine(savePath, $"{fileName}.translated.srt");
                File.WriteAllText(fullPath, translatedText);
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
}