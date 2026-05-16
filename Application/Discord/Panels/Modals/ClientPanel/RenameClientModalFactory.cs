using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.Core;
using Application.Services.Discord;
using Discord;

namespace Application.Discord.Panels.Modals.ClientPanel;

/// <summary>
/// Builds the modal used to rename an API client from the client panel.
/// </summary>
public sealed class RenameClientModalFactory(IDiscordUiService discordUiService, IInteractionCodec codec) : IModalFactory
{
    /// <inheritdoc />
    public bool CanCreate(string modalType) => modalType == "RenameClient";

    /// <inheritdoc />
    public Modal Create(OpenModalResult result)
    {
        var modalId = codec.Encode(new PanelInteraction
        {
            Panel = "client",
            Action = ClientPanelActionIds.RenameSubmit,
            EntityId = result.EntityId
        });

        return discordUiService.CreateSingleInputModal(modalId, "Rename API Client", "New Display Name", "Enter new unique name...", 50);
    }
}
