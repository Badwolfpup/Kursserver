using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace Kursserver.Utils
{
    public class AnthropicService
    {
        private readonly AnthropicClient _client;

        public AnthropicService(IConfiguration configuration)
        {
            var apiKey = configuration["Anthropic:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Anthropic API key is not configured.");
            }
            _client = new AnthropicClient(apiKey);
        }

        public async Task<string> GetCompletionAsync(string promt)
        {
            var messages = new List<Message>
            {
                new Message(RoleType.User, promt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = "claude-haiku-4-5-20251001",
                MaxTokens = 2048,
                Temperature = 1.0m,
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
            return textContent?.Text ?? string.Empty;
        }
    }
}
