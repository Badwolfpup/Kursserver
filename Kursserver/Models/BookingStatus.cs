using System.Text.Json.Serialization;

namespace Kursserver.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookingStatus
    {
        Pending,
        Accepted,
        Declined
    }
}
