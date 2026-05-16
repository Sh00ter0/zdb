using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.Core;
using Application.Services.Discord;
using Discord;

namespace Application.Discord.Panels.Modals.ClientPanel;

/// <summary>
/// Builds the modal used to update Zabbix credentials from the client panel.
/// </summary>
public sealed class UpdateZabbixModalFactory(IDiscordUiService discordUiService, IInteractionCodec codec) : IModalFactory
{
    /// <inheritdoc />
    public bool CanCreate(string modalType) => modalType == "UpdateZabbix";

    /// <inheritdoc />
    public Modal Create(OpenModalResult result)
    {
        var modalId = codec.Encode(new PanelInteraction
        {
            Panel = "client",
            Action = ClientPanelActionIds.ZabbixSubmit,
            EntityId = result.EntityId
        });

        return discordUiService.CreateDualInputModal(modalId, "Update Zabbix Connection", "New Zabbix API URL", "New Zabbix API Token", "https://...", "Enter token...");
    }
}
