using Discord;
using Discord.Interactions;

namespace Infrastructure.Models.Modals
{
    public class ZabbixActionModal : IModal
    {
        public string Title => "Zabbix: Event Processing";

        [InputLabel("Comment")]
        [ModalTextInput("comment_text", TextInputStyle.Paragraph, placeholder: "Describe the actions taken...", minLength: 1, maxLength: 500)]
        public string Comment { get; set; } = string.Empty;
    }
}
