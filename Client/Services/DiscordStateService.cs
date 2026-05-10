using Discord;
using Discord.WebSocket;
using Serilog;
using System.Threading.Tasks;

namespace Client.Services
{
    public class DiscordStateService
    {
        private readonly Serilog.ILogger _apiLog;
        public bool IsReady { get; private set; }

        public DiscordStateService(DiscordSocketClient client)
        {
            _apiLog = Log.ForContext("Source", "Discord");

            client.Ready += () => {
                IsReady = true;
                _apiLog.Information("The client has connected to the Discord servers; API access has been granted");
                return Task.CompletedTask;
            };

            client.Disconnected += (ex) => {
                IsReady = false;
                _apiLog.Warning("The client has lost connection to the Discord servers; API access has been suspended. Reason: {Message}", ex.Message);
                return Task.CompletedTask;
            };

            client.LoggedOut += () => {
                IsReady = false;
                _apiLog.Fatal("The client has lost connection to the Discord servers (logged out); API access has been suspended");
                return Task.CompletedTask;
            };
        }
    }
}