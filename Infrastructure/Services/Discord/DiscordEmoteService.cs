using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord
{
    public class DiscordEmoteService(DiscordSocketClient client, ILogger<DiscordEmoteService> logger)
        : IDiscordEmoteService
    {
        private Dictionary<string, Emote> _emotes = new();

        private readonly string _emotesDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Emotes");

        private bool _needsRefresh = false;

        private async Task RefreshCacheAsync()
        {
            try
            {
                logger.LogInformation("Fetching Application Emotes to populate cache...");
                var fetchedEmotes = await client.GetApplicationEmotesAsync();
                
                _emotes = fetchedEmotes.ToDictionary(apiEmote => apiEmote.Name, apiEmote => apiEmote);

                logger.LogInformation("Successfully cached {Count} Application Emotes.", _emotes.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch and cache Application Emotes.");
            }
        }

        public IEmote? GetEmote(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (_emotes.TryGetValue(name, out var appEmote)) return appEmote;
            
            if (Emote.TryParse(name, out var parsedEmote)) return parsedEmote;
            if (Emoji.TryParse(name, out var parsedEmoji)) return parsedEmoji;

            return null;
        }

        public async Task SynchronizeEmotesAsync()
        {
            logger.LogInformation("Starting Application Emojis synchronization from '{Directory}'...", _emotesDirectory);

            if (!Directory.Exists(_emotesDirectory))
            {
                Directory.CreateDirectory(_emotesDirectory);
                logger.LogWarning("Emotes directory did not exist. Created a new empty directory. Sync aborted.");
                return;
            }

            try
            {
                await RefreshCacheAsync();

                var localFiles = Directory.GetFiles(_emotesDirectory, "*.png");

                var localEmoteNames = localFiles
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet();

                await DeleteApplicationEmotesAsync(localEmoteNames);

                await UploadApplicationEmotesAsync(localEmoteNames!);

                if (_needsRefresh)
                {
                    logger.LogInformation("Synchronization completed with changes. Refreshing internal cache to fetch new IDs...");
                    await RefreshCacheAsync();
                }
                else
                {
                    logger.LogInformation("Synchronization completed. Both API and local directory are perfectly matched.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A critical error occurred during Emote synchronization.");
            }
        }

        private async Task UploadApplicationEmotesAsync(HashSet<string> localEmoteNames)
        {
            var added = 0;
            foreach (var key in localEmoteNames)
            {
                if (_emotes.ContainsKey(key)) continue;
                logger.LogInformation("Uploading new emote to Application: {EmoteName}", key);

                await using var stream = File.OpenRead(Path.Combine(_emotesDirectory, key + ".png"));
                var image = new Image(stream);
                
                await client.CreateApplicationEmoteAsync(key, image);
                await Task.Delay(100);
                
                _needsRefresh = true;
                added++;
            }
            logger.LogInformation("Added a total of {Count} new emotes to Application.", added);
        }

        private async Task DeleteApplicationEmotesAsync(HashSet<string?> localEmoteNames)
        {
            var deleted = 0;
            foreach (var (key, emote) in _emotes)
            {
                if (localEmoteNames.Contains(key)) continue;
                
                await client.DeleteApplicationEmoteAsync(emote.Id);
                await Task.Delay(100);
                
                _needsRefresh = true;
                deleted++;
            }
            logger.LogInformation("Deleted a total of {deleted} un-synced emotes from Application.", deleted);
        }
    }
}