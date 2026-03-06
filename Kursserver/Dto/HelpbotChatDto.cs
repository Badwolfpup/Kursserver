using Kursserver.Utils;

namespace Kursserver.Dto
{
    public class HelpbotChatDto
    {
        public string Message { get; set; } = "";
        public List<GrokMessage>? History { get; set; }
    }
}
