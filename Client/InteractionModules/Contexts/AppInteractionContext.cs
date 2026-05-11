using Client.Data;
using Discord.Interactions;
using Discord.WebSocket;

namespace Client.Contexts
{
    public class AppInteractionContext : SocketInteractionContext
    {
        public SystemAdministratorEntity? Admin { get; }

        public AppInteractionContext(DiscordSocketClient client, SocketInteraction interaction, SystemAdministratorEntity? admin)
            : base(client, interaction)
        {
            Admin = admin;
        }
    }
}
