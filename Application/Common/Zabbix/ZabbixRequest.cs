using System.Text.Json.Serialization;

namespace Application.Common.Zabbix
{
    public class ZabbixRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonPropertyName("method")]
        public required string Method { get; set; }

        [JsonPropertyName("params")]
        public required object Params { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; } = 1;

        [JsonPropertyName("auth")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Auth { get; set; }
    }
}
