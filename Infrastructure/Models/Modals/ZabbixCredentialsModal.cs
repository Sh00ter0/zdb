using Discord;
using Discord.Interactions;

namespace Infrastructure.Models.Modals
{
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
