using Application.Common.Constants;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Payloads;
using Discord;
using System;
using System.Linq;

namespace Application.Discord.Panels.ClientPanel.Payloads;

/// <summary>
/// Payload containing only the API client identifier.
/// </summary>
/// <param name="ClientId">The API client identifier.</param>
public sealed record ClientEntityPayload(long ClientId) : IInteractionPayload
{
    /// <summary>
    /// Extracts the API client identifier from the panel context.
    /// </summary>
    /// <param name="ctx">The current panel context.</param>
    /// <returns>A typed client entity payload.</returns>
    public static ClientEntityPayload FromContext(ConfigPanelContext ctx) =>
        new(long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId in context.")));
}

/// <summary>
/// Payload describing a requested active or disabled client status.
/// </summary>
/// <param name="ClientId">The API client identifier.</param>
/// <param name="IsEnabled">Whether the client should be enabled.</param>
public sealed record ToggleClientStatusPayload(long ClientId, bool IsEnabled) : IInteractionPayload
{
    /// <summary>
    /// Extracts the target client and selected status value from the panel context.
    /// </summary>
    /// <param name="ctx">The current panel context.</param>
    /// <returns>A typed status-toggle payload.</returns>
    public static ToggleClientStatusPayload FromContext(ConfigPanelContext ctx)
    {
        var clientId = long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId in context."));
        var selectedValue = ctx.RawInteractionData?.FirstOrDefault();

        bool isEnabled = selectedValue == DiscordComponentActions.StatusTrue;

        return new ToggleClientStatusPayload(clientId, isEnabled);
    }
}

/// <summary>
/// Payload containing the submitted client display name.
/// </summary>
/// <param name="ClientId">The API client identifier.</param>
/// <param name="NewName">The submitted client name.</param>
public sealed record RenameClientPayload(long ClientId, string NewName) : IInteractionPayload
{
    /// <summary>
    /// Extracts the target client and submitted name from a rename modal interaction.
    /// </summary>
    /// <param name="ctx">The current panel context.</param>
    /// <returns>A typed rename payload.</returns>
    public static RenameClientPayload FromContext(ConfigPanelContext ctx)
    {
        var clientId = long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId."));
        var modal = ctx.Context.Interaction as IModalInteraction ?? throw new InvalidOperationException("Interaction is not a modal.");

        var newName = modal.Data.Components.FirstOrDefault()?.Value ?? throw new InvalidOperationException("Missing modal input value.");

        return new RenameClientPayload(clientId, newName);
    }
}

/// <summary>
/// Payload containing submitted Zabbix API connection values.
/// </summary>
/// <param name="ClientId">The API client identifier.</param>
/// <param name="ApiUrl">The submitted Zabbix API URL.</param>
/// <param name="ApiToken">The submitted Zabbix API token.</param>
public sealed record UpdateZabbixPayload(long ClientId, string ApiUrl, string ApiToken) : IInteractionPayload
{
    /// <summary>
    /// Extracts the target client and submitted Zabbix fields from a modal interaction.
    /// </summary>
    /// <param name="ctx">The current panel context.</param>
    /// <returns>A typed Zabbix update payload.</returns>
    public static UpdateZabbixPayload FromContext(ConfigPanelContext ctx)
    {
        var clientId = long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId."));
        var modal = ctx.Context.Interaction as IModalInteraction ?? throw new InvalidOperationException("Interaction is not a modal.");
        var components = modal.Data.Components.ToList();

        var apiUrl = components.ElementAtOrDefault(0)?.Value ?? string.Empty;
        var apiToken = components.ElementAtOrDefault(1)?.Value ?? string.Empty;

        return new UpdateZabbixPayload(clientId, apiUrl, apiToken);
    }
}
