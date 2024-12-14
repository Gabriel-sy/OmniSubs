using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
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
            Directory.CreateDirectory(Path.GetDirectoryName(DATA_PATH));
            File.WriteAllText(DATA_PATH, token);
            using(var sw = new StreamWriter(DATA_PATH, true))
            {
                sw.WriteLine("\n" + username);
                sw.WriteLine(password);
            }
            MessageBox.Show("Login successful!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            MessageBox.Show("There was an error while saving data, try again.");
        }
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
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Username or password incorrect, try again");
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