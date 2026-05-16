using System.Text;
using Microsoft.Extensions.Logging;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Encodes panel interactions into compact Discord custom ids and decodes them back.
/// </summary>
public sealed class CompactInteractionCodec(ILogger<CompactInteractionCodec> logger) : IInteractionCodec
{
    /// <inheritdoc />
    public string Encode(PanelInteraction interaction)
    {
        var sb = new StringBuilder();
        sb.Append($"p:{interaction.Panel}|a:{interaction.Action}");
        
        if (!string.IsNullOrEmpty(interaction.EntityId)) sb.Append($"|e:{interaction.EntityId}");
        if (!string.IsNullOrEmpty(interaction.SubEntityId)) sb.Append($"|s:{interaction.SubEntityId}");
        if (interaction.UserId > 0) sb.Append($"|u:{interaction.UserId}");
        
        if (interaction.Metadata is { Count: > 0 })
        {
            var metaString = string.Join(";", interaction.Metadata.Select(kv => $"{kv.Key}={kv.Value}"));
            sb.Append($"|m:{metaString}");
        }

        var result = sb.ToString();
        if (result.Length > 100)
        {
            // To consider: Higher log level or even throw an exception, as this will cause the interaction to fail in Discord
            // Temporarily logging a warning for now to identify potential issues without breaking functionality
            logger.LogWarning("Encoded customId exceeds 100 characters limit! Length: {Length}, Content: {Content}", result.Length, result);
        }

        return result;
    }

    /// <inheritdoc />
    public PanelInteraction Decode(string customId)
    {
        var parts = customId.Split('|');
        var dict = parts.Select(p => p.Split(':', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : string.Empty);

        var metadata = new Dictionary<string, string>();
        if (dict.TryGetValue("m", out var metaRaw) && !string.IsNullOrEmpty(metaRaw))
        {
            var metaPairs = metaRaw.Split(';');
            foreach (var pair in metaPairs)
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2) metadata[kv[0]] = kv[1];
            }
        }

        return new PanelInteraction
        {
            Panel = dict.GetValueOrDefault("p") ?? throw new InvalidOperationException("Missing Panel ID in interaction"),
            Action = dict.GetValueOrDefault("a") ?? throw new InvalidOperationException("Missing Action ID in interaction"),
            EntityId = dict.GetValueOrDefault("e"),
            SubEntityId = dict.GetValueOrDefault("s"),
            UserId = dict.TryGetValue("u", out var uRaw) && ulong.TryParse(uRaw, out var u) ? u : 0,
            Metadata = metadata.Count > 0 ? metadata : null
        };
    }
}
