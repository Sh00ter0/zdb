using Client.Data;
using Client.Data.Repositories;
using Client.Security;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Handlers
{
    public class ApiClientAutocompleteHandler : BaseAutocompleteHandler
    {
        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;

        public ApiClientAutocompleteHandler(
            ILogger<ApiClientAutocompleteHandler> logger,
            IDbContextFactory<ApiSecurityDbContext> dbContextFactory)
            : base(logger)
        {
            _dbContextFactory = dbContextFactory;
        }

        protected override async Task<IEnumerable<AutocompleteResult>> GetSuggestionsAsync(string userInput, IInteractionContext context)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var query = dbContext.IntegrationClients.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                query = query.Where(c => c.Name.Contains(userInput));
            }

            var matchingClients = await query
                .OrderBy(c => c.Name)
                .Select(c => new AutocompleteResult(
                    c.IsActive ? c.Name : $"{c.Name} (Disabled)",
                    c.Name))
                .ToListAsync();

            return matchingClients;
        }
    }
    public class ApiTargetAutocompleteHandler : BaseAutocompleteHandler
    {
        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;

        public ApiTargetAutocompleteHandler(
            ILogger<ApiTargetAutocompleteHandler> logger,
            IDbContextFactory<ApiSecurityDbContext> dbContextFactory)
            : base(logger)
        {
            _dbContextFactory = dbContextFactory;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var apiAdminRepository = services.GetRequiredService<SystemAdministratorRepository>();
            if (!await apiAdminRepository.IsActiveAsync(context.User.Id))
            {
                return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
            }

            var clientOption = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == "client");
            var clientName = clientOption?.Value?.ToString();

            if (string.IsNullOrWhiteSpace(clientName))
            {
                return AutocompletionResult.FromSuccess(new[] {
                new AutocompleteResult("⚠️ Select a client first", "none")
            });
            }

            string userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var allClientTargets = await dbContext.KnownDeliveryTargets
                .AsNoTracking()
                .Where(t => t.IntegrationClient.Name == clientName)
                .OrderBy(t => t.Name)
                .Select(t => new AutocompleteResult($"{t.Name} ({t.ChannelType})", t.TargetId.ToString()))
                .ToListAsync();

            if (!allClientTargets.Any())
            {
                return AutocompletionResult.FromSuccess(new[] {
                new AutocompleteResult("❌ This client has no targets assigned", "none")
            });
            }

            var filteredResults = allClientTargets
                .Where(x => x.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!filteredResults.Any() && !string.IsNullOrWhiteSpace(userInput))
            {
                return AutocompletionResult.FromSuccess(new[] {
                new AutocompleteResult($"⚠️ Clear '{userInput}' to see targets for {clientName}", "none")
            });
            }

            var finalResults = filteredResults.Any() ? filteredResults : allClientTargets;
            return AutocompletionResult.FromSuccess(finalResults.Take(25));
        }

        protected override Task<IEnumerable<AutocompleteResult>> GetSuggestionsAsync(string userInput, IInteractionContext context)
            => Task.FromResult(Enumerable.Empty<AutocompleteResult>());
    }
}
