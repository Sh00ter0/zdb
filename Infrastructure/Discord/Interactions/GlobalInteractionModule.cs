using Application.Discord.Panels.Core;
using Discord;
using Discord.Interactions;
using Infrastructure.Discord.SlashCommands;

namespace Infrastructure.Discord.Interactions;

// Pusty, uniwersalny model wymagany przez rygorystyczny walidator biblioteki Discord.NET
public class CatchAllModal : IModal
{
    public string Title => "Action";
}

public sealed class GlobalInteractionModule(InteractionDispatcher dispatcher) : InteractionModuleBase<AppInteractionContext>
{
    // Discord.NET wstrzykuje Scoped IServiceProvider automatycznie
    public IServiceProvider ServiceProvider { get; set; } = null!;

    // ZMIANA: Znak '*' powoduje przekazanie stringa, więc dodajemy 'string wildcardData'
    [ComponentInteraction("p:*", ignoreGroupNames: true)]
    public async Task HandleComponentAsync(string wildcardData)
    {
        var interaction = (IComponentInteraction)Context.Interaction;
        var customId = interaction.Data.CustomId;

        // Wyciągamy wartości, jeśli to Select Menu
        string[]? selectedValues = interaction.Data.Type == global::Discord.ComponentType.SelectMenu
            ? interaction.Data.Values.ToArray()
            : null;

        if (selectedValues is { Length: > 0 } && selectedValues[0].StartsWith("p:", StringComparison.Ordinal))
        {
            customId = selectedValues[0];
            selectedValues = null;
        }

        await dispatcher.DispatchAsync(Context, ServiceProvider, customId, selectedValues);
    }

    // ZMIANA: Znak '*' przekazuje stringa, a atrybut wymaga interfejsu IModal na końcu
    [ModalInteraction("p:*", ignoreGroupNames: true)]
    public async Task HandleModalAsync(string wildcardData, CatchAllModal dummyModal)
    {
        var interaction = (IModalInteraction)Context.Interaction;
        var customId = interaction.Data.CustomId;

        // Omijamy autoi-bindowanie Discord.NET i wyciągamy surowe dane bezpośrednio z interakcji
        var modalValues = interaction.Data.Components
            .Select(c => c.Value)
            .ToArray();

        await dispatcher.DispatchAsync(Context, ServiceProvider, customId, modalValues);
    }
}
