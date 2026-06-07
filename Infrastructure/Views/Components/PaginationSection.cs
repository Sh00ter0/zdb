using Application.Views.Components;
using Discord;

namespace Infrastructure.Views.Components;

public class PaginationSection(string id, int page, int totalPages) : IViewSection
{
    public IViewSection Build(ContainerBuilder builder)
    {
        builder.WithActionRow(row =>
        {
            row.AddComponents(
                new ButtonBuilder().WithCustomId($"nav:{id}:1:first").WithEmote(new Emoji("⏪"))
                    .WithStyle(ButtonStyle.Secondary).WithDisabled(page == 1),
                new ButtonBuilder().WithCustomId($"nav:{id}:{page - 1}:prev")
                    .WithEmote(new Emoji("⬅️")).WithStyle(ButtonStyle.Secondary).WithDisabled(page == 1),
                new ButtonBuilder().WithCustomId($"nav:{id}:{page + 1}:next")
                    .WithEmote(new Emoji("➡️")).WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(page == totalPages),
                new ButtonBuilder().WithCustomId($"nav:{id}:{totalPages}:last").WithEmote(new Emoji("⏩"))
                    .WithStyle(ButtonStyle.Secondary).WithDisabled(page == totalPages)
            );
        });
        return this;
    }
}