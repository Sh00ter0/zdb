using Application.Common.API;
using Application.Discord.Panels.ClientPanel;
using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Orchestration;
using Application.Discord.Panels.LayoutBuilders;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;
using Application.Discord.Panels.Middleware;
using Application.Discord.Panels.Modals;
using Application.Discord.Panels.Modals.ClientPanel;
using Application.Discord.Panels.Rendering;
using Application.Discord.Panels.Rendering.ClientPanel;
using Application.Discord.Panels.Rendering.LayoutMappers;
using Application.Discord.Panels.Rendering.Shared;
using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Application.Services.Pagination;
using Client.Handlers;
using Client.Middleware;
using Client.Middleware.Exceptions;
using Client.Models;
using Client.Policies.Handlers;
using Client.Policies.Requirements;
using Client.Security;
using Client.Vault;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Domain.Enums;
using Infrastructure.Discord.Events;
using Infrastructure.Discord.SlashCommands.Commands;
using Infrastructure.Discord.SlashCommands.Commands.Controllers;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Api;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.Client;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.WellKnownTargets;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.System;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Zabbix;
using Infrastructure.Logging;
using Infrastructure.Mediators;
using Infrastructure.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.API;
using Infrastructure.Services.Discord;
using Infrastructure.Services.Zabbix;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Net;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<LogSanitizerEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:j}{NewLine}{Exception}",
        theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

