using Discord;

namespace Application.Services.Discord
{
    public interface IDiscordEmoteService
    {
        Task RefreshCacheAsync();
        IEmote? GetEmote(string? name);
        Task SynchronizeEmotesAsync();
    }
}
