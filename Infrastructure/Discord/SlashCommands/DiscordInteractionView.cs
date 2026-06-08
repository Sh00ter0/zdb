using Discord;
using Discord.Interactions;

namespace Infrastructure.Discord.SlashCommands;

public abstract class DiscordInteractionView : InteractionModuleBase<AppInteractionContext>
{
    public Task DeferInteractionAsync(bool ephemeral = false)
    {
        return DeferAsync(ephemeral);
    }

    public Task RespondInteractionAsync(MessageComponent components, bool ephemeral = false,
        MessageFlags flags = MessageFlags.None)
    {
        return RespondAsync(components: components, ephemeral: ephemeral, flags: flags);
    }

    public Task FollowupInteractionAsync(string? text = null, MessageComponent? components = null,
        bool ephemeral = false, MessageFlags flags = MessageFlags.None)
    {
        return FollowupAsync(text: text, components: components, ephemeral: ephemeral, flags: flags);
    }

    public Task RespondWithModalInteractionAsync(Modal modal)
    {
        return RespondWithModalAsync(modal);
    }
}
