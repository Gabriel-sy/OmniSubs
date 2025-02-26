using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SubsDownloaderExtension
{
    public class LoadingForm
    {
        private Form _loadingForm;
        private ProgressBar _progressBar;
        private Label _loadingLabel;
        private Label _percentageLabel;
        private Panel _contentPanel;
        private string _path;
        private bool _isTranslateAndDownload;
        public LoadingForm(string path, bool isTranslateAndDownload)
        {
            _path = path;
            _isTranslateAndDownload = isTranslateAndDownload;
        }

        public void ShowLoadingBox()
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
                var fileName = Path.GetFileNameWithoutExtension(_path);
                var savePath = Path.GetDirectoryName(Path.GetFullPath(_path));
                var fullPath = Path.Combine(savePath, $"{fileName}.translated.srt");
                File.Delete(fullPath);
                CloseLoadingBox();
            };

            if (_isTranslateAndDownload == false)
            {
                _contentPanel.Controls.Add(cancelButton);
            }
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

            _loadingForm.FormClosed += (sender, e) =>
            {
                animationTimer.Stop();
            };

            _loadingForm.Show();
        }

        public void CloseLoadingBox()
        {
            _loadingForm.Close();
            _loadingForm.Dispose();
        }

        public void UpdateProgressBar(int currentPart, int totalParts)
        {
            if (_progressBar != null && !_progressBar.IsDisposed && totalParts > 0)
            {
                int percentage = (int)((double)currentPart / totalParts * 100);

                if (_progressBar.InvokeRequired)
                {
                    _progressBar.Invoke(new Action(() =>
                    {
                        UpdateProgressBarUI(percentage);
                    }));
                }
                else
                {
                    UpdateProgressBarUI(percentage);
                }
            }
        }

        public void UpdateProgressBarUI(int percentage)
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
