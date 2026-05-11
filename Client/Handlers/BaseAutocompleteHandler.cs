using Application.Repositories;
using Discord;
using Discord.Interactions;

public abstract class BaseAutocompleteHandler : AutocompleteHandler
{
    protected readonly ILogger Logger;

    protected BaseAutocompleteHandler(ILogger logger)
    {
        Logger = logger;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        try
        {
            var apiBotAdminRepository = services.GetRequiredService<ISystemAdministratorRepository>();

            if (!await apiBotAdminRepository.IsActiveAsync(context.User.Id))
            {
                return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
            }

            string userInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? "";
            var results = await GetSuggestionsAsync(userInput, context);

            return AutocompletionResult.FromSuccess(results.Take(25));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while generating autocomplete suggestions.");
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
        }
    }

    protected abstract Task<IEnumerable<AutocompleteResult>> GetSuggestionsAsync(string userInput, IInteractionContext context);
}
