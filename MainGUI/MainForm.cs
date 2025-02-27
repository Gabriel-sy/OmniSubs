using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MainGUI
{
    public partial class MainForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private CheckBox chkHearingImpaired;
        private CheckBox chkLanguageCode;
        private ComboBox cmbPrimaryLanguage;
        private ComboBox cmbSecondaryLanguage;
        private TextBox txtGeminiApiKey;
        private LinkLabel lnkGeminiKey;
        private Button btnInstall;

        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");

        public MainForm()
        {
            this.Text = "Subtitle Manager";
            this.Size = new Size(550, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            SetupControls();

            PopulateLanguages();

            _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");

            btnLogin.Click += async (s, e) => await BtnLogin_Click(s, e);

            btnInstall.Click += BtnInstall_Click;
        }

        private void SetupControls()
        {
            this.Text = "Subtitle Manager";
            this.Size = new Size(550, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Subtitle Manager",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Size = new Size(300, 30),
                Location = new Point(30, 20),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            this.Controls.Add(lblTitle);

            GroupBox grpLogin = new GroupBox
            {
                Text = "Opensubtitles.com login",
                Size = new Size(490, 180),
                Location = new Point(30, 60),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(grpLogin);

            Label lblUsername = new Label
            {
                Text = "Login:",
                Size = new Size(100, 20),
                Location = new Point(20, 30),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpLogin.Controls.Add(lblUsername);

            txtUsername = new TextBox
            {
                Size = new Size(250, 30),
                Location = new Point(20, 50),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };
            grpLogin.Controls.Add(txtUsername);

            Label lblPassword = new Label
            {
                Text = "Password:",
                Size = new Size(100, 20),
                Location = new Point(20, 80),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpLogin.Controls.Add(lblPassword);

            txtPassword = new TextBox
            {
                Size = new Size(250, 30),
                Location = new Point(20, 100),
                PasswordChar = '•',
                BorderStyle = BorderStyle.FixedSingle
            };
            grpLogin.Controls.Add(txtPassword);

            btnLogin = new Button
            {
                Text = "Login",
                Size = new Size(250, 33),
                Location = new Point(20, 130),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            grpLogin.Controls.Add(btnLogin);

            GroupBox grpConfig = new GroupBox
            {
                Text = "Config",
                Size = new Size(490, 290),
                Location = new Point(30, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(grpConfig);

            Label lblPrimaryLang = new Label
            {
                Text = "Primary Language:",
                Size = new Size(130, 20),
                Location = new Point(20, 30),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpConfig.Controls.Add(lblPrimaryLang);

            cmbPrimaryLanguage = new ComboBox
            {
                Size = new Size(150, 25),
                Location = new Point(20, 50),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpConfig.Controls.Add(cmbPrimaryLanguage);

            Label lblSecondaryLang = new Label
            {
                Text = "Secondary Language:",
                Size = new Size(130, 20),
                Location = new Point(200, 30),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpConfig.Controls.Add(lblSecondaryLang);

            cmbSecondaryLanguage = new ComboBox
            {
                Size = new Size(150, 25),
                Location = new Point(200, 50),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            grpConfig.Controls.Add(cmbSecondaryLanguage);

            chkHearingImpaired = new CheckBox
            {
                Text = "Download hearing impaired subtitles",
                Size = new Size(250, 20),
                Location = new Point(20, 90),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpConfig.Controls.Add(chkHearingImpaired);

            chkLanguageCode = new CheckBox
            {
                Text = "Put language code in subtitle name",
                Size = new Size(250, 20),
                Location = new Point(20, 120),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            grpConfig.Controls.Add(chkLanguageCode);

            Panel pnlGemini = new Panel
            {
                Size = new Size(450, 70),
                Location = new Point(20, 160),
                BackColor = Color.FromArgb(230, 240, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            grpConfig.Controls.Add(pnlGemini);

            Label lblGeminiApiKey = new Label
            {
                Text = "Gemini API Key (Optional):",
                Size = new Size(180, 20),
                Location = new Point(10, 10),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            pnlGemini.Controls.Add(lblGeminiApiKey);

            txtGeminiApiKey = new TextBox
            {
                Size = new Size(350, 25),
                Location = new Point(10, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlGemini.Controls.Add(txtGeminiApiKey);

            lnkGeminiKey = new LinkLabel
            {
                Text = "Get API Key",
                Size = new Size(100, 20),
                Location = new Point(370, 30),
                LinkColor = Color.FromArgb(0, 102, 204)
            };
            lnkGeminiKey.Click += new EventHandler(LnkGeminiKey_Click);
            pnlGemini.Controls.Add(lnkGeminiKey);

            btnInstall = new Button
            {
                Text = "Install",
                Size = new Size(120, 35),
                Location = new Point(350, 240),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            grpConfig.Controls.Add(btnInstall);
        }

        private void PopulateLanguages()
        {
            var languageMap = new Dictionary<string, string>
            {
                { "English", "en" },
                { "Spanish", "es" },
                { "Mandarin Chinese", "zh" },
                { "Hindi", "hi" },
                { "Arabic", "ar" },
                { "Bengali", "bn" },
                { "Portuguese (Brazil)", "pt-BR" },
                { "Portuguese (Portugal)", "pt-PT" },
                { "Russian", "ru" },
                { "Japanese", "ja" },
                { "German", "de" },
                { "French", "fr" },
                { "Italian", "it" },
                { "Korean", "ko" },
                { "Vietnamese", "vi" },
                { "Turkish", "tr" },
                { "Polish", "pl" },
                { "Ukrainian", "uk" },
                { "Persian", "fa" },
                { "Thai", "th" },
                { "Dutch", "nl" },
                { "Greek", "el" },
                { "Czech", "cs" },
                { "Swedish", "sv" },
                { "Romanian", "ro" },
                { "Hungarian", "hu" },
                { "Danish", "da" },
                { "Finnish", "fi" },
                { "Norwegian", "no" },
                { "Slovak", "sk" },
                { "Croatian", "hr" },
                { "Bulgarian", "bg" },
                { "Hebrew", "he" },
                { "Lithuanian", "lt" },
                { "Slovenian", "sl" },
                { "Estonian", "et" },
                { "Latvian", "lv" },
                { "Serbian", "sr" },
                { "Indonesian", "id" }
            };

            foreach (string language in languageMap.Keys)
            {
                cmbPrimaryLanguage.Items.Add(language);
                cmbSecondaryLanguage.Items.Add(language);
            }

            if (cmbPrimaryLanguage.Items.Count > 0)
                cmbPrimaryLanguage.SelectedIndex = 0;

            if (cmbSecondaryLanguage.Items.Count > 1)
                cmbSecondaryLanguage.SelectedIndex = 1;
        }

        private void LnkGeminiKey_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://makersuite.google.com/app/apikey",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;
            string jsonContent = $"{{ \"username\": \"{username}\", \"password\": \"{password}\" }}";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.opensubtitles.com/api/v1/login")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            try
            {
                using (HttpResponseMessage response = await _httpClient.SendAsync(request))
                {
                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        MessageBox.Show("The API is limited. Please wait a bit.");
                        return;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        MessageBox.Show("Incorrect username or password, try again.");
                        return;
                    }
                    else
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        var jwt = await JsonSerializer.DeserializeAsync<Jwt>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (jwt != null && !string.IsNullOrEmpty(jwt.Token))
                        {
                            SaveLoginData(jwt.Token, username, password);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login error: " + ex.Message);
            }
        }

        private void SaveLoginData(string token, string username, string password)
        {
            try
            {
                var primaryLanguage = "";
                var secondaryLanguage = "";
                var hearingImpaired = false;
                var languageCodeFlag = false;
                var geminiKey = "";
                if (File.Exists(DATA_PATH) && File.ReadLines(DATA_PATH).Count() > 3)
                {
                    var fileText = File.ReadLines(DATA_PATH).ToList();
                    primaryLanguage = fileText[3];
                    secondaryLanguage = fileText[4];
                    hearingImpaired = fileText[5] == "True";
                    languageCodeFlag = fileText[6] == "True";
                    geminiKey = fileText[7];

                    string dir = Path.GetDirectoryName(DATA_PATH);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    using (StreamWriter sw = new StreamWriter(DATA_PATH, false))
                    {
                        sw.WriteLine(token);
                        sw.WriteLine(username);
                        sw.WriteLine(password);
                        sw.WriteLine(primaryLanguage);
                        sw.WriteLine(secondaryLanguage);
                        sw.WriteLine(hearingImpaired);
                        sw.WriteLine(languageCodeFlag);
                        sw.WriteLine(geminiKey.Length > 1 ? geminiKey : "False");
                    }
                    MessageBox.Show("Login data saved successfully!");
                }
                else
                {
                    string dir = Path.GetDirectoryName(DATA_PATH);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    using (StreamWriter sw = new StreamWriter(DATA_PATH, false))
                    {
                        sw.WriteLine(token);
                        sw.WriteLine(username);
                        sw.WriteLine(password);
                    }
                    MessageBox.Show("Login data saved successfully!");
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving login data: " + ex.Message);
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            SaveConfigData();
        }

        private void SaveConfigData()
        {
            try
            {
                var token = File.ReadLines(DATA_PATH).Take(1).First();
                var login = File.ReadLines(DATA_PATH).Skip(1).Take(1).First();
                var password = File.ReadLines(DATA_PATH).Skip(2).Take(1).First();
                string primaryLanguage = cmbPrimaryLanguage.SelectedItem?.ToString() ?? "English";
                string secondaryLanguage = cmbSecondaryLanguage.SelectedItem?.ToString() ?? "Spanish";

                string primaryLangCode = GetLanguageCode(primaryLanguage);
                string secondaryLangCode = GetLanguageCode(secondaryLanguage);
                bool hearingImpaired = chkHearingImpaired.Checked;
                bool languageCodeFlag = chkLanguageCode.Checked;
                string geminiKey = txtGeminiApiKey.Text.Trim();

                using (StreamWriter sw = new StreamWriter(DATA_PATH, false))
                {
                    sw.WriteLine(token);
                    sw.WriteLine(login);
                    sw.WriteLine(password);
                    sw.WriteLine(primaryLangCode);
                    sw.WriteLine(secondaryLangCode);
                    sw.WriteLine(hearingImpaired);
                    sw.WriteLine(languageCodeFlag);
                    sw.WriteLine(geminiKey.Length > 1 ? geminiKey : "False");
                }
                RunBatchFile();
                MessageBox.Show("Configuration saved successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration: " + ex.Message);
            }
        }

        private void RunBatchFile()
        {
            var batchFilePath = Path.Combine(Application.StartupPath, "setup.bat");
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = batchFilePath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing the bat file: {batchFilePath}");
            }
        }

        private string GetLanguageCode(string languageName)
        {
            var languageMap = new Dictionary<string, string>
            {
                { "English", "en" },
                { "Spanish", "es" },
                { "Mandarin Chinese", "zh" },
                { "Hindi", "hi" },
                { "Arabic", "ar" },
                { "Bengali", "bn" },
                { "Portuguese (Brazil)", "pt-BR" },
                { "Portuguese (Portugal)", "pt-PT" },
                { "Russian", "ru" },
                { "Japanese", "ja" },
                { "German", "de" },
                { "French", "fr" },
                { "Italian", "it" },
                { "Korean", "ko" },
                { "Vietnamese", "vi" },
                { "Turkish", "tr" },
                { "Polish", "pl" },
                { "Ukrainian", "uk" },
                { "Persian", "fa" },
                { "Thai", "th" },
                { "Dutch", "nl" },
                { "Greek", "el" },
                { "Czech", "cs" },
                { "Swedish", "sv" },
                { "Romanian", "ro" },
                { "Hungarian", "hu" },
                { "Danish", "da" },
                { "Finnish", "fi" },
                { "Norwegian", "no" },
                { "Slovak", "sk" },
                { "Croatian", "hr" },
                { "Bulgarian", "bg" },
                { "Hebrew", "he" },
                { "Lithuanian", "lt" },
                { "Slovenian", "sl" },
                { "Estonian", "et" },
                { "Latvian", "lv" },
                { "Serbian", "sr" },
                { "Indonesian", "id" }
            };

            if (languageMap.TryGetValue(languageName, out string code))
            {
                return code;
            }
            return "";
        }
    }

    public class Jwt
    {
        public string Token { get; set; }
    }
}
