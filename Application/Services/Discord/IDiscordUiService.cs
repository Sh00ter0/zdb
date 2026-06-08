using Discord;

namespace Application.Services.Discord
{
    public interface IDiscordUiService
    {
        MessageComponent CreateStandardContainer(string header, string body, Color? accentColor = null, string? footerNote = null);
        Modal CreateConfirmationModal(string customId, string title, string inputLabel, string placeholder, int maxLength);
        MessageComponent CreatePaginatedContainer(string header, string pageText, int currentPage, int totalPages, string sessionId, Color? accentColor = null, ButtonBuilder? customActionBtn = null);
    }
}
