using Kursserver.Dto;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Endpoints
{
    public static class HelpbotEndpoints
    {
        private static string? _systemPrompt;

        /// <summary>
        /// SCENARIO: User asks a question in the help chatbot popover
        /// CALLS: useHelpbot() → helpbotService.chat()
        /// SIDE EFFECTS:
        ///   - Calls Grok API with conversation history and system prompt
        ///   - Returns AI response in Swedish about site features only
        /// </summary>
        public static void MapHelpbotEndpoints(this WebApplication app)
        {
            app.MapPost("api/helpbot/chat", [Authorize] async (HelpbotChatDto dto, GrokService grokService, IWebHostEnvironment env) =>
            {
                try
                {
                    if (_systemPrompt == null)
                    {
                        var path = Path.Combine(env.ContentRootPath, "helpbot-docs.md");
                        _systemPrompt = await File.ReadAllTextAsync(path);
                    }

                    var messages = new List<GrokMessage>
                    {
                        new GrokMessage { Role = "system", Content = _systemPrompt }
                    };

                    if (dto.History != null)
                    {
                        messages.AddRange(dto.History.TakeLast(6));
                    }

                    messages.Add(new GrokMessage { Role = "user", Content = dto.Message });

                    var reply = await grokService.GetChatCompletionAsync(messages.ToArray());
                    return Results.Ok(new { reply });
                }
                catch (Exception)
                {
                    return Results.Problem("Helpbot service error", statusCode: 500);
                }
            });
        }
    }
}
