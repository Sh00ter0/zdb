using Client.Data;
using Client.Data.Repositories;
using Client.Handlers;
using Client.Models;
using Client.Security;
using Client.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Net;
using System.Threading.RateLimiting;

// Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",
        theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

try
{
    Log.Information("The application is starting, please wait...");

    var builder = WebApplication.CreateBuilder(args);


    // Serilog Configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code));

    builder.Configuration.AddEnvironmentVariables(prefix: "DZB_");

    HostingExtensions.ValidateRequiredSecrets(builder.Configuration);

    builder.Services.AddApplicationOptions(builder.Configuration);

    var apiConfig = builder.Configuration.GetSection("api").Get<AppApiConfig>() ?? new AppApiConfig();
    var masterKey = EncryptionKeyGuard.EnsureKeyOrExit(apiConfig);

    builder.Services.AddApplicationInfrastructure(apiConfig, masterKey);
    builder.Services.AddApplicationSecurity(apiConfig);

    builder.Services.AddHostedService<StartupService>();

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseRouting();

    app.Use(async (context, next) =>
    {
        if (!app.Environment.IsDevelopment() && !apiConfig.allowInsecureHttp && !context.Request.IsHttps)
        {
            Log.Warning("Rejected insecure HTTP request for path {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "HTTPS required",
                message = "This API only accepts secure HTTPS requests."
            });
            return;
        }
        await next();
    });

    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application has been stopped due to a critical error");
}
finally
{
    await Log.CloseAndFlushAsync();
}

internal static class HostingExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AppDiscordConfig>().Bind(config.GetSection("discord"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.apiToken), MissingConfigMessage("discord:apiToken", "DZB_discord__apiToken"));

        services.AddOptions<AppApiConfig>().Bind(config.GetSection("api"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.headerName), "ApiHeaderName must be provided")
            .Validate(x => !string.IsNullOrWhiteSpace(x.databasePath), "ApiDatabasePath must be provided")
            .Validate(x => !string.IsNullOrWhiteSpace(x.apiKeyHashPepper), MissingConfigMessage("api:apiKeyHashPepper", "DZB_api__apiKeyHashPepper"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.masterEncryptionKey), MissingConfigMessage("api:masterEncryptionKey", "DZB_api__masterEncryptionKey"));

        return services;
    }

    public static void ValidateRequiredSecrets(IConfiguration config)
    {
        var missingSecrets = new List<string>();

        AddMissingSecret(config, missingSecrets, "discord:apiToken", "DZB_discord__apiToken");
        AddMissingSecret(config, missingSecrets, "api:apiKeyHashPepper", "DZB_api__apiKeyHashPepper");
        AddMissingSecret(config, missingSecrets, "api:masterEncryptionKey", "DZB_api__masterEncryptionKey");

        if (missingSecrets.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required production secrets. Configure them using environment variables: " +
                string.Join("; ", missingSecrets));
        }
    }

    private static void AddMissingSecret(IConfiguration config, List<string> missingSecrets, string configPath, string environmentVariable)
    {
        if (string.IsNullOrWhiteSpace(config[configPath]))
        {
            missingSecrets.Add($"{configPath} (environment variable: {environmentVariable})");
        }
    }

    private static string MissingConfigMessage(string configPath, string environmentVariable)
    {
        return $"{configPath} must be provided. Expected environment variable: {environmentVariable}";
    }

    public static IServiceCollection AddApplicationInfrastructure(this IServiceCollection services, AppApiConfig apiConfig, string masterKey)
    {
        var databasePath = DbPath.GetDatabasePath(apiConfig.databasePath);
        var dbDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Using DbContextFactory to allow scoped DbContext in singleton services
        services.AddDbContextFactory<ApiSecurityDbContext>(opts => opts.UseSqlite($"Data Source={databasePath}"));

        // Discord Client and Interaction Service
        services.AddSingleton(new DiscordSocketConfig { GatewayIntents = GatewayIntents.DirectMessages, AlwaysDownloadUsers = false, ConnectionTimeout = 30000, LogLevel = LogSeverity.Verbose });
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig { DefaultRunMode = Discord.Interactions.RunMode.Async }));

        // 3. Application Services
        services.AddMemoryCache();
        services.AddSingleton<InteractionHandler>();
        services.AddSingleton<IApiSecurityStore, ApiSecurityStore>();
        services.AddSingleton<IEncryptionService>(sp => new EncryptionService(masterKey, sp.GetRequiredService<ILogger<EncryptionService>>()));
        services.AddSingleton<DiscordStateService>();
        services.AddSingleton<FirstRunAdminSetupService>();
        services.AddHttpClient<ZabbixService>();
        services.AddSingleton<ZabbixService>();
        services.AddSingleton<IDiscordUiService, DiscordUiService>();
        services.AddSingleton<IPaginationService, PaginationService>();
        services.AddSingleton<IDiscordTargetSyncService, DiscordTargetSyncService>();
        services.AddSingleton<IApplicationEmoteCache, ApplicationEmoteCache>();

        // Data Repositories
        services.AddSingleton<IntegrationClientRepository, ApiClientRepository>();
        services.AddSingleton<KnownDeliveryTargetRepository, ApiTargetRepository>();
        services.AddSingleton<SystemAdministratorRepository, BotAdminRepository>();

        services.AddControllers();
        services.AddOpenApi();

        return services;
    }

    public static IServiceCollection AddApplicationSecurity(this IServiceCollection services, AppApiConfig apiConfig)
    {
        const string apiKeyScheme = "ApiKey";

        services.AddAuthentication(apiKeyScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(apiKeyScheme, _ => { });

        services.AddAuthorization(opts =>
            opts.AddPolicy("ZabbixIngress", policy => policy.AddAuthenticationSchemes(apiKeyScheme).RequireAuthenticatedUser())
        );

        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxyAddress in apiConfig.knownProxies)
            {
                if (IPAddress.TryParse(proxyAddress, out var ipAddress)) opts.KnownProxies.Add(ipAddress);
            }
        });

        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.AddPolicy("zabbix-api", httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = apiConfig.rateLimitPermit,
                    Window = TimeSpan.FromSeconds(apiConfig.rateLimitWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }
}
