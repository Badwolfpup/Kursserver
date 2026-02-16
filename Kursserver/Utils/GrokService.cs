using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kursserver.Utils
{
    public class GrokService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.x.ai/v1/chat/completions";

        public GrokService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["Grok:ApiKey"]
                ?? throw new ArgumentNullException("Grok API key is required");
        }

        public async Task<string> GetCompletionAsync(string prompt, string model = "grok-code-fast-1")
        {
            var request = new GrokRequest
            {
                Model = model,
                Messages = new[]
                {
                    new GrokMessage { Role = "user", Content = prompt }
                },
                Temperature = 1.0,
                MaxTokens = 4000
            };

            return await SendRequestAsync(request);
        }

        private async Task<string> SendRequestAsync(GrokRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Headers =
                {
                    { "Authorization", $"Bearer {_apiKey}" }
                },
                Content = JsonContent.Create(request, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
            };

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Grok API request failed with status {response.StatusCode}: {errorContent}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<GrokResponse>(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }
            );

            return result?.Choices?[0]?.Message?.Content
                ?? throw new Exception("No response from Grok API");
        }
    }

    public class GrokRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "grok-code-fast-1";

        [JsonPropertyName("messages")]
        public GrokMessage[] Messages { get; set; } = Array.Empty<GrokMessage>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 1.0;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 4000;
    }

    public class GrokMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class GrokResponse
    {
        [JsonPropertyName("choices")]
        public GrokChoice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public GrokUsage? Usage { get; set; }
    }

    public class GrokChoice
    {
        [JsonPropertyName("message")]
        public GrokMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class GrokUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
