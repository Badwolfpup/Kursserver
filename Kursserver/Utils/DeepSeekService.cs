using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kursserver.Utils
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.deepseek.com/v1/chat/completions";

        public DeepSeekService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["DeepSeek:ApiKey"]
                ?? throw new ArgumentNullException("DeepSeek API key is required");
        }

        // Simple completion for structured generation (exercises, projects)
        public async Task<string> GetCompletionAsync(string prompt, string model = "deepseek-coder")
        {
            var request = new DeepSeekRequest
            {
                Model = model,
                Messages = new[]
                {
                    new DeepSeekMessage { Role = "user", Content = prompt }
                },
                Temperature = 1.3,
                MaxTokens = 4000
            };

            return await SendRequestAsync(request);
        }

        // Core method to send requests
        private async Task<string> SendRequestAsync(DeepSeekRequest request)
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
                    $"DeepSeek API request failed with status {response.StatusCode}: {errorContent}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }
            );

            return result?.Choices?[0]?.Message?.Content
                ?? throw new Exception("No response from DeepSeek API");
        }
    }

    // Request/Response Models
    public class DeepSeekRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "deepseek-coder";

        [JsonPropertyName("messages")]
        public DeepSeekMessage[] Messages { get; set; } = Array.Empty<DeepSeekMessage>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 2000;
    }

    public class DeepSeekMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class DeepSeekResponse
    {
        [JsonPropertyName("choices")]
        public DeepSeekChoice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public DeepSeekUsage? Usage { get; set; }
    }

    public class DeepSeekChoice
    {
        [JsonPropertyName("message")]
        public DeepSeekMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class DeepSeekUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}