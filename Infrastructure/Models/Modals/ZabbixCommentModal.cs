using Discord.Interactions;

namespace Infrastructure.Models.Modals
{
    public class ZabbixCommentModal : IModal
    {
        public string Title => "Add Comment";

        [InputLabel("Comment")]
        [ModalTextInput("comment_text")]
        public string Comment { get; set; } = string.Empty;
    }
}
