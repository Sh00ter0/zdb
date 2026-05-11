using System.Text.Json.Serialization;

namespace Application.Common.Zabbix
{
    public class ZabbixAcknowledgeResult
    {
        [JsonPropertyName("eventids")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long[] EventIds { get; set; } = [];
    }
}
