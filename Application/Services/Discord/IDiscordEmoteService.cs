using Discord;

namespace Application.Services.Discord
{
    public interface IDiscordEmoteService
    {
        IEmote? GetEmote(string name);
        Task SynchronizeEmotesAsync();
    }
}
