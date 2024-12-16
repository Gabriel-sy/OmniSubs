using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LoginWindow.Entities;

namespace LoginWindow;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly string DATA_PATH = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SubtitleDownloader",
        "data.txt");

    public MainWindow()
    {
        InitializeComponent();
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");
    }

    void SaveToken(string token, string username, string password)
    {
        try
        {
            var selectedLanguage = ((ComboBoxItem)LanguageSelector.SelectedItem).Content.ToString() ?? "English";

            var languageCode = GetLanguageCode(selectedLanguage);

            if (!Directory.Exists(Path.GetDirectoryName(DATA_PATH)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DATA_PATH)); 
            }
            
            File.WriteAllText(DATA_PATH, token);
            using(var sw = new StreamWriter(DATA_PATH, true))
            {
                sw.WriteLine("\n" + username);
                sw.WriteLine(password);
                sw.WriteLine(languageCode);
            }
            MessageBox.Show("Login successful!");
        }
        catch (SystemException e)
        {
            MessageBox.Show("There was an error while saving your data, try again.");
        }
    }

    static string GetLanguageCode(string languageName)
    {
        var languageMap = new Dictionary<string, string>
        {
            { "English", "en" },
            { "Spanish (Español)", "es" },
            { "Mandarin Chinese (中文)", "zh" },
            { "Hindi (हिन्दी)", "hi" },
            { "Arabic (العربية)", "ar" },
            { "Bengali (বাংলা)", "bn" },
            { "Portuguese (Brazil)", "pt-BR" },
            { "Portuguese (Portugal)", "pt-PT" },
            { "Russian (Русский)", "ru" },
            { "Japanese (日本語)", "ja" },
            { "German (Deutsch)", "de" },
            { "French (Français)", "fr" },
            { "Italian (Italiano)", "it" },
            { "Korean (한국어)", "ko" },
            { "Vietnamese (Tiếng Việt)", "vi" },
            { "Turkish (Türkçe)", "tr" },
            { "Polish (Polski)", "pl" },
            { "Ukrainian (Українська)", "uk" },
            { "Persian (فارسی)", "fa" },
            { "Thai (ไทย)", "th" },
            { "Dutch (Nederlands)", "nl" },
            { "Greek (Ελληνικά)", "el" },
            { "Czech (Čeština)", "cs" },
            { "Swedish (Svenska)", "sv" },
            { "Romanian (Română)", "ro" },
            { "Hungarian (Magyar)", "hu" },
            { "Danish (Dansk)", "da" },
            { "Finnish (Suomi)", "fi" },
            { "Norwegian (Norsk)", "no" },
            { "Slovak (Slovenčina)", "sk" },
            { "Croatian (Hrvatski)", "hr" },
            { "Bulgarian (Български)", "bg" },
            { "Hebrew (עברית)", "he" },
            { "Lithuanian (Lietuvių)", "lt" },
            { "Slovenian (Slovenščina)", "sl" },
            { "Estonian (Eesti)", "et" },
            { "Latvian (Latviešu)", "lv" },
            { "Serbian (Српски)", "sr" },
            { "Indonesian (Bahasa Indonesia)", "id" },
            { "Malay (Bahasa Melayu)", "ms" },
            { "Tagalog (Filipino)", "tl" },
            { "Urdu (اردو)", "ur" },
            { "Tamil (தமிழ்)", "ta" },
            { "Telugu (తెలుగు)", "te" },
            { "Kannada (ಕನ್ನಡ)", "kn" },
            { "Malayalam (മലയാളം)", "ml" },
            { "Marathi (मराठी)", "mr" },
            { "Gujarati (ગુજરાતી)", "gu" },
            { "Punjabi (ਪੰਜਾਬੀ)", "pa" }
        };
        languageMap.TryGetValue(languageName, out string languageCode);

        return languageCode;
    }

    async void ButtonClick(object sender, RoutedEventArgs e)
    {
        var username = UsernameInput.Text;
        var password = PasswordInput.Password;
        
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
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                MessageBox.Show("The API is limited. This is a opensubtitles issue, it usually fixes itself so just wait a bit, but could take up to an hour.");
            } else if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Incorrect username or password, try again.");
            }
            else
            {
                var stream = await response.Content.ReadAsStreamAsync();

                var responseBody = JsonSerializer.Deserialize<Jwt>(stream, _serializerOptions);
                
                if (responseBody is not null)
                {
                    SaveToken(responseBody.Token, username, password);
                }
                
            }
            
        }
    }
}