try
{
    Log.Information("The application is starting, please wait...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        };
    });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<LogSanitizerEnricher>()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:j}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code));

    builder.AddEnterpriseSecrets();

    HostingExtensions.ValidateRequiredSecrets(builder.Configuration);

    builder.Services.AddApplicationOptions(builder.Configuration);

    var apiConfig = builder.Configuration.GetSection("api").Get<AppApiConfig>() ?? new AppApiConfig();
    var masterKey = EncryptionKeyGuard.EnsureKeyOrExit(apiConfig);

    builder.Services.AddApplicationInfrastructure(apiConfig, masterKey);
    builder.Services.AddApplicationSecurity(apiConfig);

    builder.Services.AddHostedService<StartupService>();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseForwardedHeaders();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseMiddleware<SecureRequestMiddleware>();
        app.UseMiddleware<DiscordStatusMiddleware>();
    }

    app.UseRouting();
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
                "Missing required production secrets. Configure them in Vault or environment variables: " +
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
        return $"{configPath} must be provided. Source: Vault or ENV: {environmentVariable}";
    }

    public static IServiceCollection AddApplicationInfrastructure(this IServiceCollection services, AppApiConfig apiConfig, string masterKey)
    {
        var databasePath = DbPath.GetDatabasePath(apiConfig.databasePath);
        var dbDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        services.AddDbContextFactory<ApiSecurityDbContext>(opts => opts.UseSqlite($"Data Source={databasePath}", b => b.MigrationsAssembly("Infrastructure")));

        services.AddSingleton(new DiscordSocketConfig { GatewayIntents = GatewayIntents.DirectMessages, AlwaysDownloadUsers = false, LogLevel = LogSeverity.Verbose });
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<DiscordStartupService>();
        services.AddSingleton(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig { DefaultRunMode = Discord.Interactions.RunMode.Async }));

        services.AddMemoryCache();
        services.AddSingleton<InteractionHandler>();
        services.AddSingleton<IApiSecurityStore, ApiSecurityStore>();
        services.AddSingleton<IEncryptionService>(sp => new EncryptionService(masterKey, sp.GetRequiredService<ILogger<EncryptionService>>()));
        services.AddSingleton<DiscordStateService>();
        services.AddSingleton<DiscordStateMediator>();

        services.AddHttpClient();
        services.AddHostedService<DiscordWatchdogService>();

        services.AddSingleton<FirstRunAdminSetupService>();
        services.AddHttpClient<ZabbixService>();
        services.AddSingleton<ZabbixService>();
        services.AddSingleton<IDiscordUiService, DiscordUiService>();
        services.AddSingleton<IPaginationService, PaginationService>();
        services.AddSingleton<IDiscordTargetSyncService, DiscordTargetSyncService>();
        services.AddSingleton<IDiscordEmoteService, DiscordEmoteService>();
        services.AddScoped<DiscordAlertService>();

        services.AddSingleton<IIntegrationClientRepository, ApiClientRepository>();
        services.AddSingleton<IKnownDeliveryTargetRepository, ApiTargetRepository>();
        services.AddSingleton<ISystemAdministratorRepository, BotAdminRepository>();


        // +++ // Start

        services.AddSingleton<IInteractionCodec, CompactInteractionCodec>();
        services.AddSingleton<IPanelRegistry>(sp =>
        {
            var panels = sp.GetServices<IConfigPanel>();
            return new PanelRegistry(panels);
        });


        // --- ORCHESTRATION ---
        services.AddSingleton<InteractionPipeline>();
        services.AddSingleton<ModalCoordinator>();
        services.AddSingleton<InteractionResponseHandler>();
        services.AddSingleton<IInteractionErrorBoundary, DefaultErrorBoundary>(); // NEW: Error Boundary
        services.AddSingleton<InteractionDispatcher>();

        // --- MIDDLEWARES ---
        // Note: Order of registration dictates the execution pipeline
        services.AddSingleton<IPanelMiddleware, LoggingMiddleware>();

        // --- PANELS ---
        services.AddSingleton<IConfigPanel, ClientPanel>();

        // --- ACTION HANDLERS (CLIENT PANEL) ---
        // Navigation & Routing Actions
        services.AddSingleton<IPanelActionHandler, OpenStatusMenuAction>();
        services.AddSingleton<IPanelActionHandler, OpenTargetsAction>();
        services.AddSingleton<IPanelActionHandler, OpenRenameModalAction>();
        services.AddSingleton<IPanelActionHandler, OpenZabbixModalAction>();
        services.AddSingleton<IPanelActionHandler, PromptRenewAction>();
        services.AddSingleton<IPanelActionHandler, PromptDeleteAction>();
        services.AddSingleton<IPanelActionHandler, BackToClientOverviewAction>();
        services.AddSingleton<IPanelActionHandler, CloseClientPanelAction>();

        // Submit & Execution Actions
        services.AddSingleton<IPanelActionHandler, ToggleClientStatusAction>();
        services.AddSingleton<IPanelActionHandler, RenameSubmitAction>();
        services.AddSingleton<IPanelActionHandler, ZabbixSubmitAction>();
        services.AddSingleton<IPanelActionHandler, ConfirmRenewAction>();
        services.AddSingleton<IPanelActionHandler, ConfirmDeleteAction>();

        // --- LAYOUT BUILDERS ---
        services.AddSingleton<ClientOverviewLayoutBuilder>();
        services.AddSingleton<ClientStatusLayoutBuilder>();
        services.AddSingleton<ClientTargetsLayoutBuilder>();
        services.AddSingleton<ClientWarningLayoutBuilder>();
        services.AddSingleton<ClientDeletedLayoutBuilder>();
        services.AddSingleton<PanelErrorLayoutBuilder>();

        // --- LAYOUT MAPPERS ---
        services.AddSingleton<ButtonMapper>();
        services.AddSingleton<SelectMenuMapper>();
        services.AddSingleton<TextMapper>();
        services.AddSingleton<SectionMapper>();
        services.AddSingleton<SeparatorMapper>();
        services.AddSingleton<ActionRowMapper>();
        services.AddSingleton<ContainerMapper>();
        services.AddSingleton<ModalMapper>();
        services.AddSingleton<EmbedMapper>();

        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<ContainerMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<SectionMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<TextMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<SeparatorMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<ActionRowMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<ButtonMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<SelectMenuMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<ModalMapper>());
        services.AddSingleton<ILayoutComponentMapper>(sp => sp.GetRequiredService<EmbedMapper>());

        // --- RENDERERS ---
        services.AddSingleton<DiscordLayoutMapper>();
        services.AddSingleton<IPanelRenderer, DiscordPanelRenderer>(); // Main delegator

        // Specific View Renderers
        services.AddSingleton<IPanelViewRenderer, ClientOverviewRenderer>();
        services.AddSingleton<IPanelViewRenderer, ClientStatusRenderer>();
        services.AddSingleton<IPanelViewRenderer, ClientTargetsRenderer>();
        services.AddSingleton<IPanelViewRenderer, ClientWarningRenderer>();
        services.AddSingleton<IPanelViewRenderer, ClientDeletedRenderer>();

        // Shared / System Renderers
        services.AddSingleton<IPanelViewRenderer, PanelErrorRenderer>(); // NEW: Error Boundary Renderer

        // --- MODAL FACTORIES ---
        services.AddSingleton<IModalFactory, RenameClientModalFactory>();
        services.AddSingleton<IModalFactory, UpdateZabbixModalFactory>();
        // +++ // End



        services.AddTransient<ClientCommandsController>();
        services.AddTransient<WellKnownTargetsController>();
        services.AddTransient<AdministrationCommandsController>();
        services.AddTransient<ZabbixDirectMessageController>();

        services.AddControllers();
        services.AddOpenApi();

        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationHandler, TargetAccessHandler>();

        return services;
    }

    public static IServiceCollection AddApplicationSecurity(this IServiceCollection services, AppApiConfig apiConfig)
    {
        const string apiKeyScheme = "ApiKey";

        services.AddAuthentication(apiKeyScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(apiKeyScheme, _ => { });

        services.AddAuthorization(opts =>
        {
            opts.AddPolicy(Policy.ZabbixIngress, policy => policy.AddAuthenticationSchemes(apiKeyScheme).RequireAuthenticatedUser());
            opts.AddPolicy(Policy.TargetAccess, policy => policy.RequireAuthenticatedUser().AddRequirements(new TargetAccessRequirement()));
        });

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
# if DEBUG
// xUnit
public partial class Program { }
#endif
