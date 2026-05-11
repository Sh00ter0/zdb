using Discord;
using Discord.Interactions;

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
    public class AppDiscordConfig
    {
        public string apiToken { get; set; } = null!;
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

}
