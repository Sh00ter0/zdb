using Domain.Enums;

namespace Infrastructure.Exceptions;

public class InteractionException : Exception
{
    public InteractionException(
        string message,
        InteractionExceptionLevel level = InteractionExceptionLevel.Warn,
        bool logCopy = false,
        string? header = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Level = level;
        LogCopy = logCopy;
        Header = header ?? GetDefaultHeader(level);
    }

    public InteractionExceptionLevel Level { get; }
    public bool LogCopy { get; }
    public string Header { get; }

    public static InteractionException Info(string message, bool logCopy = false, string? header = null)
    {
        return new InteractionException(message, InteractionExceptionLevel.Info, logCopy, header);
    }

    public static InteractionException Warn(string message, bool logCopy = false, string? header = null)
    {
        return new InteractionException(message, InteractionExceptionLevel.Warn, logCopy, header);
    }

    public static InteractionException Crit(string message, bool logCopy = false, string? header = null)
    {
        return new InteractionException(message, InteractionExceptionLevel.Crit, logCopy, header);
    }

    private static string GetDefaultHeader(InteractionExceptionLevel level)
    {
        return level switch
        {
            InteractionExceptionLevel.Info => "Information",
            InteractionExceptionLevel.Warn => "Action Failed",
            InteractionExceptionLevel.Crit => "Critical Notice",
            _ => "Action Failed"
        };
    }
}
