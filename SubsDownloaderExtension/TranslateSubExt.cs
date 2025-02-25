using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Form _loadingForm;
        private ProgressBar _progressBar;
        private Label _loadingLabel;
        private Label _percentageLabel;
        private Panel _contentPanel;
        private GeminiService _translationService;
        private List<Task<string>> _translationTasks = new List<Task<string>>();
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

        public async void TranslateSub(string language)
        {
            ShowLoadingBox();
            try
            {
                var fileContent = File.ReadLines(SelectedItemPaths.First()).ToList();
                List<List<string>> textsToTranslate = new List<List<string>>();
                StringBuilder translatedTextBuilder = new StringBuilder();
                _translationService = new GeminiService(15);
                var maxLines = fileContent.Count;
                var cutAmount = (int)Math.Ceiling((double)maxLines / 800);
                var takeAmount = 0;
                var basePrompt =
                    $"I need you to translate ONLY the following text in the following .srt file into {language}, and return the same .srt, but with the translated phrases, without ```srt or similar:";
                var totalParts = textsToTranslate.Count;
                var completedParts = 0;

                while (cutAmount > 0)
                {
                    textsToTranslate.Add(fileContent.Skip(takeAmount).Take(800).ToList());
                    takeAmount += 800;
                    cutAmount--;
                }

                totalParts = textsToTranslate.Count;

                foreach (var list in textsToTranslate)
                {
                    var singleString = string.Join("\n", list);
                    var prompt = basePrompt + singleString;
                    _translationTasks.Add(_translationService.TranslateSubtitle(prompt));
                }

                foreach (var translationTask in _translationTasks)
                {
                    translationTask.ContinueWith(completed =>
                    {
                        completedParts++;
                        UpdateProgressBar(completedParts, totalParts);
                    });
                }

                string[] translatedParts = await Task.WhenAll(_translationTasks);
                
                if (_loadingForm == null || _loadingForm.IsDisposed)
                {
                    return;
                }

                foreach (var part in translatedParts)
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        translatedTextBuilder.Append(part);
                    } else if (string.IsNullOrEmpty(part))
                    {
                        return;
                    }

                    if (part == "429")
                    {
                        MessageBox.Show("The limit of requests has been reached. It could be the daily or minute limit, if it doesn't work in a minute, try again tomorrow.");
                        return;
                    } else if (part == "500")
                    {
                        MessageBox.Show("There was an internal server error at Google. Please try again later");
                        return;
                    } else if (part == "503")
                    {
                        MessageBox.Show("Google services are currently unavailable. Please try again later");
                        return;
                    } else if (part == "unknown")
                    {
                        MessageBox.Show("An unknown error occurred. Please try again later");
                        return;
                    }
                }

                var translatedText = translatedTextBuilder.ToString();

                if (!string.IsNullOrEmpty(translatedText))
                {
                    var fileName = Path.GetFileNameWithoutExtension(SelectedItemPaths.First());
                    var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
                    var fullPath = Path.Combine(savePath, $"{fileName}.translated.srt");
                    File.WriteAllText(fullPath, translatedText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _translationService?.Dispose();
                _translationService = null;
                CloseLoadingBox(); 
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
        private void ShowLoadingBox()
{
    _loadingForm = new Form
    {
        Text = "Translating Subtitle",
        Size = new Size(400, 200),
        StartPosition = FormStartPosition.CenterScreen,
        FormBorderStyle = FormBorderStyle.None,
        ControlBox = false,
        BackColor = Color.White,
        ShowInTaskbar = true
    };

    _loadingForm.FormBorderStyle = FormBorderStyle.None;
    _loadingForm.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, _loadingForm.Width, _loadingForm.Height, 15, 15));

    _contentPanel = new Panel
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(25),
        BackColor = Color.White
    };

    var titleLabel = new Label
    {
        Text = "Subtitle Translation",
        Dock = DockStyle.Top,
        TextAlign = ContentAlignment.TopCenter,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.FromArgb(47, 85, 151),
        Padding = new Padding(0, 0, 0, 5),
        Height = 30
    };

    _loadingLabel = new Label
    {
        Text = "Please wait while your subtitle is being translated...",
        Dock = DockStyle.Top,
        TextAlign = ContentAlignment.TopCenter,
        Font = new Font("Segoe UI", 10, FontStyle.Regular),
        ForeColor = Color.FromArgb(80, 80, 80),
        Padding = new Padding(0, 5, 0, 20),
        Height = 50
    };

    _progressBar = new ProgressBar
    {
        Dock = DockStyle.Top,
        Style = ProgressBarStyle.Continuous,
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        Height = 18
    };

    _progressBar.ForeColor = Color.FromArgb(75, 139, 197);
    _progressBar.BackColor = Color.FromArgb(240, 240, 240);

    _percentageLabel = new Label
    {
        Text = "0%",
        Dock = DockStyle.Top,
        TextAlign = ContentAlignment.TopRight,
        Font = new Font("Segoe UI", 9, FontStyle.Regular),
        ForeColor = Color.FromArgb(100, 100, 100),
        Padding = new Padding(0, 3, 0, 0),
        Height = 30
    };

    var cancelButton = new Button
    {
        Text = "Cancel",
        Dock = DockStyle.Bottom,
        FlatStyle = FlatStyle.Flat,
        Height = 30,
        ForeColor = Color.FromArgb(80, 80, 80),
        Font = new Font("Segoe UI", 9, FontStyle.Regular),
        Cursor = Cursors.Hand
    };
    cancelButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
    cancelButton.Click += (sender, e) => 
    {
        _translationService?.CancelAllOperations();
        var fileName = Path.GetFileNameWithoutExtension(SelectedItemPaths.First());
        var savePath = Path.GetDirectoryName(Path.GetFullPath(SelectedItemPaths.First()));
        var fullPath = Path.Combine(savePath, $"{fileName}.translated.srt");
        File.Delete(fullPath);
        CloseLoadingBox();
    };

    _contentPanel.Controls.Add(cancelButton);
    _contentPanel.Controls.Add(_percentageLabel);
    _contentPanel.Controls.Add(_progressBar);
    _contentPanel.Controls.Add(_loadingLabel);
    _contentPanel.Controls.Add(titleLabel);

    _loadingForm.Controls.Add(_contentPanel);

    Timer animationTimer = new Timer { Interval = 200 };
    string[] animationText = { "translated", "translated.", "translated..", "translated..." };
    int animationIndex = 0;
    
    animationTimer.Tick += (sender, e) =>
    {
        _loadingLabel.Text = "Please wait while your subtitle is being " + animationText[animationIndex];
        animationIndex = (animationIndex + 1) % animationText.Length;
    };
    
    animationTimer.Start();
    
    _loadingForm.FormClosed += (sender, e) => {
        animationTimer.Stop();
    };

    _loadingForm.Show();
}

