using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Services.Discord
{
    public class DiscordUiService(DiscordSocketClient client, IDiscordEmoteService emoteCache)
        : IDiscordUiService
    {
        public MessageComponent CreateStandardContainer(string header, string body, Color? accentColor = null,
            string? footerNote = null)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create(header)
                .AddSection(
                    new TextSection(body)
                );

            if (accentColor != null) layout.WithAccentColor(accentColor.Value);
            if (footerNote != null) layout.WithFooter(footerNote);

            return layout.Build();
        }

        public Modal CreateConfirmationModal(string customId, string title, string inputLabel, string placeholder,
            int maxLength)
        {
            var mb = new ModalBuilder()
                .WithTitle(title)
                .WithCustomId(customId)
                .AddTextInput(label: inputLabel, customId: "confirm_text", style: TextInputStyle.Short,
                    placeholder: placeholder, required: true, maxLength: maxLength);
            return mb.Build();
        }

        public MessageComponent CreatePaginatedContainer(string header, string pageText, int currentPage,
            int totalPages, string sessionId, Color? accentColor = null, ButtonBuilder? customActionBtn = null)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create(header);
            if (accentColor != null) layout.WithAccentColor(accentColor.Value);

            layout.AddSection(
                new TextSection(pageText)
            );

            if (totalPages > 1)
            {
                layout.AddSection(
                    new PaginationSection(sessionId, currentPage, totalPages)
                );
            }

            if (customActionBtn is not null)
            {
                layout.AddSection(
                    new ActionSection([customActionBtn])
                );
            }

            return layout.Build();
        }
    }
}
