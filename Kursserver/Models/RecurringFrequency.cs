using System.Text.Json.Serialization;

namespace Kursserver.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecurringFrequency
    {
        Weekly,
        Biweekly
    }
}