private void CloseLoadingBox()
{
    if (_loadingForm != null && !_loadingForm.IsDisposed)
    {
        _loadingForm.Close();
        _loadingForm.Dispose();
    }
}

private void UpdateProgressBar(int currentPart, int totalParts)
{
    if (_progressBar != null && !_progressBar.IsDisposed && totalParts > 0)
    {
        int percentage = (int)((double)currentPart / totalParts * 100);
        
        if (_progressBar.InvokeRequired)
        {
            _progressBar.Invoke(new Action(() => {
                UpdateProgressBarUI(percentage);
            }));
        }
        else
        {
            UpdateProgressBarUI(percentage);
        }
    }
}

private void UpdateProgressBarUI(int percentage)
{
    if (percentage >= 0 && percentage <= 100)
    {
        _progressBar.Value = percentage;
        _percentageLabel.Text = $"{percentage}%";
    }
    else if (percentage > 100)
    {
        _progressBar.Value = 100;
        _percentageLabel.Text = "100%";
    }
    else
    {
        _progressBar.Value = 0;
        _percentageLabel.Text = "0%";
    }
    
    Application.DoEvents();
}

[DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
private static extern IntPtr CreateRoundRectRgn(
    int nLeftRect,
    int nTopRect,
    int nRightRect,
    int nBottomRect,
    int nWidthEllipse,
    int nHeightEllipse
);
    }
}