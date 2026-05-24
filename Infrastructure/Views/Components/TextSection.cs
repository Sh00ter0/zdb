using Application.Views.Components;
using Discord;

namespace Infrastructure.Views.Components;

public class TextSection(string content) : IViewSection
{
    public IViewSection Build(ContainerBuilder builder)
    {
        builder.WithSeparator();
        builder.WithTextDisplay(content);
        return this;
    }
}