using System.Text.Json.Serialization;

namespace Application.Common.Zabbix
{
    public class ZabbixEvent
    {
        [JsonPropertyName("eventid")]
        public string EventId { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Severity { get; set; }

        [JsonPropertyName("acknowledged")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Acknowledged { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
