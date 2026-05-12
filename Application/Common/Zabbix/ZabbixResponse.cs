using System.Text.Json.Serialization;

namespace Application.Common.Zabbix
{
    public class ZabbixResponse<T>
    {
        [JsonPropertyName("result")] public T? Result { get; set; }
        [JsonPropertyName("error")] public ZabbixError? Error { get; set; }
    }
}
