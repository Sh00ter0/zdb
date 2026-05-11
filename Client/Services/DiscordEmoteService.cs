using Application.Services.Discord;
using Discord;
using Discord.WebSocket;

namespace Client.Services
{
    public class DiscordEmoteService : IDiscordEmoteService
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<DiscordEmoteService> _logger;

        private Dictionary<string, Emote> _emotes = new(StringComparer.OrdinalIgnoreCase);

        private readonly string _emotesDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Emotes");

        public DiscordEmoteService(DiscordSocketClient client, ILogger<DiscordEmoteService> logger)
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

        public async Task SynchronizeEmotesAsync()
        {
            _logger.LogInformation("Starting Application Emojis synchronization from '{Directory}'...", _emotesDirectory);

            if (!Directory.Exists(_emotesDirectory))
            {
                Directory.CreateDirectory(_emotesDirectory);
                _logger.LogWarning("Emotes directory did not exist. Created a new empty directory. Sync aborted.");
                return;
            }

            try
            {
                await RefreshCacheAsync();

                var localFiles = Directory.GetFiles(_emotesDirectory, "*.png");

                var localEmoteNames = localFiles
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var currentApiEmotes = _emotes.Values;

                var emotesToDelete = currentApiEmotes
                    .Where(apiEmote => !localEmoteNames.Contains(apiEmote.Name))
                    .ToList();

                var apiEmoteNames = currentApiEmotes.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var filesToAdd = localFiles
                    .Where(file => !apiEmoteNames.Contains(Path.GetFileNameWithoutExtension(file)))
                    .ToList();

                bool cacheNeedsRefresh = false;

                if (emotesToDelete.Count > 0)
                {
                    _logger.LogInformation("Found {Count} obsolete emotes in API to delete.", emotesToDelete.Count);
                    foreach (var emote in emotesToDelete)
                    {
                        _logger.LogInformation("Deleting emote from API: {EmoteName}", emote.Name);
                        await _client.DeleteApplicationEmoteAsync(emote.Id);

                        await Task.Delay(2000);
                        cacheNeedsRefresh = true;
                    }
                }

                if (filesToAdd.Count > 0)
                {
                    _logger.LogInformation("Found {Count} new local emotes to upload.", filesToAdd.Count);
                    foreach (var file in filesToAdd)
                    {
                        var emoteName = Path.GetFileNameWithoutExtension(file);
                        _logger.LogInformation("Uploading new emote to API: {EmoteName}", emoteName);

                        await using var stream = File.OpenRead(file);
                        var image = new Image(stream);
                        await _client.CreateApplicationEmoteAsync(emoteName, image);

                        await Task.Delay(2000);
                        cacheNeedsRefresh = true;
                    }
                }

                if (cacheNeedsRefresh)
                {
                    _logger.LogInformation("Synchronization completed with changes. Refreshing internal cache to fetch new IDs...");
                    await RefreshCacheAsync();
                }
                else
                {
                    _logger.LogInformation("Synchronization completed. Both API and local directory are perfectly matched.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A critical error occurred during Emote synchronization.");
            }
        }
    }
}