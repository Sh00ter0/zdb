namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Defines all final action identifiers supported by the client management panel.
/// </summary>
public static class ClientPanelActionIds
{
    /// <summary>
    /// Navigates back to the client overview state.
    /// </summary>
    public const string Back = "@back";

    /// <summary>
    /// Closes the current client panel message.
    /// </summary>
    public const string ClosePanel = "close_panel";

    /// <summary>
    /// Opens the modal used to rename the API client.
    /// </summary>
    public const string OpenRename = "open_rename";

    /// <summary>
    /// Opens the client status management screen.
    /// </summary>
    public const string OpenStatus = "open_status";

    /// <summary>
    /// Opens the known delivery targets screen.
    /// </summary>
    public const string OpenTargets = "open_targets";

    /// <summary>
    /// Opens the modal used to update Zabbix connection data.
    /// </summary>
    public const string OpenZabbix = "open_zabbix";

    /// <summary>
    /// Opens the API key regeneration confirmation screen.
    /// </summary>
    public const string OpenRegenerateKey = "open_regenerate_key";

    /// <summary>
    /// Opens the destructive delete confirmation screen.
    /// </summary>
    public const string OpenDeleteWarning = "open_delete_warning";

    /// <summary>
    /// Applies a selected active or disabled client status.
    /// </summary>
    public const string ToggleStatus = "toggle_status";

    /// <summary>
    /// Handles the rename modal submission.
    /// </summary>
    public const string RenameSubmit = "rename_submit";

    /// <summary>
    /// Handles the Zabbix connection modal submission.
    /// </summary>
    public const string ZabbixSubmit = "zabbix_submit";

    /// <summary>
    /// Confirms API key regeneration.
    /// </summary>
    public const string RenewSubmit = "renew_submit";

    /// <summary>
    /// Confirms permanent client deletion.
    /// </summary>
    public const string DeleteSubmit = "delete_submit";
}
