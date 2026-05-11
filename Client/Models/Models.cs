using Discord;
using Discord.Interactions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Client.Models
{
    public class AppApiConfig
    {
        public string headerName { get; set; } = "X-Api-Key";
        public string databasePath { get; set; } = "Data\\api-security.db";
        public string apiKeyHashPepper { get; set; } = "__SET_API_KEY_HASH_PEPPER_VIA_ENV__";
        public bool allowInsecureHttp { get; set; }
        public int rateLimitPermit { get; set; } = 30;
        public int rateLimitWindowSeconds { get; set; } = 60;
        public List<string> knownProxies { get; set; } = [];
        public string masterEncryptionKey { get; set; } = null!;
    }
    public static class AppColors
    {
        public static readonly Color Info = new Color(0x5865F2);
        public static readonly Color Success = new Color(0x57F287);
        public static readonly Color Warning = new Color(0xFEE75C);
        public static readonly Color Error = new Color(0xED4245);

        public static readonly Color SeverityDisaster = new Color(0xE45959);
        public static readonly Color SeverityHigh = new Color(0xE97659);
        public static readonly Color SeverityAverage = new Color(0xFFA059);
        public static readonly Color SeverityWarning = new Color(0xFFC859);
        public static readonly Color SeverityInformation = new Color(0x7499FF);
        public static readonly Color SeverityNotClassified = new Color(0x97AAB3);
    }
    public class AppDiscordConfig
    {
        public string apiToken { get; set; } = null!;
    }

    public class ZabbixTag
    {
        [Required]
        [StringLength(64)]
        public string Tag { get; set; } = null!;

        [Required]
        [StringLength(256)]
        public string Value { get; set; } = null!;
    }

    public class ZabbixPayload
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required]
        [StringLength(10000)]
        public string Message { get; set; } = null!;

        [Range(0, 10)]
        public int EventSource { get; set; }

        [Range(0, 10)]
        public int EventValue { get; set; }

        [Range(0, 5)]
        public int Severity { get; set; }

        [Required]
        [RegularExpression(@"^\d{1,32}$")]
        public string EventId { get; set; } = null!;

        [Required]
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public List<ZabbixTag> Tags { get; set; } = [];

        [Range(0, 1)]
        public int ControlMenu { get; set; }
    }

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

    public class ZabbixResponse<T>
    {
        [JsonPropertyName("result")] public T? Result { get; set; }
        [JsonPropertyName("error")] public ZabbixError? Error { get; set; }
    }

    public class ZabbixError
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;
    }

    public class ZabbixCommentModal : IModal
    {
        public string Title => "Add Comment";

        [InputLabel("Comment")]
        [ModalTextInput("comment_text")]
        public string Comment { get; set; } = string.Empty;
    }

    public class ZabbixActionModal : IModal
    {
        public string Title => "Zabbix: Event Processing";

        [InputLabel("Comment")]
        [ModalTextInput("comment_text", TextInputStyle.Paragraph, placeholder: "Describe the actions taken...", minLength: 1, maxLength: 500)]
        public string Comment { get; set; } = string.Empty;
    }

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
    public class UserVisibleException : Exception
    {
        public UserVisibleException(string message) : base(message) { }
    }

    public class ClientActionModal : IModal
    {
        public string Title => "Action Confirmation";

        [InputLabel("Confirmation")]
        [ModalTextInput("confirm_text")]
        public string ConfirmText { get; set; } = string.Empty;
    }

    public class ZabbixCredentialsModal : IModal
    {
        public string Title => "Update Zabbix Connection";

        [InputLabel("New Zabbix API URL")]
        [ModalTextInput("url", TextInputStyle.Short, placeholder: "https://zabbix.yourdomain.com/api_jsonrpc.php")]
        public string Url { get; set; } = string.Empty;

        [InputLabel("New Zabbix API Token")]
        [ModalTextInput("token", TextInputStyle.Short, placeholder: "Enter new token...")]
        public string Token { get; set; } = string.Empty;
    }

    public class PaginationSessionData
    {
        public string Header { get; set; } = string.Empty;
        public List<string> Pages { get; set; } = new();
        public ButtonBuilder? CustomButton { get; set; }
    }

}
