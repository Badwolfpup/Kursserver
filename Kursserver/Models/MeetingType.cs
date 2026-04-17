using System.Text.Json.Serialization;

namespace Kursserver.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MeetingType
    {
        Intro,
        Followup,
        Other
    }
}
