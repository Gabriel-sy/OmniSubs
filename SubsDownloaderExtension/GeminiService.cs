using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SubsDownloaderExtension
{
    public class GeminiService : IDisposable
    {

        private readonly Uri _url = 
            new Uri(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=");
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private long _circuitStatus;
        private const long Closed = 0;
        private const long Tripped = 1;
        private const string Unavailable = "Unavailable";
        
        private CancellationTokenSource _cancellationTokenSource;
        
        public GeminiService(int maxConcurrentRequests)
        {
            var httpClientHandler = new HttpClientHandler()
            {
                MaxConnectionsPerServer = maxConcurrentRequests
            };
            _httpClient = new HttpClient(httpClientHandler);
            
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
            
            _circuitStatus = Closed;
            
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        private bool IsTripped()
        {
            return Interlocked.Read(ref _circuitStatus) == Tripped;
        }

        public async Task<string> TranslateSubtitle(string prompt)
        {
            try
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                
                if (IsTripped())
                {
                    return Unavailable;
                }
                
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return string.Empty;
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
                
                using (var response = await _httpClient.SendAsync(request, _cancellationTokenSource.Token))
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return string.Empty;
                    }
                    
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
                    } else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        return "500";
                    } else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        return "503";
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
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _semaphore.Release();
                }
            }
        }
        
        public void CancelAllOperations()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _httpClient?.Dispose();
                _semaphore?.Dispose();
            }
        }
    }
}