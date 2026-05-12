using Discord.Interactions;
using Discord.WebSocket;
using Domain.Entities;

namespace Infrastructure.Discord.SlashCommands
{
    public class AppInteractionContext : SocketInteractionContext
    {
        public SystemAdministrators? Admin { get; }

        public AppInteractionContext(DiscordSocketClient client, SocketInteraction interaction, SystemAdministrators? admin)
            : base(client, interaction)
        {
            Admin = admin;
        }
    }
}