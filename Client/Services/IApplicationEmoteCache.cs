using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IApplicationEmoteCache
    {
        /// <summary>
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// </summary>
        IEmote? GetEmote(string? name);
    }

    public class ApplicationEmoteCache : IApplicationEmoteCache
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<ApplicationEmoteCache> _logger;

        private Dictionary<string, Emote> _emotes = new(StringComparer.OrdinalIgnoreCase);

        public ApplicationEmoteCache(DiscordSocketClient client, ILogger<ApplicationEmoteCache> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task RefreshCacheAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Application Emotes to populate cache...");
                var fetchedEmotes = await _client.GetApplicationEmotesAsync();

                var newDict = new Dictionary<string, Emote>(StringComparer.OrdinalIgnoreCase);
                foreach (var emote in fetchedEmotes)
                {
                    newDict[emote.Name] = emote;
                }

                _emotes = newDict;
                _logger.LogInformation("Successfully cached {Count} Application Emotes.", _emotes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch and cache Application Emotes.");
            }
        }

        public IEmote? GetEmote(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (Emote.TryParse(name, out var parsedEmote)) return parsedEmote;

            if (Emoji.TryParse(name, out var parsedEmoji)) return parsedEmoji;

            if (_emotes.TryGetValue(name, out var appEmote))
            {
                return appEmote;
            }

            return null;
        }
    }
}
