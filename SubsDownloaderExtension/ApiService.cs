﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SubsDownloaderExtension
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");

        public int Limit = 1;

        public ApiService()
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add("Api-Key", "RawOHkhnXPDC0nWZHbGEh8w6xVOLXN1X");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "subsdownload v1.0");
        }

        void SaveToken(string token, string username, string password)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(DATA_PATH)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DATA_PATH));
                }

                File.WriteAllText(DATA_PATH, token);
                using (var sw = new StreamWriter(DATA_PATH, true))
                {
                    sw.WriteLine("\n" + username);
                    sw.WriteLine(password);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("There was an error when logging you into opensubtitles, try relogging.");
            }
        }

        public async void LogIn(string username, string password)
        {
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
                    MessageBox.Show("You need to login through the interface first.");
                }
                else
                {
                    var stream = await response.Content.ReadAsStringAsync();
                    var responseBody = JsonConvert.DeserializeObject<Jwt>(stream);
                    SaveToken(responseBody.Token, username, password);
                }

            }
        }

        public async Task<SearchSubtitleResult> SearchSubtitle(string token, string query, string language)
        {
            var secondLanguage = File.ReadLines(DATA_PATH).Skip(4).Take(1).First();
            var hearingImpaired = File.ReadLines(DATA_PATH).Skip(5).Take(1).First() == "True" ? "only" : "exclude";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri =
                    new Uri($"https://api.opensubtitles.com/api/v1/subtitles?query={query}&languages={language}&hearing_impaired={hearingImpaired}"),
                Headers =
                {
                    { "User-Agent", "subsdown" },
                    { "Authorization", $"Bearer {token}" },
                    { "Api-Key", "4QvhsW4PzmhnDLkome6HhV3R26mg4Dht" },
                },
            };
            using (var response = await _httpClient.SendAsync(request))
            {

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("An error ocurred while searching the subtitle, try again later");
                    return null;
                }

                try
                {
                    var body = await response.Content.ReadAsStringAsync();

                    var responseBody = JsonConvert.DeserializeObject<Result>(body);

                    if (responseBody.Data.Count < 1 && Limit == 1)
                    {
                        Limit = 2;
                        return await SearchSubtitle(token, query, secondLanguage);
                    }

                    var data = responseBody.Data.OrderByDescending(s => s.Attributes.From_trusted)
                        .ThenByDescending(s => s.Attributes.New_download_count).ThenByDescending(s => s.Attributes.Download_count);

                    return new SearchSubtitleResult
                    {
                        SubtitleId = data.First().Attributes.Files.First().File_id.ToString(),
                        Language = language
                    };
                }
                catch (Exception e)
                {
                    MessageBox.Show("No subtitle for this title was found in your language. If you enabled hearing impaired subtitles, disabling could help.");
                    return null;
                }
            }
        }

        public async void CheckJwtStillValid()
        {
            var token = File.ReadLines(DATA_PATH).Take(1).First();
            var username = File.ReadLines(DATA_PATH).Skip(1).Take(1).First();
            var password = File.ReadLines(DATA_PATH).Skip(2).Take(1).First();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.opensubtitles.com/api/v1/download"),
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                },
                Content = new StringContent("{\n  \"file_id\": 8964616\n}")
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
                    LogIn(username, password);
                }
            }
        }

        public async Task<string> GetDownloadUrl(string token, string subtitleId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.opensubtitles.com/api/v1/download"),
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                },
                Content = new StringContent("{\n  \"file_id\": " + subtitleId + "\n}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };
            using (var response = await _httpClient.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    var responseBody = JsonConvert.DeserializeObject<DownloadResult>(body);
                    return responseBody.Link;
                }
                else
                {
                    MessageBox.Show("You reached the daily quota for your account. You can either create another account and login again, or buy premium in opensubtitles.com website.");
                    return null;
                }

            }
        }
    }
    public class SearchSubtitleResult
    {
        public string SubtitleId { get; set; }
        public string Language { get; set; }
    }
}
