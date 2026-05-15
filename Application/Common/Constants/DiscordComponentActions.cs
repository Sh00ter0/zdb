namespace Application.Common.Constants;

public static class DiscordComponentActions
{
    // General Actions
    public const string Cancel = "cancel";
    public const string Manage = "manage";

    // Status Actions
    public const string StatusTrue = "status_true";
    public const string StatusFalse = "status_false";

    // Confirmation Actions
    public const string RenewConfirm = "renew_confirm";
    public const string RemoveConfirm = "remove_confirm";
    public const string SyncConfirm = "sync_confirm";

    // Target Specific Actions
    public const string CrosspostTrue = "cp_true";
    public const string CrosspostFalse = "cp_false";
    
    // System Administration
    public const string SetRole = "set_role";

    // Zabbix Actions
    public const string AckTrue = "ack_true";
    public const string AckFalse = "ack_false";
    public const string Comment = "comment";
    public const string AckActionPrefix = "ack";
    public const string SevActionPrefix = "sev";
}