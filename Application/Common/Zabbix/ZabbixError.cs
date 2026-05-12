using System.Text.Json.Serialization;

namespace Application.Common.Zabbix
{
    public class ZabbixError
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;
    }
}
