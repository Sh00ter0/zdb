using Discord.Interactions;

namespace Infrastructure.Models.Modals
{
    public class ClientActionModal : IModal
    {
        public string Title => "Action Confirmation";

        [InputLabel("Confirmation")]
        [ModalTextInput("confirm_text")]
        public string ConfirmText { get; set; } = string.Empty;
    }
}
