using Discord;

namespace Application.Views.Components;

public class TextSection(string content) : IViewSection
{
    public IViewSection Build(ContainerBuilder builder)
    {
        builder.WithSeparator();
        builder.WithTextDisplay(content);
        return this;
    }
}