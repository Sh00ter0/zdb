using Discord;

namespace Application.Views.Components;

public interface IViewSection
{
    IViewSection Build(ContainerBuilder builder);
}