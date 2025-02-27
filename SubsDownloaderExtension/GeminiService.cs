using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SubsDownloaderExtension
{
    public class GeminiService
    {

        private static readonly string DATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubtitleDownloader",
            "data.txt");

        private static string dataText = File.ReadLines(DATA_PATH).Skip(7).Take(1).First();

        private static readonly string geminiApi =
            dataText != "False" ? dataText : "";
        private readonly Uri _url =
            new Uri(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiApi}");
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private long _circuitStatus;
        private const long Closed = 0;
        private const long Tripped = 1;
        private const string Unavailable = "Unavailable";

        public GeminiService(int maxConcurrentRequests)
        {
            var httpClientHandler = new HttpClientHandler()
            {
                MaxConnectionsPerServer = maxConcurrentRequests
            };
            _httpClient = new HttpClient(httpClientHandler);

            _semaphore = new SemaphoreSlim(maxConcurrentRequests);

            _circuitStatus = Closed;
        }

        private bool IsTripped()
        {
            return Interlocked.Read(ref _circuitStatus) == Tripped;
        }

        public async Task<string> TranslateSubtitle(string prompt)
        {
            try
            {
                await _semaphore.WaitAsync();

                if (IsTripped())
                {
                    return Unavailable;
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 1,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 8192,
                        responseMimeType = "text/plain"
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = _url,
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
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
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var translatedPart = JsonConvert.DeserializeObject<CandidateResponse>(responseContent)
                            .Candidates.First().Content.Parts.First().Text;
                        return translatedPart;
                    }
                    else if (response.StatusCode == (HttpStatusCode)429)
                    {
                        Interlocked.Exchange(ref _circuitStatus, Tripped);
                        return "429";
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        MessageBox.Show(response.Content.ReadAsStringAsync().Result);
                        return "500";
                    }
                    else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        return "503";
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return "401";
                    }
                    else
                    {
                        return "unknown";
                    }

                }

            }
            catch (OperationCanceledException)
            {
                return string.Empty;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return Unavailable;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}